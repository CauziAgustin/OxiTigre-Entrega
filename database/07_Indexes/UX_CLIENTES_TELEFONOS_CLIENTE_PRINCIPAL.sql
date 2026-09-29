/*
===============================================================================
Proyecto:              Sistema Modular de Gestión OxiTigre
Componente:            UX_CLIENTES_TELEFONOS_CLIENTE_PRINCIPAL
Archivo:               UX_CLIENTES_TELEFONOS_CLIENTE_PRINCIPAL.sql
Versión:               1.0.0
Fecha:                 2026-08-19
ID pedido:             FABRICA
Desarrollador:         Agustin Omar Cauzi
Correo:                agustincauzi10@hotmail.com
Descripción funcional: Garantiza un solo teléfono principal activo por cliente.
Historial de modificaciones:
Versión | Fecha      | ID pedido | Desarrollador      | Correo                      | Descripción
1.0.0   | 2026-08-19 | FABRICA   | Agustin Omar Cauzi | agustincauzi10@hotmail.com  | Creación inicial.
===============================================================================
*/
CREATE UNIQUE INDEX [UX_CLIENTES_TELEFONOS_CLIENTE_PRINCIPAL]
    ON [COMERCIAL].[CLIENTES_TELEFONOS] ([ID_CLIENTE])
    WHERE [CODIGO_ESTADO] = N'ACTIVO' AND [ES_PRINCIPAL] = 1;
