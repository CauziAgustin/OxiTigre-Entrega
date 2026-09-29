<#
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: Tests.WinForms.PartialDateFilter
Archivo: Test-PartialDateFilter.ps1 | Versión: 1.0.0 | Fecha: 2026-08-26 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Comprueba el filtro de fechas parciales usado por todas las grillas WinForms.
Historial: 1.0.0 | 2026-08-26 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
#>
[CmdletBinding()]
param()

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$project = Join-Path $projectRoot 'frontend\OxiTigre.WinForms\OxiTigre.WinForms.csproj'
$output = Join-Path $projectRoot '.test-build\partial-date-filter\'
dotnet build $project --no-restore "-p:BaseOutputPath=$output" | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'No se pudo compilar WinForms para comprobar el filtro.' }

$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $output 'Debug\net10.0-windows\OxiTigre.WinForms.dll'))
$type = $assembly.GetType('OxiTigre.WinForms.UiTheme', $true)
$flags = [Reflection.BindingFlags]::Static -bor [Reflection.BindingFlags]::NonPublic
$method = $type.GetMethod('MatchesPartialDate', $flags)
$date = [DateOnly]::new(2026, 8, 1)

foreach ($value in @('1', '01', '1/8', '01/08', '1/8/26', '01/08/2026')) {
    if (-not $method.Invoke($null, @($date, $value))) { throw "No coincidió la fecha parcial '$value'." }
}
foreach ($value in @('2', '1/9', '1/8/25', 'texto')) {
    if ($method.Invoke($null, @($date, $value))) { throw "Coincidió incorrectamente la fecha parcial '$value'." }
}

'PARTIAL_DATE_FILTER_OK'
