/*
===============================================================================
Proyecto:              Sistema Modular de Gestión OxiTigre
Componente:            SEGURIDAD.USUARIOS_ROLES
Archivo:               USUARIOS_ROLES.sql
Versión:               1.0.0
Fecha:                 2026-08-19
ID pedido:             FABRICA
Desarrollador:         Agustin Omar Cauzi
Correo:                agustincauzi10@hotmail.com
Descripción funcional: Registra roles asignados a usuarios y su vigencia.
Historial de modificaciones:
1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
CREATE TABLE [SEGURIDAD].[USUARIOS_ROLES]
(
    [ID_USUARIO_ROL]                BIGINT IDENTITY(1,1) NOT NULL,
    [ID_USUARIO]                    BIGINT NOT NULL,
    [ID_ROL]                        BIGINT NOT NULL,
    [FECHA_VIGENCIA_DESDE_UTC]      DATETIME2(3) NOT NULL
        CONSTRAINT [DF_USUARIOS_ROLES_FECHA_VIGENCIA_DESDE_UTC] DEFAULT (SYSUTCDATETIME()),
    [FECHA_VIGENCIA_HASTA_UTC]      DATETIME2(3) NULL,
    [CODIGO_ESTADO]                 NVARCHAR(30) NOT NULL
        CONSTRAINT [DF_USUARIOS_ROLES_CODIGO_ESTADO] DEFAULT (N'ACTIVO'),
    [FECHA_ALTA_UTC]                DATETIME2(3) NOT NULL
        CONSTRAINT [DF_USUARIOS_ROLES_FECHA_ALTA_UTC] DEFAULT (SYSUTCDATETIME()),
    [ID_USUARIO_ALTA]               BIGINT NOT NULL,
    [FECHA_MODIFICACION_UTC]        DATETIME2(3) NULL,
    [ID_USUARIO_MODIFICACION]       BIGINT NULL,
    [ROW_VERSION]                   ROWVERSION NOT NULL,
    CONSTRAINT [PK_USUARIOS_ROLES] PRIMARY KEY CLUSTERED ([ID_USUARIO_ROL])
);
