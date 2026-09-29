/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.Api.Tests.SalesContractTests
Archivo: SalesContractTests.cs | Versión: 1.0.0 | Fecha: 2026-08-21 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Verifica el contrato HTTP mínimo de promociones en pedidos.
Historial: 1.0.0 | 2026-08-21 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
using System.Text.Json;
using OxiTigre.Contracts.Commercial;

namespace OxiTigre.Api.Tests;

/// <summary>Pruebas de compatibilidad del contrato comercial compartido.</summary>
public sealed class SalesContractTests
{
    /// <summary>El identificador promocional viaja en el renglón sin exponer contexto de sesión.</summary>
    [Fact]
    public void SaveOrderDetailRequest_RoundTrip_PreservesPromotion()
    {
        var detail = new SaveOrderDetailRequest(1, 2, 4, 100, 0, .21m, 9);
        var restored = JsonSerializer.Deserialize<SaveOrderDetailRequest>(JsonSerializer.Serialize(detail));

        Assert.Equal(9, restored?.PromotionId);
    }
}
