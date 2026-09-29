/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Commercial.ClientPhone
Archivo: ClientPhone.cs | Versión: 1.0.0 | Fecha: 2026-08-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Representa un teléfono del cliente dentro de las reglas comerciales.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
namespace OxiTigre.BLL.Commercial;

/// <summary>Contiene datos normalizados, prioridad, capacidades y estado de un teléfono.</summary>
public sealed record ClientPhone(long PhoneId, string TypeCode, string TypeName, string? CountryCode,
    string? AreaCode, string Number, string? Extension, short Order, bool IsPrimary, bool AllowsWhatsApp,
    string? Observation, string StatusCode);
