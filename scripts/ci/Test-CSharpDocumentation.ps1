<#
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: scripts.ci.Test-CSharpDocumentation
Archivo: Test-CSharpDocumentation.ps1 | Versión: 2.0.0 | Fecha: 2026-09-28 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Verifica el contrato XML de todos los métodos declarados en el alcance de escritorio.
Historial: 1.0.0 | 2026-09-21 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-09-21 | FABRICA | Agustin Omar Cauzi | Rechazo de retornos XML generados sin significado funcional.
Historial: 1.2.0 | 2026-09-21 | FABRICA | Agustin Omar Cauzi | Rechazo de parámetros y excepciones XML genéricos.
Historial: 2.0.0 | 2026-09-28 | FABRICA | Agustin Omar Cauzi | Cobertura obligatoria de métodos públicos, internos y privados.
===============================================================================
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$failures = [Collections.Generic.List[string]]::new()
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path

if (-not ('Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree' -as [type])) {
    $sdkLine = & dotnet --list-sdks | Select-Object -Last 1
    if ($LASTEXITCODE -ne 0 -or -not $sdkLine) { throw 'No se pudo localizar el SDK de .NET.' }
    $sdkVersion = $sdkLine.Split(' ')[0]
    $roslyn = Join-Path $env:ProgramFiles "dotnet/sdk/$sdkVersion/Roslyn/bincore"
    [Reflection.Assembly]::LoadFrom((Join-Path $roslyn 'Microsoft.CodeAnalysis.dll')) | Out-Null
    [Reflection.Assembly]::LoadFrom((Join-Path $roslyn 'Microsoft.CodeAnalysis.CSharp.dll')) | Out-Null
}

foreach ($root in @('backend', 'frontend/OxiTigre.ApiClient', 'frontend/OxiTigre.WinForms', 'shared')) {
    $path = Join-Path $repositoryRoot $root
    foreach ($file in Get-ChildItem -LiteralPath $path -Recurse -File -Filter '*.cs' |
            Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }) {
        $content = [IO.File]::ReadAllText($file.FullName)
        $tree = [Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText($content)
        $members = $tree.GetRoot().DescendantNodes() | Where-Object {
            $_ -is [Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax] -or
            $_ -is [Microsoft.CodeAnalysis.CSharp.Syntax.ConstructorDeclarationSyntax]
        }

        foreach ($member in $members) {
            $line = $member.GetLocation().GetLineSpan().StartLinePosition.Line + 1
            $name = $member.Identifier.Text
            $documentation = $member.GetLeadingTrivia().ToFullString()
            if ($documentation -match '<inheritdoc') { continue }
            if ($documentation -notmatch '<summary>') {
                $failures.Add("Falta <summary> en $($file.FullName):${line} ($name).")
                continue
            }

            if ($documentation -match '<returns>\s*(Tarea que devuelve|Resultado de tipo)') {
                $failures.Add("El <returns> no explica el resultado funcional en $($file.FullName):${line} ($name).")
            }

            if ($documentation -match 'Datos de tipo|Valor ''[^'']+'' de tipo|Fecha u hora ''[^'']+'' usada por la operación|Uno o más datos de entrada no cumplen') {
                $failures.Add("La documentación XML conserva texto genérico en $($file.FullName):${line} ($name).")
            }

            foreach ($parameter in $member.ParameterList.Parameters) {
                $parameterName = $parameter.Identifier.Text
                $parameterPattern = '<param\s+name="' + [regex]::Escape($parameterName) + '"'
                if ($documentation -notmatch $parameterPattern) {
                    $failures.Add("Falta <param name='$($parameter.Identifier.Text)'> en $($file.FullName):${line} ($name).")
                }
            }

            if ($member -is [Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax] -and
                $member.ReturnType.ToString() -ne 'void' -and
                $documentation -notmatch '<returns>') {
                $failures.Add("Falta <returns> en $($file.FullName):${line} ($name).")
            }

        }
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host 'Contratos XML de todos los métodos C# aprobados.'
exit 0
