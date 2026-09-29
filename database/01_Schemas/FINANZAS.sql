/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: Esquema Finanzas
Archivo: FINANZAS.sql | Versión: 11.0.0 | Fecha: 2026-08-27 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Aísla caja, cobros y cuenta corriente de la facturación fiscal.
Historial: 11.0.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
IF SCHEMA_ID(N'FINANZAS') IS NULL
BEGIN
    EXEC sys.sp_executesql N'CREATE SCHEMA [FINANZAS] AUTHORIZATION [dbo];';
END;
