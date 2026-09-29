/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: COMERCIAL.CLIENTES_BORRADORES
Archivo: CLIENTES_BORRADORES.sql
Versión: 1.0.0
Fecha: 2026-08-20
ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi
Correo: agustincauzi10@hotmail.com
Descripción funcional: Conserva una precarga incompleta de cliente por usuario y empresa.
Historial de modificaciones:
1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
CREATE TABLE [COMERCIAL].[CLIENTES_BORRADORES]
(
    [ID_CLIENTE_BORRADOR]          BIGINT IDENTITY(1,1) NOT NULL,
    [ID_EMPRESA]                   BIGINT NOT NULL,
    [ID_USUARIO]                   BIGINT NOT NULL,
    [CONTENIDO_JSON]               NVARCHAR(MAX) NOT NULL,
    [FECHA_ULTIMO_GUARDADO_UTC]    DATETIME2(3) NOT NULL,
    [CODIGO_ESTADO]                NVARCHAR(30) NOT NULL
        CONSTRAINT [DF_CLIENTES_BORRADORES_CODIGO_ESTADO] DEFAULT (N'BORRADOR'),
    [FECHA_ALTA_UTC]               DATETIME2(3) NOT NULL
        CONSTRAINT [DF_CLIENTES_BORRADORES_FECHA_ALTA_UTC] DEFAULT (SYSUTCDATETIME()),
    [ID_USUARIO_ALTA]              BIGINT NOT NULL,
    [FECHA_MODIFICACION_UTC]       DATETIME2(3) NULL,
    [ID_USUARIO_MODIFICACION]      BIGINT NULL,
    [ROW_VERSION]                  ROWVERSION NOT NULL,
    CONSTRAINT [PK_CLIENTES_BORRADORES] PRIMARY KEY CLUSTERED ([ID_CLIENTE_BORRADOR])
);
