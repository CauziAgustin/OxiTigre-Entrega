/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: COMERCIAL.VENTAS
Archivo: VENTAS.sql | Versión: 1.0.0 | Fecha: 2026-08-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Registra la venta interna confirmada desde un pedido reservado.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
CREATE TABLE [COMERCIAL].[VENTAS]
(
    [ID_VENTA] BIGINT IDENTITY(1,1) NOT NULL,
    [ID_EMPRESA] BIGINT NOT NULL,
    [ID_PEDIDO] BIGINT NOT NULL,
    [ID_CLIENTE] BIGINT NOT NULL,
    [ID_MOVIMIENTO] BIGINT NULL,
    [CODIGO] NVARCHAR(30) NOT NULL,
    [FECHA_VENTA_UTC] DATETIME2(3) NOT NULL,
    [MONEDA] CHAR(3) NOT NULL,
    [SUBTOTAL] DECIMAL(19,4) NOT NULL,
    [TOTAL_DESCUENTO] DECIMAL(19,4) NOT NULL,
    [TOTAL_IMPUESTO] DECIMAL(19,4) NOT NULL,
    [TOTAL] DECIMAL(19,4) NOT NULL,
    [OBSERVACION] NVARCHAR(500) NULL,
    [CODIGO_ESTADO] NVARCHAR(30) NOT NULL CONSTRAINT [DF_VENTAS_CODIGO_ESTADO] DEFAULT (N'CONFIRMADA'),
    [FECHA_ALTA_UTC] DATETIME2(3) NOT NULL CONSTRAINT [DF_VENTAS_FECHA_ALTA_UTC] DEFAULT (SYSUTCDATETIME()),
    [ID_USUARIO_ALTA] BIGINT NOT NULL,
    [FECHA_MODIFICACION_UTC] DATETIME2(3) NULL,
    [ID_USUARIO_MODIFICACION] BIGINT NULL,
    [ROW_VERSION] ROWVERSION NOT NULL,
    CONSTRAINT [PK_VENTAS] PRIMARY KEY CLUSTERED ([ID_VENTA])
);
