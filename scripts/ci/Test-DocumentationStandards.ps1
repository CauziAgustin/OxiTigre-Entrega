[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$failures = [System.Collections.Generic.List[string]]::new()
$headerPatterns = @(
    'Proyecto:',
    'Componente:',
    'Archivo:',
    'Versi[o\u00F3]n:',
    'Fecha:',
    'ID pedido:',
    'Desarrollador:',
    'Correo:',
    'Descripci[o\u00F3]n funcional:',
    'Historial'
)
$storedProcedureDocumentationPatterns = @(
    'Parámetros de entrada:',
    'Parámetros de salida:',
    'Retorno:',
    'Tablas utilizadas:',
    'Transacción:',
    'Auditoría:'
)

function Test-RequiredHeader {
    param(
        [Parameter(Mandatory)] [string] $Path,
        [Parameter(Mandatory)] [string] $Content
    )

    foreach ($pattern in $headerPatterns) {
        if ($Content -notmatch $pattern) {
            $failures.Add("Falta '$pattern' en la cabecera: $Path")
        }
    }

    if ($Content -notmatch 'Versi[o\u00F3]n:\s*\d+\.\d+\.\d+') {
        $failures.Add("Versión inválida; se espera MAYOR.MENOR.PARCHE: $Path")
    }

    if ($Content -notmatch 'Fecha:\s*\d{4}-\d{2}-\d{2}') {
        $failures.Add("Fecha inválida; se espera AAAA-MM-DD: $Path")
    }

    $dateMatch = [regex]::Match($Content, 'Fecha:\s*(?<date>\d{4}-\d{2}-\d{2})')
    if ($dateMatch.Success) {
        $declaredDate = [datetime]::MinValue
        if (-not [datetime]::TryParseExact(
                $dateMatch.Groups['date'].Value,
                'yyyy-MM-dd',
                [Globalization.CultureInfo]::InvariantCulture,
                [Globalization.DateTimeStyles]::None,
                [ref] $declaredDate
            ) -or $declaredDate.Date -gt [datetime]::Today) {
            $failures.Add("La fecha de versión no puede ser inválida ni futura: $Path")
        }
    }
}

$sourceRoots = @('backend', 'frontend', 'shared', 'tests')
foreach ($root in $sourceRoots) {
    if (-not (Test-Path -LiteralPath $root)) { continue }

    $sourceFiles = Get-ChildItem -LiteralPath $root -Recurse -File -Filter '*.cs' |
        Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }

    foreach ($file in $sourceFiles) {
        $content = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8
        Test-RequiredHeader -Path $file.FullName -Content $content
    }
}
$sqlRoots = @('database', 'platform-database')
foreach ($sqlRoot in $sqlRoots) {
    if (-not (Test-Path -LiteralPath $sqlRoot)) { continue }
    $sqlFiles = Get-ChildItem -LiteralPath $sqlRoot -Recurse -File -Filter '*.sql'
    foreach ($file in $sqlFiles) {
        $content = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8
        Test-RequiredHeader -Path $file.FullName -Content $content
    }
}

$storedProcedureRoots = @('database/06_StoredProcedures', 'platform-database/06_StoredProcedures')
foreach ($storedProcedureRoot in $storedProcedureRoots) {
    if (-not (Test-Path -LiteralPath $storedProcedureRoot)) { continue }
    $storedProcedures = Get-ChildItem -LiteralPath $storedProcedureRoot -Recurse -File -Filter '*.sql'
    foreach ($file in $storedProcedures) {
        $content = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8
        Test-RequiredHeader -Path $file.FullName -Content $content

        if ($content -notmatch 'Tipo:\s*(QUERY|COMMAND|AUDIT)\b') {
            $failures.Add("El SP debe declarar Tipo: QUERY, COMMAND o AUDIT: $($file.FullName)")
        }

        foreach ($pattern in $storedProcedureDocumentationPatterns) {
            if ($content -notmatch [regex]::Escape($pattern)) {
                $failures.Add("El SP debe documentar '$pattern': $($file.FullName)")
            }
        }

        $declaration = [regex]::Match(
            $content,
            'CREATE\s+OR\s+ALTER\s+PROCEDURE\s+\[(?<schema>[A-Z][A-Z0-9_]*)\]\.\[(?<procedure>SP_[A-Z][A-Z0-9_]*)\]',
            [System.Text.RegularExpressions.RegexOptions]::IgnoreCase
        )

        if (-not $declaration.Success) {
            $failures.Add("El SP debe usar CREATE OR ALTER y nombre calificado: $($file.FullName)")
            continue
        }

        $schema = $declaration.Groups['schema'].Value.ToUpperInvariant()
        $procedure = $declaration.Groups['procedure'].Value.ToUpperInvariant()
        if ($file.BaseName.ToUpperInvariant() -ne $procedure) {
            $failures.Add("El archivo debe llamarse $procedure.sql: $($file.FullName)")
        }

        if ($content -notmatch "Componente:\s+$([regex]::Escape($procedure))") {
            $failures.Add("El componente debe coincidir con el SP ${procedure}: $($file.FullName)")
        }

        if ($content -notmatch "Procedimiento:\s+$([regex]::Escape("$schema.$procedure"))") {
            $failures.Add("La cabecera debe declarar ${schema}.${procedure}: $($file.FullName)")
        }

        # Remove documentation and inline comments before inspecting T-SQL identifiers.
        # Otherwise an address such as developer@example.com is mistaken for a variable.
        $executableContent = [regex]::Replace(
            $content,
            '/\*[\s\S]*?\*/|--[^\r\n]*',
            '',
            [System.Text.RegularExpressions.RegexOptions]::IgnoreCase
        )
        $parameterNames = [regex]::Matches($executableContent, '(?<!@)@[A-Z][A-Z0-9_]*', 'IgnoreCase') |
            ForEach-Object { $_.Value.ToUpperInvariant() } |
            Sort-Object -Unique
        foreach ($parameter in $parameterNames) {
            if ($parameter -notmatch '^@(I|O|IO|V|T|S|C)_') {
                $failures.Add("Prefijo T-SQL no permitido '$parameter': $($file.FullName)")
            }
        }

        $isConsolidatedCommand =
            $file.FullName -match '[\\/]Commands[\\/]' -and
            $executableContent -match '(?<!@)@I_ACCION\b'

        if ($isConsolidatedCommand -and
            $content -notmatch 'Acciones admitidas mediante @I_ACCION:') {
            $failures.Add("El Command con @I_ACCION debe documentar sus acciones: $($file.FullName)")
        }

        if ($isConsolidatedCommand -and $content -notmatch 'Permiso API:') {
            $failures.Add("El Command con @I_ACCION debe documentar el permiso de sus acciones: $($file.FullName)")
        }
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host 'Estándares de documentación aprobados.'
exit 0
