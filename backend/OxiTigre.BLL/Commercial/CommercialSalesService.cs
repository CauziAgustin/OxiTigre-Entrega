/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Commercial.CommercialSalesService
Archivo: CommercialSalesService.cs | Versión: 1.3.0 | Fecha: 2026-08-28 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Autoriza y valida listas, promociones, pedidos y ventas sin aceptar contexto seguro del cliente.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-21 | FABRICA | Agustin Omar Cauzi | Validación de promociones y pedidos promocionales.
Historial: 1.2.0 | 2026-08-21 | FABRICA | Agustin Omar Cauzi | Límites SQL decimales y cálculo seguro de subtotales.
Historial: 1.3.0 | 2026-08-28 | FABRICA | Agustin Omar Cauzi | Validación de servicios y activos asociados.
===============================================================================
*/
using OxiTigre.BLL.Security;

namespace OxiTigre.BLL.Commercial;

/// <summary>Orquesta el ciclo comercial respetando empresa, permisos, dinero y concurrencia.</summary>
public sealed class CommercialSalesService(ICommercialSalesStore store)
{
    private const decimal Decimal19_4Max = 999999999999999.9999m;
    private const decimal Decimal9_6Max = 999.999999m;

    /// <summary>Recupera los datos comerciales de la empresa de la sesión.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Clientes, listas de precios, promociones, pedidos y ventas de la empresa.</returns>
    public Task<SalesSnapshot> GetAsync(
        SessionIdentity identity,
        CancellationToken cancellationToken
    )
    {
        Ensure(identity, "COMERCIAL.CONSULTAR");
        return store.GetAsync(
            identity.CompanyId,
            identity.CompanyCode,
            identity.UserId,
            identity.SessionId,
            cancellationToken
        );
    }

    /// <summary>Crea o actualiza una lista de precios.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="change">Nombre, vigencia, moneda y estado de la lista de precios.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador de la lista de precios creada o actualizada.</returns>
    /// <exception cref="ArgumentException">Falta código, nombre o moneda; la vigencia es inversa o la versión es inválida.</exception>
    public Task<long> SavePriceListAsync(
        SessionIdentity identity,
        PriceListChange change,
        CancellationToken cancellationToken
    )
    {
        Manage(identity);
        ArgumentNullException.ThrowIfNull(change);
        Edit(change.PriceListId, change.RowVersion);
        if (change.ValidUntil < change.ValidFrom)
            throw new ArgumentException(
                "La vigencia hasta no puede ser anterior a la vigencia desde."
            );
        return store.SavePriceListAsync(
            identity.CompanyId,
            identity.CompanyCode,
            identity.UserId,
            identity.SessionId,
            change with
            {
                Code = Code(change.Code),
                Name = Required(change.Name, 150),
                Currency = Currency(change.Currency),
                StatusCode = Status(change.StatusCode),
            },
            cancellationToken
        );
    }

    /// <summary>Crea o actualiza el precio de un producto.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="change">Producto, lista y precio que se guardará.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador del precio de producto guardado en la lista.</returns>
    /// <exception cref="ArgumentException">Faltan lista o producto, el precio supera la escala permitida o la versión es inválida.</exception>
    public Task<long> SavePriceAsync(
        SessionIdentity identity,
        PriceListProductChange change,
        CancellationToken cancellationToken
    )
    {
        Manage(identity);
        ArgumentNullException.ThrowIfNull(change);
        Edit(change.PriceListProductId, change.RowVersion);
        if (change.PriceListId <= 0 || change.ProductId <= 0 || !Amount(change.UnitPrice))
            throw new ArgumentException(
                "La lista, el producto y un precio válido de hasta cuatro decimales son obligatorios."
            );
        return store.SavePriceAsync(
            identity.CompanyId,
            identity.CompanyCode,
            identity.UserId,
            identity.SessionId,
            change with
            {
                StatusCode = Status(change.StatusCode),
            },
            cancellationToken
        );
    }

    /// <summary>Crea o actualiza una promoción validando que su tipo tenga una única regla aplicable.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="change">Vigencia, condición y beneficio de la promoción.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador de la promoción creada o actualizada.</returns>
    /// <exception cref="ArgumentException">Faltan producto o cantidad, la vigencia es inversa o la regla de promoción no coincide con su tipo.</exception>
    public Task<long> SavePromotionAsync(
        SessionIdentity identity,
        PromotionChange change,
        CancellationToken cancellationToken
    )
    {
        Manage(identity);
        ArgumentNullException.ThrowIfNull(change);
        Edit(change.PromotionId, change.RowVersion);
        if (change.ProductId <= 0 || !PositiveAmount(change.RequiredQuantity))
            throw new ArgumentException(
                "El producto y una cantidad requerida válida de hasta cuatro decimales son obligatorios."
            );
        if (change.ValidUntil < change.ValidFrom)
            throw new ArgumentException(
                "La vigencia hasta no puede ser anterior a la vigencia desde."
            );

        var normalized = change with
        {
            Code = Code(change.Code),
            Name = Required(change.Name, 150),
            Description = Optional(change.Description, 500),
            PromotionType = Required(change.PromotionType, 30).ToUpperInvariant(),
            StatusCode = Status(change.StatusCode),
        };
        ValidatePromotionRule(normalized);
        return store.SavePromotionAsync(
            identity.CompanyId,
            identity.CompanyCode,
            identity.UserId,
            identity.SessionId,
            normalized,
            cancellationToken
        );
    }

    /// <summary>Guarda un pedido nuevo o un borrador existente.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="change">Cliente, cabecera y renglones valorizados del pedido.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador del pedido creado o actualizado.</returns>
    /// <exception cref="ArgumentException">Faltan cliente o renglones válidos, los importes exceden sus límites o la relación con activos es inconsistente.</exception>
    public Task<long> SaveOrderAsync(
        SessionIdentity identity,
        OrderChange change,
        CancellationToken cancellationToken
    )
    {
        Manage(identity);
        ArgumentNullException.ThrowIfNull(change);
        Edit(change.OrderId, change.RowVersion);
        if (
            change.ClientId <= 0
            || change.PriceListId is <= 0
            || change.Details is not { Count: > 0 and <= 100 }
            || change.Details.Any(item =>
                item.ProductId <= 0
                || item.WarehouseId is <= 0
                || !PositiveAmount(item.Quantity)
                || !Amount(item.UnitPrice)
                || !Rate(item.DiscountRate)
                || !Rate(item.TaxRate)
                || item.PromotionId is <= 0
                || item.PromotionId is not null && item.DiscountRate != 0
            )
        )
            throw new ArgumentException(
                "Informá cliente y entre 1 y 100 renglones con importes de cuatro y porcentajes de seis decimales como máximo."
            );
        var assets = change.Assets ?? [];
        if (
            assets.Count > 100
            || assets.Any(item =>
                item.AssetId <= 0
                || item.ProductLineId <= 0
                || Required(item.LinkType, 30).ToUpperInvariant()
                    is not ("CLIENTE_SERVICIO" or "VENTA_ACTIVO" or "PRESTAMO" or "INTERCAMBIO")
                || Required(item.InboundMode, 30).ToUpperInvariant()
                    is not ("ENTREGA_CLIENTE" or "RETIRO_OXITIGRE" or "NO_APLICA")
                || Required(item.ReturnMode, 30).ToUpperInvariant()
                    is not ("RETIRO_CLIENTE" or "ENTREGA_OXITIGRE" or "NO_APLICA")
                || item.LinkType.Equals("PRESTAMO", StringComparison.OrdinalIgnoreCase)
                    && item.ExpectedReturnDate is null
                || item.ExpectedReturnDate < DateOnly.FromDateTime(change.OrderDateUtc)
                || string.IsNullOrWhiteSpace(item.Observation)
                || item.Observation.Trim().Length > 500
            )
        )
            throw new ArgumentException(
                "Los activos vinculados deben indicar activo, renglón, finalidad, ingreso, regreso y observación; un préstamo requiere devolución prevista."
            );
        if (!SubtotalFits(change.Details))
            throw new ArgumentException(
                "La suma de subtotales supera el máximo monetario permitido."
            );
        return store.SaveOrderAsync(
            identity.CompanyId,
            identity.CompanyCode,
            identity.UserId,
            identity.SessionId,
            change with
            {
                Currency = Currency(change.Currency),
                Observation = Optional(change.Observation, 500),
                Assets = assets
                    .Select(item =>
                        item with
                        {
                            LinkType = Required(item.LinkType, 30).ToUpperInvariant(),
                            InboundMode = Required(item.InboundMode, 30).ToUpperInvariant(),
                            ReturnMode = Required(item.ReturnMode, 30).ToUpperInvariant(),
                            Observation = Required(item.Observation, 500),
                        }
                    )
                    .ToList(),
            },
            cancellationToken
        );
    }

    /// <summary>Confirma el pedido y reserva stock.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="orderId">Identificador del pedido que se consulta o modifica.</param>
    /// <param name="rowVersion">Versión binaria usada para detectar modificaciones concurrentes.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    public Task ConfirmOrderAsync(
        SessionIdentity identity,
        long orderId,
        byte[] rowVersion,
        CancellationToken cancellationToken
    )
    {
        Manage(identity);
        Version(orderId, rowVersion);
        return store.ConfirmOrderAsync(
            identity.CompanyId,
            identity.CompanyCode,
            identity.UserId,
            identity.SessionId,
            orderId,
            rowVersion,
            cancellationToken
        );
    }

    /// <summary>Cancela el pedido y libera reservas.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="orderId">Identificador del pedido que se consulta o modifica.</param>
    /// <param name="rowVersion">Versión binaria usada para detectar modificaciones concurrentes.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    public Task CancelOrderAsync(
        SessionIdentity identity,
        long orderId,
        byte[] rowVersion,
        CancellationToken cancellationToken
    )
    {
        Manage(identity);
        Version(orderId, rowVersion);
        return store.CancelOrderAsync(
            identity.CompanyId,
            identity.CompanyCode,
            identity.UserId,
            identity.SessionId,
            orderId,
            rowVersion,
            cancellationToken
        );
    }

    /// <summary>Genera una venta interna desde el pedido confirmado.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="orderId">Identificador del pedido que se consulta o modifica.</param>
    /// <param name="saleDateUtc">Fecha UTC que quedará registrada en la venta generada.</param>
    /// <param name="rowVersion">Versión binaria usada para detectar modificaciones concurrentes.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador de la venta generada desde el pedido.</returns>
    public Task<long> CreateSaleAsync(
        SessionIdentity identity,
        long orderId,
        DateTime saleDateUtc,
        byte[] rowVersion,
        CancellationToken cancellationToken
    )
    {
        Manage(identity);
        Version(orderId, rowVersion);
        return store.CreateSaleAsync(
            identity.CompanyId,
            identity.CompanyCode,
            identity.UserId,
            identity.SessionId,
            orderId,
            saleDateUtc,
            rowVersion,
            cancellationToken
        );
    }

    /// <summary>Exige el permiso de gestión comercial antes de cualquier escritura.</summary>
    /// <param name="identity">Sesión autenticada cuyos permisos se comprueban.</param>
    /// <exception cref="UnauthorizedAccessException">La sesión no tiene permiso de gestión.</exception>
    private static void Manage(SessionIdentity identity) =>
        Ensure(identity, "COMERCIAL.VENTAS_GESTIONAR");

    /// <summary>Comprueba un permiso de la sesión sin aceptar una empresa enviada por el cliente.</summary>
    /// <param name="identity">Sesión autenticada que contiene los permisos otorgados.</param>
    /// <param name="permission">Código del permiso requerido para la operación.</param>
    /// <exception cref="ArgumentNullException">No se informó la identidad de sesión.</exception>
    /// <exception cref="UnauthorizedAccessException">La sesión no tiene el permiso solicitado.</exception>
    private static void Ensure(SessionIdentity identity, string permission)
    {
        ArgumentNullException.ThrowIfNull(identity);
        if (!identity.Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException($"La sesión no posee el permiso {permission}.");
    }

    /// <summary>Recorta y limita un texto obligatorio antes de enviarlo a persistencia.</summary>
    /// <param name="value">Texto proporcionado por el operador.</param>
    /// <param name="max">Cantidad máxima admitida de caracteres.</param>
    /// <returns>Texto no vacío y sin espacios en los extremos.</returns>
    /// <exception cref="ArgumentException">El texto está vacío o supera el límite.</exception>
    private static string Required(string? value, int max)
    {
        var result = value?.Trim();
        return string.IsNullOrWhiteSpace(result)
                ? throw new ArgumentException("El dato obligatorio no fue informado.")
            : result.Length > max
                ? throw new ArgumentException($"El valor supera {max} caracteres.")
            : result;
    }

    /// <summary>Normaliza un texto opcional, conservando nulo cuando no contiene datos.</summary>
    /// <param name="value">Texto opcional proporcionado por el operador.</param>
    /// <param name="max">Cantidad máxima admitida de caracteres.</param>
    /// <returns>Texto recortado o nulo si estaba vacío.</returns>
    /// <exception cref="ArgumentException">El texto informado supera el límite.</exception>
    private static string? Optional(string? value, int max) =>
        string.IsNullOrWhiteSpace(value) ? null : Required(value, max);

    /// <summary>Normaliza un código comercial obligatorio a mayúsculas.</summary>
    /// <param name="value">Código ingresado por el operador.</param>
    /// <returns>Código recortado de hasta treinta caracteres.</returns>
    /// <exception cref="ArgumentException">El código está vacío o es demasiado largo.</exception>
    private static string Code(string? value) => Required(value, 30).ToUpperInvariant();

    /// <summary>Valida un código de moneda de tres letras.</summary>
    /// <param name="value">Moneda ingresada en la lista o el pedido.</param>
    /// <returns>Código de moneda en mayúsculas.</returns>
    /// <exception cref="ArgumentException">La moneda no contiene exactamente tres letras.</exception>
    private static string Currency(string? value) =>
        Required(value, 3).ToUpperInvariant() is var code
        && code.Length == 3
        && code.All(char.IsLetter)
            ? code
            : throw new ArgumentException("La moneda debe contener tres letras.");

    /// <summary>Restringe el estado editable a activo o inactivo.</summary>
    /// <param name="value">Estado ingresado por el operador.</param>
    /// <returns>Código de estado en mayúsculas.</returns>
    /// <exception cref="ArgumentException">El estado no es ACTIVO ni INACTIVO.</exception>
    private static string Status(string? value) =>
        Code(value) is var status && status is "ACTIVO" or "INACTIVO"
            ? status
            : throw new ArgumentException("El estado debe ser ACTIVO o INACTIVO.");

    /// <summary>Exige que cada tipo de promoción informe solo los campos de su propia regla.</summary>
    /// <param name="change">Promoción ya normalizada cuya regla se valida.</param>
    /// <exception cref="ArgumentException">La regla es incompatible con el tipo elegido.</exception>
    private static void ValidatePromotionRule(PromotionChange change)
    {
        var valid = change.PromotionType switch
        {
            "CANTIDAD_PAGADA" => change.PaidQuantity is { } paid
                && Amount(paid)
                && paid < change.RequiredQuantity
                && change.DiscountedQuantity is null
                && change.DiscountRate is null
                && change.PackagePrice is null,
            "PORCENTAJE_UNIDADES" => change.PaidQuantity is null
                && change.DiscountedQuantity is { } discounted
                && PositiveAmount(discounted)
                && discounted <= change.RequiredQuantity
                && change.DiscountRate is { } rate
                && Rate(rate)
                && rate > 0
                && change.PackagePrice is null,
            "PRECIO_PAQUETE" => change.PaidQuantity is null
                && change.DiscountedQuantity is null
                && change.DiscountRate is null
                && change.PackagePrice is { } package
                && Amount(package),
            _ => false,
        };
        if (!valid)
            throw new ArgumentException(
                "La regla no coincide con el tipo de promoción seleccionado."
            );
    }

    /// <summary>Comprueba que un importe cabe en el decimal monetario usado por SQL.</summary>
    /// <param name="value">Importe no negativo a validar.</param>
    /// <returns>Verdadero si cabe en decimal(19,4).</returns>
    private static bool Amount(decimal value) => Fits(value, Decimal19_4Max, 4);

    /// <summary>Comprueba que una cantidad es positiva y cabe en decimal(19,4).</summary>
    /// <param name="value">Cantidad requerida por la operación.</param>
    /// <returns>Verdadero si la cantidad es positiva y representable.</returns>
    private static bool PositiveAmount(decimal value) => value > 0 && Amount(value);

    /// <summary>Comprueba un porcentaje expresado como fracción entre cero y uno.</summary>
    /// <param name="value">Descuento o impuesto a validar.</param>
    /// <returns>Verdadero si el porcentaje cabe en decimal(9,6).</returns>
    private static bool Rate(decimal value) => value <= 1 && Fits(value, Decimal9_6Max, 6);

    /// <summary>Comprueba signo, máximo y escala sin redondear silenciosamente el dato.</summary>
    /// <param name="value">Número decimal a validar.</param>
    /// <param name="maximum">Mayor valor permitido por el campo de destino.</param>
    /// <param name="scale">Cantidad máxima de decimales permitidos.</param>
    /// <returns>Verdadero si el valor puede persistirse sin pérdida de precisión.</returns>
    private static bool Fits(decimal value, decimal maximum, int scale) =>
        value >= 0 && value <= maximum && decimal.Round(value, scale) == value;

    /// <summary>Agrupa renglones equivalentes y previene desbordes en cantidades y subtotales.</summary>
    /// <param name="details">Renglones de pedido ya validados individualmente.</param>
    /// <returns>Verdadero si todas las sumas y productos caben en decimal(19,4).</returns>
    private static bool SubtotalFits(IReadOnlyList<OrderDetailChange> details)
    {
        var quantities =
            new Dictionary<
                (
                    long Product,
                    long? Warehouse,
                    long? Promotion,
                    decimal Price,
                    decimal Discount,
                    decimal Tax
                ),
                decimal
            >();
        foreach (var detail in details)
        {
            var key = (
                detail.ProductId,
                detail.WarehouseId,
                detail.PromotionId,
                detail.UnitPrice,
                detail.DiscountRate,
                detail.TaxRate
            );
            quantities.TryGetValue(key, out var quantity);
            if (detail.Quantity > Decimal19_4Max - quantity)
                return false;
            quantities[key] = quantity + detail.Quantity;
        }

        var total = 0m;
        foreach (var (key, quantity) in quantities)
        {
            if (key.Price != 0 && quantity > Decimal19_4Max / key.Price)
                return false;
            var subtotal = decimal.Round(quantity * key.Price, 4, MidpointRounding.AwayFromZero);
            if (subtotal > Decimal19_4Max - total)
                return false;
            total += subtotal;
        }
        return true;
    }

    /// <summary>Distingue un alta de una edición y exige versión para editar.</summary>
    /// <param name="id">Identificador nulo en altas o positivo en ediciones.</param>
    /// <param name="version">Versión de ocho bytes requerida en ediciones.</param>
    /// <exception cref="ArgumentException">El identificador o la versión no son válidos.</exception>
    private static void Edit(long? id, byte[]? version)
    {
        if (id is <= 0)
            throw new ArgumentException("El identificador no es válido.");
        if (id is not null && version is not { Length: 8 })
            throw new ArgumentException("La versión del registro no es válida.");
    }

    /// <summary>Valida el identificador y la versión usados por acciones sobre pedidos existentes.</summary>
    /// <param name="id">Identificador positivo del pedido.</param>
    /// <param name="version">Versión de ocho bytes para controlar concurrencia.</param>
    /// <exception cref="ArgumentException">El pedido o su versión no son válidos.</exception>
    private static void Version(long id, byte[]? version)
    {
        if (id <= 0 || version is not { Length: 8 })
            throw new ArgumentException("El pedido o su versión no son válidos.");
    }
}
