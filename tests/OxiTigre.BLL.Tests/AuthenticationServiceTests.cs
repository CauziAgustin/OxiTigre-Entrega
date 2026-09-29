/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Tests.AuthenticationServiceTests
Archivo: AuthenticationServiceTests.cs | Versión: 2.1.0 | Fecha: 2026-08-27 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Verifica login, token opaco y rechazo de credenciales inválidas.
Historial: 1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Prueba de empresas autorizadas tras validar la clave.
Historial: 2.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Cobertura de intentos y bloqueo temporal.
Historial: 2.1.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Asociación opaca del token con la empresa elegida.
===============================================================================
*/
using OxiTigre.BLL.Security;

namespace OxiTigre.BLL.Tests;

/// <summary>Prueba la orquestación de autenticación sin depender de SQL Server.</summary>
public sealed class AuthenticationServiceTests
{
    /// <summary>Comprueba que un login correcto persiste solo el hash del token.</summary>
    [Fact]
    public async Task LoginAsync_ValidCredentials_CreatesOpaqueSession()
    {
        var hasher = new PasswordHasher();
        var credential = hasher.Hash("NuevaClave123");
        var account = new AuthenticationAccount(
            4,
            2,
            "AOCAUZI",
            "Agustin Omar",
            "Cauzi",
            "test@example.com",
            credential.Hash,
            credential.Salt,
            credential.Algorithm,
            credential.Iterations,
            false,
            0,
            null,
            ["ADMINISTRADOR"],
            ["SEGURIDAD.CONSULTAR"],
            [new(12, "SAN_FERNANDO", "San Fernando")]
        );
        var store = new FakeAuthenticationStore(account);
        var service = new AuthenticationService(store, hasher, TimeProvider.System);

        var result = await service.LoginAsync(
            new LoginCommand("OXITIGRE", "aocauzi", "NuevaClave123", "127.0.0.1", "Tests", 12),
            CancellationToken.None
        );

        Assert.NotNull(result);
        Assert.Equal(77, result.SessionId);
        Assert.Equal("AOCAUZI", result.Username);
        Assert.NotNull(store.StoredTokenHash);
        Assert.Equal(64, store.StoredTokenHash.Length);
        Assert.StartsWith("OXITIGRE.", result.Token, StringComparison.Ordinal);
        Assert.DoesNotContain(
            result.Token,
            Convert.ToHexString(store.StoredTokenHash),
            StringComparison.Ordinal
        );
        Assert.Equal(12, store.StoredBranchId);
        Assert.Equal("San Fernando", result.BranchName);
    }

    /// <summary>Comprueba que una contraseña incorrecta no crea una sesión.</summary>
    [Fact]
    public async Task LoginAsync_InvalidPassword_ReturnsNull()
    {
        var hasher = new PasswordHasher();
        var credential = hasher.Hash("NuevaClave123");
        var account = new AuthenticationAccount(
            4,
            2,
            "AOCAUZI",
            "Agustin Omar",
            "Cauzi",
            "test@example.com",
            credential.Hash,
            credential.Salt,
            credential.Algorithm,
            credential.Iterations,
            false,
            0,
            null,
            [],
            []
        );
        var store = new FakeAuthenticationStore(account);
        var service = new AuthenticationService(store, hasher, TimeProvider.System);

        var result = await service.LoginAsync(
            new LoginCommand("OXITIGRE", "AOCAUZI", "Incorrecta123", null, "Tests"),
            CancellationToken.None
        );

        Assert.Null(result);
        Assert.Null(store.StoredTokenHash);
        Assert.False(store.LastLoginSucceeded);
    }

    /// <summary>Comprueba que solo se expone la empresa cuya clave fue verificada.</summary>
    [Fact]
    public async Task GetCompaniesAsync_ValidCredentials_ReturnsAuthorizedCompany()
    {
        var hasher = new PasswordHasher();
        var credential = hasher.Hash("NuevaClave123");
        var account = new AuthenticationAccount(
            4,
            2,
            "AOCAUZI",
            "Agustin Omar",
            "Cauzi",
            "test@example.com",
            credential.Hash,
            credential.Salt,
            credential.Algorithm,
            credential.Iterations,
            false,
            0,
            null,
            [],
            []
        );
        var store = new FakeAuthenticationStore(account);
        var service = new AuthenticationService(store, hasher, TimeProvider.System);

        var result = await service.GetCompaniesAsync(
            "aocauzi",
            "NuevaClave123",
            CancellationToken.None
        );

        Assert.Collection(
            result,
            company =>
            {
                Assert.Equal("OXITIGRE", company.Code);
                Assert.Equal("OxiTigre", company.Name);
            }
        );
    }

    /// <summary>Comprueba que una cuenta bloqueada no crea sesión aunque la clave sea correcta.</summary>
    [Fact]
    public async Task LoginAsync_LockedAccount_ReturnsNull()
    {
        var hasher = new PasswordHasher();
        var credential = hasher.Hash("NuevaClave123");
        var account = new AuthenticationAccount(
            4,
            2,
            "AOCAUZI",
            "Agustin Omar",
            "Cauzi",
            "test@example.com",
            credential.Hash,
            credential.Salt,
            credential.Algorithm,
            credential.Iterations,
            false,
            5,
            DateTimeOffset.UtcNow,
            [],
            []
        );
        var store = new FakeAuthenticationStore(account);
        var service = new AuthenticationService(store, hasher, TimeProvider.System);

        var result = await service.LoginAsync(
            new LoginCommand("OXITIGRE", "AOCAUZI", "NuevaClave123", null, "Tests"),
            CancellationToken.None
        );

        Assert.Null(result);
        Assert.Null(store.StoredTokenHash);
    }

    private sealed class FakeAuthenticationStore(AuthenticationAccount account)
        : IAuthenticationStore
    {
        public byte[]? StoredTokenHash { get; private set; }
        public long? StoredBranchId { get; private set; }
        public bool? LastLoginSucceeded { get; private set; }

        public Task<AuthenticationAccount?> FindAccountAsync(
            string companyCode,
            string username,
            CancellationToken cancellationToken
        ) => Task.FromResult<AuthenticationAccount?>(account);

        public Task<long> CreateSessionAsync(
            string companyCode,
            long userId,
            long? branchId,
            byte[] tokenHash,
            DateTimeOffset expiresAtUtc,
            string? ipAddress,
            string application,
            Guid correlationId,
            CancellationToken cancellationToken
        )
        {
            StoredTokenHash = tokenHash;
            StoredBranchId = branchId;
            return Task.FromResult(77L);
        }

        public Task<SessionIdentity?> ValidateSessionAsync(
            string companyCode,
            byte[] tokenHash,
            CancellationToken cancellationToken
        ) => Task.FromResult<SessionIdentity?>(null);

        public Task<IReadOnlyList<CompanyLoginCandidate>> FindCompanyCandidatesAsync(
            string username,
            CancellationToken cancellationToken
        ) =>
            Task.FromResult<IReadOnlyList<CompanyLoginCandidate>>([
                new("OXITIGRE", "OxiTigre", account),
            ]);

        public Task ChangePasswordAsync(
            string companyCode,
            long userId,
            PasswordHash passwordHash,
            CancellationToken cancellationToken
        ) => Task.CompletedTask;

        public Task RecordLoginResultAsync(
            string companyCode,
            long userId,
            bool succeeded,
            CancellationToken cancellationToken
        )
        {
            LastLoginSucceeded = succeeded;
            return Task.CompletedTask;
        }

        public Task CloseSessionAsync(
            string companyCode,
            long sessionId,
            long userId,
            CancellationToken cancellationToken
        ) => Task.CompletedTask;
    }
}
