/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Tests.TraceabilityServiceTests
Archivo: TraceabilityServiceTests.cs | Versión: 1.1.0 | Fecha: 2026-08-28 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Comprueba permisos, conservación y destinos de trazabilidad.
Historial: 1.0.0 | 2026-08-24 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-28 | FABRICA | Agustin Omar Cauzi | Validación del ciclo de custodia de activos de clientes.
===============================================================================
*/
using OxiTigre.BLL.Inventory;
using OxiTigre.BLL.Security;

namespace OxiTigre.BLL.Tests;

/// <summary>Comprueba reglas críticas de trazabilidad antes de acceder a SQL.</summary>
public sealed class TraceabilityServiceTests
{
    /// <summary>Impide fraccionamientos que no conservan la cantidad de origen.</summary>
    [Fact]
    public async Task CreateTransformationAsync_WithUnbalancedQuantities_Throws()
    {
        var change = new TransformationChange(1, null, 2, DateTime.UtcNow, 10, 1, "PESAJE", null, null, [new(3, 1, 8)]);
        await Assert.ThrowsAsync<ArgumentException>(() => new TraceabilityService(new FakeStore()).CreateTransformationAsync(Identity("INVENTARIO.GESTIONAR"), change, CancellationToken.None));
    }

    /// <summary>Impide indicar simultáneamente más de un tipo de destinatario.</summary>
    [Fact]
    public async Task CreateLoanAsync_WithAmbiguousDestination_Throws()
    {
        var change = new LoanChange("SUCURSAL", 1, 2, null, "ENTREGA_PROPIA", DateTime.UtcNow, null, null, null, null, [new(3, null, 0, "VACIO")]);
        await Assert.ThrowsAsync<ArgumentException>(() => new TraceabilityService(new FakeStore()).CreateLoanAsync(Identity("INVENTARIO.GESTIONAR"), change, CancellationToken.None));
    }

    /// <summary>Impide ingresar un activo del cliente sin indicar el depósito que lo recibe.</summary>
    [Fact]
    public async Task SaveClientAssetAsync_InCustodyWithoutWarehouse_Throws()
    {
        var change = new ClientAssetChange(1, 2, null, "CIL-1", "CILINDRO", 10, "kg",
            "OPERATIVO", true, "Recibido para recarga.");

        await Assert.ThrowsAsync<ArgumentException>(() => new TraceabilityService(new FakeStore())
            .SaveClientAssetAsync(Identity("INVENTARIO.GESTIONAR"), change, CancellationToken.None));
    }

    /// <summary>Usa empresa, usuario y sesión de la identidad autenticada.</summary>
    [Fact]
    public async Task GetAsync_UsesAuthenticatedContext()
    {
        var store = new FakeStore(); await new TraceabilityService(store).GetAsync(Identity("INVENTARIO.CONSULTAR"), CancellationToken.None);
        Assert.Equal((2L, 4L, 8L), (store.CompanyId, store.UserId, store.SessionId));
    }

    private static SessionIdentity Identity(params string[] permissions) => new(8, 4, 2, "OXITIGRE", "AOCAUZI", "Agustin Cauzi", DateTimeOffset.UtcNow.AddHours(1), false, [], permissions, Guid.NewGuid());

    private sealed class FakeStore : ITraceabilityStore
    {
        public long CompanyId { get; private set; }
        public long UserId { get; private set; }
        public long SessionId { get; private set; }
        public Task<TraceabilitySnapshot> GetAsync(long companyId, string companyCode, long userId, long sessionId, CancellationToken cancellationToken) { CompanyId = companyId; UserId = userId; SessionId = sessionId; return Task.FromResult(new TraceabilitySnapshot([], [], [], [], [], [], [], [], [], [])); }
        public Task<long> SaveClientAssetAsync(long companyId, string companyCode, long userId, long sessionId, ClientAssetChange change, CancellationToken cancellationToken) => Task.FromResult(1L);
        public Task<long> ChangeClientAssetCustodyAsync(long companyId, string companyCode, long userId, long sessionId, ClientAssetCustodyChange change, CancellationToken cancellationToken) => Task.FromResult(1L);
        public Task<long> CreateMeasurementAsync(long companyId, string companyCode, long userId, long sessionId, AssetMeasurementChange change, CancellationToken cancellationToken) => Task.FromResult(1L);
        public Task<long> CreateTransformationAsync(long companyId, string companyCode, long userId, long sessionId, TransformationChange change, CancellationToken cancellationToken) => Task.FromResult(1L);
        public Task<long> CreateIncidentAsync(long companyId, string companyCode, long userId, long sessionId, IncidentChange change, CancellationToken cancellationToken) => Task.FromResult(1L);
        public Task<long> CreateMaintenanceAsync(long companyId, string companyCode, long userId, long sessionId, MaintenanceChange change, CancellationToken cancellationToken) => Task.FromResult(1L);
        public Task CompleteMaintenanceAsync(long companyId, string companyCode, long userId, long sessionId, MaintenanceCompletion change, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<long> CreateLoanAsync(long companyId, string companyCode, long userId, long sessionId, LoanChange change, CancellationToken cancellationToken) => Task.FromResult(1L);
        public Task ReturnLoanAsync(long companyId, string companyCode, long userId, long sessionId, LoanReturnChange change, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
