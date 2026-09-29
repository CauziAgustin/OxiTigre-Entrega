/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Inventory.InventoryModels
Archivo: InventoryModels.cs | Versión: 1.2.0 | Fecha: 2026-08-24 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Define maestros, saldos, movimientos y cambios del módulo Inventario.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-21 | FABRICA | Agustin Omar Cauzi | Separación de stock físico, reservado y disponible.
Historial: 1.2.0 | 2026-08-24 | FABRICA | Agustin Omar Cauzi | Clasificación transversal de trazabilidad por producto.
===============================================================================
*/
namespace OxiTigre.BLL.Inventory;

/// <summary>Reúne todos los datos necesarios para operar la pantalla de Inventario.</summary>
public sealed record InventorySnapshot(IReadOnlyList<MeasurementUnit> MeasurementUnits,
    IReadOnlyList<ProductCategory> Categories, IReadOnlyList<Product> Products,
    IReadOnlyList<Warehouse> Warehouses, IReadOnlyList<WarehouseLocation> Locations,
    IReadOnlyList<StockBalance> Stock, IReadOnlyList<InventoryMovementLine> Movements);

/// <summary>Representa una unidad de medida administrable.</summary>
public sealed record MeasurementUnit(long MeasurementUnitId, string Code, string Name, string Symbol,
    bool AllowsDecimals, string StatusCode, byte[] RowVersion);
/// <summary>Representa una categoría de productos.</summary>
public sealed record ProductCategory(long ProductCategoryId, string Code, string Name, string? Description,
    string StatusCode, byte[] RowVersion);
/// <summary>Representa un producto de la empresa.</summary>
public sealed record Product(long ProductId, long ProductCategoryId, long MeasurementUnitId, string Code,
    string Name, string? Description, string? Barcode, string Category, string UnitSymbol, string StatusCode,
    byte[] RowVersion, string ItemType, string TrackingType, bool IsReusable, bool AllowsLoans, bool RequiresMaintenance,
    bool AllowsMeasurements);
/// <summary>Representa un depósito de la empresa.</summary>
public sealed record Warehouse(long WarehouseId, long? BranchId, string Code, string Name, string? Address,
    string? Description, string StatusCode, byte[] RowVersion);
/// <summary>Representa una ubicación interna de un depósito.</summary>
public sealed record WarehouseLocation(long LocationId, long WarehouseId, string Code, string Name,
    string? Description, string StatusCode, byte[] RowVersion);
/// <summary>Representa el saldo y mínimo de un producto en un depósito.</summary>
public sealed record StockBalance(long StockBalanceId, long ProductId, long WarehouseId, string ProductCode,
    string Product, string Warehouse, string UnitSymbol, decimal Quantity, decimal ReservedQuantity,
    decimal AvailableQuantity, decimal MinimumStock,
    bool IsBelowMinimum, byte[] RowVersion);
/// <summary>Representa un renglón trazable de un movimiento confirmado.</summary>
public sealed record InventoryMovementLine(long MovementId, string MovementCode, string MovementType,
    DateTime MovementDateUtc, string? Observation, string StatusCode, long MovementLineId, long ProductId,
    string ProductCode, string Product, long? OriginWarehouseId, string? OriginWarehouse, long? OriginLocationId,
    string? OriginLocation, long? DestinationWarehouseId, string? DestinationWarehouse,
    long? DestinationLocationId, string? DestinationLocation, decimal Quantity, string UnitSymbol);

/// <summary>Datos para crear o editar una unidad de medida.</summary>
public sealed record MeasurementUnitChange(long? MeasurementUnitId, string Code, string Name, string Symbol,
    bool AllowsDecimals, string StatusCode, byte[]? RowVersion);
/// <summary>Datos para crear o editar una categoría.</summary>
public sealed record ProductCategoryChange(long? ProductCategoryId, string Code, string Name, string? Description,
    string StatusCode, byte[]? RowVersion);
/// <summary>Datos para crear o editar un producto.</summary>
public sealed record ProductChange(long? ProductId, long ProductCategoryId, long MeasurementUnitId, string Name,
    string? Description, string? Barcode, string StatusCode, byte[]? RowVersion,
    string ItemType = "PRODUCTO", string TrackingType = "NINGUNA", bool IsReusable = false, bool AllowsLoans = false,
    bool RequiresMaintenance = false, bool AllowsMeasurements = false);
/// <summary>Datos para crear o editar un depósito.</summary>
public sealed record WarehouseChange(long? WarehouseId, long? BranchId, string Code, string Name, string? Address,
    string? Description, string StatusCode, byte[]? RowVersion);
/// <summary>Datos para crear o editar una ubicación.</summary>
public sealed record WarehouseLocationChange(long? LocationId, long WarehouseId, string Code, string Name,
    string? Description, string StatusCode, byte[]? RowVersion);
/// <summary>Datos para cambiar un mínimo sin alterar el saldo.</summary>
public sealed record MinimumStockChange(long StockBalanceId, decimal MinimumStock, byte[] RowVersion);
/// <summary>Datos para confirmar un movimiento completo.</summary>
public sealed record InventoryMovementChange(string MovementType, DateTime MovementDateUtc, string? Observation,
    IReadOnlyList<InventoryMovementDetailChange> Details);
/// <summary>Datos de producto, cantidad, origen y destino de un movimiento.</summary>
public sealed record InventoryMovementDetailChange(long ProductId, long? OriginWarehouseId, long? OriginLocationId,
    long? DestinationWarehouseId, long? DestinationLocationId, decimal Quantity);

/// <summary>Informa un resultado funcional controlado producido por Inventario.</summary>
public sealed class InventoryOperationException(long errorCode, string message) : Exception(message)
{
    /// <summary>Obtiene el código funcional registrado.</summary>
    public long ErrorCode { get; } = errorCode;
}
