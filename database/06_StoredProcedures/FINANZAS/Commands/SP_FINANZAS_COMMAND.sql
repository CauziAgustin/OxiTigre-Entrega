/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: SP_FINANZAS_COMMAND
Archivo: SP_FINANZAS_COMMAND.sql | Procedimiento: FINANZAS.SP_FINANZAS_COMMAND | Tipo: COMMAND
Versión: 11.0.2 | Fecha: 2026-09-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Ejecuta acciones financieras no fiscales y conserva su trazabilidad.
Parámetros de entrada: @I_ACCION, @I_AFECTA_EFECTIVO, @I_CODIGO, @I_CODIGO_ESTADO, @I_FECHA_UTC, @I_ID, @I_ID_CLIENTE, @I_ID_EMPRESA, @I_ID_SESION_CAJA, @I_ID_SUCURSAL, @I_IMPORTE, @I_IMPORTE_CONTADO, @I_JSON_APLICACIONES, @I_JSON_MEDIOS, @I_MONEDA, @I_NOMBRE, @I_OBSERVACION, @I_REQUIERE_REFERENCIA, @I_ROW_VERSION, @I_TIPO, @S_ID_SESION, @S_ID_USUARIO.
Parámetros de salida: @O_CODIGO_ERROR, @O_FILAS_AFECTADAS, @O_ID, @O_MENSAJE.
Retorno: Informa el resultado mediante los parámetros de salida declarados.
Tablas utilizadas: COMERCIAL.CLIENTES - SELECT; COMERCIAL.VENTAS - SELECT; CONFIGURACION.SUCURSALES - SELECT; FINANZAS.APLICACIONES_PAGO_VENTA - INSERT/SELECT/UPDATE; FINANZAS.CAJAS - INSERT/SELECT/UPDATE; FINANZAS.MEDIOS_PAGO - INSERT/SELECT/UPDATE; FINANZAS.MOVIMIENTOS_CUENTA - INSERT; FINANZAS.PAGOS - INSERT/SELECT/UPDATE; FINANZAS.PAGOS_MEDIOS - INSERT/SELECT; FINANZAS.SESIONES_CAJA - INSERT/SELECT/UPDATE.
Transacción: Abre una transacción explícita para agrupar sus escrituras.
Auditoría: No registra auditoría explícita dentro del procedimiento.
Acciones admitidas mediante @I_ACCION:
- MEDIO_GUARDAR: crea o modifica un medio. Requiere código, nombre, tipo, indicadores y estado.
  Permiso API: FINANZAS.CONFIGURAR.
- CAJA_GUARDAR: crea o modifica una caja física. Requiere sucursal, código, nombre, moneda y estado.
  Permiso API: FINANZAS.CONFIGURAR.
- SESION_ABRIR: abre una caja activa. Requiere caja e importe inicial; un usuario y una caja solo admiten una apertura.
  Permiso API: FINANZAS.CAJA_GESTIONAR.
- SESION_CERRAR: cierra una apertura vigente. Requiere sesión, importe contado y rowversion.
  Permiso API: FINANZAS.CAJA_GESTIONAR.
- PAGO_REGISTRAR: registra un cobro. Requiere cliente, fecha, moneda, total, medios JSON y aplicaciones JSON opcionales.
  Permiso API: FINANZAS.COBRAR.
- PAGO_REVERSAR: compensa un cobro confirmado. Requiere pago, motivo y rowversion; no elimina registros.
  Permiso API: FINANZAS.REVERSAR.
Uso: enviar únicamente los parámetros indicados para la acción elegida; los demás permanecen NULL.
Historial: 11.0.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 11.0.1 | 2026-08-27 | FABRICA | Rechazo explícito de medios y ventas inexistentes.
Historial: 11.0.2 | 2026-09-20 | FABRICA | Agustin Omar Cauzi | Documentación técnica completa del procedimiento.
===============================================================================
*/
CREATE OR ALTER PROCEDURE [FINANZAS].[SP_FINANZAS_COMMAND]
    @I_ACCION NVARCHAR(30),
    @I_ID_EMPRESA BIGINT,
    @S_ID_SESION BIGINT,
    @S_ID_USUARIO BIGINT,
    @I_ID BIGINT = NULL,
    @I_ID_SUCURSAL BIGINT = NULL,
    @I_ID_CLIENTE BIGINT = NULL,
    @I_ID_SESION_CAJA BIGINT = NULL,
    @I_CODIGO NVARCHAR(30) = NULL,
    @I_NOMBRE NVARCHAR(100) = NULL,
    @I_TIPO NVARCHAR(30) = NULL,
    @I_MONEDA CHAR(3) = NULL,
    @I_FECHA_UTC DATETIME2(3) = NULL,
    @I_IMPORTE DECIMAL(19,4) = NULL,
    @I_IMPORTE_CONTADO DECIMAL(19,4) = NULL,
    @I_AFECTA_EFECTIVO BIT = NULL,
    @I_REQUIERE_REFERENCIA BIT = NULL,
    @I_CODIGO_ESTADO NVARCHAR(30) = NULL,
    @I_OBSERVACION NVARCHAR(500) = NULL,
    @I_JSON_MEDIOS NVARCHAR(MAX) = NULL,
    @I_JSON_APLICACIONES NVARCHAR(MAX) = NULL,
    @I_ROW_VERSION VARBINARY(8) = NULL,
    @O_ID BIGINT OUTPUT,
    @O_FILAS_AFECTADAS INT OUTPUT,
    @O_CODIGO_ERROR BIGINT OUTPUT,
    @O_MENSAJE NVARCHAR(4000) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @O_ID = NULL;
    SET @O_FILAS_AFECTADAS = 0;
    SET @O_CODIGO_ERROR = NULL;
    SET @O_MENSAJE = NULL;

    BEGIN TRY
        IF @I_ACCION NOT IN (N'MEDIO_GUARDAR', N'CAJA_GUARDAR', N'SESION_ABRIR',
                             N'SESION_CERRAR', N'PAGO_REGISTRAR', N'PAGO_REVERSAR')
            THROW 52001, N'La acción financiera no es válida.', 1;

        BEGIN TRANSACTION;

        -- INICIO: Alta o edición del catálogo de medios de pago.
        IF @I_ACCION = N'MEDIO_GUARDAR'
        BEGIN
            SET @I_CODIGO = UPPER(TRIM(@I_CODIGO));
            SET @I_NOMBRE = TRIM(@I_NOMBRE);
            IF NULLIF(@I_CODIGO, N'') IS NULL OR NULLIF(@I_NOMBRE, N'') IS NULL OR
               @I_TIPO NOT IN (N'EFECTIVO', N'TRANSFERENCIA', N'TARJETA', N'CHEQUE', N'OTRO') OR
               @I_CODIGO_ESTADO NOT IN (N'ACTIVO', N'INACTIVO')
                THROW 52001, N'Completá código, nombre, tipo y estado válidos para el medio de pago.', 1;

            IF @I_ID IS NULL
            BEGIN
                INSERT INTO [FINANZAS].[MEDIOS_PAGO]
                    ([ID_EMPRESA], [CODIGO], [NOMBRE], [TIPO], [AFECTA_EFECTIVO],
                     [REQUIERE_REFERENCIA], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
                VALUES
                    (@I_ID_EMPRESA, @I_CODIGO, @I_NOMBRE, @I_TIPO, COALESCE(@I_AFECTA_EFECTIVO, 0),
                     COALESCE(@I_REQUIERE_REFERENCIA, 0), @I_CODIGO_ESTADO, @S_ID_USUARIO);
                SET @O_ID = CONVERT(BIGINT, SCOPE_IDENTITY());
            END
            ELSE
            BEGIN
                UPDATE [FINANZAS].[MEDIOS_PAGO]
                SET [NOMBRE] = @I_NOMBRE,
                    [TIPO] = @I_TIPO,
                    [AFECTA_EFECTIVO] = COALESCE(@I_AFECTA_EFECTIVO, 0),
                    [REQUIERE_REFERENCIA] = COALESCE(@I_REQUIERE_REFERENCIA, 0),
                    [CODIGO_ESTADO] = @I_CODIGO_ESTADO,
                    [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(),
                    [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO
                WHERE [ID_MEDIO_PAGO] = @I_ID AND [ID_EMPRESA] = @I_ID_EMPRESA
                  AND [CODIGO] = @I_CODIGO AND [ROW_VERSION] = @I_ROW_VERSION;
                IF @@ROWCOUNT = 0 THROW 52002, N'El medio de pago fue modificado o no existe.', 1;
                SET @O_ID = @I_ID;
            END;
            SET @O_FILAS_AFECTADAS = 1;
        END;
        -- FIN: Alta o edición del catálogo de medios de pago.

        -- INICIO: Alta o edición de una caja física vinculada a una sucursal.
        IF @I_ACCION = N'CAJA_GUARDAR'
        BEGIN
            SET @I_CODIGO = UPPER(TRIM(@I_CODIGO));
            SET @I_NOMBRE = TRIM(@I_NOMBRE);
            SET @I_MONEDA = UPPER(TRIM(@I_MONEDA));
            IF NULLIF(@I_CODIGO, N'') IS NULL OR NULLIF(@I_NOMBRE, N'') IS NULL OR
               LEN(@I_MONEDA) <> 3 OR @I_CODIGO_ESTADO NOT IN (N'ACTIVO', N'INACTIVO') OR
               NOT EXISTS (SELECT 1 FROM [CONFIGURACION].[SUCURSALES]
                           WHERE [ID_SUCURSAL] = @I_ID_SUCURSAL AND [ID_EMPRESA] = @I_ID_EMPRESA
                             AND [CODIGO_ESTADO] = N'ACTIVO')
                THROW 52001, N'Completá sucursal, código, nombre, moneda y estado válidos para la caja.', 1;

            IF @I_ID IS NULL
            BEGIN
                INSERT INTO [FINANZAS].[CAJAS]
                    ([ID_EMPRESA], [ID_SUCURSAL], [CODIGO], [NOMBRE], [MONEDA], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
                VALUES
                    (@I_ID_EMPRESA, @I_ID_SUCURSAL, @I_CODIGO, @I_NOMBRE, @I_MONEDA, @I_CODIGO_ESTADO, @S_ID_USUARIO);
                SET @O_ID = CONVERT(BIGINT, SCOPE_IDENTITY());
            END
            ELSE
            BEGIN
                IF @I_CODIGO_ESTADO = N'INACTIVO' AND EXISTS
                    (SELECT 1 FROM [FINANZAS].[SESIONES_CAJA] WHERE [ID_CAJA] = @I_ID AND [CODIGO_ESTADO] = N'ABIERTA')
                    THROW 52002, N'No se puede inactivar una caja con una sesión abierta.', 1;

                UPDATE [FINANZAS].[CAJAS]
                SET [ID_SUCURSAL] = @I_ID_SUCURSAL, [NOMBRE] = @I_NOMBRE, [MONEDA] = @I_MONEDA,
                    [CODIGO_ESTADO] = @I_CODIGO_ESTADO, [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(),
                    [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO
                WHERE [ID_CAJA] = @I_ID AND [ID_EMPRESA] = @I_ID_EMPRESA
                  AND [CODIGO] = @I_CODIGO AND [ROW_VERSION] = @I_ROW_VERSION;
                IF @@ROWCOUNT = 0 THROW 52002, N'La caja fue modificada o no existe.', 1;
                SET @O_ID = @I_ID;
            END;
            SET @O_FILAS_AFECTADAS = 1;
        END;
        -- FIN: Alta o edición de una caja física vinculada a una sucursal.

        -- INICIO: Apertura de caja exclusiva para el punto físico y el usuario.
        IF @I_ACCION = N'SESION_ABRIR'
        BEGIN
            IF COALESCE(@I_IMPORTE, -1) < 0 OR NOT EXISTS
                (SELECT 1 FROM [FINANZAS].[CAJAS]
                 WHERE [ID_CAJA] = @I_ID AND [ID_EMPRESA] = @I_ID_EMPRESA AND [CODIGO_ESTADO] = N'ACTIVO')
                THROW 52001, N'Seleccioná una caja activa e informá un importe inicial válido.', 1;
            IF EXISTS (SELECT 1 FROM [FINANZAS].[SESIONES_CAJA]
                       WHERE [ID_EMPRESA] = @I_ID_EMPRESA AND
                             ([ID_CAJA] = @I_ID OR [ID_USUARIO_APERTURA] = @S_ID_USUARIO) AND [CODIGO_ESTADO] = N'ABIERTA')
                THROW 52002, N'La caja o el usuario ya posee una sesión abierta.', 1;

            INSERT INTO [FINANZAS].[SESIONES_CAJA]
                ([ID_EMPRESA], [ID_CAJA], [ID_USUARIO_APERTURA], [FECHA_APERTURA_UTC],
                 [IMPORTE_APERTURA], [OBSERVACION_APERTURA], [ID_USUARIO_ALTA])
            VALUES
                (@I_ID_EMPRESA, @I_ID, @S_ID_USUARIO, COALESCE(@I_FECHA_UTC, SYSUTCDATETIME()),
                 @I_IMPORTE, NULLIF(TRIM(@I_OBSERVACION), N''), @S_ID_USUARIO);
            SET @O_ID = CONVERT(BIGINT, SCOPE_IDENTITY());
            SET @O_FILAS_AFECTADAS = 1;
        END;
        -- FIN: Apertura de caja exclusiva para el punto físico y el usuario.

        -- INICIO: Cierre con cálculo autoritativo del efectivo esperado.
        IF @I_ACCION = N'SESION_CERRAR'
        BEGIN
            IF COALESCE(@I_IMPORTE_CONTADO, -1) < 0
                THROW 52001, N'El importe contado debe ser cero o mayor.', 1;

            DECLARE @V_ESPERADO DECIMAL(19,4);
            SELECT @V_ESPERADO = [SES].[IMPORTE_APERTURA] + COALESCE(SUM(
                CASE WHEN [MED].[AFECTA_EFECTIVO] = 1 AND [PAG].[CODIGO_ESTADO] = N'CONFIRMADO'
                     THEN [PM].[IMPORTE] ELSE 0 END), 0)
            FROM [FINANZAS].[SESIONES_CAJA] AS [SES]
            LEFT JOIN [FINANZAS].[PAGOS] AS [PAG] ON [PAG].[ID_SESION_CAJA] = [SES].[ID_SESION_CAJA]
            LEFT JOIN [FINANZAS].[PAGOS_MEDIOS] AS [PM] ON [PM].[ID_PAGO] = [PAG].[ID_PAGO]
            LEFT JOIN [FINANZAS].[MEDIOS_PAGO] AS [MED] ON [MED].[ID_MEDIO_PAGO] = [PM].[ID_MEDIO_PAGO]
            WHERE [SES].[ID_SESION_CAJA] = @I_ID AND [SES].[ID_EMPRESA] = @I_ID_EMPRESA
              AND [SES].[ID_USUARIO_APERTURA] = @S_ID_USUARIO AND [SES].[CODIGO_ESTADO] = N'ABIERTA'
              AND [SES].[ROW_VERSION] = @I_ROW_VERSION
            GROUP BY [SES].[IMPORTE_APERTURA];
            IF @V_ESPERADO IS NULL THROW 52002, N'La sesión fue modificada, no está abierta o pertenece a otro usuario.', 1;

            UPDATE [FINANZAS].[SESIONES_CAJA]
            SET [FECHA_CIERRE_UTC] = COALESCE(@I_FECHA_UTC, SYSUTCDATETIME()),
                [ID_USUARIO_CIERRE] = @S_ID_USUARIO,
                [IMPORTE_ESPERADO] = @V_ESPERADO,
                [IMPORTE_CONTADO] = @I_IMPORTE_CONTADO,
                [DIFERENCIA] = @I_IMPORTE_CONTADO - @V_ESPERADO,
                [OBSERVACION_CIERRE] = NULLIF(TRIM(@I_OBSERVACION), N''),
                [CODIGO_ESTADO] = N'CERRADA',
                [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(),
                [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO
            WHERE [ID_SESION_CAJA] = @I_ID;
            SET @O_ID = @I_ID;
            SET @O_FILAS_AFECTADAS = 1;
        END;
        -- FIN: Cierre con cálculo autoritativo del efectivo esperado.

        -- INICIO: Cobro confirmado con medios y aplicaciones en una sola transacción.
        IF @I_ACCION = N'PAGO_REGISTRAR'
        BEGIN
            IF COALESCE(@I_IMPORTE, 0) <= 0 OR @I_FECHA_UTC IS NULL OR LEN(UPPER(TRIM(@I_MONEDA))) <> 3 OR
               NOT EXISTS (SELECT 1 FROM [COMERCIAL].[CLIENTES]
                           WHERE [ID_CLIENTE] = @I_ID_CLIENTE AND [ID_EMPRESA] = @I_ID_EMPRESA
                             AND [CODIGO_ESTADO] = N'ACTIVO')
                THROW 52001, N'Informá cliente activo, fecha, moneda y un total mayor que cero.', 1;

            DECLARE @V_MEDIOS TABLE
            (
                [ID_MEDIO_PAGO] BIGINT NOT NULL,
                [IMPORTE] DECIMAL(19,4) NOT NULL,
                [REFERENCIA] NVARCHAR(100) NULL
            );
            INSERT INTO @V_MEDIOS ([ID_MEDIO_PAGO], [IMPORTE], [REFERENCIA])
            SELECT [ID_MEDIO_PAGO], [IMPORTE], NULLIF(TRIM([REFERENCIA]), N'')
            FROM OPENJSON(COALESCE(@I_JSON_MEDIOS, N'[]')) WITH
            (
                [ID_MEDIO_PAGO] BIGINT '$.PaymentMethodId',
                [IMPORTE] DECIMAL(19,4) '$.Amount',
                [REFERENCIA] NVARCHAR(100) '$.Reference'
            );

            IF NOT EXISTS (SELECT 1 FROM @V_MEDIOS) OR EXISTS (SELECT 1 FROM @V_MEDIOS WHERE [IMPORTE] <= 0) OR
               ABS((SELECT SUM([IMPORTE]) FROM @V_MEDIOS) - @I_IMPORTE) > 0.0001
                THROW 52001, N'Los medios de pago deben sumar exactamente el total del cobro.', 1;
            IF EXISTS
            (
                SELECT 1 FROM @V_MEDIOS AS [ENT]
                LEFT JOIN [FINANZAS].[MEDIOS_PAGO] AS [MED] ON [MED].[ID_MEDIO_PAGO] = [ENT].[ID_MEDIO_PAGO]
                WHERE [MED].[ID_MEDIO_PAGO] IS NULL OR [MED].[ID_EMPRESA] <> @I_ID_EMPRESA OR
                      [MED].[CODIGO_ESTADO] <> N'ACTIVO' OR
                      ([MED].[REQUIERE_REFERENCIA] = 1 AND [ENT].[REFERENCIA] IS NULL)
            )
                THROW 52001, N'Existe un medio inactivo, ajeno o sin la referencia obligatoria.', 1;

            IF EXISTS (SELECT 1 FROM @V_MEDIOS AS [ENT] INNER JOIN [FINANZAS].[MEDIOS_PAGO] AS [MED]
                       ON [MED].[ID_MEDIO_PAGO] = [ENT].[ID_MEDIO_PAGO] WHERE [MED].[AFECTA_EFECTIVO] = 1)
               AND NOT EXISTS
               (SELECT 1 FROM [FINANZAS].[SESIONES_CAJA]
                WHERE [ID_SESION_CAJA] = @I_ID_SESION_CAJA AND [ID_EMPRESA] = @I_ID_EMPRESA
                  AND [ID_USUARIO_APERTURA] = @S_ID_USUARIO AND [CODIGO_ESTADO] = N'ABIERTA')
                THROW 52002, N'Para cobrar en efectivo debés seleccionar tu sesión de caja abierta.', 1;

            DECLARE @V_APLICACIONES TABLE ([ID_VENTA] BIGINT NOT NULL, [IMPORTE] DECIMAL(19,4) NOT NULL);
            INSERT INTO @V_APLICACIONES ([ID_VENTA], [IMPORTE])
            SELECT [ID_VENTA], [IMPORTE]
            FROM OPENJSON(COALESCE(@I_JSON_APLICACIONES, N'[]')) WITH
            (
                [ID_VENTA] BIGINT '$.SaleId',
                [IMPORTE] DECIMAL(19,4) '$.Amount'
            );
            IF EXISTS (SELECT 1 FROM @V_APLICACIONES WHERE [IMPORTE] <= 0) OR
               COALESCE((SELECT SUM([IMPORTE]) FROM @V_APLICACIONES), 0) > @I_IMPORTE OR
               EXISTS (SELECT [ID_VENTA] FROM @V_APLICACIONES GROUP BY [ID_VENTA] HAVING COUNT(*) > 1)
                THROW 52001, N'Las aplicaciones deben ser únicas, positivas y no superar el cobro.', 1;
            IF EXISTS
            (
                SELECT 1
                FROM @V_APLICACIONES AS [ENT]
                LEFT JOIN [COMERCIAL].[VENTAS] AS [VEN] ON [VEN].[ID_VENTA] = [ENT].[ID_VENTA]
                OUTER APPLY
                (
                    SELECT COALESCE(SUM([APL].[IMPORTE]), 0) AS [APLICADO]
                    FROM [FINANZAS].[APLICACIONES_PAGO_VENTA] AS [APL]
                    INNER JOIN [FINANZAS].[PAGOS] AS [PAG] ON [PAG].[ID_PAGO] = [APL].[ID_PAGO]
                    WHERE [APL].[ID_VENTA] = [ENT].[ID_VENTA] AND [APL].[CODIGO_ESTADO] = N'APLICADA'
                      AND [PAG].[CODIGO_ESTADO] = N'CONFIRMADO'
                ) AS [SAL]
                WHERE [VEN].[ID_VENTA] IS NULL OR [VEN].[ID_EMPRESA] <> @I_ID_EMPRESA OR
                      [VEN].[ID_CLIENTE] <> @I_ID_CLIENTE OR
                      [VEN].[MONEDA] <> UPPER(@I_MONEDA) OR [VEN].[CODIGO_ESTADO] <> N'CONFIRMADA' OR
                      [ENT].[IMPORTE] > [VEN].[TOTAL] - [SAL].[APLICADO]
            )
                THROW 52003, N'Una aplicación supera el saldo o no corresponde al cliente y moneda del cobro.', 1;

            DECLARE @V_NUMERO BIGINT = NEXT VALUE FOR [FINANZAS].[SEQ_PAGO_CODIGO];
            DECLARE @V_CODIGO NVARCHAR(30) = N'REC-' + CASE WHEN @V_NUMERO < 1000000
                THEN RIGHT(REPLICATE(N'0', 6) + CONVERT(NVARCHAR(20), @V_NUMERO), 6)
                ELSE CONVERT(NVARCHAR(20), @V_NUMERO) END;

            INSERT INTO [FINANZAS].[PAGOS]
                ([ID_EMPRESA], [ID_CLIENTE], [ID_SESION_CAJA], [CODIGO], [FECHA_PAGO_UTC],
                 [MONEDA], [TOTAL], [OBSERVACION], [ID_USUARIO_ALTA])
            VALUES
                (@I_ID_EMPRESA, @I_ID_CLIENTE, @I_ID_SESION_CAJA, @V_CODIGO, @I_FECHA_UTC,
                 UPPER(@I_MONEDA), @I_IMPORTE, NULLIF(TRIM(@I_OBSERVACION), N''), @S_ID_USUARIO);
            SET @O_ID = CONVERT(BIGINT, SCOPE_IDENTITY());

            INSERT INTO [FINANZAS].[PAGOS_MEDIOS]
                ([ID_PAGO], [ID_MEDIO_PAGO], [IMPORTE], [REFERENCIA], [ID_USUARIO_ALTA])
            SELECT @O_ID, [ID_MEDIO_PAGO], [IMPORTE], [REFERENCIA], @S_ID_USUARIO FROM @V_MEDIOS;

            INSERT INTO [FINANZAS].[APLICACIONES_PAGO_VENTA]
                ([ID_PAGO], [ID_VENTA], [IMPORTE], [ID_USUARIO_ALTA])
            SELECT @O_ID, [ID_VENTA], [IMPORTE], @S_ID_USUARIO FROM @V_APLICACIONES;

            INSERT INTO [FINANZAS].[MOVIMIENTOS_CUENTA]
                ([ID_EMPRESA], [ID_CLIENTE], [ID_PAGO], [TIPO_MOVIMIENTO], [ORIGEN],
                 [FECHA_MOVIMIENTO_UTC], [MONEDA], [IMPORTE], [DESCRIPCION], [ID_USUARIO_ALTA])
            VALUES
                (@I_ID_EMPRESA, @I_ID_CLIENTE, @O_ID, N'CREDITO', N'PAGO', @I_FECHA_UTC,
                 UPPER(@I_MONEDA), @I_IMPORTE, N'Cobro interno ' + @V_CODIGO, @S_ID_USUARIO);
            SET @O_FILAS_AFECTADAS = 2 + (SELECT COUNT(*) FROM @V_MEDIOS) + (SELECT COUNT(*) FROM @V_APLICACIONES);
        END;
        -- FIN: Cobro confirmado con medios y aplicaciones en una sola transacción.

        -- INICIO: Reversión compensatoria sin pérdida del movimiento original.
        IF @I_ACCION = N'PAGO_REVERSAR'
        BEGIN
            IF NULLIF(TRIM(@I_OBSERVACION), N'') IS NULL
                THROW 52001, N'El motivo de reversión es obligatorio.', 1;
            DECLARE @V_ID_CLIENTE BIGINT, @V_TOTAL DECIMAL(19,4), @V_MONEDA CHAR(3), @V_CODIGO_PAGO NVARCHAR(30);
            SELECT @V_ID_CLIENTE = [ID_CLIENTE], @V_TOTAL = [TOTAL], @V_MONEDA = [MONEDA], @V_CODIGO_PAGO = [CODIGO]
            FROM [FINANZAS].[PAGOS] WITH (UPDLOCK, HOLDLOCK)
            WHERE [ID_PAGO] = @I_ID AND [ID_EMPRESA] = @I_ID_EMPRESA AND [CODIGO_ESTADO] = N'CONFIRMADO'
              AND [ROW_VERSION] = @I_ROW_VERSION
              AND ([ID_SESION_CAJA] IS NULL OR EXISTS
                  (SELECT 1 FROM [FINANZAS].[SESIONES_CAJA] WHERE [ID_SESION_CAJA] = [PAGOS].[ID_SESION_CAJA]
                                                               AND [CODIGO_ESTADO] = N'ABIERTA'));
            IF @V_ID_CLIENTE IS NULL
                THROW 52004, N'El cobro fue modificado, ya fue revertido o pertenece a una caja cerrada.', 1;

            UPDATE [FINANZAS].[PAGOS]
            SET [MOTIVO_REVERSION] = TRIM(@I_OBSERVACION), [FECHA_REVERSION_UTC] = SYSUTCDATETIME(),
                [ID_USUARIO_REVERSION] = @S_ID_USUARIO, [CODIGO_ESTADO] = N'REVERTIDO',
                [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(), [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO
            WHERE [ID_PAGO] = @I_ID;
            UPDATE [FINANZAS].[APLICACIONES_PAGO_VENTA]
            SET [CODIGO_ESTADO] = N'REVERTIDA', [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(),
                [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO
            WHERE [ID_PAGO] = @I_ID AND [CODIGO_ESTADO] = N'APLICADA';
            INSERT INTO [FINANZAS].[MOVIMIENTOS_CUENTA]
                ([ID_EMPRESA], [ID_CLIENTE], [ID_PAGO], [TIPO_MOVIMIENTO], [ORIGEN],
                 [FECHA_MOVIMIENTO_UTC], [MONEDA], [IMPORTE], [DESCRIPCION], [ID_USUARIO_ALTA])
            VALUES
                (@I_ID_EMPRESA, @V_ID_CLIENTE, @I_ID, N'DEBITO', N'REVERSO_PAGO', SYSUTCDATETIME(),
                 @V_MONEDA, @V_TOTAL, N'Reversión del cobro ' + @V_CODIGO_PAGO + N': ' + TRIM(@I_OBSERVACION), @S_ID_USUARIO);
            SET @O_ID = @I_ID;
            SET @O_FILAS_AFECTADAS = 2 + @@ROWCOUNT;
        END;
        -- FIN: Reversión compensatoria sin pérdida del movimiento original.

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        SET @O_CODIGO_ERROR = CASE WHEN ERROR_NUMBER() BETWEEN 52001 AND 52099
                                   THEN 80000 + ERROR_NUMBER() - 52000 ELSE 80001 END;
        SET @O_MENSAJE = ERROR_MESSAGE();
    END CATCH;
END;
