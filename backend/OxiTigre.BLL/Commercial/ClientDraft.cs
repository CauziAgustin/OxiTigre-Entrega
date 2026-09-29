/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Commercial.ClientDraft
Archivo: ClientDraft.cs | Versión: 1.0.0 | Fecha: 2026-08-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Transporta datos validados para crear o actualizar un cliente.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
namespace OxiTigre.BLL.Commercial;

/// <summary>Contiene datos generales y teléfonos que reemplazarán la versión activa del cliente.</summary>
public sealed record ClientDraft(string PersonType, string NameOrBusinessName, string? Surname,
    string? DocumentType, string? DocumentNumber, string? Email, string? Observation,
    IReadOnlyList<ClientPhoneDraft> Phones);
