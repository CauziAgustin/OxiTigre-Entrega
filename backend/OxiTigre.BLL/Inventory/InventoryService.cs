/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Inventory.InventoryService
Archivo: InventoryService.cs | Versión: 1.1.0 | Fecha: 2026-08-24 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Autoriza, normaliza y valida maestros, mínimos y movimientos de Inventario.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-24 | FABRICA | Agustin Omar Cauzi | Validación del tipo de trazabilidad de producto.
===============================================================================
*/
using OxiTigre.BLL.Security;

namespace OxiTigre.BLL.Inventory;

/// <summary>Orquesta Inventario preservando aislamiento, cantidades y concurrencia.</summary>
public sealed class InventoryService(IInventoryStore store)
{
    /// <summary>Obtiene todos los datos visibles de Inventario.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Unidades, categorías, productos, depósitos, ubicaciones, existencias y movimientos de la empresa.</returns>
    public Task<InventorySnapshot> GetAsync(
        SessionIdentity identity,
        CancellationToken cancellationToken
    )
    {
        EnsurePermission(identity, "INVENTARIO.CONSULTAR");
        return store.GetAsync(
            identity.CompanyId,
            identity.CompanyCode,
            identity.UserId,
            identity.SessionId,
            cancellationToken
        );
    }

    /// <summary>Crea o modifica una unidad de medida.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="change">Código, descripción y estado de la unidad de medida.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador de la unidad de medida creada o actualizada.</returns>
    public Task<long> SaveMeasurementUnitAsync(
        SessionIdentity identity,
        MeasurementUnitChange change,
        CancellationToken cancellationToken
    )
    {
        EnsureManage(identity);
        ArgumentNullException.ThrowIfNull(change);
        ValidateEdit(change.MeasurementUnitId, change.RowVersion);
        return store.SaveMeasurementUnitAsync(
            identity.CompanyCode,
            identity.UserId,
            identity.SessionId,
            change with
            {
                Code = Code(change.Code, 30),
                Name = Required(change.Name, 100, "El nombre"),
                Symbol = Required(change.Symbol, 20, "El símbolo"),
                StatusCode = Status(change.StatusCode),
            },
            cancellationToken
        );
    }

    /// <summary>Crea o modifica una categoría.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="change">Código, nombre y estado de la categoría de productos.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador de la categoría creada o actualizada.</returns>
    public Task<long> SaveCategoryAsync(
        SessionIdentity identity,
        ProductCategoryChange change,
        CancellationToken cancellationToken
    )
    {
        EnsureManage(identity);
        ArgumentNullException.ThrowIfNull(change);
        ValidateEdit(change.ProductCategoryId, change.RowVersion);
        return store.SaveCategoryAsync(
            identity.CompanyCode,
            identity.UserId,
            identity.SessionId,
            change with
            {
                Code = Code(change.Code, 30),
                Name = Required(change.Name, 150, "El nombre"),
                Description = Optional(change.Description, 500),
                StatusCode = Status(change.StatusCode),
            },
            cancellationToken
        );
    }

    /// <summary>Crea o modifica un producto.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="change">Código, categoría, unidad y datos comerciales del producto.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador del producto creado o actualizado.</returns>
    /// <exception cref="ArgumentException">El código, la categoría, la unidad, el estado o la versión del producto no son válidos.</exception>
    public Task<long> SaveProductAsync(
        SessionIdentity identity,
        ProductChange change,
        CancellationToken cancellationToken
    )
    {
        EnsureManage(identity);
        ArgumentNullException.ThrowIfNull(change);
        ValidateEdit(change.ProductId, change.RowVersion);
        if (change.ProductCategoryId <= 0 || change.MeasurementUnitId <= 0)
            throw new ArgumentException(
                "La categoría y la unidad son obligatorias.",
                nameof(change)
            );
        var itemType = Code(change.ItemType, 20);
        if (itemType is not ("PRODUCTO" or "SERVICIO"))
            throw new ArgumentException("El tipo debe ser PRODUCTO o SERVICIO.", nameof(change));
        var trackingType = Code(change.TrackingType, 20);
        if (trackingType is not ("NINGUNA" or "LOTE" or "SERIE" or "SERIE_LOTE"))
            throw new ArgumentException(
                "La trazabilidad debe ser NINGUNA, LOTE, SERIE o SERIE_LOTE.",
                nameof(change)
            );
        if (
            itemType == "SERVICIO"
            && (
                trackingType != "NINGUNA"
                || change.IsReusable
                || change.AllowsLoans
                || change.RequiresMaintenance
                || change.AllowsMeasurements
            )
        )
            throw new ArgumentException(
                "Un servicio no administra trazabilidad, préstamos, mediciones ni mantenimiento de stock.",
                nameof(change)
            );
        return store.SaveProductAsync(
            identity.CompanyId,
            identity.CompanyCode,
            identity.UserId,
            identity.SessionId,
            change with
            {
                Name = Required(change.Name, 200, "El nombre"),
                Description = Optional(change.Description, 1000),
                Barcode = Optional(change.Barcode, 80),
                StatusCode = Status(change.StatusCode),
                ItemType = itemType,
                TrackingType = trackingType,
            },
            cancellationToken
        );
    }

    /// <summary>Crea o modifica un depósito.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="change">Código, nombre, domicilio y estado del depósito.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador del depósito creado o actualizado.</returns>
    /// <exception cref="ArgumentException">El código, nombre, estado o versión del depósito no son válidos.</exception>
    public Task<long> SaveWarehouseAsync(
        SessionIdentity identity,
        WarehouseChange change,
        CancellationToken cancellationToken
    )
    {
        EnsureManage(identity);
        ArgumentNullException.ThrowIfNull(change);
        ValidateEdit(change.WarehouseId, change.RowVersion);
        if (change.BranchId is <= 0)
            throw new ArgumentException("La sucursal no es válida.", nameof(change));
        return store.SaveWarehouseAsync(
            identity.CompanyId,
            identity.CompanyCode,
            identity.UserId,
            identity.SessionId,
            change with
            {
                Code = Code(change.Code, 30),
                Name = Required(change.Name, 150, "El nombre"),
                Address = Optional(change.Address, 250),
                Description = Optional(change.Description, 500),
                StatusCode = Status(change.StatusCode),
            },
            cancellationToken
        );
    }

    /// <summary>Crea o modifica una ubicación.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="change">Depósito, código, nombre y estado de la ubicación.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador de la ubicación creada o actualizada.</returns>
    /// <exception cref="ArgumentException">Falta un depósito válido o el código, nombre, estado o versión de la ubicación son inválidos.</exception>
    public Task<long> SaveLocationAsync(
        SessionIdentity identity,
        WarehouseLocationChange change,
        CancellationToken cancellationToken
    )
    {
        EnsureManage(identity);
        ArgumentNullException.ThrowIfNull(change);
        ValidateEdit(change.LocationId, change.RowVersion);
        if (change.WarehouseId <= 0)
            throw new ArgumentException("El depósito es obligatorio.", nameof(change));
        return store.SaveLocationAsync(
            identity.CompanyId,
            identity.CompanyCode,
            identity.UserId,
            identity.SessionId,
            change with
            {
                Code = Code(change.Code, 30),
                Name = Required(change.Name, 150, "El nombre"),
                Description = Optional(change.Description, 500),
                StatusCode = Status(change.StatusCode),
            },
            cancellationToken
        );
    }

    /// <summary>Actualiza el mínimo de alerta sin modificar el saldo.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="change">Producto, depósito y cantidad mínima que activa una alerta de reposición.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    /// <exception cref="ArgumentException">El producto, depósito o umbral mínimo no son válidos.</exception>
    public Task UpdateMinimumStockAsync(
        SessionIdentity identity,
        MinimumStockChange change,
        CancellationToken cancellationToken
    )
    {
        EnsureManage(identity);
        ArgumentNullException.ThrowIfNull(change);
        ValidateVersion(change.RowVersion);
        if (change.StockBalanceId <= 0 || change.MinimumStock < 0)
            throw new ArgumentException(
                "La existencia y el mínimo deben ser válidos.",
                nameof(change)
            );
        return store.UpdateMinimumStockAsync(
            identity.CompanyId,
            identity.CompanyCode,
            identity.UserId,
            identity.SessionId,
            change,
            cancellationToken
        );
    }

    /// <summary>Valida y confirma un movimiento de existencias.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="change">Producto, depósito, cantidad, motivo y referencias del movimiento.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador del movimiento de inventario confirmado.</returns>
    /// <exception cref="ArgumentException">El producto, depósito, cantidad o motivo del movimiento no son válidos.</exception>
    public Task<long> CreateMovementAsync(
        SessionIdentity identity,
        InventoryMovementChange change,
        CancellationToken cancellationToken
    )
    {
        EnsureManage(identity);
        ArgumentNullException.ThrowIfNull(change);
        var type = Code(change.MovementType, 30);
        if (
            type
            is not ("ENTRADA" or "SALIDA" or "TRANSFERENCIA" or "AJUSTE_ENTRADA" or "AJUSTE_SALIDA")
        )
            throw new ArgumentException("El tipo de movimiento no es válido.", nameof(change));
        if (
            change.Details is not { Count: > 0 and <= 100 }
            || change.Details.Any(detail => detail.ProductId <= 0 || detail.Quantity <= 0)
        )
            throw new ArgumentException(
                "Informá entre 1 y 100 renglones con cantidades positivas.",
                nameof(change)
            );
        return store.CreateMovementAsync(
            identity.CompanyId,
            identity.CompanyCode,
            identity.UserId,
            identity.SessionId,
            change with
            {
                MovementType = type,
                Observation = Optional(change.Observation, 500),
            },
            cancellationToken
        );
    }

    /// <summary>Exige permiso de gestión antes de modificar catálogos o existencias.</summary>
    /// <param name="identity">Sesión autenticada cuyos permisos se comprueban.</param>
    /// <exception cref="UnauthorizedAccessException">La sesión no puede gestionar inventario.</exception>
    private static void EnsureManage(SessionIdentity identity) =>
        EnsurePermission(identity, "INVENTARIO.GESTIONAR");

    /// <summary>Autoriza una operación usando permisos de la sesión, no datos del cliente.</summary>
    /// <param name="identity">Sesión autenticada que contiene los permisos.</param>
    /// <param name="permission">Permiso requerido para consultar o gestionar.</param>
    /// <exception cref="ArgumentNullException">No se informó una sesión.</exception>
    /// <exception cref="UnauthorizedAccessException">La sesión carece del permiso.</exception>
    private static void EnsurePermission(SessionIdentity identity, string permission)
    {
        ArgumentNullException.ThrowIfNull(identity);
        if (!identity.Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException($"La sesión no posee el permiso {permission}.");
    }

    /// <summary>Recorta un campo obligatorio sin truncarlo antes de persistirlo.</summary>
    /// <param name="value">Texto ingresado por el operador.</param>
    /// <param name="length">Longitud máxima de la columna de destino.</param>
    /// <param name="name">Nombre funcional usado en el error.</param>
    /// <returns>Texto no vacío y sin espacios extremos.</returns>
    /// <exception cref="ArgumentException">El valor falta o supera el límite.</exception>
    private static string Required(string? value, int length, string name) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException($"{name} es obligatorio.")
            : Optional(value, length)!;

    /// <summary>Representa un campo opcional vacío con nulo y limita el texto informado.</summary>
    /// <param name="value">Texto opcional recibido.</param>
    /// <param name="length">Longitud máxima de la columna de destino.</param>
    /// <returns>Texto recortado o nulo cuando no contiene datos.</returns>
    /// <exception cref="ArgumentException">El valor informado supera el límite.</exception>
    private static string? Optional(string? value, int length)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        return normalized?.Length > length
            ? throw new ArgumentException($"El valor supera {length} caracteres.")
            : normalized;
    }

    /// <summary>Normaliza a mayúsculas un código técnico obligatorio.</summary>
    /// <param name="value">Código del catálogo o movimiento.</param>
    /// <param name="length">Longitud máxima admitida.</param>
    /// <returns>Código recortado y en mayúsculas.</returns>
    /// <exception cref="ArgumentException">El código falta o supera el límite.</exception>
    private static string Code(string? value, int length) =>
        Required(value, length, "El código").ToUpperInvariant();

    /// <summary>Restringe el estado editable de un maestro a activo o inactivo.</summary>
    /// <param name="value">Estado ingresado desde la interfaz.</param>
    /// <returns>ACTIVO o INACTIVO normalizado.</returns>
    /// <exception cref="ArgumentException">El estado no es reconocido.</exception>
    private static string Status(string? value) =>
        Code(value, 30) is var status && status is "ACTIVO" or "INACTIVO"
            ? status
            : throw new ArgumentException("El estado debe ser ACTIVO o INACTIVO.");

    /// <summary>Distingue un alta de una edición y exige versión solo para editar.</summary>
    /// <param name="id">Identificador nulo en altas o positivo en ediciones.</param>
    /// <param name="version">Versión de fila requerida en ediciones.</param>
    /// <exception cref="ArgumentException">El identificador o la versión son inválidos.</exception>
    private static void ValidateEdit(long? id, byte[]? version)
    {
        if (id is <= 0)
            throw new ArgumentException("El identificador no es válido.");
        if (id is not null)
            ValidateVersion(version);
    }

    /// <summary>Verifica la versión usada para impedir sobrescrituras concurrentes.</summary>
    /// <param name="version">Versión binaria consultada previamente.</param>
    /// <exception cref="ArgumentException">La versión no contiene ocho bytes.</exception>
    private static void ValidateVersion(byte[]? version)
    {
        if (version is not { Length: 8 })
            throw new ArgumentException("La versión del registro no es válida.");
    }
}
