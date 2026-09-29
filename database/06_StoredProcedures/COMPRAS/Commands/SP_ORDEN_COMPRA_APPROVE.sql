/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: SP_ORDEN_COMPRA_APPROVE
Archivo: SP_ORDEN_COMPRA_APPROVE.sql | Procedimiento: COMPRAS.SP_ORDEN_COMPRA_APPROVE | Tipo: COMMAND
Versión: 1.0.1 | Fecha: 2026-09-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Aprueba una orden enviada y registra responsable y fecha.
Parámetros de entrada: @I_ID_EMPRESA, @I_ID_ORDEN_COMPRA, @I_ROW_VERSION, @S_ID_SESION, @S_ID_USUARIO.
Parámetros de salida: @O_CODIGO_ERROR, @O_FILAS_AFECTADAS, @O_MENSAJE.
Retorno: Informa el resultado mediante los parámetros de salida declarados.
Tablas utilizadas: AUDITORIA.BITACORA_CAMBIOS_ESTADO - INSERT; COMPRAS.ORDENES_COMPRA - UPDATE.
Transacción: Abre una transacción explícita para agrupar sus escrituras.
Auditoría: Registra información mediante objetos del esquema AUDITORIA referenciados por el procedimiento.
Historial: 1.0.0 | 2026-08-24 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.0.1 | 2026-09-20 | FABRICA | Agustin Omar Cauzi | Documentación técnica completa del procedimiento.
===============================================================================
*/
CREATE OR ALTER PROCEDURE [COMPRAS].[SP_ORDEN_COMPRA_APPROVE]
    @I_ID_EMPRESA BIGINT, @I_ID_ORDEN_COMPRA BIGINT, @I_ROW_VERSION BINARY(8),
    @S_ID_SESION BIGINT, @S_ID_USUARIO BIGINT,
    @O_CODIGO_ERROR BIGINT OUTPUT, @O_MENSAJE NVARCHAR(4000) OUTPUT, @O_FILAS_AFECTADAS INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON; SET @O_CODIGO_ERROR = NULL; SET @O_MENSAJE = NULL; SET @O_FILAS_AFECTADAS = 0;
    BEGIN TRANSACTION;
    UPDATE [COMPRAS].[ORDENES_COMPRA] SET [CODIGO_ESTADO] = N'APROBADA', [FECHA_APROBACION_UTC] = SYSUTCDATETIME(),
        [ID_USUARIO_APROBACION] = @S_ID_USUARIO, [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(), [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO
    WHERE [ID_ORDEN_COMPRA] = @I_ID_ORDEN_COMPRA AND [ID_EMPRESA] = @I_ID_EMPRESA
      AND [CODIGO_ESTADO] = N'PENDIENTE_APROBACION' AND [ROW_VERSION] = @I_ROW_VERSION;
    SET @O_FILAS_AFECTADAS = @@ROWCOUNT;
    IF @O_FILAS_AFECTADAS = 0 BEGIN SET @O_CODIGO_ERROR = 50003; SET @O_MENSAJE = N'Solo una orden pendiente y vigente puede aprobarse.'; ROLLBACK; RETURN; END;
    INSERT INTO [AUDITORIA].[BITACORA_CAMBIOS_ESTADO] ([ENTIDAD], [PKEY], [CODIGO_ESTADO_ANTERIOR], [CODIGO_ESTADO_NUEVO], [MOTIVO], [ID_USUARIO], [ID_CORRELACION], [ID_USUARIO_ALTA])
    VALUES (N'ORDEN_COMPRA', @I_ID_ORDEN_COMPRA, N'PENDIENTE_APROBACION', N'APROBADA', N'Aprobación', @S_ID_USUARIO, NEWID(), @S_ID_USUARIO);
    COMMIT;
END;
