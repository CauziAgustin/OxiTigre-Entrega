/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: Smoke de Finanzas Fase 11
Archivo: SMOKE_FINANZAS.sql | Versión: 11.0.0 | Fecha: 2026-08-27 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Verifica saldos, distribución de cobros y trazabilidad compensatoria.
Historial: 11.0.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
SET NOCOUNT ON;

IF SCHEMA_ID(N'FINANZAS') IS NULL OR OBJECT_ID(N'FINANZAS.PAGOS', N'U') IS NULL
    THROW 50000, N'No se instaló el núcleo financiero.', 1;

IF EXISTS
(
    SELECT 1
    FROM [FINANZAS].[PAGOS] AS [PAG]
    OUTER APPLY (SELECT SUM([IMPORTE]) AS [TOTAL] FROM [FINANZAS].[PAGOS_MEDIOS] WHERE [ID_PAGO] = [PAG].[ID_PAGO]) AS [MED]
    WHERE ABS([PAG].[TOTAL] - COALESCE([MED].[TOTAL], 0)) > 0.0001
)
    THROW 50000, N'Existe un cobro cuyo detalle de medios no coincide con el total.', 1;

IF EXISTS
(
    SELECT 1 FROM [FINANZAS].[APLICACIONES_PAGO_VENTA] AS [APL]
    INNER JOIN [FINANZAS].[PAGOS] AS [PAG] ON [PAG].[ID_PAGO] = [APL].[ID_PAGO]
    INNER JOIN [COMERCIAL].[VENTAS] AS [VEN] ON [VEN].[ID_VENTA] = [APL].[ID_VENTA]
    WHERE [PAG].[ID_CLIENTE] <> [VEN].[ID_CLIENTE] OR [PAG].[MONEDA] <> [VEN].[MONEDA]
)
    THROW 50000, N'Existe una aplicación incompatible con su venta.', 1;

IF EXISTS
(
    SELECT [ID_CAJA] FROM [FINANZAS].[SESIONES_CAJA]
    WHERE [CODIGO_ESTADO] = N'ABIERTA' GROUP BY [ID_CAJA] HAVING COUNT(*) > 1
)
    THROW 50000, N'Existe más de una apertura para la misma caja.', 1;

IF EXISTS
(
    SELECT [ID_VENTA] FROM [FINANZAS].[MOVIMIENTOS_CUENTA]
    WHERE [ORIGEN] = N'VENTA' GROUP BY [ID_VENTA] HAVING COUNT(*) > 1
)
    THROW 50000, N'Una venta generó más de un débito de cuenta corriente.', 1;

PRINT N'SMOKE_FINANZAS_OK';
