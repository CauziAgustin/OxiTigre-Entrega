/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Security.AuthenticationService
Archivo: AuthenticationService.cs | Versión: 9.0.0 | Fecha: 2026-08-27 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Orquesta login, sesión opaca y cambio seguro de contraseña.
Historial: 1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Validación de empresas disponibles para las credenciales.
Historial: 2.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Bloqueo temporal, registro de intentos y cierre de sesión.
Historial: 9.0.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Token asociado a la base de la empresa seleccionada.
===============================================================================
*/
using System.Security.Cryptography;

namespace OxiTigre.BLL.Security;

/// <summary>Aplica las reglas de autenticación sin exponer detalles de persistencia.</summary>
/// <param name="store">Persistencia de cuentas, intentos de acceso y sesiones.</param>
/// <param name="passwordHasher">Verificador y generador de hashes de contraseña.</param>
/// <param name="timeProvider">Reloj usado para expiración de sesión y bloqueo temporal.</param>
public sealed class AuthenticationService(
    IAuthenticationStore store,
    PasswordHasher passwordHasher,
    TimeProvider timeProvider
)
{
    private static readonly TimeSpan SessionDuration = TimeSpan.FromHours(8);
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    /// <summary>Devuelve únicamente las empresas cuyas credenciales fueron verificadas.</summary>
    /// <param name="username">Nombre de usuario que se normalizará.</param>
    /// <param name="password">Contraseña que se validará sin persistirla.</param>
    /// <param name="cancellationToken">Token que permite cancelar la consulta.</param>
    /// <returns>Empresas autorizadas junto con sus sucursales activas.</returns>
    public async Task<IReadOnlyList<CompanyAccessOption>> GetCompaniesAsync(
        string username,
        string password,
        CancellationToken cancellationToken
    )
    {
        var candidates = await store.FindCompanyCandidatesAsync(
            username.Trim().ToUpperInvariant(),
            cancellationToken
        );
        var companies = new List<CompanyAccessOption>();

        foreach (var candidate in candidates)
        {
            if (IsLocked(candidate.Account))
                continue;

            if (passwordHasher.Verify(password, candidate.Account))
            {
                companies.Add(
                    new CompanyAccessOption(
                        candidate.CompanyCode,
                        candidate.CompanyName,
                        candidate.Account.Branches ?? []
                    )
                );
            }
            else
            {
                await store.RecordLoginResultAsync(
                    candidate.CompanyCode,
                    candidate.Account.UserId,
                    false,
                    cancellationToken
                );
            }
        }

        return companies;
    }

    /// <summary>Valida credenciales y crea una sesión opaca persistiendo solo su hash.</summary>
    /// <param name="command">Credenciales, empresa, sucursal y origen del acceso.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación.</param>
    /// <returns>Sesión creada o <see langword="null"/> cuando el acceso no es válido.</returns>
    public async Task<AuthenticatedSession?> LoginAsync(
        LoginCommand command,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        var account = await store.FindAccountAsync(
            command.CompanyCode.Trim(),
            command.Username.Trim().ToUpperInvariant(),
            cancellationToken
        );
        if (account is null || IsLocked(account))
        {
            return null;
        }

        if (!passwordHasher.Verify(command.Password, account))
        {
            await store.RecordLoginResultAsync(
                command.CompanyCode.Trim(),
                account.UserId,
                false,
                cancellationToken
            );
            return null;
        }

        var branch = command.BranchId is null
            ? null
            : account.Branches?.SingleOrDefault(value => value.BranchId == command.BranchId.Value);
        if (command.BranchId is not null && branch is null)
        {
            return null;
        }

        await store.RecordLoginResultAsync(
            command.CompanyCode.Trim(),
            account.UserId,
            true,
            cancellationToken
        );

        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var secret = Convert
            .ToBase64String(tokenBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        var companyCode = command.CompanyCode.Trim().ToUpperInvariant();
        var token = $"{companyCode}.{secret}";
        var tokenHash = SHA512.HashData(System.Text.Encoding.UTF8.GetBytes(token));
        var expiresAtUtc = timeProvider.GetUtcNow().Add(SessionDuration);
        var correlationId = Guid.NewGuid();
        var sessionId = await store.CreateSessionAsync(
            companyCode,
            account.UserId,
            branch?.BranchId,
            tokenHash,
            expiresAtUtc,
            command.IpAddress,
            command.Application,
            correlationId,
            cancellationToken
        );

        return new AuthenticatedSession(
            sessionId,
            account.UserId,
            account.Username,
            $"{account.GivenNames} {account.Surname}",
            token,
            expiresAtUtc,
            account.MustChangePassword,
            account.Roles,
            account.Permissions,
            correlationId,
            branch?.BranchId,
            branch?.Code,
            branch?.Name
        );
    }

    /// <summary>Verifica la credencial actual y registra una contraseña nueva.</summary>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="username">Nombre de usuario normalizado de la cuenta.</param>
    /// <param name="currentPassword">Contraseña vigente que confirma la identidad.</param>
    /// <param name="newPassword">Nueva contraseña que reemplazará la vigente.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns><see langword="true"/> si la cuenta existe, la clave actual coincide y se guardó la nueva; en otro caso, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentException">La nueva contraseña no cumple la política de seguridad.</exception>
    public async Task<bool> ChangePasswordAsync(
        string companyCode,
        string username,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken
    )
    {
        PasswordHasher.ValidateNewPassword(newPassword);
        var account = await store.FindAccountAsync(
            companyCode.Trim(),
            username.Trim().ToUpperInvariant(),
            cancellationToken
        );
        if (account is null || !passwordHasher.Verify(currentPassword, account))
        {
            return false;
        }

        var replacement = passwordHasher.Hash(newPassword);
        await store.ChangePasswordAsync(
            companyCode.Trim(),
            account.UserId,
            replacement,
            cancellationToken
        );
        return true;
    }

    /// <summary>Valida un token opaco sin persistir ni registrar su texto original.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identidad de la sesión vigente o <see langword="null"/> si el token no identifica una sesión válida.</returns>
    /// <exception cref="ArgumentException">El token está vacío.</exception>
    public Task<SessionIdentity?> ValidateSessionAsync(
        string token,
        CancellationToken cancellationToken
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        var separator = token.IndexOf('.');
        if (separator <= 0)
            return Task.FromResult<SessionIdentity?>(null);
        var companyCode = token[..separator];
        var tokenHash = SHA512.HashData(System.Text.Encoding.UTF8.GetBytes(token));
        return store.ValidateSessionAsync(companyCode, tokenHash, cancellationToken);
    }

    /// <summary>Cierra voluntariamente la sesión autenticada.</summary>
    /// <param name="identity">Identidad autenticada que delimita empresa, sucursal, permisos y sesión.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la sesión queda revocada en la base de su empresa.</returns>
    /// <exception cref="ArgumentNullException">No se proporcionó identidad.</exception>
    public Task LogoutAsync(SessionIdentity identity, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(identity);
        return store.CloseSessionAsync(
            identity.CompanyCode,
            identity.SessionId,
            identity.UserId,
            cancellationToken
        );
    }

    /// <summary>Considera bloqueada la cuenta durante quince minutos desde el último bloqueo registrado.</summary>
    /// <param name="account">Cuenta consultada en la empresa candidata.</param>
    /// <returns><see langword="true"/> mientras no venza el período de bloqueo.</returns>
    private bool IsLocked(AuthenticationAccount account) =>
        account.BlockedAtUtc is { } blockedAt
        && blockedAt.Add(LockoutDuration) > timeProvider.GetUtcNow();
}
