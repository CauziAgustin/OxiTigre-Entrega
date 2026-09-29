/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.Contracts.Commercial.ClientSummaryResponse
Archivo: ClientSummaryResponse.cs | Versión: 1.0.0 | Fecha: 2026-08-19 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Expone el resumen de cliente consumido por WinForms.
Historial: 1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
namespace OxiTigre.Contracts.Commercial;

/// <summary>Informa datos generales, teléfono principal y cantidad de contactos.</summary>
public sealed record ClientSummaryResponse(
    long ClientId,
    string Code,
    string PersonType,
    string NameOrBusinessName,
    string? Surname,
    string? DocumentNumber,
    string? Email,
    string StatusCode,
    string? PrimaryPhone,
    long PhoneCount);
