/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.Api.Tests.HealthResponseTests
Archivo: HealthResponseTests.cs | Versión: 1.0.0 | Fecha: 2026-08-19 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Verifica el contrato inicial del control de salud.
Historial: 1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
using OxiTigre.Contracts.System;

namespace OxiTigre.Api.Tests;

/// <summary>Contiene pruebas unitarias del contrato <see cref="HealthResponse"/>.</summary>
public sealed class HealthResponseTests
{
    /// <summary>Comprueba que el ambiente informado no sea alterado por el contrato.</summary>
    [Fact]
    public void HealthResponse_PreservesEnvironment()
    {
        var response = new HealthResponse("Healthy", "Testing", DateTimeOffset.UtcNow);
        Assert.Equal("Testing", response.Environment);
    }
}
