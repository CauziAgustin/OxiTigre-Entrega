/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Logistics.LogisticsModels
Archivo: LogisticsModels.cs | Versión: 1.3.0 | Fecha: 2026-08-28 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Define agenda, custodia, hojas de ruta, eventos y avisos logísticos.
Historial: 1.0.0 | 2026-08-26 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Perfiles transportistas, vehículos y asignación histórica.
Historial: 1.2.0 | 2026-08-28 | FABRICA | Agustin Omar Cauzi | Pedidos candidatos, depósito receptor y activos operados por parada.
Historial: 1.3.0 | 2026-08-28 | FABRICA | Agustin Omar Cauzi | Vista móvil acotada al recorrido del transportista autenticado.
===============================================================================
*/
namespace OxiTigre.BLL.Logistics;

/// <summary>Reúne toda la información operativa de Logística para una empresa.</summary>
public sealed record LogisticsSnapshot(
    IReadOnlyList<LogisticsAddress> Addresses,
    IReadOnlyList<LogisticsRequest> Requests,
    IReadOnlyList<LogisticsAsset> Assets,
    IReadOnlyList<RouteSheet> Routes,
    IReadOnlyList<RouteStop> Stops,
    IReadOnlyList<LogisticsEvent> Events,
    IReadOnlyList<LogisticsNotification> Notifications,
    IReadOnlyList<LogisticsClient> Clients,
    IReadOnlyList<Transporter> Transporters,
    IReadOnlyList<LogisticsVehicle> Vehicles,
    IReadOnlyList<TransporterUser> TransporterUsers,
    IReadOnlyList<LogisticsOrderCandidate> OrderCandidates,
    IReadOnlyList<LogisticsWarehouse> Warehouses
);

/// <summary>Reúne el trabajo activo, las ofertas y el historial propio del transportista autenticado.</summary>
public sealed record DriverSnapshot(
    Transporter Driver,
    IReadOnlyList<RouteSheet> Routes,
    IReadOnlyList<RouteSheet> Offers,
    IReadOnlyList<RouteSheet> History,
    IReadOnlyList<RouteStop> Stops,
    IReadOnlyList<LogisticsAsset> Assets,
    IReadOnlyList<LogisticsVehicle> Vehicles
);

/// <summary>Representa un retiro o una entrega que el pedido requiere planificar.</summary>
public sealed record LogisticsOrderCandidate(
    long OrderId,
    long ClientId,
    string OrderCode,
    string Client,
    string Operation,
    string ServiceType,
    DateOnly OrderDate,
    decimal Total,
    decimal? Outstanding,
    string PaymentStatus,
    int AssetCount,
    DateOnly? ExpectedReturnDate,
    string Summary
);

/// <summary>Representa un depósito activo seleccionable para custodiar un retiro.</summary>
public sealed record LogisticsWarehouse(long WarehouseId, string Code, string Name);

/// <summary>Representa un domicilio y su ventana habitual de atención.</summary>
public sealed record LogisticsAddress(
    long AddressId,
    long ClientId,
    string Code,
    string Name,
    string Client,
    string Address,
    string? City,
    string? Province,
    string? PostalCode,
    decimal? Latitude,
    decimal? Longitude,
    string Contact,
    string Phone,
    string? Email,
    TimeOnly? FromTime,
    TimeOnly? ToTime,
    short ServiceMinutes,
    string? Restrictions,
    string Instructions,
    string StatusCode,
    byte[] RowVersion
);

/// <summary>Representa una solicitud logística planificable.</summary>
public sealed record LogisticsRequest(
    long RequestId,
    long ClientId,
    long AddressId,
    long? OrderId,
    string Code,
    string Client,
    string Address,
    string ServiceType,
    string Priority,
    DateOnly RequestedDate,
    TimeOnly? FromTime,
    TimeOnly? ToTime,
    string Instructions,
    string? Observation,
    string StatusCode,
    byte[] RowVersion,
    long? DestinationWarehouseId,
    string? DestinationWarehouse
);

/// <summary>Identifica un activo propio o del cliente cuya custodia interviene en un servicio.</summary>
public sealed record LogisticsAsset(
    long RequestAssetId,
    long RequestId,
    long? AssetId,
    string Owner,
    string Role,
    string SerialNumber,
    string? Product,
    decimal? ContentQuantity,
    string? Unit,
    string Condition,
    string? Observation,
    string StatusCode,
    DateOnly? ExpectedReturnDate,
    long? LoanId
);

/// <summary>Representa una hoja de ruta histórica.</summary>
public sealed record RouteSheet(
    long RouteId,
    long? TransporterId,
    long? VehicleId,
    string Code,
    DateOnly RouteDate,
    string RouteType,
    string AssignmentType,
    string? Driver,
    string? Plate,
    string Observation,
    DateTime? AssignedUtc,
    DateTime? DepartureUtc,
    DateTime? ClosedUtc,
    string StatusCode,
    byte[] RowVersion
);

/// <summary>Representa una parada con la foto de los datos vigentes al armar la hoja.</summary>
public sealed record RouteStop(
    long StopId,
    long RouteId,
    long RequestId,
    short Order,
    string Client,
    string Address,
    string Contact,
    string Phone,
    TimeOnly? FromTime,
    TimeOnly? ToTime,
    string Priority,
    string Instructions,
    DateTime? ArrivalUtc,
    DateTime? DepartureUtc,
    string? Result,
    string? ResultObservation,
    string StatusCode,
    byte[] RowVersion
);

/// <summary>Representa un evento inmutable del circuito.</summary>
public sealed record LogisticsEvent(
    long EventId,
    long? RequestId,
    long? RouteId,
    long? StopId,
    string EventType,
    string? PreviousStatus,
    string? NewStatus,
    string Observation,
    long UserId,
    long SessionId,
    Guid Correlation,
    DateTime DateUtc
);

/// <summary>Representa un aviso pendiente, enviado o fallido.</summary>
public sealed record LogisticsNotification(
    long NotificationId,
    long? RequestId,
    long? RouteId,
    string EventType,
    string Channel,
    string Recipient,
    string Message,
    short Attempts,
    string StatusCode,
    DateTime CreatedUtc,
    DateTime? SentUtc
);

/// <summary>Representa un cliente disponible para configurar su logística.</summary>
public sealed record LogisticsClient(long ClientId, string Code, string Name);

/// <summary>Representa el perfil operativo de una persona que transporta mercadería.</summary>
public sealed record Transporter(
    long TransporterId,
    long UserId,
    string Code,
    string UserName,
    string FullName,
    string RelationshipType,
    string? Document,
    string? Phone,
    string License,
    string? LicenseCategory,
    DateOnly? LicenseExpiration,
    string? Observation,
    string StatusCode,
    byte[] RowVersion
);

/// <summary>Representa un vehículo propio o perteneciente a un transportista particular.</summary>
public sealed record LogisticsVehicle(
    long VehicleId,
    long? OwnerTransporterId,
    string Code,
    string Plate,
    string VehicleType,
    string OwnershipType,
    string? Owner,
    string? Brand,
    string? Model,
    short? Year,
    decimal? LoadCapacityKg,
    string? InsurancePolicy,
    DateOnly? InsuranceExpiration,
    DateOnly? InspectionExpiration,
    string? Observation,
    string StatusCode,
    byte[] RowVersion
);

/// <summary>Representa un usuario con rol TRANSPORTISTA seleccionable para crear un perfil.</summary>
public sealed record TransporterUser(long UserId, string UserName, string FullName);

/// <summary>Datos para registrar un domicilio operativo.</summary>
public sealed record AddressChange(
    long ClientId,
    string Name,
    string Address,
    string? City,
    string? Province,
    string? PostalCode,
    string Contact,
    string Phone,
    string? Email,
    TimeOnly? FromTime,
    TimeOnly? ToTime,
    string Instructions
);

/// <summary>Datos de un activo involucrado en una solicitud.</summary>
public sealed record RequestAssetChange(
    long? AssetId,
    string Owner,
    string Role,
    string SerialNumber,
    string? Product,
    decimal? ContentQuantity,
    string? Unit,
    string Condition,
    string? Observation,
    DateOnly? ExpectedReturnDate = null
);

/// <summary>Datos para crear una solicitud y su custodia.</summary>
public sealed record LogisticsRequestChange(
    long ClientId,
    long AddressId,
    long? OrderId,
    string ServiceType,
    string Priority,
    DateOnly RequestedDate,
    TimeOnly? FromTime,
    TimeOnly? ToTime,
    string Instructions,
    string? Observation,
    IReadOnlyList<RequestAssetChange> Assets,
    long? DestinationWarehouseId = null
);

/// <summary>Identifica una solicitud y su orden dentro de una hoja.</summary>
public sealed record RouteRequestChange(short Order, long RequestId);

/// <summary>Datos para crear una hoja con asignación directa o publicarla para autoselección.</summary>
public sealed record RouteChange(
    DateOnly RouteDate,
    string RouteType,
    string AssignmentType,
    long? TransporterId,
    long? VehicleId,
    string Observation,
    IReadOnlyList<RouteRequestChange> Requests
);

/// <summary>Datos para asignar una oferta a un transportista y vehículo compatibles.</summary>
public sealed record RouteAssignmentChange(long TransporterId, long VehicleId, byte[] RowVersion);

/// <summary>Datos editables de una hoja mientras continúa publicada y sin asignar.</summary>
public sealed record RouteOfferChange(
    DateOnly RouteDate,
    string RouteType,
    string Observation,
    byte[] RowVersion
);

/// <summary>Datos para crear o modificar un perfil transportista.</summary>
public sealed record TransporterChange(
    long? TransporterId,
    long UserId,
    string RelationshipType,
    string? Document,
    string? Phone,
    string License,
    string? LicenseCategory,
    DateOnly? LicenseExpiration,
    string? Observation,
    string StatusCode,
    byte[]? RowVersion
);

/// <summary>Datos para crear o modificar un vehículo logístico.</summary>
public sealed record LogisticsVehicleChange(
    long? VehicleId,
    long? OwnerTransporterId,
    string Plate,
    string VehicleType,
    string OwnershipType,
    string? Brand,
    string? Model,
    short? Year,
    decimal? LoadCapacityKg,
    string? InsurancePolicy,
    DateOnly? InsuranceExpiration,
    DateOnly? InspectionExpiration,
    string? Observation,
    string StatusCode,
    byte[]? RowVersion
);

/// <summary>Error funcional controlado del módulo Logística.</summary>
public sealed class LogisticsOperationException(long errorCode, string message) : Exception(message)
{
    /// <summary>Código estable registrado en el catálogo de errores.</summary>
    public long ErrorCode { get; } = errorCode;
}
