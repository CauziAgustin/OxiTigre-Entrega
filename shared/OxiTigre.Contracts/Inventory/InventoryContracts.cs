/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.Contracts.Inventory.InventoryContracts
Archivo: InventoryContracts.cs | Versión: 1.2.0 | Fecha: 2026-08-24 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Define contratos HTTP de Inventario sin aceptar contexto seguro desde el cliente.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-21 | FABRICA | Agustin Omar Cauzi | Exposición de cantidades reservadas y disponibles.
Historial: 1.2.0 | 2026-08-24 | FABRICA | Agustin Omar Cauzi | Contrato de configuración de trazabilidad por producto.
===============================================================================
*/
namespace OxiTigre.Contracts.Inventory;

/// <summary>Reúne maestros, saldos, alertas y movimientos de la empresa autenticada.</summary>
public sealed record InventorySnapshotResponse(IReadOnlyList<MeasurementUnitResponse> MeasurementUnits,
    IReadOnlyList<ProductCategoryResponse> Categories, IReadOnlyList<ProductResponse> Products,
    IReadOnlyList<WarehouseResponse> Warehouses, IReadOnlyList<WarehouseLocationResponse> Locations,
    IReadOnlyList<StockBalanceResponse> Stock, IReadOnlyList<InventoryMovementLineResponse> Movements);
/// <summary>Expone una unidad de medida.</summary>
public sealed record MeasurementUnitResponse(long MeasurementUnitId, string Code, string Name, string Symbol,
    bool AllowsDecimals, string StatusCode, string RowVersion);
/// <summary>Expone una categoría.</summary>
public sealed record ProductCategoryResponse(long ProductCategoryId, string Code, string Name, string? Description,
    string StatusCode, string RowVersion);
/// <summary>Expone un producto.</summary>
public sealed record ProductResponse(long ProductId, long ProductCategoryId, long MeasurementUnitId, string Code,
    string Name, string? Description, string? Barcode, string Category, string UnitSymbol, string StatusCode,
    string RowVersion, string ItemType, string TrackingType, bool IsReusable, bool AllowsLoans, bool RequiresMaintenance,
    bool AllowsMeasurements);
/// <summary>Expone un depósito.</summary>
public sealed record WarehouseResponse(long WarehouseId, long? BranchId, string Code, string Name, string? Address,
    string? Description, string StatusCode, string RowVersion);
/// <summary>Expone una ubicación.</summary>
public sealed record WarehouseLocationResponse(long LocationId, long WarehouseId, string Code, string Name,
    string? Description, string StatusCode, string RowVersion);
/// <summary>Expone saldo, mínimo y alerta.</summary>
public sealed record StockBalanceResponse(long StockBalanceId, long ProductId, long WarehouseId, string ProductCode,
    string Product, string Warehouse, string UnitSymbol, decimal Quantity, decimal ReservedQuantity,
    decimal AvailableQuantity, decimal MinimumStock,
    bool IsBelowMinimum, string RowVersion);
/// <summary>Expone un renglón trazable de movimiento.</summary>
public sealed record InventoryMovementLineResponse(long MovementId, string MovementCode, string MovementType,
    DateTime MovementDateUtc, string? Observation, string StatusCode, long MovementLineId, long ProductId,
    string ProductCode, string Product, long? OriginWarehouseId, string? OriginWarehouse, long? OriginLocationId,
    string? OriginLocation, long? DestinationWarehouseId, string? DestinationWarehouse,
    long? DestinationLocationId, string? DestinationLocation, decimal Quantity, string UnitSymbol);

/// <summary>Solicita crear o editar una unidad de medida.</summary>
public sealed record SaveMeasurementUnitRequest(long? MeasurementUnitId, string Code, string Name, string Symbol,
    bool AllowsDecimals, string StatusCode, string? RowVersion);
/// <summary>Solicita crear o editar una categoría.</summary>
public sealed record SaveProductCategoryRequest(long? ProductCategoryId, string Code, string Name, string? Description,
    string StatusCode, string? RowVersion);
/// <summary>Solicita crear o editar un producto.</summary>
public sealed record SaveProductRequest(long? ProductId, long ProductCategoryId, long MeasurementUnitId, string Name,
    string? Description, string? Barcode, string StatusCode, string? RowVersion,
    string ItemType = "PRODUCTO", string TrackingType = "NINGUNA", bool IsReusable = false, bool AllowsLoans = false,
    bool RequiresMaintenance = false, bool AllowsMeasurements = false);
/// <summary>Solicita crear o editar un depósito.</summary>
public sealed record SaveWarehouseRequest(long? WarehouseId, long? BranchId, string Code, string Name, string? Address,
    string? Description, string StatusCode, string? RowVersion);
/// <summary>Solicita crear o editar una ubicación.</summary>
public sealed record SaveWarehouseLocationRequest(long? LocationId, long WarehouseId, string Code, string Name,
    string? Description, string StatusCode, string? RowVersion);
/// <summary>Solicita cambiar el mínimo de alerta.</summary>
public sealed record UpdateMinimumStockRequest(decimal MinimumStock, string RowVersion);
/// <summary>Solicita confirmar un movimiento.</summary>
public sealed record CreateInventoryMovementRequest(string MovementType, DateTime MovementDateUtc, string? Observation,
    IReadOnlyList<InventoryMovementDetailRequest> Details);
/// <summary>Solicita mover una cantidad de producto entre origen y destino.</summary>
public sealed record InventoryMovementDetailRequest(long ProductId, long? OriginWarehouseId, long? OriginLocationId,
    long? DestinationWarehouseId, long? DestinationLocationId, decimal Quantity);
/// <summary>Devuelve el identificador creado o actualizado.</summary>
public sealed record SavedInventoryResponse(long Id);
