/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Purchasing.PurchasingService
Archivo: PurchasingService.cs | Versión: 1.1.0 | Fecha: 2026-08-26 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Autoriza y valida el circuito trazable de Compras.
Historial: 1.0.0 | 2026-08-24 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-26 | FABRICA | Agustin Omar Cauzi | Validación de CUIT y reversión segura.
===============================================================================
*/
using System.Net.Mail;
using OxiTigre.BLL.Security;

namespace OxiTigre.BLL.Purchasing;

/// <summary>Orquesta proveedores, autorizaciones y recepciones sin confiar en contexto del cliente.</summary>
/// <param name="store">Persistencia que ejecuta las operaciones dentro de la empresa y sesión autenticadas.</param>
public sealed class PurchasingService(IPurchasingStore store)
{
    private const decimal Decimal19_4Max = 999999999999999.9999m;

    /// <summary>Obtiene el circuito de Compras visible para la sesión.</summary>
    /// <param name="identity">Identidad autenticada de la cual se toman empresa, usuario, sesión y permisos.</param>
    /// <param name="cancellationToken">Token que permite cancelar la consulta.</param>
    /// <returns>Proveedores, catálogos, órdenes y recepciones pertenecientes a la empresa autenticada.</returns>
    /// <exception cref="ArgumentNullException">La identidad es nula.</exception>
    /// <exception cref="UnauthorizedAccessException">La sesión no posee el permiso <c>COMPRAS.CONSULTAR</c>.</exception>
    /// <exception cref="PurchasingOperationException">SQL Server devolvió un error funcional controlado de Compras.</exception>
    public Task<PurchasingSnapshot> GetAsync(
        SessionIdentity identity,
        CancellationToken cancellationToken
    )
    {
        Ensure(identity, "COMPRAS.CONSULTAR");
        return store.GetAsync(
            identity.CompanyId,
            identity.CompanyCode,
            identity.UserId,
            identity.SessionId,
            cancellationToken
        );
    }

    /// <summary>Crea o modifica un proveedor.</summary>
    /// <param name="identity">Identidad autenticada que determina empresa, usuario, sesión y permisos.</param>
    /// <param name="change">Datos completos del proveedor y la colección de contactos activos que se guardarán.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación.</param>
    /// <returns>Identificador del proveedor creado o actualizado.</returns>
    /// <exception cref="ArgumentNullException">La identidad o el cambio son nulos.</exception>
    /// <exception cref="ArgumentException">El CUIT, correo, estado, contactos o control de concurrencia no son válidos.</exception>
    /// <exception cref="UnauthorizedAccessException">La sesión no posee el permiso <c>COMPRAS.GESTIONAR</c>.</exception>
    /// <exception cref="PurchasingOperationException">SQL Server devolvió un error funcional controlado de Compras.</exception>
    public Task<long> SaveSupplierAsync(
        SessionIdentity identity,
        SupplierChange change,
        CancellationToken cancellationToken
    )
    {
        Ensure(identity, "COMPRAS.GESTIONAR");
        ArgumentNullException.ThrowIfNull(change);
        Edit(change.SupplierId, change.RowVersion);

        var email = Optional(change.Email, 254);
        if (email is not null && !MailAddress.TryCreate(email, out _))
            throw new ArgumentException("El correo del proveedor no es válido.");

        if (
            change.Contacts is not { Count: <= 20 }
            || change.Contacts.Count > 0
                && change.Contacts.Count(contact => contact.IsPrimary) != 1
        )
            throw new ArgumentException("Informá como máximo 20 contactos y exactamente uno principal.");

        var contacts = change
            .Contacts.Select(contact =>
            {
                var contactEmail = Optional(contact.Email, 254);
                if (contactEmail is not null && !MailAddress.TryCreate(contactEmail, out _))
                    throw new ArgumentException("El correo de un contacto no es válido.");

                return contact with
                {
                    Name = Required(contact.Name, 200, "El nombre del contacto"),
                    Position = Optional(contact.Position, 100),
                    Email = contactEmail,
                    Phone = Optional(contact.Phone, 50),
                };
            })
            .ToList();

        return store.SaveSupplierAsync(
            identity.CompanyId,
            identity.CompanyCode,
            identity.UserId,
            identity.SessionId,
            change with
            {
                LegalName = Required(change.LegalName, 200, "La razón social"),
                TradeName = Optional(change.TradeName, 200),
                TaxId = TaxId(change.TaxId),
                Email = email,
                Phone = Optional(change.Phone, 50),
                PaymentTerms = Optional(change.PaymentTerms, 150),
                Observation = Optional(change.Observation, 1000),
                StatusCode = Status(change.StatusCode),
                Contacts = contacts,
            },
            cancellationToken
        );
    }

    /// <summary>Crea o modifica una orden mientras permanece en borrador.</summary>
    /// <param name="identity">Identidad autenticada que determina empresa, usuario, sesión y permisos.</param>
    /// <param name="change">Cabecera y renglones valorizados de la orden.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación.</param>
    /// <returns>Identificador de la orden creada o actualizada.</returns>
    /// <exception cref="ArgumentNullException">La identidad o el cambio son nulos.</exception>
    /// <exception cref="ArgumentException">La orden contiene relaciones, fechas, cantidades, porcentajes o importes inválidos.</exception>
    /// <exception cref="UnauthorizedAccessException">La sesión no posee el permiso <c>COMPRAS.GESTIONAR</c>.</exception>
    /// <exception cref="PurchasingOperationException">SQL Server devolvió un error funcional controlado de Compras.</exception>
    public Task<long> SaveOrderAsync(
        SessionIdentity identity,
        PurchaseOrderChange change,
        CancellationToken cancellationToken
    )
    {
        Ensure(identity, "COMPRAS.GESTIONAR");
        ArgumentNullException.ThrowIfNull(change);
        Edit(change.PurchaseOrderId, change.RowVersion);

        if (
            change.SupplierId <= 0
            || change.WarehouseId <= 0
            || change.ExpectedDeliveryDate is { } expected
                && expected < DateOnly.FromDateTime(change.OrderDateUtc)
            || change.Details is not { Count: > 0 and <= 100 }
            || change.Details.Any(detail =>
                detail.ProductId <= 0
                || !PositiveAmount(detail.Quantity)
                || !Amount(detail.UnitCost)
                || !Rate(detail.DiscountRate)
                || !Rate(detail.TaxRate)
            )
        )
            throw new ArgumentException("Informá proveedor, depósito, fechas válidas y entre 1 y 100 renglones con importes válidos.");

        if (change.Details.GroupBy(detail => detail.ProductId).Any(group => group.Count() > 1))
            throw new ArgumentException("Un producto no puede repetirse en la misma orden.");

        if (!TotalsFit(change.Details))
            throw new ArgumentException("Los totales de la orden superan el máximo monetario permitido.");

        return store.SaveOrderAsync(
            identity.CompanyId,
            identity.CompanyCode,
            identity.UserId,
            identity.SessionId,
            change with
            {
                Currency = Currency(change.Currency),
                Observation = Optional(change.Observation, 1000),
            },
            cancellationToken
        );
    }

    /// <summary>Envía un borrador a aprobación.</summary>
    /// <param name="identity">Identidad autenticada que solicita la transición.</param>
    /// <param name="purchaseOrderId">Identificador de la orden.</param>
    /// <param name="rowVersion">Versión binaria de ocho bytes leída al consultar la orden.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación.</param>
    /// <returns>Tarea que finaliza cuando la transición queda persistida.</returns>
    /// <exception cref="ArgumentException">El identificador o la versión no son válidos.</exception>
    /// <exception cref="UnauthorizedAccessException">La sesión no posee el permiso <c>COMPRAS.GESTIONAR</c>.</exception>
    /// <exception cref="PurchasingOperationException">La orden cambió de estado o versión durante la operación.</exception>
    public Task SubmitOrderAsync(
        SessionIdentity identity,
        long purchaseOrderId,
        byte[] rowVersion,
        CancellationToken cancellationToken
    ) =>
        Transition(
            identity,
            "COMPRAS.GESTIONAR",
            purchaseOrderId,
            rowVersion,
            store.SubmitOrderAsync,
            cancellationToken
        );

    /// <summary>Aprueba una orden pendiente con permiso independiente.</summary>
    /// <param name="identity">Identidad autenticada del aprobador.</param>
    /// <param name="purchaseOrderId">Identificador de la orden.</param>
    /// <param name="rowVersion">Versión binaria de ocho bytes leída al consultar la orden.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación.</param>
    /// <returns>Tarea que finaliza cuando la aprobación queda persistida.</returns>
    /// <exception cref="ArgumentException">El identificador o la versión no son válidos.</exception>
    /// <exception cref="UnauthorizedAccessException">La sesión no posee el permiso <c>COMPRAS.APROBAR</c>.</exception>
    /// <exception cref="PurchasingOperationException">La orden cambió de estado o versión durante la operación.</exception>
    public Task ApproveOrderAsync(
        SessionIdentity identity,
        long purchaseOrderId,
        byte[] rowVersion,
        CancellationToken cancellationToken
    ) =>
        Transition(
            identity,
            "COMPRAS.APROBAR",
            purchaseOrderId,
            rowVersion,
            store.ApproveOrderAsync,
            cancellationToken
        );

    /// <summary>Cancela una orden conservando su historial.</summary>
    /// <param name="identity">Identidad autenticada que solicita la cancelación.</param>
    /// <param name="purchaseOrderId">Identificador de la orden.</param>
    /// <param name="rowVersion">Versión binaria de ocho bytes leída al consultar la orden.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación.</param>
    /// <returns>Tarea que finaliza cuando la cancelación queda persistida.</returns>
    /// <exception cref="ArgumentException">El identificador o la versión no son válidos.</exception>
    /// <exception cref="UnauthorizedAccessException">La sesión no posee el permiso <c>COMPRAS.GESTIONAR</c>.</exception>
    /// <exception cref="PurchasingOperationException">La orden no admite la cancelación solicitada.</exception>
    public Task CancelOrderAsync(
        SessionIdentity identity,
        long purchaseOrderId,
        byte[] rowVersion,
        CancellationToken cancellationToken
    ) =>
        Transition(
            identity,
            "COMPRAS.GESTIONAR",
            purchaseOrderId,
            rowVersion,
            store.CancelOrderAsync,
            cancellationToken
        );

    /// <summary>Cierra definitivamente las cantidades pendientes y conserva el motivo.</summary>
    /// <param name="identity">Identidad autenticada que solicita el cierre.</param>
    /// <param name="purchaseOrderId">Identificador de la orden con saldo pendiente.</param>
    /// <param name="reason">Justificación obligatoria del cierre, con un máximo de 500 caracteres.</param>
    /// <param name="rowVersion">Versión binaria de ocho bytes leída al consultar la orden.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación.</param>
    /// <returns>Tarea que finaliza cuando el cierre queda persistido.</returns>
    /// <exception cref="ArgumentException">El identificador, la versión o el motivo no son válidos.</exception>
    /// <exception cref="UnauthorizedAccessException">La sesión no posee el permiso <c>COMPRAS.GESTIONAR</c>.</exception>
    /// <exception cref="PurchasingOperationException">La orden no admite el cierre o cambió concurrentemente.</exception>
    public Task CloseOrderAsync(
        SessionIdentity identity,
        long purchaseOrderId,
        string reason,
        byte[] rowVersion,
        CancellationToken cancellationToken
    )
    {
        Ensure(identity, "COMPRAS.GESTIONAR");
        Version(purchaseOrderId, rowVersion);

        return store.CloseOrderAsync(
            identity.CompanyId,
            identity.CompanyCode,
            identity.UserId,
            identity.SessionId,
            purchaseOrderId,
            Required(reason, 500, "El motivo de cierre"),
            rowVersion,
            cancellationToken
        );
    }

    /// <summary>Confirma cantidades recibidas; solamente lo aceptado puede ingresar al stock.</summary>
    /// <param name="identity">Identidad autenticada del receptor.</param>
    /// <param name="change">Documento, cantidades reales y datos de trazabilidad de la recepción.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación.</param>
    /// <returns>Identificador de la recepción confirmada.</returns>
    /// <exception cref="ArgumentNullException">La identidad o el cambio son nulos.</exception>
    /// <exception cref="ArgumentException">Las cantidades, diferencias, lotes, series, contenidos o versión no son válidos.</exception>
    /// <exception cref="UnauthorizedAccessException">La sesión no posee el permiso <c>COMPRAS.RECIBIR</c>.</exception>
    /// <exception cref="PurchasingOperationException">La orden, el stock o la trazabilidad rechazaron la recepción.</exception>
    public Task<long> CreateReceiptAsync(
        SessionIdentity identity,
        GoodsReceiptChange change,
        CancellationToken cancellationToken
    )
    {
        Ensure(identity, "COMPRAS.RECIBIR");
        ArgumentNullException.ThrowIfNull(change);
        Version(change.PurchaseOrderId, change.OrderRowVersion);

        if (
            change.Details is not { Count: > 0 and <= 100 }
            || change.Details.Any(detail =>
                detail.PurchaseOrderLineId <= 0
                || !Amount(detail.AcceptedQuantity)
                || !Amount(detail.RejectedQuantity)
                || !Amount(detail.DamagedQuantity)
                || detail.AcceptedQuantity + detail.RejectedQuantity + detail.DamagedQuantity <= 0
                || detail.RejectedQuantity + detail.DamagedQuantity > 0
                    && string.IsNullOrWhiteSpace(detail.DifferenceReason)
            )
        )
            throw new ArgumentException("Informá entre 1 y 100 renglones con cantidades válidas y el motivo de cada diferencia.");

        if (change.Details.GroupBy(detail => detail.PurchaseOrderLineId).Any(group => group.Count() > 1))
            throw new ArgumentException("Un renglón de orden no puede repetirse en la misma recepción.");

        foreach (var detail in change.Details)
            ValidateTraceability(detail);

        return store.CreateReceiptAsync(
            identity.CompanyId,
            identity.CompanyCode,
            identity.UserId,
            identity.SessionId,
            change with
            {
                SupplierDocumentNumber = Optional(change.SupplierDocumentNumber, 80),
                Observation = Optional(change.Observation, 1000),
                Details = change
                    .Details.Select(detail => detail with
                    {
                        DifferenceReason = Optional(detail.DifferenceReason, 500),
                        Lots = detail
                            .Lots.Select(lot => lot with
                            {
                                Code = Required(lot.Code, 50, "El lote"),
                            })
                            .ToList(),
                        SerialNumbers = detail
                            .SerialNumbers.Select(serial => serial with
                            {
                                SerialNumber = Required(serial.SerialNumber, 100, "El número de serie"),
                                AssetType = Required(serial.AssetType, 30, "El tipo de activo")
                                    .ToUpperInvariant(),
                                OwnerCode = Required(serial.OwnerCode, 30, "El código de propietario")
                                    .ToUpperInvariant(),
                                OwnerName = Required(serial.OwnerName, 200, "El propietario"),
                                ConditionCode = Required(serial.ConditionCode, 30, "La condición")
                                    .ToUpperInvariant(),
                            })
                            .ToList(),
                        ContainerContents = detail
                            .ContainerContents.Select(content => content with
                            {
                                AssetSerialNumber = Required(
                                    content.AssetSerialNumber,
                                    100,
                                    "La serie contenedora"
                                ),
                                LotCode = Optional(content.LotCode, 50),
                                MeasurementMethod = Required(
                                        content.MeasurementMethod,
                                        30,
                                        "El método de medición"
                                    )
                                    .ToUpperInvariant(),
                            })
                            .ToList(),
                    })
                    .ToList(),
            },
            cancellationToken
        );
    }

    /// <summary>Revierte una recepción intacta mediante un movimiento compensatorio auditado.</summary>
    /// <param name="identity">Identidad autenticada que solicita la reversión.</param>
    /// <param name="goodsReceiptId">Identificador de la recepción confirmada.</param>
    /// <param name="reason">Justificación obligatoria, con un máximo de 500 caracteres.</param>
    /// <param name="rowVersion">Versión binaria de ocho bytes leída al consultar la recepción.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación.</param>
    /// <returns>Tarea que finaliza cuando el movimiento compensatorio y la auditoría quedan persistidos.</returns>
    /// <exception cref="ArgumentException">El identificador, la versión o el motivo no son válidos.</exception>
    /// <exception cref="UnauthorizedAccessException">La sesión no posee el permiso <c>COMPRAS.RECIBIR</c>.</exception>
    /// <exception cref="PurchasingOperationException">El material recibido ya tuvo operaciones posteriores o la versión cambió.</exception>
    public Task ReverseReceiptAsync(
        SessionIdentity identity,
        long goodsReceiptId,
        string reason,
        byte[] rowVersion,
        CancellationToken cancellationToken
    )
    {
        Ensure(identity, "COMPRAS.RECIBIR");
        Version(goodsReceiptId, rowVersion);

        return store.ReverseReceiptAsync(
            identity.CompanyId,
            identity.CompanyCode,
            identity.UserId,
            identity.SessionId,
            goodsReceiptId,
            Required(reason, 500, "El motivo de reversión"),
            rowVersion,
            cancellationToken
        );
    }

    /// <summary>Rechaza lotes, series o contenidos ambiguos antes de confirmar la recepción.</summary>
    /// <param name="detail">Renglón recibido con sus identificadores y mediciones.</param>
    /// <exception cref="ArgumentException">Hay duplicados, cantidades inválidas, fechas invertidas o más de 100 elementos de un tipo.</exception>
    private static void ValidateTraceability(GoodsReceiptDetailChange detail)
    {
        if (
            detail.Lots is not { Count: <= 100 }
            || detail.SerialNumbers is not { Count: <= 100 }
            || detail.ContainerContents is not { Count: <= 100 }
        )
            throw new ArgumentException("La trazabilidad admite hasta 100 lotes, series y contenidos por renglón.");

        if (
            detail.Lots.Any(lot =>
                !PositiveAmount(lot.Quantity)
                || lot.ExpirationDate is { } expiration
                    && lot.ManufactureDate is { } manufacture
                    && expiration < manufacture
            )
            || detail.Lots.GroupBy(lot => lot.Code, StringComparer.OrdinalIgnoreCase)
                .Any(group => group.Count() > 1)
        )
            throw new ArgumentException("Los lotes deben ser únicos y tener cantidad y fechas válidas.");

        if (
            detail.SerialNumbers.Any(serial =>
                serial.Capacity is { } capacity && !PositiveAmount(capacity)
            )
            || detail.SerialNumbers.GroupBy(
                    serial => serial.SerialNumber,
                    StringComparer.OrdinalIgnoreCase
                )
                .Any(group => group.Count() > 1)
        )
            throw new ArgumentException("Los números de serie deben ser únicos y sus capacidades válidas.");

        if (
            detail.ContainerContents.Any(content =>
                content.ContentProductId <= 0
                || !PositiveAmount(content.Quantity)
                || content.MeasurementMethod?.Trim().ToUpperInvariant()
                    is not ("MANUAL" or "PESAJE" or "CALCULADO")
            )
        )
            throw new ArgumentException("El contenido debe indicar activo, producto, cantidad y método de medición válidos.");
    }

    /// <summary>Aplica una transición de orden con permiso y versión de concurrencia comprobados.</summary>
    /// <param name="identity">Sesión de la que se toman empresa, usuario y permisos.</param>
    /// <param name="permission">Permiso exigido para esta transición.</param>
    /// <param name="id">Orden sobre la que actúa el procedimiento.</param>
    /// <param name="version">Rowversion de ocho bytes consultada antes de editar.</param>
    /// <param name="action">Operación de persistencia correspondiente al cambio de estado.</param>
    /// <param name="cancellationToken">Cancela la operación solicitada.</param>
    /// <returns>Tarea que concluye al persistir la transición.</returns>
    /// <exception cref="ArgumentException">La orden o su versión no son válidas.</exception>
    /// <exception cref="UnauthorizedAccessException">La sesión carece del permiso solicitado.</exception>
    private Task Transition(
        SessionIdentity identity,
        string permission,
        long id,
        byte[] version,
        Func<long, string, long, long, long, byte[], CancellationToken, Task> action,
        CancellationToken cancellationToken
    )
    {
        Ensure(identity, permission);
        Version(id, version);

        return action(
            identity.CompanyId,
            identity.CompanyCode,
            identity.UserId,
            identity.SessionId,
            id,
            version,
            cancellationToken
        );
    }

    /// <summary>Comprueba el rango y escala admitidos por DECIMAL(19,4) en SQL Server.</summary>
    /// <param name="value">Importe o cantidad que se va a persistir.</param>
    /// <returns><see langword="true"/> si el valor no es negativo y cabe sin perder decimales.</returns>
    private static bool Amount(decimal value) =>
        value >= 0 && value <= Decimal19_4Max && decimal.Round(value, 4) == value;

    /// <summary>Exige una cantidad positiva compatible con DECIMAL(19,4).</summary>
    /// <param name="value">Cantidad de producto, lote o contenido.</param>
    /// <returns><see langword="true"/> si es mayor que cero y cabe en la columna.</returns>
    private static bool PositiveAmount(decimal value) => value > 0 && Amount(value);

    /// <summary>Comprueba que un descuento o impuesto sea una fracción de cero a uno con seis decimales como máximo.</summary>
    /// <param name="value">Tasa expresada como fracción, no como porcentaje entero.</param>
    /// <returns><see langword="true"/> si puede persistirse sin redondeo implícito.</returns>
    private static bool Rate(decimal value) =>
        value >= 0 && value <= 1 && decimal.Round(value, 6) == value;

    /// <summary>Calcula cada subtotal, descuento e impuesto con el mismo redondeo de la orden y descarta desbordes.</summary>
    /// <param name="details">Renglones que se van a guardar juntos.</param>
    /// <returns><see langword="true"/> si todos los importes y el total caben en DECIMAL(19,4).</returns>
    private static bool TotalsFit(IReadOnlyList<PurchaseOrderDetailChange> details)
    {
        try
        {
            decimal total = 0;

            foreach (var detail in details)
            {
                var subtotal = Money(detail.Quantity * detail.UnitCost);
                var discount = Money(subtotal * detail.DiscountRate);
                var tax = Money((subtotal - discount) * detail.TaxRate);
                var lineTotal = Money(subtotal - discount + tax);

                if (
                    subtotal > Decimal19_4Max
                    || discount > Decimal19_4Max
                    || tax > Decimal19_4Max
                    || lineTotal > Decimal19_4Max
                )
                    return false;

                total += lineTotal;
                if (total > Decimal19_4Max)
                    return false;
            }

            return true;
        }
        catch (OverflowException)
        {
            return false;
        }
    }

    /// <summary>Redondea importes a cuatro decimales, alejando del cero los empates.</summary>
    /// <param name="value">Importe calculado antes de persistirlo.</param>
    /// <returns>Importe con la escala monetaria usada por Compras.</returns>
    private static decimal Money(decimal value) =>
        decimal.Round(value, 4, MidpointRounding.AwayFromZero);

    /// <summary>Valida el dígito verificador del CUIT y lo presenta como XX-XXXXXXXX-X.</summary>
    /// <param name="value">CUIT ingresado con dígitos y separadores opcionales.</param>
    /// <returns>CUIT normalizado, apto para comparar y almacenar.</returns>
    /// <exception cref="ArgumentException">Faltan dígitos, hay caracteres prohibidos o falla el módulo 11.</exception>
    private static string TaxId(string? value)
    {
        var taxId = Required(value, 30, "La identificación fiscal");
        if (taxId.Any(character => !char.IsDigit(character) && character is not ('-' or '.' or ' ')))
            throw new ArgumentException("El CUIT del proveedor solo admite números y separadores.");

        var digits = new string(taxId.Where(char.IsDigit).ToArray());
        int[] factors = [5, 4, 3, 2, 7, 6, 5, 4, 3, 2];

        var verifier =
            digits.Length == 11
                ? 11 - digits.Take(10).Select((digit, index) => (digit - '0') * factors[index]).Sum() % 11
                : -1;
        verifier = verifier == 11 ? 0 : verifier == 10 ? 9 : verifier;

        if (digits.Length != 11 || verifier != digits[10] - '0')
            throw new ArgumentException("El CUIT del proveedor no es válido.");

        return $"{digits[..2]}-{digits.Substring(2, 8)}-{digits[^1]}";
    }

    /// <summary>Normaliza un código de moneda de tres letras.</summary>
    /// <param name="value">Código ingresado por la interfaz.</param>
    /// <returns>Código en mayúsculas.</returns>
    /// <exception cref="ArgumentException">El código está vacío o no contiene exactamente tres letras.</exception>
    private static string Currency(string? value) =>
        Required(value, 3, "La moneda").ToUpperInvariant() is var code
        && code.Length == 3
        && code.All(char.IsLetter)
            ? code
            : throw new ArgumentException("La moneda debe contener tres letras.");

    /// <summary>Limita el estado de un proveedor a activo o inactivo.</summary>
    /// <param name="value">Estado recibido de la interfaz.</param>
    /// <returns>Estado válido en mayúsculas.</returns>
    /// <exception cref="ArgumentException">El estado no pertenece al catálogo permitido.</exception>
    private static string Status(string? value) =>
        Required(value, 30, "El estado").ToUpperInvariant() is var status
        && status is "ACTIVO" or "INACTIVO"
            ? status
            : throw new ArgumentException("El estado debe ser ACTIVO o INACTIVO.");

    /// <summary>Recorta un texto obligatorio y limita su longitud antes de enviarlo a SQL.</summary>
    /// <param name="value">Texto ingresado.</param>
    /// <param name="max">Límite de caracteres de la columna de destino.</param>
    /// <param name="name">Nombre visible del campo para el error de validación.</param>
    /// <returns>Texto no vacío y sin espacios exteriores.</returns>
    /// <exception cref="ArgumentException">El texto falta o supera el límite.</exception>
    private static string Required(string? value, int max, string name) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException($"{name} es obligatorio.")
        : value.Trim() is var result && result.Length <= max
            ? result
            : throw new ArgumentException($"{name} supera {max} caracteres.");

    /// <summary>Convierte un texto opcional vacío en nulo y valida su longitud si se informó.</summary>
    /// <param name="value">Texto ingresado.</param>
    /// <param name="max">Límite de caracteres de la columna de destino.</param>
    /// <returns>Texto recortado o <see langword="null"/>.</returns>
    /// <exception cref="ArgumentException">El texto informado supera el límite.</exception>
    private static string? Optional(string? value, int max) =>
        string.IsNullOrWhiteSpace(value) ? null : Required(value, max, "El valor");

    /// <summary>Comprueba que la sesión exista y tenga el permiso de Compras requerido.</summary>
    /// <param name="identity">Identidad autenticada.</param>
    /// <param name="permission">Código de permiso exigido por la operación.</param>
    /// <exception cref="ArgumentNullException">No se proporcionó identidad.</exception>
    /// <exception cref="UnauthorizedAccessException">El permiso no está en la sesión.</exception>
    private static void Ensure(SessionIdentity identity, string permission)
    {
        ArgumentNullException.ThrowIfNull(identity);
        if (!identity.Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException($"La sesión no posee el permiso {permission}.");
    }

    /// <summary>Distingue una creación de una edición y exige rowversion solamente al editar.</summary>
    /// <param name="id">Identificador existente o <see langword="null"/> para crear.</param>
    /// <param name="version">Rowversion obtenida al consultar el registro.</param>
    /// <exception cref="ArgumentException">El identificador es inválido o falta la versión para editar.</exception>
    private static void Edit(long? id, byte[]? version)
    {
        if (id is <= 0)
            throw new ArgumentException("El identificador no es válido.");

        if (id is not null && version is not { Length: 8 })
            throw new ArgumentException("La versión del registro no es válida.");
    }

    /// <summary>Impide transiciones sobre una orden inexistente o con versión incompleta.</summary>
    /// <param name="id">Identificador de la orden.</param>
    /// <param name="version">Rowversion de ocho bytes recibida del cliente.</param>
    /// <exception cref="ArgumentException">El identificador o la versión no son válidos.</exception>
    private static void Version(long id, byte[]? version)
    {
        if (id <= 0 || version is not { Length: 8 })
            throw new ArgumentException("La orden o su versión no son válidas.");
    }
}
