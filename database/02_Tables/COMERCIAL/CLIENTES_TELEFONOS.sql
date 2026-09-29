/*
===============================================================================
Proyecto:              Sistema Modular de Gestión OxiTigre
Componente:            COMERCIAL.CLIENTES_TELEFONOS
Archivo:               CLIENTES_TELEFONOS.sql
Versión:               1.0.0
Fecha:                 2026-08-19
ID pedido:             FABRICA
Desarrollador:         Agustin Omar Cauzi
Correo:                agustincauzi10@hotmail.com

Descripción funcional:
Almacena todos los teléfonos informados por cada cliente, su tipo, orden,
prioridad de contacto, capacidades y vigencia.

Relaciones:
ID_CLIENTE referencia COMERCIAL.CLIENTES.
ID_TIPO_TELEFONO referencia CONFIGURACION.TIPOS_TELEFONO.
Las FK se incorporarán en la fase de constraints una vez creadas sus tablas base.

Reglas de integridad:
ORDEN comienza en cero. Solo puede existir un teléfono principal activo por
cliente y no puede repetirse el orden entre teléfonos activos del mismo cliente.

Historial de modificaciones:
Versión | Fecha      | ID pedido | Desarrollador       | Correo                         | Descripción
1.0.0   | 2026-08-19 | FABRICA   | Agustin Omar Cauzi  | agustincauzi10@hotmail.com     | Creación inicial.
===============================================================================
*/
CREATE TABLE [COMERCIAL].[CLIENTES_TELEFONOS]
(
    [ID_CLIENTE_TELEFONO]           BIGINT IDENTITY(1,1) NOT NULL,
    [ID_CLIENTE]                    BIGINT NOT NULL,
    [ID_TIPO_TELEFONO]              BIGINT NOT NULL,
    [CODIGO_PAIS]                   NVARCHAR(5) NULL,
    [CODIGO_AREA]                   NVARCHAR(10) NULL,
    [NUMERO]                        NVARCHAR(20) NOT NULL,
    [INTERNO]                       NVARCHAR(10) NULL,
    [ORDEN]                         SMALLINT NOT NULL
        CONSTRAINT [DF_CLIENTES_TELEFONOS_ORDEN] DEFAULT (0),
    [ES_PRINCIPAL]                  BIT NOT NULL
        CONSTRAINT [DF_CLIENTES_TELEFONOS_ES_PRINCIPAL] DEFAULT (0),
    [PERMITE_WHATSAPP]              BIT NOT NULL
        CONSTRAINT [DF_CLIENTES_TELEFONOS_PERMITE_WHATSAPP] DEFAULT (0),
    [OBSERVACION]                   NVARCHAR(500) NULL,
    [CODIGO_ESTADO]                 NVARCHAR(30) NOT NULL
        CONSTRAINT [DF_CLIENTES_TELEFONOS_CODIGO_ESTADO] DEFAULT (N'ACTIVO'),
    [FECHA_ALTA_UTC]                DATETIME2(3) NOT NULL
        CONSTRAINT [DF_CLIENTES_TELEFONOS_FECHA_ALTA_UTC] DEFAULT (SYSUTCDATETIME()),
    [ID_USUARIO_ALTA]               BIGINT NOT NULL,
    [FECHA_MODIFICACION_UTC]        DATETIME2(3) NULL,
    [ID_USUARIO_MODIFICACION]       BIGINT NULL,
    [ROW_VERSION]                   ROWVERSION NOT NULL,
    CONSTRAINT [PK_CLIENTES_TELEFONOS]
        PRIMARY KEY CLUSTERED ([ID_CLIENTE_TELEFONO])
);
