/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Security.LoginCommand
Archivo: LoginCommand.cs | Versión: 1.0.0 | Fecha: 2026-08-19 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Define los datos requeridos para iniciar una sesión funcional.
Historial: 1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
namespace OxiTigre.BLL.Security;

/// <summary>Solicita autenticación para una empresa y un origen determinados.</summary>
public sealed record LoginCommand(
    string CompanyCode,
    string Username,
    string Password,
    string? IpAddress,
    string Application,
    long? BranchId = null
);
