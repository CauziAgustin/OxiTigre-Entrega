/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Purchasing.PurchasingModels
Archivo: PurchasingModels.cs | Versión: 1.1.0 | Fecha: 2026-08-26 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Define proveedores, órdenes y recepciones parciales trazables de Compras.
Historial: 1.0.0 | 2026-08-24 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-26 | FABRICA | Agustin Omar Cauzi | Snapshot del proveedor y metadatos de reversión.
===============================================================================
*/
namespace OxiTigre.BLL.Purchasing;

/// <summary>Reúne los datos necesarios para operar Compras en la empresa autenticada.</summary>
public sealed record PurchasingSnapshot(IReadOnlyList<Supplier> Suppliers, IReadOnlyList<SupplierContact> SupplierContacts,
    IReadOnlyList<PurchasingProduct> Products, IReadOnlyList<PurchasingWarehouse> Warehouses,
    IReadOnlyList<PurchaseOrderLine> Orders, IReadOnlyList<GoodsReceiptLine> Receipts);

/// <summary>Representa un proveedor conservado con baja lógica y concurrencia optimista.</summary>
public sealed record Supplier(long SupplierId, string Code, string LegalName, string? TradeName,
    string TaxId, string? Email, string? Phone, string? PaymentTerms, string? Observation,
    string StatusCode, byte[] RowVersion);

/// <summary>Representa una persona de contacto conservada dentro de un proveedor.</summary>
public sealed record SupplierContact(long SupplierContactId, long SupplierId, string Name, string? Position,
    string? Email, string? Phone, bool IsPrimary, string StatusCode, byte[] RowVersion);

/// <summary>Representa un producto seleccionable en una orden de compra.</summary>
public sealed record PurchasingProduct(long ProductId, string Code, string Name, string UnitSymbol,
    string TrackingType, bool IsReusable, bool AllowsLoans, bool RequiresMaintenance, bool AllowsMeasurements);

/// <summary>Representa un depósito de destino seleccionable.</summary>
public sealed record PurchasingWarehouse(long WarehouseId, string Code, string Name);

/// <summary>Expone un renglón de orden con cantidades pedida, recibida y pendiente.</summary>
public sealed record PurchaseOrderLine(long PurchaseOrderId, long SupplierId, long WarehouseId,
    string OrderCode, DateTime OrderDateUtc, DateOnly? ExpectedDeliveryDate, string Supplier,
    string SupplierTaxId, string? SupplierPaymentTerms, string Warehouse, string Currency,
    decimal Subtotal, decimal TotalDiscount, decimal TotalTax,
    decimal Total, string? Observation, string StatusCode, byte[] RowVersion, long PurchaseOrderLineId,
    long ProductId, string ProductCode, string Product, decimal OrderedQuantity, decimal ReceivedQuantity,
    decimal PendingQuantity, decimal UnitCost, decimal DiscountRate, decimal TaxRate,
    decimal LineSubtotal, decimal DiscountAmount, decimal TaxAmount, decimal LineTotal, string UnitSymbol);

/// <summary>Expone un renglón inmutable de recepción y su diferencia documentada.</summary>
public sealed record GoodsReceiptLine(long GoodsReceiptId, long PurchaseOrderId, long? MovementId,
    long? ReversalMovementId,
    string ReceiptCode, DateTime ReceiptDateUtc, string? SupplierDocumentNumber, string? Observation,
    string? ReversalReason, DateTime? ReversedAtUtc, long? ReversedByUserId, string StatusCode, byte[] RowVersion,
    long GoodsReceiptLineId, long PurchaseOrderLineId, long ProductId,
    string ProductCode, string Product, decimal AcceptedQuantity, decimal RejectedQuantity,
    decimal DamagedQuantity, string? DifferenceReason, string UnitSymbol);

/// <summary>Datos editables de un proveedor.</summary>
public sealed record SupplierChange(long? SupplierId, string LegalName, string? TradeName, string TaxId,
    string? Email, string? Phone, string? PaymentTerms, string? Observation, string StatusCode,
    IReadOnlyList<SupplierContactChange> Contacts, byte[]? RowVersion);

/// <summary>Datos de un contacto que reemplazará la colección activa del proveedor.</summary>
public sealed record SupplierContactChange(string Name, string? Position, string? Email, string? Phone,
    bool IsPrimary);

/// <summary>Datos editables de una orden de compra en borrador.</summary>
public sealed record PurchaseOrderChange(long? PurchaseOrderId, long SupplierId, long WarehouseId,
    DateTime OrderDateUtc, DateOnly? ExpectedDeliveryDate, string Currency, string? Observation,
    IReadOnlyList<PurchaseOrderDetailChange> Details, byte[]? RowVersion);

/// <summary>Renglón valorizado solicitado a un proveedor.</summary>
public sealed record PurchaseOrderDetailChange(long ProductId, decimal Quantity, decimal UnitCost,
    decimal DiscountRate, decimal TaxRate);

/// <summary>Datos de una recepción que puede completar parcialmente una orden aprobada.</summary>
public sealed record GoodsReceiptChange(long PurchaseOrderId, DateTime ReceiptDateUtc,
    string? SupplierDocumentNumber, string? Observation, IReadOnlyList<GoodsReceiptDetailChange> Details,
    byte[] OrderRowVersion);

/// <summary>Cantidades aceptadas y observadas para un renglón de la orden.</summary>
public sealed record GoodsReceiptDetailChange(long PurchaseOrderLineId, decimal AcceptedQuantity,
    decimal RejectedQuantity, decimal DamagedQuantity, string? DifferenceReason,
    IReadOnlyList<GoodsReceiptLotChange> Lots, IReadOnlyList<GoodsReceiptSerialChange> SerialNumbers,
    IReadOnlyList<GoodsReceiptContainerContentChange> ContainerContents);

/// <summary>Identifica una cantidad recibida que pertenece a un lote.</summary>
public sealed record GoodsReceiptLotChange(string Code, decimal Quantity, DateOnly? ManufactureDate,
    DateOnly? ExpirationDate);

/// <summary>Identifica un activo individual recibido por su número de serie.</summary>
public sealed record GoodsReceiptSerialChange(string SerialNumber, string AssetType, decimal? Capacity,
    string OwnerCode, string OwnerName, string ConditionCode);

/// <summary>Registra el contenido almacenado dentro de un activo recibido.</summary>
public sealed record GoodsReceiptContainerContentChange(string AssetSerialNumber, long ContentProductId,
    string? LotCode, decimal Quantity, string MeasurementMethod);

/// <summary>Error funcional controlado producido por el módulo Compras.</summary>
public sealed class PurchasingOperationException(long errorCode, string message) : Exception(message)
{
    /// <summary>Código estable registrado en el catálogo de errores.</summary>
    public long ErrorCode { get; } = errorCode;
}
