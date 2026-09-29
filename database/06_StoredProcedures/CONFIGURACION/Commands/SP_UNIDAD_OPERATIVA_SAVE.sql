/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: SP_UNIDAD_OPERATIVA_SAVE
Procedimiento: CONFIGURACION.SP_UNIDAD_OPERATIVA_SAVE
Tipo:                  COMMAND
Archivo: SP_UNIDAD_OPERATIVA_SAVE.sql | Versión: 1.0.1 | Fecha: 2026-09-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Crea o actualiza una unidad dentro de una sucursal de la empresa autenticada.
Parámetros de entrada: Identificador opcional, empresa, sucursal, datos, estado y versión.
Parámetros de sesión: Usuario responsable.
Parámetros de salida: Identificador, código, mensaje y filas afectadas.
Retorno: No retorna filas.
Tablas utilizadas: CONFIGURACION.SUCURSALES y UNIDADES_OPERATIVAS.
Transacción: Escritura atómica con concurrencia optimista.
Auditoría: La DAL registra el alta o modificación.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.0.1 | 2026-09-20 | FABRICA | Agustin Omar Cauzi | Documentación técnica completa del procedimiento.
===============================================================================
*/
CREATE OR ALTER PROCEDURE [CONFIGURACION].[SP_UNIDAD_OPERATIVA_SAVE]
    @I_ID_UNIDAD_OPERATIVA BIGINT = NULL,
    @I_ID_EMPRESA BIGINT,
    @I_ID_SUCURSAL BIGINT,
    @I_CODIGO NVARCHAR(30),
    @I_NOMBRE NVARCHAR(200),
    @I_DESCRIPCION NVARCHAR(500) = NULL,
    @I_CODIGO_ESTADO NVARCHAR(30),
    @I_ROW_VERSION BINARY(8) = NULL,
    @S_ID_USUARIO BIGINT,
    @O_ID_UNIDAD_OPERATIVA BIGINT OUTPUT,
    @O_CODIGO_ERROR BIGINT OUTPUT,
    @O_MENSAJE NVARCHAR(4000) OUTPUT,
    @O_FILAS_AFECTADAS INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @O_ID_UNIDAD_OPERATIVA = @I_ID_UNIDAD_OPERATIVA; SET @O_CODIGO_ERROR = NULL; SET @O_MENSAJE = NULL; SET @O_FILAS_AFECTADAS = 0;

    IF NULLIF(LTRIM(RTRIM(@I_CODIGO)), N'') IS NULL OR NULLIF(LTRIM(RTRIM(@I_NOMBRE)), N'') IS NULL
       OR @I_CODIGO_ESTADO NOT IN (N'ACTIVO', N'INACTIVO') OR (@I_ID_UNIDAD_OPERATIVA IS NOT NULL AND @I_ROW_VERSION IS NULL)
       OR NOT EXISTS (SELECT 1 FROM [CONFIGURACION].[SUCURSALES] WHERE [ID_SUCURSAL] = @I_ID_SUCURSAL AND [ID_EMPRESA] = @I_ID_EMPRESA)
    BEGIN
        SET @O_CODIGO_ERROR = 20001;
        EXEC [AUDITORIA].[SP_ERROR_MESSAGE_RESOLVE] @I_CODIGO_ERROR = @O_CODIGO_ERROR,
            @I_MENSAJE_PERSONALIZADO = N'La sucursal, código, nombre, estado y versión de edición no son válidos.', @O_MENSAJE = @O_MENSAJE OUTPUT;
        RETURN;
    END;

    IF EXISTS
    (
        SELECT 1 FROM [CONFIGURACION].[UNIDADES_OPERATIVAS]
        WHERE [ID_SUCURSAL] = @I_ID_SUCURSAL AND [CODIGO] = UPPER(LTRIM(RTRIM(@I_CODIGO)))
          AND [ID_UNIDAD_OPERATIVA] <> COALESCE(@I_ID_UNIDAD_OPERATIVA, -1)
    )
    BEGIN
        SET @O_CODIGO_ERROR = 20001;
        EXEC [AUDITORIA].[SP_ERROR_MESSAGE_RESOLVE] @I_CODIGO_ERROR = @O_CODIGO_ERROR,
            @I_MENSAJE_PERSONALIZADO = N'Ya existe una unidad con ese código en la sucursal.', @O_MENSAJE = @O_MENSAJE OUTPUT;
        RETURN;
    END;

    BEGIN TRY
        IF @I_ID_UNIDAD_OPERATIVA IS NULL
        BEGIN
            INSERT INTO [CONFIGURACION].[UNIDADES_OPERATIVAS]
                ([ID_SUCURSAL], [CODIGO], [NOMBRE], [DESCRIPCION], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
            VALUES (@I_ID_SUCURSAL, UPPER(LTRIM(RTRIM(@I_CODIGO))), LTRIM(RTRIM(@I_NOMBRE)),
                    NULLIF(LTRIM(RTRIM(@I_DESCRIPCION)), N''), @I_CODIGO_ESTADO, @S_ID_USUARIO);
            SET @O_FILAS_AFECTADAS = @@ROWCOUNT; SET @O_ID_UNIDAD_OPERATIVA = SCOPE_IDENTITY();
        END
        ELSE
        BEGIN
            UPDATE [U]
            SET [U].[NOMBRE] = LTRIM(RTRIM(@I_NOMBRE)), [U].[DESCRIPCION] = NULLIF(LTRIM(RTRIM(@I_DESCRIPCION)), N''),
                [U].[CODIGO_ESTADO] = @I_CODIGO_ESTADO, [U].[FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(),
                [U].[ID_USUARIO_MODIFICACION] = @S_ID_USUARIO
            FROM [CONFIGURACION].[UNIDADES_OPERATIVAS] AS [U]
            INNER JOIN [CONFIGURACION].[SUCURSALES] AS [S] ON [S].[ID_SUCURSAL] = [U].[ID_SUCURSAL]
            WHERE [U].[ID_UNIDAD_OPERATIVA] = @I_ID_UNIDAD_OPERATIVA AND [U].[ID_SUCURSAL] = @I_ID_SUCURSAL
              AND [S].[ID_EMPRESA] = @I_ID_EMPRESA AND [U].[ROW_VERSION] = @I_ROW_VERSION;
            SET @O_FILAS_AFECTADAS = @@ROWCOUNT;
            IF @O_FILAS_AFECTADAS = 0
            BEGIN
                SET @O_CODIGO_ERROR = CASE WHEN EXISTS
                (SELECT 1 FROM [CONFIGURACION].[UNIDADES_OPERATIVAS] AS [U] INNER JOIN [CONFIGURACION].[SUCURSALES] AS [S] ON [S].[ID_SUCURSAL] = [U].[ID_SUCURSAL]
                 WHERE [U].[ID_UNIDAD_OPERATIVA] = @I_ID_UNIDAD_OPERATIVA AND [S].[ID_EMPRESA] = @I_ID_EMPRESA) THEN 20003 ELSE 20002 END;
                EXEC [AUDITORIA].[SP_ERROR_MESSAGE_RESOLVE] @I_CODIGO_ERROR = @O_CODIGO_ERROR,
                    @I_MENSAJE_PERSONALIZADO = NULL, @O_MENSAJE = @O_MENSAJE OUTPUT;
            END;
        END;
    END TRY
    BEGIN CATCH
        SET @O_CODIGO_ERROR = 70002; SET @O_MENSAJE = ERROR_MESSAGE(); THROW;
    END CATCH;
END;
