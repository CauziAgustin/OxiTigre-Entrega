/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.Contracts.Commercial.ClientDetailResponse
Archivo: ClientDetailResponse.cs | Versión: 1.0.0 | Fecha: 2026-08-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Expone todos los datos editables y teléfonos de un cliente.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
namespace OxiTigre.Contracts.Commercial;

/// <summary>Informa identidad funcional, datos generales, estado y contactos del cliente.</summary>
public sealed record ClientDetailResponse(long ClientId, string Code, string PersonType, string NameOrBusinessName,
    string? Surname, string? DocumentType, string? DocumentNumber, string? Email, string? Observation,
    string StatusCode, IReadOnlyList<ClientPhoneResponse> Phones);
