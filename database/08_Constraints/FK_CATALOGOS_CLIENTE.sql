/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: Relaciones de borradores de clientes
Archivo: FK_CATALOGOS_CLIENTE.sql
Versión: 1.0.0
Fecha: 2026-08-20
ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi
Correo: agustincauzi10@hotmail.com
Descripción funcional: Aísla cada borrador por empresa y usuario funcional.
Historial de modificaciones:
1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
ALTER TABLE [COMERCIAL].[CLIENTES_BORRADORES] ADD CONSTRAINT [FK_CLIENTES_BORRADORES_EMPRESAS]
    FOREIGN KEY ([ID_EMPRESA]) REFERENCES [CONFIGURACION].[EMPRESAS] ([ID_EMPRESA]);
ALTER TABLE [COMERCIAL].[CLIENTES_BORRADORES] ADD CONSTRAINT [FK_CLIENTES_BORRADORES_USUARIOS]
    FOREIGN KEY ([ID_USUARIO]) REFERENCES [SEGURIDAD].[USUARIOS] ([ID_USUARIO]);
ALTER TABLE [COMERCIAL].[CLIENTES_BORRADORES] ADD CONSTRAINT [CK_CLIENTES_BORRADORES_JSON]
    CHECK (ISJSON([CONTENIDO_JSON]) = 1);
