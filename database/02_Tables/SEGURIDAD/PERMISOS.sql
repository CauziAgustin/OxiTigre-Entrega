/*
===============================================================================
Proyecto:              Sistema Modular de Gestión OxiTigre
Componente:            SEGURIDAD.PERMISOS
Archivo:               PERMISOS.sql
Versión:               1.0.0
Fecha:                 2026-08-19
ID pedido:             FABRICA
Desarrollador:         Agustin Omar Cauzi
Correo:                agustincauzi10@hotmail.com
Descripción funcional: Define acciones autorizables dentro de cada módulo.
Historial de modificaciones:
1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
CREATE TABLE [SEGURIDAD].[PERMISOS]
(
    [ID_PERMISO]                    BIGINT IDENTITY(1,1) NOT NULL,
    [ID_MODULO]                     BIGINT NOT NULL,
    [CODIGO]                        NVARCHAR(100) NOT NULL,
    [NOMBRE]                        NVARCHAR(150) NOT NULL,
    [DESCRIPCION]                   NVARCHAR(500) NULL,
    [CODIGO_ESTADO]                 NVARCHAR(30) NOT NULL
        CONSTRAINT [DF_PERMISOS_CODIGO_ESTADO] DEFAULT (N'ACTIVO'),
    [FECHA_ALTA_UTC]                DATETIME2(3) NOT NULL
        CONSTRAINT [DF_PERMISOS_FECHA_ALTA_UTC] DEFAULT (SYSUTCDATETIME()),
    [ID_USUARIO_ALTA]               BIGINT NOT NULL,
    [FECHA_MODIFICACION_UTC]        DATETIME2(3) NULL,
    [ID_USUARIO_MODIFICACION]       BIGINT NULL,
    [ROW_VERSION]                   ROWVERSION NOT NULL,
    CONSTRAINT [PK_PERMISOS] PRIMARY KEY CLUSTERED ([ID_PERMISO])
);
