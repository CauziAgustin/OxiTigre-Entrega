/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: INVENTARIO.MOVIMIENTOS
Archivo: MOVIMIENTOS.sql | Versión: 1.0.0 | Fecha: 2026-08-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Registra el encabezado inmutable de cada movimiento confirmado.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
CREATE TABLE [INVENTARIO].[MOVIMIENTOS]
(
    [ID_MOVIMIENTO] BIGINT IDENTITY(1,1) NOT NULL,
    [ID_EMPRESA] BIGINT NOT NULL,
    [CODIGO] NVARCHAR(30) NOT NULL,
    [TIPO_MOVIMIENTO] NVARCHAR(30) NOT NULL,
    [FECHA_MOVIMIENTO_UTC] DATETIME2(3) NOT NULL,
    [OBSERVACION] NVARCHAR(500) NULL,
    [CODIGO_ESTADO] NVARCHAR(30) NOT NULL CONSTRAINT [DF_MOVIMIENTOS_CODIGO_ESTADO] DEFAULT (N'CONFIRMADO'),
    [FECHA_ALTA_UTC] DATETIME2(3) NOT NULL CONSTRAINT [DF_MOVIMIENTOS_FECHA_ALTA_UTC] DEFAULT (SYSUTCDATETIME()),
    [ID_USUARIO_ALTA] BIGINT NOT NULL,
    [FECHA_MODIFICACION_UTC] DATETIME2(3) NULL,
    [ID_USUARIO_MODIFICACION] BIGINT NULL,
    [ROW_VERSION] ROWVERSION NOT NULL,
    CONSTRAINT [PK_MOVIMIENTOS] PRIMARY KEY CLUSTERED ([ID_MOVIMIENTO])
);
