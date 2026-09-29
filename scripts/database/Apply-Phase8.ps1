<#
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: scripts.database.Apply-Phase8
Archivo: Apply-Phase8.ps1 | Versión: 1.0.0 | Fecha: 2026-08-26 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Actualiza una base existente de Fase 7 a Fase 8 sin eliminar información.
Historial: 1.0.0 | 2026-08-26 | FABRICA | Agustin Omar Cauzi | Creación inicial.
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

function Invoke-Phase8Sql
{
    param
    (
        [Parameter(Mandatory)]
        [string] $File
    )

    & sqlcmd -S $ServerInstance -d $DatabaseName -E -b -C -I -f 65001 `
        -i (Join-Path $repositoryRoot $File)

    if ($LASTEXITCODE -ne 0)
    {
        throw "Falló $File."
    }
}

if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue))
{
    throw 'No se encontró sqlcmd en PATH.'
}

$phaseCheckQuery = @"
IF OBJECT_ID(N'LOGISTICA.SOLICITUDES', N'U') IS NOT NULL
    THROW 50000, 'La Fase 8 ya existe en esta base.', 1;
"@

& sqlcmd -S $ServerInstance -d $DatabaseName -E -b -C -Q $phaseCheckQuery

if ($LASTEXITCODE -ne 0)
{
    throw 'La base ya posee Fase 8 o no está disponible.'
}

$files = @(
    'database/02_Tables/LOGISTICA/FASE8_LOGISTICA.sql',
    'database/02_Tables/LOGISTICA/FASE8_TRANSPORTE.sql',
    'database/06_StoredProcedures/LOGISTICA/Commands/SP_LOGISTICA_COMMAND.sql',
    'database/06_StoredProcedures/LOGISTICA/Queries/SP_LOGISTICA_GET.sql',
    'database/07_Indexes/IX_FASE8_LOGISTICA.sql',
    'database/07_Indexes/IX_FASE8_TRANSPORTE.sql',
    'database/08_Constraints/FK_FASE8_LOGISTICA.sql',
    'database/08_Constraints/FK_FASE8_TRANSPORTE.sql',
    'database/09_Seeds/03_ESTADOS.sql',
    'database/09_Seeds/05_ROLES_PERMISOS.sql',
    'database/09_Seeds/06_ERRORES_INICIALES.sql'
)

foreach ($file in $files)
{
    Write-Host "Aplicando $file"
    Invoke-Phase8Sql -File $file
}

if ($IncludeDevelopmentData)
{
    Invoke-Phase8Sql -File 'database/10_TestData/Development/06_LOGISTICA_EJEMPLO.sql'
    Invoke-Phase8Sql -File 'database/10_TestData/Development/07_TRANSPORTE_EJEMPLO.sql'
}

Write-Host "Fase 8 aplicada a $DatabaseName sin eliminar datos existentes."
