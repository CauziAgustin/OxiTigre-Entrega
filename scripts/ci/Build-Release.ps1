<#
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: scripts.ci.Build-Release
Archivo: Build-Release.ps1 | Versión: 1.3.0 | Fecha: 2026-09-21 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Publica binarios, instaladores SQL y manifiesto verificable de una versión.
Historial: 1.0.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-09-20 | FABRICA | Agustin Omar Cauzi | Alcance Desktop explícito y trazabilidad del árbol Git.
Historial: 1.2.0 | 2026-09-20 | FABRICA | Agustin Omar Cauzi | Documentación técnica completa dentro del artefacto.
Historial: 1.3.0 | 2026-09-21 | FABRICA | Agustin Omar Cauzi | Guía y ejemplo importable de idioma dentro del corte Desktop.
===============================================================================
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^[A-Za-z0-9][A-Za-z0-9._-]{0,99}$')]
    [string] $Version,

    [string] $OutputDirectory = 'artifacts',

    [switch] $AllowDirty
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$outputRoot = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $OutputDirectory))
$packageRoot = Join-Path $outputRoot "OxiTigre-$Version"
$archivePath = "$packageRoot.zip"
$workingTreeDirty = $false

& git -C $repositoryRoot diff --quiet
$workingTreeChanged = $LASTEXITCODE -ne 0
& git -C $repositoryRoot diff --cached --quiet
$indexChanged = $LASTEXITCODE -ne 0
$untrackedFiles = @(& git -C $repositoryRoot ls-files --others --exclude-standard)
$workingTreeDirty = $workingTreeChanged -or $indexChanged -or $untrackedFiles.Count -gt 0

if (-not $AllowDirty) {
    if ($workingTreeDirty) {
        throw 'El artefacto exige cambios versionados; confirme primero el trabajo en Git.'
    }
}

if ((Test-Path -LiteralPath $packageRoot -PathType Container) -or (Test-Path -LiteralPath $archivePath)) {
    throw "Ya existe el artefacto $Version; use otra versión o retire manualmente la salida anterior."
}

New-Item -ItemType Directory -Path $packageRoot -Force | Out-Null
$projects = @(
    @{ Name = 'Api'; Path = 'backend/OxiTigre.Api/OxiTigre.Api.csproj' },
    @{ Name = 'WinForms'; Path = 'frontend/OxiTigre.WinForms/OxiTigre.WinForms.csproj' },
    @{ Name = 'AdminWeb'; Path = 'frontend/OxiTigre.AdminWeb/OxiTigre.AdminWeb.csproj' }
)

foreach ($project in $projects) {
    $target = Join-Path $packageRoot $project.Name
    & dotnet publish (Join-Path $repositoryRoot $project.Path) --configuration Release --output $target --nologo
    if ($LASTEXITCODE -ne 0) { throw "Falló la publicación de $($project.Name)." }
}

$databaseTarget = New-Item -ItemType Directory -Path (Join-Path $packageRoot 'database')
$databaseRoots = @('01_Schemas', '02_Tables', '03_Types', '04_Functions', '05_Views',
                   '06_StoredProcedures', '07_Indexes', '08_Constraints', '09_Seeds', '11_Security', 'Migrations')
foreach ($root in $databaseRoots) {
    Copy-Item -LiteralPath (Join-Path $repositoryRoot "database/$root") -Destination $databaseTarget.FullName -Recurse
}

$testDataTarget = New-Item -ItemType Directory -Path (Join-Path $databaseTarget.FullName '10_TestData')
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'database/10_TestData/NonProduction') `
    -Destination $testDataTarget.FullName -Recurse

Copy-Item -LiteralPath (Join-Path $repositoryRoot 'platform-database') -Destination $packageRoot -Recurse
New-Item -ItemType Directory -Path (Join-Path $packageRoot 'scripts') | Out-Null
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'scripts/database') -Destination (Join-Path $packageRoot 'scripts') -Recurse
New-Item -ItemType Directory -Path (Join-Path $packageRoot 'docs') | Out-Null
foreach ($document in @(
    'ARCHITECTURE.md',
    'DEVELOPMENT_STANDARDS.md',
    'DELIVERY_2026-09-22.md',
    'DEPLOYMENT.md',
    'DATABASE_DEPLOYMENT.md',
    'INTERNATIONALIZATION.md',
    'MULTI_COMPANY.md',
    'ROADMAP.md',
    'SECURITY.md',
    'SETUP.md',
    'TEST_CASES.md'
)) {
    Copy-Item -LiteralPath (Join-Path $repositoryRoot "docs/$document") -Destination (Join-Path $packageRoot 'docs')
}
New-Item -ItemType Directory -Path (Join-Path $packageRoot 'docs/examples') | Out-Null
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'docs/examples/IDIOMA_PORTUGUES_EJEMPLO.json') `
    -Destination (Join-Path $packageRoot 'docs/examples')
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'README.md') -Destination $packageRoot

$commit = (& git -C $repositoryRoot rev-parse HEAD).Trim()
$files = Get-ChildItem -LiteralPath $packageRoot -Recurse -File | ForEach-Object {
    [ordered]@{
        Path = [System.IO.Path]::GetRelativePath($packageRoot, $_.FullName).Replace('\', '/')
        Bytes = $_.Length
        Sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
    }
}

[ordered]@{
    Version = $Version
    Commit = $commit
    Scope = 'DesktopApiDatabase'
    IncludesMobile = $false
    WorkingTreeDirty = $workingTreeDirty
    CreatedAtUtc = [DateTime]::UtcNow.ToString('O')
    Files = @($files)
} | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $packageRoot 'manifest.json') -Encoding UTF8

Compress-Archive -Path (Join-Path $packageRoot '*') -DestinationPath $archivePath -CompressionLevel Optimal
Write-Host "Artefacto creado: $archivePath"
