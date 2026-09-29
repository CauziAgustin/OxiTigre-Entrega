/*
===============================================================================
Proyecto:              Sistema Modular de Gestión OxiTigre
Componente:            SEGURIDAD.ROLES_PERMISOS
Archivo:               ROLES_PERMISOS.sql
Versión:               1.0.0
Fecha:                 2026-08-19
ID pedido:             FABRICA
Desarrollador:         Agustin Omar Cauzi
Correo:                agustincauzi10@hotmail.com
Descripción funcional: Relaciona roles con las acciones que pueden ejecutar.
Historial de modificaciones:
1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
CREATE TABLE [SEGURIDAD].[ROLES_PERMISOS]
(
    [ID_ROL_PERMISO]                BIGINT IDENTITY(1,1) NOT NULL,
    [ID_ROL]                        BIGINT NOT NULL,
    [ID_PERMISO]                    BIGINT NOT NULL,
    [CODIGO_ESTADO]                 NVARCHAR(30) NOT NULL
        CONSTRAINT [DF_ROLES_PERMISOS_CODIGO_ESTADO] DEFAULT (N'ACTIVO'),
    [FECHA_ALTA_UTC]                DATETIME2(3) NOT NULL
        CONSTRAINT [DF_ROLES_PERMISOS_FECHA_ALTA_UTC] DEFAULT (SYSUTCDATETIME()),
    [ID_USUARIO_ALTA]               BIGINT NOT NULL,
    [FECHA_MODIFICACION_UTC]        DATETIME2(3) NULL,
    [ID_USUARIO_MODIFICACION]       BIGINT NULL,
    [ROW_VERSION]                   ROWVERSION NOT NULL,
    CONSTRAINT [PK_ROLES_PERMISOS] PRIMARY KEY CLUSTERED ([ID_ROL_PERMISO])
);
