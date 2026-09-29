/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: SP_UNIDAD_MEDIDA_SAVE
Archivo: SP_UNIDAD_MEDIDA_SAVE.sql | Procedimiento: INVENTARIO.SP_UNIDAD_MEDIDA_SAVE | Tipo: COMMAND
Versión: 1.0.1 | Fecha: 2026-09-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Crea o modifica una unidad de medida con concurrencia optimista.
Parámetros de entrada: @I_CODIGO, @I_CODIGO_ESTADO, @I_ID_UNIDAD_MEDIDA, @I_NOMBRE, @I_PERMITE_DECIMALES, @I_ROW_VERSION, @I_SIMBOLO, @S_ID_USUARIO.
Parámetros de salida: @O_CODIGO_ERROR, @O_FILAS_AFECTADAS, @O_ID_UNIDAD_MEDIDA, @O_MENSAJE.
Retorno: Informa el resultado mediante los parámetros de salida declarados.
Tablas utilizadas: CONFIGURACION.UNIDADES_MEDIDA - INSERT/UPDATE; INVENTARIO.PRODUCTOS - SELECT.
Transacción: No abre una transacción explícita.
Auditoría: No registra auditoría explícita dentro del procedimiento.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.0.1 | 2026-09-20 | FABRICA | Agustin Omar Cauzi | Documentación técnica completa del procedimiento.
===============================================================================
*/
CREATE OR ALTER PROCEDURE [INVENTARIO].[SP_UNIDAD_MEDIDA_SAVE]
    @I_ID_UNIDAD_MEDIDA BIGINT = NULL, @I_CODIGO NVARCHAR(30), @I_NOMBRE NVARCHAR(100), @I_SIMBOLO NVARCHAR(20),
    @I_PERMITE_DECIMALES BIT, @I_CODIGO_ESTADO NVARCHAR(30), @I_ROW_VERSION BINARY(8) = NULL, @S_ID_USUARIO BIGINT,
    @O_ID_UNIDAD_MEDIDA BIGINT OUTPUT, @O_CODIGO_ERROR BIGINT OUTPUT, @O_MENSAJE NVARCHAR(4000) OUTPUT, @O_FILAS_AFECTADAS INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET @O_CODIGO_ERROR = NULL; SET @O_MENSAJE = NULL; SET @O_FILAS_AFECTADAS = 0;
    IF NULLIF(LTRIM(RTRIM(@I_CODIGO)), N'') IS NULL OR NULLIF(LTRIM(RTRIM(@I_NOMBRE)), N'') IS NULL OR NULLIF(LTRIM(RTRIM(@I_SIMBOLO)), N'') IS NULL OR @I_CODIGO_ESTADO NOT IN (N'ACTIVO', N'INACTIVO') THROW 50000, 'Código, nombre, símbolo y estado son obligatorios.', 1;
    IF @I_ID_UNIDAD_MEDIDA IS NULL
    BEGIN
        INSERT INTO [CONFIGURACION].[UNIDADES_MEDIDA] ([CODIGO], [NOMBRE], [SIMBOLO], [PERMITE_DECIMALES], [CODIGO_ESTADO], [ID_USUARIO_ALTA]) VALUES (@I_CODIGO, @I_NOMBRE, @I_SIMBOLO, @I_PERMITE_DECIMALES, @I_CODIGO_ESTADO, @S_ID_USUARIO);
        SET @O_ID_UNIDAD_MEDIDA = CONVERT(BIGINT, SCOPE_IDENTITY()); SET @O_FILAS_AFECTADAS = 1; RETURN;
    END;
    IF @I_CODIGO_ESTADO = N'INACTIVO' AND EXISTS (SELECT 1 FROM [INVENTARIO].[PRODUCTOS] WHERE [ID_UNIDAD_MEDIDA] = @I_ID_UNIDAD_MEDIDA AND [CODIGO_ESTADO] = N'ACTIVO')
        THROW 50001, 'No se puede inactivar una unidad utilizada por productos activos.', 1;
    UPDATE [CONFIGURACION].[UNIDADES_MEDIDA] SET [NOMBRE] = @I_NOMBRE, [SIMBOLO] = @I_SIMBOLO, [PERMITE_DECIMALES] = @I_PERMITE_DECIMALES, [CODIGO_ESTADO] = @I_CODIGO_ESTADO, [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(), [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO WHERE [ID_UNIDAD_MEDIDA] = @I_ID_UNIDAD_MEDIDA AND [ROW_VERSION] = @I_ROW_VERSION;
    SET @O_FILAS_AFECTADAS = @@ROWCOUNT; SET @O_ID_UNIDAD_MEDIDA = @I_ID_UNIDAD_MEDIDA;
    IF @O_FILAS_AFECTADAS = 0 BEGIN SET @O_CODIGO_ERROR = 30003; SET @O_MENSAJE = N'La unidad fue modificada por otro usuario o ya no existe.'; END;
END;
