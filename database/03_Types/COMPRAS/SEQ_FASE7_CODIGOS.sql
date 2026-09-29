/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: Secuencias de Compras y trazabilidad
Archivo: SEQ_FASE7_CODIGOS.sql | Versión: 1.0.0 | Fecha: 2026-08-24 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Numera documentos funcionales de la Fase 7 sin depender de la PK.
Historial: 1.0.0 | 2026-08-24 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
CREATE SEQUENCE [COMPRAS].[SEQ_PROVEEDOR_CODIGO] AS BIGINT START WITH 1 INCREMENT BY 1;
CREATE SEQUENCE [COMPRAS].[SEQ_ORDEN_COMPRA_CODIGO] AS BIGINT START WITH 1 INCREMENT BY 1;
CREATE SEQUENCE [COMPRAS].[SEQ_RECEPCION_CODIGO] AS BIGINT START WITH 1 INCREMENT BY 1;
CREATE SEQUENCE [INVENTARIO].[SEQ_ACTIVO_CODIGO] AS BIGINT START WITH 1 INCREMENT BY 1;
CREATE SEQUENCE [INVENTARIO].[SEQ_TRANSFORMACION_CODIGO] AS BIGINT START WITH 1 INCREMENT BY 1;
CREATE SEQUENCE [INVENTARIO].[SEQ_INCIDENTE_CODIGO] AS BIGINT START WITH 1 INCREMENT BY 1;
CREATE SEQUENCE [INVENTARIO].[SEQ_MANTENIMIENTO_CODIGO] AS BIGINT START WITH 1 INCREMENT BY 1;
CREATE SEQUENCE [INVENTARIO].[SEQ_PRESTAMO_CODIGO] AS BIGINT START WITH 1 INCREMENT BY 1;
