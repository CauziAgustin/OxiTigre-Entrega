/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: SP_USUARIO_UPDATE
Archivo: SP_USUARIO_UPDATE.sql
Procedimiento: SEGURIDAD.SP_USUARIO_UPDATE
Tipo: COMMAND
Versión: 1.1.0
Fecha: 2026-08-20
ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi
Correo: agustincauzi10@hotmail.com
Descripción funcional: Actualiza datos, estado y roles activos de un usuario de la empresa.
Parámetros de entrada: Empresa, usuario, datos funcionales, estado y roles JSON.
Parámetros de sesión: @S_ID_USUARIO identifica al administrador responsable.
Parámetros de salida: Código, mensaje y filas afectadas.
Retorno: No retorna filas.
Tablas utilizadas: USUARIOS - UPDATE; ROLES - READ; USUARIOS_ROLES y SESIONES - INSERT/UPDATE.
Transacción: Modificación atómica con rollback automático y controlado.
Auditoría: La DAL registra la funcionalidad y el administrador responsable.
Historial de modificaciones:
1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
1.1.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Garantiza vigencia positiva al reemplazar roles recién creados.
===============================================================================
*/
CREATE OR ALTER PROCEDURE [SEGURIDAD].[SP_USUARIO_UPDATE]
    @I_ID_EMPRESA      BIGINT,
    @I_ID_USUARIO      BIGINT,
    @I_NOMBRES         NVARCHAR(150),
    @I_APELLIDO        NVARCHAR(150),
    @I_EMAIL           NVARCHAR(254),
    @I_CODIGO_ESTADO   NVARCHAR(30),
    @I_ROLES_JSON      NVARCHAR(MAX),
    @S_ID_USUARIO      BIGINT,
    @O_CODIGO_ERROR    BIGINT OUTPUT,
    @O_MENSAJE         NVARCHAR(4000) OUTPUT,
    @O_FILAS_AFECTADAS INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @O_CODIGO_ERROR = NULL;
    SET @O_MENSAJE = NULL;
    SET @O_FILAS_AFECTADAS = 0;

    IF @I_ID_EMPRESA IS NULL OR @I_ID_USUARIO IS NULL OR @S_ID_USUARIO IS NULL
       OR NULLIF(LTRIM(RTRIM(@I_NOMBRES)), N'') IS NULL
       OR NULLIF(LTRIM(RTRIM(@I_APELLIDO)), N'') IS NULL
       OR NULLIF(LTRIM(RTRIM(@I_EMAIL)), N'') IS NULL
       OR @I_CODIGO_ESTADO NOT IN (N'ACTIVO', N'INACTIVO') OR ISJSON(@I_ROLES_JSON) <> 1
        THROW 50000, 'Los datos, estado y roles del usuario son obligatorios.', 1;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- INICIO: Validación de aislamiento, estado y roles solicitados.
        IF NOT EXISTS (SELECT 1 FROM [SEGURIDAD].[USUARIOS] WHERE [ID_USUARIO] = @I_ID_USUARIO AND [ID_EMPRESA] = @I_ID_EMPRESA)
            THROW 50001, 'El usuario no pertenece a la empresa.', 1;
        IF NOT EXISTS
        (
            SELECT 1 FROM [CONFIGURACION].[ESTADOS]
            WHERE [ENTIDAD] = N'USUARIOS' AND [CODIGO_ESTADO] = @I_CODIGO_ESTADO
        ) THROW 50002, 'El estado solicitado no está configurado para usuarios.', 1;

        DECLARE @V_ROLES TABLE ([CODIGO] NVARCHAR(50) PRIMARY KEY);
        INSERT INTO @V_ROLES ([CODIGO])
        SELECT DISTINCT UPPER(LTRIM(RTRIM([value]))) FROM OPENJSON(@I_ROLES_JSON)
        WHERE [type] = 1 AND NULLIF(LTRIM(RTRIM([value])), N'') IS NOT NULL;
        IF NOT EXISTS (SELECT 1 FROM @V_ROLES) THROW 50003, 'Debe asignarse al menos un rol.', 1;
        IF EXISTS
        (
            SELECT 1 FROM @V_ROLES AS [SOL]
            WHERE NOT EXISTS
            (
                SELECT 1 FROM [SEGURIDAD].[ROLES] AS [ROL]
                WHERE [ROL].[ID_EMPRESA] = @I_ID_EMPRESA AND [ROL].[CODIGO] = [SOL].[CODIGO]
                  AND [ROL].[CODIGO_ESTADO] = N'ACTIVO'
            )
        ) THROW 50004, 'Uno o más roles no existen o no están activos.', 1;
        -- FIN: Validación de aislamiento, estado y roles solicitados.

        -- INICIO: Actualización de los datos funcionales.
        UPDATE [SEGURIDAD].[USUARIOS]
        SET [NOMBRES] = LTRIM(RTRIM(@I_NOMBRES)), [APELLIDO] = LTRIM(RTRIM(@I_APELLIDO)),
            [EMAIL] = LTRIM(RTRIM(@I_EMAIL)), [CODIGO_ESTADO] = @I_CODIGO_ESTADO,
            [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(), [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO
        WHERE [ID_USUARIO] = @I_ID_USUARIO AND [ID_EMPRESA] = @I_ID_EMPRESA;
        SET @O_FILAS_AFECTADAS = @@ROWCOUNT;
        -- FIN: Actualización de los datos funcionales.

        -- INICIO: Reemplazo de roles activos conservando el historial.
        DECLARE @V_FECHA_CAMBIO_ROL DATETIME2(3) = SYSUTCDATETIME();
        UPDATE [UR]
        SET [CODIGO_ESTADO] = N'INACTIVO', [FECHA_VIGENCIA_HASTA_UTC] = CASE WHEN @V_FECHA_CAMBIO_ROL <= [UR].[FECHA_VIGENCIA_DESDE_UTC] THEN DATEADD(MILLISECOND, 1, [UR].[FECHA_VIGENCIA_DESDE_UTC]) ELSE @V_FECHA_CAMBIO_ROL END,
            [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(), [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO
        FROM [SEGURIDAD].[USUARIOS_ROLES] AS [UR]
        INNER JOIN [SEGURIDAD].[ROLES] AS [ROL] ON [ROL].[ID_ROL] = [UR].[ID_ROL]
        WHERE [UR].[ID_USUARIO] = @I_ID_USUARIO AND [UR].[CODIGO_ESTADO] = N'ACTIVO'
          AND NOT EXISTS (SELECT 1 FROM @V_ROLES AS [SOL] WHERE [SOL].[CODIGO] = [ROL].[CODIGO]);
        SET @O_FILAS_AFECTADAS += @@ROWCOUNT;

        INSERT INTO [SEGURIDAD].[USUARIOS_ROLES]
            ([ID_USUARIO], [ID_ROL], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
        SELECT @I_ID_USUARIO, [ROL].[ID_ROL], N'ACTIVO', @S_ID_USUARIO
        FROM [SEGURIDAD].[ROLES] AS [ROL]
        INNER JOIN @V_ROLES AS [SOL] ON [SOL].[CODIGO] = [ROL].[CODIGO]
        WHERE [ROL].[ID_EMPRESA] = @I_ID_EMPRESA
          AND NOT EXISTS
          (
              SELECT 1 FROM [SEGURIDAD].[USUARIOS_ROLES] AS [UR]
              WHERE [UR].[ID_USUARIO] = @I_ID_USUARIO AND [UR].[ID_ROL] = [ROL].[ID_ROL]
                AND [UR].[CODIGO_ESTADO] = N'ACTIVO'
          );
        SET @O_FILAS_AFECTADAS += @@ROWCOUNT;
        -- FIN: Reemplazo de roles activos conservando el historial.

        -- INICIO: Revocación de sesiones al desactivar el usuario.
        IF @I_CODIGO_ESTADO = N'INACTIVO'
        BEGIN
            UPDATE [SEGURIDAD].[SESIONES]
            SET [CODIGO_ESTADO] = N'REVOCADA', [FECHA_CIERRE_UTC] = SYSUTCDATETIME(),
                [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(), [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO
            WHERE [ID_USUARIO] = @I_ID_USUARIO AND [CODIGO_ESTADO] = N'VIGENTE';
            SET @O_FILAS_AFECTADAS += @@ROWCOUNT;
        END;
        -- FIN: Revocación de sesiones al desactivar el usuario.

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        SET @O_MENSAJE = ERROR_MESSAGE();
        THROW;
    END CATCH;
END;
