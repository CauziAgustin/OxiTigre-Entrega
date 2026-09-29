/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Commercial.ClientDetails
Archivo: ClientDetails.cs | Versión: 1.0.0 | Fecha: 2026-08-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Representa el detalle completo de un cliente comercial.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
namespace OxiTigre.BLL.Commercial;

/// <summary>Contiene identidad, datos generales, estado y teléfonos de un cliente.</summary>
public sealed record ClientDetails(long ClientId, string Code, string PersonType, string NameOrBusinessName,
    string? Surname, string? DocumentType, string? DocumentNumber, string? Email, string? Observation,
    string StatusCode, IReadOnlyList<ClientPhone> Phones);
