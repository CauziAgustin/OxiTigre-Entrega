/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: SP_PEDIDO_CANCEL
Archivo: SP_PEDIDO_CANCEL.sql | Procedimiento: COMERCIAL.SP_PEDIDO_CANCEL | Tipo: COMMAND
Versión: 1.0.1 | Fecha: 2026-09-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Cancela un pedido editable o confirmado y libera sus reservas activas.
Parámetros de entrada: @I_ID_EMPRESA, @I_ID_PEDIDO, @I_ROW_VERSION, @S_ID_SESION, @S_ID_USUARIO.
Parámetros de salida: @O_CODIGO_ERROR, @O_FILAS_AFECTADAS, @O_MENSAJE.
Retorno: Informa el resultado mediante los parámetros de salida declarados.
Tablas utilizadas: COMERCIAL.PEDIDOS - SELECT/UPDATE; INVENTARIO.RESERVAS_STOCK - UPDATE.
Transacción: Abre una transacción explícita para agrupar sus escrituras.
Auditoría: No registra auditoría explícita dentro del procedimiento.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.0.1 | 2026-09-20 | FABRICA | Agustin Omar Cauzi | Documentación técnica completa del procedimiento.
===============================================================================
*/
CREATE OR ALTER PROCEDURE [COMERCIAL].[SP_PEDIDO_CANCEL]
    @I_ID_PEDIDO BIGINT, @I_ID_EMPRESA BIGINT, @I_ROW_VERSION BINARY(8), @S_ID_SESION BIGINT, @S_ID_USUARIO BIGINT,
    @O_CODIGO_ERROR BIGINT OUTPUT, @O_MENSAJE NVARCHAR(4000) OUTPUT, @O_FILAS_AFECTADAS INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON; SET @O_CODIGO_ERROR = NULL; SET @O_MENSAJE = NULL; SET @O_FILAS_AFECTADAS = 0;
    DECLARE @V_INICIO_TRANSACCION BIT = 0;
    BEGIN TRY
        IF @@TRANCOUNT = 0 BEGIN BEGIN TRANSACTION; SET @V_INICIO_TRANSACCION = 1; END ELSE SAVE TRANSACTION [SP_PEDIDO_CANCEL_SAVEPOINT];
        IF NOT EXISTS (SELECT 1 FROM [COMERCIAL].[PEDIDOS] WITH (UPDLOCK, HOLDLOCK) WHERE [ID_PEDIDO] = @I_ID_PEDIDO AND [ID_EMPRESA] = @I_ID_EMPRESA AND [CODIGO_ESTADO] IN (N'BORRADOR', N'CONFIRMADO') AND [ROW_VERSION] = @I_ROW_VERSION)
        BEGIN SET @O_CODIGO_ERROR = 40006; SET @O_MENSAJE = N'El pedido fue modificado o su estado no permite cancelarlo.'; IF @V_INICIO_TRANSACCION = 1 ROLLBACK TRANSACTION; RETURN; END;
        UPDATE [INVENTARIO].[RESERVAS_STOCK] SET [CODIGO_ESTADO] = N'LIBERADA', [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(), [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO WHERE [ID_EMPRESA] = @I_ID_EMPRESA AND [ID_PEDIDO] = @I_ID_PEDIDO AND [CODIGO_ESTADO] = N'RESERVADA';
        SET @O_FILAS_AFECTADAS = @@ROWCOUNT;
        UPDATE [COMERCIAL].[PEDIDOS] SET [CODIGO_ESTADO] = N'CANCELADO', [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(), [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO WHERE [ID_PEDIDO] = @I_ID_PEDIDO;
        SET @O_FILAS_AFECTADAS += 1;
        IF @V_INICIO_TRANSACCION = 1 COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @V_INICIO_TRANSACCION = 1 AND XACT_STATE() <> 0 ROLLBACK TRANSACTION; ELSE IF XACT_STATE() = 1 ROLLBACK TRANSACTION [SP_PEDIDO_CANCEL_SAVEPOINT]; SET @O_MENSAJE = ERROR_MESSAGE(); THROW;
    END CATCH;
END;
