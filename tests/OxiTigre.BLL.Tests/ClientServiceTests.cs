/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Tests.ClientServiceTests
Archivo: ClientServiceTests.cs | Versión: 2.1.0 | Fecha: 2026-08-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Verifica autorización y aislamiento por empresa en clientes.
Historial: 1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 2.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Validación del alta, contexto y teléfono principal.
Historial: 2.1.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Documento, teléfono y borrador incompleto.
===============================================================================
*/
using OxiTigre.BLL.Commercial;
using OxiTigre.BLL.Security;

namespace OxiTigre.BLL.Tests;

/// <summary>Prueba el primer caso de uso del módulo Comercial.</summary>
public sealed class ClientServiceTests
{
    /// <summary>Comprueba que empresa, sesión y usuario se transfieren al almacenamiento.</summary>
    [Fact]
    public async Task ListActiveAsync_WithPermission_UsesSessionCompany()
    {
        var store = new FakeClientStore();
        var service = new ClientService(store);
        var identity = new SessionIdentity(8, 4, 2, "OXITIGRE", "AOCAUZI", "Agustin Cauzi",
            DateTimeOffset.UtcNow.AddHours(1), false, [], ["COMERCIAL.CONSULTAR"], Guid.NewGuid());

        var result = await service.ListActiveAsync(identity, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(2, store.CompanyId);
        Assert.Equal(8, store.SessionId);
    }

    /// <summary>Comprueba que la ausencia del permiso comercial bloquea la consulta.</summary>
    [Fact]
    public async Task ListActiveAsync_WithoutPermission_Throws()
    {
        var service = new ClientService(new FakeClientStore());
        var identity = new SessionIdentity(8, 4, 2, "OXITIGRE", "AOCAUZI", "Agustin Cauzi",
            DateTimeOffset.UtcNow.AddHours(1), false, [], [], Guid.NewGuid());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ListActiveAsync(identity, CancellationToken.None));
    }

    /// <summary>Comprueba que el alta exige exactamente un teléfono principal.</summary>
    [Fact]
    public async Task CreateAsync_WithoutPrimaryPhone_Throws()
    {
        var service = new ClientService(new FakeClientStore());
        var identity = new SessionIdentity(8, 4, 2, "OXITIGRE", "AOCAUZI", "Agustin Cauzi",
            DateTimeOffset.UtcNow.AddHours(1), false, [], ["COMERCIAL.GESTIONAR"], Guid.NewGuid());
        var draft = new ClientDraft("F", "Agustin", "Cauzi", "DNI", "123", null, null,
            [new ClientPhoneDraft("MOVIL", "54", "11", "55550000", null, false, true, null)]);

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(identity, draft, CancellationToken.None));
    }

    /// <summary>Comprueba que el alta normaliza datos y usa empresa, sesión y usuario autenticados.</summary>
    [Fact]
    public async Task CreateAsync_ValidDraft_UsesSessionContext()
    {
        var store = new FakeClientStore();
        var service = new ClientService(store);
        var identity = new SessionIdentity(8, 4, 2, "OXITIGRE", "AOCAUZI", "Agustin Cauzi",
            DateTimeOffset.UtcNow.AddHours(1), false, [], ["COMERCIAL.GESTIONAR"], Guid.NewGuid());
        var draft = new ClientDraft("f", " Agustin ", " Cauzi ", "dni", "30.111.222", null, null,
            [new ClientPhoneDraft("movil", "54", "11", "55550000", null, true, true, null)]);

        var clientId = await service.CreateAsync(identity, draft, CancellationToken.None);

        Assert.Equal(9, clientId);
        Assert.Equal(2, store.CompanyId);
        Assert.Equal(8, store.SessionId);
        Assert.Equal("F", store.Draft!.PersonType);
        Assert.Equal("MOVIL", store.Draft.Phones[0].TypeCode);
        Assert.Equal("30111222", store.Draft.DocumentNumber);
    }

    /// <summary>Comprueba que una carga incompleta puede guardarse como borrador.</summary>
    [Fact]
    public async Task SaveDraftAsync_IncompleteData_PersistsWithoutFinalValidation()
    {
        var store = new FakeClientStore();
        var service = new ClientService(store);
        var identity = new SessionIdentity(8, 4, 2, "OXITIGRE", "AOCAUZI", "Agustin Cauzi",
            DateTimeOffset.UtcNow.AddHours(1), false, [], ["COMERCIAL.GESTIONAR"], Guid.NewGuid());

        await service.SaveDraftAsync(identity, new ClientDraft("F", "Carga parcial", null, null, null, null, null, []), CancellationToken.None);

        Assert.Equal("Carga parcial", store.Draft!.NameOrBusinessName);
    }

    private sealed class FakeClientStore : IClientStore
    {
        public long CompanyId { get; private set; }
        public long SessionId { get; private set; }
        public ClientDraft? Draft { get; private set; }

        public Task<ClientCatalogs> GetCatalogsAsync(string companyCode, long sessionId, long userId, CancellationToken cancellationToken) =>
            Task.FromResult(new ClientCatalogs([], [], []));

        public Task<SavedClientDraft?> GetDraftAsync(long companyId, string companyCode, long sessionId, long userId, CancellationToken cancellationToken) =>
            Task.FromResult<SavedClientDraft?>(null);

        public Task SaveDraftAsync(long companyId, string companyCode, ClientDraft draft, long sessionId, long userId, CancellationToken cancellationToken)
        {
            Draft = draft;
            return Task.CompletedTask;
        }

        public Task DeleteDraftAsync(long companyId, string companyCode, long sessionId, long userId, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IReadOnlyList<ClientSummary>> ListAsync(long companyId, string companyCode, string statusCode, long sessionId, long userId, CancellationToken cancellationToken)
        {
            CompanyId = companyId;
            SessionId = sessionId;
            IReadOnlyList<ClientSummary> clients = [new ClientSummary(1, "CLI-0001", "F", "Cliente", "Demo", "0", null, "ACTIVO", "+54 11 55550001", 2)];
            return Task.FromResult(clients);
        }

        public Task<ClientDetails?> GetAsync(long companyId, string companyCode, long clientId, long sessionId, long userId, CancellationToken cancellationToken) =>
            Task.FromResult<ClientDetails?>(null);

        public Task<long> CreateAsync(long companyId, string companyCode, ClientDraft draft, long sessionId, long userId, CancellationToken cancellationToken)
        {
            CompanyId = companyId;
            SessionId = sessionId;
            Draft = draft;
            return Task.FromResult(9L);
        }

        public Task UpdateAsync(long companyId, string companyCode, long clientId, ClientDraft draft, long sessionId, long userId, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task ChangeStatusAsync(long companyId, string companyCode, long clientId, string statusCode, long sessionId, long userId, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
