/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: SP_TIPO_TELEFONO_SAVE
Procedimiento: CONFIGURACION.SP_TIPO_TELEFONO_SAVE
Tipo:                  COMMAND
Archivo: SP_TIPO_TELEFONO_SAVE.sql | Versión: 1.0.1 | Fecha: 2026-09-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Crea o mantiene tipos de teléfono conservando su código técnico.
Parámetros de entrada: Identificador opcional, código de alta, textos, estado y versión.
Parámetros de sesión: Usuario responsable.
Parámetros de salida: Identificador, código, mensaje y filas afectadas.
Retorno: No retorna filas.
Tablas utilizadas: CONFIGURACION.TIPOS_TELEFONO - INSERT/UPDATE.
Transacción: Escritura atómica con concurrencia optimista.
Auditoría: La DAL registra el alta o modificación.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.0.1 | 2026-09-20 | FABRICA | Agustin Omar Cauzi | Documentación técnica completa del procedimiento.
===============================================================================
*/
CREATE OR ALTER PROCEDURE [CONFIGURACION].[SP_TIPO_TELEFONO_SAVE]
    @I_ID_TIPO_TELEFONO BIGINT = NULL,
    @I_CODIGO NVARCHAR(30),
    @I_NOMBRE NVARCHAR(100),
    @I_DESCRIPCION NVARCHAR(500) = NULL,
    @I_CODIGO_ESTADO NVARCHAR(30),
    @I_ROW_VERSION BINARY(8) = NULL,
    @S_ID_USUARIO BIGINT,
    @O_ID_TIPO_TELEFONO BIGINT OUTPUT,
    @O_CODIGO_ERROR BIGINT OUTPUT,
    @O_MENSAJE NVARCHAR(4000) OUTPUT,
    @O_FILAS_AFECTADAS INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    SET @O_ID_TIPO_TELEFONO = @I_ID_TIPO_TELEFONO; SET @O_CODIGO_ERROR = NULL; SET @O_MENSAJE = NULL; SET @O_FILAS_AFECTADAS = 0;

    IF NULLIF(LTRIM(RTRIM(@I_CODIGO)), N'') IS NULL OR NULLIF(LTRIM(RTRIM(@I_NOMBRE)), N'') IS NULL
       OR @I_CODIGO_ESTADO NOT IN (N'ACTIVO', N'INACTIVO') OR (@I_ID_TIPO_TELEFONO IS NOT NULL AND @I_ROW_VERSION IS NULL)
    BEGIN
        SET @O_CODIGO_ERROR = 20001;
        EXEC [AUDITORIA].[SP_ERROR_MESSAGE_RESOLVE] @I_CODIGO_ERROR = @O_CODIGO_ERROR,
            @I_MENSAJE_PERSONALIZADO = N'Código, nombre, estado y versión de edición son obligatorios.', @O_MENSAJE = @O_MENSAJE OUTPUT;
        RETURN;
    END;

    IF EXISTS (SELECT 1 FROM [CONFIGURACION].[TIPOS_TELEFONO] WHERE [CODIGO] = UPPER(LTRIM(RTRIM(@I_CODIGO)))
               AND [ID_TIPO_TELEFONO] <> COALESCE(@I_ID_TIPO_TELEFONO, -1))
    BEGIN
        SET @O_CODIGO_ERROR = 20001;
        EXEC [AUDITORIA].[SP_ERROR_MESSAGE_RESOLVE] @I_CODIGO_ERROR = @O_CODIGO_ERROR,
            @I_MENSAJE_PERSONALIZADO = N'Ya existe un tipo de teléfono con ese código.', @O_MENSAJE = @O_MENSAJE OUTPUT;
        RETURN;
    END;

    BEGIN TRY
        IF @I_ID_TIPO_TELEFONO IS NULL
        BEGIN
            INSERT INTO [CONFIGURACION].[TIPOS_TELEFONO] ([CODIGO], [NOMBRE], [DESCRIPCION], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
            VALUES (UPPER(LTRIM(RTRIM(@I_CODIGO))), LTRIM(RTRIM(@I_NOMBRE)), NULLIF(LTRIM(RTRIM(@I_DESCRIPCION)), N''),
                    @I_CODIGO_ESTADO, @S_ID_USUARIO);
            SET @O_FILAS_AFECTADAS = @@ROWCOUNT; SET @O_ID_TIPO_TELEFONO = SCOPE_IDENTITY();
        END
        ELSE
        BEGIN
            UPDATE [CONFIGURACION].[TIPOS_TELEFONO]
            SET [NOMBRE] = LTRIM(RTRIM(@I_NOMBRE)), [DESCRIPCION] = NULLIF(LTRIM(RTRIM(@I_DESCRIPCION)), N''),
                [CODIGO_ESTADO] = @I_CODIGO_ESTADO, [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(),
                [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO
            WHERE [ID_TIPO_TELEFONO] = @I_ID_TIPO_TELEFONO AND [ROW_VERSION] = @I_ROW_VERSION;
            SET @O_FILAS_AFECTADAS = @@ROWCOUNT;
            IF @O_FILAS_AFECTADAS = 0
            BEGIN
                SET @O_CODIGO_ERROR = CASE WHEN EXISTS (SELECT 1 FROM [CONFIGURACION].[TIPOS_TELEFONO] WHERE [ID_TIPO_TELEFONO] = @I_ID_TIPO_TELEFONO) THEN 20003 ELSE 20002 END;
                EXEC [AUDITORIA].[SP_ERROR_MESSAGE_RESOLVE] @I_CODIGO_ERROR = @O_CODIGO_ERROR,
                    @I_MENSAJE_PERSONALIZADO = NULL, @O_MENSAJE = @O_MENSAJE OUTPUT;
            END;
        END;
    END TRY
    BEGIN CATCH
        SET @O_CODIGO_ERROR = 70002; SET @O_MENSAJE = ERROR_MESSAGE(); THROW;
    END CATCH;
END;
