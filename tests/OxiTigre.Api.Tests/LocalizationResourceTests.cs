/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.Api.Tests.LocalizationResourceTests
Archivo: LocalizationResourceTests.cs | Versión: 1.1.1 | Fecha: 2026-09-21 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Impide publicar una interfaz con recursos faltantes en inglés o español.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-29 | FABRICA | Agustin Omar Cauzi | Paridad de recursos de la aplicación móvil.
Historial: 1.1.1 | 2026-09-21 | FABRICA | Agustin Omar Cauzi | Alcance Desktop del corte académico; Mobile se valida en desarrollo.
===============================================================================
*/
using System.Xml.Linq;

namespace OxiTigre.Api.Tests;

/// <summary>Verifica la paridad estructural de los recursos visibles.</summary>
public sealed class LocalizationResourceTests
{
    /// <summary>Comprueba que ninguna cultura tenga claves faltantes.</summary>
    [Fact]
    public void SpanishAndEnglishResourcesHaveTheSameKeys()
    {
        var root = FindRepositoryRoot();
        var resources = Path.Combine(root, "frontend", "OxiTigre.WinForms", "Properties");
        var spanish = Keys(Path.Combine(resources, "Resources.resx"));
        var english = Keys(Path.Combine(resources, "Resources.en.resx"));

        Assert.Equal(spanish, english);
    }

    private static string[] Keys(string path) => XDocument.Load(path).Root!.Elements("data")
        .Select(element => (string)element.Attribute("name")!).Order(StringComparer.Ordinal).ToArray();

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "OxiTigre.sln"))) return directory.FullName;
        throw new DirectoryNotFoundException("No se encontró la raíz del repositorio.");
    }
}
