/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Commercial.ClientService
Archivo: ClientService.cs | Versión: 2.1.0 | Fecha: 2026-08-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Autoriza y valida clientes, catálogos, teléfonos, estados y borradores.
Historial: 1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 2.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Ciclo funcional completo del cliente y teléfonos.
Historial: 2.1.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Catálogos, borradores y validación integral de entrada.
===============================================================================
*/
using OxiTigre.BLL.Security;

namespace OxiTigre.BLL.Commercial;

/// <summary>Orquesta el módulo Comercial aplicando aislamiento, autorización y reglas de cliente.</summary>
public sealed class ClientService(IClientStore clientStore)
{
    /// <summary>Obtiene los catálogos parametrizados visibles en la carga de clientes.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tipos de documento, condiciones y demás catálogos usados para registrar clientes.</returns>
    public Task<ClientCatalogs> GetCatalogsAsync(
        SessionIdentity identity,
        CancellationToken cancellationToken
    )
    {
        EnsurePermission(identity, "COMERCIAL.CONSULTAR");
        return clientStore.GetCatalogsAsync(
            identity.CompanyCode,
            identity.SessionId,
            identity.UserId,
            cancellationToken
        );
    }

    /// <summary>Obtiene la precarga incompleta del usuario autenticado.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Borrador de cliente del usuario, o null si no hay uno.</returns>
    public Task<SavedClientDraft?> GetDraftAsync(
        SessionIdentity identity,
        CancellationToken cancellationToken
    )
    {
        EnsurePermission(identity, "COMERCIAL.GESTIONAR");
        return clientStore.GetDraftAsync(
            identity.CompanyId,
            identity.CompanyCode,
            identity.SessionId,
            identity.UserId,
            cancellationToken
        );
    }

    /// <summary>Guarda una precarga incompleta sin exigir los datos del alta definitiva.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="draft">Identificación, contacto y teléfonos del cliente que se guardarán.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    /// <exception cref="ArgumentException">El borrador contiene identificación, contacto o teléfonos inválidos.</exception>
    public Task SaveDraftAsync(
        SessionIdentity identity,
        ClientDraft draft,
        CancellationToken cancellationToken
    )
    {
        EnsurePermission(identity, "COMERCIAL.GESTIONAR");
        ArgumentNullException.ThrowIfNull(draft);
        var prepared = Normalize(draft);
        ValidateLengths(prepared);
        if (prepared.Phones.Count > 20)
            throw new ArgumentException(
                "Un borrador no puede contener más de 20 teléfonos.",
                nameof(draft)
            );
        return clientStore.SaveDraftAsync(
            identity.CompanyId,
            identity.CompanyCode,
            prepared,
            identity.SessionId,
            identity.UserId,
            cancellationToken
        );
    }

    /// <summary>Descarta la precarga incompleta del usuario autenticado.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    public Task DeleteDraftAsync(SessionIdentity identity, CancellationToken cancellationToken)
    {
        EnsurePermission(identity, "COMERCIAL.GESTIONAR");
        return clientStore.DeleteDraftAsync(
            identity.CompanyId,
            identity.CompanyCode,
            identity.SessionId,
            identity.UserId,
            cancellationToken
        );
    }

    /// <summary>Lista clientes del estado solicitado para una sesión autorizada.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="statusCode">Código de estado admitido por el dominio.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Clientes filtrados por el estado solicitado.</returns>
    public Task<IReadOnlyList<ClientSummary>> ListAsync(
        SessionIdentity identity,
        string statusCode,
        CancellationToken cancellationToken
    )
    {
        EnsurePermission(identity, "COMERCIAL.CONSULTAR");
        return clientStore.ListAsync(
            identity.CompanyId,
            identity.CompanyCode,
            NormalizeStatus(statusCode),
            identity.SessionId,
            identity.UserId,
            cancellationToken
        );
    }

    /// <summary>Conserva el contrato anterior para listar clientes activos.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Clientes activos disponibles para operaciones comerciales.</returns>
    public Task<IReadOnlyList<ClientSummary>> ListActiveAsync(
        SessionIdentity identity,
        CancellationToken cancellationToken
    ) => ListAsync(identity, "ACTIVO", cancellationToken);

    /// <summary>Obtiene el detalle de un cliente aislado por la empresa de la sesión.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="clientId">Identificador del cliente, verificado dentro de la empresa activa.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Datos completos del cliente indicado, o null si no existe.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Un identificador, cantidad o rango no es válido.</exception>
    public Task<ClientDetails?> GetAsync(
        SessionIdentity identity,
        long clientId,
        CancellationToken cancellationToken
    )
    {
        EnsurePermission(identity, "COMERCIAL.CONSULTAR");
        if (clientId <= 0)
            throw new ArgumentOutOfRangeException(nameof(clientId));
        return clientStore.GetAsync(
            identity.CompanyId,
            identity.CompanyCode,
            clientId,
            identity.SessionId,
            identity.UserId,
            cancellationToken
        );
    }

    /// <summary>Valida y crea un cliente con sus teléfonos.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="draft">Identificación, contacto y teléfonos del cliente que se guardarán.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador del cliente creado.</returns>
    public Task<long> CreateAsync(
        SessionIdentity identity,
        ClientDraft draft,
        CancellationToken cancellationToken
    )
    {
        EnsurePermission(identity, "COMERCIAL.GESTIONAR");
        return clientStore.CreateAsync(
            identity.CompanyId,
            identity.CompanyCode,
            Validate(draft),
            identity.SessionId,
            identity.UserId,
            cancellationToken
        );
    }

    /// <summary>Valida y actualiza un cliente con reemplazo controlado de teléfonos activos.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="clientId">Identificador del cliente, verificado dentro de la empresa activa.</param>
    /// <param name="draft">Identificación, contacto y teléfonos del cliente que se guardarán.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Un identificador, cantidad o rango no es válido.</exception>
    public Task UpdateAsync(
        SessionIdentity identity,
        long clientId,
        ClientDraft draft,
        CancellationToken cancellationToken
    )
    {
        EnsurePermission(identity, "COMERCIAL.GESTIONAR");
        if (clientId <= 0)
            throw new ArgumentOutOfRangeException(nameof(clientId));
        return clientStore.UpdateAsync(
            identity.CompanyId,
            identity.CompanyCode,
            clientId,
            Validate(draft),
            identity.SessionId,
            identity.UserId,
            cancellationToken
        );
    }

    /// <summary>Activa o inactiva lógicamente un cliente sin eliminar su historial.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="clientId">Identificador del cliente, verificado dentro de la empresa activa.</param>
    /// <param name="statusCode">Código de estado admitido por el dominio.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Un identificador, cantidad o rango no es válido.</exception>
    public Task ChangeStatusAsync(
        SessionIdentity identity,
        long clientId,
        string statusCode,
        CancellationToken cancellationToken
    )
    {
        EnsurePermission(identity, "COMERCIAL.GESTIONAR");
        if (clientId <= 0)
            throw new ArgumentOutOfRangeException(nameof(clientId));
        return clientStore.ChangeStatusAsync(
            identity.CompanyId,
            identity.CompanyCode,
            clientId,
            NormalizeStatus(statusCode),
            identity.SessionId,
            identity.UserId,
            cancellationToken
        );
    }

    /// <summary>Prepara un alta definitiva y exige identidad, documento y teléfonos consistentes.</summary>
    /// <param name="draft">Datos de cliente recibidos desde la API.</param>
    /// <returns>Datos normalizados y listos para guardar.</returns>
    /// <exception cref="ArgumentNullException">No se informó un cliente.</exception>
    /// <exception cref="ArgumentException">Faltan datos obligatorios, hay teléfonos duplicados o un formato es inválido.</exception>
    private static ClientDraft Validate(ClientDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);
        var normalized = Normalize(draft);
        ValidateLengths(normalized);
        if (normalized.PersonType is not ("F" or "J"))
            throw new ArgumentException(
                "El tipo de persona debe ser física o jurídica.",
                nameof(draft)
            );
        if (string.IsNullOrWhiteSpace(normalized.NameOrBusinessName))
            throw new ArgumentException("El nombre o razón social es obligatorio.", nameof(draft));
        if (normalized.PersonType == "F" && string.IsNullOrWhiteSpace(normalized.Surname))
            throw new ArgumentException(
                "El apellido es obligatorio para una persona física.",
                nameof(draft)
            );
        if (normalized.DocumentType is null || normalized.DocumentNumber is null)
            throw new ArgumentException(
                "El tipo y número de documento son obligatorios.",
                nameof(draft)
            );
        ValidateDocument(normalized.DocumentType, normalized.DocumentNumber);
        if (
            normalized.Email is not null
            && !System.Net.Mail.MailAddress.TryCreate(normalized.Email, out _)
        )
            throw new ArgumentException("El correo electrónico no es válido.", nameof(draft));
        if (normalized.Phones.Count == 0 || normalized.Phones.Count(phone => phone.IsPrimary) != 1)
            throw new ArgumentException(
                "Debe informar teléfonos y marcar exactamente uno como principal.",
                nameof(draft)
            );
        if (normalized.Phones.Count > 20)
            throw new ArgumentException(
                "Un cliente no puede contener más de 20 teléfonos.",
                nameof(draft)
            );
        foreach (var phone in normalized.Phones)
            ValidatePhone(phone);
        if (
            normalized
                .Phones.GroupBy(
                    phone =>
                        $"{phone.CountryCode}|{phone.AreaCode}|{phone.Number}|{phone.Extension}",
                    StringComparer.OrdinalIgnoreCase
                )
                .Any(group => group.Count() > 1)
        )
            throw new ArgumentException(
                "No se puede cargar dos veces el mismo teléfono.",
                nameof(draft)
            );
        return normalized;
    }

    /// <summary>Unifica espacios, códigos, documentos y teléfonos antes de validar o guardar.</summary>
    /// <param name="draft">Datos originales proporcionados por el operador.</param>
    /// <returns>Copia normalizada sin modificar el objeto recibido.</returns>
    /// <exception cref="ArgumentException">Un dato telefónico contiene caracteres no numéricos.</exception>
    private static ClientDraft Normalize(ClientDraft draft) =>
        draft with
        {
            PersonType = (draft.PersonType ?? string.Empty).Trim().ToUpperInvariant(),
            NameOrBusinessName = (draft.NameOrBusinessName ?? string.Empty).Trim(),
            Surname = NullIfWhiteSpace(draft.Surname),
            DocumentType = NullIfWhiteSpace(draft.DocumentType)?.ToUpperInvariant(),
            DocumentNumber = NormalizeDocumentNumber(draft.DocumentNumber),
            Email = NullIfWhiteSpace(draft.Email),
            Observation = NullIfWhiteSpace(draft.Observation),
            Phones = (draft.Phones ?? [])
                .Select(phone =>
                    phone with
                    {
                        TypeCode = (phone.TypeCode ?? string.Empty).Trim().ToUpperInvariant(),
                        CountryCode = DigitsOnly(phone.CountryCode),
                        AreaCode = DigitsOnly(phone.AreaCode),
                        Number = DigitsOnly(phone.Number) ?? string.Empty,
                        Extension = DigitsOnly(phone.Extension),
                        Observation = NullIfWhiteSpace(phone.Observation),
                    }
                )
                .ToList(),
        };

    /// <summary>Comprueba los límites de columnas generales y telefónicas de persistencia.</summary>
    /// <param name="draft">Cliente normalizado que se va a guardar como borrador o definitivo.</param>
    /// <exception cref="ArgumentException">Algún campo supera la longitud admitida.</exception>
    private static void ValidateLengths(ClientDraft draft)
    {
        if (
            draft.NameOrBusinessName.Length > 200
            || draft.Surname?.Length > 150
            || draft.DocumentType?.Length > 20
            || draft.DocumentNumber?.Length > 30
            || draft.Email?.Length > 254
            || draft.Observation?.Length > 1000
        )
            throw new ArgumentException(
                "Uno o más datos generales superan la longitud permitida.",
                nameof(draft)
            );
        if (
            draft.Phones.Any(phone =>
                phone.TypeCode.Length > 30
                || phone.CountryCode?.Length > 5
                || phone.AreaCode?.Length > 10
                || phone.Number.Length > 20
                || phone.Extension?.Length > 10
                || phone.Observation?.Length > 500
            )
        )
            throw new ArgumentException(
                "Uno o más datos telefónicos superan la longitud permitida.",
                nameof(draft)
            );
    }

    /// <summary>Aplica reglas de formato según el tipo de documento seleccionado.</summary>
    /// <param name="documentType">Código de tipo de documento del catálogo.</param>
    /// <param name="documentNumber">Número previamente normalizado, sin separadores visuales.</param>
    /// <exception cref="ArgumentException">El número contiene símbolos o incumple DNI, CUIT/CUIL o pasaporte.</exception>
    private static void ValidateDocument(string documentType, string documentNumber)
    {
        if (documentNumber.Any(character => !char.IsLetterOrDigit(character)))
            throw new ArgumentException(
                "El número de documento solo puede contener letras y números.",
                nameof(documentNumber)
            );
        if (
            documentType == "DNI"
            && (documentNumber.Length is < 7 or > 8 || !documentNumber.All(char.IsDigit))
        )
            throw new ArgumentException(
                "El DNI debe contener 7 u 8 dígitos.",
                nameof(documentNumber)
            );
        if (documentType is "CUIT" or "CUIL" && !IsValidTaxId(documentNumber))
            throw new ArgumentException(
                "El CUIT o CUIL informado no es válido.",
                nameof(documentNumber)
            );
        if (documentType == "PASAPORTE" && documentNumber.Length is < 6 or > 15)
            throw new ArgumentException(
                "El pasaporte debe contener entre 6 y 15 caracteres.",
                nameof(documentNumber)
            );
    }

    /// <summary>Verifica los once dígitos y el dígito de control de un CUIT o CUIL argentino.</summary>
    /// <param name="value">CUIT o CUIL sin guiones.</param>
    /// <returns>Verdadero cuando la longitud, los dígitos y el control son válidos.</returns>
    private static bool IsValidTaxId(string value)
    {
        if (value.Length != 11 || !value.All(char.IsDigit))
            return false;
        int[] factors = [5, 4, 3, 2, 7, 6, 5, 4, 3, 2];
        var sum = value
            .Take(10)
            .Select((character, index) => (character - '0') * factors[index])
            .Sum();
        var verifier = 11 - sum % 11;
        verifier =
            verifier == 11 ? 0
            : verifier == 10 ? 9
            : verifier;
        return verifier == value[10] - '0';
    }

    /// <summary>Exige tipo, país y número con las longitudes permitidas para un teléfono.</summary>
    /// <param name="phone">Teléfono normalizado del cliente.</param>
    /// <exception cref="ArgumentException">Falta un dato obligatorio o la longitud es inválida.</exception>
    private static void ValidatePhone(ClientPhoneDraft phone)
    {
        if (string.IsNullOrWhiteSpace(phone.TypeCode) || string.IsNullOrWhiteSpace(phone.Number))
            throw new ArgumentException(
                "Todos los teléfonos deben tener tipo y número.",
                nameof(phone)
            );
        if (phone.CountryCode is null || phone.CountryCode.Length is < 1 or > 5)
            throw new ArgumentException(
                "El código de país debe contener entre 1 y 5 dígitos.",
                nameof(phone)
            );
        if (phone.Number.Length is < 6 or > 20)
            throw new ArgumentException(
                "El número de teléfono debe contener entre 6 y 20 dígitos.",
                nameof(phone)
            );
    }

    /// <summary>Quita separadores visuales del documento para persistir una identidad estable.</summary>
    /// <param name="value">Número ingresado, que puede contener espacios, puntos o guiones.</param>
    /// <returns>Número sin separadores y en mayúsculas, o nulo si no se informó.</returns>
    private static string? NormalizeDocumentNumber(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : new string(
                value.Where(character => character is not (' ' or '.' or '-')).ToArray()
            ).ToUpperInvariant();

    /// <summary>Retira el prefijo internacional y rechaza caracteres no numéricos del teléfono.</summary>
    /// <param name="value">Código o número telefónico ingresado.</param>
    /// <returns>Dígitos sin el signo inicial más, o nulo si está vacío.</returns>
    /// <exception cref="ArgumentException">El valor contiene caracteres distintos de dígitos.</exception>
    private static string? DigitsOnly(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var trimmed = value.Trim().TrimStart('+');
        return trimmed.All(char.IsDigit)
            ? trimmed
            : throw new ArgumentException("Los datos telefónicos solo pueden contener números.");
    }

    /// <summary>Normaliza el estado y restringe cambios a los estados funcionales del cliente.</summary>
    /// <param name="statusCode">Estado recibido desde la interfaz o la API.</param>
    /// <returns>ACTIVO o INACTIVO en mayúsculas.</returns>
    /// <exception cref="ArgumentException">El estado recibido no está permitido.</exception>
    private static string NormalizeStatus(string statusCode)
    {
        var normalized = statusCode?.Trim().ToUpperInvariant();
        return normalized is "ACTIVO" or "INACTIVO"
            ? normalized
            : throw new ArgumentException(
                "El estado debe ser ACTIVO o INACTIVO.",
                nameof(statusCode)
            );
    }

    /// <summary>Evita guardar cadenas vacías donde el dato opcional se representa con nulo.</summary>
    /// <param name="value">Texto opcional recibido.</param>
    /// <returns>Texto recortado o nulo cuando no contiene información.</returns>
    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Autoriza una operación comercial usando permisos de la sesión autenticada.</summary>
    /// <param name="identity">Sesión cuyos permisos se comprueban.</param>
    /// <param name="permission">Código del permiso exigido.</param>
    /// <exception cref="ArgumentNullException">No se informó una identidad de sesión.</exception>
    /// <exception cref="UnauthorizedAccessException">La sesión no tiene el permiso requerido.</exception>
    private static void EnsurePermission(SessionIdentity identity, string permission)
    {
        ArgumentNullException.ThrowIfNull(identity);
        if (!identity.Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException($"La sesión no posee el permiso {permission}.");
    }
}
