/*
===============================================================================
Proyecto:              Sistema Modular de Gestión OxiTigre
Componente:            CONFIGURACION.TIPOS_TELEFONO
Archivo:               TIPOS_TELEFONO.sql
Versión:               1.0.0
Fecha:                 2026-08-19
ID pedido:             FABRICA
Desarrollador:         Agustin Omar Cauzi
Correo:                agustincauzi10@hotmail.com

Descripción funcional:
Define el catálogo homogéneo de clases de teléfono utilizadas por el sistema.

Relaciones:
Será referenciada por las tablas que administren teléfonos de personas o empresas.

Reglas de integridad:
CODIGO identifica de forma estable cada tipo y no debe depender del ID físico.

Historial de modificaciones:
Versión | Fecha      | ID pedido | Desarrollador       | Correo                         | Descripción
1.0.0   | 2026-08-19 | FABRICA   | Agustin Omar Cauzi  | agustincauzi10@hotmail.com     | Creación inicial.
===============================================================================
*/
CREATE TABLE [CONFIGURACION].[TIPOS_TELEFONO]
(
    [ID_TIPO_TELEFONO]              BIGINT IDENTITY(1,1) NOT NULL,
    [CODIGO]                        NVARCHAR(30) NOT NULL,
    [NOMBRE]                        NVARCHAR(100) NOT NULL,
    [DESCRIPCION]                   NVARCHAR(500) NULL,
    [CODIGO_ESTADO]                 NVARCHAR(30) NOT NULL
        CONSTRAINT [DF_TIPOS_TELEFONO_CODIGO_ESTADO] DEFAULT (N'ACTIVO'),
    [FECHA_ALTA_UTC]                DATETIME2(3) NOT NULL
        CONSTRAINT [DF_TIPOS_TELEFONO_FECHA_ALTA_UTC] DEFAULT (SYSUTCDATETIME()),
    [ID_USUARIO_ALTA]               BIGINT NOT NULL,
    [FECHA_MODIFICACION_UTC]        DATETIME2(3) NULL,
    [ID_USUARIO_MODIFICACION]       BIGINT NULL,
    [ROW_VERSION]                   ROWVERSION NOT NULL,
    CONSTRAINT [PK_TIPOS_TELEFONO]
        PRIMARY KEY CLUSTERED ([ID_TIPO_TELEFONO])
);
