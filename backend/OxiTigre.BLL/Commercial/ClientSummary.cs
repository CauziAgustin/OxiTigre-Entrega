/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Commercial.ClientSummary
Archivo: ClientSummary.cs | Versión: 1.0.0 | Fecha: 2026-08-19 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Representa el resumen de un cliente para consultas comerciales.
Historial: 1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
namespace OxiTigre.BLL.Commercial;

/// <summary>Contiene identidad, contacto principal, estado y cantidad de teléfonos.</summary>
public sealed record ClientSummary(
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
