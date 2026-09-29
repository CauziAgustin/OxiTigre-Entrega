/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.Contracts.Security.UserSummaryResponse
Archivo: UserSummaryResponse.cs | Versión: 1.0.0 | Fecha: 2026-08-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Expone identidad, estado y roles de un usuario sin datos criptográficos.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
namespace OxiTigre.Contracts.Security;

/// <summary>Resume un usuario funcional sin exponer su credencial.</summary>
public sealed record UserSummaryResponse(long UserId, string Username, string GivenNames, string Surname, string Email, string StatusCode, IReadOnlyList<string> Roles);
