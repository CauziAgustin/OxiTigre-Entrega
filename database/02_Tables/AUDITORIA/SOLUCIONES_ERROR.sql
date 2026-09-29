/*
===============================================================================
Proyecto:              Sistema Modular de Gestión OxiTigre
Componente:            AUDITORIA.SOLUCIONES_ERROR
Archivo:               SOLUCIONES_ERROR.sql
Versión:               1.0.0
Fecha:                 2026-08-19
ID pedido:             FABRICA
Desarrollador:         Agustin Omar Cauzi
Correo:                agustincauzi10@hotmail.com
Descripción funcional: Conserva soluciones versionadas para errores conocidos.
Historial de modificaciones:
1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
CREATE TABLE [AUDITORIA].[SOLUCIONES_ERROR]
(
    [ID_SOLUCION_ERROR]             BIGINT IDENTITY(1,1) NOT NULL,
    [ID_CATALOGO_ERROR]             BIGINT NOT NULL,
    [TITULO]                        NVARCHAR(200) NOT NULL,
    [DESCRIPCION]                   NVARCHAR(1000) NOT NULL,
    [PASOS_SOLUCION]                NVARCHAR(MAX) NOT NULL,
    [VERSION_DESDE]                 NVARCHAR(50) NULL,
    [VERSION_HASTA]                 NVARCHAR(50) NULL,
    [ES_PREFERIDA]                  BIT NOT NULL
        CONSTRAINT [DF_SOLUCIONES_ERROR_ES_PREFERIDA] DEFAULT (0),
    [CODIGO_ESTADO]                 NVARCHAR(30) NOT NULL
        CONSTRAINT [DF_SOLUCIONES_ERROR_CODIGO_ESTADO] DEFAULT (N'ACTIVO'),
    [FECHA_ALTA_UTC]                DATETIME2(3) NOT NULL
        CONSTRAINT [DF_SOLUCIONES_ERROR_FECHA_ALTA_UTC] DEFAULT (SYSUTCDATETIME()),
    [ID_USUARIO_ALTA]               BIGINT NOT NULL,
    [FECHA_MODIFICACION_UTC]        DATETIME2(3) NULL,
    [ID_USUARIO_MODIFICACION]       BIGINT NULL,
    [ROW_VERSION]                   ROWVERSION NOT NULL,
    CONSTRAINT [PK_SOLUCIONES_ERROR] PRIMARY KEY CLUSTERED ([ID_SOLUCION_ERROR])
);
