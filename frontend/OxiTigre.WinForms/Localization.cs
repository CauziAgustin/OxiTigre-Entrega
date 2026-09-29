/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.WinForms.Localization
Archivo: Localization.cs | Versión: 1.2.1 | Fecha: 2026-09-21 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Aplica recursos nativos y cultura seleccionada sin dependencias externas.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Persistencia local del idioma previo al login.
Historial: 1.2.0 | 2026-09-20 | FABRICA | Agustin Omar Cauzi | Importación validada de paquetes de idioma JSON.
Historial: 1.2.1 | 2026-09-21 | FABRICA | Agustin Omar Cauzi | Validación de formato y tamaño al cargar; reemplazo seguro de paquetes instalados.
===============================================================================
*/
using System.Collections;
using System.Globalization;
using System.Resources;
using System.Text;
using System.Text.Json;

namespace OxiTigre.WinForms;

/// <summary>Resuelve textos visibles y formatos según la preferencia del usuario.</summary>
internal static class Localization
{
    private static readonly ResourceManager Resources = new(
        "OxiTigre.WinForms.Properties.Resources",
        typeof(Localization).Assembly
    );
    private static readonly string ApplicationDataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "OxiTigre"
    );
    private static readonly string CultureFile = Path.Combine(
        ApplicationDataDirectory,
        "culture.txt"
    );
    private static readonly string LanguagesDirectory = Path.Combine(
        ApplicationDataDirectory,
        "languages"
    );
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
    };
    private static IReadOnlyDictionary<string, string>? importedTexts;
    private static string? importedCulture;

    /// <summary>Recupera el último idioma usado en este equipo o español como valor inicial.</summary>
    /// <returns>Código de cultura instalado que debe usar la interfaz.</returns>
    internal static string LoadSavedCulture()
    {
        try
        {
            return NormalizeInstalled(File.ReadAllText(CultureFile));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return "es-AR";
        }
    }

    /// <summary>Guarda el idioma local para presentar también el próximo login correctamente.</summary>
    /// <param name="cultureCode">Código de cultura que se conservará en el equipo.</param>
    internal static void SaveCulture(string cultureCode)
    {
        Directory.CreateDirectory(ApplicationDataDirectory);
        File.WriteAllText(CultureFile, NormalizeInstalled(cultureCode));
    }

    /// <summary>Aplica una cultura incluida o importada al hilo de interfaz.</summary>
    /// <param name="cultureCode">Código de cultura incluida o instalada que se aplicará.</param>
    /// <exception cref="CultureNotFoundException">La cultura indicada no existe en la plataforma.</exception>
    internal static void SetCulture(string cultureCode)
    {
        var normalized = NormalizeInstalled(cultureCode);
        var culture = CultureInfo.GetCultureInfo(normalized);
        var pack = TryReadInstalledPack(normalized);
        importedCulture = pack?.Culture;
        importedTexts = pack?.Translations;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    /// <summary>Obtiene un recurso o la clave cuando falta, para que el defecto sea visible.</summary>
    /// <param name="key">Clave del texto localizado que se solicita.</param>
    /// <returns>Texto localizado o la clave cuando no existe una traducción.</returns>
    internal static string Text(string key) =>
        importedCulture == CultureInfo.CurrentUICulture.Name
        && importedTexts?.TryGetValue(key, out var text) == true
            ? text
            : Resources.GetString(key, CultureInfo.CurrentUICulture) ?? key;

    /// <summary>Formatea un recurso usando la cultura activa.</summary>
    /// <param name="key">Clave del formato localizado.</param>
    /// <param name="values">Valores que completan los marcadores del formato.</param>
    /// <returns>Texto localizado con sus marcadores reemplazados.</returns>
    internal static string Format(string key, params object?[] values) =>
        string.Format(CultureInfo.CurrentCulture, Text(key), values);

    /// <summary>Enumera idiomas incluidos y paquetes importados que todavía son válidos.</summary>
    /// <returns>Idiomas disponibles para seleccionar en la aplicación.</returns>
    internal static IReadOnlyList<LanguageOption> AvailableLanguages()
    {
        var languages = new List<LanguageOption>
        {
            new("es-AR", Text("Config_Spanish"), false),
            new("en-US", Text("Config_English"), false),
        };
        if (!Directory.Exists(LanguagesDirectory))
            return languages;

        foreach (var file in Directory.EnumerateFiles(LanguagesDirectory, "*.json"))
        {
            try
            {
                var pack = ReadPack(file);
                if (languages.All(item => item.CultureCode != pack.Culture))
                    languages.Add(new LanguageOption(pack.Culture, pack.DisplayName, true));
            }
            catch (Exception exception)
                when (exception
                        is IOException
                            or UnauthorizedAccessException
                            or JsonException
                            or InvalidDataException
                            or CultureNotFoundException
                )
            {
                // Un paquete alterado fuera de la aplicación no debe impedir el inicio.
            }
        }

        return languages
            .OrderBy(item => item.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    /// <summary>Valida e instala un paquete JSON para que aparezca en el selector de idioma.</summary>
    /// <param name="sourcePath">Archivo JSON seleccionado por el administrador.</param>
    /// <returns>Idioma instalado y cantidad de textos que aporta.</returns>
    /// <exception cref="ArgumentException">La ruta de origen está vacía.</exception>
    /// <exception cref="InvalidDataException">El archivo no cumple el esquema o contiene traducciones inválidas.</exception>
    /// <exception cref="JsonException">El contenido no es JSON válido.</exception>
    /// <exception cref="IOException">No se puede leer o guardar el archivo.</exception>
    /// <exception cref="UnauthorizedAccessException">El equipo no permite acceder al archivo o al perfil local.</exception>
    internal static LanguageImportResult ImportLanguage(string sourcePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        var file = new FileInfo(sourcePath);
        if (!file.Exists || !file.Extension.Equals(".json", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Seleccioná un archivo de idioma con extensión .json.");
        var pack = ReadPack(file.FullName);
        if (pack.Culture is "es-AR" or "en-US")
            throw new InvalidDataException(
                "Los idiomas incluidos se actualizan con el sistema y no pueden reemplazarse."
            );

        Directory.CreateDirectory(LanguagesDirectory);
        var destination = Path.Combine(LanguagesDirectory, $"{pack.Culture}.json");
        var temporary = Path.Combine(
            LanguagesDirectory,
            $"{pack.Culture}.{Guid.NewGuid():N}.tmp"
        );
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(pack, JsonOptions));
            File.Move(temporary, destination, true);
        }
        finally
        {
            if (File.Exists(temporary))
                File.Delete(temporary);
        }

        return new LanguageImportResult(pack.Culture, pack.DisplayName, pack.Translations.Count);
    }

    /// <summary>Conserva una cultura instalada válida o vuelve a español cuando falta su paquete.</summary>
    /// <param name="cultureCode">Código almacenado o seleccionado en el equipo.</param>
    /// <returns>Cultura que puede activarse con los recursos disponibles.</returns>
    private static string NormalizeInstalled(string? cultureCode)
    {
        var value = cultureCode?.Trim();
        if (value is "es-AR" or "en-US")
            return value;
        return value is not null && TryReadInstalledPack(value) is not null ? value : "es-AR";
    }

    /// <summary>Lee un paquete local sin impedir el acceso si fue alterado o ya no existe.</summary>
    /// <param name="cultureCode">Cultura específica del paquete buscado.</param>
    /// <returns>Paquete validado o <see langword="null"/> si no está disponible.</returns>
    private static LanguagePack? TryReadInstalledPack(string cultureCode)
    {
        if (cultureCode is "es-AR" or "en-US")
            return null;
        try
        {
            var culture = CultureInfo.GetCultureInfo(cultureCode).Name;
            var path = Path.Combine(LanguagesDirectory, $"{culture}.json");
            return File.Exists(path) ? ReadPack(path) : null;
        }
        catch (Exception exception)
            when (exception
                    is IOException
                        or UnauthorizedAccessException
                        or JsonException
                        or InvalidDataException
                        or CultureNotFoundException
            )
        {
            return null;
        }
    }

    /// <summary>Valida tamaño, esquema, cultura, claves conocidas y formatos antes de aceptar un JSON.</summary>
    /// <param name="path">Ruta del paquete a leer.</param>
    /// <returns>Paquete normalizado con traducciones verificadas.</returns>
    /// <exception cref="InvalidDataException">El contenido no respeta el contrato de idiomas.</exception>
    /// <exception cref="JsonException">El archivo no contiene JSON válido.</exception>
    /// <exception cref="IOException">No puede leerse el archivo.</exception>
    /// <exception cref="UnauthorizedAccessException">No hay permiso para leer el archivo.</exception>
    private static LanguagePack ReadPack(string path)
    {
        if (new FileInfo(path).Length is <= 0 or > 1_048_576)
            throw new InvalidDataException("El archivo de idioma debe pesar entre 1 byte y 1 MB.");

        var json = File.ReadAllText(path);
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 4 });
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("La raíz del archivo debe ser un objeto JSON.");

        var rootNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (!rootNames.Add(property.Name))
                throw new InvalidDataException($"La propiedad '{property.Name}' está repetida.");
            if (
                property.Name
                is not ("schemaVersion" or "culture" or "displayName" or "translations")
            )
                throw new InvalidDataException(
                    $"La propiedad '{property.Name}' no pertenece al formato de idioma."
                );
        }

        var pack =
            JsonSerializer.Deserialize<LanguagePack>(json, JsonOptions)
            ?? throw new InvalidDataException("El archivo de idioma está vacío.");
        if (pack.SchemaVersion != 1)
            throw new InvalidDataException("La versión de esquema admitida es 1.");

        CultureInfo culture;
        try
        {
            culture = CultureInfo.GetCultureInfo(pack.Culture?.Trim() ?? string.Empty);
        }
        catch (CultureNotFoundException)
        {
            throw new InvalidDataException("El código de cultura no es válido.");
        }
        if (culture.IsNeutralCulture || culture.Name.Length > 10 || culture.Name != pack.Culture)
            throw new InvalidDataException(
                "Usá una cultura específica y canónica, por ejemplo pt-BR."
            );
        if (string.IsNullOrWhiteSpace(pack.DisplayName) || pack.DisplayName.Trim().Length > 80)
            throw new InvalidDataException(
                "El nombre visible del idioma es obligatorio y admite hasta 80 caracteres."
            );
        if (pack.Translations is null || pack.Translations.Count == 0)
            throw new InvalidDataException("El paquete debe contener al menos una traducción.");

        var sourceTranslations = document.RootElement.GetProperty("translations");
        if (sourceTranslations.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("translations debe ser un objeto de clave y texto.");
        var resourceTexts = BaseTexts();
        var translations = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var property in sourceTranslations.EnumerateObject())
        {
            if (
                !translations.TryAdd(
                    property.Name,
                    property.Value.ValueKind == JsonValueKind.String
                        ? property.Value.GetString() ?? string.Empty
                        : string.Empty
                )
            )
                throw new InvalidDataException($"La traducción '{property.Name}' está repetida.");
            if (!resourceTexts.TryGetValue(property.Name, out var source))
                throw new InvalidDataException(
                    $"La clave '{property.Name}' no existe en la interfaz."
                );
            var translated = translations[property.Name];
            if (string.IsNullOrWhiteSpace(translated) || translated.Length > 4000)
                throw new InvalidDataException(
                    $"La traducción '{property.Name}' está vacía o es demasiado extensa."
                );
            IReadOnlyList<int> sourceParameters;
            IReadOnlyList<int> translatedParameters;
            try
            {
                sourceParameters = Placeholders(source);
                translatedParameters = Placeholders(translated);
            }
            catch (FormatException)
            {
                throw new InvalidDataException(
                    $"La traducción '{property.Name}' contiene llaves o parámetros de formato inválidos."
                );
            }

            if (!sourceParameters.SequenceEqual(translatedParameters))
                throw new InvalidDataException(
                    $"La traducción '{property.Name}' no conserva sus parámetros {{0}}, {{1}}, etc."
                );
        }

        return new LanguagePack(1, culture.Name, pack.DisplayName.Trim(), translations);
    }

    /// <summary>Obtiene las claves y textos españoles que delimitan lo traducible en esta versión.</summary>
    /// <returns>Recursos de interfaz indexados por su clave estable.</returns>
    /// <exception cref="InvalidDataException">No se encuentran los recursos base incrustados.</exception>
    private static Dictionary<string, string> BaseTexts()
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        var set =
            Resources.GetResourceSet(CultureInfo.GetCultureInfo("es-AR"), true, true)
            ?? throw new InvalidDataException("No se pudo leer el idioma base de la aplicación.");
        foreach (DictionaryEntry item in set)
            if (item.Key is string key && item.Value is string value)
                result[key] = value;
        return result;
    }

    /// <summary>Obtiene los índices reales de un formato compuesto, excluyendo llaves escapadas.</summary>
    /// <param name="value">Texto que puede contener parámetros de <c>string.Format</c>.</param>
    /// <returns>Índices de parámetros, ordenados y con repeticiones.</returns>
    /// <exception cref="FormatException">El formato contiene llaves o índices inválidos.</exception>
    private static IReadOnlyList<int> Placeholders(string value)
    {
        CompositeFormat.Parse(value);
        var indices = new List<int>();

        for (var index = 0; index < value.Length; index++)
        {
            if (value[index] != '{')
                continue;
            if (index + 1 < value.Length && value[index + 1] == '{')
            {
                index++;
                continue;
            }

            var start = index + 1;
            var end = start;
            while (end < value.Length && char.IsAsciiDigit(value[end]))
                end++;

            if (!int.TryParse(value.AsSpan(start, end - start), CultureInfo.InvariantCulture, out var parameter))
                throw new FormatException("El índice de formato no es válido.");
            indices.Add(parameter);
        }

        indices.Sort();
        return indices;
    }

    private sealed record LanguagePack(
        int SchemaVersion,
        string Culture,
        string DisplayName,
        IReadOnlyDictionary<string, string> Translations
    );
}

/// <summary>Describe una cultura seleccionable en el equipo.</summary>
internal sealed record LanguageOption(string CultureCode, string DisplayName, bool Imported);

/// <summary>Resume un paquete instalado después de superar todas las validaciones.</summary>
internal sealed record LanguageImportResult(
    string CultureCode,
    string DisplayName,
    int TranslationCount
);
