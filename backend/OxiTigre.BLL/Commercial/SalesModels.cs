/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Commercial.SalesModels
Archivo: SalesModels.cs | Versión: 1.3.0 | Fecha: 2026-08-28 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Define listas, promociones, pedidos, reservas y ventas internas del módulo Comercial.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-21 | FABRICA | Agustin Omar Cauzi | Promociones configurables y trazabilidad en pedidos y ventas.
Historial: 1.2.0 | 2026-08-28 | FABRICA | Agustin Omar Cauzi | Servicios sin stock y vínculos trazables de activos.
Historial: 1.3.0 | 2026-08-28 | FABRICA | Agustin Omar Cauzi | Modalidades logísticas de ingreso, regreso y devolución prevista.
===============================================================================
*/
namespace OxiTigre.BLL.Commercial;

/// <summary>Reúne catálogos, pedidos y ventas visibles para la empresa autenticada.</summary>
public sealed record SalesSnapshot(IReadOnlyList<SalesClient> Clients, IReadOnlyList<PriceList> PriceLists,
    IReadOnlyList<PriceListProduct> Prices, IReadOnlyList<SalesProduct> Products,
    IReadOnlyList<SalesWarehouse> Warehouses, IReadOnlyList<Promotion> Promotions,
    IReadOnlyList<OrderLine> Orders, IReadOnlyList<SaleLine> Sales,
    IReadOnlyList<SalesAsset> Assets, IReadOnlyList<OrderAsset> OrderAssets);
/// <summary>Cliente seleccionable para pedidos.</summary>
public sealed record SalesClient(long ClientId, string Code, string Name);
/// <summary>Lista de precios comercial.</summary>
public sealed record PriceList(long PriceListId, string Code, string Name, string Currency, DateOnly? ValidFrom,
    DateOnly? ValidUntil, string StatusCode, byte[] RowVersion);
/// <summary>Precio de un producto dentro de una lista.</summary>
public sealed record PriceListProduct(long PriceListProductId, long PriceListId, long ProductId, string ProductCode,
    string Product, decimal UnitPrice, string StatusCode, byte[] RowVersion);
/// <summary>Producto seleccionable para pedidos.</summary>
public sealed record SalesProduct(long ProductId, string Code, string Name, string UnitSymbol, string ItemType);
/// <summary>Depósito seleccionable para pedidos.</summary>
public sealed record SalesWarehouse(long WarehouseId, string Code, string Name);
/// <summary>Regla promocional vigente o histórica asociada a un producto.</summary>
public sealed record Promotion(long PromotionId, long ProductId, string ProductCode, string Product,
    string Code, string Name, string? Description, string PromotionType, decimal RequiredQuantity,
    decimal? PaidQuantity, decimal? DiscountedQuantity, decimal? DiscountRate, decimal? PackagePrice,
    DateOnly? ValidFrom, DateOnly? ValidUntil, string StatusCode, byte[] RowVersion);
/// <summary>Renglón de pedido con su encabezado repetido para una grilla simple y trazable.</summary>
public sealed record OrderLine(long OrderId, long ClientId, long? PriceListId, string OrderCode, DateTime OrderDateUtc,
    string Client, string Currency, decimal Subtotal, decimal TotalDiscount, decimal TotalTax, decimal Total,
    string? Observation, string StatusCode, byte[] RowVersion, long OrderLineId, long ProductId, string ProductCode,
    string Product, long? WarehouseId, string? Warehouse, decimal Quantity, decimal UnitPrice, decimal DiscountRate,
    decimal TaxRate, decimal LineSubtotal, decimal DiscountAmount, decimal TaxAmount, decimal LineTotal, string UnitSymbol,
    long? PromotionId, string? PromotionCode, string? PromotionName);
/// <summary>Renglón inmutable de una venta interna.</summary>
public sealed record SaleLine(long SaleId, long OrderId, long ClientId, long? MovementId, string SaleCode,
    DateTime SaleDateUtc, string Client, string Currency, decimal Subtotal, decimal TotalDiscount, decimal TotalTax,
    decimal Total, string? Observation, string StatusCode, long SaleLineId, long ProductId, string ProductCode,
    string Product, long? WarehouseId, string? Warehouse, decimal Quantity, decimal UnitPrice, decimal DiscountRate,
    decimal DiscountAmount, decimal TaxRate, decimal LineTotal, string UnitSymbol, long? PromotionId,
    string? PromotionCode, string? PromotionName);
/// <summary>Activo seleccionable para vincular al trabajo, venta, préstamo o intercambio de un pedido.</summary>
public sealed record SalesAsset(long AssetId, long? ClientOwnerId, string Code, string SerialNumber,
    string Product, string Owner, string ConditionCode, string StatusCode);
/// <summary>Vínculo trazable entre un pedido, un renglón cobrable y un activo concreto.</summary>
public sealed record OrderAsset(long OrderAssetId, long OrderId, long AssetId, long ProductLineId,
    string AssetCode, string SerialNumber, string ProductLine, string LinkType, string ReturnMode,
    string Observation, string InboundMode, DateOnly? ExpectedReturnDate);

/// <summary>Datos editables de una lista de precios.</summary>
public sealed record PriceListChange(long? PriceListId, string Code, string Name, string Currency, DateOnly? ValidFrom,
    DateOnly? ValidUntil, string StatusCode, byte[]? RowVersion);
/// <summary>Datos editables de un precio.</summary>
public sealed record PriceListProductChange(long? PriceListProductId, long PriceListId, long ProductId,
    decimal UnitPrice, string StatusCode, byte[]? RowVersion);
/// <summary>Datos editables de una promoción.</summary>
public sealed record PromotionChange(long? PromotionId, long ProductId, string Code, string Name,
    string? Description, string PromotionType, decimal RequiredQuantity, decimal? PaidQuantity,
    decimal? DiscountedQuantity, decimal? DiscountRate, decimal? PackagePrice, DateOnly? ValidFrom,
    DateOnly? ValidUntil, string StatusCode, byte[]? RowVersion);
/// <summary>Datos editables de un pedido.</summary>
public sealed record OrderChange(long? OrderId, long ClientId, long? PriceListId, DateTime OrderDateUtc,
    string Currency, string? Observation, IReadOnlyList<OrderDetailChange> Details, byte[]? RowVersion,
    IReadOnlyList<OrderAssetChange>? Assets = null);
/// <summary>Renglón valorizado recibido para un pedido.</summary>
public sealed record OrderDetailChange(long ProductId, long? WarehouseId, decimal Quantity, decimal UnitPrice,
    decimal DiscountRate, decimal TaxRate, long? PromotionId = null);
/// <summary>Activo y propósito que deben quedar asociados al pedido.</summary>
public sealed record OrderAssetChange(long AssetId, long ProductLineId, string LinkType, string ReturnMode,
    string Observation, string InboundMode = "NO_APLICA", DateOnly? ExpectedReturnDate = null);

/// <summary>Error funcional devuelto de forma controlada por Comercial.</summary>
public sealed class CommercialOperationException(long errorCode, string message) : Exception(message)
{
    /// <summary>Código estable registrado en el catálogo.</summary>
    public long ErrorCode { get; } = errorCode;
}
