/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: Datos de demostración de Logística Fase 8
Archivo: 06_LOGISTICA_EJEMPLO.sql | Versión: 1.1.0 | Fecha: 2026-08-28 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Crea domicilio, retiro con custodia, urgencia y hoja planificada para pruebas manuales.
Historial: 1.0.0 | 2026-08-26 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-28 | FABRICA | Agustin Omar Cauzi | Activos reales, depósito receptor y devolución prevista.
===============================================================================
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;

DECLARE @V_ID_EMPRESA BIGINT =
(
    SELECT TOP (1) [ID_EMPRESA]
    FROM [CONFIGURACION].[EMPRESAS]
    WHERE [CODIGO] = N'OXITIGRE'
);
DECLARE @V_ID_CLIENTE BIGINT =
(
    SELECT TOP (1) [ID_CLIENTE]
    FROM [COMERCIAL].[CLIENTES]
    WHERE [ID_EMPRESA] = @V_ID_EMPRESA AND [CODIGO_ESTADO] = N'ACTIVO'
    ORDER BY [ID_CLIENTE]
);
DECLARE @V_ID_DEPOSITO BIGINT =
(
    SELECT TOP (1) [ID_DEPOSITO]
    FROM [INVENTARIO].[DEPOSITOS]
    WHERE [ID_EMPRESA] = @V_ID_EMPRESA
      AND [CODIGO] = N'CENTRAL'
      AND [CODIGO_ESTADO] = N'ACTIVO'
);
DECLARE @V_ID_PRODUCTO_CILINDRO BIGINT =
(
    SELECT TOP (1) [ID_PRODUCTO]
    FROM [INVENTARIO].[PRODUCTOS]
    WHERE [ID_EMPRESA] = @V_ID_EMPRESA
      AND [CODIGO_BARRAS] = N'DEV-CILINDRO-10'
);
DECLARE @V_ID_ACTIVO_CLIENTE BIGINT;
DECLARE @V_ID_ACTIVO_PRESTAMO BIGINT;
DECLARE @V_ID_DIRECCION BIGINT;
DECLARE @V_ID_SOLICITUD_RETIRO BIGINT;
DECLARE @V_ID_SOLICITUD_URGENTE BIGINT;
DECLARE @V_ID_HOJA_RUTA BIGINT;
DECLARE @V_CODIGO_ERROR BIGINT;
DECLARE @V_MENSAJE NVARCHAR(4000);
DECLARE @V_FILAS_AFECTADAS INT;
DECLARE @V_SOLICITUDES_JSON NVARCHAR(MAX);
DECLARE @V_ACTIVOS_JSON NVARCHAR(MAX);

IF @V_ID_CLIENTE IS NOT NULL AND
   @V_ID_DEPOSITO IS NOT NULL AND
   @V_ID_PRODUCTO_CILINDRO IS NOT NULL AND
   NOT EXISTS
   (
       SELECT 1
       FROM [LOGISTICA].[CLIENTES_DIRECCIONES]
       WHERE [ID_EMPRESA] = @V_ID_EMPRESA AND [NOMBRE] = N'Planta demo'
   )
BEGIN
    -- INICIO: Activo ajeno identificado y activo propio disponibles para probar custodia real.
    IF NOT EXISTS
    (
        SELECT 1
        FROM [INVENTARIO].[ACTIVOS]
        WHERE [ID_EMPRESA] = @V_ID_EMPRESA
          AND [NUMERO_SERIE] = N'CLI-DEMO-01'
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
               N'ACT-CLI-DEMO-01', N'CLI-DEMO-01', N'ENVASE', 10, N'kg',
               [CODIGO], CONCAT_WS(N' ', [NOMBRE_RAZON_SOCIAL], [APELLIDO]),
               N'OPERATIVO', N'EN_CLIENTE', 1
        FROM [COMERCIAL].[CLIENTES]
        WHERE [ID_CLIENTE] = @V_ID_CLIENTE;
    END;

    SET @V_ID_ACTIVO_CLIENTE =
    (
        SELECT [ID_ACTIVO]
        FROM [INVENTARIO].[ACTIVOS]
        WHERE [ID_EMPRESA] = @V_ID_EMPRESA AND [NUMERO_SERIE] = N'CLI-DEMO-01'
    );
    SET @V_ID_ACTIVO_PRESTAMO =
    (
        SELECT [ID_ACTIVO]
        FROM [INVENTARIO].[ACTIVOS]
        WHERE [ID_EMPRESA] = @V_ID_EMPRESA
          AND [NUMERO_SERIE] = N'CIL-000101'
          AND [CODIGO_ESTADO] = N'DISPONIBLE'
    );

    IF @V_ID_ACTIVO_CLIENTE IS NULL OR @V_ID_ACTIVO_PRESTAMO IS NULL
        THROW 51000, N'Los datos demostrativos requieren ambos activos operativos.', 1;
    -- FIN: Activo ajeno identificado y activo propio disponibles para probar custodia real.

    -- INICIO: Domicilio con ventana de atención y restricciones visibles para el chofer.
    EXEC [LOGISTICA].[SP_LOGISTICA_COMMAND]
        @I_ACCION = N'DIRECCION_GUARDAR',
        @I_ID_EMPRESA = @V_ID_EMPRESA,
        @S_ID_SESION = 1,
        @S_ID_USUARIO = 1,
        @I_ID_CLIENTE = @V_ID_CLIENTE,
        @I_NOMBRE = N'Planta demo',
        @I_DOMICILIO = N'Av. Industrial 1250',
        @I_LOCALIDAD = N'Tigre',
        @I_PROVINCIA = N'Buenos Aires',
        @I_CODIGO_POSTAL = N'B1648',
        @I_CONTACTO = N'Encargado de planta',
        @I_TELEFONO = N'+54 11 5555-0180',
        @I_CORREO = N'logistica.demo@oxitigre.local',
        @I_HORA_DESDE = '12:00',
        @I_HORA_HASTA = '14:00',
        @I_INSTRUCCIONES = N'Anunciarse en portería. El cliente recibe solamente entre 12:00 y 14:00.',
        @O_ID = @V_ID_DIRECCION OUTPUT,
        @O_FILAS_AFECTADAS = @V_FILAS_AFECTADAS OUTPUT,
        @O_CODIGO_ERROR = @V_CODIGO_ERROR OUTPUT,
        @O_MENSAJE = @V_MENSAJE OUTPUT;
    -- FIN: Domicilio con ventana de atención y restricciones visibles para el chofer.

    -- INICIO: Retiro del tubo del cliente con préstamo temporal de un tubo OxiTigre.
    SET @V_ACTIVOS_JSON = CONCAT
    (
        N'[{"AssetId":', @V_ID_ACTIVO_CLIENTE,
        N',"Owner":"CLIENTE","Role":"RETIRO_CLIENTE","SerialNumber":"CLI-DEMO-01",',
        N'"Product":"Oxígeno","ContentQuantity":1.5,"Unit":"kg","Condition":"OPERATIVO",',
        N'"Observation":"Propiedad del cliente"},',
        N'{"AssetId":', @V_ID_ACTIVO_PRESTAMO,
        N',"Owner":"OXITIGRE","Role":"PRESTAMO_TEMPORAL","SerialNumber":"CIL-000101",',
        N'"Product":"Oxígeno","ContentQuantity":10,"Unit":"kg","Condition":"LLENO",',
        N'"Observation":"Devolver al entregar el tubo cliente","ExpectedReturnDate":"2026-09-15"}]'
    );

    EXEC [LOGISTICA].[SP_LOGISTICA_COMMAND]
        @I_ACCION = N'SOLICITUD_CREAR',
        @I_ID_EMPRESA = @V_ID_EMPRESA,
        @S_ID_SESION = 1,
        @S_ID_USUARIO = 1,
        @I_ID_CLIENTE = @V_ID_CLIENTE,
        @I_ID_DIRECCION = @V_ID_DIRECCION,
        @I_ID_DEPOSITO = @V_ID_DEPOSITO,
        @I_TIPO = N'INTERCAMBIO_TEMPORAL',
        @I_PRIORIDAD = N'ALTA',
        @I_FECHA = '2026-08-28',
        @I_HORA_DESDE = '12:00',
        @I_HORA_HASTA = '14:00',
        @I_INSTRUCCIONES = N'Retirar el tubo CLI-DEMO-01 y dejar el tubo temporal OXI-DEMO-01.',
        @I_OBSERVACION = N'El tubo del cliente debe volver recargado; nunca vender ni consumir como propio.',
        @I_JSON = @V_ACTIVOS_JSON,
        @O_ID = @V_ID_SOLICITUD_RETIRO OUTPUT,
        @O_FILAS_AFECTADAS = @V_FILAS_AFECTADAS OUTPUT,
        @O_CODIGO_ERROR = @V_CODIGO_ERROR OUTPUT,
        @O_MENSAJE = @V_MENSAJE OUTPUT;
    -- FIN: Retiro del tubo del cliente con préstamo temporal de un tubo OxiTigre.

    -- INICIO: Solicitud inmediata disponible para crear una ruta urgente de una sola parada.
    EXEC [LOGISTICA].[SP_LOGISTICA_COMMAND]
        @I_ACCION = N'SOLICITUD_CREAR',
        @I_ID_EMPRESA = @V_ID_EMPRESA,
        @S_ID_SESION = 1,
        @S_ID_USUARIO = 1,
        @I_ID_CLIENTE = @V_ID_CLIENTE,
        @I_ID_DIRECCION = @V_ID_DIRECCION,
        @I_TIPO = N'URGENCIA',
        @I_PRIORIDAD = N'EMERGENCIA',
        @I_FECHA = '2026-08-26',
        @I_INSTRUCCIONES = N'Pedido telefónico inmediato. Llamar al contacto antes de salir.',
        @I_OBSERVACION = N'Debe quedar disponible para probar una hoja urgente.',
        @O_ID = @V_ID_SOLICITUD_URGENTE OUTPUT,
        @O_FILAS_AFECTADAS = @V_FILAS_AFECTADAS OUTPUT,
        @O_CODIGO_ERROR = @V_CODIGO_ERROR OUTPUT,
        @O_MENSAJE = @V_MENSAJE OUTPUT;
    -- FIN: Solicitud inmediata disponible para crear una ruta urgente de una sola parada.

    -- INICIO: Hoja ofrecida con el retiro; queda disponible para que un transportista la tome.
    SET @V_SOLICITUDES_JSON = CONCAT(
        N'[{"Order":1,"RequestId":',
        @V_ID_SOLICITUD_RETIRO,
        N'}]'
    );

    EXEC [LOGISTICA].[SP_LOGISTICA_COMMAND]
        @I_ACCION = N'RUTA_CREAR',
        @I_ID_EMPRESA = @V_ID_EMPRESA,
        @S_ID_SESION = 1,
        @S_ID_USUARIO = 1,
        @I_FECHA = '2026-08-28',
        @I_TIPO = N'NORMAL',
        @I_TIPO_ASIGNACION = N'OFERTA',
        @I_OBSERVACION = N'Hoja de prueba disponible para transportistas.',
        @I_JSON = @V_SOLICITUDES_JSON,
        @O_ID = @V_ID_HOJA_RUTA OUTPUT,
        @O_FILAS_AFECTADAS = @V_FILAS_AFECTADAS OUTPUT,
        @O_CODIGO_ERROR = @V_CODIGO_ERROR OUTPUT,
        @O_MENSAJE = @V_MENSAJE OUTPUT;
    -- FIN: Hoja ofrecida con el retiro; queda disponible para que un transportista la tome.
END;
