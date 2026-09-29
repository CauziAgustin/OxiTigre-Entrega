/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: SP_ORDEN_COMPRA_CANCEL
Archivo: SP_ORDEN_COMPRA_CANCEL.sql | Procedimiento: COMPRAS.SP_ORDEN_COMPRA_CANCEL | Tipo: COMMAND
Versión: 1.0.1 | Fecha: 2026-09-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Cancela una orden sin recepciones y conserva la transición.
Parámetros de entrada: @I_ID_EMPRESA, @I_ID_ORDEN_COMPRA, @I_ROW_VERSION, @S_ID_SESION, @S_ID_USUARIO.
Parámetros de salida: @O_CODIGO_ERROR, @O_FILAS_AFECTADAS, @O_MENSAJE.
Retorno: Informa el resultado mediante los parámetros de salida declarados.
Tablas utilizadas: AUDITORIA.BITACORA_CAMBIOS_ESTADO - INSERT; COMPRAS.ORDENES_COMPRA - SELECT/UPDATE; COMPRAS.RECEPCIONES_COMPRA - SELECT.
Transacción: Abre una transacción explícita para agrupar sus escrituras.
Auditoría: Registra información mediante objetos del esquema AUDITORIA referenciados por el procedimiento.
Historial: 1.0.0 | 2026-08-24 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.0.1 | 2026-09-20 | FABRICA | Agustin Omar Cauzi | Documentación técnica completa del procedimiento.
===============================================================================
*/
CREATE OR ALTER PROCEDURE [COMPRAS].[SP_ORDEN_COMPRA_CANCEL]
    @I_ID_EMPRESA BIGINT, @I_ID_ORDEN_COMPRA BIGINT, @I_ROW_VERSION BINARY(8),
    @S_ID_SESION BIGINT, @S_ID_USUARIO BIGINT,
    @O_CODIGO_ERROR BIGINT OUTPUT, @O_MENSAJE NVARCHAR(4000) OUTPUT, @O_FILAS_AFECTADAS INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON; SET @O_CODIGO_ERROR = NULL; SET @O_MENSAJE = NULL; SET @O_FILAS_AFECTADAS = 0;
    BEGIN TRANSACTION;
    DECLARE @V_ESTADO NVARCHAR(30);
    SELECT @V_ESTADO = [CODIGO_ESTADO] FROM [COMPRAS].[ORDENES_COMPRA] WITH (UPDLOCK, HOLDLOCK)
    WHERE [ID_ORDEN_COMPRA] = @I_ID_ORDEN_COMPRA AND [ID_EMPRESA] = @I_ID_EMPRESA AND [ROW_VERSION] = @I_ROW_VERSION;
    IF @V_ESTADO NOT IN (N'BORRADOR', N'PENDIENTE_APROBACION', N'APROBADA') OR EXISTS (SELECT 1 FROM [COMPRAS].[RECEPCIONES_COMPRA] WHERE [ID_ORDEN_COMPRA] = @I_ID_ORDEN_COMPRA)
    BEGIN SET @O_CODIGO_ERROR = 50003; SET @O_MENSAJE = N'La orden no puede cancelarse en su estado actual o ya posee recepciones.'; ROLLBACK; RETURN; END;
    UPDATE [COMPRAS].[ORDENES_COMPRA] SET [CODIGO_ESTADO] = N'CANCELADA', [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(), [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO WHERE [ID_ORDEN_COMPRA] = @I_ID_ORDEN_COMPRA;
    SET @O_FILAS_AFECTADAS = @@ROWCOUNT;
    INSERT INTO [AUDITORIA].[BITACORA_CAMBIOS_ESTADO] ([ENTIDAD], [PKEY], [CODIGO_ESTADO_ANTERIOR], [CODIGO_ESTADO_NUEVO], [MOTIVO], [ID_USUARIO], [ID_CORRELACION], [ID_USUARIO_ALTA])
    VALUES (N'ORDEN_COMPRA', @I_ID_ORDEN_COMPRA, @V_ESTADO, N'CANCELADA', N'Cancelación', @S_ID_USUARIO, NEWID(), @S_ID_USUARIO);
    COMMIT;
END;
