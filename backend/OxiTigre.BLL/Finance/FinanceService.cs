/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Finance.FinanceService
Archivo: FinanceService.cs | Versión: 11.0.0 | Fecha: 2026-08-27 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Autoriza y valida caja, cobros, aplicaciones y reversiones.
Historial: 11.0.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
using System.Text.Json;
using OxiTigre.BLL.Security;

namespace OxiTigre.BLL.Finance;

/// <summary>Orquesta Finanzas sin aceptar empresa, usuario ni permisos desde el cliente.</summary>
/// <param name="store">Persistencia SQL consolidada.</param>
public sealed class FinanceService(IFinanceStore store)
{
    /// <summary>Obtiene cajas, cobros, ventas pendientes y movimientos.</summary>
    /// <returns>Snapshot financiero de la empresa autenticada.</returns>
    /// <exception cref="UnauthorizedAccessException">Falta FINANZAS.CONSULTAR.</exception>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    public Task<FinanceSnapshot> GetAsync(
        SessionIdentity identity,
        CancellationToken cancellationToken
    )
    {
        Ensure(identity, "FINANZAS.CONSULTAR");
        return store.GetAsync(
            identity.CompanyId,
            identity.CompanyCode,
            identity.UserId,
            identity.SessionId,
            cancellationToken
        );
    }

    /// <summary>Crea o modifica un medio de pago.</summary>
    /// <returns>Identificador afectado.</returns>
    /// <exception cref="ArgumentException">Los datos o la versión son inválidos.</exception>
    /// <exception cref="UnauthorizedAccessException">Falta FINANZAS.CONFIGURAR.</exception>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="change">Código, tipo, reglas de referencia y estado del medio de pago.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    public Task<long> SavePaymentMethodAsync(
        SessionIdentity identity,
        PaymentMethodChange change,
        CancellationToken cancellationToken
    )
    {
        Ensure(identity, "FINANZAS.CONFIGURAR");
        ArgumentNullException.ThrowIfNull(change);
        if (
            change.Type is not ("EFECTIVO" or "TRANSFERENCIA" or "TARJETA" or "CHEQUE" or "OTRO")
            || change.StatusCode is not ("ACTIVO" or "INACTIVO")
        )
            throw new ArgumentException("Revisá el tipo y estado del medio de pago.");
        if (change.PaymentMethodId is { } id)
            Version(id, change.RowVersion);
        return Execute(
            identity,
            "MEDIO_GUARDAR",
            new Dictionary<string, object?>
            {
                ["@I_ID"] = change.PaymentMethodId,
                ["@I_CODIGO"] = Required(change.Code, 30, "El código"),
                ["@I_NOMBRE"] = Required(change.Name, 100, "El nombre"),
                ["@I_TIPO"] = change.Type,
                ["@I_AFECTA_EFECTIVO"] = change.AffectsCash,
                ["@I_REQUIERE_REFERENCIA"] = change.RequiresReference,
                ["@I_CODIGO_ESTADO"] = change.StatusCode,
                ["@I_ROW_VERSION"] = change.RowVersion,
            },
            cancellationToken
        );
    }

    /// <summary>Crea o modifica una caja física.</summary>
    /// <returns>Identificador afectado.</returns>
    /// <exception cref="ArgumentException">Los datos o la versión son inválidos.</exception>
    /// <exception cref="UnauthorizedAccessException">Falta FINANZAS.CONFIGURAR.</exception>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="change">Sucursal, código, moneda, estado y versión de la caja.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    public Task<long> SaveCashBoxAsync(
        SessionIdentity identity,
        CashBoxChange change,
        CancellationToken cancellationToken
    )
    {
        Ensure(identity, "FINANZAS.CONFIGURAR");
        ArgumentNullException.ThrowIfNull(change);
        if (
            change.BranchId <= 0
            || change.Currency?.Trim().Length != 3
            || change.StatusCode is not ("ACTIVO" or "INACTIVO")
        )
            throw new ArgumentException("Revisá sucursal, moneda y estado de la caja.");
        if (change.CashBoxId is { } id)
            Version(id, change.RowVersion);
        return Execute(
            identity,
            "CAJA_GUARDAR",
            new Dictionary<string, object?>
            {
                ["@I_ID"] = change.CashBoxId,
                ["@I_ID_SUCURSAL"] = change.BranchId,
                ["@I_CODIGO"] = Required(change.Code, 30, "El código"),
                ["@I_NOMBRE"] = Required(change.Name, 100, "El nombre"),
                ["@I_MONEDA"] = change.Currency.Trim().ToUpperInvariant(),
                ["@I_CODIGO_ESTADO"] = change.StatusCode,
                ["@I_ROW_VERSION"] = change.RowVersion,
            },
            cancellationToken
        );
    }

    /// <summary>Abre una caja para el usuario autenticado.</summary>
    /// <returns>Identificador de apertura.</returns>
    /// <exception cref="ArgumentException">Caja o importe inválido.</exception>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="cashBoxId">Identificador de la caja a consultar o modificar.</param>
    /// <param name="openingAmount">Importe inicial declarado al abrir la caja.</param>
    /// <param name="observation">Observación que explica el motivo o contexto de la operación.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    public Task<long> OpenCashSessionAsync(
        SessionIdentity identity,
        long cashBoxId,
        decimal openingAmount,
        string? observation,
        CancellationToken cancellationToken
    )
    {
        Ensure(identity, "FINANZAS.CAJA_GESTIONAR");
        if (cashBoxId <= 0 || openingAmount < 0)
            throw new ArgumentException("Revisá caja e importe inicial.");
        return Execute(
            identity,
            "SESION_ABRIR",
            new Dictionary<string, object?>
            {
                ["@I_ID"] = cashBoxId,
                ["@I_FECHA_UTC"] = DateTime.UtcNow,
                ["@I_IMPORTE"] = openingAmount,
                ["@I_OBSERVACION"] = Optional(observation, 500),
            },
            cancellationToken
        );
    }

    /// <summary>Cierra la caja propia y registra el arqueo.</summary>
    /// <returns>Tarea que finaliza al persistir el cierre.</returns>
    /// <exception cref="ArgumentException">Sesión, importe o versión inválida.</exception>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="cashSessionId">Identificador de la apertura de caja activa.</param>
    /// <param name="countedAmount">Efectivo contado al cerrar la caja.</param>
    /// <param name="observation">Observación que explica el motivo o contexto de la operación.</param>
    /// <param name="rowVersion">Versión binaria usada para detectar modificaciones concurrentes.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    public async Task CloseCashSessionAsync(
        SessionIdentity identity,
        long cashSessionId,
        decimal countedAmount,
        string? observation,
        byte[] rowVersion,
        CancellationToken cancellationToken
    )
    {
        Ensure(identity, "FINANZAS.CAJA_GESTIONAR");
        Version(cashSessionId, rowVersion);
        if (countedAmount < 0)
            throw new ArgumentException("El importe contado no puede ser negativo.");
        await Execute(
            identity,
            "SESION_CERRAR",
            new Dictionary<string, object?>
            {
                ["@I_ID"] = cashSessionId,
                ["@I_FECHA_UTC"] = DateTime.UtcNow,
                ["@I_IMPORTE_CONTADO"] = countedAmount,
                ["@I_OBSERVACION"] = Optional(observation, 500),
                ["@I_ROW_VERSION"] = rowVersion,
            },
            cancellationToken
        );
    }

    /// <summary>Registra un cobro dividido en medios y aplicado a cero o más ventas.</summary>
    /// <returns>Identificador del recibo interno.</returns>
    /// <exception cref="ArgumentException">Importes, medios o aplicaciones inválidas.</exception>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="change">Cliente, fecha, total, medios y aplicaciones a ventas del cobro.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    public Task<long> RegisterPaymentAsync(
        SessionIdentity identity,
        CustomerPaymentChange change,
        CancellationToken cancellationToken
    )
    {
        Ensure(identity, "FINANZAS.COBRAR");
        ArgumentNullException.ThrowIfNull(change);
        if (
            change.ClientId <= 0
            || change.Total <= 0
            || change.Currency?.Trim().Length != 3
            || change.Methods is not { Count: > 0 and <= 10 }
            || change.Applications is not { Count: <= 100 }
            || change.Methods.Any(item => item.PaymentMethodId <= 0 || item.Amount <= 0)
            || change.Applications.Any(item => item.SaleId <= 0 || item.Amount <= 0)
            || Math.Abs(change.Methods.Sum(item => item.Amount) - change.Total) > 0.0001m
            || change.Applications.Sum(item => item.Amount) > change.Total
            || change.Applications.Select(item => item.SaleId).Distinct().Count()
                != change.Applications.Count
        )
            throw new ArgumentException("Revisá cliente, total, medios y aplicaciones del cobro.");
        return Execute(
            identity,
            "PAGO_REGISTRAR",
            new Dictionary<string, object?>
            {
                ["@I_ID_CLIENTE"] = change.ClientId,
                ["@I_ID_SESION_CAJA"] = change.CashSessionId,
                ["@I_FECHA_UTC"] = change.PaymentDateUtc,
                ["@I_MONEDA"] = change.Currency.Trim().ToUpperInvariant(),
                ["@I_IMPORTE"] = change.Total,
                ["@I_OBSERVACION"] = Optional(change.Observation, 500),
                ["@I_JSON_MEDIOS"] = JsonSerializer.Serialize(change.Methods),
                ["@I_JSON_APLICACIONES"] = JsonSerializer.Serialize(change.Applications),
            },
            cancellationToken
        );
    }

    /// <summary>Revierte un cobro mediante un movimiento compensatorio.</summary>
    /// <returns>Tarea que finaliza al confirmar la reversión.</returns>
    /// <exception cref="ArgumentException">Pago, motivo o versión inválida.</exception>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="paymentId">Identificador del cobro que se consulta o revierte.</param>
    /// <param name="reason">Motivo funcional obligatorio de la operación.</param>
    /// <param name="rowVersion">Versión binaria usada para detectar modificaciones concurrentes.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    public async Task ReversePaymentAsync(
        SessionIdentity identity,
        long paymentId,
        string reason,
        byte[] rowVersion,
        CancellationToken cancellationToken
    )
    {
        Ensure(identity, "FINANZAS.REVERSAR");
        Version(paymentId, rowVersion);
        await Execute(
            identity,
            "PAGO_REVERSAR",
            new Dictionary<string, object?>
            {
                ["@I_ID"] = paymentId,
                ["@I_OBSERVACION"] = Required(reason, 500, "El motivo"),
                ["@I_ROW_VERSION"] = rowVersion,
            },
            cancellationToken
        );
    }

    /// <summary>Envía una acción financiera parametrizada con empresa y usuario de la sesión.</summary>
    /// <param name="identity">Sesión autenticada que fija el contexto de seguridad.</param>
    /// <param name="action">Acción reconocida por el procedimiento financiero.</param>
    /// <param name="values">Parámetros específicos de la acción.</param>
    /// <param name="cancellationToken">Token para cancelar la operación.</param>
    /// <returns>Identificador devuelto por la operación confirmada.</returns>
    private Task<long> Execute(
        SessionIdentity identity,
        string action,
        IReadOnlyDictionary<string, object?> values,
        CancellationToken cancellationToken
    ) =>
        store.ExecuteAsync(
            identity.CompanyId,
            identity.CompanyCode,
            identity.UserId,
            identity.SessionId,
            action,
            values,
            cancellationToken
        );

    /// <summary>Autoriza una consulta o modificación financiera mediante permisos de sesión.</summary>
    /// <param name="identity">Sesión autenticada que contiene los permisos.</param>
    /// <param name="permission">Código requerido para la operación.</param>
    /// <exception cref="ArgumentNullException">No se informó la identidad.</exception>
    /// <exception cref="UnauthorizedAccessException">La sesión no posee el permiso.</exception>
    private static void Ensure(SessionIdentity? identity, string permission)
    {
        ArgumentNullException.ThrowIfNull(identity);
        if (!identity.Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException($"Se requiere {permission}.");
    }

    /// <summary>Comprueba referencia y versión para no sobrescribir registros modificados.</summary>
    /// <param name="id">Identificador positivo del registro.</param>
    /// <param name="version">Versión binaria de ocho bytes.</param>
    /// <exception cref="ArgumentException">El identificador o la versión son inválidos.</exception>
    private static void Version(long id, byte[]? version)
    {
        if (id <= 0 || version is not { Length: 8 })
            throw new ArgumentException("El identificador o la versión no son válidos.");
    }

    /// <summary>Recorta y valida una observación o referencia obligatoria.</summary>
    /// <param name="value">Texto recibido desde la interfaz o la API.</param>
    /// <param name="maximumLength">Límite de caracteres del campo persistido.</param>
    /// <param name="field">Nombre funcional mostrado al operador si el valor falla.</param>
    /// <returns>Texto no vacío y sin espacios extremos.</returns>
    /// <exception cref="ArgumentException">El valor falta o supera el límite.</exception>
    private static string Required(string? value, int maximumLength, string field)
    {
        var result = value?.Trim();
        if (string.IsNullOrEmpty(result) || result.Length > maximumLength)
            throw new ArgumentException(
                $"{field} es obligatorio y admite hasta {maximumLength} caracteres."
            );
        return result;
    }

    /// <summary>Evita guardar observaciones vacías y rechaza textos demasiado largos.</summary>
    /// <param name="value">Texto opcional ingresado.</param>
    /// <param name="maximumLength">Límite de caracteres del campo persistido.</param>
    /// <returns>Texto recortado o nulo cuando no hay contenido.</returns>
    /// <exception cref="ArgumentException">El texto informado supera el límite.</exception>
    private static string? Optional(string? value, int maximumLength)
    {
        var result = value?.Trim();
        if (string.IsNullOrEmpty(result))
            return null;
        if (result.Length > maximumLength)
            throw new ArgumentException($"El valor admite hasta {maximumLength} caracteres.");
        return result;
    }
}
