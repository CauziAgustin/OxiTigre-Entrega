/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.Contracts.Commercial.SavedClientDraftResponse
Archivo: SavedClientDraftResponse.cs | Versión: 1.0.0 | Fecha: 2026-08-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Expone la precarga incompleta guardada por el usuario.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
namespace OxiTigre.Contracts.Commercial;

/// <summary>Contiene un borrador de cliente y la fecha de su última modificación.</summary>
public sealed record SavedClientDraftResponse(SaveClientRequest Draft, DateTimeOffset SavedAtUtc);
