/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: COMERCIAL.SEQ_CLIENTE_CODIGO
Archivo: SEQ_CLIENTE_CODIGO.sql | Versión: 1.0.0 | Fecha: 2026-08-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Genera el consecutivo visible de clientes dentro de la base empresarial.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
IF NOT EXISTS
(
    SELECT 1 FROM [sys].[sequences]
    WHERE [name] = N'SEQ_CLIENTE_CODIGO' AND [schema_id] = SCHEMA_ID(N'COMERCIAL')
)
BEGIN
    EXEC(N'CREATE SEQUENCE [COMERCIAL].[SEQ_CLIENTE_CODIGO] AS BIGINT START WITH 1 INCREMENT BY 1;');
END;
