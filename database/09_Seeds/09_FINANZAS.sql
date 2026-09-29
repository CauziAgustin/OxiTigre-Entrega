/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: Catálogo financiero inicial
Archivo: 09_FINANZAS.sql | Versión: 11.0.0 | Fecha: 2026-08-27 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Registra medios de pago básicos sin credenciales ni proveedores externos.
Historial: 11.0.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
DECLARE @V_ID_EMPRESA BIGINT = (SELECT [ID_EMPRESA] FROM [CONFIGURACION].[EMPRESAS] WHERE [CODIGO] = N'OXITIGRE');

INSERT INTO [FINANZAS].[MEDIOS_PAGO]
    ([ID_EMPRESA], [CODIGO], [NOMBRE], [TIPO], [AFECTA_EFECTIVO], [REQUIERE_REFERENCIA], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
SELECT @V_ID_EMPRESA, [DAT].[CODIGO], [DAT].[NOMBRE], [DAT].[TIPO], [DAT].[AFECTA_EFECTIVO],
       [DAT].[REQUIERE_REFERENCIA], N'ACTIVO', 1
FROM (VALUES
    (N'EFECTIVO', N'Efectivo', N'EFECTIVO', CONVERT(BIT, 1), CONVERT(BIT, 0)),
    (N'TRANSFERENCIA', N'Transferencia bancaria', N'TRANSFERENCIA', CONVERT(BIT, 0), CONVERT(BIT, 1)),
    (N'TARJETA', N'Tarjeta', N'TARJETA', CONVERT(BIT, 0), CONVERT(BIT, 1))
) AS [DAT] ([CODIGO], [NOMBRE], [TIPO], [AFECTA_EFECTIVO], [REQUIERE_REFERENCIA])
WHERE @V_ID_EMPRESA IS NOT NULL AND NOT EXISTS
(
    SELECT 1 FROM [FINANZAS].[MEDIOS_PAGO]
    WHERE [ID_EMPRESA] = @V_ID_EMPRESA AND [CODIGO] = [DAT].[CODIGO]
);

-- INICIO: Toda venta previa se incorpora una sola vez al saldo inicial.
INSERT INTO [FINANZAS].[MOVIMIENTOS_CUENTA]
    ([ID_EMPRESA], [ID_CLIENTE], [ID_VENTA], [TIPO_MOVIMIENTO], [ORIGEN],
     [FECHA_MOVIMIENTO_UTC], [MONEDA], [IMPORTE], [DESCRIPCION], [ID_USUARIO_ALTA])
SELECT [VEN].[ID_EMPRESA], [VEN].[ID_CLIENTE], [VEN].[ID_VENTA], N'DEBITO', N'VENTA',
       [VEN].[FECHA_VENTA_UTC], [VEN].[MONEDA], [VEN].[TOTAL], N'Venta interna ' + [VEN].[CODIGO], [VEN].[ID_USUARIO_ALTA]
FROM [COMERCIAL].[VENTAS] AS [VEN]
WHERE NOT EXISTS
(
    SELECT 1 FROM [FINANZAS].[MOVIMIENTOS_CUENTA]
    WHERE [ID_VENTA] = [VEN].[ID_VENTA] AND [ORIGEN] = N'VENTA'
);
-- FIN: Toda venta previa se incorpora una sola vez al saldo inicial.
