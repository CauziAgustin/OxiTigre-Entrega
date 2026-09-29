<#
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: Tests.WinForms.DashboardSummary
Archivo: Test-DashboardSummary.ps1 | Versión: 1.4.0 | Fecha: 2026-09-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Comprueba las clasificaciones de estados usadas por el Home de escritorio.
Historial: 1.0.0 | 2026-09-03 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-09-03 | FABRICA | Agustin Omar Cauzi | Vigencias comerciales inclusivas.
Historial: 1.2.0 | 2026-09-03 | FABRICA | Agustin Omar Cauzi | Detección de existencias incompatibles.
Historial: 1.3.0 | 2026-09-03 | FABRICA | Agustin Omar Cauzi | Clasificación de órdenes listas y vencidas.
Historial: 1.4.0 | 2026-09-20 | FABRICA | Agustin Omar Cauzi | Alertas de activos, contenido y revisiones.
===============================================================================
#>
[CmdletBinding()]
param()

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$project = Join-Path $projectRoot 'frontend\OxiTigre.WinForms\OxiTigre.WinForms.csproj'
$output = Join-Path $projectRoot '.test-build\dashboard-summary\'

dotnet build $project --no-restore "-p:BaseOutputPath=$output" | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'No se pudo compilar WinForms para comprobar el Home.' }

$assembly = [Reflection.Assembly]::LoadFrom(
    (Join-Path $output 'Debug\net10.0-windows\OxiTigre.WinForms.dll'))
$type = $assembly.GetType('OxiTigre.WinForms.DashboardSummaryLogic', $true)
$flags = [Reflection.BindingFlags]::Static -bor [Reflection.BindingFlags]::NonPublic

function Invoke-Rule {
    param(
        [Parameter(Mandatory)] [string] $Name,
        [Parameter(Mandatory)] [AllowEmptyString()] [string] $Status
    )

    return $type.GetMethod($Name, $flags).Invoke($null, @($Status))
}

foreach ($status in @('BORRADOR', 'CONFIRMADO')) {
    if (-not (Invoke-Rule 'IsOpenOrder' $status)) { throw "El pedido $status debe estar abierto." }
}
foreach ($status in @('VENDIDO', 'CANCELADO')) {
    if (Invoke-Rule 'IsOpenOrder' $status) { throw "El pedido $status no debe estar abierto." }
}
foreach ($status in @('APROBADA', 'RECIBIDA_PARCIAL')) {
    if (-not (Invoke-Rule 'IsReceivablePurchaseOrder' $status)) {
        throw "La orden $status debe permitir recepción."
    }
}
foreach ($status in @('BORRADOR', 'PENDIENTE_APROBACION', 'RECIBIDA', 'CERRADA')) {
    if (Invoke-Rule 'IsReceivablePurchaseOrder' $status) {
        throw "La orden $status no debe permitir recepción."
    }
}
foreach ($status in @('PLANIFICADA', 'DESPACHADA', 'PAUSADA')) {
    if (-not (Invoke-Rule 'IsActiveRoute' $status)) { throw "La hoja $status debe estar activa." }
}
foreach ($status in @('COMPLETADA', 'ENTREGADA', 'DEVUELTO', 'CERRADA')) {
    if (-not (Invoke-Rule 'IsCompleted' $status)) { throw "El estado $status debe ser final." }
}

$effective = $type.GetMethod('IsEffective', $flags)
$today = [DateOnly]::new(2026, 9, 3)
if (-not $effective.Invoke($null, @('ACTIVO', [DateOnly]::new(2026, 9, 3), [DateOnly]::new(2026, 9, 3), $today))) {
    throw 'Los límites de vigencia deben ser inclusivos.'
}
if ($effective.Invoke($null, @('ACTIVO', $null, [DateOnly]::new(2026, 9, 2), $today))) {
    throw 'Una configuración vencida no debe considerarse vigente.'
}
if ($effective.Invoke($null, @('INACTIVO', $null, $null, $today))) {
    throw 'Una configuración inactiva no debe considerarse vigente.'
}

$invalidStock = $type.GetMethod('IsInvalidStockBalance', $flags)
if ($invalidStock.Invoke($null, @([decimal]10, [decimal]2, [decimal]8))) {
    throw 'Una posición coherente no debe solicitar auditoría.'
}
if (-not $invalidStock.Invoke($null, @([decimal]5, [decimal]6, [decimal]-1))) {
    throw 'Una reserva mayor al físico debe solicitar auditoría.'
}

$assetAttention = $type.GetMethod('NeedsAssetAttention', $flags)
foreach ($state in @(
    @('BLOQUEADO', 'OPERATIVO'),
    @('DISPONIBLE', 'DANADO'),
    @('DISPONIBLE', 'EN_REVISION'),
    @('DISPONIBLE', 'EN_MANTENIMIENTO')
)) {
    if (-not $assetAttention.Invoke($null, $state)) {
        throw "El activo $($state -join '/') debe requerir intervención."
    }
}
if ($assetAttention.Invoke($null, @('DISPONIBLE', 'OPERATIVO'))) {
    throw 'Un activo disponible y operativo no debe generar una alerta.'
}

$contentInconsistent = $type.GetMethod('IsTraceabilityContentInconsistent', $flags)
if (-not $contentInconsistent.Invoke($null, @([decimal]-1, [long]1, [long]1))) {
    throw 'Una cantidad de contenido negativa debe requerir auditoría.'
}
if (-not $contentInconsistent.Invoke($null, @([decimal]5, $null, [long]1))) {
    throw 'El contenido positivo sin producto debe requerir auditoría.'
}
if (-not $contentInconsistent.Invoke($null, @([decimal]5, [long]1, $null))) {
    throw 'El contenido positivo sin lote debe requerir auditoría.'
}
if ($contentInconsistent.Invoke($null, @([decimal]5, [long]1, [long]1))) {
    throw 'El contenido con producto y lote no debe generar una alerta.'
}

$reviewDue = $type.GetMethod('IsMaintenanceReviewDue', $flags)
if (-not $reviewDue.Invoke($null, @('COMPLETADO', [DateOnly]::new(2026, 9, 3), $today))) {
    throw 'Una revisión prevista para hoy debe requerir control.'
}
if ($reviewDue.Invoke($null, @('COMPLETADO', [DateOnly]::new(2026, 9, 4), $today))) {
    throw 'Una revisión futura no debe requerir control.'
}
if ($reviewDue.Invoke($null, @('EN_CURSO', [DateOnly]::new(2026, 9, 2), $today))) {
    throw 'Un mantenimiento todavía abierto no debe contarse como revisión programada vencida.'
}

$overduePurchase = $type.GetMethod('IsOverduePurchaseOrder', $flags)
$comparisonDate = [DateOnly]::new(2026, 9, 3)
if (-not $overduePurchase.Invoke($null, @('APROBADA', [DateOnly]::new(2026, 9, 2), $true, $comparisonDate))) {
    throw 'Una orden aprobada, pendiente y vencida debe requerir revisión.'
}
if ($overduePurchase.Invoke($null, @('APROBADA', [DateOnly]::new(2026, 9, 3), $true, $comparisonDate))) {
    throw 'Una entrega prevista para hoy no debe marcarse vencida.'
}
if ($overduePurchase.Invoke($null, @('RECIBIDA', [DateOnly]::new(2026, 9, 2), $true, $comparisonDate))) {
    throw 'Una orden finalizada no debe marcarse vencida.'
}

'DASHBOARD_SUMMARY_OK'
