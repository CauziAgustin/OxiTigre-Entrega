<#
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: scripts.development.Start-Api
Archivo: Start-Api.ps1 | Versión: 1.0.0 | Fecha: 2026-09-21 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Inicia la API local de la entrega Desktop en el puerto 5000.
Historial: 1.0.0 | 2026-09-21 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$env:ASPNETCORE_ENVIRONMENT = 'Development'

& dotnet run --project (Join-Path $repositoryRoot 'backend/OxiTigre.Api/OxiTigre.Api.csproj') --urls 'http://localhost:5000'
exit $LASTEXITCODE
