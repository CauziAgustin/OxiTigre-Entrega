/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.Contracts.Security.SessionResponse
Archivo: SessionResponse.cs | Versión: 1.0.0 | Fecha: 2026-08-19 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Expone la identidad correspondiente a un token de sesión válido.
Historial: 1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
namespace OxiTigre.Contracts.Security;

/// <summary>Informa identidad, vigencia y autorizaciones de la sesión.</summary>
public sealed record SessionResponse(
    long SessionId,
    long UserId,
    string Username,
    string DisplayName,
    DateTimeOffset ExpiresAtUtc,
    bool MustChangePassword,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    Guid CorrelationId,
    long? BranchId = null,
    string? BranchCode = null,
    string? BranchName = null
);
