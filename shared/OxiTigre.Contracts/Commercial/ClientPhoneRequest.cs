/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.Contracts.Commercial.ClientPhoneRequest
Archivo: ClientPhoneRequest.cs | Versión: 1.0.0 | Fecha: 2026-08-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Define un teléfono incluido en el alta o edición de un cliente.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
namespace OxiTigre.Contracts.Commercial;

/// <summary>Contiene clasificación, número, prioridad y capacidades de contacto.</summary>
public sealed record ClientPhoneRequest(
    string TypeCode,
    string? CountryCode,
    string? AreaCode,
    string Number,
    string? Extension,
    bool IsPrimary,
    bool AllowsWhatsApp,
    string? Observation);
