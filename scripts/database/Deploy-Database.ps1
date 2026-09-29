<#
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: scripts.database.Deploy-Database
Archivo: Deploy-Database.ps1 | Versión: 1.3.0 | Fecha: 2026-08-27 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Instala una base operativa nueva respetando el orden de dependencias de sus objetos.
Historial: 1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Incorporación del ambiente Testing.
Historial: 1.2.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Datos ficticios compartidos por Testing y Sandbox.
Historial: 1.3.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Baseline verificable de migraciones incluidas.
===============================================================================
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string] $ServerInstance,

    [Parameter(Mandatory)]
    [ValidatePattern('^[A-Za-z][A-Za-z0-9_]{0,127}$')]
    [string] $DatabaseName,

    [Parameter(Mandatory)]
    [ValidateSet('Development', 'Testing', 'Sandbox', 'Production')]
    [string] $Environment,

    [PSCredential] $SqlCredential
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$databaseRoot = Join-Path $repositoryRoot 'database'

function Invoke-OxiTigreSqlCmd {
    param(
        [Parameter(Mandatory)] [string] $TargetDatabase,
        [string] $Query,
        [string] $InputFile
    )

    $arguments = @('-S', $ServerInstance, '-d', $TargetDatabase, '-b', '-r', '1', '-C', '-I', '-f', '65001')
    if ($null -eq $SqlCredential) {
        $arguments += '-E'
    }
    else {
        $arguments += @('-U', $SqlCredential.UserName)
        $env:SQLCMDPASSWORD = $SqlCredential.GetNetworkCredential().Password
    }

    if ($Query) { $arguments += @('-Q', $Query) }
    if ($InputFile) { $arguments += @('-i', $InputFile) }

    try {
        & sqlcmd @arguments
        if ($LASTEXITCODE -ne 0) {
            throw "sqlcmd finalizó con código $LASTEXITCODE."
        }
    }
    finally {
        if ($null -ne $SqlCredential) {
            Remove-Item Env:SQLCMDPASSWORD -ErrorAction SilentlyContinue
        }
    }
}

if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
    throw 'No se encontró sqlcmd en PATH. Instale Microsoft SQL Server Command Line Utilities.'
}

$createDatabaseQuery = @"
IF DB_ID(N'$DatabaseName') IS NULL
BEGIN
    DECLARE @CreateDatabaseSql NVARCHAR(MAX) = N'CREATE DATABASE ' + QUOTENAME(N'$DatabaseName');
    EXEC sys.sp_executesql @CreateDatabaseSql;
END;
"@
Invoke-OxiTigreSqlCmd -TargetDatabase 'master' -Query $createDatabaseQuery

$orderedRoots = @(
    '01_Schemas',
    '02_Tables',
    '03_Types',
    '04_Functions',
    '05_Views',
    '06_StoredProcedures',
    '07_Indexes',
    '08_Constraints',
    '09_Seeds',
    '11_Security'
)

foreach ($relativeRoot in $orderedRoots) {
    $objectRoot = Join-Path $databaseRoot $relativeRoot
    if (-not (Test-Path -LiteralPath $objectRoot)) { continue }

    Get-ChildItem -LiteralPath $objectRoot -Recurse -File -Filter '*.sql' |
        Sort-Object FullName |
        ForEach-Object {
            Write-Host "Aplicando $($_.FullName.Substring($repositoryRoot.Length + 1))"
            Invoke-OxiTigreSqlCmd -TargetDatabase $DatabaseName -InputFile $_.FullName
        }
}

if ($Environment -eq 'Development') {
    $developmentRoot = Join-Path $databaseRoot '10_TestData/Development'
    Get-ChildItem -LiteralPath $developmentRoot -Recurse -File -Filter '*.sql' |
        Sort-Object FullName |
        ForEach-Object {
            Write-Host "Aplicando $($_.FullName.Substring($repositoryRoot.Length + 1))"
            Invoke-OxiTigreSqlCmd -TargetDatabase $DatabaseName -InputFile $_.FullName
        }
}

if ($Environment -in @('Testing', 'Sandbox')) {
    $nonProductionRoot = Join-Path $databaseRoot '10_TestData/NonProduction'
    Get-ChildItem -LiteralPath $nonProductionRoot -Recurse -File -Filter '*.sql' |
        Sort-Object FullName |
        ForEach-Object {
            Write-Host "Aplicando $($_.FullName.Substring($repositoryRoot.Length + 1))"
            Invoke-OxiTigreSqlCmd -TargetDatabase $DatabaseName -InputFile $_.FullName
        }
}

& (Join-Path $PSScriptRoot 'Apply-Migrations.ps1') `
    -ServerInstance $ServerInstance `
    -DatabaseName $DatabaseName `
    -Baseline `
    -SqlCredential $SqlCredential

Write-Host "Base $DatabaseName desplegada para $Environment."
