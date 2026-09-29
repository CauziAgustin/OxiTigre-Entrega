/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.AdminWeb.AdminAccess
Archivo: AdminAccess.cs | Versión: 1.0.0 | Fecha: 2026-08-21 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Centraliza la condición mínima de ingreso al panel administrativo web.
Historial: 1.0.0 | 2026-08-21 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
namespace OxiTigre.AdminWeb;

/// <summary>Valida que la identidad de aplicación posea el rol administrativo.</summary>
public static class AdminAccess
{
    /// <summary>Indica si los roles recibidos habilitan el panel web.</summary>
    public static bool IsAdministrator(IEnumerable<string> roles) =>
        roles.Contains("ADMINISTRADOR", StringComparer.OrdinalIgnoreCase);
}
