<#
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: scripts.ci.Test-DesktopDelivery
Archivo: Test-DesktopDelivery.ps1 | Versión: 1.1.1 | Fecha: 2026-09-21 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Valida el alcance de entrega de escritorio sin compilar la aplicación móvil.
Historial: 1.0.0 | 2026-09-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-09-20 | FABRICA | Agustin Omar Cauzi | Validación de paquetes de idioma importables.
Historial: 1.1.1 | 2026-09-21 | FABRICA | Agustin Omar Cauzi | Resultado preciso: la aceptación visual requiere revisión manual.
===============================================================================
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$solutionFilter = Join-Path $repositoryRoot 'OxiTigre.Desktop.slnf'

Push-Location $repositoryRoot
try {
    & dotnet restore $solutionFilter --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Falló la restauración del alcance Desktop.' }

    & dotnet build $solutionFilter --configuration Release --no-restore --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Falló la compilación Release del alcance Desktop.' }

    & dotnet test $solutionFilter --configuration Release --no-build --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Fallaron las pruebas automatizadas .NET del alcance Desktop.' }

    foreach ($script in @(
        'scripts/ci/Test-Repository.ps1',
        'scripts/ci/Test-SqlSyntax.ps1',
        'tests/WinForms/Test-PartialDateFilter.ps1',
        'tests/WinForms/Test-DashboardSummary.ps1',
        'tests/WinForms/Test-LanguagePack.ps1'
    )) {
        & pwsh -NoProfile -File (Join-Path $repositoryRoot $script)
        if ($LASTEXITCODE -ne 0) { throw "Falló la validación $script." }
    }

    Write-Host 'ENTREGA_DESKTOP_OK: compilación, pruebas, repositorio, SQL, filtros, dashboard e idiomas aprobados. La aceptación visual manual sigue pendiente.'
}
finally {
    Pop-Location
}
