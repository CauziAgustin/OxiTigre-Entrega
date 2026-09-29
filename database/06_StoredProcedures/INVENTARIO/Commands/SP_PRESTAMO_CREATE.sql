/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: SP_PRESTAMO_CREATE
Archivo: SP_PRESTAMO_CREATE.sql | Procedimiento: INVENTARIO.SP_PRESTAMO_CREATE | Tipo: COMMAND
Versión: 1.0.1 | Fecha: 2026-09-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Registra préstamo y snapshot de salida de activos sin cambiar propietario.
Parámetros de entrada: @I_ACTIVOS_JSON, @I_DESTINO_EXTERNO, @I_EMPRESA_DESTINO_CODIGO, @I_FECHA_DEVOLUCION_PREVISTA, @I_FECHA_SALIDA_UTC, @I_ID_CLIENTE_DESTINO, @I_ID_CORRELACION_INTEREMPRESA, @I_ID_EMPRESA, @I_ID_SUCURSAL_DESTINO, @I_MODALIDAD_ENTREGA, @I_OBSERVACION, @I_TIPO_DESTINO, @S_ID_SESION, @S_ID_USUARIO.
Parámetros de salida: @O_CODIGO_ERROR, @O_FILAS_AFECTADAS, @O_ID_PRESTAMO, @O_MENSAJE.
Retorno: Informa el resultado mediante los parámetros de salida declarados.
Tablas utilizadas: COMERCIAL.CLIENTES - SELECT; CONFIGURACION.SUCURSALES - SELECT; INVENTARIO.ACTIVOS - SELECT; INVENTARIO.ACTIVOS_EVENTOS - INSERT; INVENTARIO.PRESTAMOS - INSERT; INVENTARIO.PRESTAMOS_DETALLES - INSERT; INVENTARIO.PRODUCTOS - SELECT.
Transacción: Abre una transacción explícita para agrupar sus escrituras.
Auditoría: No registra auditoría explícita dentro del procedimiento.
Historial: 1.0.0 | 2026-08-24 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.0.1 | 2026-09-20 | FABRICA | Agustin Omar Cauzi | Documentación técnica completa del procedimiento.
===============================================================================
*/
CREATE OR ALTER PROCEDURE [INVENTARIO].[SP_PRESTAMO_CREATE]
    @I_ID_EMPRESA BIGINT,
    @I_TIPO_DESTINO NVARCHAR (30),
    @I_ID_SUCURSAL_DESTINO BIGINT=NULL,
    @I_ID_CLIENTE_DESTINO BIGINT=NULL,
    @I_DESTINO_EXTERNO NVARCHAR (200)=NULL,
    @I_MODALIDAD_ENTREGA NVARCHAR (30),
    @I_FECHA_SALIDA_UTC DATETIME2 (3),
    @I_FECHA_DEVOLUCION_PREVISTA DATETIME2 (3)=NULL,
    @I_OBSERVACION NVARCHAR (1000)=NULL,
    @I_EMPRESA_DESTINO_CODIGO NVARCHAR (30)=NULL,
    @I_ID_CORRELACION_INTEREMPRESA UNIQUEIDENTIFIER=NULL,
    @I_ACTIVOS_JSON NVARCHAR (MAX),
    @S_ID_SESION BIGINT,
    @S_ID_USUARIO BIGINT,
    @O_ID_PRESTAMO BIGINT OUTPUT,
    @O_CODIGO_ERROR BIGINT OUTPUT,
    @O_MENSAJE NVARCHAR (4000) OUTPUT,
    @O_FILAS_AFECTADAS INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @O_CODIGO_ERROR = NULL;
    SET @O_MENSAJE = NULL;
    SET @O_FILAS_AFECTADAS = 0;
    IF ISJSON(@I_ACTIVOS_JSON) <> 1
        THROW 50012, 'Los activos del préstamo no son válidos.', 1;
    DECLARE @V_ACTIVOS TABLE (
        [ID_ACTIVO] BIGINT          PRIMARY KEY,
        [ID_LOTE]   BIGINT          NULL,
        [CANTIDAD]  DECIMAL (19, 4) NULL,
        [CONDICION] NVARCHAR (30)  );
    INSERT @V_ACTIVOS
    SELECT [AssetId],
           [LotId],
           [Quantity],
           [ConditionCode]
    FROM   OPENJSON (@I_ACTIVOS_JSON) WITH ([AssetId] BIGINT, [LotId] BIGINT, [Quantity] DECIMAL (19, 4), [ConditionCode] NVARCHAR (30));
    IF NOT EXISTS (SELECT 1
                   FROM   @V_ACTIVOS)
       OR @I_MODALIDAD_ENTREGA NOT IN (N'ENTREGA_PROPIA', N'RETIRO_DESTINATARIO', N'TERCERO')
       OR (@I_FECHA_DEVOLUCION_PREVISTA IS NOT NULL
           AND @I_FECHA_DEVOLUCION_PREVISTA < @I_FECHA_SALIDA_UTC)
        THROW 50012, 'El préstamo no es válido.', 1;
    IF (@I_TIPO_DESTINO = N'SUCURSAL'
        AND (@I_ID_SUCURSAL_DESTINO IS NULL
             OR @I_ID_CLIENTE_DESTINO IS NOT NULL
             OR @I_DESTINO_EXTERNO IS NOT NULL))
       OR (@I_TIPO_DESTINO = N'CLIENTE'
           AND (@I_ID_CLIENTE_DESTINO IS NULL
                OR @I_ID_SUCURSAL_DESTINO IS NOT NULL
                OR @I_DESTINO_EXTERNO IS NOT NULL))
       OR (@I_TIPO_DESTINO IN (N'TERCERO', N'INTEREMPRESA')
           AND (@I_DESTINO_EXTERNO IS NULL
                OR @I_ID_SUCURSAL_DESTINO IS NOT NULL
                OR @I_ID_CLIENTE_DESTINO IS NOT NULL))
       OR (@I_TIPO_DESTINO = N'INTEREMPRESA'
           AND @I_EMPRESA_DESTINO_CODIGO IS NULL)
       OR @I_TIPO_DESTINO NOT IN (N'SUCURSAL', N'CLIENTE', N'TERCERO', N'INTEREMPRESA')
        THROW 50012, 'El tipo y el destino del préstamo no coinciden.', 1;
    BEGIN TRANSACTION;
    IF (@I_TIPO_DESTINO = N'SUCURSAL'
        AND NOT EXISTS (SELECT 1
                        FROM   [CONFIGURACION].[SUCURSALES]
                        WHERE  [ID_SUCURSAL] = @I_ID_SUCURSAL_DESTINO
                               AND [ID_EMPRESA] = @I_ID_EMPRESA
                               AND [CODIGO_ESTADO] = N'ACTIVO'))
       OR (@I_TIPO_DESTINO = N'CLIENTE'
           AND NOT EXISTS (SELECT 1
                           FROM   [COMERCIAL].[CLIENTES]
                           WHERE  [ID_CLIENTE] = @I_ID_CLIENTE_DESTINO
                                  AND [ID_EMPRESA] = @I_ID_EMPRESA
                                  AND [CODIGO_ESTADO] = N'ACTIVO'))
        BEGIN
            SET @O_CODIGO_ERROR = 30009;
            SET @O_MENSAJE = N'El destino no existe, no está activo o no pertenece a la empresa.';
            ROLLBACK;
            RETURN;
        END
    IF EXISTS (SELECT 1
               FROM   @V_ACTIVOS AS [T]
               WHERE  NOT EXISTS (SELECT 1
                                  FROM   [INVENTARIO].[ACTIVOS] AS [A] WITH (UPDLOCK, HOLDLOCK)
                                         INNER JOIN
                                         [INVENTARIO].[PRODUCTOS] AS [P]
                                         ON [P].[ID_PRODUCTO] = [A].[ID_PRODUCTO]
                                  WHERE  [A].[ID_ACTIVO] = [T].[ID_ACTIVO]
                                         AND [A].[ID_EMPRESA] = @I_ID_EMPRESA
                                         AND [A].[CODIGO_ESTADO] = N'DISPONIBLE'
                                         AND [P].[ADMITE_PRESTAMO] = 1))
        BEGIN
            SET @O_CODIGO_ERROR = 30009;
            SET @O_MENSAJE = N'Algún activo no está disponible o no admite préstamos.';
            ROLLBACK;
            RETURN;
        END
    DECLARE @V_CODIGO AS NVARCHAR (30) = N'PRE-' + RIGHT(REPLICATE(N'0', 6) + CONVERT (NVARCHAR (20),  NEXT VALUE FOR [INVENTARIO].[SEQ_PRESTAMO_CODIGO]), 6);
    INSERT  [INVENTARIO].[PRESTAMOS] ([ID_EMPRESA], [CODIGO], [TIPO_DESTINO], [ID_SUCURSAL_DESTINO], [ID_CLIENTE_DESTINO], [DESTINO_EXTERNO], [MODALIDAD_ENTREGA], [FECHA_SALIDA_UTC], [FECHA_DEVOLUCION_PREVISTA], [OBSERVACION], [EMPRESA_DESTINO_CODIGO], [ID_CORRELACION_INTEREMPRESA], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
    VALUES                          (@I_ID_EMPRESA, @V_CODIGO, @I_TIPO_DESTINO, @I_ID_SUCURSAL_DESTINO, @I_ID_CLIENTE_DESTINO, @I_DESTINO_EXTERNO, @I_MODALIDAD_ENTREGA, @I_FECHA_SALIDA_UTC, @I_FECHA_DEVOLUCION_PREVISTA, @I_OBSERVACION, @I_EMPRESA_DESTINO_CODIGO, @I_ID_CORRELACION_INTEREMPRESA, N'PRESTADO', @S_ID_USUARIO);
    SET @O_ID_PRESTAMO = SCOPE_IDENTITY();
    INSERT [INVENTARIO].[PRESTAMOS_DETALLES] ([ID_PRESTAMO], [ID_ACTIVO], [ID_LOTE_SALIDA], [CANTIDAD_SALIDA], [CONDICION_SALIDA], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
    SELECT @O_ID_PRESTAMO,
           [ID_ACTIVO],
           [ID_LOTE],
           [CANTIDAD],
           [CONDICION],
           N'PRESTADO',
           @S_ID_USUARIO
    FROM   @V_ACTIVOS;
    UPDATE [A]
    SET    [CODIGO_ESTADO]           = N'PRESTADO',
           [FECHA_MODIFICACION_UTC]  = SYSUTCDATETIME(),
           [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO
    FROM   [INVENTARIO].[ACTIVOS] AS [A]
           INNER JOIN
           @V_ACTIVOS AS [T]
           ON [T].[ID_ACTIVO] = [A].[ID_ACTIVO];
    INSERT [INVENTARIO].[ACTIVOS_EVENTOS] ([ID_EMPRESA], [ID_ACTIVO], [TIPO_EVENTO], [FECHA_EVENTO_UTC], [CODIGO_ESTADO_ANTES], [CODIGO_ESTADO_DESPUES], [CONDICION_ANTES], [CONDICION_DESPUES], [CANTIDAD_ANTES], [CANTIDAD_DESPUES], [ID_PRESTAMO], [OBSERVACION], [ID_CORRELACION], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
    SELECT @I_ID_EMPRESA,
           [A].[ID_ACTIVO],
           N'PRESTAMO_SALIDA',
           @I_FECHA_SALIDA_UTC,
           N'DISPONIBLE',
           N'PRESTADO',
           [A].[CONDICION_ACTUAL],
           [T].[CONDICION],
           [T].[CANTIDAD],
           [T].[CANTIDAD],
           @O_ID_PRESTAMO,
           @I_OBSERVACION,
           COALESCE (@I_ID_CORRELACION_INTEREMPRESA, NEWID()),
           N'CONFIRMADO',
           @S_ID_USUARIO
    FROM   @V_ACTIVOS AS [T]
           INNER JOIN
           [INVENTARIO].[ACTIVOS] AS [A]
           ON [A].[ID_ACTIVO] = [T].[ID_ACTIVO];
    SET @O_FILAS_AFECTADAS = 1 + (SELECT COUNT(*)
                                  FROM   @V_ACTIVOS);
    COMMIT TRANSACTION;
END
