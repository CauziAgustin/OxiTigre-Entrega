/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Security.SessionIdentity
Archivo: SessionIdentity.cs | Versión: 1.0.0 | Fecha: 2026-08-19 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Representa la identidad recuperada desde una sesión vigente.
Historial: 1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
namespace OxiTigre.BLL.Security;

/// <summary>Contiene usuario, sesión, vigencia, roles y permisos validados.</summary>
public sealed record SessionIdentity(
    long SessionId,
    long UserId,
    long CompanyId,
    string CompanyCode,
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
