/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: SP_ACTIVO_CLIENTE_CUSTODIA
Archivo: SP_ACTIVO_CLIENTE_CUSTODIA.sql | Procedimiento: INVENTARIO.SP_ACTIVO_CLIENTE_CUSTODIA | Tipo: COMMAND
Versión: 1.0.1 | Fecha: 2026-09-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Confirma el ingreso o la entrega de un activo propiedad de un cliente conservando un evento inmutable.
Parámetros de entrada: @I_ACCION, @I_ID_ACTIVO, @I_ID_DEPOSITO, @I_ID_EMPRESA, @I_OBSERVACION, @I_ROW_VERSION, @S_ID_SESION, @S_ID_USUARIO.
Parámetros de salida: @O_CODIGO_ERROR, @O_FILAS_AFECTADAS, @O_ID_ACTIVO, @O_MENSAJE.
Retorno: Informa el resultado mediante los parámetros de salida declarados.
Tablas utilizadas: INVENTARIO.ACTIVOS - SELECT/UPDATE; INVENTARIO.ACTIVOS_EVENTOS - INSERT; INVENTARIO.DEPOSITOS - SELECT.
Transacción: Abre una transacción explícita para agrupar sus escrituras.
Auditoría: No registra auditoría explícita dentro del procedimiento.
Acciones admitidas mediante @I_ACCION: INGRESAR pasa de EN_CLIENTE a DISPONIBLE en un depósito; ENTREGAR pasa de DISPONIBLE a EN_CLIENTE.
Permiso API: INVENTARIO.GESTIONAR para todas las acciones.
Historial: 1.0.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Ciclo de custodia con concurrencia optimista.
Historial: 1.0.1 | 2026-09-20 | FABRICA | Agustin Omar Cauzi | Documentación técnica completa del procedimiento.
===============================================================================
*/
CREATE OR ALTER PROCEDURE [INVENTARIO].[SP_ACTIVO_CLIENTE_CUSTODIA]
    @I_ID_EMPRESA BIGINT,
    @I_ID_ACTIVO BIGINT,
    @I_ACCION NVARCHAR(30),
    @I_ID_DEPOSITO BIGINT = NULL,
    @I_OBSERVACION NVARCHAR(500),
    @I_ROW_VERSION BINARY(8),
    @S_ID_SESION BIGINT,
    @S_ID_USUARIO BIGINT,
    @O_ID_ACTIVO BIGINT OUTPUT,
    @O_CODIGO_ERROR BIGINT OUTPUT,
    @O_MENSAJE NVARCHAR(4000) OUTPUT,
    @O_FILAS_AFECTADAS INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @O_ID_ACTIVO = NULL;
    SET @O_CODIGO_ERROR = NULL;
    SET @O_MENSAJE = NULL;
    SET @O_FILAS_AFECTADAS = 0;

    DECLARE @V_ACCION NVARCHAR(30) = UPPER(LTRIM(RTRIM(@I_ACCION)));
    DECLARE @V_CONDICION NVARCHAR(30);
    DECLARE @V_ESTADO_ANTES NVARCHAR(30);
    DECLARE @V_ESTADO_DESPUES NVARCHAR(30);

    IF @V_ACCION NOT IN (N'INGRESAR', N'ENTREGAR')
       OR NULLIF(LTRIM(RTRIM(@I_OBSERVACION)), N'') IS NULL
       OR (@V_ACCION = N'INGRESAR' AND @I_ID_DEPOSITO IS NULL)
       OR (@V_ACCION = N'ENTREGAR' AND @I_ID_DEPOSITO IS NOT NULL)
    BEGIN
        SET @O_CODIGO_ERROR = 30010;
        SET @O_MENSAJE = N'Informá una acción, ubicación y observación compatibles.';
        RETURN;
    END;

    IF @V_ACCION = N'INGRESAR'
       AND NOT EXISTS
       (
           SELECT 1
           FROM [INVENTARIO].[DEPOSITOS]
           WHERE [ID_DEPOSITO] = @I_ID_DEPOSITO
             AND [ID_EMPRESA] = @I_ID_EMPRESA
             AND [CODIGO_ESTADO] = N'ACTIVO'
       )
    BEGIN
        SET @O_CODIGO_ERROR = 30010;
        SET @O_MENSAJE = N'El depósito no existe, está inactivo o pertenece a otra empresa.';
        RETURN;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT
            @V_ESTADO_ANTES = [CODIGO_ESTADO],
            @V_CONDICION = [CONDICION_ACTUAL]
        FROM [INVENTARIO].[ACTIVOS] WITH (UPDLOCK, HOLDLOCK)
        WHERE [ID_ACTIVO] = @I_ID_ACTIVO
          AND [ID_EMPRESA] = @I_ID_EMPRESA
          AND [ID_CLIENTE_PROPIETARIO] IS NOT NULL
          AND [ROW_VERSION] = @I_ROW_VERSION;

        IF @V_ESTADO_ANTES IS NULL
        BEGIN
            SET @O_CODIGO_ERROR = 30003;
            SET @O_MENSAJE = N'El activo cambió, no existe o no pertenece a un cliente de la empresa.';
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        SET @V_ESTADO_DESPUES = CASE WHEN @V_ACCION = N'INGRESAR' THEN N'DISPONIBLE' ELSE N'EN_CLIENTE' END;

        IF (@V_ACCION = N'INGRESAR' AND @V_ESTADO_ANTES <> N'EN_CLIENTE')
           OR (@V_ACCION = N'ENTREGAR' AND @V_ESTADO_ANTES <> N'DISPONIBLE')
        BEGIN
            SET @O_CODIGO_ERROR = 30010;
            SET @O_MENSAJE = N'El estado actual del activo no permite confirmar esa acción de custodia.';
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        UPDATE [INVENTARIO].[ACTIVOS]
        SET [ID_DEPOSITO] = CASE WHEN @V_ACCION = N'INGRESAR' THEN @I_ID_DEPOSITO ELSE NULL END,
            [ID_UBICACION] = NULL,
            [CODIGO_ESTADO] = @V_ESTADO_DESPUES,
            [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(),
            [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO
        WHERE [ID_ACTIVO] = @I_ID_ACTIVO;

        INSERT INTO [INVENTARIO].[ACTIVOS_EVENTOS]
        (
            [ID_EMPRESA], [ID_ACTIVO], [TIPO_EVENTO], [FECHA_EVENTO_UTC],
            [CODIGO_ESTADO_ANTES], [CODIGO_ESTADO_DESPUES], [CONDICION_ANTES], [CONDICION_DESPUES],
            [OBSERVACION], [ID_CORRELACION], [CODIGO_ESTADO], [ID_USUARIO_ALTA]
        )
        VALUES
        (
            @I_ID_EMPRESA, @I_ID_ACTIVO,
            CASE WHEN @V_ACCION = N'INGRESAR' THEN N'CLIENTE_INGRESO' ELSE N'CLIENTE_ENTREGA' END,
            SYSUTCDATETIME(), @V_ESTADO_ANTES, @V_ESTADO_DESPUES, @V_CONDICION, @V_CONDICION,
            LTRIM(RTRIM(@I_OBSERVACION)), NEWID(), N'CONFIRMADO', @S_ID_USUARIO
        );

        SET @O_ID_ACTIVO = @I_ID_ACTIVO;
        SET @O_FILAS_AFECTADAS = 1;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
        BEGIN
            ROLLBACK TRANSACTION;
        END;

        SET @O_MENSAJE = ERROR_MESSAGE();
        THROW;
    END CATCH;
END;
