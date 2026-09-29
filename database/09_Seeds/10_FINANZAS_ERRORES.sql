/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: Catálogo de errores financieros
Archivo: 10_FINANZAS_ERRORES.sql | Versión: 11.0.0 | Fecha: 2026-08-27 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Registra errores estables de caja, cobros y cuenta corriente.
Historial: 11.0.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
INSERT INTO [AUDITORIA].[CATALOGO_ERRORES]
    ([ID_MODULO], [NUMERO_ERROR], [CODIGO_ERROR], [NOMBRE], [DESCRIPCION], [CAUSA_PROBABLE],
     [ACCION_RECOMENDADA], [SEVERIDAD], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
SELECT [MOD].[ID_MODULO], [ERR].[NUMERO], [ERR].[CODIGO], [ERR].[NOMBRE], [ERR].[DESCRIPCION],
       [ERR].[CAUSA], [ERR].[ACCION], [ERR].[SEVERIDAD], N'ACTIVO', 1
FROM (VALUES
    (CONVERT(SMALLINT, 1), CONVERT(BIGINT, 80001), N'VALIDACIÓN FINANCIERA',
     N'Los datos del movimiento financiero no son válidos.', N'Campos incompletos o importes inconsistentes.',
     N'Revisar cliente, moneda, importes y medios.', N'ADVERTENCIA'),
    (CONVERT(SMALLINT, 2), CONVERT(BIGINT, 80002), N'CONFLICTO DE CAJA',
     N'La caja o el registro cambió desde la consulta.', N'Apertura duplicada, caja cerrada o concurrencia.',
     N'Actualizar la pantalla y revisar la apertura vigente.', N'ADVERTENCIA'),
    (CONVERT(SMALLINT, 3), CONVERT(BIGINT, 80003), N'APLICACIÓN DE COBRO INVÁLIDA',
     N'El cobro no puede aplicarse a la venta indicada.', N'Saldo, cliente o moneda incompatible.',
     N'Revisar el saldo pendiente de las ventas seleccionadas.', N'ADVERTENCIA'),
    (CONVERT(SMALLINT, 4), CONVERT(BIGINT, 80004), N'REVERSIÓN NO DISPONIBLE',
     N'El cobro no puede revertirse en su estado actual.', N'Cobro ya revertido o caja cerrada.',
     N'Revisar el historial y registrar una corrección en una caja abierta.', N'ADVERTENCIA')
) AS [ERR] ([NUMERO], [CODIGO], [NOMBRE], [DESCRIPCION], [CAUSA], [ACCION], [SEVERIDAD])
CROSS JOIN (SELECT [ID_MODULO] FROM [CONFIGURACION].[MODULOS] WHERE [CODIGO] = N'FINANZAS') AS [MOD]
WHERE NOT EXISTS (SELECT 1 FROM [AUDITORIA].[CATALOGO_ERRORES] WHERE [CODIGO_ERROR] = [ERR].[CODIGO]);
