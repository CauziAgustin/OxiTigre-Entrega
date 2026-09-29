/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Security.UserAdministrationService
Archivo: UserAdministrationService.cs | Versión: 2.1.0 | Fecha: 2026-08-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Administra usuarios, roles, estados, credenciales temporales y sesiones.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Validación de correo y contexto auditable.
Historial: 2.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Gestión integral de usuarios y roles.
Historial: 2.1.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Autorrevocación administrativa de sesiones.
===============================================================================
*/
namespace OxiTigre.BLL.Security;

/// <summary>Aplica autorización y convenciones para el alta de usuarios funcionales.</summary>
public sealed class UserAdministrationService(
    IUserAdministrationStore store,
    PasswordHasher passwordHasher
)
{
    /// <summary>Lista usuarios únicamente para administradores de la empresa.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Usuarios y roles asignados que puede administrar la sesión.</returns>
    public Task<IReadOnlyList<UserSummary>> ListAsync(
        SessionIdentity identity,
        CancellationToken cancellationToken
    )
    {
        EnsureAdministrator(identity);
        return store.ListAsync(
            identity.CompanyId,
            identity.CompanyCode,
            identity.SessionId,
            identity.UserId,
            cancellationToken
        );
    }

    /// <summary>Lista los roles que pueden asignarse en la empresa.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Roles disponibles para asignar a usuarios.</returns>
    public Task<IReadOnlyList<RoleSummary>> ListRolesAsync(
        SessionIdentity identity,
        CancellationToken cancellationToken
    )
    {
        EnsureAdministrator(identity);
        return store.ListRolesAsync(
            identity.CompanyId,
            identity.CompanyCode,
            identity.SessionId,
            identity.UserId,
            cancellationToken
        );
    }

    /// <summary>Genera el usuario corporativo y registra su credencial temporal y rol.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="givenNames">Nombres del usuario.</param>
    /// <param name="surname">Apellido del usuario.</param>
    /// <param name="email">Correo electrónico de contacto del usuario.</param>
    /// <param name="roleCodes">Códigos de roles que se asignan al usuario.</param>
    /// <param name="temporaryPassword">Contraseña temporal que deberá cambiarse al ingresar.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador del usuario creado.</returns>
    public async Task<long> CreateAsync(
        SessionIdentity identity,
        string givenNames,
        string surname,
        string email,
        IReadOnlyList<string> roleCodes,
        string temporaryPassword,
        CancellationToken cancellationToken
    )
    {
        EnsureAdministrator(identity);
        ValidateUser(givenNames, surname, email, roleCodes);

        var users = await store.ListAsync(
            identity.CompanyId,
            identity.CompanyCode,
            identity.SessionId,
            identity.UserId,
            cancellationToken
        );
        var username = UsernameGenerator.Generate(
            givenNames,
            surname,
            users.Select(user => user.Username).ToHashSet(StringComparer.OrdinalIgnoreCase)
        );
        return await store.CreateAsync(
            identity.CompanyId,
            identity.CompanyCode,
            username,
            givenNames.Trim(),
            surname.Trim(),
            email.Trim(),
            NormalizeRoles(roleCodes),
            passwordHasher.Hash(temporaryPassword),
            identity.UserId,
            identity.SessionId,
            cancellationToken
        );
    }

    /// <summary>Actualiza identidad, estado y roles sin permitir que el administrador se quite su propio acceso.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="givenNames">Nombres del usuario.</param>
    /// <param name="surname">Apellido del usuario.</param>
    /// <param name="email">Correo electrónico de contacto del usuario.</param>
    /// <param name="statusCode">Código de estado admitido por el dominio.</param>
    /// <param name="roleCodes">Códigos de roles que se asignan al usuario.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    /// <exception cref="ArgumentException">La identidad, correo, estado, roles o versión del usuario no son válidos.</exception>
    /// <exception cref="InvalidOperationException">El estado actual no admite la operación.</exception>
    public Task UpdateAsync(
        SessionIdentity identity,
        long userId,
        string givenNames,
        string surname,
        string email,
        string statusCode,
        IReadOnlyList<string> roleCodes,
        CancellationToken cancellationToken
    )
    {
        EnsureAdministrator(identity);
        ValidateUser(givenNames, surname, email, roleCodes);
        statusCode = statusCode.Trim().ToUpperInvariant();
        if (statusCode is not ("ACTIVO" or "INACTIVO"))
            throw new ArgumentException(
                "El estado debe ser ACTIVO o INACTIVO.",
                nameof(statusCode)
            );
        var normalizedRoles = NormalizeRoles(roleCodes);
        if (
            userId == identity.UserId
            && (statusCode != "ACTIVO" || !normalizedRoles.Contains("ADMINISTRADOR"))
        )
            throw new InvalidOperationException(
                "No podés desactivar tu propio usuario ni quitarte el rol ADMINISTRADOR."
            );

        return store.UpdateAsync(
            identity.CompanyId,
            identity.CompanyCode,
            userId,
            givenNames.Trim(),
            surname.Trim(),
            email.Trim(),
            statusCode,
            normalizedRoles,
            identity.UserId,
            identity.SessionId,
            cancellationToken
        );
    }

    /// <summary>Asigna una contraseña temporal segura y obliga a cambiarla en el siguiente ingreso.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="temporaryPassword">Contraseña temporal que deberá cambiarse al ingresar.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    public Task ResetPasswordAsync(
        SessionIdentity identity,
        long userId,
        string temporaryPassword,
        CancellationToken cancellationToken
    )
    {
        EnsureAdministrator(identity);
        return store.ResetPasswordAsync(
            identity.CompanyId,
            identity.CompanyCode,
            userId,
            passwordHasher.Hash(temporaryPassword),
            identity.UserId,
            identity.SessionId,
            cancellationToken
        );
    }

    /// <summary>Revoca todas las sesiones vigentes del usuario, incluida la sesión del administrador actual.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    public Task RevokeSessionsAsync(
        SessionIdentity identity,
        long userId,
        CancellationToken cancellationToken
    )
    {
        EnsureAdministrator(identity);
        return store.RevokeSessionsAsync(
            identity.CompanyId,
            identity.CompanyCode,
            userId,
            identity.UserId,
            identity.SessionId,
            cancellationToken
        );
    }

    /// <summary>Valida nombres, apellido, correo y roles antes de guardar un usuario.</summary>
    /// <param name="givenNames">Nombres declarados para el usuario.</param>
    /// <param name="surname">Apellido declarado para el usuario.</param>
    /// <param name="email">Correo electrónico declarado para el usuario.</param>
    /// <param name="roleCodes">Códigos de rol que se validan y asignan al usuario.</param>
    /// <exception cref="ArgumentException">Se produce cuando un dato de entrada no cumple el formato o la regla esperada.</exception>
    private static void ValidateUser(
        string givenNames,
        string surname,
        string email,
        IReadOnlyList<string> roleCodes
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(givenNames);
        ArgumentException.ThrowIfNullOrWhiteSpace(surname);
        ArgumentNullException.ThrowIfNull(roleCodes);
        if (roleCodes.Count == 0 || roleCodes.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Seleccioná al menos un rol válido.", nameof(roleCodes));
        if (!System.Net.Mail.MailAddress.TryCreate(email, out _))
            throw new ArgumentException(
                "El correo electrónico no tiene un formato válido.",
                nameof(email)
            );
    }

    /// <summary>Normaliza, elimina duplicados y ordena los códigos de rol recibidos.</summary>
    /// <param name="roleCodes">Códigos de rol que se validan y asignan al usuario.</param>
    /// <returns>Colección de registros u opciones obtenida por la operación.</returns>
    private static IReadOnlyList<string> NormalizeRoles(IReadOnlyList<string> roleCodes) =>
        roleCodes
            .Select(role => role.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>Comprueba que la sesión pertenezca a un administrador habilitado.</summary>
    /// <param name="identity">Identidad autenticada que aporta empresa, sucursal, usuario, sesión y permisos.</param>
    /// <exception cref="UnauthorizedAccessException">Se produce cuando la sesión no posee el permiso requerido.</exception>
    private static void EnsureAdministrator(SessionIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        if (!identity.Roles.Contains("ADMINISTRADOR", StringComparer.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException();
    }
}
