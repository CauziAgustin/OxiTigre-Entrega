<#
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: scripts.database.Test-DatabaseDeployment
Archivo: Test-DatabaseDeployment.ps1 | Versión: 1.7.2 | Fecha: 2026-09-21 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Comprueba objetos y datos técnicos mínimos después de un despliegue.
Historial: 1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Verificación estructural de Inventario.
Historial: 1.2.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Verificación estructural de pedidos, reservas y ventas.
Historial: 1.3.0 | 2026-08-24 | FABRICA | Agustin Omar Cauzi | Verificación estructural de Compras y trazabilidad.
Historial: 1.4.0 | 2026-08-26 | FABRICA | Agustin Omar Cauzi | Verificación de snapshots y reversión de recepciones.
Historial: 1.5.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Verificación estructural de Logística por ambiente.
Historial: 1.6.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Aislamiento de datos por ambiente no productivo.
Historial: 1.7.0 | 2026-08-29 | FABRICA | Agustin Omar Cauzi | Verificación de Finanzas, preparación fiscal y operación móvil.
Historial: 1.7.1 | 2026-08-29 | FABRICA | Agustin Omar Cauzi | Verificación del bloqueo concurrente móvil.
Historial: 1.7.2 | 2026-09-21 | FABRICA | Agustin Omar Cauzi | Ajuste de la validación al alcance de escritorio de la entrega.
===============================================================================
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $ServerInstance,
    [Parameter(Mandatory)] [ValidatePattern('^[A-Za-z][A-Za-z0-9_]{0,127}$')] [string] $DatabaseName,
    [ValidateSet('Development', 'Testing', 'Sandbox', 'Production')] [string] $Environment = 'Development',
    [PSCredential] $SqlCredential
)

$ErrorActionPreference = 'Stop'
$arguments = @('-S', $ServerInstance, '-d', $DatabaseName, '-b', '-r', '1', '-C', '-I', '-f', '65001')
if ($null -eq $SqlCredential) {
    $arguments += '-E'
}
else {
    $arguments += @('-U', $SqlCredential.UserName)
    $env:SQLCMDPASSWORD = $SqlCredential.GetNetworkCredential().Password
}

$environmentDataVerification = switch ($Environment) {
    'Development' {
        "IF NOT EXISTS (SELECT 1 FROM SEGURIDAD.USUARIOS WHERE NOMBRE_USUARIO = N'AOCAUZI') THROW 50003, 'Falta AOCAUZI.', 1;"
    }
    { $_ -in @('Testing', 'Sandbox') } {
        @"
IF NOT EXISTS (SELECT 1 FROM SEGURIDAD.USUARIOS WHERE NOMBRE_USUARIO = N'QAADMIN') THROW 50003, 'Falta QAADMIN.', 1;
IF NOT EXISTS (SELECT 1 FROM COMERCIAL.CLIENTES WHERE CODIGO = N'CLI-900001') THROW 50026, 'Falta el cliente ficticio.', 1;
IF NOT EXISTS (SELECT 1 FROM INVENTARIO.PRODUCTOS WHERE CODIGO = N'PRD-900001') THROW 50027, 'Falta el producto ficticio.', 1;
"@
    }
    'Production' {
        @"
IF EXISTS (SELECT 1 FROM SEGURIDAD.USUARIOS WHERE NOMBRE_USUARIO IN (N'AOCAUZI', N'QAADMIN')) THROW 50028, 'Producción contiene un usuario de demostración.', 1;
IF EXISTS (SELECT 1 FROM INVENTARIO.PRODUCTOS WHERE CODIGO_BARRAS LIKE N'DEV-%' OR CODIGO_BARRAS LIKE N'QA-%') THROW 50029, 'Producción contiene productos de demostración.', 1;
"@
    }
}
$inspectionUsername = if ($Environment -eq 'Development') { 'AOCAUZI' } elseif ($Environment -in @('Testing', 'Sandbox')) { 'QAADMIN' } else { '' }

$verificationQuery = @"
SET NOCOUNT ON;
IF DB_ID(N'$DatabaseName') IS NULL THROW 50000, 'La base no existe.', 1;
IF OBJECT_ID(N'SEGURIDAD.USUARIOS', N'U') IS NULL THROW 50001, 'Falta SEGURIDAD.USUARIOS.', 1;
IF OBJECT_ID(N'AUDITORIA.SP_ERROR_GET_BY_CODE', N'P') IS NULL THROW 50002, 'Falta AUDITORIA.SP_ERROR_GET_BY_CODE.', 1;
$environmentDataVerification
IF NOT EXISTS (SELECT 1 FROM AUDITORIA.CATALOGO_ERRORES WHERE CODIGO_ERROR = 70001) THROW 50004, 'Falta el error 70001.', 1;
IF OBJECT_ID(N'AUDITORIA.MIGRACIONES_ESQUEMA', N'U') IS NULL THROW 50030, 'Falta el registro de migraciones.', 1;
IF OBJECT_ID(N'INVENTARIO.EXISTENCIAS', N'U') IS NULL THROW 50005, 'Falta INVENTARIO.EXISTENCIAS.', 1;
IF OBJECT_ID(N'INVENTARIO.SP_MOVIMIENTO_CREATE', N'P') IS NULL THROW 50006, 'Falta INVENTARIO.SP_MOVIMIENTO_CREATE.', 1;
IF NOT EXISTS (SELECT 1 FROM AUDITORIA.CATALOGO_ERRORES WHERE CODIGO_ERROR = 30004) THROW 50007, 'Falta el error 30004.', 1;
IF NOT EXISTS (SELECT 1 FROM SEGURIDAD.PERMISOS WHERE CODIGO = N'INVENTARIO.GESTIONAR') THROW 50008, 'Falta INVENTARIO.GESTIONAR.', 1;
IF OBJECT_ID(N'COMERCIAL.PEDIDOS', N'U') IS NULL OR OBJECT_ID(N'COMERCIAL.VENTAS', N'U') IS NULL OR OBJECT_ID(N'INVENTARIO.RESERVAS_STOCK', N'U') IS NULL THROW 50009, 'Falta el modelo de pedidos y ventas.', 1;
IF OBJECT_ID(N'COMERCIAL.SP_VENTA_CREATE_FROM_PEDIDO', N'P') IS NULL THROW 50010, 'Falta SP_VENTA_CREATE_FROM_PEDIDO.', 1;
IF NOT EXISTS (SELECT 1 FROM SEGURIDAD.PERMISOS WHERE CODIGO = N'COMERCIAL.VENTAS_GESTIONAR') THROW 50011, 'Falta COMERCIAL.VENTAS_GESTIONAR.', 1;
IF NOT EXISTS (SELECT 1 FROM AUDITORIA.CATALOGO_ERRORES WHERE CODIGO_ERROR = 40007) THROW 50012, 'Falta el error 40007.', 1;
IF OBJECT_ID(N'COMPRAS.ORDENES_COMPRA', N'U') IS NULL OR OBJECT_ID(N'COMPRAS.RECEPCIONES_COMPRA', N'U') IS NULL THROW 50013, 'Falta el modelo de Compras.', 1;
IF OBJECT_ID(N'INVENTARIO.ACTIVOS_EVENTOS', N'U') IS NULL OR OBJECT_ID(N'INVENTARIO.SP_TRANSFORMACION_CREATE', N'P') IS NULL THROW 50014, 'Falta el modelo de trazabilidad.', 1;
IF NOT EXISTS (SELECT 1 FROM SEGURIDAD.PERMISOS WHERE CODIGO = N'COMPRAS.RECIBIR') THROW 50015, 'Falta COMPRAS.RECIBIR.', 1;
IF NOT EXISTS (SELECT 1 FROM AUDITORIA.CATALOGO_ERRORES WHERE CODIGO_ERROR = 50001) THROW 50016, 'Falta el catálogo de errores de Compras.', 1;
IF OBJECT_ID(N'COMPRAS.SP_RECEPCION_REVERSE', N'P') IS NULL THROW 50017, 'Falta COMPRAS.SP_RECEPCION_REVERSE.', 1;
IF COL_LENGTH(N'COMPRAS.ORDENES_COMPRA', N'PROVEEDOR_RAZON_SOCIAL') IS NULL OR COL_LENGTH(N'COMPRAS.RECEPCIONES_COMPRA', N'ID_MOVIMIENTO_REVERSION') IS NULL THROW 50018, 'Faltan snapshots o campos de reversión de Compras.', 1;
IF NOT EXISTS (SELECT 1 FROM AUDITORIA.CATALOGO_ERRORES WHERE CODIGO_ERROR = 50008) THROW 50019, 'Falta el error funcional 50008.', 1;
IF OBJECT_ID(N'LOGISTICA.SOLICITUDES', N'U') IS NULL OR OBJECT_ID(N'LOGISTICA.HOJAS_RUTA', N'U') IS NULL OR OBJECT_ID(N'LOGISTICA.NOTIFICACIONES', N'U') IS NULL THROW 50020, 'Falta el modelo de Logística.', 1;
IF OBJECT_ID(N'LOGISTICA.TRANSPORTISTAS', N'U') IS NULL OR OBJECT_ID(N'LOGISTICA.VEHICULOS', N'U') IS NULL THROW 50021, 'Falta el modelo de transporte.', 1;
IF OBJECT_ID(N'LOGISTICA.SP_LOGISTICA_GET', N'P') IS NULL OR OBJECT_ID(N'LOGISTICA.SP_LOGISTICA_COMMAND', N'P') IS NULL THROW 50022, 'Faltan los procedimientos de Logística.', 1;
IF COL_LENGTH(N'LOGISTICA.HOJAS_RUTA', N'ID_TRANSPORTISTA') IS NULL OR COL_LENGTH(N'LOGISTICA.HOJAS_RUTA', N'ID_VEHICULO') IS NULL THROW 50023, 'Faltan las referencias de transporte en hojas de ruta.', 1;
IF NOT EXISTS (SELECT 1 FROM SEGURIDAD.PERMISOS WHERE CODIGO = N'LOGISTICA.GESTIONAR') THROW 50024, 'Falta LOGISTICA.GESTIONAR.', 1;
IF NOT EXISTS (SELECT 1 FROM AUDITORIA.CATALOGO_ERRORES WHERE CODIGO_ERROR = 60004) THROW 50025, 'Falta el catálogo de errores de Logística.', 1;
IF SCHEMA_ID(N'FINANZAS') IS NULL THROW 50031, 'Falta el esquema FINANZAS.', 1;
IF OBJECT_ID(N'FINANZAS.PAGOS', N'U') IS NULL OR OBJECT_ID(N'FINANZAS.SESIONES_CAJA', N'U') IS NULL OR OBJECT_ID(N'FINANZAS.MOVIMIENTOS_CUENTA', N'U') IS NULL THROW 50032, 'Falta el modelo de Finanzas.', 1;
IF OBJECT_ID(N'FINANZAS.SP_FINANZAS_GET', N'P') IS NULL OR OBJECT_ID(N'FINANZAS.SP_FINANZAS_COMMAND', N'P') IS NULL THROW 50033, 'Faltan los procedimientos de Finanzas.', 1;
IF NOT EXISTS (SELECT 1 FROM SEGURIDAD.PERMISOS WHERE CODIGO = N'FINANZAS.COBRAR') THROW 50034, 'Falta FINANZAS.COBRAR.', 1;
IF NOT EXISTS (SELECT 1 FROM SEGURIDAD.PERMISOS WHERE CODIGO = N'FISCAL.CONSULTAR') THROW 50035, 'Falta FISCAL.CONSULTAR.', 1;
IF NOT EXISTS (SELECT 1 FROM CONFIGURACION.PARAMETROS_SISTEMA WHERE CLAVE = N'FISCAL.AMBIENTE_ARCA') THROW 50036, 'Falta la preparación fiscal.', 1;
IF OBJECT_DEFINITION(OBJECT_ID(N'LOGISTICA.SP_LOGISTICA_COMMAND')) NOT LIKE N'%PARADA_LLEGADA%' OR OBJECT_DEFINITION(OBJECT_ID(N'LOGISTICA.SP_LOGISTICA_COMMAND')) NOT LIKE N'%PARADA_INCIDENCIA%' OR OBJECT_DEFINITION(OBJECT_ID(N'LOGISTICA.SP_LOGISTICA_COMMAND')) NOT LIKE N'%UPDLOCK, HOLDLOCK%' THROW 50037, 'Faltan las operaciones de parada o su control de concurrencia.', 1;
IF (SELECT COUNT(*) FROM sys.schemas WHERE name IN (N'SEGURIDAD', N'CONFIGURACION', N'INVENTARIO', N'COMERCIAL', N'COMPRAS', N'LOGISTICA', N'FINANZAS', N'AUDITORIA')) < 8 THROW 50039, 'Falta un esquema operativo.', 1;

SELECT COUNT(*) AS CANTIDAD_SCHEMAS
FROM sys.schemas WHERE name IN (N'SEGURIDAD', N'CONFIGURACION', N'INVENTARIO', N'COMERCIAL', N'COMPRAS', N'LOGISTICA', N'FINANZAS', N'AUDITORIA');
SELECT COUNT(*) AS CANTIDAD_TABLAS_OXITIGRE
FROM sys.tables AS T INNER JOIN sys.schemas AS S ON S.schema_id = T.schema_id
WHERE S.name IN (N'SEGURIDAD', N'CONFIGURACION', N'INVENTARIO', N'COMERCIAL', N'COMPRAS', N'LOGISTICA', N'FINANZAS', N'AUDITORIA');
SELECT U.NOMBRE_USUARIO, U.DEBE_CAMBIAR_CLAVE, R.CODIGO AS ROL
FROM SEGURIDAD.USUARIOS AS U
INNER JOIN SEGURIDAD.USUARIOS_ROLES AS UR ON UR.ID_USUARIO = U.ID_USUARIO AND UR.CODIGO_ESTADO = N'ACTIVO'
INNER JOIN SEGURIDAD.ROLES AS R ON R.ID_ROL = UR.ID_ROL
WHERE U.NOMBRE_USUARIO = N'$inspectionUsername';
SELECT C.CODIGO, C.NOMBRE_RAZON_SOCIAL, COUNT(T.ID_CLIENTE_TELEFONO) AS TELEFONOS
FROM COMERCIAL.CLIENTES AS C
LEFT JOIN COMERCIAL.CLIENTES_TELEFONOS AS T ON T.ID_CLIENTE = C.ID_CLIENTE AND T.CODIGO_ESTADO = N'ACTIVO'
GROUP BY C.CODIGO, C.NOMBRE_RAZON_SOCIAL;
"@
$arguments += @('-Q', $verificationQuery)

try {
    & sqlcmd @arguments
    if ($LASTEXITCODE -ne 0) { throw "La verificación finalizó con código $LASTEXITCODE." }
    Write-Host "Verificación de $DatabaseName aprobada."
}
finally {
    if ($null -ne $SqlCredential) { Remove-Item Env:SQLCMDPASSWORD -ErrorAction SilentlyContinue }
}
