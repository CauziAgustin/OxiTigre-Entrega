/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Tests.PurchasingServiceTests
Archivo: PurchasingServiceTests.cs | Versión: 1.0.0 | Fecha: 2026-08-24 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Verifica permisos y reglas críticas del circuito de Compras.
Historial: 1.0.0 | 2026-08-24 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
using OxiTigre.BLL.Purchasing;
using OxiTigre.BLL.Security;

namespace OxiTigre.BLL.Tests;

/// <summary>Prueba autorización, aislamiento y recepciones parciales.</summary>
public sealed class PurchasingServiceTests
{
    /// <summary>Comprueba que la consulta usa solamente el contexto autenticado.</summary>
    [Fact]
    public async Task GetAsync_WithPermission_UsesSessionContext()
    {
        var store = new FakePurchasingStore();
        await new PurchasingService(store).GetAsync(Identity("COMPRAS.CONSULTAR"), CancellationToken.None);
        Assert.Equal((2, 4, 8), (store.CompanyId, store.UserId, store.SessionId));
    }

    /// <summary>Comprueba que aprobar requiere permiso independiente.</summary>
    [Fact]
    public async Task ApproveOrderAsync_WithoutApprovePermission_Throws()
    {
        var service = new PurchasingService(new FakePurchasingStore());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ApproveOrderAsync(
            Identity("COMPRAS.GESTIONAR"), 1, new byte[8], CancellationToken.None));
    }

    /// <summary>Comprueba que una recepción con diferencia exige explicar el motivo.</summary>
    [Fact]
    public async Task CreateReceiptAsync_WithRejectedQuantityWithoutReason_Throws()
    {
        var service = new PurchasingService(new FakePurchasingStore());
        var change = new GoodsReceiptChange(1, DateTime.UtcNow, "R-1", null,
            [new GoodsReceiptDetailChange(1, 0, 2, 0, null, [], [], [])], new byte[8]);
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateReceiptAsync(
            Identity("COMPRAS.RECIBIR"), change, CancellationToken.None));
    }

    /// <summary>Comprueba que una recepción parcial válida conserva todas sus cantidades.</summary>
    [Fact]
    public async Task CreateReceiptAsync_WithPartialReceipt_PreservesQuantities()
    {
        var store = new FakePurchasingStore();
        var change = new GoodsReceiptChange(1, DateTime.UtcNow, "R-2", null,
            [new GoodsReceiptDetailChange(3, 7, 2, 1, "Faltante y una unidad dañada", [], [], [])], new byte[8]);
        await new PurchasingService(store).CreateReceiptAsync(Identity("COMPRAS.RECIBIR"), change,
            CancellationToken.None);
        Assert.Equal((7, 2, 1), (store.Receipt!.Details[0].AcceptedQuantity,
            store.Receipt.Details[0].RejectedQuantity, store.Receipt.Details[0].DamagedQuantity));
    }

    /// <summary>Comprueba que una multiplicación monetaria fuera de rango se rechaza antes de SQL.</summary>
    [Fact]
    public async Task SaveOrderAsync_WithOverflowingTotal_Throws()
    {
        var service = new PurchasingService(new FakePurchasingStore());
        var change = new PurchaseOrderChange(null, 1, 1, DateTime.UtcNow, null, "ARS", null,
            [new PurchaseOrderDetailChange(1, 999999999999999.9999m, 999999999999999.9999m, 0, 0)], null);

        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveOrderAsync(
            Identity("COMPRAS.GESTIONAR"), change, CancellationToken.None));
    }

    /// <summary>Comprueba que la reversión conserva el motivo normalizado y el contexto autenticado.</summary>
    [Fact]
    public async Task ReverseReceiptAsync_WithPermission_PreservesAuditContext()
    {
        var store = new FakePurchasingStore();
        await new PurchasingService(store).ReverseReceiptAsync(Identity("COMPRAS.RECIBIR"), 12,
            "  Carga duplicada  ", new byte[8], CancellationToken.None);

        Assert.Equal((2, 4, 8, 12, "Carga duplicada"), store.Reversal);
    }

    private static SessionIdentity Identity(params string[] permissions) => new(8, 4, 2, "OXITIGRE", "AOCAUZI",
        "Agustin Cauzi", DateTimeOffset.UtcNow.AddHours(1), false, [], permissions, Guid.NewGuid());

    private sealed class FakePurchasingStore : IPurchasingStore
    {
        public long CompanyId { get; private set; }
        public long UserId { get; private set; }
        public long SessionId { get; private set; }
        public GoodsReceiptChange? Receipt { get; private set; }
        public (long CompanyId, long UserId, long SessionId, long ReceiptId, string Reason) Reversal { get; private set; }

        public Task<PurchasingSnapshot> GetAsync(long companyId, string companyCode, long userId, long sessionId,
            CancellationToken cancellationToken)
        {
            CompanyId = companyId; UserId = userId; SessionId = sessionId;
            return Task.FromResult(new PurchasingSnapshot([], [], [], [], [], []));
        }
        public Task<long> SaveSupplierAsync(long companyId, string companyCode, long userId, long sessionId,
            SupplierChange change, CancellationToken cancellationToken) => Task.FromResult(1L);
        public Task<long> SaveOrderAsync(long companyId, string companyCode, long userId, long sessionId,
            PurchaseOrderChange change, CancellationToken cancellationToken) => Task.FromResult(1L);
        public Task SubmitOrderAsync(long companyId, string companyCode, long userId, long sessionId,
            long purchaseOrderId, byte[] rowVersion, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task ApproveOrderAsync(long companyId, string companyCode, long userId, long sessionId,
            long purchaseOrderId, byte[] rowVersion, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task CancelOrderAsync(long companyId, string companyCode, long userId, long sessionId,
            long purchaseOrderId, byte[] rowVersion, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task CloseOrderAsync(long companyId, string companyCode, long userId, long sessionId,
            long purchaseOrderId, string reason, byte[] rowVersion, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<long> CreateReceiptAsync(long companyId, string companyCode, long userId, long sessionId,
            GoodsReceiptChange change, CancellationToken cancellationToken)
        { Receipt = change; return Task.FromResult(1L); }
        public Task ReverseReceiptAsync(long companyId, string companyCode, long userId, long sessionId,
            long goodsReceiptId, string reason, byte[] rowVersion, CancellationToken cancellationToken)
        {
            Reversal = (companyId, userId, sessionId, goodsReceiptId, reason);
            return Task.CompletedTask;
        }
    }
}
