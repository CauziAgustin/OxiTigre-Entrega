/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.Contracts.Security.CompanyOptionResponse
Archivo: CompanyOptionResponse.cs | Versión: 1.0.0 | Fecha: 2026-08-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Expone una empresa habilitada para el inicio de sesión.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
namespace OxiTigre.Contracts.Security;

/// <summary>Representa una empresa que el usuario autenticado puede seleccionar.</summary>
public sealed record CompanyOptionResponse(
    string Code,
    string Name,
    IReadOnlyList<BranchOptionResponse>? Branches = null
);
