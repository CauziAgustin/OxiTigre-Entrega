/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.Contracts.Logistics.LogisticsContracts
Archivo: LogisticsContracts.cs | Versión: 1.4.0 | Fecha: 2026-08-29 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Define el contrato HTTP del circuito logístico de Fase 8.
Historial: 1.0.0 | 2026-08-26 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Transportistas, vehículos y asignación tipada a rutas.
Historial: 1.2.0 | 2026-08-28 | FABRICA | Agustin Omar Cauzi | Pedidos candidatos, depósito receptor y confirmación parcial de activos.
Historial: 1.3.0 | 2026-08-28 | FABRICA | Agustin Omar Cauzi | Contrato móvil limitado al recorrido del transportista autenticado.
Historial: 1.4.0 | 2026-08-29 | FABRICA | Agustin Omar Cauzi | Llegada e incidencias operativas desde la aplicación móvil.
===============================================================================
*/
namespace OxiTigre.Contracts.Logistics;

/// <summary>Reúne agenda, custodia, hojas históricas, eventos y avisos.</summary>
public sealed record LogisticsSnapshotResponse(
    IReadOnlyList<LogisticsAddressResponse> Addresses,
    IReadOnlyList<LogisticsRequestResponse> Requests,
    IReadOnlyList<LogisticsAssetResponse> Assets,
    IReadOnlyList<RouteSheetResponse> Routes,
    IReadOnlyList<RouteStopResponse> Stops,
    IReadOnlyList<LogisticsEventResponse> Events,
    IReadOnlyList<LogisticsNotificationResponse> Notifications,
    IReadOnlyList<LogisticsClientResponse> Clients,
    IReadOnlyList<TransporterResponse> Transporters,
    IReadOnlyList<LogisticsVehicleResponse> Vehicles,
    IReadOnlyList<TransporterUserResponse> TransporterUsers,
    IReadOnlyList<LogisticsOrderCandidateResponse> OrderCandidates,
    IReadOnlyList<LogisticsWarehouseResponse> Warehouses
);

/// <summary>Devuelve el perfil, trabajo activo, ofertas e historial propio del transportista.</summary>
public sealed record DriverSnapshotResponse(
    DriverProfileResponse Driver,
    IReadOnlyList<RouteSheetResponse> Routes,
    IReadOnlyList<RouteSheetResponse> Offers,
    IReadOnlyList<RouteSheetResponse> History,
    IReadOnlyList<RouteStopResponse> Stops,
    IReadOnlyList<LogisticsAssetResponse> Assets,
    IReadOnlyList<LogisticsVehicleResponse> Vehicles
);

/// <summary>Expone los datos operativos propios que el transportista necesita durante el recorrido.</summary>
public sealed record DriverProfileResponse(
    long TransporterId,
    string Code,
    string FullName,
    string RelationshipType,
    string? Document,
    string? Phone,
    string License,
    string? LicenseCategory,
    DateOnly? LicenseExpiration,
    string? Observation
);

/// <summary>Expone una operación de retiro o entrega derivada de un pedido confirmado.</summary>
public sealed record LogisticsOrderCandidateResponse(
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

/// <summary>Expone un depósito seleccionable como destino de un retiro.</summary>
public sealed record LogisticsWarehouseResponse(long WarehouseId, string Code, string Name);

/// <summary>Expone un domicilio operativo.</summary>
public sealed record LogisticsAddressResponse(
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
    string RowVersion
);

/// <summary>Expone una solicitud planificable.</summary>
public sealed record LogisticsRequestResponse(
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
    string RowVersion,
    long? DestinationWarehouseId,
    string? DestinationWarehouse
);

/// <summary>Expone un activo cuya custodia participa en una solicitud.</summary>
public sealed record LogisticsAssetResponse(
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

/// <summary>Expone una hoja de ruta histórica.</summary>
public sealed record RouteSheetResponse(
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
    string RowVersion
);

/// <summary>Expone una parada y su foto histórica.</summary>
public sealed record RouteStopResponse(
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
    string RowVersion
);

/// <summary>Expone un evento inmutable.</summary>
public sealed record LogisticsEventResponse(
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

/// <summary>Expone un aviso de la bandeja transaccional.</summary>
public sealed record LogisticsNotificationResponse(
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

/// <summary>Expone un cliente seleccionable.</summary>
public sealed record LogisticsClientResponse(long ClientId, string Code, string Name);

/// <summary>Expone un perfil transportista vinculado a un usuario de la empresa.</summary>
public sealed record TransporterResponse(
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
    string RowVersion
);

/// <summary>Expone un vehículo disponible para hojas de ruta.</summary>
public sealed record LogisticsVehicleResponse(
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
    string RowVersion
);

/// <summary>Expone un usuario con rol TRANSPORTISTA seleccionable.</summary>
public sealed record TransporterUserResponse(long UserId, string UserName, string FullName);

/// <summary>Solicita crear un domicilio operativo.</summary>
public sealed record SaveLogisticsAddressRequest(
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

/// <summary>Informa un activo propio o ajeno involucrado en el servicio.</summary>
public sealed record LogisticsRequestAssetRequest(
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

/// <summary>Solicita crear una solicitud logística.</summary>
public sealed record CreateLogisticsRequest(
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
    IReadOnlyList<LogisticsRequestAssetRequest> Assets,
    long? DestinationWarehouseId = null
);

/// <summary>Identifica una solicitud y su posición.</summary>
public sealed record RouteRequestItem(short Order, long RequestId);

/// <summary>Solicita crear una hoja asignada directamente o publicarla como disponible.</summary>
public sealed record CreateRouteRequest(
    DateOnly RouteDate,
    string RouteType,
    string AssignmentType,
    long? TransporterId,
    long? VehicleId,
    string Observation,
    IReadOnlyList<RouteRequestItem> Requests
);

/// <summary>Solicita asignar una oferta desde la oficina.</summary>
public sealed record AssignRouteRequest(long TransporterId, long VehicleId, string RowVersion);

/// <summary>Solicita tomar una oferta con un vehículo habilitado para el transportista autenticado.</summary>
public sealed record ClaimRouteOfferRequest(long VehicleId, string RowVersion);

/// <summary>Solicita cambiar la agenda descriptiva de una hoja todavía ofrecida.</summary>
public sealed record UpdateRouteOfferRequest(
    DateOnly RouteDate,
    string RouteType,
    string Observation,
    string RowVersion
);

/// <summary>Solicita crear o modificar un perfil transportista.</summary>
public sealed record SaveTransporterRequest(
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
    string? RowVersion
);

/// <summary>Solicita crear o modificar un vehículo logístico.</summary>
public sealed record SaveLogisticsVehicleRequest(
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
    string? RowVersion
);

/// <summary>Solicita una transición con concurrencia optimista.</summary>
public sealed record LogisticsTransitionRequest(string RowVersion, string? Observation);

/// <summary>Solicita registrar una incidencia durante una parada activa.</summary>
public sealed record ReportRouteStopIncidentRequest(
    string Type,
    string Observation,
    string RowVersion
);

/// <summary>Solicita confirmar una parada.</summary>
public sealed record CompleteRouteStopRequest(
    string Result,
    string Observation,
    string RowVersion,
    IReadOnlyList<long>? CompletedRequestAssetIds = null
);

/// <summary>Devuelve el identificador afectado.</summary>
public sealed record SavedLogisticsResponse(long Id);
