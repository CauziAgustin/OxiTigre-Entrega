/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: Seed de traducciones iniciales
Archivo: 08_TRADUCCIONES_INICIALES.sql | Versión: 1.0.0 | Fecha: 2026-08-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Registra nombres ingleses iniciales sin duplicar columnas de los catálogos.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
INSERT INTO [CONFIGURACION].[TRADUCCIONES_CATALOGO]
    ([ENTIDAD], [CODIGO], [CULTURA], [NOMBRE], [DESCRIPCION], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
SELECT [T].[ENTIDAD], [T].[CODIGO], N'en-US', [T].[NOMBRE], [T].[DESCRIPCION], N'ACTIVO', 1
FROM (VALUES
    (N'MODULOS', N'SEGURIDAD', N'Security', N'Users, authentication and permissions'),
    (N'MODULOS', N'CONFIGURACION', N'Configuration', N'Shared settings and catalogs'),
    (N'MODULOS', N'INVENTARIO', N'Inventory', N'Stock and movements'),
    (N'MODULOS', N'COMERCIAL', N'Commercial', N'Customers, sales and orders'),
    (N'MODULOS', N'COMPRAS', N'Purchasing', N'Suppliers and purchases'),
    (N'MODULOS', N'LOGISTICA', N'Logistics', N'Preparation and distribution'),
    (N'MODULOS', N'AUDITORIA', N'Audit', N'Errors and functional traceability'),
    (N'TIPOS_TELEFONO', N'MOVIL', N'Mobile', NULL),
    (N'TIPOS_TELEFONO', N'FIJO', N'Landline', NULL),
    (N'TIPOS_TELEFONO', N'LABORAL', N'Work', NULL),
    (N'TIPOS_TELEFONO', N'WHATSAPP', N'WhatsApp', NULL),
    (N'TIPOS_TELEFONO', N'EMERGENCIA', N'Emergency', NULL),
    (N'TIPOS_DOCUMENTO', N'DNI', N'National Identity Document', NULL),
    (N'TIPOS_DOCUMENTO', N'CUIL', N'Labor Identification Code', NULL),
    (N'TIPOS_DOCUMENTO', N'CUIT', N'Tax Identification Code', NULL),
    (N'TIPOS_DOCUMENTO', N'PASAPORTE', N'Passport', NULL),
    (N'TIPOS_DOCUMENTO', N'CDI', N'Identification Code', NULL),
    (N'PAISES', N'AR', N'Argentina', NULL),
    (N'PAISES', N'OTRO', N'Other country (manual code)', NULL)
) AS [T] ([ENTIDAD], [CODIGO], [NOMBRE], [DESCRIPCION])
WHERE NOT EXISTS
(
    SELECT 1 FROM [CONFIGURACION].[TRADUCCIONES_CATALOGO] AS [A]
    WHERE [A].[ENTIDAD] = [T].[ENTIDAD] AND [A].[CODIGO] = [T].[CODIGO] AND [A].[CULTURA] = N'en-US'
);

INSERT INTO [CONFIGURACION].[TRADUCCIONES_CATALOGO]
    ([ENTIDAD], [CODIGO], [CULTURA], [NOMBRE], [DESCRIPCION], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
SELECT N'ESTADOS:' + [E].[ENTIDAD], [E].[CODIGO_ESTADO], N'en-US', [T].[NOMBRE], [T].[DESCRIPCION], N'ACTIVO', 1
FROM [CONFIGURACION].[ESTADOS] AS [E]
INNER JOIN (VALUES
    (N'ACTIVO', N'Active', N'Record available for functional use.'),
    (N'INACTIVO', N'Inactive', N'Record retained but unavailable for new operations.'),
    (N'VIGENTE', N'Valid', N'Authenticated session available for use.'),
    (N'CERRADA', N'Closed', N'Session closed normally.'),
    (N'REVOCADA', N'Revoked', N'Session invalidated administratively.'),
    (N'EXPIRADA', N'Expired', N'Record outside its validity period.'),
    (N'PENDIENTE', N'Pending', N'Record pending processing.'),
    (N'UTILIZADA', N'Used', N'Record consumed successfully.'),
    (N'BORRADOR', N'Draft', N'Incomplete data available to continue.'),
    (N'DESCARTADO', N'Discarded', N'Record used or discarded by the user.'),
    (N'REGISTRADO', N'Registered', N'Execution stored for audit.')
) AS [T] ([CODIGO], [NOMBRE], [DESCRIPCION]) ON [T].[CODIGO] = [E].[CODIGO_ESTADO]
WHERE NOT EXISTS
(
    SELECT 1 FROM [CONFIGURACION].[TRADUCCIONES_CATALOGO] AS [A]
    WHERE [A].[ENTIDAD] = N'ESTADOS:' + [E].[ENTIDAD] AND [A].[CODIGO] = [E].[CODIGO_ESTADO] AND [A].[CULTURA] = N'en-US'
);
