/*
===============================================================================
Proyecto:              Sistema Modular de Gestión OxiTigre
Componente:            CONFIGURACION.EMPRESAS
Archivo:               EMPRESAS.sql
Versión:               1.0.0
Fecha:                 2026-08-19
ID pedido:             FABRICA
Desarrollador:         Agustin Omar Cauzi
Correo:                agustincauzi10@hotmail.com
Descripción funcional: Identifica la empresa propietaria de la base operativa.
Historial de modificaciones:
1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
CREATE TABLE [CONFIGURACION].[EMPRESAS]
(
    [ID_EMPRESA]                    BIGINT IDENTITY(1,1) NOT NULL,
    [CODIGO]                        NVARCHAR(30) NOT NULL,
    [RAZON_SOCIAL]                  NVARCHAR(200) NOT NULL,
    [NOMBRE_FANTASIA]               NVARCHAR(200) NULL,
    [CUIT]                          CHAR(11) NULL,
    [EMAIL]                         NVARCHAR(254) NULL,
    [CODIGO_ESTADO]                 NVARCHAR(30) NOT NULL
        CONSTRAINT [DF_EMPRESAS_CODIGO_ESTADO] DEFAULT (N'ACTIVO'),
    [FECHA_ALTA_UTC]                DATETIME2(3) NOT NULL
        CONSTRAINT [DF_EMPRESAS_FECHA_ALTA_UTC] DEFAULT (SYSUTCDATETIME()),
    [ID_USUARIO_ALTA]               BIGINT NOT NULL,
    [FECHA_MODIFICACION_UTC]        DATETIME2(3) NULL,
    [ID_USUARIO_MODIFICACION]       BIGINT NULL,
    [ROW_VERSION]                   ROWVERSION NOT NULL,
    CONSTRAINT [PK_EMPRESAS] PRIMARY KEY CLUSTERED ([ID_EMPRESA])
);
