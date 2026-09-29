/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Tests.ConfigurationServiceTests
Archivo: ConfigurationServiceTests.cs | Versión: 1.1.0 | Fecha: 2026-09-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Verifica autorización, aislamiento e idioma en Configuración.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-09-20 | FABRICA | Agustin Omar Cauzi | Validación de culturas importables.
===============================================================================
*/
using OxiTigre.BLL.Configuration;
using OxiTigre.BLL.Security;

namespace OxiTigre.BLL.Tests;

/// <summary>Prueba las reglas mínimas del servicio de Configuración.</summary>
public sealed class ConfigurationServiceTests
{
    /// <summary>Comprueba que la consulta usa empresa, usuario y sesión autenticados.</summary>
    [Fact]
    public async Task GetAsync_WithPermission_UsesSessionContext()
    {
        var store = new FakeConfigurationStore();
        var service = new ConfigurationService(store);

        await service.GetAsync(Identity("CONFIGURACION.CONSULTAR"), CancellationToken.None);

        Assert.Equal(2, store.CompanyId);
        Assert.Equal(4, store.UserId);
        Assert.Equal(8, store.SessionId);
    }

    /// <summary>Comprueba que una mutación sin permiso de gestión se rechaza.</summary>
    [Fact]
    public async Task SaveBranchAsync_WithoutManagePermission_Throws()
    {
        var service = new ConfigurationService(new FakeConfigurationStore());
        var change = new BranchChange(
            null,
            "CENTRO",
            "Centro",
            null,
            null,
            null,
            null,
            "ACTIVO",
            null
        );

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.SaveBranchAsync(
                Identity("CONFIGURACION.CONSULTAR"),
                change,
                CancellationToken.None
            )
        );
    }

    /// <summary>Comprueba que una cultura importable válida puede guardarse.</summary>
    [Fact]
    public async Task SaveCultureAsync_ValidSpecificCulture_SavesCanonicalCode()
    {
        var store = new FakeConfigurationStore();
        var service = new ConfigurationService(store);

        await service.SaveCultureAsync(Identity(), "pt-br", CancellationToken.None);

        Assert.Equal("pt-BR", store.CultureCode);
    }

    /// <summary>Comprueba que un código inexistente no llega al almacenamiento.</summary>
    [Fact]
    public async Task SaveCultureAsync_InvalidCulture_Throws()
    {
        var service = new ConfigurationService(new FakeConfigurationStore());
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SaveCultureAsync(Identity(), "idioma-inexistente", CancellationToken.None)
        );
    }

    private static SessionIdentity Identity(params string[] permissions) =>
        new(
            8,
            4,
            2,
            "OXITIGRE",
            "AOCAUZI",
            "Agustin Cauzi",
            DateTimeOffset.UtcNow.AddHours(1),
            false,
            [],
            permissions,
            Guid.NewGuid()
        );

    private sealed class FakeConfigurationStore : IConfigurationStore
    {
        public long CompanyId { get; private set; }
        public long UserId { get; private set; }
        public long SessionId { get; private set; }
        public string? CultureCode { get; private set; }

        public Task<ConfigurationSnapshot> GetAsync(
            long companyId,
            string companyCode,
            long userId,
            long sessionId,
            CancellationToken cancellationToken
        )
        {
            CompanyId = companyId;
            UserId = userId;
            SessionId = sessionId;
            return Task.FromResult(
                new ConfigurationSnapshot(
                    new CompanyConfiguration(
                        companyId,
                        companyCode,
                        "OxiTigre",
                        null,
                        null,
                        null,
                        "ACTIVO",
                        new byte[8]
                    ),
                    [],
                    [],
                    [],
                    [],
                    [],
                    [],
                    [],
                    [],
                    "es-AR"
                )
            );
        }

        public Task<string> GetCultureAsync(
            string companyCode,
            long userId,
            long sessionId,
            CancellationToken cancellationToken
        ) => Task.FromResult("es-AR");

        public Task SaveCultureAsync(
            string companyCode,
            long userId,
            long sessionId,
            string cultureCode,
            CancellationToken cancellationToken
        )
        {
            CultureCode = cultureCode;
            return Task.CompletedTask;
        }

        public Task UpdateCompanyAsync(
            long companyId,
            string companyCode,
            long userId,
            long sessionId,
            CompanyChange change,
            CancellationToken cancellationToken
        ) => Task.CompletedTask;

        public Task<long> SaveBranchAsync(
            long companyId,
            string companyCode,
            long userId,
            long sessionId,
            BranchChange change,
            CancellationToken cancellationToken
        ) => Task.FromResult(1L);

        public Task<long> SaveOperatingUnitAsync(
            long companyId,
            string companyCode,
            long userId,
            long sessionId,
            OperatingUnitChange change,
            CancellationToken cancellationToken
        ) => Task.FromResult(1L);

        public Task<long> SavePhoneTypeAsync(
            string companyCode,
            long userId,
            long sessionId,
            PhoneTypeChange change,
            CancellationToken cancellationToken
        ) => Task.FromResult(1L);

        public Task UpdateStateAsync(
            string companyCode,
            long userId,
            long sessionId,
            StateChange change,
            CancellationToken cancellationToken
        ) => Task.CompletedTask;

        public Task<long> SaveParameterAsync(
            string companyCode,
            long userId,
            long sessionId,
            SystemParameterChange change,
            CancellationToken cancellationToken
        ) => Task.FromResult(1L);

        public Task<long> SaveModuleAsync(
            string companyCode,
            long userId,
            long sessionId,
            ModuleChange change,
            CancellationToken cancellationToken
        ) => Task.FromResult(1L);

        public Task<long> CreateErrorAsync(
            string companyCode,
            long userId,
            long sessionId,
            CreateErrorCatalogChange change,
            CancellationToken cancellationToken
        ) => Task.FromResult(20005L);

        public Task UpdateErrorAsync(
            string companyCode,
            long userId,
            long sessionId,
            UpdateErrorCatalogChange change,
            CancellationToken cancellationToken
        ) => Task.CompletedTask;

        public Task<long> SaveTranslationAsync(
            string companyCode,
            long userId,
            long sessionId,
            CatalogTranslationChange change,
            CancellationToken cancellationToken
        ) => Task.FromResult(1L);
    }
}
