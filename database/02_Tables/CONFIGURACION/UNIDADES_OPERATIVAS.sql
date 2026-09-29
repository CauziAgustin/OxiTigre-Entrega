/*
===============================================================================
Proyecto:              Sistema Modular de Gestión OxiTigre
Componente:            CONFIGURACION.UNIDADES_OPERATIVAS
Archivo:               UNIDADES_OPERATIVAS.sql
Versión:               1.0.0
Fecha:                 2026-08-19
ID pedido:             FABRICA
Desarrollador:         Agustin Omar Cauzi
Correo:                agustincauzi10@hotmail.com
Descripción funcional: Define áreas operativas dentro de cada sucursal.
Historial de modificaciones:
1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
CREATE TABLE [CONFIGURACION].[UNIDADES_OPERATIVAS]
(
    [ID_UNIDAD_OPERATIVA]           BIGINT IDENTITY(1,1) NOT NULL,
    [ID_SUCURSAL]                   BIGINT NOT NULL,
    [CODIGO]                        NVARCHAR(30) NOT NULL,
    [NOMBRE]                        NVARCHAR(200) NOT NULL,
    [DESCRIPCION]                   NVARCHAR(500) NULL,
    [CODIGO_ESTADO]                 NVARCHAR(30) NOT NULL
        CONSTRAINT [DF_UNIDADES_OPERATIVAS_CODIGO_ESTADO] DEFAULT (N'ACTIVO'),
    [FECHA_ALTA_UTC]                DATETIME2(3) NOT NULL
        CONSTRAINT [DF_UNIDADES_OPERATIVAS_FECHA_ALTA_UTC] DEFAULT (SYSUTCDATETIME()),
    [ID_USUARIO_ALTA]               BIGINT NOT NULL,
    [FECHA_MODIFICACION_UTC]        DATETIME2(3) NULL,
    [ID_USUARIO_MODIFICACION]       BIGINT NULL,
    [ROW_VERSION]                   ROWVERSION NOT NULL,
    CONSTRAINT [PK_UNIDADES_OPERATIVAS] PRIMARY KEY CLUSTERED ([ID_UNIDAD_OPERATIVA])
);
