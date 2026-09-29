/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: SP_TRADUCCION_CATALOGO_SAVE
Procedimiento: CONFIGURACION.SP_TRADUCCION_CATALOGO_SAVE
Tipo:                  COMMAND
Archivo: SP_TRADUCCION_CATALOGO_SAVE.sql | Versión: 1.1.1 | Fecha: 2026-09-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Crea o mantiene la traducción visible de un código de catálogo existente.
Parámetros de entrada: Identificador opcional, entidad, código, cultura, textos, estado y versión.
Parámetros de sesión: Usuario responsable.
Parámetros de salida: Identificador, código, mensaje y filas afectadas.
Retorno: No retorna filas.
Tablas utilizadas: CONFIGURACION.TRADUCCIONES_CATALOGO y catálogos admitidos.
Transacción: Escritura atómica con concurrencia optimista.
Auditoría: La DAL registra el alta o modificación.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-09-20 | FABRICA | Agustin Omar Cauzi | Traducciones para culturas importables.
Historial: 1.1.1 | 2026-09-20 | FABRICA | Agustin Omar Cauzi | Documentación técnica completa del procedimiento.
===============================================================================
*/
CREATE OR ALTER PROCEDURE [CONFIGURACION].[SP_TRADUCCION_CATALOGO_SAVE]
    @I_ID_TRADUCCION_CATALOGO BIGINT = NULL,
    @I_ENTIDAD NVARCHAR(100),
    @I_CODIGO NVARCHAR(100),
    @I_CULTURA NVARCHAR(10),
    @I_NOMBRE NVARCHAR(200),
    @I_DESCRIPCION NVARCHAR(1000) = NULL,
    @I_CODIGO_ESTADO NVARCHAR(30),
    @I_ROW_VERSION BINARY(8) = NULL,
    @S_ID_USUARIO BIGINT,
    @O_ID_TRADUCCION_CATALOGO BIGINT OUTPUT,
    @O_CODIGO_ERROR BIGINT OUTPUT,
    @O_MENSAJE NVARCHAR(4000) OUTPUT,
    @O_FILAS_AFECTADAS INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    SET @O_ID_TRADUCCION_CATALOGO = @I_ID_TRADUCCION_CATALOGO; SET @O_CODIGO_ERROR = NULL; SET @O_MENSAJE = NULL; SET @O_FILAS_AFECTADAS = 0;
    SET @I_ENTIDAD = UPPER(LTRIM(RTRIM(@I_ENTIDAD))); SET @I_CODIGO = UPPER(LTRIM(RTRIM(@I_CODIGO)));

    IF NULLIF(@I_ENTIDAD, N'') IS NULL OR NULLIF(@I_CODIGO, N'') IS NULL OR NULLIF(LTRIM(RTRIM(@I_NOMBRE)), N'') IS NULL
       OR LEN(@I_CULTURA) NOT BETWEEN 4 AND 10
       OR CHARINDEX(N'-', @I_CULTURA) NOT BETWEEN 2 AND LEN(@I_CULTURA) - 1
       OR @I_CULTURA LIKE N'% %'
       OR @I_CODIGO_ESTADO NOT IN (N'ACTIVO', N'INACTIVO')
       OR (@I_ID_TRADUCCION_CATALOGO IS NOT NULL AND @I_ROW_VERSION IS NULL)
    BEGIN
        SET @O_CODIGO_ERROR = 20001;
        EXEC [AUDITORIA].[SP_ERROR_MESSAGE_RESOLVE] @I_CODIGO_ERROR = @O_CODIGO_ERROR,
            @I_MENSAJE_PERSONALIZADO = N'Entidad, código, cultura, nombre, estado y versión de edición no son válidos.', @O_MENSAJE = @O_MENSAJE OUTPUT;
        RETURN;
    END;

    IF NOT
    (
        (@I_ENTIDAD = N'MODULOS' AND EXISTS (SELECT 1 FROM [CONFIGURACION].[MODULOS] WHERE [CODIGO] = @I_CODIGO)) OR
        (@I_ENTIDAD = N'TIPOS_TELEFONO' AND EXISTS (SELECT 1 FROM [CONFIGURACION].[TIPOS_TELEFONO] WHERE [CODIGO] = @I_CODIGO)) OR
        (@I_ENTIDAD = N'TIPOS_DOCUMENTO' AND EXISTS (SELECT 1 FROM [CONFIGURACION].[TIPOS_DOCUMENTO] WHERE [CODIGO] = @I_CODIGO)) OR
        (@I_ENTIDAD = N'PAISES' AND EXISTS (SELECT 1 FROM [CONFIGURACION].[PAISES] WHERE [CODIGO] = @I_CODIGO)) OR
        (@I_ENTIDAD LIKE N'ESTADOS:%' AND EXISTS
            (SELECT 1 FROM [CONFIGURACION].[ESTADOS] WHERE [ENTIDAD] = SUBSTRING(@I_ENTIDAD, 9, 100) AND [CODIGO_ESTADO] = @I_CODIGO))
    )
    BEGIN
        SET @O_CODIGO_ERROR = 20002;
        EXEC [AUDITORIA].[SP_ERROR_MESSAGE_RESOLVE] @I_CODIGO_ERROR = @O_CODIGO_ERROR,
            @I_MENSAJE_PERSONALIZADO = N'El código de catálogo que se intenta traducir no existe.', @O_MENSAJE = @O_MENSAJE OUTPUT;
        RETURN;
    END;

    BEGIN TRY
        IF @I_ID_TRADUCCION_CATALOGO IS NULL
        BEGIN
            INSERT INTO [CONFIGURACION].[TRADUCCIONES_CATALOGO]
                ([ENTIDAD], [CODIGO], [CULTURA], [NOMBRE], [DESCRIPCION], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
            VALUES (@I_ENTIDAD, @I_CODIGO, @I_CULTURA, LTRIM(RTRIM(@I_NOMBRE)),
                    NULLIF(LTRIM(RTRIM(@I_DESCRIPCION)), N''), @I_CODIGO_ESTADO, @S_ID_USUARIO);
            SET @O_FILAS_AFECTADAS = @@ROWCOUNT; SET @O_ID_TRADUCCION_CATALOGO = SCOPE_IDENTITY();
        END
        ELSE
        BEGIN
            UPDATE [CONFIGURACION].[TRADUCCIONES_CATALOGO]
            SET [NOMBRE] = LTRIM(RTRIM(@I_NOMBRE)), [DESCRIPCION] = NULLIF(LTRIM(RTRIM(@I_DESCRIPCION)), N''),
                [CODIGO_ESTADO] = @I_CODIGO_ESTADO, [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(),
                [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO
            WHERE [ID_TRADUCCION_CATALOGO] = @I_ID_TRADUCCION_CATALOGO AND [ROW_VERSION] = @I_ROW_VERSION;
            SET @O_FILAS_AFECTADAS = @@ROWCOUNT;
            IF @O_FILAS_AFECTADAS = 0
            BEGIN
                SET @O_CODIGO_ERROR = CASE WHEN EXISTS (SELECT 1 FROM [CONFIGURACION].[TRADUCCIONES_CATALOGO] WHERE [ID_TRADUCCION_CATALOGO] = @I_ID_TRADUCCION_CATALOGO) THEN 20003 ELSE 20002 END;
                EXEC [AUDITORIA].[SP_ERROR_MESSAGE_RESOLVE] @I_CODIGO_ERROR = @O_CODIGO_ERROR,
                    @I_MENSAJE_PERSONALIZADO = NULL, @O_MENSAJE = @O_MENSAJE OUTPUT;
            END;
        END;
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() IN (2601, 2627)
        BEGIN
            SET @O_CODIGO_ERROR = 20001; SET @O_MENSAJE = N'Ya existe una traducción para ese catálogo, código e idioma.'; RETURN;
        END;
        SET @O_CODIGO_ERROR = 70002; SET @O_MENSAJE = ERROR_MESSAGE(); THROW;
    END CATCH;
END;
