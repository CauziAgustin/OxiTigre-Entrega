/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Security.UserSummary
Archivo: UserSummary.cs | Versión: 1.0.0 | Fecha: 2026-08-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Representa un usuario funcional con estado y roles para administración.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
namespace OxiTigre.BLL.Security;

/// <summary>Representa un usuario funcional para administración.</summary>
public sealed record UserSummary(long UserId, string Username, string GivenNames, string Surname, string Email, string StatusCode, IReadOnlyList<string> Roles);
