/*
===============================================================================
Proyecto:              Sistema Modular de Gestión OxiTigre
Componente:            Seed de autorización
Archivo:               05_ROLES_PERMISOS.sql
Versión:               11.0.0
Fecha:                 2026-08-27
ID pedido:             FABRICA
Desarrollador:         Agustin Omar Cauzi
Correo:                agustincauzi10@hotmail.com
Descripción funcional: Registra roles y permisos mínimos de administración y consulta.
Historial de modificaciones:
1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
1.1.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Permiso exclusivo COMERCIAL.GESTIONAR para administradores.
1.2.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Gestión restringida de Configuración y catálogo de errores.
1.3.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Consulta y gestión separadas para Inventario.
1.4.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Gestión separada para pedidos, precios y ventas.
1.5.0 | 2026-08-26 | FABRICA | Agustin Omar Cauzi | Consulta, gestión y despacho del circuito logístico.
1.6.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Rol transportista con acceso operativo acotado.
11.0.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Rol cajero y permisos financieros segregados.
===============================================================================
*/
DECLARE @V_ID_EMPRESA BIGINT = (SELECT [ID_EMPRESA] FROM [CONFIGURACION].[EMPRESAS] WHERE [CODIGO] = N'OXITIGRE');

INSERT INTO [SEGURIDAD].[ROLES] ([ID_EMPRESA], [CODIGO], [NOMBRE], [DESCRIPCION], [ES_SISTEMA], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
SELECT @V_ID_EMPRESA, [R].[CODIGO], [R].[NOMBRE], [R].[DESCRIPCION], 1, N'ACTIVO', 1
FROM (VALUES
    (N'ADMINISTRADOR', N'Administrador', N'Administración funcional completa de la empresa.'),
    (N'CONSULTA', N'Consulta', N'Acceso de solo lectura autorizado.'),
    (N'TRANSPORTISTA', N'Transportista', N'Acceso al despacho y confirmación de recorridos asignados.'),
    (N'CAJERO', N'Cajero', N'Acceso a la apertura, cobro y cierre de su propia caja.')
) AS [R] ([CODIGO], [NOMBRE], [DESCRIPCION])
WHERE NOT EXISTS
(
    SELECT 1 FROM [SEGURIDAD].[ROLES] AS [ACT]
    WHERE [ACT].[ID_EMPRESA] = @V_ID_EMPRESA AND [ACT].[CODIGO] = [R].[CODIGO]
);

INSERT INTO [SEGURIDAD].[PERMISOS]
    ([ID_MODULO], [CODIGO], [NOMBRE], [DESCRIPCION], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
SELECT [M].[ID_MODULO], N'COMERCIAL.GESTIONAR', N'Gestionar clientes',
       N'Permite crear, editar y cambiar el estado de clientes y teléfonos.', N'ACTIVO', 1
FROM [CONFIGURACION].[MODULOS] AS [M]
WHERE [M].[CODIGO] = N'COMERCIAL'
  AND NOT EXISTS (SELECT 1 FROM [SEGURIDAD].[PERMISOS] WHERE [CODIGO] = N'COMERCIAL.GESTIONAR');

INSERT INTO [SEGURIDAD].[PERMISOS]
    ([ID_MODULO], [CODIGO], [NOMBRE], [DESCRIPCION], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
SELECT [M].[ID_MODULO], [P].[CODIGO], [P].[NOMBRE], [P].[DESCRIPCION], N'ACTIVO', 1
FROM (VALUES
    (N'CONFIGURACION', N'CONFIGURACION.GESTIONAR', N'Gestionar configuración', N'Permite administrar estructura, catálogos, parámetros y módulos.'),
    (N'AUDITORIA', N'AUDITORIA.GESTIONAR_ERRORES', N'Gestionar catálogo de errores', N'Permite crear y mantener códigos de error conocidos.'),
    (N'INVENTARIO', N'INVENTARIO.GESTIONAR', N'Gestionar inventario', N'Permite mantener maestros, mínimos y confirmar movimientos de stock.'),
    (N'COMERCIAL', N'COMERCIAL.VENTAS_GESTIONAR', N'Gestionar pedidos y ventas', N'Permite mantener listas, confirmar pedidos y registrar ventas internas.'),
    (N'COMPRAS', N'COMPRAS.GESTIONAR', N'Gestionar compras', N'Permite mantener proveedores y órdenes en borrador.'),
    (N'COMPRAS', N'COMPRAS.APROBAR', N'Aprobar compras', N'Permite aprobar órdenes de compra pendientes.'),
    (N'COMPRAS', N'COMPRAS.RECIBIR', N'Recibir compras', N'Permite confirmar recepciones y cerrar saldos pendientes.'),
    (N'FINANZAS', N'FINANZAS.COBRAR', N'Registrar cobros', N'Permite registrar cobros y aplicarlos a ventas internas.'),
    (N'FINANZAS', N'FINANZAS.CAJA_GESTIONAR', N'Gestionar caja', N'Permite abrir y cerrar la caja propia con arqueo.'),
    (N'FINANZAS', N'FINANZAS.REVERSAR', N'Reversar cobros', N'Permite realizar una reversión compensatoria auditada.'),
    (N'FINANZAS', N'FINANZAS.CONFIGURAR', N'Configurar finanzas', N'Permite mantener cajas físicas y medios de pago.')
) AS [P] ([MODULO], [CODIGO], [NOMBRE], [DESCRIPCION])
INNER JOIN [CONFIGURACION].[MODULOS] AS [M] ON [M].[CODIGO] = [P].[MODULO]
WHERE NOT EXISTS (SELECT 1 FROM [SEGURIDAD].[PERMISOS] WHERE [CODIGO] = [P].[CODIGO]);

INSERT INTO [SEGURIDAD].[PERMISOS] ([ID_MODULO], [CODIGO], [NOMBRE], [DESCRIPCION], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
SELECT [M].[ID_MODULO], CONCAT([M].[CODIGO], N'.CONSULTAR'), CONCAT(N'Consultar ', [M].[NOMBRE]),
       N'Permite consultar información del módulo.', N'ACTIVO', 1
FROM [CONFIGURACION].[MODULOS] AS [M]
WHERE NOT EXISTS
(
    SELECT 1 FROM [SEGURIDAD].[PERMISOS] AS [P]
    WHERE [P].[CODIGO] = CONCAT([M].[CODIGO], N'.CONSULTAR')
);

INSERT INTO [SEGURIDAD].[ROLES_PERMISOS] ([ID_ROL], [ID_PERMISO], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
SELECT [R].[ID_ROL], [P].[ID_PERMISO], N'ACTIVO', 1
FROM [SEGURIDAD].[ROLES] AS [R]
CROSS JOIN [SEGURIDAD].[PERMISOS] AS [P]
WHERE [R].[ID_EMPRESA] = @V_ID_EMPRESA
  AND
  (
      [R].[CODIGO] = N'ADMINISTRADOR'
      OR ([R].[CODIGO] = N'CONSULTA' AND [P].[CODIGO] LIKE N'%.CONSULTAR')
  )
  AND NOT EXISTS
  (
      SELECT 1 FROM [SEGURIDAD].[ROLES_PERMISOS] AS [RP]
      WHERE [RP].[ID_ROL] = [R].[ID_ROL] AND [RP].[ID_PERMISO] = [P].[ID_PERMISO]
        AND [RP].[CODIGO_ESTADO] = N'ACTIVO'
  );

INSERT INTO [SEGURIDAD].[ROLES_PERMISOS]
    ([ID_ROL], [ID_PERMISO], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
SELECT [R].[ID_ROL], [P].[ID_PERMISO], N'ACTIVO', 1
FROM [SEGURIDAD].[ROLES] AS [R]
INNER JOIN [SEGURIDAD].[PERMISOS] AS [P] ON [P].[CODIGO] = N'COMERCIAL.GESTIONAR'
WHERE [R].[ID_EMPRESA] = @V_ID_EMPRESA AND [R].[CODIGO] = N'ADMINISTRADOR'
  AND NOT EXISTS
  (
      SELECT 1 FROM [SEGURIDAD].[ROLES_PERMISOS] AS [RP]
      WHERE [RP].[ID_ROL] = [R].[ID_ROL] AND [RP].[ID_PERMISO] = [P].[ID_PERMISO]
        AND [RP].[CODIGO_ESTADO] = N'ACTIVO'
  );

-- INICIO: Permisos operativos de Logística Fase 8.
INSERT INTO [SEGURIDAD].[PERMISOS]
    ([ID_MODULO], [CODIGO], [NOMBRE], [DESCRIPCION], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
SELECT [M].[ID_MODULO], [P].[CODIGO], [P].[NOMBRE], [P].[DESCRIPCION], N'ACTIVO', 1
FROM (VALUES
    (N'LOGISTICA.GESTIONAR', N'Gestionar logística', N'Permite mantener domicilios, solicitudes y hojas planificadas.'),
    (N'LOGISTICA.DESPACHAR', N'Despachar logística', N'Permite despachar rutas y confirmar el resultado de sus paradas.')
) AS [P] ([CODIGO], [NOMBRE], [DESCRIPCION])
CROSS JOIN (SELECT [ID_MODULO] FROM [CONFIGURACION].[MODULOS] WHERE [CODIGO] = N'LOGISTICA') AS [M]
WHERE NOT EXISTS (SELECT 1 FROM [SEGURIDAD].[PERMISOS] WHERE [CODIGO] = [P].[CODIGO]);

INSERT INTO [SEGURIDAD].[ROLES_PERMISOS] ([ID_ROL], [ID_PERMISO], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
SELECT [R].[ID_ROL], [P].[ID_PERMISO], N'ACTIVO', 1
FROM [SEGURIDAD].[ROLES] AS [R]
INNER JOIN [SEGURIDAD].[PERMISOS] AS [P] ON [P].[CODIGO] IN (N'LOGISTICA.GESTIONAR', N'LOGISTICA.DESPACHAR')
WHERE [R].[ID_EMPRESA] = @V_ID_EMPRESA AND [R].[CODIGO] = N'ADMINISTRADOR'
  AND NOT EXISTS (SELECT 1 FROM [SEGURIDAD].[ROLES_PERMISOS] AS [RP]
                  WHERE [RP].[ID_ROL] = [R].[ID_ROL] AND [RP].[ID_PERMISO] = [P].[ID_PERMISO]
                    AND [RP].[CODIGO_ESTADO] = N'ACTIVO');
-- FIN: Permisos operativos de Logística Fase 8.

INSERT INTO [SEGURIDAD].[ROLES_PERMISOS]
    ([ID_ROL], [ID_PERMISO], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
SELECT [R].[ID_ROL], [P].[ID_PERMISO], N'ACTIVO', 1
FROM [SEGURIDAD].[ROLES] AS [R]
INNER JOIN [SEGURIDAD].[PERMISOS] AS [P]
    ON [P].[CODIGO] IN (N'LOGISTICA.CONSULTAR', N'LOGISTICA.DESPACHAR')
WHERE [R].[ID_EMPRESA] = @V_ID_EMPRESA
  AND [R].[CODIGO] = N'TRANSPORTISTA'
  AND NOT EXISTS
  (
      SELECT 1 FROM [SEGURIDAD].[ROLES_PERMISOS] AS [RP]
      WHERE [RP].[ID_ROL] = [R].[ID_ROL]
        AND [RP].[ID_PERMISO] = [P].[ID_PERMISO]
        AND [RP].[CODIGO_ESTADO] = N'ACTIVO'
  );

-- INICIO: Un cajero consulta Finanzas, cobra y administra exclusivamente su apertura.
INSERT INTO [SEGURIDAD].[ROLES_PERMISOS]
    ([ID_ROL], [ID_PERMISO], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
SELECT [R].[ID_ROL], [P].[ID_PERMISO], N'ACTIVO', 1
FROM [SEGURIDAD].[ROLES] AS [R]
INNER JOIN [SEGURIDAD].[PERMISOS] AS [P]
    ON [P].[CODIGO] IN (N'FINANZAS.CONSULTAR', N'FINANZAS.COBRAR', N'FINANZAS.CAJA_GESTIONAR')
WHERE [R].[ID_EMPRESA] = @V_ID_EMPRESA AND [R].[CODIGO] = N'CAJERO'
  AND NOT EXISTS
  (
      SELECT 1 FROM [SEGURIDAD].[ROLES_PERMISOS] AS [RP]
      WHERE [RP].[ID_ROL] = [R].[ID_ROL] AND [RP].[ID_PERMISO] = [P].[ID_PERMISO]
        AND [RP].[CODIGO_ESTADO] = N'ACTIVO'
  );
-- FIN: Un cajero consulta Finanzas, cobra y administra exclusivamente su apertura.
