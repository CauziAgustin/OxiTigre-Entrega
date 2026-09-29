/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Security.AuthenticatedSession
Archivo: AuthenticatedSession.cs | Versión: 1.0.0 | Fecha: 2026-08-19 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Devuelve la sesión opaca y autorizaciones tras un login correcto.
Historial: 1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
namespace OxiTigre.BLL.Security;

/// <summary>Representa una sesión autenticada; el token solo se entrega una vez.</summary>
public sealed record AuthenticatedSession(
    long SessionId,
    long UserId,
    string Username,
    string DisplayName,
    string Token,
    DateTimeOffset ExpiresAtUtc,
    bool MustChangePassword,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    Guid CorrelationId,
    long? BranchId = null,
    string? BranchCode = null,
    string? BranchName = null
);
