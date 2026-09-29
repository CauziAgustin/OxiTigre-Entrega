/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.Contracts.Security.ResetUserPasswordRequest
Archivo: ResetUserPasswordRequest.cs | Versión: 1.0.0 | Fecha: 2026-08-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Transporta una contraseña temporal para un reinicio administrativo.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
namespace OxiTigre.Contracts.Security;

/// <summary>Solicita reemplazar la contraseña por una credencial temporal.</summary>
public sealed record ResetUserPasswordRequest(string TemporaryPassword);
