/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.Contracts.Security.CreateUserRequest
Archivo: CreateUserRequest.cs | Versión: 2.0.0 | Fecha: 2026-08-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Define el alta de un usuario funcional con roles y contraseña temporal.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 2.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Soporte de múltiples roles.
===============================================================================
*/
namespace OxiTigre.Contracts.Security;

/// <summary>Solicita el alta de un usuario funcional con sus roles iniciales.</summary>
public sealed record CreateUserRequest(string GivenNames, string Surname, string Email, IReadOnlyList<string> RoleCodes, string TemporaryPassword);
