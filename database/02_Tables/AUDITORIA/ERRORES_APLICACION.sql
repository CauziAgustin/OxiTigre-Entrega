/*
===============================================================================
Proyecto:              Sistema Modular de Gestión OxiTigre
Componente:            AUDITORIA.ERRORES_APLICACION
Archivo:               ERRORES_APLICACION.sql
Versión:               1.0.0
Fecha:                 2026-08-19
ID pedido:             FABRICA
Desarrollador:         Agustin Omar Cauzi
Correo:                agustincauzi10@hotmail.com
Descripción funcional: Registra cada ocurrencia de un error funcional o técnico.
Historial de modificaciones:
1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
CREATE TABLE [AUDITORIA].[ERRORES_APLICACION]
(
    [ID_ERROR_APLICACION]           BIGINT IDENTITY(1,1) NOT NULL,
    [ID_CATALOGO_ERROR]             BIGINT NULL,
    [CODIGO_ERROR]                  BIGINT NULL,
    [ID_USUARIO]                    BIGINT NULL,
    [ID_SESION]                     BIGINT NULL,
    [ID_DISPOSITIVO_ACCESO]         BIGINT NULL,
    [COMPONENTE]                    NVARCHAR(200) NOT NULL,
    [SCHEMA_SP]                     NVARCHAR(128) NULL,
    [NOMBRE_SP]                     NVARCHAR(128) NULL,
    [MENSAJE]                       NVARCHAR(4000) NOT NULL,
    [DETALLE_TECNICO]               NVARCHAR(MAX) NULL,
    [STACK_TRACE]                   NVARCHAR(MAX) NULL,
    [PARAMETROS_REDACTADOS_JSON]    NVARCHAR(MAX) NULL,
    [FECHA_OCURRENCIA_UTC]          DATETIME2(3) NOT NULL
        CONSTRAINT [DF_ERRORES_APLICACION_FECHA_OCURRENCIA_UTC] DEFAULT (SYSUTCDATETIME()),
    [EQUIPO_ORIGEN]                 NVARCHAR(150) NULL,
    [IP_ORIGEN]                     NVARCHAR(45) NULL,
    [APLICACION_ORIGEN]             NVARCHAR(100) NOT NULL,
    [ID_CORRELACION]                UNIQUEIDENTIFIER NOT NULL,
    [CODIGO_ESTADO]                 NVARCHAR(30) NOT NULL
        CONSTRAINT [DF_ERRORES_APLICACION_CODIGO_ESTADO] DEFAULT (N'REGISTRADO'),
    [FECHA_ALTA_UTC]                DATETIME2(3) NOT NULL
        CONSTRAINT [DF_ERRORES_APLICACION_FECHA_ALTA_UTC] DEFAULT (SYSUTCDATETIME()),
    [ID_USUARIO_ALTA]               BIGINT NOT NULL,
    [FECHA_MODIFICACION_UTC]        DATETIME2(3) NULL,
    [ID_USUARIO_MODIFICACION]       BIGINT NULL,
    [ROW_VERSION]                   ROWVERSION NOT NULL,
    CONSTRAINT [PK_ERRORES_APLICACION] PRIMARY KEY CLUSTERED ([ID_ERROR_APLICACION])
);
