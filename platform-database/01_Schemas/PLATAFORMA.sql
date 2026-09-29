/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: Schema de plataforma multiempresa
Archivo: PLATAFORMA.sql | Versión: 1.0.0 | Fecha: 2026-08-27 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Crea el espacio central que registra las bases operativas autorizadas.
Historial: 1.0.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
IF SCHEMA_ID(N'PLATAFORMA') IS NULL
    EXEC (N'CREATE SCHEMA [PLATAFORMA] AUTHORIZATION [dbo];');
