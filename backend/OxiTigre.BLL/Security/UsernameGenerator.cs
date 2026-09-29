/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Security.UsernameGenerator
Archivo: UsernameGenerator.cs | Versión: 1.0.0 | Fecha: 2026-08-19 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Genera nombres de usuario corporativos únicos y normalizados.
Historial: 1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
using System.Globalization;
using System.Text;

namespace OxiTigre.BLL.Security;

/// <summary>
/// Genera identificadores de usuario según la convención corporativa de nombres y apellidos.
/// </summary>
public static class UsernameGenerator
{
    /// <summary>
    /// Genera un usuario sin acentos, en mayúsculas, y amplía el primer nombre ante una colisión.
    /// </summary>
    /// <param name="givenNames">Uno o más nombres de la persona.</param>
    /// <param name="surname">Apellido utilizado como parte estable del usuario.</param>
    /// <param name="existingUsernames">Usuarios existentes contra los que se comprueba unicidad.</param>
    /// <returns>El primer nombre de usuario disponible según la convención vigente.</returns>
    /// <exception cref="ArgumentException">El nombre o apellido no contiene caracteres válidos.</exception>
    /// <exception cref="InvalidOperationException">No existe un candidato disponible.</exception>
    public static string Generate(string givenNames, string surname, ISet<string>? existingUsernames = null)
    {
        var names = SplitAndNormalize(givenNames);
        var normalizedSurname = Normalize(surname);

        if (names.Length == 0 || normalizedSurname.Length == 0)
        {
            throw new ArgumentException("El nombre y el apellido son obligatorios.");
        }

        existingUsernames ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var prefixLength = names.Length == 1 ? Math.Min(2, names[0].Length) : 1;

        while (prefixLength <= names[0].Length)
        {
            var firstNamePrefix = names[0][..prefixLength];
            var remainingInitials = string.Concat(names.Skip(1).Select(name => name[0]));
            var candidate = firstNamePrefix + remainingInitials + normalizedSurname;

            if (!existingUsernames.Contains(candidate))
            {
                return candidate;
            }

            prefixLength++;
        }

        var baseCandidate = names[0] + string.Concat(names.Skip(1).Select(name => name[0])) + normalizedSurname;
        for (var suffix = 2; suffix < int.MaxValue; suffix++)
        {
            var candidate = baseCandidate + suffix.ToString(CultureInfo.InvariantCulture);
            if (!existingUsernames.Contains(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("No fue posible generar un nombre de usuario único.");
    }

    /// <summary>Separa un nombre compuesto y normaliza cada término utilizable.</summary>
    /// <param name="value">Texto que se normaliza, valida o asigna.</param>
    /// <returns>Colección de registros u opciones obtenida por la operación.</returns>
    private static string[] SplitAndNormalize(string value) => value
        .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(Normalize)
        .Where(part => part.Length > 0)
        .ToArray();

    /// <summary>Quita diacríticos y caracteres no alfanuméricos de un término de usuario.</summary>
    /// <param name="value">Texto que se normaliza, valida o asigna.</param>
    /// <returns>Texto normalizado, localizado o formateado por la operación.</returns>
    private static string Normalize(string value)
    {
        var decomposed = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark &&
                char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToUpperInvariant(character));
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
