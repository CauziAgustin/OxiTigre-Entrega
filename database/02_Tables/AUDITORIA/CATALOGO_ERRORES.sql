/*
===============================================================================
Proyecto:              Sistema Modular de Gestión OxiTigre
Componente:            AUDITORIA.CATALOGO_ERRORES
Archivo:               CATALOGO_ERRORES.sql
Versión:               1.0.0
Fecha:                 2026-08-19
ID pedido:             FABRICA
Desarrollador:         Agustin Omar Cauzi
Correo:                agustincauzi10@hotmail.com
Descripción funcional: Define errores conocidos, su código funcional y orientación inicial.
Historial de modificaciones:
1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
CREATE TABLE [AUDITORIA].[CATALOGO_ERRORES]
(
    [ID_CATALOGO_ERROR]             BIGINT IDENTITY(1,1) NOT NULL,
    [ID_MODULO]                     BIGINT NOT NULL,
    [NUMERO_ERROR]                  SMALLINT NOT NULL,
    [CODIGO_ERROR]                  BIGINT NOT NULL,
    [NOMBRE]                        NVARCHAR(200) NOT NULL,
    [DESCRIPCION]                   NVARCHAR(1000) NOT NULL,
    [CAUSA_PROBABLE]                NVARCHAR(1000) NULL,
    [ACCION_RECOMENDADA]            NVARCHAR(2000) NULL,
    [SEVERIDAD]                     NVARCHAR(30) NOT NULL,
    [CODIGO_ESTADO]                 NVARCHAR(30) NOT NULL
        CONSTRAINT [DF_CATALOGO_ERRORES_CODIGO_ESTADO] DEFAULT (N'ACTIVO'),
    [FECHA_ALTA_UTC]                DATETIME2(3) NOT NULL
        CONSTRAINT [DF_CATALOGO_ERRORES_FECHA_ALTA_UTC] DEFAULT (SYSUTCDATETIME()),
    [ID_USUARIO_ALTA]               BIGINT NOT NULL,
    [FECHA_MODIFICACION_UTC]        DATETIME2(3) NULL,
    [ID_USUARIO_MODIFICACION]       BIGINT NULL,
    [ROW_VERSION]                   ROWVERSION NOT NULL,
    CONSTRAINT [PK_CATALOGO_ERRORES] PRIMARY KEY CLUSTERED ([ID_CATALOGO_ERROR])
);
