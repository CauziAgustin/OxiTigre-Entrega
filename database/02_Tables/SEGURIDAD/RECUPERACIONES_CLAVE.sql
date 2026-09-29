/*
===============================================================================
Proyecto:              Sistema Modular de Gestión OxiTigre
Componente:            SEGURIDAD.RECUPERACIONES_CLAVE
Archivo:               RECUPERACIONES_CLAVE.sql
Versión:               1.0.0
Fecha:                 2026-08-19
ID pedido:             FABRICA
Desarrollador:         Agustin Omar Cauzi
Correo:                agustincauzi10@hotmail.com
Descripción funcional: Controla solicitudes temporales para restablecer credenciales.
Historial de modificaciones:
1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
CREATE TABLE [SEGURIDAD].[RECUPERACIONES_CLAVE]
(
    [ID_RECUPERACION_CLAVE]         BIGINT IDENTITY(1,1) NOT NULL,
    [ID_USUARIO]                    BIGINT NOT NULL,
    [HASH_TOKEN]                    VARBINARY(64) NOT NULL,
    [FECHA_SOLICITUD_UTC]           DATETIME2(3) NOT NULL,
    [FECHA_EXPIRACION_UTC]          DATETIME2(3) NOT NULL,
    [FECHA_USO_UTC]                 DATETIME2(3) NULL,
    [IP_ORIGEN]                     NVARCHAR(45) NULL,
    [CODIGO_ESTADO]                 NVARCHAR(30) NOT NULL
        CONSTRAINT [DF_RECUPERACIONES_CLAVE_CODIGO_ESTADO] DEFAULT (N'PENDIENTE'),
    [FECHA_ALTA_UTC]                DATETIME2(3) NOT NULL
        CONSTRAINT [DF_RECUPERACIONES_CLAVE_FECHA_ALTA_UTC] DEFAULT (SYSUTCDATETIME()),
    [ID_USUARIO_ALTA]               BIGINT NOT NULL,
    [FECHA_MODIFICACION_UTC]        DATETIME2(3) NULL,
    [ID_USUARIO_MODIFICACION]       BIGINT NULL,
    [ROW_VERSION]                   ROWVERSION NOT NULL,
    CONSTRAINT [PK_RECUPERACIONES_CLAVE] PRIMARY KEY CLUSTERED ([ID_RECUPERACION_CLAVE])
);
