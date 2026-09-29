/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Tests.UsernameGeneratorTests
Archivo: UsernameGeneratorTests.cs | Versión: 1.0.0 | Fecha: 2026-08-19 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Verifica usuarios corporativos y códigos de error.
Historial: 1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
using OxiTigre.BLL.Security;
using OxiTigre.Domain.Errors;

namespace OxiTigre.BLL.Tests;

/// <summary>Contiene pruebas de convenciones funcionales compartidas.</summary>
public sealed class UsernameGeneratorTests
{
    /// <summary>Verifica el uso de la inicial de cada nombre.</summary>
    [Fact]
    public void Generate_UsesEachGivenNameInitial_WhenThereAreMultipleNames() =>
        Assert.Equal("AOCAUZI", UsernameGenerator.Generate("Agustin Omar", "Cauzi"));

    /// <summary>Verifica el uso de dos letras cuando existe un solo nombre.</summary>
    [Fact]
    public void Generate_UsesFirstTwoLetters_WhenThereIsOneGivenName() =>
        Assert.Equal("AGCAUZI", UsernameGenerator.Generate("Agustin", "Cauzi"));

    /// <summary>Verifica la ampliación progresiva del primer nombre ante duplicados.</summary>
    [Fact]
    public void Generate_ExpandsFirstName_WhenCandidateExists()
    {
        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "AOCAUZI" };
        Assert.Equal("AGOCAUZI", UsernameGenerator.Generate("Agustin Omar", "Cauzi", existing));
    }

    /// <summary>Verifica la composición variable del módulo y los cuatro dígitos del error.</summary>
    /// <param name="module">Módulo utilizado por el caso de prueba.</param>
    /// <param name="sequence">Consecutivo utilizado por el caso de prueba.</param>
    /// <param name="expected">Código numérico esperado.</param>
    [Theory]
    [InlineData(1, 1, 10001)]
    [InlineData(12, 1, 120001)]
    [InlineData(123, 1, 1230001)]
    public void ErrorCode_UsesModuleAndFourDigitSequence(int module, int sequence, long expected)
    {
        Assert.Equal(expected, ErrorCode.Create(module, sequence).Value);
    }
}
