/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: SP_MODULO_SAVE
Procedimiento: CONFIGURACION.SP_MODULO_SAVE
Tipo:                  COMMAND
Archivo: SP_MODULO_SAVE.sql | Versión: 1.0.1 | Fecha: 2026-09-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Crea módulos o mantiene sus textos sin alterar identidades técnicas utilizadas.
Parámetros de entrada: Identificador opcional, número/código de alta, textos, orden, estado y versión.
Parámetros de sesión: Usuario responsable.
Parámetros de salida: Identificador, código, mensaje y filas afectadas.
Retorno: No retorna filas.
Tablas utilizadas: CONFIGURACION.MODULOS y sus dependencias - INSERT/UPDATE/READ.
Transacción: Escritura atómica con concurrencia optimista.
Auditoría: La DAL registra el alta o modificación.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.0.1 | 2026-09-20 | FABRICA | Agustin Omar Cauzi | Documentación técnica completa del procedimiento.
===============================================================================
*/
CREATE OR ALTER PROCEDURE [CONFIGURACION].[SP_MODULO_SAVE]
    @I_ID_MODULO BIGINT = NULL,
    @I_NUMERO_MODULO SMALLINT,
    @I_CODIGO NVARCHAR(30),
    @I_NOMBRE NVARCHAR(150),
    @I_DESCRIPCION NVARCHAR(500) = NULL,
    @I_ORDEN SMALLINT,
    @I_CODIGO_ESTADO NVARCHAR(30),
    @I_ROW_VERSION BINARY(8) = NULL,
    @S_ID_USUARIO BIGINT,
    @O_ID_MODULO BIGINT OUTPUT,
    @O_CODIGO_ERROR BIGINT OUTPUT,
    @O_MENSAJE NVARCHAR(4000) OUTPUT,
    @O_FILAS_AFECTADAS INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    SET @O_ID_MODULO = @I_ID_MODULO; SET @O_CODIGO_ERROR = NULL; SET @O_MENSAJE = NULL; SET @O_FILAS_AFECTADAS = 0;

    IF @I_NUMERO_MODULO NOT BETWEEN 1 AND 999 OR NULLIF(LTRIM(RTRIM(@I_CODIGO)), N'') IS NULL
       OR NULLIF(LTRIM(RTRIM(@I_NOMBRE)), N'') IS NULL OR @I_ORDEN < 0
       OR @I_CODIGO_ESTADO NOT IN (N'ACTIVO', N'INACTIVO') OR (@I_ID_MODULO IS NOT NULL AND @I_ROW_VERSION IS NULL)
    BEGIN
        SET @O_CODIGO_ERROR = 20001;
        EXEC [AUDITORIA].[SP_ERROR_MESSAGE_RESOLVE] @I_CODIGO_ERROR = @O_CODIGO_ERROR,
            @I_MENSAJE_PERSONALIZADO = N'Número, código, nombre, orden, estado y versión de edición no son válidos.', @O_MENSAJE = @O_MENSAJE OUTPUT;
        RETURN;
    END;

    IF @I_CODIGO_ESTADO = N'INACTIVO' AND @I_ID_MODULO IS NOT NULL AND
    (
        EXISTS (SELECT 1 FROM [SEGURIDAD].[PERMISOS] WHERE [ID_MODULO] = @I_ID_MODULO AND [CODIGO_ESTADO] = N'ACTIVO') OR
        EXISTS (SELECT 1 FROM [CONFIGURACION].[PARAMETROS_SISTEMA] WHERE [ID_MODULO] = @I_ID_MODULO AND [CODIGO_ESTADO] = N'ACTIVO') OR
        EXISTS (SELECT 1 FROM [AUDITORIA].[CATALOGO_ERRORES] WHERE [ID_MODULO] = @I_ID_MODULO AND [CODIGO_ESTADO] = N'ACTIVO')
    )
    BEGIN
        SET @O_CODIGO_ERROR = 20004;
        EXEC [AUDITORIA].[SP_ERROR_MESSAGE_RESOLVE] @I_CODIGO_ERROR = @O_CODIGO_ERROR,
            @I_MENSAJE_PERSONALIZADO = N'No se puede inactivar un módulo con permisos, parámetros o errores activos.', @O_MENSAJE = @O_MENSAJE OUTPUT;
        RETURN;
    END;

    IF EXISTS (SELECT 1 FROM [CONFIGURACION].[MODULOS] WHERE ([NUMERO_MODULO] = @I_NUMERO_MODULO OR [CODIGO] = UPPER(LTRIM(RTRIM(@I_CODIGO))))
               AND [ID_MODULO] <> COALESCE(@I_ID_MODULO, -1))
    BEGIN
        SET @O_CODIGO_ERROR = 20001;
        EXEC [AUDITORIA].[SP_ERROR_MESSAGE_RESOLVE] @I_CODIGO_ERROR = @O_CODIGO_ERROR,
            @I_MENSAJE_PERSONALIZADO = N'Ya existe un módulo con ese número o código.', @O_MENSAJE = @O_MENSAJE OUTPUT;
        RETURN;
    END;

    BEGIN TRY
        IF @I_ID_MODULO IS NULL
        BEGIN
            INSERT INTO [CONFIGURACION].[MODULOS]
                ([NUMERO_MODULO], [CODIGO], [NOMBRE], [DESCRIPCION], [ORDEN], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
            VALUES (@I_NUMERO_MODULO, UPPER(LTRIM(RTRIM(@I_CODIGO))), LTRIM(RTRIM(@I_NOMBRE)),
                    NULLIF(LTRIM(RTRIM(@I_DESCRIPCION)), N''), @I_ORDEN, @I_CODIGO_ESTADO, @S_ID_USUARIO);
            SET @O_FILAS_AFECTADAS = @@ROWCOUNT; SET @O_ID_MODULO = SCOPE_IDENTITY();
        END
        ELSE
        BEGIN
            UPDATE [CONFIGURACION].[MODULOS]
            SET [NOMBRE] = LTRIM(RTRIM(@I_NOMBRE)), [DESCRIPCION] = NULLIF(LTRIM(RTRIM(@I_DESCRIPCION)), N''),
                [ORDEN] = @I_ORDEN, [CODIGO_ESTADO] = @I_CODIGO_ESTADO,
                [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(), [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO
            WHERE [ID_MODULO] = @I_ID_MODULO AND [ROW_VERSION] = @I_ROW_VERSION;
            SET @O_FILAS_AFECTADAS = @@ROWCOUNT;
            IF @O_FILAS_AFECTADAS = 0
            BEGIN
                SET @O_CODIGO_ERROR = CASE WHEN EXISTS (SELECT 1 FROM [CONFIGURACION].[MODULOS] WHERE [ID_MODULO] = @I_ID_MODULO) THEN 20003 ELSE 20002 END;
                EXEC [AUDITORIA].[SP_ERROR_MESSAGE_RESOLVE] @I_CODIGO_ERROR = @O_CODIGO_ERROR,
                    @I_MENSAJE_PERSONALIZADO = NULL, @O_MENSAJE = @O_MENSAJE OUTPUT;
            END;
        END;
    END TRY
    BEGIN CATCH
        SET @O_CODIGO_ERROR = 70002; SET @O_MENSAJE = ERROR_MESSAGE(); THROW;
    END CATCH;
END;
