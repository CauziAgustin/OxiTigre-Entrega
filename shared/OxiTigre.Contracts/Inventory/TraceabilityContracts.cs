/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.Contracts.Inventory.TraceabilityContracts
Archivo: TraceabilityContracts.cs | Versión: 1.0.0 | Fecha: 2026-08-24 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Define contratos HTTP de trazabilidad industrial.
Historial: 1.0.0 | 2026-08-24 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
namespace OxiTigre.Contracts.Inventory;

/// <summary>Reúne el estado actual almacenado y los documentos de trazabilidad.</summary>
public sealed record TraceabilitySnapshotResponse(IReadOnlyList<TraceLotResponse> Lots,
    IReadOnlyList<TraceAssetResponse> Assets, IReadOnlyList<AssetMeasurementResponse> Measurements,
    IReadOnlyList<TransformationLineResponse> Transformations, IReadOnlyList<IndustrialIncidentResponse> Incidents,
    IReadOnlyList<AssetMaintenanceResponse> Maintenances, IReadOnlyList<AssetLoanLineResponse> Loans,
    IReadOnlyList<AssetEventResponse> Events, IReadOnlyList<TraceDestinationResponse> Branches,
    IReadOnlyList<TraceDestinationResponse> Clients);
/// <summary>Expone una sucursal o cliente seleccionable como destino.</summary>
public sealed record TraceDestinationResponse(long Id, string Code, string Name);
/// <summary>Saldo trazado por lote.</summary>
public sealed record TraceLotResponse(long LotId, long ProductId, string Code, string? SupplierLotCode,
    string Product, long WarehouseId,
    string Warehouse, decimal Quantity, DateOnly? ExpirationDate, string StatusCode);
/// <summary>Activo serializado con su contenido actual almacenado.</summary>
public sealed record TraceAssetResponse(long AssetId, long ProductId, long? OwnerClientId, string Code, string SerialNumber,
    string Product, string AssetType, string Owner, string Condition, long? WarehouseId, string? Warehouse,
    long? LocationId, string? Location,
    string StatusCode, decimal? Capacity, string? CapacityUnit, long? ContentProductId, string? ContentProduct,
    long? ContentLotId, string? ContentLot, decimal? ContentQuantity, string? ContentUnit, string RowVersion);
/// <summary>Medición histórica de un activo.</summary>
public sealed record AssetMeasurementResponse(long MeasurementId, long AssetId, string Asset,
    DateTime MeasurementDateUtc, string MeasurementType, decimal Value, string Unit, string Method,
    string Source, bool IsEstimated, string? DeviceReference, string? Observation);
/// <summary>Destino histórico de un fraccionamiento.</summary>
public sealed record TransformationLineResponse(long TransformationId, string Code,
    DateTime TransformationDateUtc, long SourceLotId, string SourceLot, long? SourceAssetId,
    string? SourceAsset, string Product, decimal SourceQuantity, decimal LossQuantity, string Method,
    string? LossReason, long DestinationAssetId, string DestinationAsset, decimal LoadedQuantity);
/// <summary>Incidente industrial confirmado.</summary>
public sealed record IndustrialIncidentResponse(long IncidentId, string Code, DateTime IncidentDateUtc,
    string IncidentType, string Product, string Warehouse, long? AssetId, string? Asset,
    decimal LossQuantity, bool IsEstimated, string Cause, string? ActionTaken, string StatusCode);
/// <summary>Mantenimiento de un activo.</summary>
public sealed record AssetMaintenanceResponse(long MaintenanceId, string Code, long AssetId, string Asset,
    string MaintenanceType, DateTime StartDateUtc, DateTime? EndDateUtc, string WorkDescription,
    string? Result, decimal? Cost, DateOnly? NextReviewDate, string StatusCode, string RowVersion);
/// <summary>Renglón de préstamo con snapshots.</summary>
public sealed record AssetLoanLineResponse(long LoanId, string Code, string Destination, string DeliveryMode,
    DateTime DepartureDateUtc, DateTime? ExpectedReturnDateUtc, DateTime? ActualReturnDateUtc,
    string StatusCode, string RowVersion, long LoanLineId, long AssetId, string Asset,
    decimal? DepartureQuantity, string DepartureCondition, decimal? ReturnQuantity, string? ReturnCondition);
/// <summary>Evento inmutable de un activo.</summary>
public sealed record AssetEventResponse(long EventId, long AssetId, string Asset, string EventType,
    DateTime EventDateUtc, string? StatusBefore, string StatusAfter, string? ConditionBefore,
    string ConditionAfter, decimal? QuantityBefore, decimal? QuantityAfter, string? Observation,
    Guid CorrelationId, long UserId);

/// <summary>Solicita registrar un activo cuyo propietario es un cliente.</summary>
public sealed record SaveClientAssetRequest(long ClientId, long ProductId, long? WarehouseId,
    string? SerialNumber, string AssetType, decimal? Capacity, string? CapacityUnit,
    string ConditionCode, bool IsInCustody, string Observation);
/// <summary>Solicita confirmar el ingreso o la entrega de un activo propiedad de un cliente.</summary>
public sealed record ChangeClientAssetCustodyRequest(long AssetId, string Action, long? WarehouseId,
    string Observation, string RowVersion);

/// <summary>Solicita registrar una medición.</summary>
public sealed record CreateAssetMeasurementRequest(long AssetId, DateTime MeasurementDateUtc,
    string MeasurementType, decimal Value, string Unit, string Method, string Source, bool IsEstimated,
    string? DeviceReference, string? Observation);
/// <summary>Solicita confirmar un fraccionamiento.</summary>
public sealed record CreateTransformationRequest(long SourceLotId, long? SourceAssetId, long ContentProductId,
    DateTime TransformationDateUtc, decimal SourceQuantity, decimal LossQuantity, string Method,
    string? LossReason, string? Observation, IReadOnlyList<TransformationDestinationRequest> Destinations);
/// <summary>Destino de fraccionamiento.</summary>
public sealed record TransformationDestinationRequest(long AssetId, long LotId, decimal Quantity);
/// <summary>Solicita confirmar un incidente.</summary>
public sealed record CreateIncidentRequest(long ProductId, long WarehouseId, long? AssetId, long? LotId,
    string IncidentType, DateTime IncidentDateUtc, decimal? QuantityBefore, decimal LossQuantity,
    decimal? QuantityAfter, bool IsEstimated, string? MeasurementMethod, string Cause,
    string? ActionTaken, string? EvidenceReference);
/// <summary>Solicita iniciar mantenimiento.</summary>
public sealed record CreateMaintenanceRequest(long AssetId, long? IncidentId, long? SupplierId,
    string MaintenanceType, DateTime StartDateUtc, string WorkDescription, decimal? Cost);
/// <summary>Solicita completar mantenimiento.</summary>
public sealed record CompleteMaintenanceRequest(long MaintenanceId, DateTime EndDateUtc, string Result,
    string? OldComponent, string? NewComponent, decimal? Cost, string? CertificateReference,
    DateOnly? NextReviewDate, string RowVersion);
/// <summary>Solicita confirmar un préstamo.</summary>
public sealed record CreateLoanRequest(string DestinationType, long? DestinationBranchId,
    long? DestinationClientId, string? ExternalDestination, string DeliveryMode, DateTime DepartureDateUtc,
    DateTime? ExpectedReturnDateUtc, string? Observation, string? DestinationCompanyCode,
    Guid? IntercompanyCorrelationId, IReadOnlyList<LoanAssetRequest> Assets);
/// <summary>Snapshot de salida del activo.</summary>
public sealed record LoanAssetRequest(long AssetId, long? LotId, decimal? Quantity, string ConditionCode);
/// <summary>Solicita confirmar una devolución.</summary>
public sealed record ReturnLoanRequest(long LoanId, DateTime ReturnDateUtc, string? Observation,
    IReadOnlyList<LoanAssetReturnRequest> Assets, string RowVersion);
/// <summary>Snapshot de devolución del activo.</summary>
public sealed record LoanAssetReturnRequest(long LoanLineId, decimal? Quantity, string ConditionCode,
    string? Observation);
/// <summary>Devuelve el identificador creado.</summary>
public sealed record SavedTraceabilityResponse(long Id);
