/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Security.AuthenticationAccount
Archivo: AuthenticationAccount.cs | Versión: 1.1.0 | Fecha: 2026-08-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Representa la cuenta segura recuperada para autenticación.
Historial: 1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Datos de bloqueo por intentos fallidos.
===============================================================================
*/
namespace OxiTigre.BLL.Security;

/// <summary>Contiene identidad, credencial derivada y autorizaciones efectivas.</summary>
public sealed record AuthenticationAccount(
    long UserId,
    long CompanyId,
    string Username,
    string GivenNames,
    string Surname,
    string Email,
    byte[] PasswordHash,
    byte[] PasswordSalt,
    string PasswordAlgorithm,
    int PasswordIterations,
    bool MustChangePassword,
    short FailedAttempts,
    DateTimeOffset? BlockedAtUtc,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<BranchLoginOption>? Branches = null
);
