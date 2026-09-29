/*
===============================================================================
Proyecto:              Sistema Modular de Gestión OxiTigre
Componente:            UX_CLIENTES_TELEFONOS_CLIENTE_ORDEN
Archivo:               UX_CLIENTES_TELEFONOS_CLIENTE_ORDEN.sql
Versión:               1.0.0
Fecha:                 2026-08-19
ID pedido:             FABRICA
Desarrollador:         Agustin Omar Cauzi
Correo:                agustincauzi10@hotmail.com
Descripción funcional: Evita repetir el orden de teléfonos activos de un cliente.
Historial de modificaciones:
Versión | Fecha      | ID pedido | Desarrollador      | Correo                      | Descripción
1.0.0   | 2026-08-19 | FABRICA   | Agustin Omar Cauzi | agustincauzi10@hotmail.com  | Creación inicial.
===============================================================================
*/
CREATE UNIQUE INDEX [UX_CLIENTES_TELEFONOS_CLIENTE_ORDEN]
    ON [COMERCIAL].[CLIENTES_TELEFONOS] ([ID_CLIENTE], [ORDEN])
    WHERE [CODIGO_ESTADO] = N'ACTIVO';
