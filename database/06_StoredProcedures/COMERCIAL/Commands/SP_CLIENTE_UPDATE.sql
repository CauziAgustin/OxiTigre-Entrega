/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: SP_CLIENTE_UPDATE
Archivo: SP_CLIENTE_UPDATE.sql
Procedimiento: COMERCIAL.SP_CLIENTE_UPDATE
Tipo: COMMAND
Versión: 1.1.0
Fecha: 2026-08-20
ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi
Correo: agustincauzi10@hotmail.com
Descripción funcional: Actualiza un cliente y reemplaza su colección de teléfonos activos conservando historial.
Parámetros de entrada: Empresa, cliente, datos generales y teléfonos JSON.
Parámetros de sesión: Sesión y usuario responsables.
Parámetros de salida: Código, mensaje y filas afectadas.
Retorno: No retorna filas.
Tablas utilizadas: COMERCIAL.CLIENTES y CLIENTES_TELEFONOS - UPDATE/INSERT; CONFIGURACION.TIPOS_TELEFONO - READ.
Transacción: Actualización y reemplazo de teléfonos atómicos.
Auditoría: La DAL registra la modificación sin datos personales completos.
Historial de modificaciones:
1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
1.1.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Validación parametrizada de documento y teléfonos.
===============================================================================
*/
CREATE OR ALTER PROCEDURE [COMERCIAL].[SP_CLIENTE_UPDATE]
    @I_ID_EMPRESA BIGINT, @I_ID_CLIENTE BIGINT, @I_TIPO_PERSONA CHAR(1),
    @I_NOMBRE_RAZON_SOCIAL NVARCHAR(200), @I_APELLIDO NVARCHAR(150) = NULL,
    @I_TIPO_DOCUMENTO NVARCHAR(20) = NULL, @I_NUMERO_DOCUMENTO NVARCHAR(30) = NULL,
    @I_EMAIL NVARCHAR(254) = NULL, @I_OBSERVACION NVARCHAR(1000) = NULL,
    @I_TELEFONOS_JSON NVARCHAR(MAX), @S_ID_SESION BIGINT, @S_ID_USUARIO BIGINT,
    @O_CODIGO_ERROR BIGINT OUTPUT, @O_MENSAJE NVARCHAR(4000) OUTPUT, @O_FILAS_AFECTADAS INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @O_CODIGO_ERROR = NULL; SET @O_MENSAJE = NULL; SET @O_FILAS_AFECTADAS = 0;

    IF @I_ID_EMPRESA IS NULL OR @I_ID_CLIENTE IS NULL OR @S_ID_SESION IS NULL OR @S_ID_USUARIO IS NULL
       OR @I_TIPO_PERSONA NOT IN ('F', 'J') OR NULLIF(LTRIM(RTRIM(@I_NOMBRE_RAZON_SOCIAL)), N'') IS NULL
       OR NULLIF(LTRIM(RTRIM(@I_TIPO_DOCUMENTO)), N'') IS NULL OR NULLIF(LTRIM(RTRIM(@I_NUMERO_DOCUMENTO)), N'') IS NULL
       OR ISJSON(@I_TELEFONOS_JSON) <> 1
        THROW 50000, 'Empresa, cliente, sesión, datos generales y teléfonos son obligatorios.', 1;
    IF @I_TIPO_PERSONA = 'F' AND NULLIF(LTRIM(RTRIM(@I_APELLIDO)), N'') IS NULL
        THROW 50001, 'El apellido es obligatorio para una persona física.', 1;
    IF @I_TIPO_PERSONA = 'J' SET @I_APELLIDO = NULL;
    IF NOT EXISTS
    (
        SELECT 1 FROM [CONFIGURACION].[TIPOS_DOCUMENTO]
        WHERE [CODIGO] = @I_TIPO_DOCUMENTO AND [CODIGO_ESTADO] = N'ACTIVO'
          AND ((@I_TIPO_PERSONA = 'F' AND [APLICA_PERSONA_FISICA] = 1)
            OR (@I_TIPO_PERSONA = 'J' AND [APLICA_PERSONA_JURIDICA] = 1))
    ) THROW 50002, 'El tipo de documento no está habilitado para la persona seleccionada.', 1;

    DECLARE @T_TELEFONOS TABLE
    (
        [ORDEN] SMALLINT NOT NULL, [CODIGO_TIPO] NVARCHAR(30) NOT NULL, [CODIGO_PAIS] NVARCHAR(5) NULL,
        [CODIGO_AREA] NVARCHAR(10) NULL, [NUMERO] NVARCHAR(20) NOT NULL, [INTERNO] NVARCHAR(10) NULL,
        [ES_PRINCIPAL] BIT NOT NULL, [PERMITE_WHATSAPP] BIT NOT NULL, [OBSERVACION] NVARCHAR(500) NULL
    );

    INSERT INTO @T_TELEFONOS
    SELECT CONVERT(SMALLINT, [J].[key]), [T].[CODIGO_TIPO], [T].[CODIGO_PAIS], [T].[CODIGO_AREA],
           [T].[NUMERO], [T].[INTERNO], [T].[ES_PRINCIPAL], [T].[PERMITE_WHATSAPP], [T].[OBSERVACION]
    FROM OPENJSON(@I_TELEFONOS_JSON) AS [J]
    CROSS APPLY OPENJSON([J].[value]) WITH
    (
        [CODIGO_TIPO] NVARCHAR(30) '$.TypeCode', [CODIGO_PAIS] NVARCHAR(5) '$.CountryCode',
        [CODIGO_AREA] NVARCHAR(10) '$.AreaCode', [NUMERO] NVARCHAR(20) '$.Number',
        [INTERNO] NVARCHAR(10) '$.Extension', [ES_PRINCIPAL] BIT '$.IsPrimary',
        [PERMITE_WHATSAPP] BIT '$.AllowsWhatsApp', [OBSERVACION] NVARCHAR(500) '$.Observation'
    ) AS [T];

    IF NOT EXISTS (SELECT 1 FROM @T_TELEFONOS)
       OR (SELECT COUNT(*) FROM @T_TELEFONOS WHERE [ES_PRINCIPAL] = 1) <> 1
       OR EXISTS (SELECT 1 FROM @T_TELEFONOS WHERE NULLIF(LTRIM(RTRIM([NUMERO])), N'') IS NULL)
       OR EXISTS (SELECT 1 FROM @T_TELEFONOS WHERE [CODIGO_PAIS] IS NULL OR [CODIGO_PAIS] LIKE N'%[^0-9]%' OR [NUMERO] LIKE N'%[^0-9]%')
       OR EXISTS
       (
           SELECT 1 FROM @T_TELEFONOS AS [TEL]
           WHERE NOT EXISTS (SELECT 1 FROM [CONFIGURACION].[TIPOS_TELEFONO] AS [TIP]
               WHERE [TIP].[CODIGO] = [TEL].[CODIGO_TIPO] AND [TIP].[CODIGO_ESTADO] = N'ACTIVO')
       )
        THROW 50003, 'Debe informar teléfonos válidos y exactamente uno principal.', 1;

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE [COMERCIAL].[CLIENTES]
        SET [TIPO_PERSONA] = @I_TIPO_PERSONA, [NOMBRE_RAZON_SOCIAL] = @I_NOMBRE_RAZON_SOCIAL,
            [APELLIDO] = @I_APELLIDO, [TIPO_DOCUMENTO] = @I_TIPO_DOCUMENTO,
            [NUMERO_DOCUMENTO] = @I_NUMERO_DOCUMENTO, [EMAIL] = @I_EMAIL,
            [OBSERVACION] = @I_OBSERVACION, [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(),
            [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO
        WHERE [ID_EMPRESA] = @I_ID_EMPRESA AND [ID_CLIENTE] = @I_ID_CLIENTE;

        IF @@ROWCOUNT = 0 THROW 50004, 'El cliente no existe en la empresa de la sesión.', 1;

        UPDATE [COMERCIAL].[CLIENTES_TELEFONOS]
        SET [CODIGO_ESTADO] = N'INACTIVO', [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(),
            [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO
        WHERE [ID_CLIENTE] = @I_ID_CLIENTE AND [CODIGO_ESTADO] = N'ACTIVO';

        INSERT INTO [COMERCIAL].[CLIENTES_TELEFONOS]
            ([ID_CLIENTE], [ID_TIPO_TELEFONO], [CODIGO_PAIS], [CODIGO_AREA], [NUMERO], [INTERNO],
             [ORDEN], [ES_PRINCIPAL], [PERMITE_WHATSAPP], [OBSERVACION], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
        SELECT @I_ID_CLIENTE, [TIP].[ID_TIPO_TELEFONO], [TEL].[CODIGO_PAIS], [TEL].[CODIGO_AREA],
               [TEL].[NUMERO], [TEL].[INTERNO], [TEL].[ORDEN], [TEL].[ES_PRINCIPAL],
               [TEL].[PERMITE_WHATSAPP], [TEL].[OBSERVACION], N'ACTIVO', @S_ID_USUARIO
        FROM @T_TELEFONOS AS [TEL]
        INNER JOIN [CONFIGURACION].[TIPOS_TELEFONO] AS [TIP] ON [TIP].[CODIGO] = [TEL].[CODIGO_TIPO];

        SET @O_FILAS_AFECTADAS = 1 + (SELECT COUNT(*) FROM @T_TELEFONOS);
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        SET @O_MENSAJE = ERROR_MESSAGE();
        THROW;
    END CATCH;
END;
