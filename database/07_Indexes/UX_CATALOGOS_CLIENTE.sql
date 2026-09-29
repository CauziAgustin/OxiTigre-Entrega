/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: Índices de catálogos y borradores de clientes
Archivo: UX_CATALOGOS_CLIENTE.sql
Versión: 1.0.0
Fecha: 2026-08-20
ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi
Correo: agustincauzi10@hotmail.com
Descripción funcional: Evita catálogos, documentos y borradores activos duplicados.
Historial de modificaciones:
1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
CREATE UNIQUE INDEX [UX_TIPOS_DOCUMENTO_CODIGO] ON [CONFIGURACION].[TIPOS_DOCUMENTO] ([CODIGO]);
CREATE UNIQUE INDEX [UX_PAISES_CODIGO] ON [CONFIGURACION].[PAISES] ([CODIGO]);
CREATE UNIQUE INDEX [UX_PAISES_PREDETERMINADO] ON [CONFIGURACION].[PAISES] ([ES_PREDETERMINADO]) WHERE [ES_PREDETERMINADO] = 1 AND [CODIGO_ESTADO] = N'ACTIVO';
CREATE UNIQUE INDEX [UX_CLIENTES_DOCUMENTO] ON [COMERCIAL].[CLIENTES] ([ID_EMPRESA], [TIPO_DOCUMENTO], [NUMERO_DOCUMENTO]) WHERE [TIPO_DOCUMENTO] IS NOT NULL AND [NUMERO_DOCUMENTO] IS NOT NULL;
CREATE UNIQUE INDEX [UX_CLIENTES_BORRADORES_ACTIVO] ON [COMERCIAL].[CLIENTES_BORRADORES] ([ID_EMPRESA], [ID_USUARIO]) WHERE [CODIGO_ESTADO] = N'BORRADOR';
