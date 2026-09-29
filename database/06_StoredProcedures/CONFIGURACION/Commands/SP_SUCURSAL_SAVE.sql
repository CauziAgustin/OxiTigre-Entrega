/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: SP_SUCURSAL_SAVE
Procedimiento: CONFIGURACION.SP_SUCURSAL_SAVE
Tipo:                  COMMAND
Archivo: SP_SUCURSAL_SAVE.sql | Versión: 1.0.1 | Fecha: 2026-09-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Crea o actualiza una sucursal aislada por empresa y sin eliminación física.
Parámetros de entrada: Identificador opcional, empresa, datos, estado y versión para edición.
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
CREATE OR ALTER PROCEDURE [CONFIGURACION].[SP_SUCURSAL_SAVE]
    @I_ID_SUCURSAL BIGINT = NULL,
    @I_ID_EMPRESA BIGINT,
    @I_CODIGO NVARCHAR(30),
    @I_NOMBRE NVARCHAR(200),
    @I_DOMICILIO NVARCHAR(250) = NULL,
    @I_LOCALIDAD NVARCHAR(150) = NULL,
    @I_PROVINCIA NVARCHAR(150) = NULL,
    @I_CODIGO_POSTAL NVARCHAR(20) = NULL,
    @I_CODIGO_ESTADO NVARCHAR(30),
    @I_ROW_VERSION BINARY(8) = NULL,
    @S_ID_USUARIO BIGINT,
    @O_ID_SUCURSAL BIGINT OUTPUT,
    @O_CODIGO_ERROR BIGINT OUTPUT,
    @O_MENSAJE NVARCHAR(4000) OUTPUT,
    @O_FILAS_AFECTADAS INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @O_ID_SUCURSAL = @I_ID_SUCURSAL; SET @O_CODIGO_ERROR = NULL; SET @O_MENSAJE = NULL; SET @O_FILAS_AFECTADAS = 0;

    IF NULLIF(LTRIM(RTRIM(@I_CODIGO)), N'') IS NULL OR NULLIF(LTRIM(RTRIM(@I_NOMBRE)), N'') IS NULL
       OR @I_CODIGO_ESTADO NOT IN (N'ACTIVO', N'INACTIVO') OR (@I_ID_SUCURSAL IS NOT NULL AND @I_ROW_VERSION IS NULL)
    BEGIN
        SET @O_CODIGO_ERROR = 20001;
        EXEC [AUDITORIA].[SP_ERROR_MESSAGE_RESOLVE] @I_CODIGO_ERROR = @O_CODIGO_ERROR,
            @I_MENSAJE_PERSONALIZADO = N'Código, nombre, estado y versión de edición son obligatorios.', @O_MENSAJE = @O_MENSAJE OUTPUT;
        RETURN;
    END;

    IF @I_CODIGO_ESTADO = N'INACTIVO' AND EXISTS
    (
        SELECT 1 FROM [CONFIGURACION].[UNIDADES_OPERATIVAS]
        WHERE [ID_SUCURSAL] = @I_ID_SUCURSAL AND [CODIGO_ESTADO] = N'ACTIVO'
    )
    BEGIN
        SET @O_CODIGO_ERROR = 20004;
        EXEC [AUDITORIA].[SP_ERROR_MESSAGE_RESOLVE] @I_CODIGO_ERROR = @O_CODIGO_ERROR,
            @I_MENSAJE_PERSONALIZADO = N'No se puede inactivar la sucursal mientras tenga unidades operativas activas.', @O_MENSAJE = @O_MENSAJE OUTPUT;
        RETURN;
    END;

    IF EXISTS
    (
        SELECT 1 FROM [CONFIGURACION].[SUCURSALES]
        WHERE [ID_EMPRESA] = @I_ID_EMPRESA AND [CODIGO] = UPPER(LTRIM(RTRIM(@I_CODIGO)))
          AND [ID_SUCURSAL] <> COALESCE(@I_ID_SUCURSAL, -1)
    )
    BEGIN
        SET @O_CODIGO_ERROR = 20001;
        EXEC [AUDITORIA].[SP_ERROR_MESSAGE_RESOLVE] @I_CODIGO_ERROR = @O_CODIGO_ERROR,
            @I_MENSAJE_PERSONALIZADO = N'Ya existe una sucursal con ese código en la empresa.', @O_MENSAJE = @O_MENSAJE OUTPUT;
        RETURN;
    END;

    BEGIN TRY
        IF @I_ID_SUCURSAL IS NULL
        BEGIN
            INSERT INTO [CONFIGURACION].[SUCURSALES]
                ([ID_EMPRESA], [CODIGO], [NOMBRE], [DOMICILIO], [LOCALIDAD], [PROVINCIA], [CODIGO_POSTAL], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
            VALUES
                (@I_ID_EMPRESA, UPPER(LTRIM(RTRIM(@I_CODIGO))), LTRIM(RTRIM(@I_NOMBRE)), NULLIF(LTRIM(RTRIM(@I_DOMICILIO)), N''),
                 NULLIF(LTRIM(RTRIM(@I_LOCALIDAD)), N''), NULLIF(LTRIM(RTRIM(@I_PROVINCIA)), N''),
                 NULLIF(LTRIM(RTRIM(@I_CODIGO_POSTAL)), N''), @I_CODIGO_ESTADO, @S_ID_USUARIO);
            SET @O_FILAS_AFECTADAS = @@ROWCOUNT; SET @O_ID_SUCURSAL = SCOPE_IDENTITY();
        END
        ELSE
        BEGIN
            UPDATE [CONFIGURACION].[SUCURSALES]
            SET [NOMBRE] = LTRIM(RTRIM(@I_NOMBRE)), [DOMICILIO] = NULLIF(LTRIM(RTRIM(@I_DOMICILIO)), N''),
                [LOCALIDAD] = NULLIF(LTRIM(RTRIM(@I_LOCALIDAD)), N''), [PROVINCIA] = NULLIF(LTRIM(RTRIM(@I_PROVINCIA)), N''),
                [CODIGO_POSTAL] = NULLIF(LTRIM(RTRIM(@I_CODIGO_POSTAL)), N''), [CODIGO_ESTADO] = @I_CODIGO_ESTADO,
                [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(), [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO
            WHERE [ID_SUCURSAL] = @I_ID_SUCURSAL AND [ID_EMPRESA] = @I_ID_EMPRESA AND [ROW_VERSION] = @I_ROW_VERSION;
            SET @O_FILAS_AFECTADAS = @@ROWCOUNT;
            IF @O_FILAS_AFECTADAS = 0
            BEGIN
                SET @O_CODIGO_ERROR = CASE WHEN EXISTS (SELECT 1 FROM [CONFIGURACION].[SUCURSALES] WHERE [ID_SUCURSAL] = @I_ID_SUCURSAL AND [ID_EMPRESA] = @I_ID_EMPRESA) THEN 20003 ELSE 20002 END;
                EXEC [AUDITORIA].[SP_ERROR_MESSAGE_RESOLVE] @I_CODIGO_ERROR = @O_CODIGO_ERROR,
                    @I_MENSAJE_PERSONALIZADO = NULL, @O_MENSAJE = @O_MENSAJE OUTPUT;
            END;
        END;
    END TRY
    BEGIN CATCH
        SET @O_CODIGO_ERROR = 70002; SET @O_MENSAJE = ERROR_MESSAGE(); THROW;
    END CATCH;
END;
