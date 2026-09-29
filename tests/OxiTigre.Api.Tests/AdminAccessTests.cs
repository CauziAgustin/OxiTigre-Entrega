/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.Api.Tests.AdminAccessTests
Archivo: AdminAccessTests.cs | Versión: 1.0.0 | Fecha: 2026-08-21 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Verifica que el panel web acepte exclusivamente el rol administrador.
Historial: 1.0.0 | 2026-08-21 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
using OxiTigre.AdminWeb;

namespace OxiTigre.Api.Tests;

/// <summary>Protege la regla de acceso mínimo del panel administrativo.</summary>
public sealed class AdminAccessTests
{
    /// <summary>Comprueba aceptación sin distinguir mayúsculas y rechazo de roles de consulta.</summary>
    [Fact]
    public void IsAdministrator_AllowsOnlyAdministratorRole()
    {
        Assert.True(AdminAccess.IsAdministrator(["administrador"]));
        Assert.False(AdminAccess.IsAdministrator(["CONSULTA"]));
        Assert.False(AdminAccess.IsAdministrator([]));
    }
}
