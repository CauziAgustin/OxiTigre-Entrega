/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Finance.FinanceModels
Archivo: FinanceModels.cs | Versión: 11.0.0 | Fecha: 2026-08-27 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Define caja, cobros, aplicaciones y cuenta corriente no fiscal.
Historial: 11.0.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
namespace OxiTigre.BLL.Finance;

/// <summary>Reúne el estado financiero operativo de la empresa autenticada.</summary>
public sealed record FinanceSnapshot(IReadOnlyList<FinanceBranch> Branches,
    IReadOnlyList<FinanceClient> Clients, IReadOnlyList<PaymentMethod> PaymentMethods,
    IReadOnlyList<CashBox> CashBoxes, IReadOnlyList<CashSession> CashSessions,
    IReadOnlyList<ReceivableSale> Sales, IReadOnlyList<CustomerPayment> Payments,
    IReadOnlyList<CustomerPaymentMethod> PaymentLines, IReadOnlyList<PaymentApplication> Applications,
    IReadOnlyList<AccountMovement> AccountMovements);

/// <summary>Representa una sucursal seleccionable para una caja.</summary>
public sealed record FinanceBranch(long BranchId, string Code, string Name);
/// <summary>Representa un cliente y su saldo actual en ARS.</summary>
public sealed record FinanceClient(long ClientId, string Code, string Name, decimal Balance);
/// <summary>Representa un medio de cobro configurable.</summary>
public sealed record PaymentMethod(long PaymentMethodId, string Code, string Name, string Type,
    bool AffectsCash, bool RequiresReference, string StatusCode, byte[] RowVersion);
/// <summary>Representa un punto físico de caja.</summary>
public sealed record CashBox(long CashBoxId, long BranchId, string Code, string Name, string Branch,
    string Currency, string StatusCode, byte[] RowVersion);
/// <summary>Representa una apertura y su arqueo de cierre.</summary>
public sealed record CashSession(long CashSessionId, long CashBoxId, string CashBox, long OpeningUserId,
    string User, DateTime OpenedUtc, decimal OpeningAmount, DateTime? ClosedUtc, decimal? ExpectedAmount,
    decimal? CountedAmount, decimal? Difference, string? OpeningObservation, string? ClosingObservation,
    string StatusCode, byte[] RowVersion);
/// <summary>Representa una venta interna y su saldo pendiente.</summary>
public sealed record ReceivableSale(long SaleId, long ClientId, string Code, DateTime SaleDateUtc,
    string Client, string Currency, decimal Total, decimal Outstanding, string StatusCode);
/// <summary>Representa un cobro confirmado o revertido.</summary>
public sealed record CustomerPayment(long PaymentId, long ClientId, long? CashSessionId, string Code,
    DateTime PaymentDateUtc, string Client, string Currency, decimal Total, string? Observation,
    string? ReversalReason, DateTime? ReversedUtc, string StatusCode, byte[] RowVersion);
/// <summary>Detalla un medio utilizado dentro de un cobro.</summary>
public sealed record CustomerPaymentMethod(long PaymentLineId, long PaymentId, long PaymentMethodId,
    string PaymentMethod, decimal Amount, string? Reference, string StatusCode);
/// <summary>Detalla cuánto de un cobro se imputó a una venta.</summary>
public sealed record PaymentApplication(long ApplicationId, long PaymentId, long SaleId, string Sale,
    decimal Amount, string StatusCode);
/// <summary>Representa un débito o crédito inmutable de cuenta corriente.</summary>
public sealed record AccountMovement(long AccountMovementId, long ClientId, long? SaleId, long? PaymentId,
    string MovementType, string Source, DateTime MovementDateUtc, string Currency, decimal Amount,
    string Description, Guid Correlation, string StatusCode);

/// <summary>Datos para crear o modificar un medio de pago.</summary>
public sealed record PaymentMethodChange(long? PaymentMethodId, string Code, string Name, string Type,
    bool AffectsCash, bool RequiresReference, string StatusCode, byte[]? RowVersion);
/// <summary>Datos para crear o modificar una caja física.</summary>
public sealed record CashBoxChange(long? CashBoxId, long BranchId, string Code, string Name,
    string Currency, string StatusCode, byte[]? RowVersion);
/// <summary>Importe aportado por un medio dentro del cobro.</summary>
public sealed record PaymentMethodAmount(long PaymentMethodId, decimal Amount, string? Reference);
/// <summary>Importe aplicado a una venta interna.</summary>
public sealed record SaleApplicationAmount(long SaleId, decimal Amount);
/// <summary>Datos completos para registrar un cobro.</summary>
public sealed record CustomerPaymentChange(long ClientId, long? CashSessionId, DateTime PaymentDateUtc,
    string Currency, decimal Total, string? Observation, IReadOnlyList<PaymentMethodAmount> Methods,
    IReadOnlyList<SaleApplicationAmount> Applications);

/// <summary>Error funcional estable del módulo Finanzas.</summary>
public sealed class FinanceOperationException(long errorCode, string message) : Exception(message)
{
    /// <summary>Código registrado en el catálogo de errores.</summary>
    public long ErrorCode { get; } = errorCode;
}
