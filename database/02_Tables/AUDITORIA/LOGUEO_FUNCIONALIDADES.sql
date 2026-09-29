/*
===============================================================================
Proyecto:              Sistema Modular de Gestión OxiTigre
Componente:            AUDITORIA.LOGUEO_FUNCIONALIDADES
Archivo:               LOGUEO_FUNCIONALIDADES.sql
Versión:               1.0.1
Fecha:                 2026-08-19
ID pedido:             FABRICA
Desarrollador:         Agustin Omar Cauzi
Correo:                agustincauzi10@hotmail.com
Descripción funcional: Registra el ciclo completo de cada SP ejecutado por una funcionalidad.
Historial de modificaciones:
1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
1.0.1 | 2026-08-19 | FABRICA | Homogeneización del estado inicial REGISTRADO.
===============================================================================
*/
CREATE TABLE [AUDITORIA].[LOGUEO_FUNCIONALIDADES]
(
    [ID_LOGUEO_FUNCIONALIDAD]       BIGINT IDENTITY(1,1) NOT NULL,
    [ID_EMPRESA]                    BIGINT NOT NULL,
    [ID_MODULO]                     BIGINT NOT NULL,
    [ID_USUARIO]                    BIGINT NULL,
    [ID_SESION]                     BIGINT NULL,
    [ID_DISPOSITIVO_ACCESO]         BIGINT NULL,
    [ID_ERROR_APLICACION]           BIGINT NULL,
    [FUNCIONALIDAD]                 NVARCHAR(200) NOT NULL,
    [PKEY]                          NVARCHAR(200) NULL,
    [PANTALLA]                      NVARCHAR(200) NULL,
    [ACCION]                        NVARCHAR(100) NOT NULL,
    [CASO_USO]                      NVARCHAR(50) NULL,
    [SCHEMA_SP]                     NVARCHAR(128) NOT NULL,
    [NOMBRE_SP]                     NVARCHAR(128) NOT NULL,
    [PARAMETROS_REDACTADOS_JSON]    NVARCHAR(MAX) NULL,
    [OUTPUTS_REDACTADOS_JSON]       NVARCHAR(MAX) NULL,
    [FECHA_INICIO_UTC]              DATETIME2(3) NOT NULL,
    [FECHA_FIN_UTC]                 DATETIME2(3) NULL,
    [DURACION_MS]                   BIGINT NULL,
    [FILAS_AFECTADAS]               INT NULL,
    [RESULTADO]                     NVARCHAR(30) NOT NULL,
    [CODIGO_ERROR]                  BIGINT NULL,
    [MENSAJE_ERROR]                 NVARCHAR(4000) NULL,
    [EQUIPO_ORIGEN]                 NVARCHAR(150) NULL,
    [IP_ORIGEN]                     NVARCHAR(45) NULL,
    [APLICACION_ORIGEN]             NVARCHAR(100) NOT NULL,
    [ID_CORRELACION]                UNIQUEIDENTIFIER NOT NULL,
    [CODIGO_ESTADO]                 NVARCHAR(30) NOT NULL
        CONSTRAINT [DF_LOGUEO_FUNCIONALIDADES_CODIGO_ESTADO] DEFAULT (N'REGISTRADO'),
    [FECHA_ALTA_UTC]                DATETIME2(3) NOT NULL
        CONSTRAINT [DF_LOGUEO_FUNCIONALIDADES_FECHA_ALTA_UTC] DEFAULT (SYSUTCDATETIME()),
    [ID_USUARIO_ALTA]               BIGINT NOT NULL,
    [FECHA_MODIFICACION_UTC]        DATETIME2(3) NULL,
    [ID_USUARIO_MODIFICACION]       BIGINT NULL,
    [ROW_VERSION]                   ROWVERSION NOT NULL,
    CONSTRAINT [PK_LOGUEO_FUNCIONALIDADES] PRIMARY KEY CLUSTERED ([ID_LOGUEO_FUNCIONALIDAD])
);
