/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.Contracts.Commercial.SalesContracts
Archivo: SalesContracts.cs | Versión: 1.3.0 | Fecha: 2026-08-28 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Define contratos HTTP del flujo de listas, promociones, pedidos y ventas internas.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-21 | FABRICA | Agustin Omar Cauzi | Promociones configurables y trazabilidad del descuento aplicado.
Historial: 1.2.0 | 2026-08-28 | FABRICA | Agustin Omar Cauzi | Servicios y activos concretos asociados al pedido.
Historial: 1.3.0 | 2026-08-28 | FABRICA | Agustin Omar Cauzi | Modalidades logísticas de ingreso, regreso y devolución prevista.
===============================================================================
*/
namespace OxiTigre.Contracts.Commercial;

/// <summary>Respuesta completa de la pantalla de Pedidos y Ventas.</summary>
public sealed record SalesSnapshotResponse(IReadOnlyList<SalesClientResponse> Clients, IReadOnlyList<PriceListResponse> PriceLists,
    IReadOnlyList<PriceListProductResponse> Prices, IReadOnlyList<SalesProductResponse> Products,
    IReadOnlyList<SalesWarehouseResponse> Warehouses, IReadOnlyList<PromotionResponse> Promotions,
    IReadOnlyList<OrderLineResponse> Orders, IReadOnlyList<SaleLineResponse> Sales,
    IReadOnlyList<SalesAssetResponse> Assets, IReadOnlyList<OrderAssetResponse> OrderAssets);
/// <summary>Cliente seleccionable.</summary>
public sealed record SalesClientResponse(long ClientId, string Code, string Name);
/// <summary>Lista de precios visible.</summary>
public sealed record PriceListResponse(long PriceListId, string Code, string Name, string Currency, DateOnly? ValidFrom, DateOnly? ValidUntil, string StatusCode, string RowVersion);
/// <summary>Precio de producto visible.</summary>
public sealed record PriceListProductResponse(long PriceListProductId, long PriceListId, long ProductId, string ProductCode, string Product, decimal UnitPrice, string StatusCode, string RowVersion);
/// <summary>Producto seleccionable.</summary>
public sealed record SalesProductResponse(long ProductId, string Code, string Name, string UnitSymbol, string ItemType);
/// <summary>Depósito seleccionable.</summary>
public sealed record SalesWarehouseResponse(long WarehouseId, string Code, string Name);
/// <summary>Promoción configurable para un producto durante una vigencia.</summary>
public sealed record PromotionResponse(long PromotionId, long ProductId, string ProductCode, string Product,
    string Code, string Name, string? Description, string PromotionType, decimal RequiredQuantity,
    decimal? PaidQuantity, decimal? DiscountedQuantity, decimal? DiscountRate, decimal? PackagePrice,
    DateOnly? ValidFrom, DateOnly? ValidUntil, string StatusCode, string RowVersion);
/// <summary>Renglón y encabezado de pedido.</summary>
public sealed record OrderLineResponse(long OrderId, long ClientId, long? PriceListId, string OrderCode, DateTime OrderDateUtc, string Client, string Currency,
    decimal Subtotal, decimal TotalDiscount, decimal TotalTax, decimal Total, string? Observation, string StatusCode, string RowVersion,
    long OrderLineId, long ProductId, string ProductCode, string Product, long? WarehouseId, string? Warehouse, decimal Quantity,
    decimal UnitPrice, decimal DiscountRate, decimal TaxRate, decimal LineSubtotal, decimal DiscountAmount, decimal TaxAmount,
    decimal LineTotal, string UnitSymbol, long? PromotionId, string? PromotionCode, string? PromotionName);
/// <summary>Renglón y encabezado de venta interna.</summary>
public sealed record SaleLineResponse(long SaleId, long OrderId, long ClientId, long? MovementId, string SaleCode, DateTime SaleDateUtc, string Client,
    string Currency, decimal Subtotal, decimal TotalDiscount, decimal TotalTax, decimal Total, string? Observation, string StatusCode,
    long SaleLineId, long ProductId, string ProductCode, string Product, long? WarehouseId, string? Warehouse, decimal Quantity,
    decimal UnitPrice, decimal DiscountRate, decimal DiscountAmount, decimal TaxRate, decimal LineTotal, string UnitSymbol,
    long? PromotionId, string? PromotionCode, string? PromotionName);
/// <summary>Activo disponible para asociar a un pedido.</summary>
public sealed record SalesAssetResponse(long AssetId, long? ClientOwnerId, string Code, string SerialNumber,
    string Product, string Owner, string ConditionCode, string StatusCode);
/// <summary>Activo concreto y finalidad registrada en un pedido.</summary>
public sealed record OrderAssetResponse(long OrderAssetId, long OrderId, long AssetId, long ProductLineId,
    string AssetCode, string SerialNumber, string ProductLine, string LinkType, string ReturnMode,
    string Observation, string InboundMode, DateOnly? ExpectedReturnDate);

/// <summary>Solicita crear o actualizar una lista.</summary>
public sealed record SavePriceListRequest(long? PriceListId, string Code, string Name, string Currency, DateOnly? ValidFrom, DateOnly? ValidUntil, string StatusCode, string? RowVersion);
/// <summary>Solicita crear o actualizar un precio.</summary>
public sealed record SavePriceListProductRequest(long? PriceListProductId, long PriceListId, long ProductId, decimal UnitPrice, string StatusCode, string? RowVersion);
/// <summary>Solicita crear o actualizar una promoción.</summary>
public sealed record SavePromotionRequest(long? PromotionId, long ProductId, string Code, string Name,
    string? Description, string PromotionType, decimal RequiredQuantity, decimal? PaidQuantity,
    decimal? DiscountedQuantity, decimal? DiscountRate, decimal? PackagePrice, DateOnly? ValidFrom,
    DateOnly? ValidUntil, string StatusCode, string? RowVersion);
/// <summary>Solicita crear o actualizar un pedido borrador.</summary>
public sealed record SaveOrderRequest(long? OrderId, long ClientId, long? PriceListId, DateTime OrderDateUtc, string Currency, string? Observation, IReadOnlyList<SaveOrderDetailRequest> Details, string? RowVersion,
    IReadOnlyList<SaveOrderAssetRequest>? Assets = null);
/// <summary>Renglón recibido para un pedido.</summary>
public sealed record SaveOrderDetailRequest(long ProductId, long? WarehouseId, decimal Quantity, decimal UnitPrice,
    decimal DiscountRate, decimal TaxRate, long? PromotionId = null);
/// <summary>Solicita asociar un activo concreto a un renglón del pedido.</summary>
public sealed record SaveOrderAssetRequest(long AssetId, long ProductLineId, string LinkType, string ReturnMode,
    string Observation, string InboundMode = "NO_APLICA", DateOnly? ExpectedReturnDate = null);
/// <summary>Solicita una transición protegida por versión.</summary>
public sealed record OrderTransitionRequest(string RowVersion);
/// <summary>Solicita vender un pedido confirmado.</summary>
public sealed record CreateSaleRequest(DateTime SaleDateUtc, string RowVersion);
/// <summary>Devuelve el identificador creado o actualizado.</summary>
public sealed record SavedCommercialResponse(long Id);
