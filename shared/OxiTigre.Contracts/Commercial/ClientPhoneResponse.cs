/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.Contracts.Commercial.ClientPhoneResponse
Archivo: ClientPhoneResponse.cs | Versión: 1.0.0 | Fecha: 2026-08-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Expone un teléfono vigente de un cliente.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
namespace OxiTigre.Contracts.Commercial;

/// <summary>Informa identidad, clasificación, número, orden y capacidades del teléfono.</summary>
public sealed record ClientPhoneResponse(long PhoneId, string TypeCode, string TypeName, string? CountryCode,
    string? AreaCode, string Number, string? Extension, short Order, bool IsPrimary, bool AllowsWhatsApp,
    string? Observation, string StatusCode);
