/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: SP_USUARIO_CREATE
Archivo: SP_USUARIO_CREATE.sql
Procedimiento: SEGURIDAD.SP_USUARIO_CREATE
Tipo: COMMAND
Versión: 2.0.0
Fecha: 2026-08-20
ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi
Correo: agustincauzi10@hotmail.com
Descripción funcional: Crea un usuario funcional y le asigna uno o más roles dentro de la misma empresa.
Parámetros de entrada: Identidad, credencial derivada y arreglo JSON de códigos de rol.
Parámetros de sesión: @S_ID_USUARIO identifica al administrador responsable.
Parámetros de salida: Usuario creado, código, mensaje y filas afectadas.
Retorno: No retorna filas.
Tablas utilizadas: SEGURIDAD.USUARIOS - INSERT; SEGURIDAD.ROLES - READ; SEGURIDAD.USUARIOS_ROLES - INSERT.
Transacción: Alta de usuario y rol atómica con rollback automático y controlado.
Auditoría: La DAL registra la funcionalidad sin persistir hash ni salt en el detalle.
Historial de modificaciones:
1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
2.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Soporte atómico de múltiples roles.
===============================================================================
*/
CREATE OR ALTER PROCEDURE [SEGURIDAD].[SP_USUARIO_CREATE]
    @I_ID_EMPRESA         BIGINT,
    @I_NOMBRE_USUARIO     NVARCHAR(100),
    @I_NOMBRES            NVARCHAR(150),
    @I_APELLIDO           NVARCHAR(150),
    @I_EMAIL              NVARCHAR(254),
    @I_ROLES_JSON         NVARCHAR(MAX),
    @I_HASH_CLAVE         VARBINARY(64),
    @I_SALT_CLAVE         VARBINARY(32),
    @I_ALGORITMO_CLAVE    NVARCHAR(30),
    @I_ITERACIONES_CLAVE  INT,
    @S_ID_USUARIO         BIGINT,
    @O_ID_USUARIO         BIGINT OUTPUT,
    @O_CODIGO_ERROR       BIGINT OUTPUT,
    @O_MENSAJE            NVARCHAR(4000) OUTPUT,
    @O_FILAS_AFECTADAS    INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @O_ID_USUARIO = NULL;
    SET @O_CODIGO_ERROR = NULL;
    SET @O_MENSAJE = NULL;
    SET @O_FILAS_AFECTADAS = 0;

    IF @I_ID_EMPRESA IS NULL OR @S_ID_USUARIO IS NULL
       OR NULLIF(LTRIM(RTRIM(@I_NOMBRE_USUARIO)), N'') IS NULL
       OR NULLIF(LTRIM(RTRIM(@I_NOMBRES)), N'') IS NULL
       OR NULLIF(LTRIM(RTRIM(@I_APELLIDO)), N'') IS NULL
       OR NULLIF(LTRIM(RTRIM(@I_EMAIL)), N'') IS NULL
       OR ISJSON(@I_ROLES_JSON) <> 1
       OR @I_HASH_CLAVE IS NULL OR @I_SALT_CLAVE IS NULL OR @I_ITERACIONES_CLAVE < 100000
    BEGIN
        THROW 50000, 'Los datos del usuario, rol y credencial son obligatorios.', 1;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @V_ROLES TABLE ([CODIGO] NVARCHAR(50) PRIMARY KEY);
        INSERT INTO @V_ROLES ([CODIGO])
        SELECT DISTINCT UPPER(LTRIM(RTRIM([value]))) FROM OPENJSON(@I_ROLES_JSON)
        WHERE [type] = 1 AND NULLIF(LTRIM(RTRIM([value])), N'') IS NOT NULL;

        IF NOT EXISTS (SELECT 1 FROM @V_ROLES)
            THROW 50001, 'Debe asignarse al menos un rol.', 1;
        IF EXISTS
        (
            SELECT 1 FROM @V_ROLES AS [SOL]
            WHERE NOT EXISTS
            (
                SELECT 1 FROM [SEGURIDAD].[ROLES] AS [ROL]
                WHERE [ROL].[ID_EMPRESA] = @I_ID_EMPRESA AND [ROL].[CODIGO] = [SOL].[CODIGO]
                  AND [ROL].[CODIGO_ESTADO] = N'ACTIVO'
            )
        ) THROW 50002, 'Uno o más roles no existen o no están activos.', 1;

        INSERT INTO [SEGURIDAD].[USUARIOS]
            ([ID_EMPRESA], [NOMBRE_USUARIO], [NOMBRES], [APELLIDO], [EMAIL], [HASH_CLAVE],
             [SALT_CLAVE], [ALGORITMO_CLAVE], [ITERACIONES_CLAVE], [DEBE_CAMBIAR_CLAVE],
             [CODIGO_ESTADO], [ID_USUARIO_ALTA])
        VALUES
            (@I_ID_EMPRESA, @I_NOMBRE_USUARIO, @I_NOMBRES, @I_APELLIDO, @I_EMAIL, @I_HASH_CLAVE,
             @I_SALT_CLAVE, @I_ALGORITMO_CLAVE, @I_ITERACIONES_CLAVE, 1, N'ACTIVO', @S_ID_USUARIO);

        SET @O_ID_USUARIO = CONVERT(BIGINT, SCOPE_IDENTITY());

        INSERT INTO [SEGURIDAD].[USUARIOS_ROLES]
            ([ID_USUARIO], [ID_ROL], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
        SELECT @O_ID_USUARIO, [ROL].[ID_ROL], N'ACTIVO', @S_ID_USUARIO
        FROM [SEGURIDAD].[ROLES] AS [ROL]
        INNER JOIN @V_ROLES AS [SOL] ON [SOL].[CODIGO] = [ROL].[CODIGO]
        WHERE [ROL].[ID_EMPRESA] = @I_ID_EMPRESA;

        SET @O_FILAS_AFECTADAS = 1 + @@ROWCOUNT;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        SET @O_MENSAJE = ERROR_MESSAGE();
        THROW;
    END CATCH;
END;
