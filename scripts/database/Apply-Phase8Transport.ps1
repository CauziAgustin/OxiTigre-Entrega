<#
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: scripts.database.Apply-Phase8Transport
Archivo: Apply-Phase8Transport.ps1 | Versión: 1.0.0 | Fecha: 2026-08-27 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Agrega transporte a una base que ya posee la Fase 8 sin eliminar datos.
Historial: 1.0.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
#>
[CmdletBinding()]
param
(
    [Parameter(Mandatory)]
    [string] $ServerInstance,

    [Parameter(Mandatory)]
    [ValidatePattern('^[A-Za-z][A-Za-z0-9_]{0,127}$')]
    [string] $DatabaseName,

    [switch] $IncludeDevelopmentData
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$files = @(
    'database/02_Tables/LOGISTICA/FASE8_TRANSPORTE.sql',
    'database/06_StoredProcedures/LOGISTICA/Commands/SP_LOGISTICA_COMMAND.sql',
    'database/06_StoredProcedures/LOGISTICA/Queries/SP_LOGISTICA_GET.sql',
    'database/07_Indexes/IX_FASE8_TRANSPORTE.sql',
    'database/08_Constraints/FK_FASE8_TRANSPORTE.sql',
    'database/09_Seeds/05_ROLES_PERMISOS.sql'
)

if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue))
{
    throw 'No se encontró sqlcmd en PATH.'
}

foreach ($file in $files)
{
    Write-Host "Aplicando $file"
    & sqlcmd -S $ServerInstance -d $DatabaseName -E -b -C -I -f 65001 `
        -i (Join-Path $repositoryRoot $file)

    if ($LASTEXITCODE -ne 0)
    {
        throw "Falló $file."
    }
}

if ($IncludeDevelopmentData)
{
    & sqlcmd -S $ServerInstance -d $DatabaseName -E -b -C -I -f 65001 `
        -i (Join-Path $repositoryRoot 'database/10_TestData/Development/07_TRANSPORTE_EJEMPLO.sql')
    if ($LASTEXITCODE -ne 0) { throw 'Fallaron los datos de transporte de desarrollo.' }
}

Write-Host "Extensión de transporte aplicada a $DatabaseName sin eliminar información."
