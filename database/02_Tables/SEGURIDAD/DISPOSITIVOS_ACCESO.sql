/*
===============================================================================
Proyecto:              Sistema Modular de Gestión OxiTigre
Componente:            SEGURIDAD.DISPOSITIVOS_ACCESO
Archivo:               DISPOSITIVOS_ACCESO.sql
Versión:               1.0.0
Fecha:                 2026-08-19
ID pedido:             FABRICA
Desarrollador:         Agustin Omar Cauzi
Correo:                agustincauzi10@hotmail.com
Descripción funcional: Identifica equipos y aplicaciones desde los que acceden usuarios.
Historial de modificaciones:
1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
CREATE TABLE [SEGURIDAD].[DISPOSITIVOS_ACCESO]
(
    [ID_DISPOSITIVO_ACCESO]         BIGINT IDENTITY(1,1) NOT NULL,
    [ID_USUARIO]                    BIGINT NULL,
    [IDENTIFICADOR_DISPOSITIVO]     NVARCHAR(200) NOT NULL,
    [NOMBRE_DISPOSITIVO]            NVARCHAR(150) NULL,
    [TIPO_DISPOSITIVO]              NVARCHAR(50) NULL,
    [SISTEMA_OPERATIVO]             NVARCHAR(150) NULL,
    [VERSION_APLICACION]            NVARCHAR(50) NULL,
    [ES_CONFIABLE]                  BIT NOT NULL
        CONSTRAINT [DF_DISPOSITIVOS_ACCESO_ES_CONFIABLE] DEFAULT (0),
    [FECHA_ULTIMO_ACCESO_UTC]       DATETIME2(3) NULL,
    [CODIGO_ESTADO]                 NVARCHAR(30) NOT NULL
        CONSTRAINT [DF_DISPOSITIVOS_ACCESO_CODIGO_ESTADO] DEFAULT (N'ACTIVO'),
    [FECHA_ALTA_UTC]                DATETIME2(3) NOT NULL
        CONSTRAINT [DF_DISPOSITIVOS_ACCESO_FECHA_ALTA_UTC] DEFAULT (SYSUTCDATETIME()),
    [ID_USUARIO_ALTA]               BIGINT NOT NULL,
    [FECHA_MODIFICACION_UTC]        DATETIME2(3) NULL,
    [ID_USUARIO_MODIFICACION]       BIGINT NULL,
    [ROW_VERSION]                   ROWVERSION NOT NULL,
    CONSTRAINT [PK_DISPOSITIVOS_ACCESO] PRIMARY KEY CLUSTERED ([ID_DISPOSITIVO_ACCESO])
);
