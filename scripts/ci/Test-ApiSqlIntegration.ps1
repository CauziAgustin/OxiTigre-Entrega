<#
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: scripts.ci.Test-ApiSqlIntegration
Archivo: Test-ApiSqlIntegration.ps1 | Versión: 1.3.0 | Fecha: 2026-08-29 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Comprueba login, aislamiento, configuración y logout contra SQL Server Testing.
Historial: 1.0.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-29 | FABRICA | Agustin Omar Cauzi | Compatibilidad con Windows PowerShell 5.1.
Historial: 1.2.0 | 2026-08-29 | FABRICA | Agustin Omar Cauzi | Cierre compatible del proceso de integración.
Historial: 1.3.0 | 2026-08-29 | FABRICA | Agustin Omar Cauzi | Validación HTTP 401 compatible con PowerShell 5.1.
===============================================================================
#>
[CmdletBinding()]
param(
    [string] $ServerInstance = '.\SQLEXPRESS',
    [string] $PlatformDatabaseName = 'OxiTigre_Platform_Testing',
    [ValidateRange(1024, 65535)] [int] $Port = 5098,
    [switch] $NoBuild
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$project = Join-Path $repositoryRoot 'backend/OxiTigre.Api/OxiTigre.Api.csproj'
$baseUrl = "http://localhost:$Port"
$testCredential = 'OxiTest123!'
$apiProcess = $null

if (-not $NoBuild) {
    & dotnet build $project --configuration Release --nologo
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo compilar la API para la prueba de integración.' }
}

$startInfo = [System.Diagnostics.ProcessStartInfo]::new('dotnet')
$startInfo.UseShellExecute = $false
$startInfo.CreateNoWindow = $true
$startInfo.WorkingDirectory = $repositoryRoot
$startInfo.Arguments = "run --project `"$project`" --configuration Release --no-build --no-launch-profile --urls `"$baseUrl`""
$startInfo.Environment['ASPNETCORE_ENVIRONMENT'] = 'Testing'
$startInfo.Environment['ConnectionStrings__OxiTigrePlatform'] =
    "Server=$ServerInstance;Database=$PlatformDatabaseName;Integrated Security=True;Encrypt=False;TrustServerCertificate=True"

try {
    $apiProcess = [System.Diagnostics.Process]::Start($startInfo)
    $deadline = [DateTime]::UtcNow.AddSeconds(30)
    do {
        if ($apiProcess.HasExited) { throw "La API terminó con código $($apiProcess.ExitCode)." }
        Start-Sleep -Milliseconds 500
        try { $health = Invoke-RestMethod -Uri "$baseUrl/health" -TimeoutSec 2 } catch { $health = $null }
    } until ($health.Status -eq 'Healthy' -or [DateTime]::UtcNow -ge $deadline)
    if ($health.Status -ne 'Healthy') { throw 'La API no respondió dentro de 30 segundos.' }

    $credentials = @{ username = 'QAADMIN'; password = $testCredential } | ConvertTo-Json
    $companies = Invoke-RestMethod -Method Post -Uri "$baseUrl/api/security/companies" `
        -ContentType 'application/json' -Body $credentials
    if (@($companies).Count -ne 1 -or @($companies)[0].code -ne 'OXITIGRE') {
        throw 'El usuario ficticio no resolvió exclusivamente OXITIGRE.'
    }

    $loginBody = @{ companyCode = 'OXITIGRE'; username = 'QAADMIN'; password = $testCredential } | ConvertTo-Json
    $session = Invoke-RestMethod -Method Post -Uri "$baseUrl/api/security/login" `
        -ContentType 'application/json' -Body $loginBody
    if (-not $session.token.StartsWith('OXITIGRE.')) { throw 'El token no quedó asociado a OXITIGRE.' }

    $headers = @{ Authorization = "Bearer $($session.token)" }
    $configuration = Invoke-RestMethod -Uri "$baseUrl/api/configuration" -Headers $headers
    if ($configuration.company.code -ne 'OXITIGRE') { throw 'La configuración provino de otra empresa.' }

    $overview = Invoke-RestMethod -Uri "$baseUrl/api/platform/overview" -Headers $headers
    if (@($overview.companies).Count -ne 1 -or @($overview.companies)[0].status -ne 'DISPONIBLE') {
        throw 'La vista global no informó la base Testing disponible.'
    }

    Invoke-RestMethod -Method Post -Uri "$baseUrl/api/security/logout" -Headers $headers | Out-Null
    $revokedStatus = $null
    try {
        $revokedStatus = [int](Invoke-WebRequest -Uri "$baseUrl/api/security/session" -Headers $headers).StatusCode
    }
    catch {
        if ($null -eq $_.Exception.Response) { throw }
        $revokedStatus = [int]$_.Exception.Response.StatusCode
    }
    if ($revokedStatus -ne 401) { throw 'El token continuó vigente después del logout.' }

    Write-Host 'Integración API-SQL aprobada: login, empresa, consulta global y logout.'
}
finally {
    if ($null -ne $apiProcess -and -not $apiProcess.HasExited) {
        $apiProcess.Kill()
        $apiProcess.WaitForExit()
    }
}
