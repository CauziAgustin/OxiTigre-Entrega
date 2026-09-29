/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: SP_CATEGORIA_PRODUCTO_SAVE
Archivo: SP_CATEGORIA_PRODUCTO_SAVE.sql | Procedimiento: INVENTARIO.SP_CATEGORIA_PRODUCTO_SAVE | Tipo: COMMAND
Versión: 1.0.1 | Fecha: 2026-09-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Crea o modifica categorías de producto con concurrencia optimista.
Parámetros de entrada: @I_CODIGO, @I_CODIGO_ESTADO, @I_DESCRIPCION, @I_ID_CATEGORIA_PRODUCTO, @I_NOMBRE, @I_ROW_VERSION, @S_ID_USUARIO.
Parámetros de salida: @O_CODIGO_ERROR, @O_FILAS_AFECTADAS, @O_ID_CATEGORIA_PRODUCTO, @O_MENSAJE.
Retorno: Informa el resultado mediante los parámetros de salida declarados.
Tablas utilizadas: INVENTARIO.CATEGORIAS_PRODUCTO - INSERT/UPDATE; INVENTARIO.PRODUCTOS - SELECT.
Transacción: No abre una transacción explícita.
Auditoría: No registra auditoría explícita dentro del procedimiento.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.0.1 | 2026-09-20 | FABRICA | Agustin Omar Cauzi | Documentación técnica completa del procedimiento.
===============================================================================
*/
CREATE OR ALTER PROCEDURE [INVENTARIO].[SP_CATEGORIA_PRODUCTO_SAVE]
    @I_ID_CATEGORIA_PRODUCTO BIGINT = NULL, @I_CODIGO NVARCHAR(30), @I_NOMBRE NVARCHAR(150), @I_DESCRIPCION NVARCHAR(500) = NULL,
    @I_CODIGO_ESTADO NVARCHAR(30), @I_ROW_VERSION BINARY(8) = NULL, @S_ID_USUARIO BIGINT,
    @O_ID_CATEGORIA_PRODUCTO BIGINT OUTPUT, @O_CODIGO_ERROR BIGINT OUTPUT, @O_MENSAJE NVARCHAR(4000) OUTPUT, @O_FILAS_AFECTADAS INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET @O_CODIGO_ERROR = NULL; SET @O_MENSAJE = NULL; SET @O_FILAS_AFECTADAS = 0;
    IF NULLIF(LTRIM(RTRIM(@I_CODIGO)), N'') IS NULL OR NULLIF(LTRIM(RTRIM(@I_NOMBRE)), N'') IS NULL OR @I_CODIGO_ESTADO NOT IN (N'ACTIVO', N'INACTIVO') THROW 50000, 'Código, nombre y estado son obligatorios.', 1;
    IF @I_ID_CATEGORIA_PRODUCTO IS NULL
    BEGIN
        INSERT INTO [INVENTARIO].[CATEGORIAS_PRODUCTO] ([CODIGO], [NOMBRE], [DESCRIPCION], [CODIGO_ESTADO], [ID_USUARIO_ALTA]) VALUES (@I_CODIGO, @I_NOMBRE, @I_DESCRIPCION, @I_CODIGO_ESTADO, @S_ID_USUARIO);
        SET @O_ID_CATEGORIA_PRODUCTO = CONVERT(BIGINT, SCOPE_IDENTITY()); SET @O_FILAS_AFECTADAS = 1; RETURN;
    END;
    IF @I_CODIGO_ESTADO = N'INACTIVO' AND EXISTS (SELECT 1 FROM [INVENTARIO].[PRODUCTOS] WHERE [ID_CATEGORIA_PRODUCTO] = @I_ID_CATEGORIA_PRODUCTO AND [CODIGO_ESTADO] = N'ACTIVO')
        THROW 50001, 'No se puede inactivar una categoría utilizada por productos activos.', 1;
    UPDATE [INVENTARIO].[CATEGORIAS_PRODUCTO] SET [NOMBRE] = @I_NOMBRE, [DESCRIPCION] = @I_DESCRIPCION, [CODIGO_ESTADO] = @I_CODIGO_ESTADO, [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(), [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO WHERE [ID_CATEGORIA_PRODUCTO] = @I_ID_CATEGORIA_PRODUCTO AND [ROW_VERSION] = @I_ROW_VERSION;
    SET @O_FILAS_AFECTADAS = @@ROWCOUNT; SET @O_ID_CATEGORIA_PRODUCTO = @I_ID_CATEGORIA_PRODUCTO;
    IF @O_FILAS_AFECTADAS = 0 BEGIN SET @O_CODIGO_ERROR = 30003; SET @O_MENSAJE = N'La categoría fue modificada por otro usuario o ya no existe.'; END;
END;
