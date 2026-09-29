<#
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: scripts.database.Apply-Phase7
Archivo: Apply-Phase7.ps1 | Versión: 1.0.0 | Fecha: 2026-08-24 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Actualiza una base existente de Fase 6 a Fase 7 sin eliminar datos.
Historial: 1.0.0 | 2026-08-24 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $ServerInstance,
    [Parameter(Mandatory)] [ValidatePattern('^[A-Za-z][A-Za-z0-9_]{0,127}$')] [string] $DatabaseName,
    [switch] $IncludeDevelopmentData
)
$ErrorActionPreference='Stop'
$repositoryRoot=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
function Invoke-Sql([string]$file){& sqlcmd -S $ServerInstance -d $DatabaseName -E -b -C -I -f 65001 -i (Join-Path $repositoryRoot $file);if($LASTEXITCODE-ne 0){throw "Falló $file."}}
& sqlcmd -S $ServerInstance -d $DatabaseName -E -b -C -Q "IF OBJECT_ID(N'COMPRAS.PROVEEDORES',N'U') IS NOT NULL THROW 50000,'La Fase 7 ya existe en esta base.',1;"
if($LASTEXITCODE-ne 0){throw 'La base ya posee Fase 7 o no está disponible.'}
$files=@(
 'database/Migrations/20260824_FASE7_PRODUCT_COLUMNS.sql','database/02_Tables/COMPRAS/FASE7_COMPRAS.sql',
 'database/02_Tables/INVENTARIO/FASE7_TRAZABILIDAD.sql','database/03_Types/COMPRAS/SEQ_FASE7_CODIGOS.sql',
 'database/06_StoredProcedures/COMPRAS/Commands/SP_ORDEN_COMPRA_APPROVE.sql','database/06_StoredProcedures/COMPRAS/Commands/SP_ORDEN_COMPRA_CANCEL.sql',
 'database/06_StoredProcedures/COMPRAS/Commands/SP_ORDEN_COMPRA_CLOSE.sql','database/06_StoredProcedures/COMPRAS/Commands/SP_ORDEN_COMPRA_SAVE.sql',
 'database/06_StoredProcedures/COMPRAS/Commands/SP_ORDEN_COMPRA_SUBMIT.sql','database/06_StoredProcedures/COMPRAS/Commands/SP_PROVEEDOR_SAVE.sql',
 'database/06_StoredProcedures/COMPRAS/Commands/SP_RECEPCION_CREATE.sql','database/06_StoredProcedures/COMPRAS/Commands/SP_RECEPCION_REVERSE.sql','database/06_StoredProcedures/COMPRAS/Queries/SP_COMPRAS_GET.sql',
 'database/06_StoredProcedures/INVENTARIO/Commands/SP_INCIDENTE_CREATE.sql','database/06_StoredProcedures/INVENTARIO/Commands/SP_MANTENIMIENTO_COMPLETE.sql',
 'database/06_StoredProcedures/INVENTARIO/Commands/SP_MANTENIMIENTO_CREATE.sql','database/06_StoredProcedures/INVENTARIO/Commands/SP_MEDICION_ACTIVO_CREATE.sql',
 'database/06_StoredProcedures/INVENTARIO/Commands/SP_PRESTAMO_CREATE.sql','database/06_StoredProcedures/INVENTARIO/Commands/SP_PRESTAMO_RETURN.sql',
 'database/06_StoredProcedures/INVENTARIO/Commands/SP_TRANSFORMACION_CREATE.sql','database/06_StoredProcedures/INVENTARIO/Commands/SP_PRODUCTO_SAVE.sql',
 'database/06_StoredProcedures/INVENTARIO/Queries/SP_INVENTARIO_GET.sql','database/06_StoredProcedures/INVENTARIO/Queries/SP_TRAZABILIDAD_GET.sql',
 'database/07_Indexes/IX_FASE7_COMPRAS_TRAZABILIDAD.sql','database/08_Constraints/CK_FASE7_COMPRAS_TRAZABILIDAD.sql',
 'database/08_Constraints/FK_FASE7_COMPRAS_TRAZABILIDAD.sql','database/09_Seeds/05_ROLES_PERMISOS.sql','database/09_Seeds/06_ERRORES_INICIALES.sql'
)
foreach($file in $files){Write-Host "Aplicando $file";Invoke-Sql $file}
if($IncludeDevelopmentData){Invoke-Sql 'database/10_TestData/Development/05_COMPRAS_TRAZABILIDAD_EJEMPLO.sql'}
Write-Host "Fase 7 aplicada a $DatabaseName sin eliminar datos existentes."
