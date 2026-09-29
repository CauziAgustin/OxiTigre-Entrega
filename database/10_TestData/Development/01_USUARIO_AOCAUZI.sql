/*
===============================================================================
Proyecto:              Sistema Modular de Gestión OxiTigre
Componente:            Usuario administrador de Development
Archivo:               01_USUARIO_AOCAUZI.sql
Versión:               1.0.0
Fecha:                 2026-08-19
ID pedido:             FABRICA
Desarrollador:         Agustin Omar Cauzi
Correo:                agustincauzi10@hotmail.com
Descripción funcional: Crea AOCAUZI exclusivamente para pruebas locales iniciales.
Historial de modificaciones:
1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
DECLARE @V_ID_EMPRESA BIGINT = (SELECT [ID_EMPRESA] FROM [CONFIGURACION].[EMPRESAS] WHERE [CODIGO] = N'OXITIGRE');

IF NOT EXISTS
(
    SELECT 1 FROM [SEGURIDAD].[USUARIOS]
    WHERE [ID_EMPRESA] = @V_ID_EMPRESA AND [NOMBRE_USUARIO] = N'AOCAUZI'
)
BEGIN
    INSERT INTO [SEGURIDAD].[USUARIOS]
    (
        [ID_EMPRESA], [NOMBRE_USUARIO], [NOMBRES], [APELLIDO], [EMAIL],
        [HASH_CLAVE], [SALT_CLAVE], [ALGORITMO_CLAVE], [ITERACIONES_CLAVE],
        [DEBE_CAMBIAR_CLAVE], [CODIGO_ESTADO], [ID_USUARIO_ALTA]
    )
    VALUES
    (
        @V_ID_EMPRESA, N'AOCAUZI', N'Agustin Omar', N'Cauzi', N'agustincauzi10@hotmail.com',
        0x8EEFD95BBACD6FB085C9255DF15B00E49D085E7AA8984164BE48E06195F91BAC9BE9D767EBB59DA525FD5BCDF2C124A468AD1ED3FDBA3512A72267946527E92E,
        0xEB91E6E6A9835ABE0DA7CB91CB98D6B245493AA5091BD09857F089409880166B,
        N'PBKDF2-SHA512', 210000, 1, N'ACTIVO', 1
    );
END;

DECLARE @V_ID_USUARIO BIGINT = (SELECT [ID_USUARIO] FROM [SEGURIDAD].[USUARIOS] WHERE [ID_EMPRESA] = @V_ID_EMPRESA AND [NOMBRE_USUARIO] = N'AOCAUZI');
DECLARE @V_ID_ROL BIGINT = (SELECT [ID_ROL] FROM [SEGURIDAD].[ROLES] WHERE [ID_EMPRESA] = @V_ID_EMPRESA AND [CODIGO] = N'ADMINISTRADOR');

IF NOT EXISTS
(
    SELECT 1 FROM [SEGURIDAD].[USUARIOS_ROLES]
    WHERE [ID_USUARIO] = @V_ID_USUARIO AND [ID_ROL] = @V_ID_ROL AND [CODIGO_ESTADO] = N'ACTIVO'
)
BEGIN
    INSERT INTO [SEGURIDAD].[USUARIOS_ROLES] ([ID_USUARIO], [ID_ROL], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
    VALUES (@V_ID_USUARIO, @V_ID_ROL, N'ACTIVO', @V_ID_USUARIO);
END;
