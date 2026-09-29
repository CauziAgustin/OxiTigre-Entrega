<#
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: Tests.WinForms.LanguagePack
Archivo: Test-LanguagePack.ps1 | Versión: 1.1.0 | Fecha: 2026-09-21 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Comprueba el contrato JSON y las validaciones de los idiomas importables.
Historial: 1.0.0 | 2026-09-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-09-21 | FABRICA | Agustin Omar Cauzi | Casos de llaves malformadas, escapes y tamaño excesivo.
===============================================================================
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$project = Join-Path $projectRoot 'frontend/OxiTigre.WinForms/OxiTigre.WinForms.csproj'
$assemblyPath = Join-Path $projectRoot 'frontend/OxiTigre.WinForms/bin/Release/net10.0-windows/OxiTigre.WinForms.dll'

if (-not (Test-Path -LiteralPath $assemblyPath)) {
    & dotnet build $project --configuration Release --no-restore --nologo | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo compilar WinForms para validar los idiomas.' }
}

$assembly = [Reflection.Assembly]::LoadFrom($assemblyPath)
$type = $assembly.GetType('OxiTigre.WinForms.Localization', $true)
$flags = [Reflection.BindingFlags]::Static -bor [Reflection.BindingFlags]::NonPublic
$readPack = $type.GetMethod('ReadPack', $flags)
if ($null -eq $readPack) { throw 'No se encontró el validador de paquetes de idioma.' }

function Invoke-LanguageValidation {
    param([Parameter(Mandatory)] [string] $Path)

    try {
        return $readPack.Invoke($null, @($Path))
    }
    catch [Reflection.TargetInvocationException] {
        throw $_.Exception.InnerException
    }
}

function Assert-InvalidLanguage {
    param(
        [Parameter(Mandatory)] [string] $Json,
        [Parameter(Mandatory)] [string] $ExpectedMessage
    )

    $path = Join-Path ([IO.Path]::GetTempPath()) ("oxitigre-language-{0}.json" -f [guid]::NewGuid())
    try {
        [IO.File]::WriteAllText($path, $Json, [Text.UTF8Encoding]::new($false))
        try {
            $null = Invoke-LanguageValidation $path
            throw "El paquete inválido fue aceptado: $ExpectedMessage"
        }
        catch [IO.InvalidDataException] {
            if ($_.Exception.Message -notlike "*$ExpectedMessage*") {
                throw "Validación inesperada. Se esperaba '$ExpectedMessage' y se obtuvo '$($_.Exception.Message)'."
            }
        }
    }
    finally {
        if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path -Force }
    }
}

$example = Join-Path $projectRoot 'docs/examples/IDIOMA_PORTUGUES_EJEMPLO.json'
$validPack = Invoke-LanguageValidation $example
if ($validPack.Culture -ne 'pt-BR' -or $validPack.Translations.Count -lt 1) {
    throw 'El ejemplo portugués no cumple el contrato importable.'
}

Assert-InvalidLanguage '{"schemaVersion":1,"culture":"pt-BR","displayName":"Português","translations":{"Clave_Inexistente":"Texto"}}' "no existe en la interfaz"
Assert-InvalidLanguage '{"schemaVersion":1,"culture":"portugues","displayName":"Português","translations":{"Common_Save":"Salvar"}}' "código de cultura no es válido"
Assert-InvalidLanguage '{"schemaVersion":1,"culture":"pt-BR","displayName":"Português","translations":{"Dashboard_Hello":"Olá"}}' "no conserva sus parámetros"
Assert-InvalidLanguage '{"schemaVersion":1,"culture":"pt-BR","displayName":"Português","translations":{"Dashboard_Hello":"Olá {0} {"}}' "contiene llaves o parámetros de formato inválidos"
Assert-InvalidLanguage '{"schemaVersion":1,"culture":"pt-BR","displayName":"Português","translations":{"Dashboard_Hello":"Olá {{0}}"}}' "no conserva sus parámetros"
Assert-InvalidLanguage '{"schemaVersion":1,"culture":"pt-BR","displayName":"Português","translations":{"Dashboard_Hello":"Olá {0} {0}"}}' "no conserva sus parámetros"
Assert-InvalidLanguage ('{"schemaVersion":1,"culture":"pt-BR","displayName":"' + ('x' * 1048576) + '","translations":{"Common_Save":"Salvar"}}') "entre 1 byte y 1 MB"

$escapedPath = Join-Path ([IO.Path]::GetTempPath()) ("oxitigre-language-{0}.json" -f [guid]::NewGuid())
try {
    [IO.File]::WriteAllText(
        $escapedPath,
        '{"schemaVersion":1,"culture":"pt-BR","displayName":"Português","translations":{"Dashboard_Hello":"Olá {{{0}}}","Purchases_EditorTotal":"Total estimado: {0:N2} BRL"}}',
        [Text.UTF8Encoding]::new($false)
    )
    $null = Invoke-LanguageValidation $escapedPath
}
finally {
    if (Test-Path -LiteralPath $escapedPath) { Remove-Item -LiteralPath $escapedPath -Force }
}

'LANGUAGE_PACK_OK'
