/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: SP_EXISTENCIA_MINIMO_UPDATE
Archivo: SP_EXISTENCIA_MINIMO_UPDATE.sql | Procedimiento: INVENTARIO.SP_EXISTENCIA_MINIMO_UPDATE | Tipo: COMMAND
Versión: 1.0.1 | Fecha: 2026-09-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Actualiza el mínimo de alerta sin alterar el saldo físico.
Parámetros de entrada: @I_ID_EMPRESA, @I_ID_EXISTENCIA, @I_ROW_VERSION, @I_STOCK_MINIMO, @S_ID_USUARIO.
Parámetros de salida: @O_CODIGO_ERROR, @O_FILAS_AFECTADAS, @O_MENSAJE.
Retorno: Informa el resultado mediante los parámetros de salida declarados.
Tablas utilizadas: INVENTARIO.EXISTENCIAS - UPDATE.
Transacción: No abre una transacción explícita.
Auditoría: No registra auditoría explícita dentro del procedimiento.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.0.1 | 2026-09-20 | FABRICA | Agustin Omar Cauzi | Documentación técnica completa del procedimiento.
===============================================================================
*/
CREATE OR ALTER PROCEDURE [INVENTARIO].[SP_EXISTENCIA_MINIMO_UPDATE]
    @I_ID_EXISTENCIA BIGINT, @I_ID_EMPRESA BIGINT, @I_STOCK_MINIMO DECIMAL(19,4), @I_ROW_VERSION BINARY(8), @S_ID_USUARIO BIGINT,
    @O_CODIGO_ERROR BIGINT OUTPUT, @O_MENSAJE NVARCHAR(4000) OUTPUT, @O_FILAS_AFECTADAS INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET @O_CODIGO_ERROR = NULL; SET @O_MENSAJE = NULL; SET @O_FILAS_AFECTADAS = 0;
    IF @I_STOCK_MINIMO < 0 THROW 50000, 'El stock mínimo no puede ser negativo.', 1;
    UPDATE [INVENTARIO].[EXISTENCIAS] SET [STOCK_MINIMO] = @I_STOCK_MINIMO, [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(), [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO WHERE [ID_EXISTENCIA] = @I_ID_EXISTENCIA AND [ID_EMPRESA] = @I_ID_EMPRESA AND [ROW_VERSION] = @I_ROW_VERSION;
    SET @O_FILAS_AFECTADAS = @@ROWCOUNT;
    IF @O_FILAS_AFECTADAS = 0 BEGIN SET @O_CODIGO_ERROR = 30003; SET @O_MENSAJE = N'La existencia fue modificada por otro usuario o no pertenece a la empresa.'; END;
END;
