/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: SMOKE_COMERCIAL
Archivo: SMOKE_COMERCIAL.sql | Versión: 1.2.0 | Fecha: 2026-08-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Comprueba alta, edición, teléfonos y estado del módulo Comercial sin conservar datos.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Catálogos y borrador recuperable.
Historial: 1.2.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Valida código catalogado para cliente inexistente.
===============================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @V_ID_EMPRESA BIGINT = (SELECT [ID_EMPRESA] FROM [CONFIGURACION].[EMPRESAS] WHERE [CODIGO] = N'OXITIGRE');
    DECLARE @V_ID_USUARIO BIGINT = (SELECT [ID_USUARIO] FROM [SEGURIDAD].[USUARIOS] WHERE [ID_EMPRESA] = @V_ID_EMPRESA AND [NOMBRE_USUARIO] = N'AOCAUZI');
    DECLARE @V_ID_CLIENTE BIGINT;
    DECLARE @V_CODIGO_ERROR BIGINT;
    DECLARE @V_MENSAJE NVARCHAR(4000);
    DECLARE @V_FILAS INT;

    -- INICIO: Consulta individual sin resultado con código y descripción catalogados.
    EXEC [COMERCIAL].[SP_CLIENTE_GET]
        @I_ID_EMPRESA = @V_ID_EMPRESA, @I_ID_CLIENTE = -1,
        @O_CODIGO_ERROR = @V_CODIGO_ERROR OUTPUT, @O_MENSAJE = @V_MENSAJE OUTPUT;
    IF @V_CODIGO_ERROR <> 40002 OR NULLIF(@V_MENSAJE, N'') IS NULL
        THROW 51006, 'La consulta individual no informó el cliente inexistente.', 1;
    -- FIN: Consulta individual sin resultado con código y descripción catalogados.

    EXEC [COMERCIAL].[SP_CLIENTE_BORRADOR_SAVE]
        @I_ID_EMPRESA = @V_ID_EMPRESA,
        @I_CONTENIDO_JSON = N'{"PersonType":"F","NameOrBusinessName":"Prueba parcial","Surname":null,"DocumentType":null,"DocumentNumber":null,"Email":null,"Observation":null,"Phones":[]}',
        @S_ID_SESION = 1, @S_ID_USUARIO = @V_ID_USUARIO,
        @O_CODIGO_ERROR = @V_CODIGO_ERROR OUTPUT, @O_MENSAJE = @V_MENSAJE OUTPUT,
        @O_FILAS_AFECTADAS = @V_FILAS OUTPUT;
    IF NOT EXISTS (SELECT 1 FROM [COMERCIAL].[CLIENTES_BORRADORES] WHERE [ID_EMPRESA] = @V_ID_EMPRESA AND [ID_USUARIO] = @V_ID_USUARIO AND [CODIGO_ESTADO] = N'BORRADOR')
        THROW 51004, 'No se guardó la precarga incompleta.', 1;

    EXEC [COMERCIAL].[SP_CLIENTE_CREATE]
        @I_ID_EMPRESA = @V_ID_EMPRESA, @I_TIPO_PERSONA = 'F',
        @I_NOMBRE_RAZON_SOCIAL = N'Prueba', @I_APELLIDO = N'Comercial',
        @I_TIPO_DOCUMENTO = N'DNI', @I_NUMERO_DOCUMENTO = N'99999999',
        @I_EMAIL = N'prueba@example.com', @I_OBSERVACION = N'Smoke test',
        @I_TELEFONOS_JSON = N'[{"TypeCode":"MOVIL","CountryCode":"54","AreaCode":"11","Number":"55550001","Extension":null,"IsPrimary":true,"AllowsWhatsApp":true,"Observation":null},{"TypeCode":"FIJO","CountryCode":"54","AreaCode":"11","Number":"55550002","Extension":null,"IsPrimary":false,"AllowsWhatsApp":false,"Observation":null}]',
        @S_ID_SESION = 1, @S_ID_USUARIO = @V_ID_USUARIO,
        @O_ID_CLIENTE = @V_ID_CLIENTE OUTPUT, @O_CODIGO_ERROR = @V_CODIGO_ERROR OUTPUT,
        @O_MENSAJE = @V_MENSAJE OUTPUT, @O_FILAS_AFECTADAS = @V_FILAS OUTPUT;

    IF @V_ID_CLIENTE IS NULL OR @V_FILAS <> 3
        THROW 51000, 'Falló el alta transaccional del cliente.', 1;
    IF (SELECT COUNT(*) FROM [COMERCIAL].[CLIENTES_TELEFONOS] WHERE [ID_CLIENTE] = @V_ID_CLIENTE AND [CODIGO_ESTADO] = N'ACTIVO') <> 2
        THROW 51001, 'No se crearon los dos teléfonos activos.', 1;
    IF EXISTS (SELECT 1 FROM [COMERCIAL].[CLIENTES_BORRADORES] WHERE [ID_EMPRESA] = @V_ID_EMPRESA AND [ID_USUARIO] = @V_ID_USUARIO AND [CODIGO_ESTADO] = N'BORRADOR')
        THROW 51005, 'El alta no descartó automáticamente el borrador.', 1;

    EXEC [COMERCIAL].[SP_CLIENTE_UPDATE]
        @I_ID_EMPRESA = @V_ID_EMPRESA, @I_ID_CLIENTE = @V_ID_CLIENTE, @I_TIPO_PERSONA = 'J',
        @I_NOMBRE_RAZON_SOCIAL = N'Prueba Comercial SRL', @I_APELLIDO = NULL,
        @I_TIPO_DOCUMENTO = N'CUIT', @I_NUMERO_DOCUMENTO = N'30999999991',
        @I_EMAIL = N'empresa@example.com', @I_OBSERVACION = N'Actualizado',
        @I_TELEFONOS_JSON = N'[{"TypeCode":"LABORAL","CountryCode":"54","AreaCode":"11","Number":"55550003","Extension":"10","IsPrimary":true,"AllowsWhatsApp":false,"Observation":null}]',
        @S_ID_SESION = 1, @S_ID_USUARIO = @V_ID_USUARIO,
        @O_CODIGO_ERROR = @V_CODIGO_ERROR OUTPUT, @O_MENSAJE = @V_MENSAJE OUTPUT,
        @O_FILAS_AFECTADAS = @V_FILAS OUTPUT;

    IF (SELECT COUNT(*) FROM [COMERCIAL].[CLIENTES_TELEFONOS] WHERE [ID_CLIENTE] = @V_ID_CLIENTE AND [CODIGO_ESTADO] = N'ACTIVO') <> 1
        THROW 51002, 'La edición no reemplazó los teléfonos activos.', 1;

    EXEC [COMERCIAL].[SP_CLIENTE_STATUS_CHANGE]
        @I_ID_EMPRESA = @V_ID_EMPRESA, @I_ID_CLIENTE = @V_ID_CLIENTE, @I_CODIGO_ESTADO = N'INACTIVO',
        @S_ID_SESION = 1, @S_ID_USUARIO = @V_ID_USUARIO,
        @O_CODIGO_ERROR = @V_CODIGO_ERROR OUTPUT, @O_MENSAJE = @V_MENSAJE OUTPUT,
        @O_FILAS_AFECTADAS = @V_FILAS OUTPUT;

    IF (SELECT [CODIGO_ESTADO] FROM [COMERCIAL].[CLIENTES] WHERE [ID_CLIENTE] = @V_ID_CLIENTE) <> N'INACTIVO'
        THROW 51003, 'Falló el cambio lógico de estado.', 1;

    ROLLBACK TRANSACTION;
    SELECT N'OK' AS [RESULTADO], N'Catálogos, borrador, alta, edición, teléfonos y estado validados; datos revertidos.' AS [DETALLE];
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
