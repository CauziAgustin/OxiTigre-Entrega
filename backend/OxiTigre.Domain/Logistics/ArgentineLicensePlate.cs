/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.Domain.Logistics.ArgentineLicensePlate
Archivo: ArgentineLicensePlate.cs | Versión: 1.0.0 | Fecha: 2026-08-27 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Normaliza y presenta patentes argentinas antiguas y Mercosur.
Historial: 1.0.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
namespace OxiTigre.Domain.Logistics;

/// <summary>Aplica las formas argentinas ABC-123 y AB-123-CD sin guardar separadores.</summary>
public static class ArgentineLicensePlate
{
    /// <summary>Normaliza una patente completa para persistirla.</summary>
    /// <param name="value">Patente escrita con o sin espacios o guiones.</param>
    /// <returns>Seis o siete caracteres en mayúsculas y sin separadores.</returns>
    /// <exception cref="ArgumentException">La patente no corresponde a un formato argentino admitido.</exception>
    public static string Normalize(string? value)
    {
        var normalized = Clean(value);
        if (!IsOld(normalized) && !IsMercosur(normalized))
        {
            throw new ArgumentException("La patente debe tener el formato ABC-123 o AB-123-CD.", nameof(value));
        }

        return normalized;
    }

    /// <summary>Presenta una patente almacenada usando guiones.</summary>
    /// <param name="value">Patente completa con o sin separadores.</param>
    /// <returns>Patente formateada o el valor original cuando es histórico y no reconocido.</returns>
    public static string? Format(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = Clean(value);
        return normalized.Length switch
        {
            6 when IsOld(normalized) => $"{normalized[..3]}-{normalized[3..]}",
            7 when IsMercosur(normalized) => $"{normalized[..2]}-{normalized[2..5]}-{normalized[5..]}",
            _ => value.Trim().ToUpperInvariant()
        };
    }

    /// <summary>Agrega guiones mientras el usuario escribe sin exigir que la patente esté completa.</summary>
    /// <param name="value">Texto parcial ingresado.</param>
    /// <returns>Texto parcial en mayúsculas con los separadores visuales posibles.</returns>
    public static string FormatInput(string? value)
    {
        var normalized = Clean(value);
        if (normalized.Length > 7) normalized = normalized[..7];
        if (normalized.Length <= 3) return normalized;

        var mercosur = normalized.Length >= 2 && char.IsDigit(normalized[2]);
        if (!mercosur) return $"{normalized[..3]}-{normalized[3..]}";
        return normalized.Length <= 5
            ? $"{normalized[..2]}-{normalized[2..]}"
            : $"{normalized[..2]}-{normalized[2..5]}-{normalized[5..]}";
    }

    /// <summary>Normaliza una patente argentina quitando separadores y convirtiéndola a mayúsculas.</summary>
    /// <param name="value">Texto que se normaliza, valida o asigna.</param>
    /// <returns>Texto normalizado, localizado o formateado por la operación.</returns>
    private static string Clean(string? value) => new((value ?? string.Empty)
        .Where(char.IsLetterOrDigit)
        .Select(char.ToUpperInvariant)
        .ToArray());

    /// <summary>Indica si el valor coincide con el formato argentino anterior.</summary>
    /// <param name="value">Texto que se normaliza, valida o asigna.</param>
    /// <returns>Verdadero cuando el valor coincide con el formato argentino anterior; en caso contrario, falso.</returns>
    private static bool IsOld(string value) => value.Length == 6
        && value[..3].All(char.IsLetter)
        && value[3..].All(char.IsDigit);

    /// <summary>Indica si el valor coincide con el formato argentino Mercosur.</summary>
    /// <param name="value">Texto que se normaliza, valida o asigna.</param>
    /// <returns>Verdadero cuando el valor coincide con el formato argentino mercosur; en caso contrario, falso.</returns>
    private static bool IsMercosur(string value) => value.Length == 7
        && value[..2].All(char.IsLetter)
        && value[2..5].All(char.IsDigit)
        && value[5..].All(char.IsLetter);
}
