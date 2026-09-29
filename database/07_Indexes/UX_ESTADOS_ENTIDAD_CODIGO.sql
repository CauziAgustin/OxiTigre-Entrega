/*
===============================================================================
Proyecto:              Sistema Modular de Gestión OxiTigre
Componente:            UX_ESTADOS_ENTIDAD_CODIGO
Archivo:               UX_ESTADOS_ENTIDAD_CODIGO.sql
Versión:               1.0.0
Fecha:                 2026-08-19
ID pedido:             FABRICA
Desarrollador:         Agustin Omar Cauzi
Correo:                agustincauzi10@hotmail.com
Descripción funcional: Evita códigos de estado duplicados dentro de una entidad.
Historial de modificaciones:
Versión | Fecha      | ID pedido | Desarrollador      | Correo                      | Descripción
1.0.0   | 2026-08-19 | FABRICA   | Agustin Omar Cauzi | agustincauzi10@hotmail.com  | Creación inicial.
===============================================================================
*/
CREATE UNIQUE INDEX [UX_ESTADOS_ENTIDAD_CODIGO]
    ON [CONFIGURACION].[ESTADOS] ([ENTIDAD], [CODIGO_ESTADO]);
