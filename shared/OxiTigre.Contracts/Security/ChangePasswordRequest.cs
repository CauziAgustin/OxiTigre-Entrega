/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.Contracts.Security.ChangePasswordRequest
Archivo: ChangePasswordRequest.cs | Versión: 1.0.0 | Fecha: 2026-08-19 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Define el cambio de contraseña verificando la credencial anterior.
Historial: 1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
namespace OxiTigre.Contracts.Security;

/// <summary>Contiene identidad, contraseña actual y contraseña nueva.</summary>
public sealed record ChangePasswordRequest(string CompanyCode, string Username, string CurrentPassword, string NewPassword);
