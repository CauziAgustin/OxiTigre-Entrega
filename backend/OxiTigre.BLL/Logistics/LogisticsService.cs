/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Logistics.LogisticsService
Archivo: LogisticsService.cs | Versión: 1.4.0 | Fecha: 2026-08-29 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Autoriza y valida agenda, hojas, despacho y confirmaciones logísticas.
Historial: 1.0.0 | 2026-08-26 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Gestión de transportistas, vehículos y asignación tipada a rutas.
Historial: 1.2.0 | 2026-08-28 | FABRICA | Agustin Omar Cauzi | Pedidos planificables y custodia automática por activo confirmado.
Historial: 1.3.0 | 2026-08-28 | FABRICA | Agustin Omar Cauzi | Acceso móvil restringido a rutas propias del transportista.
Historial: 1.4.0 | 2026-08-29 | FABRICA | Agustin Omar Cauzi | Llegada e incidencias trazables en paradas asignadas.
===============================================================================
*/
using System.Net.Mail;
using System.Text.Json;
using OxiTigre.BLL.Security;
using OxiTigre.Domain.Logistics;

namespace OxiTigre.BLL.Logistics;

/// <summary>Orquesta Logística sin confiar en datos de empresa o permisos enviados por el cliente.</summary>
/// <param name="store">Persistencia SQL consolidada.</param>
public sealed class LogisticsService(ILogisticsStore store)
{
    /// <summary>Obtiene agenda, rutas históricas, eventos y avisos.</summary>
    /// <param name="identity">Sesión autenticada.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Snapshot aislado por empresa.</returns>
    /// <exception cref="ArgumentNullException">La identidad es nula.</exception>
    /// <exception cref="UnauthorizedAccessException">Falta LOGISTICA.CONSULTAR.</exception>
    /// <exception cref="LogisticsOperationException">SQL Server devolvió un error funcional controlado.</exception>
    public Task<LogisticsSnapshot> GetAsync(
        SessionIdentity identity,
        CancellationToken cancellationToken
    )
    {
        Ensure(identity, "LOGISTICA.CONSULTAR");

        if (IsRestrictedDriver(identity))
        {
            throw new UnauthorizedAccessException(
                "El transportista debe usar su vista de recorridos asignados."
            );
        }

        return store.GetAsync(
            identity.CompanyId,
            identity.CompanyCode,
            identity.UserId,
            identity.SessionId,
            cancellationToken
        );
    }

    /// <summary>Obtiene trabajo activo, ofertas, historial y vehículos del transportista autenticado.</summary>
    /// <param name="identity">Sesión del transportista.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Perfil, rutas propias, ofertas disponibles, historial, paradas, activos y vehículos habilitados.</returns>
    /// <exception cref="ArgumentNullException">La identidad es nula.</exception>
    /// <exception cref="UnauthorizedAccessException">La sesión no corresponde a un transportista activo.</exception>
    /// <exception cref="LogisticsOperationException">SQL Server devolvió un error funcional controlado.</exception>
    public async Task<DriverSnapshot> GetDriverAsync(
        SessionIdentity identity,
        CancellationToken cancellationToken
    )
    {
        Ensure(identity, "LOGISTICA.CONSULTAR");

        if (!identity.Roles.Contains("TRANSPORTISTA", StringComparer.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("La sesión no posee el rol Transportista.");
        }

        var snapshot = await LoadDriverSourceAsync(identity, cancellationToken);
        var driver = Driver(snapshot, identity);
        var routes = snapshot
            .Routes.Where(route =>
                route.TransporterId == driver.TransporterId
                && route.StatusCode is "PLANIFICADA" or "DESPACHADA" or "PAUSADA"
            )
            .ToList();
        var offers = snapshot
            .Routes.Where(route => route.TransporterId is null && route.StatusCode == "OFRECIDA")
            .ToList();
        var history = snapshot
            .Routes.Where(route =>
                route.TransporterId == driver.TransporterId
                && route.StatusCode is "COMPLETADA" or "CANCELADA"
            )
            .OrderByDescending(route => route.RouteDate)
            .ThenByDescending(route => route.RouteId)
            .Take(100)
            .ToList();
        var routeIds = routes
            .Concat(offers)
            .Concat(history)
            .Select(route => route.RouteId)
            .ToHashSet();
        var stops = snapshot.Stops.Where(stop => routeIds.Contains(stop.RouteId)).ToList();
        var requestIds = stops.Select(stop => stop.RequestId).ToHashSet();
        var assets = snapshot.Assets.Where(asset => requestIds.Contains(asset.RequestId)).ToList();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var vehicles = snapshot
            .Vehicles.Where(vehicle =>
                vehicle.StatusCode == "ACTIVO"
                && (
                    vehicle.OwnerTransporterId is null
                    || vehicle.OwnerTransporterId == driver.TransporterId
                )
                && (vehicle.InsuranceExpiration is null || vehicle.InsuranceExpiration >= today)
                && (vehicle.InspectionExpiration is null || vehicle.InspectionExpiration >= today)
            )
            .ToList();

        return new(driver, routes, offers, history, stops, assets, vehicles);
    }

    /// <summary>Registra un domicilio con instrucciones obligatorias.</summary>
    /// <param name="identity">Sesión autenticada.</param>
    /// <param name="change">Domicilio y horario.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Identificador creado.</returns>
    /// <exception cref="ArgumentNullException">La identidad o el cambio son nulos.</exception>
    /// <exception cref="ArgumentException">Los datos u horario son inválidos.</exception>
    /// <exception cref="UnauthorizedAccessException">Falta LOGISTICA.GESTIONAR.</exception>
    /// <exception cref="LogisticsOperationException">SQL Server devolvió un error funcional controlado.</exception>
    public Task<long> SaveAddressAsync(
        SessionIdentity identity,
        AddressChange change,
        CancellationToken cancellationToken
    )
    {
        Ensure(identity, "LOGISTICA.GESTIONAR");
        ArgumentNullException.ThrowIfNull(change);

        if (
            change.ClientId <= 0
            || change.FromTime.HasValue != change.ToTime.HasValue
            || change.FromTime >= change.ToTime
        )
        {
            throw new ArgumentException("Informá cliente y un horario válido.");
        }

        var email = Optional(change.Email, 254);
        if (email is not null && !MailAddress.TryCreate(email, out _))
        {
            throw new ArgumentException("El correo no es válido.");
        }

        var values = new Dictionary<string, object?>
        {
            ["@I_ID_CLIENTE"] = change.ClientId,
            ["@I_NOMBRE"] = Required(change.Name, 100, "El nombre"),
            ["@I_DOMICILIO"] = Required(change.Address, 300, "El domicilio"),
            ["@I_LOCALIDAD"] = Optional(change.City, 100),
            ["@I_PROVINCIA"] = Optional(change.Province, 100),
            ["@I_CODIGO_POSTAL"] = Optional(change.PostalCode, 20),
            ["@I_CONTACTO"] = Required(change.Contact, 200, "El contacto"),
            ["@I_TELEFONO"] = Required(change.Phone, 50, "El teléfono"),
            ["@I_CORREO"] = email,
            ["@I_HORA_DESDE"] = change.FromTime,
            ["@I_HORA_HASTA"] = change.ToTime,
            ["@I_INSTRUCCIONES"] = Required(change.Instructions, 1000, "Las instrucciones"),
        };

        return Execute(identity, "DIRECCION_GUARDAR", values, cancellationToken);
    }

    /// <summary>Crea una solicitud, incluyendo los activos bajo custodia cuando corresponda.</summary>
    /// <param name="identity">Sesión autenticada.</param>
    /// <param name="change">Servicio, agenda y activos.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Identificador creado.</returns>
    /// <exception cref="ArgumentNullException">La identidad o el cambio son nulos.</exception>
    /// <exception cref="ArgumentException">La agenda o custodia es inválida.</exception>
    /// <exception cref="UnauthorizedAccessException">Falta LOGISTICA.GESTIONAR.</exception>
    /// <exception cref="LogisticsOperationException">SQL Server devolvió un error funcional controlado.</exception>
    public Task<long> CreateRequestAsync(
        SessionIdentity identity,
        LogisticsRequestChange change,
        CancellationToken cancellationToken
    )
    {
        Ensure(identity, "LOGISTICA.GESTIONAR");
        ArgumentNullException.ThrowIfNull(change);

        if (
            change.ClientId <= 0
            || change.AddressId <= 0
            || change.FromTime.HasValue != change.ToTime.HasValue
            || change.FromTime >= change.ToTime
            || change.Assets is not { Count: <= 50 }
            || (
                change.ServiceType is "RETIRO_RECARGA" or "INTERCAMBIO_TEMPORAL"
                && change.DestinationWarehouseId is not > 0
            )
        )
        {
            throw new ArgumentException(
                "Informá cliente, dirección, horario, depósito de los retiros y hasta 50 activos."
            );
        }

        if (
            change.ServiceType is "RETIRO_RECARGA" or "INTERCAMBIO_TEMPORAL" or "DEVOLUCION_CLIENTE"
            && change.Assets.Count == 0
        )
        {
            throw new ArgumentException("Este servicio requiere identificar al menos un activo.");
        }

        foreach (var asset in change.Assets)
        {
            if (
                asset.Owner is not ("CLIENTE" or "OXITIGRE")
                || asset.Role
                    is not ("RETIRO_CLIENTE" or "PRESTAMO_TEMPORAL" or "ENTREGA" or "DEVOLUCION")
                || string.IsNullOrWhiteSpace(asset.SerialNumber)
                || asset.Role == "PRESTAMO_TEMPORAL" && asset.ExpectedReturnDate is null
            )
            {
                throw new ArgumentException(
                    "Revisá propietario, rol, serie y devolución prevista de cada activo."
                );
            }
        }

        var values = new Dictionary<string, object?>
        {
            ["@I_ID_CLIENTE"] = change.ClientId,
            ["@I_ID_DIRECCION"] = change.AddressId,
            ["@I_ID_PEDIDO"] = change.OrderId,
            ["@I_ID_DEPOSITO"] = change.DestinationWarehouseId,
            ["@I_TIPO"] = Required(change.ServiceType, 30, "El tipo"),
            ["@I_PRIORIDAD"] = Required(change.Priority, 20, "La prioridad"),
            ["@I_FECHA"] = change.RequestedDate,
            ["@I_HORA_DESDE"] = change.FromTime,
            ["@I_HORA_HASTA"] = change.ToTime,
            ["@I_INSTRUCCIONES"] = Required(change.Instructions, 1000, "Las instrucciones"),
            ["@I_OBSERVACION"] = Optional(change.Observation, 1000),
            ["@I_JSON"] = JsonSerializer.Serialize(change.Assets),
        };

        return Execute(identity, "SOLICITUD_CREAR", values, cancellationToken);
    }

    /// <summary>Crea una hoja asignada directamente o publicada para autoselección.</summary>
    /// <param name="identity">Sesión autenticada.</param>
    /// <param name="change">Cabecera y orden de solicitudes.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Identificador creado.</returns>
    /// <exception cref="ArgumentNullException">La identidad o el cambio son nulos.</exception>
    /// <exception cref="ArgumentException">La hoja o sus solicitudes son inválidas.</exception>
    /// <exception cref="UnauthorizedAccessException">Falta LOGISTICA.GESTIONAR.</exception>
    /// <exception cref="LogisticsOperationException">SQL Server devolvió un error funcional controlado.</exception>
    public Task<long> CreateRouteAsync(
        SessionIdentity identity,
        RouteChange change,
        CancellationToken cancellationToken
    )
    {
        Ensure(identity, "LOGISTICA.GESTIONAR");
        ArgumentNullException.ThrowIfNull(change);

        var assignmentType = Required(change.AssignmentType, 20, "La asignación")
            .ToUpperInvariant();
        if (
            assignmentType is not ("DIRECTA" or "OFERTA")
            || assignmentType == "DIRECTA"
                && (change.TransporterId is not > 0 || change.VehicleId is not > 0)
            || assignmentType == "OFERTA"
                && (change.TransporterId is not null || change.VehicleId is not null)
            || change.Requests is not { Count: > 0 and <= 100 }
            || change.Requests.Select(item => item.RequestId).Distinct().Count()
                != change.Requests.Count
            || change.RouteType == "URGENTE" && change.Requests.Count != 1
        )
        {
            throw new ArgumentException(
                "Elegí asignación directa con chofer y vehículo, u oferta sin asignarlos; informá entre 1 y 100 solicitudes únicas."
            );
        }

        var values = new Dictionary<string, object?>
        {
            ["@I_FECHA"] = change.RouteDate,
            ["@I_TIPO"] = Required(change.RouteType, 20, "El tipo"),
            ["@I_TIPO_ASIGNACION"] = assignmentType,
            ["@I_ID_TRANSPORTISTA"] = change.TransporterId,
            ["@I_ID_VEHICULO"] = change.VehicleId,
            ["@I_OBSERVACION"] = Required(change.Observation, 1000, "La observación"),
            ["@I_JSON"] = JsonSerializer.Serialize(change.Requests),
        };

        return Execute(identity, "RUTA_CREAR", values, cancellationToken);
    }

    /// <summary>Asigna desde la oficina una hoja publicada a un transportista y vehículo.</summary>
    /// <param name="identity">Sesión operadora autenticada.</param>
    /// <param name="routeId">Hoja ofrecida.</param>
    /// <param name="change">Transportista, vehículo y versión consultada.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Tarea que finaliza al persistir la asignación.</returns>
    /// <exception cref="ArgumentException">Los identificadores o la versión no son válidos.</exception>
    /// <exception cref="UnauthorizedAccessException">Falta LOGISTICA.GESTIONAR.</exception>
    /// <exception cref="LogisticsOperationException">SQL Server rechazó la asignación.</exception>
    public async Task AssignRouteAsync(
        SessionIdentity identity,
        long routeId,
        RouteAssignmentChange change,
        CancellationToken cancellationToken
    )
    {
        Ensure(identity, "LOGISTICA.GESTIONAR");
        ArgumentNullException.ThrowIfNull(change);
        Version(routeId, change.RowVersion);
        if (change.TransporterId <= 0 || change.VehicleId <= 0)
        {
            throw new ArgumentException("Seleccioná un transportista y un vehículo válidos.");
        }

        await Execute(
            identity,
            "RUTA_ASIGNAR",
            new Dictionary<string, object?>
            {
                ["@I_ID"] = routeId,
                ["@I_ID_TRANSPORTISTA"] = change.TransporterId,
                ["@I_ID_VEHICULO"] = change.VehicleId,
                ["@I_ROW_VERSION"] = change.RowVersion,
            },
            cancellationToken
        );
    }

    /// <summary>Permite que el transportista autenticado tome una oferta todavía libre.</summary>
    /// <param name="identity">Sesión del transportista.</param>
    /// <param name="routeId">Hoja ofrecida.</param>
    /// <param name="vehicleId">Vehículo propio o de la empresa elegido para el recorrido.</param>
    /// <param name="rowVersion">Versión consultada de la oferta.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Tarea que finaliza cuando la oferta queda asignada.</returns>
    /// <exception cref="ArgumentException">El identificador o la versión no son válidos.</exception>
    /// <exception cref="UnauthorizedAccessException">La sesión no corresponde a un transportista habilitado.</exception>
    /// <exception cref="LogisticsOperationException">La oferta cambió o el vehículo no es compatible.</exception>
    public async Task ClaimRouteOfferAsync(
        SessionIdentity identity,
        long routeId,
        long vehicleId,
        byte[] rowVersion,
        CancellationToken cancellationToken
    )
    {
        Ensure(identity, "LOGISTICA.DESPACHAR");
        Version(routeId, rowVersion);
        if (
            vehicleId <= 0
            || !identity.Roles.Contains("TRANSPORTISTA", StringComparer.OrdinalIgnoreCase)
        )
        {
            throw new UnauthorizedAccessException("Se requiere un perfil Transportista activo.");
        }

        var snapshot = await LoadDriverSourceAsync(identity, cancellationToken);
        var driver = Driver(snapshot, identity);
        if (
            !snapshot.Routes.Any(route =>
                route.RouteId == routeId
                && route.TransporterId is null
                && route.StatusCode == "OFRECIDA"
            )
        )
        {
            throw new LogisticsOperationException(
                60001,
                "La oferta ya no está disponible. Actualizá la lista."
            );
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        if (
            !snapshot.Vehicles.Any(vehicle =>
                vehicle.VehicleId == vehicleId
                && vehicle.StatusCode == "ACTIVO"
                && (
                    vehicle.OwnerTransporterId is null
                    || vehicle.OwnerTransporterId == driver.TransporterId
                )
                && (vehicle.InsuranceExpiration is null || vehicle.InsuranceExpiration >= today)
                && (vehicle.InspectionExpiration is null || vehicle.InspectionExpiration >= today)
            )
        )
        {
            throw new ArgumentException("El vehículo no está habilitado para este transportista.");
        }

        await Execute(
            identity,
            "RUTA_TOMAR_OFERTA",
            new Dictionary<string, object?>
            {
                ["@I_ID"] = routeId,
                ["@I_ID_VEHICULO"] = vehicleId,
                ["@I_ROW_VERSION"] = rowVersion,
            },
            cancellationToken
        );
    }

    /// <summary>Modifica la fecha, tipo y observación de una oferta todavía libre.</summary>
    /// <param name="identity">Sesión operadora autenticada.</param>
    /// <param name="routeId">Hoja ofrecida.</param>
    /// <param name="change">Agenda descriptiva y versión consultada.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Tarea que finaliza al persistir los cambios.</returns>
    /// <exception cref="ArgumentException">Los datos o la versión no son válidos.</exception>
    /// <exception cref="UnauthorizedAccessException">Falta LOGISTICA.GESTIONAR.</exception>
    /// <exception cref="LogisticsOperationException">La oferta cambió o dejó de estar libre.</exception>
    public async Task UpdateRouteOfferAsync(
        SessionIdentity identity,
        long routeId,
        RouteOfferChange change,
        CancellationToken cancellationToken
    )
    {
        Ensure(identity, "LOGISTICA.GESTIONAR");
        ArgumentNullException.ThrowIfNull(change);
        Version(routeId, change.RowVersion);
        var routeType = Required(change.RouteType, 20, "El tipo").ToUpperInvariant();
        if (routeType is not ("NORMAL" or "URGENTE"))
        {
            throw new ArgumentException("El tipo de hoja no es válido.");
        }

        await Execute(
            identity,
            "RUTA_OFERTA_ACTUALIZAR",
            new Dictionary<string, object?>
            {
                ["@I_ID"] = routeId,
                ["@I_FECHA"] = change.RouteDate,
                ["@I_TIPO"] = routeType,
                ["@I_OBSERVACION"] = Required(change.Observation, 1000, "La observación"),
                ["@I_ROW_VERSION"] = change.RowVersion,
            },
            cancellationToken
        );
    }

    /// <summary>Crea o modifica el perfil operativo de un usuario transportista.</summary>
    /// <param name="identity">Sesión autenticada.</param>
    /// <param name="change">Usuario, licencia, vínculo y vigencia.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Identificador del perfil afectado.</returns>
    /// <exception cref="ArgumentException">Los datos o la versión no son válidos.</exception>
    /// <exception cref="UnauthorizedAccessException">Falta LOGISTICA.GESTIONAR.</exception>
    public Task<long> SaveTransporterAsync(
        SessionIdentity identity,
        TransporterChange change,
        CancellationToken cancellationToken
    )
    {
        Ensure(identity, "LOGISTICA.GESTIONAR");
        ArgumentNullException.ThrowIfNull(change);
        if (
            change.UserId <= 0
            || change.RelationshipType is not ("EMPRESA" or "PARTICULAR")
            || change.StatusCode is not ("ACTIVO" or "INACTIVO")
        )
        {
            throw new ArgumentException("Informá usuario, vínculo y estado válidos.");
        }
        if (change.TransporterId is { } id)
            Version(id, change.RowVersion ?? []);

        return Execute(
            identity,
            "TRANSPORTISTA_GUARDAR",
            new Dictionary<string, object?>
            {
                ["@I_ID"] = change.TransporterId,
                ["@I_ID_USUARIO"] = change.UserId,
                ["@I_TIPO_PROPIEDAD"] = change.RelationshipType,
                ["@I_DOCUMENTO"] = Optional(change.Document, 30),
                ["@I_TELEFONO"] = Optional(change.Phone, 50),
                ["@I_LICENCIA"] = Required(change.License, 100, "La licencia"),
                ["@I_CATEGORIA"] = Optional(change.LicenseCategory, 30),
                ["@I_VENCIMIENTO"] = change.LicenseExpiration,
                ["@I_OBSERVACION"] = Optional(change.Observation, 1000),
                ["@I_CODIGO_ESTADO"] = change.StatusCode,
                ["@I_ROW_VERSION"] = change.RowVersion,
            },
            cancellationToken
        );
    }

    /// <summary>Crea o modifica un vehículo y valida su propietario.</summary>
    /// <param name="identity">Sesión autenticada.</param>
    /// <param name="change">Patente, características, propiedad y vencimientos.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Identificador del vehículo afectado.</returns>
    /// <exception cref="ArgumentException">La patente, propiedad o versión son inválidas.</exception>
    /// <exception cref="UnauthorizedAccessException">Falta LOGISTICA.GESTIONAR.</exception>
    public Task<long> SaveVehicleAsync(
        SessionIdentity identity,
        LogisticsVehicleChange change,
        CancellationToken cancellationToken
    )
    {
        Ensure(identity, "LOGISTICA.GESTIONAR");
        ArgumentNullException.ThrowIfNull(change);
        if (
            change.VehicleType is not ("CAMION" or "CAMIONETA" or "UTILITARIO" or "AUTO" or "OTRO")
            || change.OwnershipType is not ("EMPRESA" or "TRANSPORTISTA")
            || change.OwnershipType == "EMPRESA" && change.OwnerTransporterId is not null
            || change.OwnershipType == "TRANSPORTISTA" && change.OwnerTransporterId is null
            || change.Year is < 1950 or > 2200
            || change.LoadCapacityKg is <= 0
            || change.StatusCode is not ("ACTIVO" or "INACTIVO")
        )
        {
            throw new ArgumentException(
                "Revisá tipo, propietario, año, capacidad y estado del vehículo."
            );
        }
        if (change.VehicleId is { } id)
            Version(id, change.RowVersion ?? []);

        return Execute(
            identity,
            "VEHICULO_GUARDAR",
            new Dictionary<string, object?>
            {
                ["@I_ID"] = change.VehicleId,
                ["@I_ID_TRANSPORTISTA"] = change.OwnerTransporterId,
                ["@I_PATENTE"] = ArgentineLicensePlate.Normalize(change.Plate),
                ["@I_TIPO"] = change.VehicleType,
                ["@I_TIPO_PROPIEDAD"] = change.OwnershipType,
                ["@I_MARCA"] = Optional(change.Brand, 80),
                ["@I_MODELO"] = Optional(change.Model, 80),
                ["@I_ANIO"] = change.Year,
                ["@I_CAPACIDAD"] = change.LoadCapacityKg,
                ["@I_POLIZA"] = Optional(change.InsurancePolicy, 100),
                ["@I_VENCIMIENTO_SEGURO"] = change.InsuranceExpiration,
                ["@I_VENCIMIENTO_REVISION"] = change.InspectionExpiration,
                ["@I_OBSERVACION"] = Optional(change.Observation, 1000),
                ["@I_CODIGO_ESTADO"] = change.StatusCode,
                ["@I_ROW_VERSION"] = change.RowVersion,
            },
            cancellationToken
        );
    }

    /// <summary>Despacha o cancela una hoja planificada.</summary>
    /// <param name="identity">Sesión autenticada.</param>
    /// <param name="routeId">Hoja afectada.</param>
    /// <param name="rowVersion">Versión consultada.</param>
    /// <param name="cancel">Indica cancelación en vez de despacho.</param>
    /// <param name="observation">Motivo obligatorio al cancelar; no se usa al despachar.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Tarea que finaliza al persistir.</returns>
    /// <exception cref="ArgumentException">El identificador, la versión o el motivo son inválidos.</exception>
    /// <exception cref="UnauthorizedAccessException">Falta el permiso correspondiente.</exception>
    /// <exception cref="LogisticsOperationException">SQL Server rechazó la transición.</exception>
    public async Task ChangeRouteAsync(
        SessionIdentity identity,
        long routeId,
        byte[] rowVersion,
        bool cancel,
        string? observation,
        CancellationToken cancellationToken
    )
    {
        Ensure(identity, cancel ? "LOGISTICA.GESTIONAR" : "LOGISTICA.DESPACHAR");
        Version(routeId, rowVersion);

        if (IsRestrictedDriver(identity))
        {
            if (cancel)
            {
                throw new UnauthorizedAccessException(
                    "El transportista no puede cancelar una hoja asignada."
                );
            }

            var snapshot = await LoadDriverSourceAsync(identity, cancellationToken);
            var driver = Driver(snapshot, identity);
            if (
                !snapshot.Routes.Any(route =>
                    route.RouteId == routeId
                    && route.TransporterId == driver.TransporterId
                    && route.StatusCode == "PLANIFICADA"
                )
            )
            {
                throw new UnauthorizedAccessException(
                    "La hoja no está planificada para este transportista."
                );
            }
        }

        var values = new Dictionary<string, object?>
        {
            ["@I_ID"] = routeId,
            ["@I_ROW_VERSION"] = rowVersion,
            ["@I_OBSERVACION"] = cancel ? Required(observation, 1000, "El motivo") : null,
        };

        await Execute(
            identity,
            cancel ? "RUTA_CANCELAR" : "RUTA_DESPACHAR",
            values,
            cancellationToken
        );
    }

    /// <summary>Pausa o reanuda una hoja propia que ya fue despachada.</summary>
    /// <param name="identity">Sesión autenticada.</param>
    /// <param name="routeId">Hoja en ejecución.</param>
    /// <param name="rowVersion">Versión consultada.</param>
    /// <param name="pause">Indica pausa; <see langword="false"/> indica reanudación.</param>
    /// <param name="observation">Motivo obligatorio de la pausa o reanudación.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Tarea que finaliza al persistir la transición.</returns>
    /// <exception cref="ArgumentException">El identificador, la versión o el motivo son inválidos.</exception>
    /// <exception cref="UnauthorizedAccessException">La hoja no pertenece al transportista.</exception>
    /// <exception cref="LogisticsOperationException">SQL Server rechazó la transición.</exception>
    public async Task ChangeRoutePauseAsync(
        SessionIdentity identity,
        long routeId,
        byte[] rowVersion,
        bool pause,
        string? observation,
        CancellationToken cancellationToken
    )
    {
        Ensure(identity, "LOGISTICA.DESPACHAR");
        Version(routeId, rowVersion);
        var reason = Required(
            observation,
            1000,
            pause ? "El motivo de la pausa" : "La observación"
        );

        if (IsRestrictedDriver(identity))
        {
            var snapshot = await LoadDriverSourceAsync(identity, cancellationToken);
            var driver = Driver(snapshot, identity);
            var expected = pause ? "DESPACHADA" : "PAUSADA";
            if (
                !snapshot.Routes.Any(route =>
                    route.RouteId == routeId
                    && route.TransporterId == driver.TransporterId
                    && route.StatusCode == expected
                )
            )
            {
                throw new UnauthorizedAccessException(
                    "La hoja no está en un estado válido para esta acción."
                );
            }
        }

        await Execute(
            identity,
            pause ? "RUTA_PAUSAR" : "RUTA_REANUDAR",
            new Dictionary<string, object?>
            {
                ["@I_ID"] = routeId,
                ["@I_ROW_VERSION"] = rowVersion,
                ["@I_OBSERVACION"] = reason,
            },
            cancellationToken
        );
    }

    /// <summary>Registra la llegada a una parada de una hoja despachada.</summary>
    /// <param name="identity">Sesión autenticada.</param>
    /// <param name="stopId">Parada afectada.</param>
    /// <param name="rowVersion">Versión consultada.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Tarea que finaliza al guardar la hora de llegada.</returns>
    /// <exception cref="ArgumentException">El identificador o la versión son inválidos.</exception>
    /// <exception cref="UnauthorizedAccessException">La parada no pertenece al transportista.</exception>
    /// <exception cref="LogisticsOperationException">SQL Server rechazó la transición.</exception>
    public async Task MarkStopArrivalAsync(
        SessionIdentity identity,
        long stopId,
        byte[] rowVersion,
        CancellationToken cancellationToken
    )
    {
        Ensure(identity, "LOGISTICA.DESPACHAR");
        Version(stopId, rowVersion);
        await EnsureAssignedDriverStopAsync(identity, stopId, cancellationToken);

        await Execute(
            identity,
            "PARADA_LLEGADA",
            new Dictionary<string, object?> { ["@I_ID"] = stopId, ["@I_ROW_VERSION"] = rowVersion },
            cancellationToken
        );
    }

    /// <summary>Registra una incidencia sin cerrar ni alterar el resultado de la parada.</summary>
    /// <param name="identity">Sesión autenticada.</param>
    /// <param name="stopId">Parada afectada.</param>
    /// <param name="type">Categoría operativa estable.</param>
    /// <param name="observation">Descripción concreta de lo ocurrido.</param>
    /// <param name="rowVersion">Versión consultada.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Tarea que finaliza al conservar el evento.</returns>
    /// <exception cref="ArgumentException">La categoría, observación o versión son inválidas.</exception>
    /// <exception cref="UnauthorizedAccessException">La parada no pertenece al transportista.</exception>
    /// <exception cref="LogisticsOperationException">SQL Server rechazó la operación.</exception>
    public async Task ReportStopIncidentAsync(
        SessionIdentity identity,
        long stopId,
        string type,
        string observation,
        byte[] rowVersion,
        CancellationToken cancellationToken
    )
    {
        Ensure(identity, "LOGISTICA.DESPACHAR");
        Version(stopId, rowVersion);

        var normalizedType = Required(type, 30, "El tipo").ToUpperInvariant();
        if (
            normalizedType
            is not (
                "DEMORA"
                or "CLIENTE_AUSENTE"
                or "ACCESO_IMPEDIDO"
                or "MERCADERIA"
                or "VEHICULO"
                or "OTRO"
            )
        )
        {
            throw new ArgumentException("El tipo de incidencia no es válido.");
        }

        await EnsureAssignedDriverStopAsync(identity, stopId, cancellationToken);
        await Execute(
            identity,
            "PARADA_INCIDENCIA",
            new Dictionary<string, object?>
            {
                ["@I_ID"] = stopId,
                ["@I_RESULTADO"] = normalizedType,
                ["@I_OBSERVACION"] = Required(observation, 1000, "La observación"),
                ["@I_ROW_VERSION"] = rowVersion,
            },
            cancellationToken
        );
    }

    /// <summary>Confirma el resultado de una parada y cierra automáticamente la hoja al finalizar todas.</summary>
    /// <param name="identity">Sesión autenticada.</param>
    /// <param name="stopId">Parada afectada.</param>
    /// <param name="result">Resultado funcional.</param>
    /// <param name="observation">Detalle obligatorio.</param>
    /// <param name="rowVersion">Versión consultada.</param>
    /// <param name="completedRequestAssetIds">Activos efectivamente operados si el resultado fue parcial.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Tarea que finaliza al persistir.</returns>
    /// <exception cref="ArgumentException">El resultado, observación o versión son inválidos.</exception>
    /// <exception cref="UnauthorizedAccessException">Falta LOGISTICA.DESPACHAR.</exception>
    /// <exception cref="LogisticsOperationException">SQL Server rechazó la confirmación.</exception>
    public async Task CompleteStopAsync(
        SessionIdentity identity,
        long stopId,
        string result,
        string observation,
        byte[] rowVersion,
        IReadOnlyList<long>? completedRequestAssetIds,
        CancellationToken cancellationToken
    )
    {
        Ensure(identity, "LOGISTICA.DESPACHAR");
        Version(stopId, rowVersion);

        var assignedStop = await EnsureAssignedDriverStopAsync(identity, stopId, cancellationToken);
        if (assignedStop is { ArrivalUtc: null })
        {
            throw new ArgumentException(
                "Registrá la llegada antes de confirmar el resultado de la parada."
            );
        }

        var completedAssets = completedRequestAssetIds ?? [];
        if (
            completedAssets.Any(id => id <= 0)
            || completedAssets.Distinct().Count() != completedAssets.Count
        )
        {
            throw new ArgumentException("La selección de activos operados no es válida.");
        }

        var values = new Dictionary<string, object?>
        {
            ["@I_ID"] = stopId,
            ["@I_RESULTADO"] = Required(result, 30, "El resultado"),
            ["@I_OBSERVACION"] = Required(observation, 1000, "La observación"),
            ["@I_ROW_VERSION"] = rowVersion,
            ["@I_JSON"] =
                completedAssets.Count == 0 ? null : JsonSerializer.Serialize(completedAssets),
        };

        await Execute(identity, "PARADA_CONFIRMAR", values, cancellationToken);
    }

    /// <summary>Exige que una parada planificada pertenezca a la hoja despachada del chofer.</summary>
    /// <param name="identity">Sesión del usuario que intenta operar la parada.</param>
    /// <param name="stopId">Parada cuyo acceso se comprueba.</param>
    /// <param name="cancellationToken">Token para cancelar la lectura del estado logístico.</param>
    /// <returns>Parada autorizada; nulo si la sesión no está restringida al rol transportista.</returns>
    /// <exception cref="UnauthorizedAccessException">El chofer no está activo o la parada no le corresponde.</exception>
    private async Task<RouteStop?> EnsureAssignedDriverStopAsync(
        SessionIdentity identity,
        long stopId,
        CancellationToken cancellationToken
    )
    {
        if (!IsRestrictedDriver(identity))
        {
            return null;
        }

        var snapshot = await LoadDriverSourceAsync(identity, cancellationToken);
        var driver = Driver(snapshot, identity);
        var stop = snapshot.Stops.SingleOrDefault(item =>
            item.StopId == stopId
            && item.StatusCode == "PLANIFICADA"
            && snapshot.Routes.Any(route =>
                route.RouteId == item.RouteId
                && route.TransporterId == driver.TransporterId
                && route.StatusCode == "DESPACHADA"
            )
        );

        return stop
            ?? throw new UnauthorizedAccessException(
                "La parada no está en una hoja despachada para este transportista."
            );
    }

    /// <summary>Consulta el estado de la empresa desde la sesión para validar al transportista.</summary>
    /// <param name="identity">Sesión autenticada que determina la empresa y el usuario.</param>
    /// <param name="cancellationToken">Token para cancelar la consulta.</param>
    /// <returns>Transportistas, vehículos, hojas y paradas vigentes de la empresa.</returns>
    private Task<LogisticsSnapshot> LoadDriverSourceAsync(
        SessionIdentity identity,
        CancellationToken cancellationToken
    ) =>
        store.GetAsync(
            identity.CompanyId,
            identity.CompanyCode,
            identity.UserId,
            identity.SessionId,
            cancellationToken
        );

    /// <summary>Obtiene el perfil activo que corresponde al usuario autenticado.</summary>
    /// <param name="snapshot">Estado logístico consultado para la empresa.</param>
    /// <param name="identity">Sesión del usuario transportista.</param>
    /// <returns>Perfil transportista activo del usuario.</returns>
    /// <exception cref="UnauthorizedAccessException">No existe un perfil activo para ese usuario.</exception>
    private static Transporter Driver(LogisticsSnapshot snapshot, SessionIdentity identity) =>
        snapshot.Transporters.SingleOrDefault(driver =>
            driver.UserId == identity.UserId && driver.StatusCode == "ACTIVO"
        )
        ?? throw new UnauthorizedAccessException(
            "El usuario no posee un perfil transportista activo."
        );

    /// <summary>Determina si se debe limitar la operación a hojas asignadas al chofer.</summary>
    /// <param name="identity">Sesión cuyos roles se comprueban.</param>
    /// <returns>Verdadero para un transportista que no posee el rol administrador.</returns>
    private static bool IsRestrictedDriver(SessionIdentity identity) =>
        identity.Roles.Contains("TRANSPORTISTA", StringComparer.OrdinalIgnoreCase)
        && !identity.Roles.Contains("ADMINISTRADOR", StringComparer.OrdinalIgnoreCase);

    /// <summary>Ejecuta una acción logística usando exclusivamente el contexto de la sesión.</summary>
    /// <param name="identity">Sesión que fija empresa, usuario y autorización.</param>
    /// <param name="action">Acción reconocida por el procedimiento logístico.</param>
    /// <param name="values">Parámetros propios de la acción.</param>
    /// <param name="cancellationToken">Token para cancelar la operación.</param>
    /// <returns>Identificador devuelto por la acción ejecutada.</returns>
    private Task<long> Execute(
        SessionIdentity identity,
        string action,
        IReadOnlyDictionary<string, object?> values,
        CancellationToken cancellationToken
    ) =>
        store.ExecuteAsync(
            identity.CompanyId,
            identity.CompanyCode,
            identity.UserId,
            identity.SessionId,
            action,
            values,
            cancellationToken
        );

    /// <summary>Exige el permiso operativo sin confiar en roles o empresa declarados por el cliente.</summary>
    /// <param name="identity">Sesión autenticada a comprobar.</param>
    /// <param name="permission">Código del permiso requerido.</param>
    /// <exception cref="ArgumentNullException">No se informó una sesión.</exception>
    /// <exception cref="UnauthorizedAccessException">La sesión no posee el permiso.</exception>
    private static void Ensure(SessionIdentity? identity, string permission)
    {
        ArgumentNullException.ThrowIfNull(identity);

        if (!identity.Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException($"Se requiere {permission}.");
        }
    }

    /// <summary>Exige identificador y versión de fila válidos para evitar editar datos obsoletos.</summary>
    /// <param name="id">Identificador positivo de la entidad.</param>
    /// <param name="version">Versión binaria de ocho bytes leída previamente.</param>
    /// <exception cref="ArgumentException">El identificador o la versión no son válidos.</exception>
    private static void Version(long id, byte[] version)
    {
        if (id <= 0 || version is not { Length: 8 })
        {
            throw new ArgumentException("El identificador o la versión no son válidos.");
        }
    }

    /// <summary>Recorta y valida un campo de texto obligatorio.</summary>
    /// <param name="value">Valor proporcionado por el operador.</param>
    /// <param name="maximumLength">Máximo de caracteres permitido.</param>
    /// <param name="field">Nombre usado en el mensaje de validación.</param>
    /// <returns>Valor no vacío y sin espacios en los extremos.</returns>
    /// <exception cref="ArgumentException">El valor falta o supera el máximo.</exception>
    private static string Required(string? value, int maximumLength, string field)
    {
        var result = value?.Trim();
        if (string.IsNullOrEmpty(result) || result.Length > maximumLength)
        {
            throw new ArgumentException(
                $"{field} es obligatorio y admite hasta {maximumLength} caracteres."
            );
        }

        return result;
    }

    /// <summary>Recorta un campo opcional y convierte blancos a nulo.</summary>
    /// <param name="value">Valor opcional proporcionado por el operador.</param>
    /// <param name="maximumLength">Máximo de caracteres permitido.</param>
    /// <returns>Valor recortado o nulo si no hay contenido.</returns>
    /// <exception cref="ArgumentException">El texto informado supera el máximo.</exception>
    private static string? Optional(string? value, int maximumLength)
    {
        var result = value?.Trim();
        if (string.IsNullOrEmpty(result))
        {
            return null;
        }

        if (result.Length > maximumLength)
        {
            throw new ArgumentException($"El valor admite hasta {maximumLength} caracteres.");
        }

        return result;
    }
}
