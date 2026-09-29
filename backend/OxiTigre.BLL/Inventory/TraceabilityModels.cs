/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Inventory.TraceabilityModels
Archivo: TraceabilityModels.cs | Versión: 1.0.0 | Fecha: 2026-08-24 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Define el estado actual almacenado y los documentos inmutables de trazabilidad industrial.
Historial: 1.0.0 | 2026-08-24 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
namespace OxiTigre.BLL.Inventory;

/// <summary>Reúne lotes, activos y documentos de trazabilidad de la empresa autenticada.</summary>
public sealed record TraceabilitySnapshot(IReadOnlyList<TraceLot> Lots, IReadOnlyList<TraceAsset> Assets,
    IReadOnlyList<AssetMeasurement> Measurements, IReadOnlyList<TransformationLine> Transformations,
    IReadOnlyList<IndustrialIncident> Incidents, IReadOnlyList<AssetMaintenance> Maintenances,
    IReadOnlyList<AssetLoanLine> Loans, IReadOnlyList<AssetEvent> Events,
    IReadOnlyList<TraceDestination> Branches, IReadOnlyList<TraceDestination> Clients);

/// <summary>Expone un destino seleccionable sin revelar datos internos adicionales.</summary>
public sealed record TraceDestination(long Id, string Code, string Name);

/// <summary>Expone un saldo por lote y posición.</summary>
public sealed record TraceLot(long LotId, long ProductId, string Code, string? SupplierLotCode, string Product,
    long WarehouseId, string Warehouse, decimal Quantity, DateOnly? ExpirationDate, string StatusCode);
/// <summary>Expone la información actual confirmada de un activo individual.</summary>
public sealed record TraceAsset(long AssetId, long ProductId, long? OwnerClientId, string Code, string SerialNumber, string AssetType,
    string Product, long? WarehouseId, string? Warehouse, long? LocationId, string? Location,
    string Owner, string ConditionCode, string StatusCode,
    decimal? Capacity, string? CapacityUnit, long? ContentProductId, string? ContentProduct, long? ContentLotId,
    string? ContentLot, decimal? ContentQuantity, string? ContentUnit, byte[] RowVersion);
/// <summary>Expone una medición append-only.</summary>
public sealed record AssetMeasurement(long MeasurementId, long AssetId, string Asset, DateTime MeasurementDateUtc,
    string MeasurementType, decimal Value, string Unit, string Method, string Source, bool IsEstimated,
    string? DeviceReference, string? Observation);
/// <summary>Expone un destino y la merma de un fraccionamiento confirmado.</summary>
public sealed record TransformationLine(long TransformationId, string Code, DateTime TransformationDateUtc,
    long SourceLotId, string SourceLot, long? SourceAssetId, string? SourceAsset, string Product,
    decimal SourceQuantity, decimal LossQuantity, string Method, string? LossReason,
    long DestinationAssetId, string DestinationAsset, decimal LoadedQuantity);
/// <summary>Expone un incidente industrial confirmado.</summary>
public sealed record IndustrialIncident(long IncidentId, string Code, DateTime IncidentDateUtc, string IncidentType,
    string Product, string Warehouse, long? AssetId, string? Asset, decimal LossQuantity, bool IsEstimated,
    string Cause, string? ActionTaken, string StatusCode);
/// <summary>Expone una orden de mantenimiento de un activo.</summary>
public sealed record AssetMaintenance(long MaintenanceId, string Code, long AssetId, string Asset,
    string MaintenanceType, DateTime StartDateUtc, DateTime? EndDateUtc, string WorkDescription,
    string? Result, decimal? Cost, DateOnly? NextReviewDate, string StatusCode, byte[] RowVersion);
/// <summary>Expone un activo prestado y su compromiso de devolución.</summary>
public sealed record AssetLoanLine(long LoanId, string Code, string Destination, string DeliveryMode,
    DateTime DepartureDateUtc, DateTime? ExpectedReturnDateUtc, DateTime? ActualReturnDateUtc,
    string StatusCode, byte[] RowVersion, long LoanLineId, long AssetId, string Asset,
    decimal? DepartureQuantity, string DepartureCondition, decimal? ReturnQuantity, string? ReturnCondition);
/// <summary>Expone una transición inmutable del activo con usuario y correlación.</summary>
public sealed record AssetEvent(long EventId, long AssetId, string Asset, string EventType, DateTime EventDateUtc,
    string? StatusBefore, string StatusAfter, string? ConditionBefore, string ConditionAfter,
    decimal? QuantityBefore, decimal? QuantityAfter, string? Observation, Guid CorrelationId, long UserId);

/// <summary>Datos validados para registrar un activo propiedad de un cliente.</summary>
public sealed record ClientAssetChange(long ClientId, long ProductId, long? WarehouseId,
    string? SerialNumber, string AssetType, decimal? Capacity, string? CapacityUnit,
    string ConditionCode, bool IsInCustody, string Observation);
/// <summary>Datos validados para confirmar un cambio de custodia.</summary>
public sealed record ClientAssetCustodyChange(long AssetId, string Action, long? WarehouseId,
    string Observation, byte[] RowVersion);

/// <summary>Datos para registrar una medición sin alterar stock.</summary>
public sealed record AssetMeasurementChange(long AssetId, DateTime MeasurementDateUtc, string MeasurementType,
    decimal Value, string Unit, string Method, string Source, bool IsEstimated, string? DeviceReference,
    string? Observation);
/// <summary>Datos para confirmar el reparto de contenido de un origen a destinos.</summary>
public sealed record TransformationChange(long SourceLotId, long? SourceAssetId, long ContentProductId,
    DateTime TransformationDateUtc, decimal SourceQuantity, decimal LossQuantity, string Method,
    string? LossReason, string? Observation, IReadOnlyList<TransformationDestinationChange> Destinations);
/// <summary>Destino de un fraccionamiento.</summary>
public sealed record TransformationDestinationChange(long AssetId, long LotId, decimal Quantity);
/// <summary>Datos para confirmar una pérdida, rotura o desvío.</summary>
public sealed record IncidentChange(long ProductId, long WarehouseId, long? AssetId, long? LotId,
    string IncidentType, DateTime IncidentDateUtc, decimal? QuantityBefore, decimal LossQuantity,
    decimal? QuantityAfter, bool IsEstimated, string? MeasurementMethod, string Cause, string? ActionTaken,
    string? EvidenceReference);
/// <summary>Datos para iniciar una orden de mantenimiento.</summary>
public sealed record MaintenanceChange(long AssetId, long? IncidentId, long? SupplierId, string MaintenanceType,
    DateTime StartDateUtc, string WorkDescription, decimal? Cost, DateOnly? NextReviewDate);
/// <summary>Datos para completar una orden de mantenimiento.</summary>
public sealed record MaintenanceCompletion(long MaintenanceId, DateTime EndDateUtc, string Result,
    string? OldComponent, string? NewComponent, decimal? Cost, string? CertificateReference,
    DateOnly? NextReviewDate, byte[] RowVersion);
/// <summary>Datos para prestar uno o más activos con snapshot de salida.</summary>
public sealed record LoanChange(string DestinationType, long? DestinationBranchId, long? DestinationClientId,
    string? ExternalDestination, string DeliveryMode, DateTime DepartureDateUtc, DateTime? ExpectedReturnDateUtc,
    string? Observation, string? DestinationCompanyCode, Guid? IntercompanyCorrelationId,
    IReadOnlyList<LoanAssetChange> Assets);
/// <summary>Snapshot de un activo prestado.</summary>
public sealed record LoanAssetChange(long AssetId, long? LotId, decimal? Quantity, string ConditionCode);
/// <summary>Datos de devolución de un préstamo.</summary>
public sealed record LoanReturnChange(long LoanId, DateTime ReturnDateUtc, string? Observation,
    IReadOnlyList<LoanAssetReturnChange> Assets, byte[] RowVersion);
/// <summary>Snapshot de devolución de un activo.</summary>
public sealed record LoanAssetReturnChange(long LoanLineId, decimal? Quantity, string ConditionCode,
    string? Observation);
