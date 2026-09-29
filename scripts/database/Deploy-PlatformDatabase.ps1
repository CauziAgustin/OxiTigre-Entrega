<#
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: scripts.database.Deploy-PlatformDatabase
Archivo: Deploy-PlatformDatabase.ps1 | Versión: 1.0.0 | Fecha: 2026-08-27 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Instala OxiTigre_Platform y registra una base operativa sin persistir credenciales.
Historial: 1.0.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [ValidateNotNullOrEmpty()] [string] $ServerInstance,
    [ValidatePattern('^[A-Za-z][A-Za-z0-9_]{0,127}$')] [string] $PlatformDatabaseName = 'OxiTigre_Platform',
    [Parameter(Mandatory)] [ValidatePattern('^[A-Z][A-Z0-9_]{1,29}$')] [string] $CompanyCode,
    [Parameter(Mandatory)] [ValidateNotNullOrEmpty()] [string] $CompanyName,
    [Parameter(Mandatory)] [ValidatePattern('^[A-Za-z][A-Za-z0-9_]{0,127}$')] [string] $OperationalDatabaseName,
    [long] $LocalCompanyId = 1,
    [string] $SchemaVersion = '9.0.0'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$platformRoot = Join-Path $repositoryRoot 'platform-database'

function Invoke-PlatformSql {
    param([Parameter(Mandatory)] [string] $Database, [string] $Query, [string] $InputFile)

    $arguments = @('-S', $ServerInstance, '-d', $Database, '-E', '-b', '-r', '1', '-C', '-I', '-f', '65001')
    if ($Query) { $arguments += @('-Q', $Query) }
    if ($InputFile) { $arguments += @('-i', $InputFile) }
    & sqlcmd @arguments
    if ($LASTEXITCODE -ne 0) { throw "sqlcmd finalizó con código $LASTEXITCODE." }
}

if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
    throw 'No se encontró sqlcmd en PATH.'
}

Invoke-PlatformSql -Database 'master' -Query @"
IF DB_ID(N'$PlatformDatabaseName') IS NULL
BEGIN
    DECLARE @V_SQL NVARCHAR(MAX) = N'CREATE DATABASE ' + QUOTENAME(N'$PlatformDatabaseName');
    EXEC sys.sp_executesql @V_SQL;
END;
"@

Get-ChildItem -LiteralPath $platformRoot -Recurse -File -Filter '*.sql' |
    Sort-Object FullName |
    ForEach-Object { Invoke-PlatformSql -Database $PlatformDatabaseName -InputFile $_.FullName }

$sqlCompanyName = $CompanyName.Replace("'", "''")
$sqlServer = $ServerInstance.Replace("'", "''")
Invoke-PlatformSql -Database $PlatformDatabaseName -Query @"
EXEC [PLATAFORMA].[SP_EMPRESA_BASE_SAVE]
    @I_CODIGO = N'$CompanyCode',
    @I_RAZON_SOCIAL = N'$sqlCompanyName',
    @I_SERVIDOR_SQL = N'$sqlServer',
    @I_BASE_DATOS = N'$OperationalDatabaseName',
    @I_ID_EMPRESA_LOCAL = $LocalCompanyId,
    @I_VERSION_ESQUEMA = N'$SchemaVersion',
    @I_CIFRAR_CONEXION = 0,
    @I_CONFIAR_CERTIFICADO = 1;
"@

Write-Host "Plataforma $PlatformDatabaseName preparada para $CompanyCode."
