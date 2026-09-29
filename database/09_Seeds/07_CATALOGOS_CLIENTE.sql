/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: Seed de catálogos de clientes
Archivo: 07_CATALOGOS_CLIENTE.sql
Versión: 1.0.0
Fecha: 2026-08-20
ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi
Correo: agustincauzi10@hotmail.com
Descripción funcional: Registra documentos argentinos y selección de país inicial.
Historial de modificaciones:
1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
INSERT INTO [CONFIGURACION].[TIPOS_DOCUMENTO]
    ([CODIGO], [NOMBRE], [APLICA_PERSONA_FISICA], [APLICA_PERSONA_JURIDICA], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
SELECT [T].[CODIGO], [T].[NOMBRE], [T].[FISICA], [T].[JURIDICA], N'ACTIVO', 1
FROM (VALUES
    (N'DNI', N'Documento Nacional de Identidad', CONVERT(BIT, 1), CONVERT(BIT, 0)),
    (N'CUIL', N'Código Único de Identificación Laboral', CONVERT(BIT, 1), CONVERT(BIT, 0)),
    (N'CUIT', N'Clave Única de Identificación Tributaria', CONVERT(BIT, 1), CONVERT(BIT, 1)),
    (N'PASAPORTE', N'Pasaporte', CONVERT(BIT, 1), CONVERT(BIT, 0)),
    (N'CDI', N'Clave de Identificación', CONVERT(BIT, 1), CONVERT(BIT, 1))
) AS [T] ([CODIGO], [NOMBRE], [FISICA], [JURIDICA])
WHERE NOT EXISTS (SELECT 1 FROM [CONFIGURACION].[TIPOS_DOCUMENTO] WHERE [CODIGO] = [T].[CODIGO]);

INSERT INTO [CONFIGURACION].[PAISES]
    ([CODIGO], [NOMBRE], [CODIGO_TELEFONICO], [ES_PREDETERMINADO], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
SELECT [P].[CODIGO], [P].[NOMBRE], [P].[CODIGO_TELEFONICO], [P].[PREDETERMINADO], N'ACTIVO', 1
FROM (VALUES
    (N'AR', N'Argentina', N'54', CONVERT(BIT, 1)),
    (N'OTRO', N'Otro país (código manual)', CONVERT(NVARCHAR(5), NULL), CONVERT(BIT, 0))
) AS [P] ([CODIGO], [NOMBRE], [CODIGO_TELEFONICO], [PREDETERMINADO])
WHERE NOT EXISTS (SELECT 1 FROM [CONFIGURACION].[PAISES] WHERE [CODIGO] = [P].[CODIGO]);
