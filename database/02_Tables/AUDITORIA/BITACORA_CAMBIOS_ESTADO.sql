/*
===============================================================================
Proyecto:              Sistema Modular de Gestión OxiTigre
Componente:            AUDITORIA.BITACORA_CAMBIOS_ESTADO
Archivo:               BITACORA_CAMBIOS_ESTADO.sql
Versión:               1.0.0
Fecha:                 2026-08-19
ID pedido:             FABRICA
Desarrollador:         Agustin Omar Cauzi
Correo:                agustincauzi10@hotmail.com
Descripción funcional: Conserva cada transición de estado realizada sobre una entidad.
Historial de modificaciones:
1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
CREATE TABLE [AUDITORIA].[BITACORA_CAMBIOS_ESTADO]
(
    [ID_BITACORA_CAMBIO_ESTADO]     BIGINT IDENTITY(1,1) NOT NULL,
    [ID_USUARIO]                    BIGINT NOT NULL,
    [ID_SESION]                     BIGINT NULL,
    [ENTIDAD]                       NVARCHAR(100) NOT NULL,
    [PKEY]                          NVARCHAR(200) NOT NULL,
    [CODIGO_ESTADO_ANTERIOR]        NVARCHAR(30) NULL,
    [CODIGO_ESTADO_NUEVO]           NVARCHAR(30) NOT NULL,
    [MOTIVO]                        NVARCHAR(1000) NULL,
    [FECHA_CAMBIO_UTC]              DATETIME2(3) NOT NULL
        CONSTRAINT [DF_BITACORA_CAMBIOS_ESTADO_FECHA_CAMBIO_UTC] DEFAULT (SYSUTCDATETIME()),
    [ID_CORRELACION]                UNIQUEIDENTIFIER NOT NULL,
    [CODIGO_ESTADO]                 NVARCHAR(30) NOT NULL
        CONSTRAINT [DF_BITACORA_CAMBIOS_ESTADO_CODIGO_ESTADO] DEFAULT (N'REGISTRADO'),
    [FECHA_ALTA_UTC]                DATETIME2(3) NOT NULL
        CONSTRAINT [DF_BITACORA_CAMBIOS_ESTADO_FECHA_ALTA_UTC] DEFAULT (SYSUTCDATETIME()),
    [ID_USUARIO_ALTA]               BIGINT NOT NULL,
    [FECHA_MODIFICACION_UTC]        DATETIME2(3) NULL,
    [ID_USUARIO_MODIFICACION]       BIGINT NULL,
    [ROW_VERSION]                   ROWVERSION NOT NULL,
    CONSTRAINT [PK_BITACORA_CAMBIOS_ESTADO] PRIMARY KEY CLUSTERED ([ID_BITACORA_CAMBIO_ESTADO])
);
