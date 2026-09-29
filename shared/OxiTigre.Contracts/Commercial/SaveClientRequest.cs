/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.Contracts.Commercial.SaveClientRequest
Archivo: SaveClientRequest.cs | Versión: 1.0.0 | Fecha: 2026-08-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Define los datos editables de un cliente y sus teléfonos.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
namespace OxiTigre.Contracts.Commercial;

/// <summary>Solicita crear o actualizar un cliente con su colección completa de teléfonos activos.</summary>
public sealed record SaveClientRequest(
    string PersonType,
    string NameOrBusinessName,
    string? Surname,
    string? DocumentType,
    string? DocumentNumber,
    string? Email,
    string? Observation,
    IReadOnlyList<ClientPhoneRequest> Phones);
