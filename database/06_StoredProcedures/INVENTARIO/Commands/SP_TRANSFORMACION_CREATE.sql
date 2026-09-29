/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: SP_TRANSFORMACION_CREATE
Archivo: SP_TRANSFORMACION_CREATE.sql | Procedimiento: INVENTARIO.SP_TRANSFORMACION_CREATE | Tipo: COMMAND
Versión: 1.0.1 | Fecha: 2026-09-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Transfiere contenido de un lote origen a activos destino conservando genealogía y merma.
Parámetros de entrada: @I_CANTIDAD_MERMA, @I_CANTIDAD_ORIGEN, @I_DESTINOS_JSON, @I_FECHA_TRANSFORMACION_UTC, @I_ID_ACTIVO_ORIGEN, @I_ID_EMPRESA, @I_ID_LOTE_ORIGEN, @I_ID_PRODUCTO_CONTENIDO, @I_METODO_MEDICION, @I_MOTIVO_MERMA, @I_OBSERVACION, @S_ID_SESION, @S_ID_USUARIO.
Parámetros de salida: @O_CODIGO_ERROR, @O_FILAS_AFECTADAS, @O_ID_TRANSFORMACION, @O_MENSAJE.
Retorno: Informa el resultado mediante los parámetros de salida declarados.
Tablas utilizadas: INVENTARIO.ACTIVOS - SELECT; INVENTARIO.ACTIVOS_CONTENIDOS - SELECT; INVENTARIO.ACTIVOS_EVENTOS - INSERT; INVENTARIO.LOTES - SELECT; INVENTARIO.LOTES_EXISTENCIAS - SELECT/UPDATE; INVENTARIO.TRANSFORMACIONES - INSERT; INVENTARIO.TRANSFORMACIONES_DETALLES - INSERT.
Transacción: Abre una transacción explícita para agrupar sus escrituras.
Auditoría: No registra auditoría explícita dentro del procedimiento.
Historial: 1.0.0 | 2026-08-24 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.0.1 | 2026-09-20 | FABRICA | Agustin Omar Cauzi | Documentación técnica completa del procedimiento.
===============================================================================
*/
CREATE OR ALTER PROCEDURE [INVENTARIO].[SP_TRANSFORMACION_CREATE]
    @I_ID_EMPRESA BIGINT,
    @I_ID_LOTE_ORIGEN BIGINT,
    @I_ID_ACTIVO_ORIGEN BIGINT=NULL,
    @I_ID_PRODUCTO_CONTENIDO BIGINT,
    @I_FECHA_TRANSFORMACION_UTC DATETIME2 (3),
    @I_CANTIDAD_ORIGEN DECIMAL (19, 4),
    @I_CANTIDAD_MERMA DECIMAL (19, 4),
    @I_METODO_MEDICION NVARCHAR (30),
    @I_MOTIVO_MERMA NVARCHAR (500)=NULL,
    @I_OBSERVACION NVARCHAR (1000)=NULL,
    @I_DESTINOS_JSON NVARCHAR (MAX),
    @S_ID_SESION BIGINT,
    @S_ID_USUARIO BIGINT,
    @O_ID_TRANSFORMACION BIGINT OUTPUT,
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
    IF ISJSON(@I_DESTINOS_JSON) <> 1
        THROW 50009, 'Los destinos del fraccionamiento no son válidos.', 1;
    DECLARE @V_DESTINOS TABLE (
        [ID_ACTIVO] BIGINT          PRIMARY KEY,
        [ID_LOTE]   BIGINT         ,
        [CANTIDAD]  DECIMAL (19, 4));
    INSERT @V_DESTINOS
    SELECT [AssetId],
           [LotId],
           [Quantity]
    FROM   OPENJSON (@I_DESTINOS_JSON) WITH ([AssetId] BIGINT, [LotId] BIGINT, [Quantity] DECIMAL (19, 4));
    IF NOT EXISTS (SELECT 1
                   FROM   @V_DESTINOS)
       OR EXISTS (SELECT 1
                  FROM   @V_DESTINOS
                  WHERE  [CANTIDAD] <= 0)
       OR (SELECT SUM([CANTIDAD])
           FROM   @V_DESTINOS) + @I_CANTIDAD_MERMA <> @I_CANTIDAD_ORIGEN
        THROW 50009, 'El origen debe coincidir con destinos más merma.', 1;
    BEGIN TRANSACTION;
    DECLARE @V_DISPONIBLE AS DECIMAL (19, 4);
    SELECT @V_DISPONIBLE = [CANTIDAD]
    FROM   [INVENTARIO].[LOTES_EXISTENCIAS] WITH (UPDLOCK, HOLDLOCK)
    WHERE  [ID_EMPRESA] = @I_ID_EMPRESA
           AND [ID_LOTE] = @I_ID_LOTE_ORIGEN;
    IF @V_DISPONIBLE IS NULL
       OR @V_DISPONIBLE < @I_CANTIDAD_ORIGEN
       OR NOT EXISTS (SELECT 1
                      FROM   [INVENTARIO].[LOTES]
                      WHERE  [ID_LOTE] = @I_ID_LOTE_ORIGEN
                             AND [ID_EMPRESA] = @I_ID_EMPRESA
                             AND [ID_PRODUCTO] = @I_ID_PRODUCTO_CONTENIDO)
       OR EXISTS (SELECT 1
                  FROM   @V_DESTINOS AS [T]
                  WHERE  NOT EXISTS (SELECT 1
                                     FROM   [INVENTARIO].[ACTIVOS] AS [A]
                                     WHERE  [A].[ID_ACTIVO] = [T].[ID_ACTIVO]
                                            AND [A].[ID_EMPRESA] = @I_ID_EMPRESA
                                            AND [A].[CODIGO_ESTADO] = N'DISPONIBLE')
                         OR [T].[ID_LOTE] <> @I_ID_LOTE_ORIGEN)
       OR EXISTS (SELECT 1
                  FROM   @V_DESTINOS AS [T]
                         INNER JOIN
                         [INVENTARIO].[ACTIVOS_CONTENIDOS] AS [C]
                         ON [C].[ID_ACTIVO] = [T].[ID_ACTIVO]
                            AND [C].[CODIGO_ESTADO] = N'ACTIVO'
                  WHERE  [C].[ID_PRODUCTO_CONTENIDO] <> @I_ID_PRODUCTO_CONTENIDO
                         OR [C].[ID_LOTE] <> [T].[ID_LOTE])
        BEGIN
            SET @O_CODIGO_ERROR = 30006;
            SET @O_MENSAJE = N'El origen no tiene saldo o algún destino no es compatible.';
            ROLLBACK;
            RETURN;
        END
    UPDATE [INVENTARIO].[LOTES_EXISTENCIAS]
    SET    [CANTIDAD]                = [CANTIDAD] - @I_CANTIDAD_ORIGEN,
           [FECHA_MODIFICACION_UTC]  = SYSUTCDATETIME(),
           [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO
    WHERE  [ID_EMPRESA] = @I_ID_EMPRESA
           AND [ID_LOTE] = @I_ID_LOTE_ORIGEN;
    DECLARE @V_CODIGO AS NVARCHAR (30) = N'TRF-' + RIGHT(REPLICATE(N'0', 6) + CONVERT (NVARCHAR (20),  NEXT VALUE FOR [INVENTARIO].[SEQ_TRANSFORMACION_CODIGO]), 6);
    INSERT  [INVENTARIO].[TRANSFORMACIONES] ([ID_EMPRESA], [ID_LOTE_ORIGEN], [ID_PRODUCTO_CONTENIDO], [ID_ACTIVO_ORIGEN], [CODIGO], [FECHA_TRANSFORMACION_UTC], [CANTIDAD_ORIGEN], [CANTIDAD_MERMA], [METODO_MEDICION], [MOTIVO_MERMA], [OBSERVACION], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
    VALUES                                 (@I_ID_EMPRESA, @I_ID_LOTE_ORIGEN, @I_ID_PRODUCTO_CONTENIDO, @I_ID_ACTIVO_ORIGEN, @V_CODIGO, @I_FECHA_TRANSFORMACION_UTC, @I_CANTIDAD_ORIGEN, @I_CANTIDAD_MERMA, @I_METODO_MEDICION, @I_MOTIVO_MERMA, @I_OBSERVACION, N'CONFIRMADA', @S_ID_USUARIO);
    SET @O_ID_TRANSFORMACION = SCOPE_IDENTITY();
    MERGE INTO [INVENTARIO].[ACTIVOS_CONTENIDOS] WITH (HOLDLOCK)
     AS [C]
    USING @V_DESTINOS AS [T] ON [C].[ID_ACTIVO] = [T].[ID_ACTIVO]
                                AND [C].[CODIGO_ESTADO] = N'ACTIVO'
    WHEN MATCHED AND [C].[ID_PRODUCTO_CONTENIDO] = @I_ID_PRODUCTO_CONTENIDO
                     AND [C].[ID_LOTE] = [T].[ID_LOTE] THEN UPDATE
    SET [C].[CANTIDAD_ACTUAL]         = [C].[CANTIDAD_ACTUAL] + [T].[CANTIDAD],
        [C].[FECHA_MODIFICACION_UTC]  = SYSUTCDATETIME(),
        [C].[ID_USUARIO_MODIFICACION] = @S_ID_USUARIO
    WHEN NOT MATCHED THEN INSERT ([ID_EMPRESA], [ID_ACTIVO], [ID_PRODUCTO_CONTENIDO], [ID_LOTE], [CANTIDAD_ACTUAL], [CODIGO_ESTADO], [ID_USUARIO_ALTA]) VALUES (@I_ID_EMPRESA, [T].[ID_ACTIVO], @I_ID_PRODUCTO_CONTENIDO, [T].[ID_LOTE], [T].[CANTIDAD], N'ACTIVO', @S_ID_USUARIO);
    INSERT [INVENTARIO].[TRANSFORMACIONES_DETALLES] ([ID_TRANSFORMACION], [ID_ACTIVO_DESTINO], [ID_LOTE_DESTINO], [CANTIDAD_ANTES], [CANTIDAD_CARGADA], [CANTIDAD_DESPUES], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
    SELECT @O_ID_TRANSFORMACION,
           [T].[ID_ACTIVO],
           [T].[ID_LOTE],
           [C].[CANTIDAD_ACTUAL] - [T].[CANTIDAD],
           [T].[CANTIDAD],
           [C].[CANTIDAD_ACTUAL],
           N'ACTIVO',
           @S_ID_USUARIO
    FROM   @V_DESTINOS AS [T]
           INNER JOIN
           [INVENTARIO].[ACTIVOS_CONTENIDOS] AS [C]
           ON [C].[ID_ACTIVO] = [T].[ID_ACTIVO]
              AND [C].[ID_LOTE] = [T].[ID_LOTE]
              AND [C].[CODIGO_ESTADO] = N'ACTIVO';
    INSERT [INVENTARIO].[ACTIVOS_EVENTOS] ([ID_EMPRESA], [ID_ACTIVO], [TIPO_EVENTO], [FECHA_EVENTO_UTC], [CODIGO_ESTADO_ANTES], [CODIGO_ESTADO_DESPUES], [CONDICION_ANTES], [CONDICION_DESPUES], [CANTIDAD_ANTES], [CANTIDAD_DESPUES], [ID_TRANSFORMACION], [OBSERVACION], [ID_CORRELACION], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
    SELECT @I_ID_EMPRESA,
           [A].[ID_ACTIVO],
           N'FRACCIONAMIENTO',
           @I_FECHA_TRANSFORMACION_UTC,
           [A].[CODIGO_ESTADO],
           [A].[CODIGO_ESTADO],
           [A].[CONDICION_ACTUAL],
           [A].[CONDICION_ACTUAL],
           [C].[CANTIDAD_ACTUAL] - [T].[CANTIDAD],
           [C].[CANTIDAD_ACTUAL],
           @O_ID_TRANSFORMACION,
           @I_OBSERVACION,
           NEWID(),
           N'CONFIRMADO',
           @S_ID_USUARIO
    FROM   @V_DESTINOS AS [T]
           INNER JOIN
           [INVENTARIO].[ACTIVOS] AS [A]
           ON [A].[ID_ACTIVO] = [T].[ID_ACTIVO]
           INNER JOIN
           [INVENTARIO].[ACTIVOS_CONTENIDOS] AS [C]
           ON [C].[ID_ACTIVO] = [T].[ID_ACTIVO]
              AND [C].[ID_LOTE] = [T].[ID_LOTE]
              AND [C].[CODIGO_ESTADO] = N'ACTIVO';
    SET @O_FILAS_AFECTADAS = 1 + (SELECT COUNT(*)
                                  FROM   @V_DESTINOS);
    COMMIT TRANSACTION;
END
