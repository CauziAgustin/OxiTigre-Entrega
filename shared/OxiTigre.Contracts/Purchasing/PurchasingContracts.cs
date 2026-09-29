/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.Contracts.Purchasing.PurchasingContracts
Archivo: PurchasingContracts.cs | Versión: 1.1.0 | Fecha: 2026-08-26 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Define el contrato HTTP de proveedores, órdenes y recepciones parciales.
Historial: 1.0.0 | 2026-08-24 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-26 | FABRICA | Agustin Omar Cauzi | Reversión auditable de recepciones.
===============================================================================
*/
namespace OxiTigre.Contracts.Purchasing;

/// <summary>Reúne catálogos y documentos del circuito de Compras.</summary>
public sealed record PurchasingSnapshotResponse(IReadOnlyList<SupplierResponse> Suppliers,
    IReadOnlyList<SupplierContactResponse> SupplierContacts,
    IReadOnlyList<PurchasingProductResponse> Products, IReadOnlyList<PurchasingWarehouseResponse> Warehouses,
    IReadOnlyList<PurchaseOrderLineResponse> Orders, IReadOnlyList<GoodsReceiptLineResponse> Receipts);

/// <summary>Expone un proveedor.</summary>
public sealed record SupplierResponse(long SupplierId, string Code, string LegalName, string? TradeName,
    string TaxId, string? Email, string? Phone, string? PaymentTerms, string? Observation,
    string StatusCode, string RowVersion);

/// <summary>Expone una persona de contacto del proveedor.</summary>
public sealed record SupplierContactResponse(long SupplierContactId, long SupplierId, string Name,
    string? Position, string? Email, string? Phone, bool IsPrimary, string StatusCode, string RowVersion);

/// <summary>Expone un producto seleccionable en Compras.</summary>
public sealed record PurchasingProductResponse(long ProductId, string Code, string Name, string UnitSymbol,
    string TrackingType, bool IsReusable, bool AllowsLoans, bool RequiresMaintenance, bool AllowsMeasurements);

/// <summary>Expone un depósito de destino.</summary>
public sealed record PurchasingWarehouseResponse(long WarehouseId, string Code, string Name);

/// <summary>Expone un renglón de orden y sus cantidades acumuladas.</summary>
public sealed record PurchaseOrderLineResponse(long PurchaseOrderId, long SupplierId, long WarehouseId,
    string OrderCode, DateTime OrderDateUtc, DateOnly? ExpectedDeliveryDate, string Supplier,
    string SupplierTaxId, string? SupplierPaymentTerms, string Warehouse, string Currency,
    decimal Subtotal, decimal TotalDiscount, decimal TotalTax,
    decimal Total, string? Observation, string StatusCode, string RowVersion, long PurchaseOrderLineId,
    long ProductId, string ProductCode, string Product, decimal OrderedQuantity, decimal ReceivedQuantity,
    decimal PendingQuantity, decimal UnitCost, decimal DiscountRate, decimal TaxRate, decimal LineSubtotal,
    decimal DiscountAmount, decimal TaxAmount, decimal LineTotal, string UnitSymbol);

/// <summary>Expone un renglón inmutable de recepción.</summary>
public sealed record GoodsReceiptLineResponse(long GoodsReceiptId, long PurchaseOrderId, long? MovementId,
    long? ReversalMovementId,
    string ReceiptCode, DateTime ReceiptDateUtc, string? SupplierDocumentNumber, string? Observation,
    string? ReversalReason, DateTime? ReversedAtUtc, long? ReversedByUserId, string StatusCode, string RowVersion,
    long GoodsReceiptLineId, long PurchaseOrderLineId, long ProductId,
    string ProductCode, string Product, decimal AcceptedQuantity, decimal RejectedQuantity,
    decimal DamagedQuantity, string? DifferenceReason, string UnitSymbol);

/// <summary>Solicita crear o actualizar un proveedor.</summary>
public sealed record SaveSupplierRequest(long? SupplierId, string LegalName, string? TradeName, string TaxId,
    string? Email, string? Phone, string? PaymentTerms, string? Observation, string StatusCode,
    IReadOnlyList<SupplierContactRequest> Contacts, string? RowVersion);

/// <summary>Informa una persona de contacto para reemplazar la colección activa.</summary>
public sealed record SupplierContactRequest(string Name, string? Position, string? Email, string? Phone,
    bool IsPrimary);

/// <summary>Solicita crear o actualizar una orden de compra en borrador.</summary>
public sealed record SavePurchaseOrderRequest(long? PurchaseOrderId, long SupplierId, long WarehouseId,
    DateTime OrderDateUtc, DateOnly? ExpectedDeliveryDate, string Currency, string? Observation,
    IReadOnlyList<PurchaseOrderDetailRequest> Details, string? RowVersion);

/// <summary>Representa un renglón valorizado solicitado al proveedor.</summary>
public sealed record PurchaseOrderDetailRequest(long ProductId, decimal Quantity, decimal UnitCost,
    decimal DiscountRate, decimal TaxRate);

/// <summary>Solicita una transición de orden con control de concurrencia.</summary>
public sealed record PurchaseOrderTransitionRequest(string RowVersion);

/// <summary>Solicita cerrar el saldo pendiente de una orden con justificación.</summary>
public sealed record ClosePurchaseOrderRequest(string Reason, string RowVersion);

/// <summary>Solicita confirmar una recepción total o parcial.</summary>
public sealed record CreateGoodsReceiptRequest(DateTime ReceiptDateUtc, string? SupplierDocumentNumber,
    string? Observation, IReadOnlyList<GoodsReceiptDetailRequest> Details, string OrderRowVersion);

/// <summary>Solicita compensar una recepción conservando el documento original.</summary>
public sealed record ReverseGoodsReceiptRequest(string Reason, string RowVersion);

/// <summary>Informa cantidades aceptadas, rechazadas y dañadas de un renglón.</summary>
public sealed record GoodsReceiptDetailRequest(long PurchaseOrderLineId, decimal AcceptedQuantity,
    decimal RejectedQuantity, decimal DamagedQuantity, string? DifferenceReason,
    IReadOnlyList<GoodsReceiptLotRequest> Lots, IReadOnlyList<GoodsReceiptSerialRequest> SerialNumbers,
    IReadOnlyList<GoodsReceiptContainerContentRequest> ContainerContents);

/// <summary>Informa un lote incluido en la cantidad aceptada.</summary>
public sealed record GoodsReceiptLotRequest(string Code, decimal Quantity, DateOnly? ManufactureDate,
    DateOnly? ExpirationDate);

/// <summary>Informa un activo individual incluido en la cantidad aceptada.</summary>
public sealed record GoodsReceiptSerialRequest(string SerialNumber, string AssetType, decimal? Capacity,
    string OwnerCode, string OwnerName, string ConditionCode);

/// <summary>Informa el contenido actual guardado dentro de un activo recibido.</summary>
public sealed record GoodsReceiptContainerContentRequest(string AssetSerialNumber, long ContentProductId,
    string? LotCode, decimal Quantity, string MeasurementMethod);

/// <summary>Devuelve el identificador creado o actualizado.</summary>
public sealed record SavedPurchasingResponse(long Id);
