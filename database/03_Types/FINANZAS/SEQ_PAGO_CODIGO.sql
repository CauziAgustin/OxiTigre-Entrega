/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: Secuencia de recibos internos
Archivo: SEQ_PAGO_CODIGO.sql | Versión: 11.0.0 | Fecha: 2026-08-27 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Numera recibos internos sin confundirlos con comprobantes fiscales.
Historial: 11.0.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
IF OBJECT_ID(N'[FINANZAS].[SEQ_PAGO_CODIGO]', N'SO') IS NULL
BEGIN
    EXEC sys.sp_executesql N'CREATE SEQUENCE [FINANZAS].[SEQ_PAGO_CODIGO] AS BIGINT START WITH 1 INCREMENT BY 1;';
END;
