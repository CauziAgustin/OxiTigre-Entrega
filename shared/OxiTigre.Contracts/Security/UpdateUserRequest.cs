/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.Contracts.Security.UpdateUserRequest
Archivo: UpdateUserRequest.cs | Versión: 1.0.0 | Fecha: 2026-08-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Define los datos, estado y roles editables de un usuario.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
namespace OxiTigre.Contracts.Security;

/// <summary>Solicita actualizar un usuario y reemplazar sus roles activos.</summary>
public sealed record UpdateUserRequest(string GivenNames, string Surname, string Email, string StatusCode, IReadOnlyList<string> RoleCodes);
