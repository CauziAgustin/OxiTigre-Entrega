/*
===============================================================================
Proyecto:              Sistema Modular de Gestión OxiTigre
Componente:            CONFIGURACION.ESTADOS
Archivo:               ESTADOS.sql
Versión:               1.0.0
Fecha:                 2026-08-19
ID pedido:             FABRICA
Desarrollador:         Agustin Omar Cauzi
Correo:                agustincauzi10@hotmail.com

Descripción funcional:
Documenta los códigos de estado admitidos por cada entidad. Es un catálogo
informativo sin FK; los SP validan su existencia antes de modificar datos.

Relaciones:
No posee relaciones físicas por decisión funcional. ENTIDAD y CODIGO_ESTADO
identifican de manera única el estado documentado.

Reglas de integridad:
La combinación ENTIDAD + CODIGO_ESTADO es única. Los códigos son estables,
se escriben en mayúsculas y no deben utilizarse como textos de presentación.

Historial de modificaciones:
Versión | Fecha      | ID pedido | Desarrollador       | Correo                         | Descripción
1.0.0   | 2026-08-19 | FABRICA   | Agustin Omar Cauzi  | agustincauzi10@hotmail.com     | Creación inicial.
===============================================================================
*/
CREATE TABLE [CONFIGURACION].[ESTADOS]
(
    [ID_ESTADO]                     BIGINT IDENTITY(1,1) NOT NULL,
    [ENTIDAD]                       NVARCHAR(100) NOT NULL,
    [CODIGO_ESTADO]                 NVARCHAR(30) NOT NULL,
    [NOMBRE]                        NVARCHAR(100) NOT NULL,
    [DESCRIPCION]                   NVARCHAR(500) NULL,
    [ES_INICIAL]                    BIT NOT NULL
        CONSTRAINT [DF_ESTADOS_ES_INICIAL] DEFAULT (0),
    [ES_FINAL]                      BIT NOT NULL
        CONSTRAINT [DF_ESTADOS_ES_FINAL] DEFAULT (0),
    [ORDEN]                         SMALLINT NOT NULL
        CONSTRAINT [DF_ESTADOS_ORDEN] DEFAULT (0),
    [FECHA_VIGENCIA_DESDE_UTC]      DATETIME2(3) NOT NULL
        CONSTRAINT [DF_ESTADOS_FECHA_VIGENCIA_DESDE_UTC] DEFAULT (SYSUTCDATETIME()),
    [FECHA_VIGENCIA_HASTA_UTC]      DATETIME2(3) NULL,
    [FECHA_ALTA_UTC]                DATETIME2(3) NOT NULL
        CONSTRAINT [DF_ESTADOS_FECHA_ALTA_UTC] DEFAULT (SYSUTCDATETIME()),
    [ID_USUARIO_ALTA]               BIGINT NOT NULL,
    [FECHA_MODIFICACION_UTC]        DATETIME2(3) NULL,
    [ID_USUARIO_MODIFICACION]       BIGINT NULL,
    [ROW_VERSION]                   ROWVERSION NOT NULL,
    CONSTRAINT [PK_ESTADOS]
        PRIMARY KEY CLUSTERED ([ID_ESTADO])
);
