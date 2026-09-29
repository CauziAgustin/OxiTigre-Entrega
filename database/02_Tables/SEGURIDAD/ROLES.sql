/*
===============================================================================
Proyecto:              Sistema Modular de Gestión OxiTigre
Componente:            SEGURIDAD.ROLES
Archivo:               ROLES.sql
Versión:               1.0.0
Fecha:                 2026-08-19
ID pedido:             FABRICA
Desarrollador:         Agustin Omar Cauzi
Correo:                agustincauzi10@hotmail.com
Descripción funcional: Define agrupaciones de permisos asignables a usuarios.
Historial de modificaciones:
1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
CREATE TABLE [SEGURIDAD].[ROLES]
(
    [ID_ROL]                        BIGINT IDENTITY(1,1) NOT NULL,
    [ID_EMPRESA]                    BIGINT NULL,
    [CODIGO]                        NVARCHAR(50) NOT NULL,
    [NOMBRE]                        NVARCHAR(150) NOT NULL,
    [DESCRIPCION]                   NVARCHAR(500) NULL,
    [ES_SISTEMA]                    BIT NOT NULL
        CONSTRAINT [DF_ROLES_ES_SISTEMA] DEFAULT (0),
    [CODIGO_ESTADO]                 NVARCHAR(30) NOT NULL
        CONSTRAINT [DF_ROLES_CODIGO_ESTADO] DEFAULT (N'ACTIVO'),
    [FECHA_ALTA_UTC]                DATETIME2(3) NOT NULL
        CONSTRAINT [DF_ROLES_FECHA_ALTA_UTC] DEFAULT (SYSUTCDATETIME()),
    [ID_USUARIO_ALTA]               BIGINT NOT NULL,
    [FECHA_MODIFICACION_UTC]        DATETIME2(3) NULL,
    [ID_USUARIO_MODIFICACION]       BIGINT NULL,
    [ROW_VERSION]                   ROWVERSION NOT NULL,
    CONSTRAINT [PK_ROLES] PRIMARY KEY CLUSTERED ([ID_ROL])
);
