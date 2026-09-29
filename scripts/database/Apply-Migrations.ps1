<#
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: scripts.database.Apply-Migrations
Archivo: Apply-Migrations.ps1 | Versión: 1.1.0 | Fecha: 2026-08-28 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Aplica una sola vez migraciones verificadas por nombre y hash SHA-256.
Historial: 1.0.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-28 | FABRICA | Ruta segura del repositorio para migraciones que reutilizan definiciones declarativas.
===============================================================================
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [ValidateNotNullOrEmpty()] [string] $ServerInstance,
    [Parameter(Mandatory)] [ValidatePattern('^[A-Za-z][A-Za-z0-9_]{0,127}$')] [string] $DatabaseName,
    [switch] $Baseline,
    [PSCredential] $SqlCredential
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$migrationsRoot = Join-Path $repositoryRoot 'database/Migrations'
$ledgerFile = Join-Path $repositoryRoot 'database/02_Tables/AUDITORIA/MIGRACIONES_ESQUEMA.sql'

function Invoke-MigrationSql {
    param([string] $Query, [string] $InputFile, [switch] $Capture)

    $arguments = @('-S', $ServerInstance, '-d', $DatabaseName, '-b', '-r', '1', '-C', '-I', '-f', '65001')
    if ($Capture) { $arguments += @('-h', '-1', '-W') }
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
        Push-Location -LiteralPath $repositoryRoot
        $result = & sqlcmd @arguments
        if ($LASTEXITCODE -ne 0) { throw "sqlcmd finalizó con código $LASTEXITCODE." }
        if ($Capture) { return $result }
    }
    finally {
        Pop-Location
        if ($null -ne $SqlCredential) { Remove-Item Env:SQLCMDPASSWORD -ErrorAction SilentlyContinue }
    }
}

if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) { throw 'No se encontró sqlcmd en PATH.' }

$ledgerQuery = "SET NOCOUNT ON; SELECT IIF(OBJECT_ID(N'AUDITORIA.MIGRACIONES_ESQUEMA', N'U') IS NULL, 0, 1);"
$ledgerExists = Invoke-MigrationSql -Capture -Query $ledgerQuery
if (($ledgerExists | Where-Object { $_ -match '^[01]$' } | Select-Object -First 1) -ne '1') {
    Invoke-MigrationSql -InputFile $ledgerFile
}

$migrations = Get-ChildItem -LiteralPath $migrationsRoot -File -Filter '*.sql' | Sort-Object Name
foreach ($migration in $migrations) {
    if ($migration.Name -notmatch '^\d{8}_[A-Z0-9_]+\.sql$') {
        throw "Nombre de migración inválido: $($migration.Name)."
    }

    $hash = (Get-FileHash -LiteralPath $migration.FullName -Algorithm SHA256).Hash
    $registeredQuery =
        "SET NOCOUNT ON; SELECT [HASH_SHA256] FROM [AUDITORIA].[MIGRACIONES_ESQUEMA] WHERE [ARCHIVO] = N'$($migration.Name)';"
    $registered = Invoke-MigrationSql -Capture -Query $registeredQuery
    $registeredHash = $registered | Where-Object { $_ -match '^[A-Fa-f0-9]{64}$' } | Select-Object -First 1

    if ($registeredHash) {
        if ($registeredHash -ne $hash) { throw "La migración aplicada $($migration.Name) cambió de contenido." }
        continue
    }

    if (-not $Baseline) {
        Write-Host "Aplicando migración $($migration.Name)"
        $wrapperPath = Join-Path ([System.IO.Path]::GetTempPath()) "oxitigre-migration-$([Guid]::NewGuid().ToString('N')).sql"
        $wrapper = @"
SET XACT_ABORT ON;
BEGIN TRANSACTION;
:r "$($migration.FullName)"
INSERT INTO [AUDITORIA].[MIGRACIONES_ESQUEMA] ([ARCHIVO], [HASH_SHA256], [CODIGO_ESTADO])
VALUES (N'$($migration.Name)', N'$hash', N'APLICADA');
COMMIT TRANSACTION;
"@
        try {
            [System.IO.File]::WriteAllText($wrapperPath, $wrapper, [System.Text.UTF8Encoding]::new($false))
            Invoke-MigrationSql -InputFile $wrapperPath
        }
        finally {
            if (Test-Path -LiteralPath $wrapperPath) { Remove-Item -LiteralPath $wrapperPath -Force }
        }
        continue
    }

    Invoke-MigrationSql -Query @"
INSERT INTO [AUDITORIA].[MIGRACIONES_ESQUEMA] ([ARCHIVO], [HASH_SHA256], [CODIGO_ESTADO])
VALUES (N'$($migration.Name)', N'$hash', N'APLICADA');
"@
}

Write-Host "Migraciones de $DatabaseName verificadas: $($migrations.Count)."
