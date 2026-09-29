/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: SP_ERROR_UPDATE
Procedimiento: AUDITORIA.SP_ERROR_UPDATE
Tipo:                  COMMAND
Archivo: SP_ERROR_UPDATE.sql | Versión: 1.0.1 | Fecha: 2026-09-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Mantiene el diagnóstico de un código sin modificar módulo, número ni código generado.
Parámetros de entrada: Registro, diagnóstico, severidad, estado y versión.
Parámetros de sesión: Usuario responsable.
Parámetros de salida: Código, mensaje y filas afectadas.
Retorno: No retorna filas.
Tablas utilizadas: AUDITORIA.CATALOGO_ERRORES - UPDATE.
Transacción: Actualización atómica con concurrencia optimista.
Auditoría: La DAL registra la modificación.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.0.1 | 2026-09-20 | FABRICA | Agustin Omar Cauzi | Documentación técnica completa del procedimiento.
===============================================================================
*/
CREATE OR ALTER PROCEDURE [AUDITORIA].[SP_ERROR_UPDATE]
    @I_ID_CATALOGO_ERROR BIGINT,
    @I_NOMBRE NVARCHAR(200),
    @I_DESCRIPCION NVARCHAR(1000),
    @I_CAUSA_PROBABLE NVARCHAR(1000) = NULL,
    @I_ACCION_RECOMENDADA NVARCHAR(2000) = NULL,
    @I_SEVERIDAD NVARCHAR(30),
    @I_CODIGO_ESTADO NVARCHAR(30),
    @I_ROW_VERSION BINARY(8),
    @S_ID_USUARIO BIGINT,
    @O_CODIGO_ERROR BIGINT OUTPUT,
    @O_MENSAJE NVARCHAR(4000) OUTPUT,
    @O_FILAS_AFECTADAS INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    SET @O_CODIGO_ERROR = NULL; SET @O_MENSAJE = NULL; SET @O_FILAS_AFECTADAS = 0;

    IF NULLIF(LTRIM(RTRIM(@I_NOMBRE)), N'') IS NULL OR NULLIF(LTRIM(RTRIM(@I_DESCRIPCION)), N'') IS NULL
       OR @I_SEVERIDAD NOT IN (N'INFORMATIVO', N'ADVERTENCIA', N'ERROR', N'CRITICO')
       OR @I_CODIGO_ESTADO NOT IN (N'ACTIVO', N'INACTIVO') OR @I_ROW_VERSION IS NULL
    BEGIN
        SET @O_CODIGO_ERROR = 70003;
        EXEC [AUDITORIA].[SP_ERROR_MESSAGE_RESOLVE] @I_CODIGO_ERROR = @O_CODIGO_ERROR,
            @I_MENSAJE_PERSONALIZADO = NULL, @O_MENSAJE = @O_MENSAJE OUTPUT;
        RETURN;
    END;

    BEGIN TRY
        UPDATE [AUDITORIA].[CATALOGO_ERRORES]
        SET [NOMBRE] = LTRIM(RTRIM(@I_NOMBRE)), [DESCRIPCION] = LTRIM(RTRIM(@I_DESCRIPCION)),
            [CAUSA_PROBABLE] = NULLIF(LTRIM(RTRIM(@I_CAUSA_PROBABLE)), N''),
            [ACCION_RECOMENDADA] = NULLIF(LTRIM(RTRIM(@I_ACCION_RECOMENDADA)), N''),
            [SEVERIDAD] = @I_SEVERIDAD, [CODIGO_ESTADO] = @I_CODIGO_ESTADO,
            [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(), [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO
        WHERE [ID_CATALOGO_ERROR] = @I_ID_CATALOGO_ERROR AND [ROW_VERSION] = @I_ROW_VERSION;
        SET @O_FILAS_AFECTADAS = @@ROWCOUNT;
        IF @O_FILAS_AFECTADAS = 0
        BEGIN
            SET @O_CODIGO_ERROR = CASE WHEN EXISTS (SELECT 1 FROM [AUDITORIA].[CATALOGO_ERRORES] WHERE [ID_CATALOGO_ERROR] = @I_ID_CATALOGO_ERROR) THEN 20003 ELSE 20002 END;
            EXEC [AUDITORIA].[SP_ERROR_MESSAGE_RESOLVE] @I_CODIGO_ERROR = @O_CODIGO_ERROR,
                @I_MENSAJE_PERSONALIZADO = NULL, @O_MENSAJE = @O_MENSAJE OUTPUT;
        END;
    END TRY
    BEGIN CATCH
        SET @O_CODIGO_ERROR = 70002; SET @O_MENSAJE = ERROR_MESSAGE(); THROW;
    END CATCH;
END;
