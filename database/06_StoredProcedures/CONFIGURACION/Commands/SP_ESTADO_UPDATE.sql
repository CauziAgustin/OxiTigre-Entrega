/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: SP_ESTADO_UPDATE
Procedimiento: CONFIGURACION.SP_ESTADO_UPDATE
Tipo:                  COMMAND
Archivo: SP_ESTADO_UPDATE.sql | Versión: 1.0.1 | Fecha: 2026-09-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Mantiene los textos, orden y visibilidad de un estado sin cambiar su identidad técnica.
Parámetros de entrada: Estado, textos, orden, vigencia y versión.
Parámetros de sesión: Usuario responsable.
Parámetros de salida: Código, mensaje y filas afectadas.
Retorno: No retorna filas.
Tablas utilizadas: CONFIGURACION.ESTADOS - UPDATE.
Transacción: Actualización atómica con concurrencia optimista.
Auditoría: La DAL registra la modificación.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.0.1 | 2026-09-20 | FABRICA | Agustin Omar Cauzi | Documentación técnica completa del procedimiento.
===============================================================================
*/
CREATE OR ALTER PROCEDURE [CONFIGURACION].[SP_ESTADO_UPDATE]
    @I_ID_ESTADO BIGINT,
    @I_NOMBRE NVARCHAR(100),
    @I_DESCRIPCION NVARCHAR(500) = NULL,
    @I_ORDEN SMALLINT,
    @I_FECHA_VIGENCIA_HASTA_UTC DATETIME2(3) = NULL,
    @I_ROW_VERSION BINARY(8),
    @S_ID_USUARIO BIGINT,
    @O_CODIGO_ERROR BIGINT OUTPUT,
    @O_MENSAJE NVARCHAR(4000) OUTPUT,
    @O_FILAS_AFECTADAS INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    SET @O_CODIGO_ERROR = NULL; SET @O_MENSAJE = NULL; SET @O_FILAS_AFECTADAS = 0;

    IF NULLIF(LTRIM(RTRIM(@I_NOMBRE)), N'') IS NULL OR @I_ORDEN < 0 OR @I_ROW_VERSION IS NULL
    BEGIN
        SET @O_CODIGO_ERROR = 20001;
        EXEC [AUDITORIA].[SP_ERROR_MESSAGE_RESOLVE] @I_CODIGO_ERROR = @O_CODIGO_ERROR,
            @I_MENSAJE_PERSONALIZADO = N'Nombre, orden no negativo y versión son obligatorios.', @O_MENSAJE = @O_MENSAJE OUTPUT;
        RETURN;
    END;

    BEGIN TRY
        UPDATE [CONFIGURACION].[ESTADOS]
        SET [NOMBRE] = LTRIM(RTRIM(@I_NOMBRE)), [DESCRIPCION] = NULLIF(LTRIM(RTRIM(@I_DESCRIPCION)), N''),
            [ORDEN] = @I_ORDEN, [FECHA_VIGENCIA_HASTA_UTC] = @I_FECHA_VIGENCIA_HASTA_UTC,
            [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(), [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO
        WHERE [ID_ESTADO] = @I_ID_ESTADO AND [ROW_VERSION] = @I_ROW_VERSION;
        SET @O_FILAS_AFECTADAS = @@ROWCOUNT;
        IF @O_FILAS_AFECTADAS = 0
        BEGIN
            SET @O_CODIGO_ERROR = CASE WHEN EXISTS (SELECT 1 FROM [CONFIGURACION].[ESTADOS] WHERE [ID_ESTADO] = @I_ID_ESTADO) THEN 20003 ELSE 20002 END;
            EXEC [AUDITORIA].[SP_ERROR_MESSAGE_RESOLVE] @I_CODIGO_ERROR = @O_CODIGO_ERROR,
                @I_MENSAJE_PERSONALIZADO = NULL, @O_MENSAJE = @O_MENSAJE OUTPUT;
        END;
    END TRY
    BEGIN CATCH
        SET @O_CODIGO_ERROR = 70002; SET @O_MENSAJE = ERROR_MESSAGE(); THROW;
    END CATCH;
END;
