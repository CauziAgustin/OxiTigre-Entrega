/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: INVENTARIO.EXISTENCIAS
Archivo: EXISTENCIAS.sql | Versión: 1.0.0 | Fecha: 2026-08-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Conserva el saldo y mínimo de cada producto por depósito.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
CREATE TABLE [INVENTARIO].[EXISTENCIAS]
(
    [ID_EXISTENCIA] BIGINT IDENTITY(1,1) NOT NULL,
    [ID_EMPRESA] BIGINT NOT NULL,
    [ID_PRODUCTO] BIGINT NOT NULL,
    [ID_DEPOSITO] BIGINT NOT NULL,
    [CANTIDAD] DECIMAL(19,4) NOT NULL CONSTRAINT [DF_EXISTENCIAS_CANTIDAD] DEFAULT (0),
    [STOCK_MINIMO] DECIMAL(19,4) NOT NULL CONSTRAINT [DF_EXISTENCIAS_STOCK_MINIMO] DEFAULT (0),
    [CODIGO_ESTADO] NVARCHAR(30) NOT NULL CONSTRAINT [DF_EXISTENCIAS_CODIGO_ESTADO] DEFAULT (N'ACTIVO'),
    [FECHA_ALTA_UTC] DATETIME2(3) NOT NULL CONSTRAINT [DF_EXISTENCIAS_FECHA_ALTA_UTC] DEFAULT (SYSUTCDATETIME()),
    [ID_USUARIO_ALTA] BIGINT NOT NULL,
    [FECHA_MODIFICACION_UTC] DATETIME2(3) NULL,
    [ID_USUARIO_MODIFICACION] BIGINT NULL,
    [ROW_VERSION] ROWVERSION NOT NULL,
    CONSTRAINT [PK_EXISTENCIAS] PRIMARY KEY CLUSTERED ([ID_EXISTENCIA])
);
