/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.Contracts.Security.LoginRequest
Archivo: LoginRequest.cs | Versión: 1.0.0 | Fecha: 2026-08-19 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Define la solicitud pública de inicio de sesión.
Historial: 1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
namespace OxiTigre.Contracts.Security;

/// <summary>Contiene empresa, usuario y contraseña que serán validados.</summary>
public sealed record LoginRequest(
    string CompanyCode,
    string Username,
    string Password,
    long? BranchId = null
);
