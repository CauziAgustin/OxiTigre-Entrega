/*
===============================================================================
Proyecto:              Sistema Modular de Gestión OxiTigre
Componente:            CK_CLIENTES_TELEFONOS
Archivo:               CK_CLIENTES_TELEFONOS.sql
Versión:               1.0.0
Fecha:                 2026-08-19
ID pedido:             FABRICA
Desarrollador:         Agustin Omar Cauzi
Correo:                agustincauzi10@hotmail.com
Descripción funcional: Valida orden y contenido mínimo del teléfono del cliente.
Historial de modificaciones:
Versión | Fecha      | ID pedido | Desarrollador      | Correo                      | Descripción
1.0.0   | 2026-08-19 | FABRICA   | Agustin Omar Cauzi | agustincauzi10@hotmail.com  | Creación inicial.
===============================================================================
*/
ALTER TABLE [COMERCIAL].[CLIENTES_TELEFONOS]
    ADD CONSTRAINT [CK_CLIENTES_TELEFONOS_ORDEN]
        CHECK ([ORDEN] >= 0);

ALTER TABLE [COMERCIAL].[CLIENTES_TELEFONOS]
    ADD CONSTRAINT [CK_CLIENTES_TELEFONOS_NUMERO]
        CHECK (LEN(LTRIM(RTRIM([NUMERO]))) > 0);
