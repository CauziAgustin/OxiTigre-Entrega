/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Security.CompanyLoginCandidate
Archivo: CompanyLoginCandidate.cs | Versión: 1.0.0 | Fecha: 2026-08-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Relaciona una cuenta válida con su empresa de acceso.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
namespace OxiTigre.BLL.Security;

/// <summary>Contiene la credencial de una empresa disponible para el usuario.</summary>
public sealed record CompanyLoginCandidate(string CompanyCode, string CompanyName, AuthenticationAccount Account);
