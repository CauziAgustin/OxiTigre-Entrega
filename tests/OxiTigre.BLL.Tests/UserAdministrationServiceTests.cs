/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Tests.UserAdministrationServiceTests
Archivo: UserAdministrationServiceTests.cs | Versión: 1.1.0 | Fecha: 2026-08-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Verifica roles múltiples y protección del administrador autenticado.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Comprueba la autorrevocación administrativa.
===============================================================================
*/
using OxiTigre.BLL.Security;

namespace OxiTigre.BLL.Tests;

/// <summary>Prueba reglas críticas de la administración de usuarios sin SQL Server.</summary>
public sealed class UserAdministrationServiceTests
{
    private static readonly SessionIdentity Administrator = new(10, 1, 1, "OXITIGRE", "AOCAUZI",
        "Agustin Omar Cauzi", DateTimeOffset.UtcNow.AddHours(8), false, ["ADMINISTRADOR"], [], Guid.NewGuid());

    /// <summary>Impide que el administrador quite su propio acceso.</summary>
    [Fact]
    public async Task UpdateAsync_SelfWithoutAdministratorRole_RejectsChange()
    {
        var service = new UserAdministrationService(new FakeUserAdministrationStore(), new PasswordHasher());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync(Administrator, 1,
            "Agustin Omar", "Cauzi", "agustin@example.com", "ACTIVO", ["CONSULTA"], CancellationToken.None));
    }

    /// <summary>Conserva todos los roles seleccionados durante el alta.</summary>
    [Fact]
    public async Task CreateAsync_MultipleRoles_PassesNormalizedRoles()
    {
        var store = new FakeUserAdministrationStore();
        var service = new UserAdministrationService(store, new PasswordHasher());

        await service.CreateAsync(Administrator, "Juan Pablo", "Perez", "juan@example.com",
            ["administrador", "consulta"], "ClaveTemporal123", CancellationToken.None);

        Assert.Equal(["ADMINISTRADOR", "CONSULTA"], store.CreatedRoles);
    }

    /// <summary>Permite que el administrador revoque también su sesión vigente.</summary>
    [Fact]
    public async Task RevokeSessionsAsync_Self_ForwardsCurrentUser()
    {
        var store = new FakeUserAdministrationStore();
        var service = new UserAdministrationService(store, new PasswordHasher());

        await service.RevokeSessionsAsync(Administrator, Administrator.UserId, CancellationToken.None);

        Assert.Equal(Administrator.UserId, store.RevokedUserId);
    }

    private sealed class FakeUserAdministrationStore : IUserAdministrationStore
    {
        public IReadOnlyList<string> CreatedRoles { get; private set; } = [];
        public long? RevokedUserId { get; private set; }

        public Task<IReadOnlyList<UserSummary>> ListAsync(long companyId, string companyCode, long sessionId,
            long userId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<UserSummary>>([]);

        public Task<IReadOnlyList<RoleSummary>> ListRolesAsync(long companyId, string companyCode, long sessionId,
            long userId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<RoleSummary>>([]);

        public Task<long> CreateAsync(long companyId, string companyCode, string username, string givenNames,
            string surname, string email, IReadOnlyList<string> roleCodes, PasswordHash passwordHash,
            long administratorId, long sessionId, CancellationToken cancellationToken)
        {
            CreatedRoles = roleCodes;
            return Task.FromResult(2L);
        }

        public Task UpdateAsync(long companyId, string companyCode, long targetUserId, string givenNames,
            string surname, string email, string statusCode, IReadOnlyList<string> roleCodes, long administratorId,
            long sessionId, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task ResetPasswordAsync(long companyId, string companyCode, long targetUserId,
            PasswordHash passwordHash, long administratorId, long sessionId,
            CancellationToken cancellationToken) => Task.CompletedTask;

        public Task RevokeSessionsAsync(long companyId, string companyCode, long targetUserId,
            long administratorId, long sessionId, CancellationToken cancellationToken)
        {
            RevokedUserId = targetUserId;
            return Task.CompletedTask;
        }
    }
}
