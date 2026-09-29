/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Security.BranchLoginOption
Archivo: BranchLoginOption.cs | Versión: 1.0.0 | Fecha: 2026-09-02 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Representa una sucursal activa elegible durante el acceso.
Historial: 1.0.0 | 2026-09-02 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
namespace OxiTigre.BLL.Security;

/// <summary>Identifica una sucursal activa perteneciente a la empresa autenticada.</summary>
public sealed record BranchLoginOption(long BranchId, string Code, string Name);
