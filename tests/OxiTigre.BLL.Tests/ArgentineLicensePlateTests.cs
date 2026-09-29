/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Tests.ArgentineLicensePlateTests
Archivo: ArgentineLicensePlateTests.cs | Versión: 1.0.0 | Fecha: 2026-08-27 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Verifica los formatos argentinos admitidos para vehículos logísticos.
Historial: 1.0.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
using OxiTigre.Domain.Logistics;

namespace OxiTigre.BLL.Tests;

/// <summary>Comprueba normalización, formato visual y rechazo de patentes inválidas.</summary>
public sealed class ArgentineLicensePlateTests
{
    /// <summary>Comprueba formatos históricos y Mercosur con distintos separadores.</summary>
    /// <param name="input">Texto ingresado por el usuario.</param>
    /// <param name="stored">Valor canónico esperado.</param>
    /// <param name="displayed">Valor visual esperado.</param>
    [Theory]
    [InlineData("abc 123", "ABC123", "ABC-123")]
    [InlineData("ab-123-cd", "AB123CD", "AB-123-CD")]
    public void Normalize_AcceptsArgentineFormats(string input, string stored, string displayed)
    {
        Assert.Equal(stored, ArgentineLicensePlate.Normalize(input));
        Assert.Equal(displayed, ArgentineLicensePlate.Format(input));
    }

    /// <summary>Comprueba que una combinación ajena a los formatos admitidos sea rechazada.</summary>
    [Fact]
    public void Normalize_RejectsUnsupportedPlate() =>
        Assert.Throws<ArgumentException>(() => ArgentineLicensePlate.Normalize("123-ABC"));
}
