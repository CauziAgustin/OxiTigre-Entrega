<#
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: scripts.database.Apply-PurchasesRevision
Archivo: Apply-PurchasesRevision.ps1 | Versión: 1.0.0 | Fecha: 2026-08-26 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Aplica de forma repetible la revisión de integridad y usabilidad de Compras.
Historial: 1.0.0 | 2026-08-26 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $ServerInstance,
    [Parameter(Mandatory)] [ValidatePattern('^[A-Za-z][A-Za-z0-9_]{0,127}$')] [string] $DatabaseName
)
$ErrorActionPreference='Stop'
$repositoryRoot=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$files=@(
    'database/Migrations/20260826_PURCHASES_USABILITY_AND_REVERSAL.sql',
    'database/06_StoredProcedures/COMPRAS/Commands/SP_PROVEEDOR_SAVE.sql',
    'database/06_StoredProcedures/COMPRAS/Commands/SP_ORDEN_COMPRA_SAVE.sql',
    'database/06_StoredProcedures/COMPRAS/Commands/SP_RECEPCION_REVERSE.sql',
    'database/06_StoredProcedures/COMPRAS/Queries/SP_COMPRAS_GET.sql',
    'database/09_Seeds/06_ERRORES_INICIALES.sql'
)
foreach($file in $files)
{
    Write-Host "Aplicando $file"
    & sqlcmd -S $ServerInstance -d $DatabaseName -E -b -C -I -f 65001 -i (Join-Path $repositoryRoot $file)
    if($LASTEXITCODE-ne 0){throw "Falló $file."}
}
Write-Host "Revisión de Compras aplicada a $DatabaseName."
