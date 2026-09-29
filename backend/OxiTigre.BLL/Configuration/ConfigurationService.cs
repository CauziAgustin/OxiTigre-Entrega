/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Configuration.ConfigurationService
Archivo: ConfigurationService.cs | Versión: 1.1.0 | Fecha: 2026-09-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Autoriza, normaliza y valida la administración de Configuración.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-09-20 | FABRICA | Agustin Omar Cauzi | Culturas importables validadas con CultureInfo.
===============================================================================
*/
using System.Globalization;
using System.Net.Mail;
using OxiTigre.BLL.Security;

namespace OxiTigre.BLL.Configuration;

/// <summary>Orquesta Configuración preservando empresa, permisos, secretos y códigos técnicos.</summary>
public sealed class ConfigurationService(IConfigurationStore store)
{
    /// <summary>Obtiene la configuración visible para una sesión autorizada.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Empresas, sucursales, catálogos, parámetros y módulos configurables.</returns>
    public Task<ConfigurationSnapshot> GetAsync(
        SessionIdentity identity,
        CancellationToken cancellationToken
    )
    {
        EnsurePermission(identity, "CONFIGURACION.CONSULTAR");
        return store.GetAsync(
            identity.CompanyId,
            identity.CompanyCode,
            identity.UserId,
            identity.SessionId,
            cancellationToken
        );
    }

    /// <summary>Obtiene la preferencia de idioma de cualquier sesión válida.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Código de cultura guardado para el usuario.</returns>
    public Task<string> GetCultureAsync(
        SessionIdentity identity,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(identity);
        return store.GetCultureAsync(
            identity.CompanyCode,
            identity.UserId,
            identity.SessionId,
            cancellationToken
        );
    }

    /// <summary>Guarda una cultura específica válida para el usuario actual.</summary>
    /// <exception cref="ArgumentException">El código no representa una cultura específica o supera el almacenamiento disponible.</exception>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="cultureCode">Código de cultura específico y canónico, por ejemplo es-AR.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    public Task SaveCultureAsync(
        SessionIdentity identity,
        string cultureCode,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(identity);
        return store.SaveCultureAsync(
            identity.CompanyCode,
            identity.UserId,
            identity.SessionId,
            Culture(cultureCode),
            cancellationToken
        );
    }

    /// <summary>Actualiza únicamente la empresa de la sesión.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="change">Datos legales, estado y versión de la empresa que se modificará.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    /// <exception cref="ArgumentException">La razón social, correo, CUIT o versión de la empresa no son válidos.</exception>
    public Task UpdateCompanyAsync(
        SessionIdentity identity,
        CompanyChange change,
        CancellationToken cancellationToken
    )
    {
        EnsureManage(identity);
        ArgumentNullException.ThrowIfNull(change);
        var legalName = Required(change.LegalName, 200, "La razón social");
        var email = Optional(change.Email, 254);
        if (email is not null && !MailAddress.TryCreate(email, out _))
            throw new ArgumentException("El correo electrónico no es válido.", nameof(change));
        var taxId = Optional(change.TaxId, 11)
            ?.Replace("-", string.Empty, StringComparison.Ordinal);
        if (taxId is not null && (taxId.Length != 11 || !taxId.All(char.IsDigit)))
            throw new ArgumentException("El CUIT debe contener 11 dígitos.", nameof(change));
        ValidateVersion(change.RowVersion);
        return store.UpdateCompanyAsync(
            identity.CompanyId,
            identity.CompanyCode,
            identity.UserId,
            identity.SessionId,
            change with
            {
                LegalName = legalName,
                TradeName = Optional(change.TradeName, 200),
                TaxId = taxId,
                Email = email,
            },
            cancellationToken
        );
    }

    /// <summary>Crea o actualiza una sucursal.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="change">Código, nombre, domicilio, estado y versión de la sucursal.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador de la sucursal creada o actualizada.</returns>
    public Task<long> SaveBranchAsync(
        SessionIdentity identity,
        BranchChange change,
        CancellationToken cancellationToken
    )
    {
        EnsureManage(identity);
        ArgumentNullException.ThrowIfNull(change);
        ValidateEdit(change.BranchId, change.RowVersion);
        return store.SaveBranchAsync(
            identity.CompanyId,
            identity.CompanyCode,
            identity.UserId,
            identity.SessionId,
            change with
            {
                Code = Code(change.Code, 30),
                Name = Required(change.Name, 200, "El nombre"),
                Address = Optional(change.Address, 250),
                City = Optional(change.City, 150),
                Province = Optional(change.Province, 150),
                PostalCode = Optional(change.PostalCode, 20),
                StatusCode = Status(change.StatusCode),
            },
            cancellationToken
        );
    }

    /// <summary>Crea o actualiza una unidad operativa.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="change">Código, descripción y estado de la unidad operativa.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador de la unidad operativa creada o actualizada.</returns>
    /// <exception cref="ArgumentException">Falta la sucursal o el código, nombre, estado o versión no son válidos.</exception>
    public Task<long> SaveOperatingUnitAsync(
        SessionIdentity identity,
        OperatingUnitChange change,
        CancellationToken cancellationToken
    )
    {
        EnsureManage(identity);
        ArgumentNullException.ThrowIfNull(change);
        ValidateEdit(change.OperatingUnitId, change.RowVersion);
        if (change.BranchId <= 0)
            throw new ArgumentException("La sucursal es obligatoria.", nameof(change));
        return store.SaveOperatingUnitAsync(
            identity.CompanyId,
            identity.CompanyCode,
            identity.UserId,
            identity.SessionId,
            change with
            {
                Code = Code(change.Code, 30),
                Name = Required(change.Name, 200, "El nombre"),
                Description = Optional(change.Description, 500),
                StatusCode = Status(change.StatusCode),
            },
            cancellationToken
        );
    }

    /// <summary>Crea o actualiza un tipo de teléfono.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="change">Código, nombre y estado del tipo de teléfono.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador del tipo de teléfono creado o actualizado.</returns>
    public Task<long> SavePhoneTypeAsync(
        SessionIdentity identity,
        PhoneTypeChange change,
        CancellationToken cancellationToken
    )
    {
        EnsureManage(identity);
        ArgumentNullException.ThrowIfNull(change);
        ValidateEdit(change.PhoneTypeId, change.RowVersion);
        return store.SavePhoneTypeAsync(
            identity.CompanyCode,
            identity.UserId,
            identity.SessionId,
            change with
            {
                Code = Code(change.Code, 30),
                Name = Required(change.Name, 100, "El nombre"),
                Description = Optional(change.Description, 500),
                StatusCode = Status(change.StatusCode),
            },
            cancellationToken
        );
    }

    /// <summary>Actualiza textos y visibilidad de un estado.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="change">Código, nombre y versión del estado configurable.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    /// <exception cref="ArgumentException">El código, nombre o versión del estado no son válidos.</exception>
    public Task UpdateStateAsync(
        SessionIdentity identity,
        StateChange change,
        CancellationToken cancellationToken
    )
    {
        EnsureManage(identity);
        ArgumentNullException.ThrowIfNull(change);
        ValidateVersion(change.RowVersion);
        if (change.StateId <= 0 || change.Order < 0)
            throw new ArgumentException("El estado y el orden no son válidos.", nameof(change));
        return store.UpdateStateAsync(
            identity.CompanyCode,
            identity.UserId,
            identity.SessionId,
            change with
            {
                Name = Required(change.Name, 100, "El nombre"),
                Description = Optional(change.Description, 500),
            },
            cancellationToken
        );
    }

    /// <summary>Crea o actualiza un parámetro tipado.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="change">Clave, valor y versión del parámetro del sistema.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador del parámetro guardado.</returns>
    /// <exception cref="ArgumentException">La clave, el valor o la versión del parámetro no son válidos.</exception>
    public Task<long> SaveParameterAsync(
        SessionIdentity identity,
        SystemParameterChange change,
        CancellationToken cancellationToken
    )
    {
        EnsureManage(identity);
        ArgumentNullException.ThrowIfNull(change);
        ValidateEdit(change.ParameterId, change.RowVersion);
        var type = Code(change.DataType, 30);
        if (type is not ("TEXTO" or "ENTERO" or "DECIMAL" or "BOOLEANO" or "FECHA"))
            throw new ArgumentException("El tipo de parámetro no está soportado.", nameof(change));
        if (change.IsSecret && !string.IsNullOrWhiteSpace(change.Value))
            throw new ArgumentException(
                "Un secreto debe usar una referencia externa, no un valor.",
                nameof(change)
            );
        if (!change.IsSecret && !string.IsNullOrWhiteSpace(change.SecretReference))
            throw new ArgumentException(
                "Un parámetro común no puede tener referencia secreta.",
                nameof(change)
            );
        return store.SaveParameterAsync(
            identity.CompanyCode,
            identity.UserId,
            identity.SessionId,
            change with
            {
                Key = Code(change.Key, 100),
                Value = Optional(change.Value, 2000),
                DataType = type,
                SecretReference = Optional(change.SecretReference, 250),
                Description = Optional(change.Description, 500),
                StatusCode = Status(change.StatusCode),
            },
            cancellationToken
        );
    }

    /// <summary>Crea o actualiza un módulo preservando número y código en ediciones.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="change">Módulo, estado y versión de la configuración que se guardará.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador del módulo configurado.</returns>
    /// <exception cref="ArgumentException">El código, estado o versión del módulo no son válidos.</exception>
    public Task<long> SaveModuleAsync(
        SessionIdentity identity,
        ModuleChange change,
        CancellationToken cancellationToken
    )
    {
        EnsureManage(identity);
        ArgumentNullException.ThrowIfNull(change);
        ValidateEdit(change.ModuleId, change.RowVersion);
        if (change.ModuleNumber is < 1 or > 999 || change.Order < 0)
            throw new ArgumentException(
                "El número u orden del módulo no es válido.",
                nameof(change)
            );
        return store.SaveModuleAsync(
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

    /// <summary>Crea un código de error dentro de un módulo.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="change">Código y descripción del nuevo error funcional.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Código numérico asignado al error funcional.</returns>
    /// <exception cref="ArgumentException">El código o la descripción del error funcional no son válidos.</exception>
    public Task<long> CreateErrorAsync(
        SessionIdentity identity,
        CreateErrorCatalogChange change,
        CancellationToken cancellationToken
    )
    {
        EnsurePermission(identity, "AUDITORIA.GESTIONAR_ERRORES");
        ArgumentNullException.ThrowIfNull(change);
        if (change.ModuleId <= 0)
            throw new ArgumentException("El módulo es obligatorio.", nameof(change));
        return store.CreateErrorAsync(
            identity.CompanyCode,
            identity.UserId,
            identity.SessionId,
            change with
            {
                Name = Required(change.Name, 200, "El nombre"),
                Description = Required(change.Description, 1000, "La descripción"),
                ProbableCause = Optional(change.ProbableCause, 1000),
                RecommendedAction = Optional(change.RecommendedAction, 2000),
                Severity = Severity(change.Severity),
            },
            cancellationToken
        );
    }

    /// <summary>Actualiza el diagnóstico de un código existente.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="change">Código, descripción, estado y versión del error funcional.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    /// <exception cref="ArgumentException">El código, descripción, estado o versión del error funcional no son válidos.</exception>
    public Task UpdateErrorAsync(
        SessionIdentity identity,
        UpdateErrorCatalogChange change,
        CancellationToken cancellationToken
    )
    {
        EnsurePermission(identity, "AUDITORIA.GESTIONAR_ERRORES");
        ArgumentNullException.ThrowIfNull(change);
        ValidateVersion(change.RowVersion);
        if (change.ErrorId <= 0)
            throw new ArgumentException("El error es obligatorio.", nameof(change));
        return store.UpdateErrorAsync(
            identity.CompanyCode,
            identity.UserId,
            identity.SessionId,
            change with
            {
                Name = Required(change.Name, 200, "El nombre"),
                Description = Required(change.Description, 1000, "La descripción"),
                ProbableCause = Optional(change.ProbableCause, 1000),
                RecommendedAction = Optional(change.RecommendedAction, 2000),
                Severity = Severity(change.Severity),
                StatusCode = Status(change.StatusCode),
            },
            cancellationToken
        );
    }

    /// <summary>Crea o actualiza una traducción para un código existente.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="change">Cultura, catálogo, clave y traducción que se conservarán.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador de la traducción de catálogo guardada.</returns>
    public Task<long> SaveTranslationAsync(
        SessionIdentity identity,
        CatalogTranslationChange change,
        CancellationToken cancellationToken
    )
    {
        EnsureManage(identity);
        ArgumentNullException.ThrowIfNull(change);
        ValidateEdit(change.TranslationId, change.RowVersion);
        return store.SaveTranslationAsync(
            identity.CompanyCode,
            identity.UserId,
            identity.SessionId,
            change with
            {
                Entity = Code(change.Entity, 100),
                Code = Code(change.Code, 100),
                CultureCode = Culture(change.CultureCode),
                Name = Required(change.Name, 200, "El nombre"),
                Description = Optional(change.Description, 1000),
                StatusCode = Status(change.StatusCode),
            },
            cancellationToken
        );
    }

    /// <summary>Exige permiso de gestión para modificar catálogos o traducciones.</summary>
    /// <param name="identity">Sesión autenticada cuyos permisos se comprueban.</param>
    /// <exception cref="UnauthorizedAccessException">La sesión no puede gestionar configuración.</exception>
    private static void EnsureManage(SessionIdentity identity) =>
        EnsurePermission(identity, "CONFIGURACION.GESTIONAR");

    /// <summary>Valida el permiso concedido por la sesión antes de consultar o editar.</summary>
    /// <param name="identity">Sesión autenticada que contiene los permisos.</param>
    /// <param name="permission">Código del permiso requerido.</param>
    /// <exception cref="ArgumentNullException">No se informó una sesión.</exception>
    /// <exception cref="UnauthorizedAccessException">Falta el permiso indicado.</exception>
    private static void EnsurePermission(SessionIdentity identity, string permission)
    {
        ArgumentNullException.ThrowIfNull(identity);
        if (!identity.Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException($"La sesión no posee el permiso {permission}.");
    }

    /// <summary>Recorta y limita un texto obligatorio usado en configuración.</summary>
    /// <param name="value">Valor recibido desde la interfaz o la API.</param>
    /// <param name="length">Longitud máxima persistible.</param>
    /// <param name="name">Nombre del campo para el error funcional.</param>
    /// <returns>Texto no vacío y sin espacios extremos.</returns>
    /// <exception cref="ArgumentException">Falta el valor o supera el límite.</exception>
    private static string Required(string? value, int length, string name) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException($"{name} es obligatorio.")
            : Optional(value, length)!;

    /// <summary>Convierte un identificador de cultura a su forma canónica específica.</summary>
    /// <param name="value">Código cultural del catálogo de traducciones.</param>
    /// <returns>Nombre canónico de cultura, de hasta diez caracteres.</returns>
    /// <exception cref="ArgumentException">La cultura no existe, es neutra o demasiado larga.</exception>
    private static string Culture(string? value)
    {
        try
        {
            var culture = CultureInfo.GetCultureInfo(value?.Trim() ?? string.Empty);
            if (culture.IsNeutralCulture || culture.Name.Length > 10)
                throw new ArgumentException(
                    "La cultura debe ser específica y tener hasta 10 caracteres.",
                    nameof(value)
                );
            return culture.Name;
        }
        catch (CultureNotFoundException exception)
        {
            throw new ArgumentException(
                "El código de cultura no es válido.",
                nameof(value),
                exception
            );
        }
    }

    /// <summary>Recorta un dato opcional sin guardar cadenas vacías ni truncar contenido.</summary>
    /// <param name="value">Texto opcional recibido.</param>
    /// <param name="length">Longitud máxima persistible.</param>
    /// <returns>Texto recortado o nulo si no hay contenido.</returns>
    /// <exception cref="ArgumentException">El texto informado supera el límite.</exception>
    private static string? Optional(string? value, int length)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        return normalized?.Length > length
            ? throw new ArgumentException($"El valor supera {length} caracteres.")
            : normalized;
    }

    /// <summary>Normaliza un código técnico obligatorio a mayúsculas.</summary>
    /// <param name="value">Código ingresado por el operador.</param>
    /// <param name="length">Longitud máxima del catálogo de destino.</param>
    /// <returns>Código recortado y en mayúsculas.</returns>
    /// <exception cref="ArgumentException">El código falta o supera el límite.</exception>
    private static string Code(string? value, int length) =>
        Required(value, length, "El código").ToUpperInvariant();

    /// <summary>Limita el estado de un catálogo a activo o inactivo.</summary>
    /// <param name="value">Estado ingresado.</param>
    /// <returns>ACTIVO o INACTIVO normalizado.</returns>
    /// <exception cref="ArgumentException">El estado no es reconocido.</exception>
    private static string Status(string? value) =>
        Code(value, 30) is var status && status is "ACTIVO" or "INACTIVO"
            ? status
            : throw new ArgumentException("El estado debe ser ACTIVO o INACTIVO.");

    /// <summary>Limita la gravedad de un error parametrizado a los niveles admitidos.</summary>
    /// <param name="value">Severidad ingresada.</param>
    /// <returns>Código normalizado de severidad.</returns>
    /// <exception cref="ArgumentException">La severidad no es reconocida.</exception>
    private static string Severity(string? value) =>
        Code(value, 30) is var severity
        && severity is "INFORMATIVO" or "ADVERTENCIA" or "ERROR" or "CRITICO"
            ? severity
            : throw new ArgumentException("La severidad no es válida.");

    /// <summary>Distingue un alta de una edición y exige control de concurrencia al editar.</summary>
    /// <param name="id">Identificador nulo en alta o positivo en edición.</param>
    /// <param name="version">Versión del registro requerida en edición.</param>
    /// <exception cref="ArgumentException">El identificador o la versión no son válidos.</exception>
    private static void ValidateEdit(long? id, byte[]? version)
    {
        if (id is <= 0)
            throw new ArgumentException("El identificador de edición no es válido.");
        if (id is not null)
            ValidateVersion(version);
    }

    /// <summary>Comprueba el formato de la versión de fila antes de actualizar.</summary>
    /// <param name="version">Versión binaria leída anteriormente.</param>
    /// <exception cref="ArgumentException">La versión no contiene ocho bytes.</exception>
    private static void ValidateVersion(byte[]? version)
    {
        if (version is not { Length: 8 })
            throw new ArgumentException("La versión del registro no es válida.");
    }
}
