/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: INVENTARIO.RESERVAS_STOCK
Archivo: RESERVAS_STOCK.sql | Versión: 1.0.0 | Fecha: 2026-08-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Separa el stock comprometido por pedidos del saldo físico.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
CREATE TABLE [INVENTARIO].[RESERVAS_STOCK]
(
    [ID_RESERVA_STOCK] BIGINT IDENTITY(1,1) NOT NULL,
    [ID_EMPRESA] BIGINT NOT NULL,
    [ID_PEDIDO] BIGINT NOT NULL,
    [ID_PEDIDO_DETALLE] BIGINT NOT NULL,
    [ID_PRODUCTO] BIGINT NOT NULL,
    [ID_DEPOSITO] BIGINT NOT NULL,
    [CANTIDAD] DECIMAL(19,4) NOT NULL,
    [CODIGO_ESTADO] NVARCHAR(30) NOT NULL CONSTRAINT [DF_RESERVAS_STOCK_CODIGO_ESTADO] DEFAULT (N'RESERVADA'),
    [FECHA_ALTA_UTC] DATETIME2(3) NOT NULL CONSTRAINT [DF_RESERVAS_STOCK_FECHA_ALTA_UTC] DEFAULT (SYSUTCDATETIME()),
    [ID_USUARIO_ALTA] BIGINT NOT NULL,
    [FECHA_MODIFICACION_UTC] DATETIME2(3) NULL,
    [ID_USUARIO_MODIFICACION] BIGINT NULL,
    [ROW_VERSION] ROWVERSION NOT NULL,
    CONSTRAINT [PK_RESERVAS_STOCK] PRIMARY KEY CLUSTERED ([ID_RESERVA_STOCK])
);
