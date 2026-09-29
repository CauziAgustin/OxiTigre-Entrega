/*
===============================================================================
Proyecto:              Sistema Modular de Gestión OxiTigre
Componente:            Seed de módulos
Archivo:               01_MODULOS.sql
Versión:               1.0.0
Fecha:                 2026-08-19
ID pedido:             FABRICA
Desarrollador:         Agustin Omar Cauzi
Correo:                agustincauzi10@hotmail.com
Descripción funcional: Registra los módulos iniciales y su prefijo de error.
Historial de modificaciones:
1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
MERGE [CONFIGURACION].[MODULOS] AS [DESTINO]
USING
(
    VALUES
        (1, N'SEGURIDAD', N'Seguridad', N'Usuarios, autenticación y permisos', 0),
        (2, N'CONFIGURACION', N'Configuración', N'Parámetros y catálogos comunes', 1),
        (3, N'INVENTARIO', N'Inventario', N'Existencias y movimientos', 2),
        (4, N'COMERCIAL', N'Comercial', N'Clientes, ventas y pedidos', 3),
        (5, N'COMPRAS', N'Compras', N'Proveedores y adquisiciones', 4),
        (6, N'LOGISTICA', N'Logística', N'Preparación y distribución', 5),
        (7, N'AUDITORIA', N'Auditoría', N'Errores y trazabilidad funcional', 6),
        (8, N'FINANZAS', N'Finanzas', N'Cajas, cobros y cuenta corriente no fiscal', 7),
        (9, N'FISCAL', N'Facturación fiscal', N'Preparación y autorización tributaria de comprobantes', 8)
) AS [ORIGEN] ([NUMERO_MODULO], [CODIGO], [NOMBRE], [DESCRIPCION], [ORDEN])
ON [DESTINO].[CODIGO] = [ORIGEN].[CODIGO]
WHEN NOT MATCHED THEN
    INSERT ([NUMERO_MODULO], [CODIGO], [NOMBRE], [DESCRIPCION], [ORDEN], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
    VALUES ([ORIGEN].[NUMERO_MODULO], [ORIGEN].[CODIGO], [ORIGEN].[NOMBRE], [ORIGEN].[DESCRIPCION], [ORIGEN].[ORDEN], N'ACTIVO', 1);
