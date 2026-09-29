/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.Api.Tests.PurchasingContractTests
Archivo: PurchasingContractTests.cs | Versión: 1.0.0 | Fecha: 2026-08-24 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Verifica que una recepción parcial conserve cantidades y versión de orden.
Historial: 1.0.0 | 2026-08-24 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
using System.Text.Json;
using OxiTigre.Contracts.Purchasing;

namespace OxiTigre.Api.Tests;

/// <summary>Prueba la compatibilidad del contrato HTTP de Compras.</summary>
public sealed class PurchasingContractTests
{
    /// <summary>Comprueba que aceptado, rechazado y dañado viajan sin perder trazabilidad.</summary>
    [Fact]
    public void CreateGoodsReceiptRequest_RoundTrip_PreservesDifferences()
    {
        var request = new CreateGoodsReceiptRequest(DateTime.UtcNow, "R-1", null,
            [new GoodsReceiptDetailRequest(4, 7, 2, 1, "Faltante y daño",
                [new GoodsReceiptLotRequest("LOTE-1", 7, null, null)], [], [])], "AQIDBAUGBwg=");
        var restored = JsonSerializer.Deserialize<CreateGoodsReceiptRequest>(JsonSerializer.Serialize(request));

        Assert.Equal((7, 2, 1, "Faltante y daño"), (restored!.Details[0].AcceptedQuantity,
            restored.Details[0].RejectedQuantity, restored.Details[0].DamagedQuantity,
            restored.Details[0].DifferenceReason));
        Assert.Equal("LOTE-1", restored.Details[0].Lots[0].Code);
    }
}
