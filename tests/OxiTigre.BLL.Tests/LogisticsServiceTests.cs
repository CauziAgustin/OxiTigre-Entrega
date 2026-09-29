/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Tests.LogisticsServiceTests
Archivo: LogisticsServiceTests.cs | Versión: 1.4.0 | Fecha: 2026-08-29 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Verifica permisos, urgencias y custodia crítica del circuito logístico.
Historial: 1.0.0 | 2026-08-26 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Asignación tipada de transporte y propiedad de vehículos.
Historial: 1.2.0 | 2026-08-28 | FABRICA | Agustin Omar Cauzi | Confirmación parcial con activos seleccionados.
Historial: 1.3.0 | 2026-08-28 | FABRICA | Agustin Omar Cauzi | Aislamiento de rutas y paradas del transportista autenticado.
Historial: 1.4.0 | 2026-08-29 | FABRICA | Agustin Omar Cauzi | Llegada previa e incidencias móviles validadas.
===============================================================================
*/
using OxiTigre.BLL.Logistics;
using OxiTigre.BLL.Security;

namespace OxiTigre.BLL.Tests;

/// <summary>Prueba las reglas que deben fallar antes de acceder a SQL Server.</summary>
public sealed class LogisticsServiceTests
{
    /// <summary>Comprueba que una ruta urgente no agrupe varias solicitudes.</summary>
    /// <returns>Tarea que finaliza cuando la validación fue comprobada.</returns>
    [Fact]
    public async Task CreateRouteAsync_UrgentWithMultipleRequests_Throws()
    {
        var service = new LogisticsService(new FakeLogisticsStore());
        var change = new RouteChange(
            DateOnly.FromDateTime(DateTime.Today),
            "URGENTE",
            "DIRECTA",
            1,
            2,
            "Entrega inmediata",
            [new(1, 10), new(2, 11)]
        );

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateRouteAsync(
                Identity("LOGISTICA.GESTIONAR"),
                change,
                CancellationToken.None
            )
        );
    }

    /// <summary>Comprueba que una oferta se cree sin inventar chofer ni vehículo.</summary>
    /// <returns>Tarea que finaliza cuando se capturó el comando.</returns>
    [Fact]
    public async Task CreateRouteAsync_Offer_SendsUnassignedMode()
    {
        var store = new FakeLogisticsStore();
        var change = new RouteChange(
            DateOnly.FromDateTime(DateTime.Today),
            "NORMAL",
            "OFERTA",
            null,
            null,
            "Disponible para autoselección",
            [new(1, 10)]
        );

        await new LogisticsService(store).CreateRouteAsync(
            Identity("LOGISTICA.GESTIONAR"),
            change,
            CancellationToken.None
        );

        Assert.Equal("RUTA_CREAR", store.Action);
        Assert.Equal("OFERTA", store.Values?["@I_TIPO_ASIGNACION"]);
        Assert.Null(store.Values?["@I_ID_TRANSPORTISTA"]);
    }

    /// <summary>Comprueba que un intercambio identifique al menos un activo bajo custodia.</summary>
    /// <returns>Tarea que finaliza cuando la validación fue comprobada.</returns>
    [Fact]
    public async Task CreateRequestAsync_ExchangeWithoutAsset_Throws()
    {
        var service = new LogisticsService(new FakeLogisticsStore());
        var change = new LogisticsRequestChange(
            1,
            2,
            null,
            "INTERCAMBIO_TEMPORAL",
            "ALTA",
            DateOnly.FromDateTime(DateTime.Today),
            null,
            null,
            "Retirar y prestar tubo",
            null,
            []
        );

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateRequestAsync(
                Identity("LOGISTICA.GESTIONAR"),
                change,
                CancellationToken.None
            )
        );
    }

    /// <summary>Comprueba que la consulta utilice únicamente el contexto de la sesión.</summary>
    /// <returns>Tarea que finaliza cuando se capturó el contexto.</returns>
    [Fact]
    public async Task GetAsync_WithPermission_UsesSessionContext()
    {
        var store = new FakeLogisticsStore();

        await new LogisticsService(store).GetAsync(
            Identity("LOGISTICA.CONSULTAR"),
            CancellationToken.None
        );

        Assert.Equal((2, 4, 8), (store.CompanyId, store.UserId, store.SessionId));
    }

    /// <summary>Comprueba que una ruta no se pueda cancelar sin conservar el motivo.</summary>
    /// <returns>Tarea que finaliza cuando la validación fue comprobada.</returns>
    [Fact]
    public async Task ChangeRouteAsync_CancelWithoutReason_Throws()
    {
        var service = new LogisticsService(new FakeLogisticsStore());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ChangeRouteAsync(
                Identity("LOGISTICA.GESTIONAR"),
                1,
                new byte[8],
                true,
                null,
                CancellationToken.None
            )
        );
    }

    /// <summary>Comprueba que un vehículo particular identifique a su transportista propietario.</summary>
    [Fact]
    public async Task SaveVehicleAsync_IndependentWithoutOwner_Throws()
    {
        var service = new LogisticsService(new FakeLogisticsStore());
        var change = new LogisticsVehicleChange(
            VehicleId: null,
            OwnerTransporterId: null,
            Plate: "AB-123-CD",
            VehicleType: "CAMIONETA",
            OwnershipType: "TRANSPORTISTA",
            Brand: null,
            Model: null,
            Year: null,
            LoadCapacityKg: null,
            InsurancePolicy: null,
            InsuranceExpiration: null,
            InspectionExpiration: null,
            Observation: null,
            StatusCode: "ACTIVO",
            RowVersion: null
        );

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SaveVehicleAsync(
                Identity("LOGISTICA.GESTIONAR"),
                change,
                CancellationToken.None
            )
        );
    }

    /// <summary>Comprueba que una parada parcial envíe únicamente los activos seleccionados.</summary>
    /// <returns>Tarea que finaliza cuando se capturó el comando.</returns>
    [Fact]
    public async Task CompleteStopAsync_Partial_SendsSelectedAssets()
    {
        var store = new FakeLogisticsStore();

        await new LogisticsService(store).CompleteStopAsync(
            Identity("LOGISTICA.DESPACHAR"),
            9,
            "PARCIAL",
            "Se retiró un tubo.",
            new byte[8],
            [21, 22],
            CancellationToken.None
        );

        Assert.Equal("PARADA_CONFIRMAR", store.Action);
        Assert.Equal("[21,22]", store.Values?["@I_JSON"]);
    }

    /// <summary>Comprueba que la vista móvil excluya rutas de otros transportistas.</summary>
    /// <returns>Tarea que finaliza cuando se filtró el recorrido.</returns>
    [Fact]
    public async Task GetDriverAsync_ReturnsOnlyAssignedRoute()
    {
        var store = DriverStore();

        var result = await new LogisticsService(store).GetDriverAsync(
            DriverIdentity(),
            CancellationToken.None
        );

        Assert.Equal(3, result.Driver.TransporterId);
        Assert.Equal(10, Assert.Single(result.Routes).RouteId);
        Assert.Equal(20, Assert.Single(result.Stops).StopId);
        Assert.Equal(40, Assert.Single(result.Assets).RequestAssetId);
    }

    /// <summary>Comprueba que el transportista no confirme una parada ajena.</summary>
    /// <returns>Tarea que finaliza cuando se rechazó la operación.</returns>
    [Fact]
    public async Task CompleteStopAsync_UnassignedDriverStop_Throws()
    {
        var store = DriverStore();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            new LogisticsService(store).CompleteStopAsync(
                DriverIdentity("LOGISTICA.DESPACHAR"),
                99,
                "ENTREGADA",
                "Entrega completa.",
                new byte[8],
                [],
                CancellationToken.None
            )
        );

        Assert.Null(store.Action);
    }

    /// <summary>Comprueba que el transportista pueda registrar la llegada solamente en su parada.</summary>
    /// <returns>Tarea que finaliza cuando se capturó la acción consolidada.</returns>
    [Fact]
    public async Task MarkStopArrivalAsync_AssignedDriver_SendsAction()
    {
        var store = DriverStore();

        await new LogisticsService(store).MarkStopArrivalAsync(
            DriverIdentity("LOGISTICA.DESPACHAR"),
            20,
            new byte[8],
            CancellationToken.None
        );

        Assert.Equal("PARADA_LLEGADA", store.Action);
        Assert.Equal(20L, store.Values?["@I_ID"]);
    }

    /// <summary>Comprueba que el transportista no cierre una visita sin registrar su llegada.</summary>
    /// <returns>Tarea que finaliza cuando se rechazó la secuencia incompleta.</returns>
    [Fact]
    public async Task CompleteStopAsync_AssignedDriverWithoutArrival_Throws()
    {
        var store = DriverStore();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            new LogisticsService(store).CompleteStopAsync(
                DriverIdentity("LOGISTICA.DESPACHAR"),
                20,
                "ENTREGADA",
                "Entrega completa.",
                new byte[8],
                [],
                CancellationToken.None
            )
        );

        Assert.Contains("llegada", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(store.Action);
    }

    /// <summary>Comprueba que una incidencia válida conserve categoría y detalle.</summary>
    /// <returns>Tarea que finaliza cuando se capturó el comando.</returns>
    [Fact]
    public async Task ReportStopIncidentAsync_AssignedDriver_SendsCategory()
    {
        var store = DriverStore();

        await new LogisticsService(store).ReportStopIncidentAsync(
            DriverIdentity("LOGISTICA.DESPACHAR"),
            20,
            "demora",
            "Tránsito detenido.",
            new byte[8],
            CancellationToken.None
        );

        Assert.Equal("PARADA_INCIDENCIA", store.Action);
        Assert.Equal("DEMORA", store.Values?["@I_RESULTADO"]);
    }

    /// <summary>Comprueba que un transportista tome una oferta con un vehículo compatible.</summary>
    /// <returns>Tarea que finaliza cuando se capturó la acción atómica.</returns>
    [Fact]
    public async Task ClaimRouteOfferAsync_AvailableOffer_SendsAtomicAction()
    {
        var store = DriverStore();

        await new LogisticsService(store).ClaimRouteOfferAsync(
            DriverIdentity("LOGISTICA.DESPACHAR"),
            12,
            5,
            new byte[8],
            CancellationToken.None
        );

        Assert.Equal("RUTA_TOMAR_OFERTA", store.Action);
        Assert.Equal(5L, store.Values?["@I_ID_VEHICULO"]);
    }

    /// <summary>Comprueba que el transportista pueda pausar solamente su recorrido activo.</summary>
    /// <returns>Tarea que finaliza cuando se capturó la transición.</returns>
    [Fact]
    public async Task ChangeRoutePauseAsync_AssignedDriver_SendsPause()
    {
        var store = DriverStore();

        await new LogisticsService(store).ChangeRoutePauseAsync(
            DriverIdentity("LOGISTICA.DESPACHAR"),
            10,
            new byte[8],
            true,
            "Pausa por control preventivo.",
            CancellationToken.None
        );

        Assert.Equal("RUTA_PAUSAR", store.Action);
        Assert.Equal("Pausa por control preventivo.", store.Values?["@I_OBSERVACION"]);
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

    private static SessionIdentity DriverIdentity(params string[] additionalPermissions) =>
        new(
            8,
            4,
            2,
            "OXITIGRE",
            "CHOFER",
            "Chofer de prueba",
            DateTimeOffset.UtcNow.AddHours(1),
            false,
            ["TRANSPORTISTA"],
            ["LOGISTICA.CONSULTAR", .. additionalPermissions],
            Guid.NewGuid()
        );

    private static FakeLogisticsStore DriverStore()
    {
        var driver = new Transporter(
            3,
            4,
            "TRA-000003",
            "CHOFER",
            "Chofer de prueba",
            "EMPRESA",
            null,
            "11-5555-0101",
            "LIC-003",
            "CARGAS",
            null,
            null,
            "ACTIVO",
            new byte[8]
        );
        var ownRoute = new RouteSheet(
            10,
            3,
            5,
            "RUT-000010",
            DateOnly.FromDateTime(DateTime.Today),
            "NORMAL",
            "DIRECTA",
            driver.FullName,
            "AB123CD",
            "Ruta propia",
            DateTime.UtcNow,
            DateTime.UtcNow,
            null,
            "DESPACHADA",
            new byte[8]
        );
        var otherRoute = new RouteSheet(
            11,
            7,
            6,
            "RUT-000011",
            DateOnly.FromDateTime(DateTime.Today),
            "NORMAL",
            "DIRECTA",
            "Otro chofer",
            "AC123CD",
            "Ruta ajena",
            DateTime.UtcNow,
            DateTime.UtcNow,
            null,
            "DESPACHADA",
            new byte[8]
        );
        var offer = new RouteSheet(
            12,
            null,
            null,
            "RUT-000012",
            DateOnly.FromDateTime(DateTime.Today),
            "NORMAL",
            "OFERTA",
            null,
            null,
            "Oferta libre",
            null,
            null,
            null,
            "OFRECIDA",
            new byte[8]
        );
        var history = new RouteSheet(
            13,
            3,
            5,
            "RUT-000013",
            DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
            "NORMAL",
            "OFERTA",
            driver.FullName,
            "AB123CD",
            "Ruta completada",
            DateTime.UtcNow,
            DateTime.UtcNow,
            DateTime.UtcNow,
            "COMPLETADA",
            new byte[8]
        );
        var ownStop = new RouteStop(
            20,
            10,
            30,
            1,
            "Cliente propio",
            "Domicilio 1",
            "Contacto",
            "11-5555-0102",
            null,
            null,
            "NORMAL",
            "Entregar",
            null,
            null,
            null,
            null,
            "PLANIFICADA",
            new byte[8]
        );
        var otherStop = new RouteStop(
            21,
            11,
            31,
            1,
            "Cliente ajeno",
            "Domicilio 2",
            "Contacto",
            "11-5555-0103",
            null,
            null,
            "NORMAL",
            "Entregar",
            null,
            null,
            null,
            null,
            "PLANIFICADA",
            new byte[8]
        );
        var ownAsset = new LogisticsAsset(
            40,
            30,
            50,
            "OXITIGRE",
            "ENTREGA",
            "SERIE-1",
            "Tubo",
            10,
            "kg",
            "OPERATIVO",
            null,
            "PENDIENTE",
            null,
            null
        );
        var otherAsset = new LogisticsAsset(
            41,
            31,
            51,
            "OXITIGRE",
            "ENTREGA",
            "SERIE-2",
            "Tubo",
            10,
            "kg",
            "OPERATIVO",
            null,
            "PENDIENTE",
            null,
            null
        );
        var companyVehicle = new LogisticsVehicle(
            5,
            null,
            "VEH-000005",
            "AB123CD",
            "CAMIONETA",
            "EMPRESA",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            "ACTIVO",
            new byte[8]
        );

        return new FakeLogisticsStore
        {
            Snapshot = new(
                [],
                [],
                [ownAsset, otherAsset],
                [ownRoute, otherRoute, offer, history],
                [ownStop, otherStop],
                [],
                [],
                [],
                [driver],
                [companyVehicle],
                [],
                [],
                []
            ),
        };
    }

    private sealed class FakeLogisticsStore : ILogisticsStore
    {
        public LogisticsSnapshot Snapshot { get; init; } =
            new([], [], [], [], [], [], [], [], [], [], [], [], []);

        public long CompanyId { get; private set; }

        public long UserId { get; private set; }

        public long SessionId { get; private set; }

        public string? Action { get; private set; }

        public IReadOnlyDictionary<string, object?>? Values { get; private set; }

        public Task<LogisticsSnapshot> GetAsync(
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

            return Task.FromResult(Snapshot);
        }

        public Task<long> ExecuteAsync(
            long companyId,
            string companyCode,
            long userId,
            long sessionId,
            string action,
            IReadOnlyDictionary<string, object?> values,
            CancellationToken cancellationToken
        )
        {
            Action = action;
            Values = values;

            return Task.FromResult(1L);
        }
    }
}
