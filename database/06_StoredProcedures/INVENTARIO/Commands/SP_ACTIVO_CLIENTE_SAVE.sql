/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: SP_ACTIVO_CLIENTE_SAVE
Archivo: SP_ACTIVO_CLIENTE_SAVE.sql | Procedimiento: INVENTARIO.SP_ACTIVO_CLIENTE_SAVE | Tipo: COMMAND
Versión: 1.0.1 | Fecha: 2026-09-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Registra un activo propiedad de un cliente y su ubicación inicial sin alterar existencias comerciales.
Parámetros de entrada: @I_CAPACIDAD, @I_CONDICION, @I_EN_CUSTODIA, @I_ID_CLIENTE, @I_ID_DEPOSITO, @I_ID_EMPRESA, @I_ID_PRODUCTO, @I_NUMERO_SERIE, @I_OBSERVACION, @I_TIPO_ACTIVO, @I_UNIDAD_CAPACIDAD, @S_ID_SESION, @S_ID_USUARIO.
Parámetros de salida: @O_CODIGO_ERROR, @O_FILAS_AFECTADAS, @O_ID_ACTIVO, @O_MENSAJE.
Retorno: Informa el resultado mediante los parámetros de salida declarados.
Tablas utilizadas: COMERCIAL.CLIENTES - SELECT; INVENTARIO.ACTIVOS - INSERT/SELECT; INVENTARIO.ACTIVOS_EVENTOS - INSERT; INVENTARIO.DEPOSITOS - SELECT; INVENTARIO.PRODUCTOS - SELECT.
Transacción: Abre una transacción explícita para agrupar sus escrituras.
Auditoría: No registra auditoría explícita dentro del procedimiento.
Acciones de entrada: @I_EN_CUSTODIA = 0 conserva el activo en poder del cliente; @I_EN_CUSTODIA = 1 registra su ingreso al depósito informado.
Historial: 1.0.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Alta de activos de clientes con evento inmutable.
Historial: 1.0.1 | 2026-09-20 | FABRICA | Agustin Omar Cauzi | Documentación técnica completa del procedimiento.
===============================================================================
*/
CREATE OR ALTER PROCEDURE [INVENTARIO].[SP_ACTIVO_CLIENTE_SAVE]
    @I_ID_EMPRESA BIGINT,
    @I_ID_CLIENTE BIGINT,
    @I_ID_PRODUCTO BIGINT,
    @I_ID_DEPOSITO BIGINT = NULL,
    @I_NUMERO_SERIE NVARCHAR(100) = NULL,
    @I_TIPO_ACTIVO NVARCHAR(30),
    @I_CAPACIDAD DECIMAL(19,4) = NULL,
    @I_UNIDAD_CAPACIDAD NVARCHAR(20) = NULL,
    @I_CONDICION NVARCHAR(30),
    @I_EN_CUSTODIA BIT,
    @I_OBSERVACION NVARCHAR(500),
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

    DECLARE @V_CODIGO NVARCHAR(30);
    DECLARE @V_CODIGO_CLIENTE NVARCHAR(30);
    DECLARE @V_NOMBRE_CLIENTE NVARCHAR(200);
    DECLARE @V_NUMERO_SERIE NVARCHAR(100) = NULLIF(LTRIM(RTRIM(@I_NUMERO_SERIE)), N'');
    DECLARE @V_SECUENCIA BIGINT;
    DECLARE @V_ESTADO NVARCHAR(30) = CASE WHEN @I_EN_CUSTODIA = 1 THEN N'DISPONIBLE' ELSE N'EN_CLIENTE' END;

    IF @I_ID_CLIENTE <= 0
       OR @I_ID_PRODUCTO <= 0
       OR NULLIF(LTRIM(RTRIM(@I_TIPO_ACTIVO)), N'') IS NULL
       OR NULLIF(LTRIM(RTRIM(@I_CONDICION)), N'') IS NULL
       OR NULLIF(LTRIM(RTRIM(@I_OBSERVACION)), N'') IS NULL
       OR @I_CAPACIDAD < 0
       OR (@I_EN_CUSTODIA = 1 AND @I_ID_DEPOSITO IS NULL)
       OR (@I_EN_CUSTODIA = 0 AND @I_ID_DEPOSITO IS NOT NULL)
    BEGIN
        SET @O_CODIGO_ERROR = 30010;
        SET @O_MENSAJE = N'Informá cliente, producto, condición, ubicación inicial y observación válidos.';
        RETURN;
    END;

    SELECT
        @V_CODIGO_CLIENTE = [CODIGO],
        @V_NOMBRE_CLIENTE = LEFT(CONCAT([NOMBRE_RAZON_SOCIAL], COALESCE(N' ' + NULLIF([APELLIDO], N''), N'')), 200)
    FROM [COMERCIAL].[CLIENTES]
    WHERE [ID_CLIENTE] = @I_ID_CLIENTE
      AND [ID_EMPRESA] = @I_ID_EMPRESA
      AND [CODIGO_ESTADO] = N'ACTIVO';

    IF @V_CODIGO_CLIENTE IS NULL
       OR NOT EXISTS
       (
           SELECT 1
           FROM [INVENTARIO].[PRODUCTOS]
           WHERE [ID_PRODUCTO] = @I_ID_PRODUCTO
             AND [ID_EMPRESA] = @I_ID_EMPRESA
             AND [CODIGO_ESTADO] = N'ACTIVO'
             AND ([TIPO_TRAZABILIDAD] = N'SERIE' OR [ES_REUTILIZABLE] = 1)
       )
       OR (@I_ID_DEPOSITO IS NOT NULL AND NOT EXISTS
       (
           SELECT 1
           FROM [INVENTARIO].[DEPOSITOS]
           WHERE [ID_DEPOSITO] = @I_ID_DEPOSITO
             AND [ID_EMPRESA] = @I_ID_EMPRESA
             AND [CODIGO_ESTADO] = N'ACTIVO'
       ))
    BEGIN
        SET @O_CODIGO_ERROR = 30010;
        SET @O_MENSAJE = N'El cliente, producto trazable o depósito no existe, está inactivo o pertenece a otra empresa.';
        RETURN;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        SET @V_SECUENCIA = NEXT VALUE FOR [INVENTARIO].[SEQ_ACTIVO_CODIGO];
        SET @V_CODIGO = N'ACT-C-' + RIGHT(REPLICATE(N'0', 6) + CONVERT(NVARCHAR(20), @V_SECUENCIA), 6);
        SET @V_NUMERO_SERIE = COALESCE(@V_NUMERO_SERIE, N'SIN-SERIE-' + @V_CODIGO);

        IF EXISTS
        (
            SELECT 1
            FROM [INVENTARIO].[ACTIVOS] WITH (UPDLOCK, HOLDLOCK)
            WHERE [ID_EMPRESA] = @I_ID_EMPRESA
              AND [NUMERO_SERIE] = @V_NUMERO_SERIE
        )
        BEGIN
            SET @O_CODIGO_ERROR = 30010;
            SET @O_MENSAJE = N'Ya existe un activo con ese número de serie en la empresa.';
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        INSERT INTO [INVENTARIO].[ACTIVOS]
        (
            [ID_EMPRESA], [ID_PRODUCTO], [ID_CLIENTE_PROPIETARIO], [ID_DEPOSITO], [CODIGO],
            [NUMERO_SERIE], [TIPO_ACTIVO], [CAPACIDAD], [UNIDAD_CAPACIDAD], [PROPIETARIO_CODIGO],
            [PROPIETARIO_NOMBRE], [CONDICION_ACTUAL], [CODIGO_ESTADO], [ID_USUARIO_ALTA]
        )
        VALUES
        (
            @I_ID_EMPRESA, @I_ID_PRODUCTO, @I_ID_CLIENTE, @I_ID_DEPOSITO, @V_CODIGO,
            @V_NUMERO_SERIE, UPPER(LTRIM(RTRIM(@I_TIPO_ACTIVO))), @I_CAPACIDAD,
            NULLIF(LTRIM(RTRIM(@I_UNIDAD_CAPACIDAD)), N''), @V_CODIGO_CLIENTE,
            @V_NOMBRE_CLIENTE, UPPER(LTRIM(RTRIM(@I_CONDICION))), @V_ESTADO, @S_ID_USUARIO
        );

        SET @O_ID_ACTIVO = SCOPE_IDENTITY();

        INSERT INTO [INVENTARIO].[ACTIVOS_EVENTOS]
        (
            [ID_EMPRESA], [ID_ACTIVO], [TIPO_EVENTO], [FECHA_EVENTO_UTC],
            [CODIGO_ESTADO_ANTES], [CODIGO_ESTADO_DESPUES], [CONDICION_ANTES], [CONDICION_DESPUES],
            [OBSERVACION], [ID_CORRELACION], [CODIGO_ESTADO], [ID_USUARIO_ALTA]
        )
        VALUES
        (
            @I_ID_EMPRESA, @O_ID_ACTIVO,
            CASE WHEN @I_EN_CUSTODIA = 1 THEN N'CLIENTE_INGRESO' ELSE N'CLIENTE_ALTA' END,
            SYSUTCDATETIME(), NULL, @V_ESTADO, NULL, UPPER(LTRIM(RTRIM(@I_CONDICION))),
            LTRIM(RTRIM(@I_OBSERVACION)), NEWID(), N'CONFIRMADO', @S_ID_USUARIO
        );

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
