/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.Contracts.Commercial.ClientCatalogResponse
Archivo: ClientCatalogResponse.cs | Versión: 1.0.0 | Fecha: 2026-08-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Expone catálogos parametrizados para la carga de clientes.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
namespace OxiTigre.Contracts.Commercial;

/// <summary>Reúne documentos, países y tipos de teléfono activos.</summary>
public sealed record ClientCatalogResponse(IReadOnlyList<DocumentTypeOptionResponse> DocumentTypes,
    IReadOnlyList<CountryOptionResponse> Countries, IReadOnlyList<PhoneTypeOptionResponse> PhoneTypes);

/// <summary>Describe un tipo de documento y las personas a las que aplica.</summary>
public sealed record DocumentTypeOptionResponse(string Code, string Name, bool AppliesToNaturalPerson, bool AppliesToLegalPerson);

/// <summary>Describe un país seleccionable y su prefijo telefónico sugerido.</summary>
public sealed record CountryOptionResponse(string Code, string Name, string? PhoneCode, bool IsDefault);

/// <summary>Describe un tipo de teléfono activo.</summary>
public sealed record PhoneTypeOptionResponse(string Code, string Name);
