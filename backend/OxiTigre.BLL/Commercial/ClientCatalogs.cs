/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Commercial.ClientCatalogs
Archivo: ClientCatalogs.cs | Versión: 1.0.0 | Fecha: 2026-08-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Representa catálogos activos para cargar clientes sin valores libres.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
namespace OxiTigre.BLL.Commercial;

/// <summary>Reúne las opciones activas requeridas por la pantalla de clientes.</summary>
public sealed record ClientCatalogs(IReadOnlyList<DocumentTypeOption> DocumentTypes,
    IReadOnlyList<CountryOption> Countries, IReadOnlyList<PhoneTypeOption> PhoneTypes);

/// <summary>Representa un tipo de documento parametrizado.</summary>
public sealed record DocumentTypeOption(string Code, string Name, bool AppliesToNaturalPerson, bool AppliesToLegalPerson);

/// <summary>Representa un país y su prefijo telefónico sugerido.</summary>
public sealed record CountryOption(string Code, string Name, string? PhoneCode, bool IsDefault);

/// <summary>Representa un tipo de teléfono parametrizado.</summary>
public sealed record PhoneTypeOption(string Code, string Name);
