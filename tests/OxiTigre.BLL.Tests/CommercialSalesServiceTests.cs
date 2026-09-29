/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Tests.CommercialSalesServiceTests
Archivo: CommercialSalesServiceTests.cs | Versión: 1.2.0 | Fecha: 2026-08-21 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Verifica permiso, aislamiento y validación monetaria y promocional del flujo comercial.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-21 | FABRICA | Agustin Omar Cauzi | Reglas y contexto seguro de promociones.
Historial: 1.2.0 | 2026-08-21 | FABRICA | Agustin Omar Cauzi | Escala, rango y acumulación monetaria segura.
===============================================================================
*/
using OxiTigre.BLL.Commercial;
using OxiTigre.BLL.Security;

namespace OxiTigre.BLL.Tests;

/// <summary>Pruebas unitarias mínimas del servicio de pedidos y ventas.</summary>
public sealed class CommercialSalesServiceTests
{
    /// <summary>La consulta usa únicamente empresa y usuario de la sesión.</summary>
    [Fact]
    public async Task GetAsync_WithPermission_UsesSessionContext()
    {
        var store = new FakeStore(); await new CommercialSalesService(store).GetAsync(Identity("COMERCIAL.CONSULTAR"), CancellationToken.None);
        Assert.Equal((2, 4, 8), (store.CompanyId, store.UserId, store.SessionId));
    }

    /// <summary>Una consulta no habilita mutaciones.</summary>
    [Fact]
    public async Task SavePriceListAsync_WithoutManagePermission_Throws()
    {
        var service = new CommercialSalesService(new FakeStore());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.SavePriceListAsync(Identity("COMERCIAL.CONSULTAR"), new(null, "GENERAL", "General", "ARS", null, null, "ACTIVO", null), CancellationToken.None));
    }

    /// <summary>Los porcentajes no pueden superar el cien por ciento.</summary>
    [Fact]
    public async Task SaveOrderAsync_WithInvalidPercentage_Throws()
    {
        var service = new CommercialSalesService(new FakeStore());
        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveOrderAsync(Identity("COMERCIAL.VENTAS_GESTIONAR"), new(null, 1, 1, DateTime.UtcNow, "ARS", null, [new(1, 1, 1, 10, 1.01m, 0)], null), CancellationToken.None));
    }

    /// <summary>La promoción usa empresa, usuario y sesión autenticados y normaliza sus códigos.</summary>
    [Fact]
    public async Task SavePromotionAsync_WithPaidQuantityRule_UsesSessionContext()
    {
        var store = new FakeStore(); var service = new CommercialSalesService(store);
        await service.SavePromotionAsync(Identity("COMERCIAL.VENTAS_GESTIONAR"),
            new(null, 1, " promo-2x1 ", "Dos por uno", null, "cantidad_pagada", 2, 1,
                null, null, null, null, null, "activo", null), CancellationToken.None);

        Assert.Equal((2, 4, 8), (store.CompanyId, store.UserId, store.SessionId));
        Assert.Equal(("PROMO-2X1", "CANTIDAD_PAGADA", "ACTIVO"),
            (store.Promotion!.Code, store.Promotion.PromotionType, store.Promotion.StatusCode));
    }

    /// <summary>No permite sumar un descuento manual a una promoción del mismo renglón.</summary>
    [Fact]
    public async Task SaveOrderAsync_WithPromotionAndManualDiscount_Throws()
    {
        var service = new CommercialSalesService(new FakeStore());
        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveOrderAsync(
            Identity("COMERCIAL.VENTAS_GESTIONAR"), new(null, 1, 1, DateTime.UtcNow, "ARS", null,
                [new(1, 1, 2, 10, .1m, 0, 3)], null), CancellationToken.None));
    }

    /// <summary>Un precio con más de cuatro decimales no llega a SQL Server.</summary>
    [Fact]
    public async Task SavePriceAsync_WithExcessScale_Throws()
    {
        var service = new CommercialSalesService(new FakeStore());
        await Assert.ThrowsAsync<ArgumentException>(() => service.SavePriceAsync(
            Identity("COMERCIAL.VENTAS_GESTIONAR"), new(null, 1, 1, 10.00001m, "ACTIVO", null), CancellationToken.None));
    }

    /// <summary>Los porcentajes respetan la escala DECIMAL(9,6).</summary>
    [Fact]
    public async Task SaveOrderAsync_WithExcessPercentageScale_Throws()
    {
        var service = new CommercialSalesService(new FakeStore());
        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveOrderAsync(
            Identity("COMERCIAL.VENTAS_GESTIONAR"), new(null, 1, 1, DateTime.UtcNow, "ARS", null,
                [new(1, 1, 1, 10, .1234567m, 0)], null), CancellationToken.None));
    }

    /// <summary>Un importe promocional fuera de DECIMAL(19,4) se rechaza de forma funcional.</summary>
    [Fact]
    public async Task SavePromotionAsync_WithOutOfRangePackagePrice_Throws()
    {
        var service = new CommercialSalesService(new FakeStore());
        await Assert.ThrowsAsync<ArgumentException>(() => service.SavePromotionAsync(
            Identity("COMERCIAL.VENTAS_GESTIONAR"), new(null, 1, "PAQUETE", "Paquete", null,
                "PRECIO_PAQUETE", 1, null, null, null, 1000000000000000m, null, null, "ACTIVO", null),
            CancellationToken.None));
    }

    /// <summary>La suma de renglones no puede superar la capacidad monetaria del pedido.</summary>
    [Fact]
    public async Task SaveOrderAsync_WithSubtotalSumOutOfRange_Throws()
    {
        var service = new CommercialSalesService(new FakeStore());
        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveOrderAsync(
            Identity("COMERCIAL.VENTAS_GESTIONAR"), new(null, 1, 1, DateTime.UtcNow, "ARS", null,
                [new(1, 1, 1, 600000000000000m, 0, 0), new(2, 1, 1, 600000000000000m, 0, 0)], null),
            CancellationToken.None));
    }

    /// <summary>El control previo evita que la multiplicación decimal desborde dentro de .NET.</summary>
    [Fact]
    public async Task SaveOrderAsync_WithMultiplicationOverflow_ThrowsControlledError()
    {
        var service = new CommercialSalesService(new FakeStore());
        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveOrderAsync(
            Identity("COMERCIAL.VENTAS_GESTIONAR"), new(null, 1, 1, DateTime.UtcNow, "ARS", null,
                [new(1, 1, 999999999999999.9999m, 999999999999999.9999m, 0, 0)], null),
            CancellationToken.None));
    }

    /// <summary>La cantidad acumulada de renglones compatibles tampoco desborda su columna SQL.</summary>
    [Fact]
    public async Task SaveOrderAsync_WithGroupedQuantityOutOfRange_Throws()
    {
        var service = new CommercialSalesService(new FakeStore());
        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveOrderAsync(
            Identity("COMERCIAL.VENTAS_GESTIONAR"), new(null, 1, 1, DateTime.UtcNow, "ARS", null,
                [new(1, 1, 600000000000000m, 0, 0, 0), new(1, 1, 600000000000000m, 0, 0, 0)], null),
            CancellationToken.None));
    }

    /// <summary>Un servicio puede guardarse sin depósito y conserva el activo vinculado normalizado.</summary>
    [Fact]
    public async Task SaveOrderAsync_WithServiceAndAsset_NormalizesTraceability()
    {
        var store = new FakeStore();
        await new CommercialSalesService(store).SaveOrderAsync(Identity("COMERCIAL.VENTAS_GESTIONAR"),
            new(null, 1, 1, DateTime.UtcNow, "ARS", null,
                [new(7, null, 1, 12000, 0, .21m)], null,
                [new(3, 7, " cliente_servicio ", " retiro_cliente ", " Tubo recibido. ")]),
            CancellationToken.None);

        Assert.Null(store.Order!.Details[0].WarehouseId);
        Assert.Equal(("CLIENTE_SERVICIO", "RETIRO_CLIENTE", "Tubo recibido."),
            (store.Order.Assets![0].LinkType, store.Order.Assets[0].ReturnMode, store.Order.Assets[0].Observation));
    }

    /// <summary>La observación del activo es obligatoria porque forma parte de la trazabilidad.</summary>
    [Fact]
    public async Task SaveOrderAsync_WithAssetWithoutObservation_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => new CommercialSalesService(new FakeStore()).SaveOrderAsync(
            Identity("COMERCIAL.VENTAS_GESTIONAR"), new(null, 1, 1, DateTime.UtcNow, "ARS", null,
                [new(7, null, 1, 12000, 0, .21m)], null,
                [new(3, 7, "CLIENTE_SERVICIO", "RETIRO_CLIENTE", " ")]), CancellationToken.None));
    }

    private static SessionIdentity Identity(params string[] permissions) => new(8, 4, 2, "OXITIGRE", "AOCAUZI", "Agustin Cauzi", DateTimeOffset.UtcNow.AddHours(1), false, [], permissions, Guid.NewGuid());
    private sealed class FakeStore : ICommercialSalesStore
    {
        internal long CompanyId { get; private set; }
        internal long UserId { get; private set; }
        internal long SessionId { get; private set; }
        internal PromotionChange? Promotion { get; private set; }
        internal OrderChange? Order { get; private set; }
        public Task<SalesSnapshot> GetAsync(long companyId, string companyCode, long userId, long sessionId, CancellationToken cancellationToken) { Context(companyId, userId, sessionId); return Task.FromResult(new SalesSnapshot([], [], [], [], [], [], [], [], [], [])); }
        public Task<long> SavePriceListAsync(long companyId, string companyCode, long userId, long sessionId, PriceListChange change, CancellationToken cancellationToken) => Task.FromResult(1L);
        public Task<long> SavePriceAsync(long companyId, string companyCode, long userId, long sessionId, PriceListProductChange change, CancellationToken cancellationToken) => Task.FromResult(1L);
        public Task<long> SavePromotionAsync(long companyId, string companyCode, long userId, long sessionId, PromotionChange change, CancellationToken cancellationToken) { Context(companyId, userId, sessionId); Promotion = change; return Task.FromResult(1L); }
        public Task<long> SaveOrderAsync(long companyId, string companyCode, long userId, long sessionId, OrderChange change, CancellationToken cancellationToken) { Order = change; return Task.FromResult(1L); }
        public Task ConfirmOrderAsync(long companyId, string companyCode, long userId, long sessionId, long orderId, byte[] rowVersion, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task CancelOrderAsync(long companyId, string companyCode, long userId, long sessionId, long orderId, byte[] rowVersion, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<long> CreateSaleAsync(long companyId, string companyCode, long userId, long sessionId, long orderId, DateTime saleDateUtc, byte[] rowVersion, CancellationToken cancellationToken) => Task.FromResult(1L);
        private void Context(long companyId, long userId, long sessionId) { CompanyId = companyId; UserId = userId; SessionId = sessionId; }
    }
}
