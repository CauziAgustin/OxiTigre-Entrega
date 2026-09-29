/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Security.IAuthenticationStore
Archivo: IAuthenticationStore.cs | Versión: 2.0.0 | Fecha: 2026-08-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Define las operaciones persistentes requeridas por autenticación.
Historial: 1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Consulta de cuentas candidatas por empresa.
Historial: 2.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Control de intentos y cierre de sesión.
===============================================================================
*/
namespace OxiTigre.BLL.Security;

/// <summary>Abstrae el acceso a cuentas, sesiones y credenciales.</summary>
public interface IAuthenticationStore
{
    /// <summary>Busca las cuentas activas de un usuario en las empresas disponibles localmente.</summary>
    /// <param name="username">Nombre de usuario normalizado de la cuenta.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Cuentas candidatas por empresa para el nombre de usuario indicado.</returns>
    Task<IReadOnlyList<CompanyLoginCandidate>> FindCompanyCandidatesAsync(
        string username,
        CancellationToken cancellationToken
    );

    /// <summary>Busca una cuenta activa con sus roles y permisos.</summary>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="username">Nombre de usuario normalizado de la cuenta.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Cuenta de la empresa indicada, o null si no existe.</returns>
    Task<AuthenticationAccount?> FindAccountAsync(
        string companyCode,
        string username,
        CancellationToken cancellationToken
    );

    /// <summary>Persiste el hash de un token y su sucursal operativa, y devuelve el identificador.</summary>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="branchId">Identificador interno de la sucursal activa.</param>
    /// <param name="tokenHash">SHA-512 del token opaco; el token original no se persiste.</param>
    /// <param name="expiresAtUtc">Instante UTC a partir del cual la sesión deja de ser válida.</param>
    /// <param name="ipAddress">Dirección IP de origen registrada para la sesión.</param>
    /// <param name="application">Nombre de la aplicación desde la que se abrió la sesión.</param>
    /// <param name="correlationId">Identificador que correlaciona la operación entre capas y registros.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador persistido de la nueva sesión opaca.</returns>
    Task<long> CreateSessionAsync(
        string companyCode,
        long userId,
        long? branchId,
        byte[] tokenHash,
        DateTimeOffset expiresAtUtc,
        string? ipAddress,
        string application,
        Guid correlationId,
        CancellationToken cancellationToken
    );

    /// <summary>Recupera la identidad correspondiente a un hash de sesión vigente.</summary>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="tokenHash">SHA-512 del token presentado para buscar la sesión sin exponerlo.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identidad asociada al hash de token vigente, o null si no existe.</returns>
    Task<SessionIdentity?> ValidateSessionAsync(
        string companyCode,
        byte[] tokenHash,
        CancellationToken cancellationToken
    );

    /// <summary>Reemplaza la credencial derivada y revoca sesiones anteriores.</summary>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="passwordHash">Hash de contraseña; nunca contiene la clave en texto claro.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    Task ChangePasswordAsync(
        string companyCode,
        long userId,
        PasswordHash passwordHash,
        CancellationToken cancellationToken
    );

    /// <summary>Actualiza el contador de intentos y el bloqueo temporal de una cuenta.</summary>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="succeeded">Indica si se aplica la condición 'succeeded'.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    Task RecordLoginResultAsync(
        string companyCode,
        long userId,
        bool succeeded,
        CancellationToken cancellationToken
    );

    /// <summary>Cierra la sesión indicada si pertenece al usuario autenticado.</summary>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    Task CloseSessionAsync(
        string companyCode,
        long sessionId,
        long userId,
        CancellationToken cancellationToken
    );
}
