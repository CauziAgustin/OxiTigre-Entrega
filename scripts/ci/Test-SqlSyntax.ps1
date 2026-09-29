<#
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: scripts.ci.Test-SqlSyntax
Archivo: Test-SqlSyntax.ps1 | Versión: 1.2.0 | Fecha: 2026-08-28 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Valida sintaxis T-SQL sin modificar ninguna base de datos.
Historial: 1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Inclusión de la base central multiempresa.
Historial: 1.2.0 | 2026-08-28 | FABRICA | Agustin Omar Cauzi | Compatibilidad con inclusiones SQLCMD validadas por separado.
===============================================================================
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$visualStudioRoot = Join-Path $env:ProgramFiles 'Microsoft Visual Studio/2022'
$assembly = $null

if (Test-Path -LiteralPath $visualStudioRoot) {
    $assembly = Get-ChildItem -LiteralPath $visualStudioRoot -Recurse -File -Filter 'Microsoft.SqlServer.TransactSql.ScriptDom.dll' -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -match '[\\/]SQLDB[\\/]DAC[\\/]' } |
        Select-Object -First 1
}

if ($null -eq $assembly) {
    Write-Warning 'No se encontró Microsoft.SqlServer.TransactSql.ScriptDom; se omite únicamente el análisis sintáctico local.'
    exit 0
}

Add-Type -Path $assembly.FullName
$parser = [Microsoft.SqlServer.TransactSql.ScriptDom.TSql160Parser]::new($true)
$failures = [System.Collections.Generic.List[string]]::new()
$sqlFiles = @('database', 'platform-database') |
    Where-Object { Test-Path -LiteralPath $_ } |
    ForEach-Object { Get-ChildItem -LiteralPath $_ -Recurse -File -Filter '*.sql' }

foreach ($file in $sqlFiles) {
    $content = [System.IO.File]::ReadAllText($file.FullName)
    $content = [regex]::Replace(
        $content,
        '(?m)^\s*:[rR]\s+.*$',
        '-- Inclusión SQLCMD validada mediante su archivo de origen.'
    )
    $reader = [System.IO.StringReader]::new($content)
    try {
        $parseErrors = $null
        $null = $parser.Parse($reader, [ref] $parseErrors)
        foreach ($parseError in $parseErrors) {
            $failures.Add("$($file.FullName):$($parseError.Line),$($parseError.Column): $($parseError.Message)")
        }
    }
    finally {
        $reader.Dispose()
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host "Sintaxis T-SQL aprobada para $($sqlFiles.Count) archivos."
exit 0
