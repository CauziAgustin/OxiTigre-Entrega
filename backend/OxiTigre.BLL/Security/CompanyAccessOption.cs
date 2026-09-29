/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Security.CompanyAccessOption
Archivo: CompanyAccessOption.cs | Versión: 1.0.0 | Fecha: 2026-09-02 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Agrupa una empresa validada y sus sucursales activas.
Historial: 1.0.0 | 2026-09-02 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
namespace OxiTigre.BLL.Security;

/// <summary>Contiene una empresa autorizada y los lugares habilitados para trabajar.</summary>
public sealed record CompanyAccessOption(
    string Code,
    string Name,
    IReadOnlyList<BranchLoginOption> Branches
);
