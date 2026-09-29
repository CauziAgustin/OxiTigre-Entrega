<#
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: scripts.ci.Test-Repository
Archivo: Test-Repository.ps1 | Versión: 1.3.0 | Fecha: 2026-09-21 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Valida archivos regenerables, secretos, conflictos y documentación obligatoria del repositorio.
Historial: 1.0.0 | FABRICA | Validación inicial del repositorio.
Historial: 1.1.0 | 2026-08-24 | FABRICA | Agustin Omar Cauzi | Evita falsos positivos en contraseñas recibidas desde formularios.
Historial: 1.2.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Validación sintáctica de automatizaciones PowerShell.
Historial: 1.3.0 | 2026-09-21 | FABRICA | Agustin Omar Cauzi | Validación del contrato XML público de C#.
===============================================================================
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$failures = [System.Collections.Generic.List[string]]::new()
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$repositoryRootForGit = $repositoryRoot.Replace('\', '/')
$trackedFiles = @(& git -c "safe.directory=$repositoryRootForGit" ls-files)
if ($LASTEXITCODE -ne 0) {
    throw 'No se pudo leer el índice Git para validar el repositorio.'
}

$forbiddenPathPatterns = @(
    '(^|/)\.vs/',
    '(^|/)bin/',
    '(^|/)obj/',
    '(^|/)packages/',
    '(^|/)artifacts/',
    '(^|/)output/'
)

foreach ($file in $trackedFiles) {
    $normalized = $file -replace '\\', '/'
    foreach ($pattern in $forbiddenPathPatterns) {
        if ($normalized -match $pattern) {
            $failures.Add("Archivo regenerable versionado: $file")
            break
        }
    }

    $extension = [System.IO.Path]::GetExtension($file).ToLowerInvariant()
    if ($extension -in @('.pfx', '.p12', '.key', '.pem')) {
        $failures.Add("Archivo sensible versionado: $file")
    }

    if (Test-Path -LiteralPath $file) {
        $size = (Get-Item -LiteralPath $file).Length
        if ($size -gt 95MB) {
            $failures.Add("Archivo mayor a 95 MB: $file")
        }
    }
}

$powerShellFiles = Get-ChildItem -LiteralPath 'scripts' -Recurse -File -Filter '*.ps1'
foreach ($file in $powerShellFiles) {
    $tokens = $null
    $parseErrors = $null
    [System.Management.Automation.Language.Parser]::ParseFile(
        $file.FullName,
        [ref] $tokens,
        [ref] $parseErrors
    ) | Out-Null
    foreach ($parseError in $parseErrors) {
        $failures.Add("PowerShell inválido $($file.FullName):$($parseError.Extent.StartLineNumber): $($parseError.Message)")
    }
}

$conflicts = @(& git -c "safe.directory=$repositoryRootForGit" grep -n -E '^(<<<<<<<( .*)?|=======|>>>>>>>( .*)?)$' -- . 2>$null)
foreach ($conflict in $conflicts) {
    $failures.Add("Marcador de conflicto: $conflict")
}

$secretPatterns = @(
    'gh[pousr]_[A-Za-z0-9_]{20,}',
    '-----BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY-----',
    '(?i)\b(password|pwd)\b\s*=\s*["''][^"''\r\n]+["'']'
)

$textExtensions = @('.cs', '.json', '.config', '.sql', '.ps1', '.yml', '.yaml', '.props', '.targets')
foreach ($file in $trackedFiles) {
    if (-not (Test-Path -LiteralPath $file)) { continue }
    if ([System.IO.Path]::GetExtension($file).ToLowerInvariant() -notin $textExtensions) { continue }

    $content = Get-Content -Raw -LiteralPath $file
    foreach ($pattern in $secretPatterns) {
        if ($content -match $pattern) {
            $failures.Add("Posible secreto en: $file")
            break
        }
    }
}

$requiredFiles = @(
    'README.md',
    'docs/ARCHITECTURE.md',
    'docs/DATABASE.md',
    'docs/SECURITY.md',
    'docs/GIT_WORKFLOW.md',
    'docs/CI_CD.md',
    'docs/DEVELOPMENT_STANDARDS.md',
    'docs/DATA_DICTIONARY.md',
    'templates/sql/StoredProcedure.sql.template',
    'templates/sql/Table.sql.template',
    'templates/csharp/Component.cs.template'
)

foreach ($required in $requiredFiles) {
    if (-not (Test-Path -LiteralPath $required)) {
        $failures.Add("Falta archivo obligatorio: $required")
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    exit 1
}

& (Join-Path $PSScriptRoot 'Test-DocumentationStandards.ps1')
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

& (Join-Path $PSScriptRoot 'Test-CSharpDocumentation.ps1')
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Write-Host 'Validación de repositorio aprobada.'
exit 0
