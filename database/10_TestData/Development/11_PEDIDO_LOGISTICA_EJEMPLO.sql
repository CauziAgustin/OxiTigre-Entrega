/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: Datos de desarrollo para Pedidos y Logística
Archivo: 11_PEDIDO_LOGISTICA_EJEMPLO.sql | Versión: 1.0.0 | Fecha: 2026-08-28 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Crea un pedido confirmado de recarga con retiro y devolución del tubo del cliente.
Historial: 1.0.0 | 2026-08-28 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;

DECLARE @V_ID_EMPRESA BIGINT =
(
    SELECT [ID_EMPRESA]
    FROM [CONFIGURACION].[EMPRESAS]
    WHERE [CODIGO] = N'OXITIGRE'
);
DECLARE @V_ID_USUARIO BIGINT =
(
    SELECT TOP (1) [ID_USUARIO]
    FROM [SEGURIDAD].[USUARIOS]
    WHERE [ID_EMPRESA] = @V_ID_EMPRESA
      AND [CODIGO_ESTADO] = N'ACTIVO'
    ORDER BY [ID_USUARIO]
);
DECLARE @V_ID_CLIENTE BIGINT =
(
    SELECT TOP (1) [ID_CLIENTE]
    FROM [COMERCIAL].[CLIENTES]
    WHERE [ID_EMPRESA] = @V_ID_EMPRESA
      AND [CODIGO_ESTADO] = N'ACTIVO'
    ORDER BY [ID_CLIENTE]
);
DECLARE @V_ID_LISTA BIGINT =
(
    SELECT [ID_LISTA_PRECIO]
    FROM [COMERCIAL].[LISTAS_PRECIOS]
    WHERE [ID_EMPRESA] = @V_ID_EMPRESA
      AND [CODIGO] = N'GENERAL'
      AND [CODIGO_ESTADO] = N'ACTIVO'
);
DECLARE @V_ID_SERVICIO BIGINT =
(
    SELECT [ID_PRODUCTO]
    FROM [INVENTARIO].[PRODUCTOS]
    WHERE [ID_EMPRESA] = @V_ID_EMPRESA
      AND [CODIGO] = N'SRV-RECARGA'
      AND [CODIGO_ESTADO] = N'ACTIVO'
);
DECLARE @V_ID_PRODUCTO_CILINDRO BIGINT =
(
    SELECT [ID_PRODUCTO]
    FROM [INVENTARIO].[PRODUCTOS]
    WHERE [ID_EMPRESA] = @V_ID_EMPRESA
      AND [CODIGO_BARRAS] = N'DEV-CILINDRO-10'
);
DECLARE @V_PRECIO DECIMAL(19,4) =
(
    SELECT [PRECIO_UNITARIO]
    FROM [COMERCIAL].[LISTAS_PRECIOS_PRODUCTOS]
    WHERE [ID_LISTA_PRECIO] = @V_ID_LISTA
      AND [ID_PRODUCTO] = @V_ID_SERVICIO
      AND [CODIGO_ESTADO] = N'ACTIVO'
);

IF @V_ID_EMPRESA IS NULL OR @V_ID_USUARIO IS NULL OR @V_ID_CLIENTE IS NULL
   OR @V_ID_LISTA IS NULL OR @V_ID_SERVICIO IS NULL OR @V_ID_PRODUCTO_CILINDRO IS NULL
   OR @V_PRECIO IS NULL
BEGIN
    THROW 51000, N'Faltan catálogos previos para crear el pedido logístico demostrativo.', 1;
END;

-- INICIO: El tubo permanece propiedad del cliente mientras está fuera de OxiTigre.
IF NOT EXISTS
(
    SELECT 1
    FROM [INVENTARIO].[ACTIVOS]
    WHERE [ID_EMPRESA] = @V_ID_EMPRESA
      AND [NUMERO_SERIE] = N'CLI-PED-DEMO-01'
)
BEGIN
    INSERT INTO [INVENTARIO].[ACTIVOS]
    (
        [ID_EMPRESA], [ID_PRODUCTO], [ID_CLIENTE_PROPIETARIO], [CODIGO],
        [NUMERO_SERIE], [TIPO_ACTIVO], [CAPACIDAD], [UNIDAD_CAPACIDAD],
        [PROPIETARIO_CODIGO], [PROPIETARIO_NOMBRE], [CONDICION_ACTUAL],
        [CODIGO_ESTADO], [ID_USUARIO_ALTA]
    )
    SELECT @V_ID_EMPRESA, @V_ID_PRODUCTO_CILINDRO, @V_ID_CLIENTE,
           N'ACT-CLI-PED-DEMO', N'CLI-PED-DEMO-01', N'ENVASE', 10, N'kg',
           [CODIGO], CONCAT_WS(N' ', [NOMBRE_RAZON_SOCIAL], [APELLIDO]),
           N'OPERATIVO', N'EN_CLIENTE', @V_ID_USUARIO
    FROM [COMERCIAL].[CLIENTES]
    WHERE [ID_CLIENTE] = @V_ID_CLIENTE;
END;
-- FIN: El tubo permanece propiedad del cliente mientras está fuera de OxiTigre.

DECLARE @V_ID_ACTIVO BIGINT =
(
    SELECT [ID_ACTIVO]
    FROM [INVENTARIO].[ACTIVOS]
    WHERE [ID_EMPRESA] = @V_ID_EMPRESA
      AND [NUMERO_SERIE] = N'CLI-PED-DEMO-01'
);
DECLARE @V_ID_PEDIDO BIGINT =
(
    SELECT [ID_PEDIDO]
    FROM [COMERCIAL].[PEDIDOS]
    WHERE [ID_EMPRESA] = @V_ID_EMPRESA
      AND [OBSERVACION] = N'DEMO_LOGISTICA: retiro, recarga y devolución del tubo del cliente.'
);

-- INICIO: Pedido cobrable que alimenta automáticamente la planificación logística.
IF @V_ID_PEDIDO IS NULL
BEGIN
    DECLARE @V_DETALLES_JSON NVARCHAR(MAX) = CONCAT
    (
        N'[{"ProductId":', @V_ID_SERVICIO,
        N',"WarehouseId":null,"Quantity":1,"UnitPrice":',
        CONVERT(NVARCHAR(40), @V_PRECIO),
        N',"DiscountRate":0,"TaxRate":0.21}]'
    );
    DECLARE @V_ACTIVOS_JSON NVARCHAR(MAX) = CONCAT
    (
        N'[{"AssetId":', @V_ID_ACTIVO,
        N',"ProductLineId":', @V_ID_SERVICIO,
        N',"LinkType":"CLIENTE_SERVICIO","InboundMode":"RETIRO_OXITIGRE",',
        N'"ReturnMode":"ENTREGA_OXITIGRE","ExpectedReturnDate":null,',
        N'"Observation":"Retirar, recargar y devolver el mismo tubo del cliente."}]'
    );
    DECLARE @V_CODIGO_ERROR BIGINT;
    DECLARE @V_MENSAJE NVARCHAR(4000);
    DECLARE @V_FILAS_AFECTADAS INT;

    EXEC [COMERCIAL].[SP_PEDIDO_SAVE]
        @I_ID_EMPRESA = @V_ID_EMPRESA,
        @I_ID_CLIENTE = @V_ID_CLIENTE,
        @I_ID_LISTA_PRECIO = @V_ID_LISTA,
        @I_FECHA_PEDIDO_UTC = '2026-08-28T12:00:00',
        @I_MONEDA = N'ARS',
        @I_OBSERVACION = N'DEMO_LOGISTICA: retiro, recarga y devolución del tubo del cliente.',
        @I_DETALLES_JSON = @V_DETALLES_JSON,
        @I_ACTIVOS_JSON = @V_ACTIVOS_JSON,
        @S_ID_SESION = 1,
        @S_ID_USUARIO = @V_ID_USUARIO,
        @O_ID_PEDIDO = @V_ID_PEDIDO OUTPUT,
        @O_CODIGO_ERROR = @V_CODIGO_ERROR OUTPUT,
        @O_MENSAJE = @V_MENSAJE OUTPUT,
        @O_FILAS_AFECTADAS = @V_FILAS_AFECTADAS OUTPUT;

    IF @V_CODIGO_ERROR IS NOT NULL
        THROW 51000, @V_MENSAJE, 1;

    DECLARE @V_ROW_VERSION BINARY(8) =
    (
        SELECT [ROW_VERSION]
        FROM [COMERCIAL].[PEDIDOS]
        WHERE [ID_PEDIDO] = @V_ID_PEDIDO
    );

    EXEC [COMERCIAL].[SP_PEDIDO_CONFIRM]
        @I_ID_PEDIDO = @V_ID_PEDIDO,
        @I_ID_EMPRESA = @V_ID_EMPRESA,
        @I_ROW_VERSION = @V_ROW_VERSION,
        @S_ID_SESION = 1,
        @S_ID_USUARIO = @V_ID_USUARIO,
        @O_CODIGO_ERROR = @V_CODIGO_ERROR OUTPUT,
        @O_MENSAJE = @V_MENSAJE OUTPUT,
        @O_FILAS_AFECTADAS = @V_FILAS_AFECTADAS OUTPUT;

    IF @V_CODIGO_ERROR IS NOT NULL
        THROW 51000, @V_MENSAJE, 1;
END;
-- FIN: Pedido cobrable que alimenta automáticamente la planificación logística.

SELECT [PED].[CODIGO], [PED].[CODIGO_ESTADO], [ACT].[NUMERO_SERIE],
       [VINC].[MODALIDAD_INGRESO], [VINC].[MODALIDAD_RETORNO]
FROM [COMERCIAL].[PEDIDOS] AS [PED]
INNER JOIN [COMERCIAL].[PEDIDOS_ACTIVOS] AS [VINC] ON [VINC].[ID_PEDIDO] = [PED].[ID_PEDIDO]
INNER JOIN [INVENTARIO].[ACTIVOS] AS [ACT] ON [ACT].[ID_ACTIVO] = [VINC].[ID_ACTIVO]
WHERE [PED].[ID_PEDIDO] = @V_ID_PEDIDO;
