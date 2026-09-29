<#
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: scripts.development.Start-Desktop
Archivo: Start-Desktop.ps1 | Versión: 1.0.0 | Fecha: 2026-09-21 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Inicia WinForms contra la API local de la entrega Desktop.
Historial: 1.0.0 | 2026-09-21 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$env:OXITIGRE_API_URL = 'http://localhost:5000/'

& dotnet run --project (Join-Path $repositoryRoot 'frontend/OxiTigre.WinForms/OxiTigre.WinForms.csproj')
exit $LASTEXITCODE
