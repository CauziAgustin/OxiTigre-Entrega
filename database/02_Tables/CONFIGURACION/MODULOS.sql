/*
===============================================================================
Proyecto:              Sistema Modular de Gestión OxiTigre
Componente:            CONFIGURACION.MODULOS
Archivo:               MODULOS.sql
Versión:               1.0.0
Fecha:                 2026-08-19
ID pedido:             FABRICA
Desarrollador:         Agustin Omar Cauzi
Correo:                agustincauzi10@hotmail.com
Descripción funcional: Define módulos y su número utilizado en códigos de error.
Historial de modificaciones:
1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
CREATE TABLE [CONFIGURACION].[MODULOS]
(
    [ID_MODULO]                     BIGINT IDENTITY(1,1) NOT NULL,
    [NUMERO_MODULO]                 SMALLINT NOT NULL,
    [CODIGO]                        NVARCHAR(30) NOT NULL,
    [NOMBRE]                        NVARCHAR(150) NOT NULL,
    [DESCRIPCION]                   NVARCHAR(500) NULL,
    [ORDEN]                         SMALLINT NOT NULL
        CONSTRAINT [DF_MODULOS_ORDEN] DEFAULT (0),
    [CODIGO_ESTADO]                 NVARCHAR(30) NOT NULL
        CONSTRAINT [DF_MODULOS_CODIGO_ESTADO] DEFAULT (N'ACTIVO'),
    [FECHA_ALTA_UTC]                DATETIME2(3) NOT NULL
        CONSTRAINT [DF_MODULOS_FECHA_ALTA_UTC] DEFAULT (SYSUTCDATETIME()),
    [ID_USUARIO_ALTA]               BIGINT NOT NULL,
    [FECHA_MODIFICACION_UTC]        DATETIME2(3) NULL,
    [ID_USUARIO_MODIFICACION]       BIGINT NULL,
    [ROW_VERSION]                   ROWVERSION NOT NULL,
    CONSTRAINT [PK_MODULOS] PRIMARY KEY CLUSTERED ([ID_MODULO])
);
