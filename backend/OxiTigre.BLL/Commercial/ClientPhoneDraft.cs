/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Commercial.ClientPhoneDraft
Archivo: ClientPhoneDraft.cs | Versión: 1.0.0 | Fecha: 2026-08-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Transporta un teléfono recibido para guardar un cliente.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
namespace OxiTigre.BLL.Commercial;

/// <summary>Contiene clasificación, número, prioridad y capacidades de un teléfono solicitado.</summary>
public sealed record ClientPhoneDraft(string TypeCode, string? CountryCode, string? AreaCode, string Number,
    string? Extension, bool IsPrimary, bool AllowsWhatsApp, string? Observation);
