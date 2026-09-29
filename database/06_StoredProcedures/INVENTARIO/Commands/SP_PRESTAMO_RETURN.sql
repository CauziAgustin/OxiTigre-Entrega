/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: SP_PRESTAMO_RETURN
Archivo: SP_PRESTAMO_RETURN.sql | Procedimiento: INVENTARIO.SP_PRESTAMO_RETURN | Tipo: COMMAND
Versión: 1.0.1 | Fecha: 2026-09-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Registra devolución y diferencias conservando ambos snapshots.
Parámetros de entrada: @I_ACTIVOS_JSON, @I_FECHA_DEVOLUCION_UTC, @I_ID_EMPRESA, @I_ID_PRESTAMO, @I_OBSERVACION, @I_ROW_VERSION, @S_ID_SESION, @S_ID_USUARIO.
Parámetros de salida: @O_CODIGO_ERROR, @O_FILAS_AFECTADAS, @O_MENSAJE.
Retorno: Informa el resultado mediante los parámetros de salida declarados.
Tablas utilizadas: INVENTARIO.ACTIVOS - SELECT; INVENTARIO.ACTIVOS_EVENTOS - INSERT; INVENTARIO.PRESTAMOS - SELECT/UPDATE; INVENTARIO.PRESTAMOS_DETALLES - SELECT.
Transacción: Abre una transacción explícita para agrupar sus escrituras.
Auditoría: No registra auditoría explícita dentro del procedimiento.
Historial: 1.0.0 | 2026-08-24 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.0.1 | 2026-09-20 | FABRICA | Agustin Omar Cauzi | Documentación técnica completa del procedimiento.
===============================================================================
*/
CREATE OR ALTER PROCEDURE [INVENTARIO].[SP_PRESTAMO_RETURN]
    @I_ID_EMPRESA BIGINT,
    @I_ID_PRESTAMO BIGINT,
    @I_FECHA_DEVOLUCION_UTC DATETIME2 (3),
    @I_OBSERVACION NVARCHAR (1000)=NULL,
    @I_ACTIVOS_JSON NVARCHAR (MAX),
    @I_ROW_VERSION BINARY (8),
    @S_ID_SESION BIGINT,
    @S_ID_USUARIO BIGINT,
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
    DECLARE @V_DEVOLUCIONES TABLE (
        [ID_DETALLE]  BIGINT          PRIMARY KEY,
        [CANTIDAD]    DECIMAL (19, 4) NULL,
        [CONDICION]   NVARCHAR (30)  ,
        [OBSERVACION] NVARCHAR (500) );
    INSERT @V_DEVOLUCIONES
    SELECT [LoanLineId],
           [Quantity],
           [ConditionCode],
           [Observation]
    FROM   OPENJSON (@I_ACTIVOS_JSON) WITH ([LoanLineId] BIGINT, [Quantity] DECIMAL (19, 4), [ConditionCode] NVARCHAR (30), [Observation] NVARCHAR (500));
    BEGIN TRANSACTION;
    DECLARE @V_SALIDA AS DATETIME2 (3);
    SELECT @V_SALIDA = [FECHA_SALIDA_UTC]
    FROM   [INVENTARIO].[PRESTAMOS] WITH (UPDLOCK, HOLDLOCK)
    WHERE  [ID_PRESTAMO] = @I_ID_PRESTAMO
           AND [ID_EMPRESA] = @I_ID_EMPRESA
           AND [CODIGO_ESTADO] = N'PRESTADO'
           AND [ROW_VERSION] = @I_ROW_VERSION;
    IF @V_SALIDA IS NULL
       OR @I_FECHA_DEVOLUCION_UTC < @V_SALIDA
       OR NOT EXISTS (SELECT 1
                      FROM   @V_DEVOLUCIONES)
       OR EXISTS (SELECT 1
                  FROM   [INVENTARIO].[PRESTAMOS_DETALLES] AS [D]
                  WHERE  [D].[ID_PRESTAMO] = @I_ID_PRESTAMO
                         AND NOT EXISTS (SELECT 1
                                         FROM   @V_DEVOLUCIONES AS [T]
                                         WHERE  [T].[ID_DETALLE] = [D].[ID_PRESTAMO_DETALLE]))
        BEGIN
            SET @O_CODIGO_ERROR = 30009;
            SET @O_MENSAJE = N'El préstamo cambió o la devolución está incompleta.';
            ROLLBACK;
            RETURN;
        END
    UPDATE [D]
    SET    [CANTIDAD_DEVUELTA]       = [T].[CANTIDAD],
           [CONDICION_DEVOLUCION]    = [T].[CONDICION],
           [OBSERVACION_DEVOLUCION]  = [T].[OBSERVACION],
           [CODIGO_ESTADO]           = N'DEVUELTO',
           [FECHA_MODIFICACION_UTC]  = SYSUTCDATETIME(),
           [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO
    FROM   [INVENTARIO].[PRESTAMOS_DETALLES] AS [D]
           INNER JOIN
           @V_DEVOLUCIONES AS [T]
           ON [T].[ID_DETALLE] = [D].[ID_PRESTAMO_DETALLE]
    WHERE  [D].[ID_PRESTAMO] = @I_ID_PRESTAMO;
    UPDATE [A]
    SET    [CODIGO_ESTADO]           = CASE WHEN [T].[CONDICION] IN (N'OPERATIVO', N'BUENO') THEN N'DISPONIBLE' ELSE N'BLOQUEADO' END,
           [CONDICION_ACTUAL]        = [T].[CONDICION],
           [FECHA_MODIFICACION_UTC]  = SYSUTCDATETIME(),
           [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO
    FROM   [INVENTARIO].[ACTIVOS] AS [A]
           INNER JOIN
           [INVENTARIO].[PRESTAMOS_DETALLES] AS [D]
           ON [D].[ID_ACTIVO] = [A].[ID_ACTIVO]
           INNER JOIN
           @V_DEVOLUCIONES AS [T]
           ON [T].[ID_DETALLE] = [D].[ID_PRESTAMO_DETALLE];
    UPDATE [INVENTARIO].[PRESTAMOS]
    SET    [FECHA_DEVOLUCION_REAL_UTC] = @I_FECHA_DEVOLUCION_UTC,
           [OBSERVACION]               = CONCAT(COALESCE ([OBSERVACION], N''), CASE WHEN [OBSERVACION] IS NULL THEN N'' ELSE N' | ' END, @I_OBSERVACION),
           [CODIGO_ESTADO]             = N'DEVUELTO',
           [FECHA_MODIFICACION_UTC]    = SYSUTCDATETIME(),
           [ID_USUARIO_MODIFICACION]   = @S_ID_USUARIO
    WHERE  [ID_PRESTAMO] = @I_ID_PRESTAMO;
    INSERT [INVENTARIO].[ACTIVOS_EVENTOS] ([ID_EMPRESA], [ID_ACTIVO], [TIPO_EVENTO], [FECHA_EVENTO_UTC], [CODIGO_ESTADO_ANTES], [CODIGO_ESTADO_DESPUES], [CONDICION_ANTES], [CONDICION_DESPUES], [CANTIDAD_ANTES], [CANTIDAD_DESPUES], [ID_PRESTAMO], [OBSERVACION], [ID_CORRELACION], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
    SELECT @I_ID_EMPRESA,
           [A].[ID_ACTIVO],
           N'PRESTAMO_DEVOLUCION',
           @I_FECHA_DEVOLUCION_UTC,
           N'PRESTADO',
           [A].[CODIGO_ESTADO],
           [D].[CONDICION_SALIDA],
           [T].[CONDICION],
           [D].[CANTIDAD_SALIDA],
           [T].[CANTIDAD],
           @I_ID_PRESTAMO,
           COALESCE ([T].[OBSERVACION], @I_OBSERVACION),
           NEWID(),
           N'CONFIRMADO',
           @S_ID_USUARIO
    FROM   @V_DEVOLUCIONES AS [T]
           INNER JOIN
           [INVENTARIO].[PRESTAMOS_DETALLES] AS [D]
           ON [D].[ID_PRESTAMO_DETALLE] = [T].[ID_DETALLE]
           INNER JOIN
           [INVENTARIO].[ACTIVOS] AS [A]
           ON [A].[ID_ACTIVO] = [D].[ID_ACTIVO];
    SET @O_FILAS_AFECTADAS = 1 + (SELECT COUNT(*)
                                  FROM   @V_DEVOLUCIONES);
    COMMIT TRANSACTION;
END
