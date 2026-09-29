/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Inventory.TraceabilityService
Archivo: TraceabilityService.cs | Versión: 1.0.0 | Fecha: 2026-08-24 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Autoriza y valida las operaciones industriales trazables de Inventario.
Historial: 1.0.0 | 2026-08-24 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
using OxiTigre.BLL.Security;

namespace OxiTigre.BLL.Inventory;

/// <summary>Orquesta trazabilidad sin aceptar empresa, usuario ni sesión desde el cliente.</summary>
public sealed class TraceabilityService(ITraceabilityStore store)
{
    private const decimal Maximum = 999999999999999.9999m;

    /// <summary>Obtiene el estado actual almacenado y su historial.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Lotes, activos, mediciones, transformaciones, incidencias, mantenimientos y préstamos de la empresa.</returns>
    public Task<TraceabilitySnapshot> GetAsync(
        SessionIdentity identity,
        CancellationToken cancellationToken
    )
    {
        Ensure(identity, "INVENTARIO.CONSULTAR");
        return store.GetAsync(
            identity.CompanyId,
            identity.CompanyCode,
            identity.UserId,
            identity.SessionId,
            cancellationToken
        );
    }

    /// <summary>Registra un activo propiedad de un cliente y conserva su ubicación inicial.</summary>
    /// <param name="identity">Identidad autenticada que aporta empresa, usuario y sesión.</param>
    /// <param name="change">Cliente, producto, identificación física y situación de custodia.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación.</param>
    /// <returns>Identificador interno del activo creado.</returns>
    /// <exception cref="UnauthorizedAccessException">La sesión no posee permiso para gestionar inventario.</exception>
    /// <exception cref="ArgumentException">Los datos no identifican un activo o una ubicación inicial válida.</exception>
    public Task<long> SaveClientAssetAsync(
        SessionIdentity identity,
        ClientAssetChange change,
        CancellationToken cancellationToken
    )
    {
        Manage(identity);
        ArgumentNullException.ThrowIfNull(change);

        if (
            change.ClientId <= 0
            || change.ProductId <= 0
            || change.Capacity is { } capacity && !Amount(capacity)
            || change.IsInCustody != change.WarehouseId.HasValue
            || change.WarehouseId <= 0
        )
        {
            throw new ArgumentException(
                "Informá cliente, producto, capacidad y ubicación inicial válidos."
            );
        }

        return store.SaveClientAssetAsync(
            identity.CompanyId,
            identity.CompanyCode,
            identity.UserId,
            identity.SessionId,
            change with
            {
                SerialNumber = Optional(change.SerialNumber, 100)?.ToUpperInvariant(),
                AssetType = Code(change.AssetType),
                CapacityUnit = Optional(change.CapacityUnit, 20),
                ConditionCode = Code(change.ConditionCode),
                Observation = Required(change.Observation, 500),
            },
            cancellationToken
        );
    }

    /// <summary>Confirma el ingreso o la entrega de un activo propiedad de un cliente.</summary>
    /// <param name="identity">Identidad autenticada que aporta empresa, usuario y sesión.</param>
    /// <param name="change">Acción de custodia, depósito, motivo y versión leída.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación.</param>
    /// <returns>Identificador del activo actualizado.</returns>
    /// <exception cref="UnauthorizedAccessException">La sesión no posee permiso para gestionar inventario.</exception>
    /// <exception cref="ArgumentException">La acción, el depósito o la versión no son válidos.</exception>
    public Task<long> ChangeClientAssetCustodyAsync(
        SessionIdentity identity,
        ClientAssetCustodyChange change,
        CancellationToken cancellationToken
    )
    {
        Manage(identity);
        ArgumentNullException.ThrowIfNull(change);
        Version(change.AssetId, change.RowVersion);

        var action = Code(change.Action);
        if (
            action is not ("INGRESAR" or "ENTREGAR")
            || action == "INGRESAR" && change.WarehouseId <= 0
            || action == "ENTREGAR" && change.WarehouseId is not null
        )
        {
            throw new ArgumentException("La acción y el depósito no son compatibles.");
        }

        return store.ChangeClientAssetCustodyAsync(
            identity.CompanyId,
            identity.CompanyCode,
            identity.UserId,
            identity.SessionId,
            change with
            {
                Action = action,
                Observation = Required(change.Observation, 500),
            },
            cancellationToken
        );
    }

    /// <summary>Registra una medición append-only.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="change">Activo, fecha, valor, unidad, método y origen de la medición que se registrará.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador de la medición registrada.</returns>
    /// <exception cref="ArgumentException">El activo, valor, unidad, método o fecha de la medición no son válidos.</exception>
    public Task<long> CreateMeasurementAsync(
        SessionIdentity identity,
        AssetMeasurementChange change,
        CancellationToken cancellationToken
    )
    {
        Manage(identity);
        ArgumentNullException.ThrowIfNull(change);
        if (change.AssetId <= 0 || !Amount(change.Value))
            throw new ArgumentException("El activo y el valor medido son obligatorios.");
        return store.CreateMeasurementAsync(
            identity.CompanyId,
            identity.CompanyCode,
            identity.UserId,
            identity.SessionId,
            change with
            {
                MeasurementType = Code(change.MeasurementType),
                Unit = Required(change.Unit, 20),
                Method = Code(change.Method),
                Source = Source(change.Source),
                DeviceReference = Optional(change.DeviceReference, 100),
                Observation = Optional(change.Observation, 500),
            },
            cancellationToken
        );
    }

    /// <summary>Confirma un fraccionamiento conservando origen, destinos y merma.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="change">Origen, cantidad consumida, merma y destinos del fraccionamiento.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador de la transformación o fraccionamiento confirmado.</returns>
    /// <exception cref="ArgumentException">El origen, las cantidades, la merma o los destinos del fraccionamiento no son válidos.</exception>
    public Task<long> CreateTransformationAsync(
        SessionIdentity identity,
        TransformationChange change,
        CancellationToken cancellationToken
    )
    {
        Manage(identity);
        ArgumentNullException.ThrowIfNull(change);
        if (
            change.SourceLotId <= 0
            || change.ContentProductId <= 0
            || !Positive(change.SourceQuantity)
            || !Amount(change.LossQuantity)
            || change.Destinations is not { Count: > 0 and <= 100 }
            || change.Destinations.Any(item =>
                item.AssetId <= 0 || item.LotId <= 0 || !Positive(item.Quantity)
            )
            || change.Destinations.Sum(item => item.Quantity) + change.LossQuantity
                != change.SourceQuantity
        )
            throw new ArgumentException(
                "La cantidad de origen debe ser igual a destinos más merma."
            );
        return store.CreateTransformationAsync(
            identity.CompanyId,
            identity.CompanyCode,
            identity.UserId,
            identity.SessionId,
            change with
            {
                Method = Code(change.Method),
                LossReason = Optional(change.LossReason, 500),
                Observation = Optional(change.Observation, 1000),
            },
            cancellationToken
        );
    }

    /// <summary>Confirma un incidente y su eventual merma.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="change">Activo o lote afectado, tipo, fecha, motivo y observación de la incidencia.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador de la incidencia registrada.</returns>
    /// <exception cref="ArgumentException">La referencia afectada, el tipo, motivo o fecha de la incidencia no son válidos.</exception>
    public Task<long> CreateIncidentAsync(
        SessionIdentity identity,
        IncidentChange change,
        CancellationToken cancellationToken
    )
    {
        Manage(identity);
        ArgumentNullException.ThrowIfNull(change);
        if (change.ProductId <= 0 || change.WarehouseId <= 0 || !Amount(change.LossQuantity))
            throw new ArgumentException("Producto, depósito y pérdida válida son obligatorios.");
        return store.CreateIncidentAsync(
            identity.CompanyId,
            identity.CompanyCode,
            identity.UserId,
            identity.SessionId,
            change with
            {
                IncidentType = Code(change.IncidentType),
                MeasurementMethod = Optional(change.MeasurementMethod, 30)?.ToUpperInvariant(),
                Cause = Required(change.Cause, 1000),
                ActionTaken = Optional(change.ActionTaken, 1000),
                EvidenceReference = Optional(change.EvidenceReference, 500),
            },
            cancellationToken
        );
    }

    /// <summary>Inicia mantenimiento y bloquea el activo.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="change">Activo, tipo, fechas y observación del mantenimiento.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador del mantenimiento registrado.</returns>
    /// <exception cref="ArgumentException">El activo, tipo o fecha del mantenimiento no son válidos.</exception>
    public Task<long> CreateMaintenanceAsync(
        SessionIdentity identity,
        MaintenanceChange change,
        CancellationToken cancellationToken
    )
    {
        Manage(identity);
        ArgumentNullException.ThrowIfNull(change);
        if (change.AssetId <= 0 || change.Cost is { } cost && !Amount(cost))
            throw new ArgumentException("El activo y el costo deben ser válidos.");
        return store.CreateMaintenanceAsync(
            identity.CompanyId,
            identity.CompanyCode,
            identity.UserId,
            identity.SessionId,
            change with
            {
                MaintenanceType = Code(change.MaintenanceType),
                WorkDescription = Required(change.WorkDescription, 1000),
            },
            cancellationToken
        );
    }

    /// <summary>Completa mantenimiento y deja el activo disponible o dado de baja según resultado.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="change">Fecha de cierre, resultado, componentes reemplazados, costo y versión del mantenimiento.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    /// <exception cref="ArgumentException">El mantenimiento, la versión o los datos de cierre no son válidos.</exception>
    public Task CompleteMaintenanceAsync(
        SessionIdentity identity,
        MaintenanceCompletion change,
        CancellationToken cancellationToken
    )
    {
        Manage(identity);
        ArgumentNullException.ThrowIfNull(change);
        Version(change.MaintenanceId, change.RowVersion);
        if (change.Cost is { } cost && !Amount(cost))
            throw new ArgumentException("El costo no es válido.");
        return store.CompleteMaintenanceAsync(
            identity.CompanyId,
            identity.CompanyCode,
            identity.UserId,
            identity.SessionId,
            change with
            {
                Result = Required(change.Result, 1000),
                OldComponent = Optional(change.OldComponent, 200),
                NewComponent = Optional(change.NewComponent, 200),
                CertificateReference = Optional(change.CertificateReference, 500),
            },
            cancellationToken
        );
    }

    /// <summary>Confirma préstamo y snapshot de salida.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="change">Destino, entrega, devolución prevista y activos incluidos en el préstamo.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador del préstamo registrado.</returns>
    /// <exception cref="ArgumentException">El destino, la fecha prevista o los activos del préstamo no son válidos.</exception>
    public Task<long> CreateLoanAsync(
        SessionIdentity identity,
        LoanChange change,
        CancellationToken cancellationToken
    )
    {
        Manage(identity);
        ArgumentNullException.ThrowIfNull(change);
        var destination = Destination(
            change.DestinationType,
            change.DestinationBranchId,
            change.DestinationClientId,
            change.ExternalDestination
        );
        if (
            change.Assets is not { Count: > 0 and <= 100 }
            || change.Assets.Any(item =>
                item.AssetId <= 0 || item.Quantity is { } quantity && !Amount(quantity)
            )
            || change.ExpectedReturnDateUtc < change.DepartureDateUtc
            || destination == "INTEREMPRESA"
                && string.IsNullOrWhiteSpace(change.DestinationCompanyCode)
        )
            throw new ArgumentException("Informá activos, destino y fechas de préstamo válidos.");
        return store.CreateLoanAsync(
            identity.CompanyId,
            identity.CompanyCode,
            identity.UserId,
            identity.SessionId,
            change with
            {
                DestinationType = destination,
                DeliveryMode = Delivery(change.DeliveryMode),
                ExternalDestination = Optional(change.ExternalDestination, 200),
                Observation = Optional(change.Observation, 1000),
                DestinationCompanyCode = Optional(change.DestinationCompanyCode, 30)
                    ?.ToUpperInvariant(),
                Assets = change
                    .Assets.Select(item => item with { ConditionCode = Code(item.ConditionCode) })
                    .ToList(),
            },
            cancellationToken
        );
    }

    /// <summary>Confirma devolución con snapshot y diferencias.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="change">Préstamo, fecha, activos devueltos y observación de la devolución.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    /// <exception cref="ArgumentException">El préstamo, la fecha o los activos devueltos no son válidos.</exception>
    public Task ReturnLoanAsync(
        SessionIdentity identity,
        LoanReturnChange change,
        CancellationToken cancellationToken
    )
    {
        Manage(identity);
        ArgumentNullException.ThrowIfNull(change);
        Version(change.LoanId, change.RowVersion);
        if (
            change.Assets is not { Count: > 0 and <= 100 }
            || change.Assets.Any(item =>
                item.LoanLineId <= 0 || item.Quantity is { } quantity && !Amount(quantity)
            )
        )
            throw new ArgumentException("La devolución debe incluir renglones válidos.");
        return store.ReturnLoanAsync(
            identity.CompanyId,
            identity.CompanyCode,
            identity.UserId,
            identity.SessionId,
            change with
            {
                Observation = Optional(change.Observation, 1000),
                Assets = change
                    .Assets.Select(item =>
                        item with
                        {
                            ConditionCode = Code(item.ConditionCode),
                            Observation = Optional(item.Observation, 500),
                        }
                    )
                    .ToList(),
            },
            cancellationToken
        );
    }

    /// <summary>Exige el permiso de gestión antes de registrar operaciones industriales.</summary>
    /// <param name="identity">Sesión autenticada que aporta los permisos.</param>
    /// <exception cref="UnauthorizedAccessException">La sesión no puede gestionar inventario.</exception>
    private static void Manage(SessionIdentity identity) =>
        Ensure(identity, "INVENTARIO.GESTIONAR");

    /// <summary>Comprueba un permiso de la sesión sin aceptar contexto de seguridad del cliente.</summary>
    /// <param name="identity">Sesión autenticada a comprobar.</param>
    /// <param name="permission">Permiso requerido para la operación.</param>
    /// <exception cref="ArgumentNullException">No se informó una sesión.</exception>
    /// <exception cref="UnauthorizedAccessException">La sesión carece del permiso.</exception>
    private static void Ensure(SessionIdentity identity, string permission)
    {
        ArgumentNullException.ThrowIfNull(identity);
        if (!identity.Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException($"La sesión no posee el permiso {permission}.");
    }

    /// <summary>Comprueba que una magnitud cabe en decimal(19,4) sin redondeo implícito.</summary>
    /// <param name="value">Cantidad o medición a validar.</param>
    /// <returns>Verdadero si el valor es no negativo, representable y tiene hasta cuatro decimales.</returns>
    private static bool Amount(decimal value) =>
        value >= 0 && value <= Maximum && decimal.Round(value, 4) == value;

    /// <summary>Exige una cantidad estrictamente positiva dentro del rango persistible.</summary>
    /// <param name="value">Cantidad a validar.</param>
    /// <returns>Verdadero si es positiva y cabe en decimal(19,4).</returns>
    private static bool Positive(decimal value) => value > 0 && Amount(value);

    /// <summary>Recorta un dato obligatorio y evita truncarlo al persistirlo.</summary>
    /// <param name="value">Texto ingresado por el operador.</param>
    /// <param name="max">Cantidad máxima de caracteres.</param>
    /// <returns>Texto no vacío y sin espacios extremos.</returns>
    /// <exception cref="ArgumentException">El texto falta o supera el máximo.</exception>
    private static string Required(string? value, int max) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("El dato obligatorio no fue informado.")
        : value.Trim() is var result && result.Length <= max ? result
        : throw new ArgumentException($"El valor supera {max} caracteres.");

    /// <summary>Convierte un dato opcional vacío a nulo y limita el texto informado.</summary>
    /// <param name="value">Texto opcional ingresado.</param>
    /// <param name="max">Cantidad máxima de caracteres.</param>
    /// <returns>Texto recortado o nulo cuando no hay contenido.</returns>
    /// <exception cref="ArgumentException">El texto informado supera el máximo.</exception>
    private static string? Optional(string? value, int max) =>
        string.IsNullOrWhiteSpace(value) ? null : Required(value, max);

    /// <summary>Normaliza un código industrial obligatorio a mayúsculas.</summary>
    /// <param name="value">Código recibido desde la interfaz o la API.</param>
    /// <returns>Código normalizado de hasta treinta caracteres.</returns>
    /// <exception cref="ArgumentException">El código falta o es demasiado largo.</exception>
    private static string Code(string? value) => Required(value, 30).ToUpperInvariant();

    /// <summary>Restringe el origen declarado de una medición a los valores conocidos.</summary>
    /// <param name="value">Origen de la medición informado por el operador.</param>
    /// <returns>MANUAL, PESAJE, CALCULADO o SENSOR en mayúsculas.</returns>
    /// <exception cref="ArgumentException">El origen no está permitido.</exception>
    private static string Source(string? value) =>
        Code(value) is var source && source is "MANUAL" or "PESAJE" or "CALCULADO" or "SENSOR"
            ? source
            : throw new ArgumentException("El origen de medición no es válido.");

    /// <summary>Valida quién realiza la entrega física de un préstamo.</summary>
    /// <param name="value">Modalidad de entrega informada.</param>
    /// <returns>Código normalizado de entrega propia, retiro del destinatario o tercero.</returns>
    /// <exception cref="ArgumentException">La modalidad no está permitida.</exception>
    private static string Delivery(string? value) =>
        Code(value) is var mode && mode is "ENTREGA_PROPIA" or "RETIRO_DESTINATARIO" or "TERCERO"
            ? mode
            : throw new ArgumentException("La modalidad de entrega no es válida.");

    /// <summary>Exige que el tipo de destinatario coincida con un único identificador o nombre externo.</summary>
    /// <param name="value">Tipo de destino: sucursal, cliente, tercero o interempresa.</param>
    /// <param name="branchId">Sucursal requerida solo para destino sucursal.</param>
    /// <param name="clientId">Cliente requerido solo para destino cliente.</param>
    /// <param name="external">Referencia externa requerida para tercero o interempresa.</param>
    /// <returns>Tipo de destino normalizado.</returns>
    /// <exception cref="ArgumentException">El tipo y los datos del destinatario no coinciden.</exception>
    private static string Destination(
        string? value,
        long? branchId,
        long? clientId,
        string? external
    ) =>
        Code(value) is var type
        && (
            (
                type == "SUCURSAL"
                && branchId > 0
                && clientId is null
                && string.IsNullOrWhiteSpace(external)
            )
            || (
                type == "CLIENTE"
                && clientId > 0
                && branchId is null
                && string.IsNullOrWhiteSpace(external)
            )
            || (
                type is "TERCERO" or "INTEREMPRESA"
                && branchId is null
                && clientId is null
                && !string.IsNullOrWhiteSpace(external)
            )
        )
            ? type
            : throw new ArgumentException("El tipo y el destino del préstamo no coinciden.");

    /// <summary>Verifica la referencia y versión usada en operaciones sobre registros existentes.</summary>
    /// <param name="id">Identificador positivo del registro.</param>
    /// <param name="version">Versión binaria de ocho bytes para controlar concurrencia.</param>
    /// <exception cref="ArgumentException">El registro o su versión son inválidos.</exception>
    private static void Version(long id, byte[]? version)
    {
        if (id <= 0 || version is not { Length: 8 })
            throw new ArgumentException("El registro o su versión no son válidos.");
    }
}
