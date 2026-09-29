/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: SP_INCIDENTE_CREATE
Archivo: SP_INCIDENTE_CREATE.sql | Procedimiento: INVENTARIO.SP_INCIDENTE_CREATE | Tipo: COMMAND
Versión: 1.0.1 | Fecha: 2026-09-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Registra pérdida/rotura y ajusta stock o contenido, bloqueando el activo afectado.
Parámetros de entrada: @I_ACCION_TOMADA, @I_CANTIDAD_ANTES, @I_CANTIDAD_DESPUES, @I_CANTIDAD_PERDIDA, @I_CAUSA, @I_ES_ESTIMADA, @I_EVIDENCIA_REFERENCIA, @I_FECHA_INCIDENTE_UTC, @I_ID_ACTIVO, @I_ID_DEPOSITO, @I_ID_EMPRESA, @I_ID_LOTE, @I_ID_PRODUCTO, @I_METODO_MEDICION, @I_TIPO_INCIDENTE, @S_ID_SESION, @S_ID_USUARIO.
Parámetros de salida: @O_CODIGO_ERROR, @O_FILAS_AFECTADAS, @O_ID_INCIDENTE, @O_MENSAJE.
Retorno: Informa el resultado mediante los parámetros de salida declarados.
Tablas utilizadas: INVENTARIO.ACTIVOS - SELECT/UPDATE; INVENTARIO.ACTIVOS_CONTENIDOS - UPDATE; INVENTARIO.ACTIVOS_EVENTOS - INSERT; INVENTARIO.INCIDENTES - INSERT; INVENTARIO.LOTES_EXISTENCIAS - UPDATE.
Transacción: Abre una transacción explícita para agrupar sus escrituras.
Auditoría: No registra auditoría explícita dentro del procedimiento.
Historial: 1.0.0 | 2026-08-24 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.0.1 | 2026-09-20 | FABRICA | Agustin Omar Cauzi | Documentación técnica completa del procedimiento.
===============================================================================
*/
CREATE OR ALTER PROCEDURE [INVENTARIO].[SP_INCIDENTE_CREATE]
    @I_ID_EMPRESA BIGINT,
    @I_ID_PRODUCTO BIGINT,
    @I_ID_DEPOSITO BIGINT,
    @I_ID_ACTIVO BIGINT=NULL,
    @I_ID_LOTE BIGINT=NULL,
    @I_TIPO_INCIDENTE NVARCHAR (30),
    @I_FECHA_INCIDENTE_UTC DATETIME2 (3),
    @I_CANTIDAD_ANTES DECIMAL (19, 4)=NULL,
    @I_CANTIDAD_PERDIDA DECIMAL (19, 4),
    @I_CANTIDAD_DESPUES DECIMAL (19, 4)=NULL,
    @I_ES_ESTIMADA BIT,
    @I_METODO_MEDICION NVARCHAR (30)=NULL,
    @I_CAUSA NVARCHAR (1000),
    @I_ACCION_TOMADA NVARCHAR (1000)=NULL,
    @I_EVIDENCIA_REFERENCIA NVARCHAR (500)=NULL,
    @S_ID_SESION BIGINT,
    @S_ID_USUARIO BIGINT,
    @O_ID_INCIDENTE BIGINT OUTPUT,
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
    IF @I_CANTIDAD_PERDIDA < 0
       OR (@I_CANTIDAD_ANTES IS NOT NULL
           AND @I_CANTIDAD_DESPUES IS NOT NULL
           AND @I_CANTIDAD_ANTES <> @I_CANTIDAD_DESPUES + @I_CANTIDAD_PERDIDA)
        THROW 50010, 'Las cantidades del incidente no son coherentes.', 1;
    BEGIN TRANSACTION;
    DECLARE @V_ESTADO AS NVARCHAR (30), @V_CONDICION AS NVARCHAR (30), @V_MOV AS BIGINT = NULL, @V_ANTES AS DECIMAL (19, 4) = @I_CANTIDAD_ANTES;
    IF @I_ID_ACTIVO IS NOT NULL
        SELECT @V_ESTADO = [CODIGO_ESTADO],
               @V_CONDICION = [CONDICION_ACTUAL]
        FROM   [INVENTARIO].[ACTIVOS] WITH (UPDLOCK, HOLDLOCK)
        WHERE  [ID_ACTIVO] = @I_ID_ACTIVO
               AND [ID_EMPRESA] = @I_ID_EMPRESA;
    IF @I_ID_ACTIVO IS NOT NULL
       AND @V_ESTADO IS NULL
        BEGIN
            SET @O_CODIGO_ERROR = 30007;
            SET @O_MENSAJE = N'El activo no pertenece a la empresa.';
            ROLLBACK;
            RETURN;
        END
    IF @I_CANTIDAD_PERDIDA > 0
        BEGIN
            DECLARE @V_JSON AS NVARCHAR (MAX) = (SELECT @I_ID_PRODUCTO AS [ProductId],
                                                        @I_ID_DEPOSITO AS [OriginWarehouseId],
                                                        @I_CANTIDAD_PERDIDA AS [Quantity]
                                                 FOR    JSON PATH);
            DECLARE @V_E AS BIGINT, @V_M AS NVARCHAR (4000), @V_F AS INT;
            EXECUTE [INVENTARIO].[SP_MOVIMIENTO_CREATE] @I_ID_EMPRESA, N'AJUSTE_SALIDA', @I_FECHA_INCIDENTE_UTC, @I_CAUSA, @V_JSON, NULL, @S_ID_SESION, @S_ID_USUARIO, @V_MOV OUTPUT, @V_E OUTPUT, @V_M OUTPUT, @V_F OUTPUT;
            IF @V_E IS NOT NULL
                BEGIN
                    SET @O_CODIGO_ERROR = 30007;
                    SET @O_MENSAJE = @V_M;
                    ROLLBACK;
                    RETURN;
                END
            IF @I_ID_LOTE IS NOT NULL
                BEGIN
                    UPDATE [INVENTARIO].[LOTES_EXISTENCIAS]
                    SET    [CANTIDAD]                = [CANTIDAD] - @I_CANTIDAD_PERDIDA,
                           [FECHA_MODIFICACION_UTC]  = SYSUTCDATETIME(),
                           [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO
                    WHERE  [ID_EMPRESA] = @I_ID_EMPRESA
                           AND [ID_LOTE] = @I_ID_LOTE
                           AND [ID_DEPOSITO] = @I_ID_DEPOSITO
                           AND [CANTIDAD] >= @I_CANTIDAD_PERDIDA;
                    IF @@ROWCOUNT <> 1
                        BEGIN
                            SET @O_CODIGO_ERROR = 30007;
                            SET @O_MENSAJE = N'El lote no posee saldo suficiente para registrar la pérdida.';
                            ROLLBACK;
                            RETURN;
                        END
                END
            IF @I_ID_ACTIVO IS NOT NULL
                BEGIN
                    UPDATE [INVENTARIO].[ACTIVOS_CONTENIDOS]
                    SET    @V_ANTES                  = [CANTIDAD_ACTUAL],
                           [CANTIDAD_ACTUAL]         = [CANTIDAD_ACTUAL] - @I_CANTIDAD_PERDIDA,
                           [FECHA_MODIFICACION_UTC]  = SYSUTCDATETIME(),
                           [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO
                    WHERE  [ID_ACTIVO] = @I_ID_ACTIVO
                           AND [ID_PRODUCTO_CONTENIDO] = @I_ID_PRODUCTO
                           AND (@I_ID_LOTE IS NULL
                                OR [ID_LOTE] = @I_ID_LOTE)
                           AND [CANTIDAD_ACTUAL] >= @I_CANTIDAD_PERDIDA
                           AND [CODIGO_ESTADO] = N'ACTIVO';
                    IF @@ROWCOUNT <> 1
                        BEGIN
                            SET @O_CODIGO_ERROR = 30007;
                            SET @O_MENSAJE = N'El activo no posee contenido suficiente para registrar la pérdida.';
                            ROLLBACK;
                            RETURN;
                        END
                END
        END
    DECLARE @V_CODIGO AS NVARCHAR (30) = N'INC-' + RIGHT(REPLICATE(N'0', 6) + CONVERT (NVARCHAR (20),  NEXT VALUE FOR [INVENTARIO].[SEQ_INCIDENTE_CODIGO]), 6);
    INSERT  [INVENTARIO].[INCIDENTES] ([ID_EMPRESA], [ID_PRODUCTO], [ID_DEPOSITO], [ID_ACTIVO], [ID_LOTE], [ID_MOVIMIENTO], [CODIGO], [TIPO_INCIDENTE], [FECHA_INCIDENTE_UTC], [CANTIDAD_ANTES], [CANTIDAD_PERDIDA], [CANTIDAD_DESPUES], [ES_ESTIMADA], [METODO_MEDICION], [CAUSA], [ACCION_TOMADA], [EVIDENCIA_REFERENCIA], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
    VALUES                           (@I_ID_EMPRESA, @I_ID_PRODUCTO, @I_ID_DEPOSITO, @I_ID_ACTIVO, @I_ID_LOTE, @V_MOV, @V_CODIGO, @I_TIPO_INCIDENTE, @I_FECHA_INCIDENTE_UTC, COALESCE (@I_CANTIDAD_ANTES, @V_ANTES), @I_CANTIDAD_PERDIDA, COALESCE (@I_CANTIDAD_DESPUES, @V_ANTES - @I_CANTIDAD_PERDIDA), @I_ES_ESTIMADA, @I_METODO_MEDICION, @I_CAUSA, @I_ACCION_TOMADA, @I_EVIDENCIA_REFERENCIA, N'CONFIRMADO', @S_ID_USUARIO);
    SET @O_ID_INCIDENTE = SCOPE_IDENTITY();
    IF @I_ID_ACTIVO IS NOT NULL
        BEGIN
            UPDATE [INVENTARIO].[ACTIVOS]
            SET    [CODIGO_ESTADO]           = N'BLOQUEADO',
                   [CONDICION_ACTUAL]        = N'EN_REVISION',
                   [FECHA_MODIFICACION_UTC]  = SYSUTCDATETIME(),
                   [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO
            WHERE  [ID_ACTIVO] = @I_ID_ACTIVO;
            INSERT  [INVENTARIO].[ACTIVOS_EVENTOS] ([ID_EMPRESA], [ID_ACTIVO], [TIPO_EVENTO], [FECHA_EVENTO_UTC], [CODIGO_ESTADO_ANTES], [CODIGO_ESTADO_DESPUES], [CONDICION_ANTES], [CONDICION_DESPUES], [CANTIDAD_ANTES], [CANTIDAD_DESPUES], [ID_INCIDENTE], [OBSERVACION], [ID_CORRELACION], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
            VALUES                                (@I_ID_EMPRESA, @I_ID_ACTIVO, N'INCIDENTE', @I_FECHA_INCIDENTE_UTC, @V_ESTADO, N'BLOQUEADO', @V_CONDICION, N'EN_REVISION', COALESCE (@I_CANTIDAD_ANTES, @V_ANTES), COALESCE (@I_CANTIDAD_DESPUES, @V_ANTES - @I_CANTIDAD_PERDIDA), @O_ID_INCIDENTE, @I_CAUSA, NEWID(), N'CONFIRMADO', @S_ID_USUARIO);
        END
    SET @O_FILAS_AFECTADAS = 1;
    COMMIT TRANSACTION;
END
