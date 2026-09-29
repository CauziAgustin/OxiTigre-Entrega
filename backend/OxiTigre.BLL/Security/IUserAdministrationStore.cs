/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Security.IUserAdministrationStore
Archivo: IUserAdministrationStore.cs | Versión: 2.0.0 | Fecha: 2026-08-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Define persistencia aislada y auditable para administrar usuarios.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Contexto de empresa y sesión para auditoría.
Historial: 2.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Edición, roles, reset y revocación de sesiones.
===============================================================================
*/
namespace OxiTigre.BLL.Security;

/// <summary>Define la persistencia necesaria para administrar usuarios funcionales.</summary>
public interface IUserAdministrationStore
{
    /// <summary>Lista usuarios y roles de una empresa.</summary>
    /// <param name="companyId">Identificador interno de la empresa.</param>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Usuarios y roles asignados que puede administrar la sesión.</returns>
    Task<IReadOnlyList<UserSummary>> ListAsync(
        long companyId,
        string companyCode,
        long sessionId,
        long userId,
        CancellationToken cancellationToken
    );

    /// <summary>Lista roles activos de una empresa.</summary>
    /// <param name="companyId">Identificador interno de la empresa.</param>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Roles disponibles para asignar a usuarios.</returns>
    Task<IReadOnlyList<RoleSummary>> ListRolesAsync(
        long companyId,
        string companyCode,
        long sessionId,
        long userId,
        CancellationToken cancellationToken
    );

    /// <summary>Crea un usuario activo con su rol inicial.</summary>
    /// <param name="companyId">Identificador interno de la empresa.</param>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="username">Nombre de usuario normalizado de la cuenta.</param>
    /// <param name="givenNames">Nombres del usuario.</param>
    /// <param name="surname">Apellido del usuario.</param>
    /// <param name="email">Correo electrónico de contacto del usuario.</param>
    /// <param name="roleCodes">Códigos de roles que se asignan al usuario.</param>
    /// <param name="passwordHash">Hash de contraseña; nunca contiene la clave en texto claro.</param>
    /// <param name="administratorId">Identificador del administrador que autoriza la operación.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador del usuario creado.</returns>
    Task<long> CreateAsync(
        long companyId,
        string companyCode,
        string username,
        string givenNames,
        string surname,
        string email,
        IReadOnlyList<string> roleCodes,
        PasswordHash passwordHash,
        long administratorId,
        long sessionId,
        CancellationToken cancellationToken
    );

    /// <summary>Actualiza datos, estado y roles activos de un usuario.</summary>
    /// <param name="companyId">Identificador interno de la empresa.</param>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="targetUserId">Identificador del usuario sobre el que actúa el administrador.</param>
    /// <param name="givenNames">Nombres del usuario.</param>
    /// <param name="surname">Apellido del usuario.</param>
    /// <param name="email">Correo electrónico de contacto del usuario.</param>
    /// <param name="statusCode">Código de estado admitido por el dominio.</param>
    /// <param name="roleCodes">Códigos de roles que se asignan al usuario.</param>
    /// <param name="administratorId">Identificador del administrador que autoriza la operación.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    Task UpdateAsync(
        long companyId,
        string companyCode,
        long targetUserId,
        string givenNames,
        string surname,
        string email,
        string statusCode,
        IReadOnlyList<string> roleCodes,
        long administratorId,
        long sessionId,
        CancellationToken cancellationToken
    );

    /// <summary>Reemplaza la contraseña por una temporal y revoca sesiones vigentes.</summary>
    /// <param name="companyId">Identificador interno de la empresa.</param>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="targetUserId">Identificador del usuario sobre el que actúa el administrador.</param>
    /// <param name="passwordHash">Hash de contraseña; nunca contiene la clave en texto claro.</param>
    /// <param name="administratorId">Identificador del administrador que autoriza la operación.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    Task ResetPasswordAsync(
        long companyId,
        string companyCode,
        long targetUserId,
        PasswordHash passwordHash,
        long administratorId,
        long sessionId,
        CancellationToken cancellationToken
    );

    /// <summary>Revoca las sesiones vigentes de un usuario administrado.</summary>
    /// <param name="companyId">Identificador interno de la empresa.</param>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="targetUserId">Identificador del usuario sobre el que actúa el administrador.</param>
    /// <param name="administratorId">Identificador del administrador que autoriza la operación.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    Task RevokeSessionsAsync(
        long companyId,
        string companyCode,
        long targetUserId,
        long administratorId,
        long sessionId,
        CancellationToken cancellationToken
    );
}
