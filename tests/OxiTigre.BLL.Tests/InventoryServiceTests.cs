/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Tests.InventoryServiceTests
Archivo: InventoryServiceTests.cs | Versión: 1.1.0 | Fecha: 2026-08-24 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Verifica autorización, aislamiento y cantidades de Inventario.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-24 | FABRICA | Agustin Omar Cauzi | Validación de tipos de trazabilidad.
===============================================================================
*/
using OxiTigre.BLL.Inventory;
using OxiTigre.BLL.Security;

namespace OxiTigre.BLL.Tests;

/// <summary>Prueba las reglas mínimas del servicio de Inventario.</summary>
public sealed class InventoryServiceTests
{
    /// <summary>Comprueba que la consulta utiliza el contexto autenticado.</summary>
    [Fact]
    public async Task GetAsync_WithPermission_UsesSessionContext()
    {
        var store = new FakeInventoryStore();
        await new InventoryService(store).GetAsync(Identity("INVENTARIO.CONSULTAR"), CancellationToken.None);
        Assert.Equal((2, 4, 8), (store.CompanyId, store.UserId, store.SessionId));
    }

    /// <summary>Comprueba que una mutación requiere el permiso de gestión.</summary>
    [Fact]
    public async Task SaveCategoryAsync_WithoutManagePermission_Throws()
    {
        var service = new InventoryService(new FakeInventoryStore());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.SaveCategoryAsync(
            Identity("INVENTARIO.CONSULTAR"), new(null, "TUBOS", "Tubos", null, "ACTIVO", null), CancellationToken.None));
    }

    /// <summary>Comprueba que nunca se confirma una cantidad nula o negativa.</summary>
    [Fact]
    public async Task CreateMovementAsync_WithInvalidQuantity_Throws()
    {
        var service = new InventoryService(new FakeInventoryStore());
        var change = new InventoryMovementChange("ENTRADA", DateTime.UtcNow, null, [new(1, null, null, 1, null, 0)]);
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateMovementAsync(
            Identity("INVENTARIO.GESTIONAR"), change, CancellationToken.None));
    }

    /// <summary>Comprueba que un producto no acepte un tipo de trazabilidad inventado.</summary>
    [Fact]
    public async Task SaveProductAsync_WithInvalidTrackingType_Throws()
    {
        var service = new InventoryService(new FakeInventoryStore());
        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveProductAsync(
            Identity("INVENTARIO.GESTIONAR"), new ProductChange(null, 1, 1, "Producto", null, null,
                "ACTIVO", null, "PRODUCTO", "OTRO"), CancellationToken.None));
    }

    private static SessionIdentity Identity(params string[] permissions) => new(8, 4, 2, "OXITIGRE", "AOCAUZI",
        "Agustin Cauzi", DateTimeOffset.UtcNow.AddHours(1), false, [], permissions, Guid.NewGuid());

    private sealed class FakeInventoryStore : IInventoryStore
    {
        public long CompanyId { get; private set; }
        public long UserId { get; private set; }
        public long SessionId { get; private set; }
        public Task<InventorySnapshot> GetAsync(long companyId, string companyCode, long userId, long sessionId, CancellationToken cancellationToken) { CompanyId = companyId; UserId = userId; SessionId = sessionId; return Task.FromResult(new InventorySnapshot([], [], [], [], [], [], [])); }
        public Task<long> SaveMeasurementUnitAsync(string companyCode, long userId, long sessionId, MeasurementUnitChange change, CancellationToken cancellationToken) => Task.FromResult(1L);
        public Task<long> SaveCategoryAsync(string companyCode, long userId, long sessionId, ProductCategoryChange change, CancellationToken cancellationToken) => Task.FromResult(1L);
        public Task<long> SaveProductAsync(long companyId, string companyCode, long userId, long sessionId, ProductChange change, CancellationToken cancellationToken) => Task.FromResult(1L);
        public Task<long> SaveWarehouseAsync(long companyId, string companyCode, long userId, long sessionId, WarehouseChange change, CancellationToken cancellationToken) => Task.FromResult(1L);
        public Task<long> SaveLocationAsync(long companyId, string companyCode, long userId, long sessionId, WarehouseLocationChange change, CancellationToken cancellationToken) => Task.FromResult(1L);
        public Task UpdateMinimumStockAsync(long companyId, string companyCode, long userId, long sessionId, MinimumStockChange change, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<long> CreateMovementAsync(long companyId, string companyCode, long userId, long sessionId, InventoryMovementChange change, CancellationToken cancellationToken) => Task.FromResult(1L);
    }
}
