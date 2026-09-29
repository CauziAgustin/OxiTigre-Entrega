<#
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: scripts.database.Deploy-Environment
Archivo: Deploy-Environment.ps1 | Versión: 1.0.0 | Fecha: 2026-08-27 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Instala y valida juntas la base operativa y la plataforma de un ambiente nuevo.
Historial: 1.0.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [ValidateNotNullOrEmpty()] [string] $ServerInstance,
    [Parameter(Mandatory)] [ValidateSet('Development', 'Testing', 'Sandbox', 'Production')] [string] $Environment,
    [ValidatePattern('^[A-Za-z][A-Za-z0-9_]{0,127}$')] [string] $OperationalDatabaseName,
    [ValidatePattern('^[A-Za-z][A-Za-z0-9_]{0,127}$')] [string] $PlatformDatabaseName
)

$ErrorActionPreference = 'Stop'
$operationalName = if ($OperationalDatabaseName) { $OperationalDatabaseName } else { "OxiTigre_$Environment" }
$platformName = if ($PlatformDatabaseName) { $PlatformDatabaseName } elseif ($Environment -eq 'Development') {
    'OxiTigre_Platform'
} else {
    "OxiTigre_Platform_$Environment"
}

& (Join-Path $PSScriptRoot 'Deploy-Database.ps1') `
    -ServerInstance $ServerInstance `
    -DatabaseName $operationalName `
    -Environment $Environment

& (Join-Path $PSScriptRoot 'Deploy-PlatformDatabase.ps1') `
    -ServerInstance $ServerInstance `
    -PlatformDatabaseName $platformName `
    -CompanyCode 'OXITIGRE' `
    -CompanyName 'OxiTigre' `
    -OperationalDatabaseName $operationalName `
    -LocalCompanyId 1 `
    -SchemaVersion '10.0.0'

& (Join-Path $PSScriptRoot 'Test-DatabaseDeployment.ps1') `
    -ServerInstance $ServerInstance `
    -DatabaseName $operationalName `
    -Environment $Environment

Write-Host "Ambiente $Environment preparado: $platformName -> $operationalName."
