/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.Contracts.Security.LoginResponse
Archivo: LoginResponse.cs | Versión: 1.0.0 | Fecha: 2026-08-19 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Expone la sesión autenticada sin datos criptográficos internos.
Historial: 1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
namespace OxiTigre.Contracts.Security;

/// <summary>Devuelve token opaco, vigencia, identidad y autorizaciones.</summary>
public sealed record LoginResponse(
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
