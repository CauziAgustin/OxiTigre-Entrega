/*
===============================================================================
Proyecto:              Sistema Modular de Gestión OxiTigre
Componente:            Seed de tipos de teléfono
Archivo:               04_TIPOS_TELEFONO.sql
Versión:               1.0.0
Fecha:                 2026-08-19
ID pedido:             FABRICA
Desarrollador:         Agustin Omar Cauzi
Correo:                agustincauzi10@hotmail.com
Descripción funcional: Registra las clasificaciones iniciales de teléfonos.
Historial de modificaciones:
1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
MERGE [CONFIGURACION].[TIPOS_TELEFONO] AS [DESTINO]
USING (VALUES
    (N'MOVIL', N'Móvil'), (N'FIJO', N'Fijo'), (N'LABORAL', N'Laboral'),
    (N'WHATSAPP', N'WhatsApp'), (N'EMERGENCIA', N'Emergencia')
) AS [ORIGEN] ([CODIGO], [NOMBRE])
ON [DESTINO].[CODIGO] = [ORIGEN].[CODIGO]
WHEN NOT MATCHED THEN
    INSERT ([CODIGO], [NOMBRE], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
    VALUES ([ORIGEN].[CODIGO], [ORIGEN].[NOMBRE], N'ACTIVO', 1);
