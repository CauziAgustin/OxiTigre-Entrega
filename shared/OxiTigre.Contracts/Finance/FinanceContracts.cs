/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.Contracts.Finance.FinanceContracts
Archivo: FinanceContracts.cs | Versión: 11.0.0 | Fecha: 2026-08-27 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Define el contrato HTTP de caja, cobros y cuenta corriente no fiscal.
Historial: 11.0.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
namespace OxiTigre.Contracts.Finance;

/// <summary>Reúne el estado financiero visible para la empresa autenticada.</summary>
public sealed record FinanceSnapshotResponse(IReadOnlyList<FinanceBranchResponse> Branches,
    IReadOnlyList<FinanceClientResponse> Clients, IReadOnlyList<PaymentMethodResponse> PaymentMethods,
    IReadOnlyList<CashBoxResponse> CashBoxes, IReadOnlyList<CashSessionResponse> CashSessions,
    IReadOnlyList<ReceivableSaleResponse> Sales, IReadOnlyList<CustomerPaymentResponse> Payments,
    IReadOnlyList<CustomerPaymentMethodResponse> PaymentLines, IReadOnlyList<PaymentApplicationResponse> Applications,
    IReadOnlyList<AccountMovementResponse> AccountMovements);

/// <summary>Expone una sucursal seleccionable.</summary>
public sealed record FinanceBranchResponse(long BranchId, string Code, string Name);
/// <summary>Expone un cliente y su saldo actual en ARS.</summary>
public sealed record FinanceClientResponse(long ClientId, string Code, string Name, decimal Balance);
/// <summary>Expone un medio de pago configurable.</summary>
public sealed record PaymentMethodResponse(long PaymentMethodId, string Code, string Name, string Type,
    bool AffectsCash, bool RequiresReference, string StatusCode, string RowVersion);
/// <summary>Expone una caja física.</summary>
public sealed record CashBoxResponse(long CashBoxId, long BranchId, string Code, string Name, string Branch,
    string Currency, string StatusCode, string RowVersion);
/// <summary>Expone una apertura y su cierre.</summary>
public sealed record CashSessionResponse(long CashSessionId, long CashBoxId, string CashBox, long OpeningUserId,
    string User, DateTime OpenedUtc, decimal OpeningAmount, DateTime? ClosedUtc, decimal? ExpectedAmount,
    decimal? CountedAmount, decimal? Difference, string? OpeningObservation, string? ClosingObservation,
    string StatusCode, string RowVersion);
/// <summary>Expone una venta interna y su saldo pendiente.</summary>
public sealed record ReceivableSaleResponse(long SaleId, long ClientId, string Code, DateTime SaleDateUtc,
    string Client, string Currency, decimal Total, decimal Outstanding, string StatusCode);
/// <summary>Expone un cobro confirmado o revertido.</summary>
public sealed record CustomerPaymentResponse(long PaymentId, long ClientId, long? CashSessionId, string Code,
    DateTime PaymentDateUtc, string Client, string Currency, decimal Total, string? Observation,
    string? ReversalReason, DateTime? ReversedUtc, string StatusCode, string RowVersion);
/// <summary>Expone un medio utilizado en un cobro.</summary>
public sealed record CustomerPaymentMethodResponse(long PaymentLineId, long PaymentId, long PaymentMethodId,
    string PaymentMethod, decimal Amount, string? Reference, string StatusCode);
/// <summary>Expone una aplicación de cobro a venta.</summary>
public sealed record PaymentApplicationResponse(long ApplicationId, long PaymentId, long SaleId, string Sale,
    decimal Amount, string StatusCode);
/// <summary>Expone un movimiento inmutable de cuenta corriente.</summary>
public sealed record AccountMovementResponse(long AccountMovementId, long ClientId, long? SaleId, long? PaymentId,
    string MovementType, string Source, DateTime MovementDateUtc, string Currency, decimal Amount,
    string Description, Guid Correlation, string StatusCode);

/// <summary>Solicita crear o modificar un medio de pago.</summary>
public sealed record SavePaymentMethodRequest(long? PaymentMethodId, string Code, string Name, string Type,
    bool AffectsCash, bool RequiresReference, string StatusCode, string? RowVersion);
/// <summary>Solicita crear o modificar una caja física.</summary>
public sealed record SaveCashBoxRequest(long? CashBoxId, long BranchId, string Code, string Name,
    string Currency, string StatusCode, string? RowVersion);
/// <summary>Solicita abrir una caja para el usuario autenticado.</summary>
public sealed record OpenCashSessionRequest(long CashBoxId, decimal OpeningAmount, string? Observation);
/// <summary>Solicita cerrar una apertura con su arqueo contado.</summary>
public sealed record CloseCashSessionRequest(decimal CountedAmount, string? Observation, string RowVersion);
/// <summary>Representa un medio y su importe dentro del cobro.</summary>
public sealed record PaymentMethodAmountRequest(long PaymentMethodId, decimal Amount, string? Reference);
/// <summary>Representa el importe del cobro aplicado a una venta.</summary>
public sealed record SaleApplicationAmountRequest(long SaleId, decimal Amount);
/// <summary>Solicita registrar un cobro con sus medios y aplicaciones.</summary>
public sealed record RegisterCustomerPaymentRequest(long ClientId, long? CashSessionId, DateTime PaymentDateUtc,
    string Currency, decimal Total, string? Observation, IReadOnlyList<PaymentMethodAmountRequest>? Methods,
    IReadOnlyList<SaleApplicationAmountRequest>? Applications);
/// <summary>Solicita revertir un cobro mediante compensación.</summary>
public sealed record ReverseCustomerPaymentRequest(string Reason, string RowVersion);
/// <summary>Devuelve el identificador creado por una acción financiera.</summary>
public sealed record SavedFinanceResponse(long Id);
