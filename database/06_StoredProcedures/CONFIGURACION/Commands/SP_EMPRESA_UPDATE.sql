/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: SP_EMPRESA_UPDATE
Procedimiento: CONFIGURACION.SP_EMPRESA_UPDATE
Tipo:                  COMMAND
Archivo: SP_EMPRESA_UPDATE.sql | Versión: 1.0.1 | Fecha: 2026-09-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Actualiza los datos visibles de la empresa propietaria sin alterar su código ni estado.
Parámetros de entrada: Empresa, datos editables y versión leída.
Parámetros de sesión: Usuario responsable.
Parámetros de salida: Código, mensaje y filas afectadas.
Retorno: No retorna filas.
Tablas utilizadas: CONFIGURACION.EMPRESAS - UPDATE.
Transacción: Actualización atómica con concurrencia optimista.
Auditoría: La DAL registra la modificación funcional.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.0.1 | 2026-09-20 | FABRICA | Agustin Omar Cauzi | Documentación técnica completa del procedimiento.
===============================================================================
*/
CREATE OR ALTER PROCEDURE [CONFIGURACION].[SP_EMPRESA_UPDATE]
    @I_ID_EMPRESA BIGINT,
    @I_RAZON_SOCIAL NVARCHAR(200),
    @I_NOMBRE_FANTASIA NVARCHAR(200) = NULL,
    @I_CUIT CHAR(11) = NULL,
    @I_EMAIL NVARCHAR(254) = NULL,
    @I_ROW_VERSION BINARY(8),
    @S_ID_USUARIO BIGINT,
    @O_CODIGO_ERROR BIGINT OUTPUT,
    @O_MENSAJE NVARCHAR(4000) OUTPUT,
    @O_FILAS_AFECTADAS INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @O_CODIGO_ERROR = NULL; SET @O_MENSAJE = NULL; SET @O_FILAS_AFECTADAS = 0;

    IF NULLIF(LTRIM(RTRIM(@I_RAZON_SOCIAL)), N'') IS NULL OR @I_ROW_VERSION IS NULL
    BEGIN
        SET @O_CODIGO_ERROR = 20001;
        EXEC [AUDITORIA].[SP_ERROR_MESSAGE_RESOLVE] @I_CODIGO_ERROR = @O_CODIGO_ERROR,
            @I_MENSAJE_PERSONALIZADO = N'La razón social y la versión del registro son obligatorias.', @O_MENSAJE = @O_MENSAJE OUTPUT;
        RETURN;
    END;

    BEGIN TRY
        UPDATE [CONFIGURACION].[EMPRESAS]
        SET [RAZON_SOCIAL] = LTRIM(RTRIM(@I_RAZON_SOCIAL)), [NOMBRE_FANTASIA] = NULLIF(LTRIM(RTRIM(@I_NOMBRE_FANTASIA)), N''),
            [CUIT] = NULLIF(LTRIM(RTRIM(@I_CUIT)), ''), [EMAIL] = NULLIF(LTRIM(RTRIM(@I_EMAIL)), N''),
            [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(), [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO
        WHERE [ID_EMPRESA] = @I_ID_EMPRESA AND [ROW_VERSION] = @I_ROW_VERSION;
        SET @O_FILAS_AFECTADAS = @@ROWCOUNT;

        IF @O_FILAS_AFECTADAS = 0
        BEGIN
            SET @O_CODIGO_ERROR = CASE WHEN EXISTS (SELECT 1 FROM [CONFIGURACION].[EMPRESAS] WHERE [ID_EMPRESA] = @I_ID_EMPRESA) THEN 20003 ELSE 20002 END;
            EXEC [AUDITORIA].[SP_ERROR_MESSAGE_RESOLVE] @I_CODIGO_ERROR = @O_CODIGO_ERROR,
                @I_MENSAJE_PERSONALIZADO = NULL, @O_MENSAJE = @O_MENSAJE OUTPUT;
        END;
    END TRY
    BEGIN CATCH
        SET @O_CODIGO_ERROR = 70002; SET @O_MENSAJE = ERROR_MESSAGE(); THROW;
    END CATCH;
END;
