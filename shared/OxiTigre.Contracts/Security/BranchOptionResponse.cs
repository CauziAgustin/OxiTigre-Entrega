/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.Contracts.Security.BranchOptionResponse
Archivo: BranchOptionResponse.cs | Versión: 1.0.0 | Fecha: 2026-09-02 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Expone una sucursal activa durante la selección de acceso.
Historial: 1.0.0 | 2026-09-02 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
namespace OxiTigre.Contracts.Security;

/// <summary>Describe una sucursal disponible dentro de una empresa validada.</summary>
public sealed record BranchOptionResponse(long BranchId, string Code, string Name);
