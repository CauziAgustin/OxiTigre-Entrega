/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Configuration.ConfigurationModels
Archivo: ConfigurationModels.cs | Versión: 1.0.0 | Fecha: 2026-08-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Define la configuración administrable y sus cambios con control de versión.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
namespace OxiTigre.BLL.Configuration;

/// <summary>Reúne toda la configuración necesaria para la pantalla administrativa.</summary>
public sealed record ConfigurationSnapshot(CompanyConfiguration Company, IReadOnlyList<BranchConfiguration> Branches,
    IReadOnlyList<OperatingUnitConfiguration> OperatingUnits, IReadOnlyList<PhoneTypeConfiguration> PhoneTypes,
    IReadOnlyList<StateConfiguration> States, IReadOnlyList<SystemParameterConfiguration> Parameters,
    IReadOnlyList<ModuleConfiguration> Modules, IReadOnlyList<ErrorCatalogConfiguration> Errors,
    IReadOnlyList<CatalogTranslationConfiguration> Translations, string CultureCode);

/// <summary>Representa la empresa propietaria de la base operativa.</summary>
public sealed record CompanyConfiguration(long CompanyId, string Code, string LegalName, string? TradeName,
    string? TaxId, string? Email, string StatusCode, byte[] RowVersion);

/// <summary>Representa una sucursal de la empresa.</summary>
public sealed record BranchConfiguration(long BranchId, string Code, string Name, string? Address, string? City,
    string? Province, string? PostalCode, string StatusCode, byte[] RowVersion);

/// <summary>Representa una unidad operativa dependiente de una sucursal.</summary>
public sealed record OperatingUnitConfiguration(long OperatingUnitId, long BranchId, string Code, string Name,
    string? Description, string StatusCode, byte[] RowVersion);

/// <summary>Representa un tipo de teléfono parametrizado.</summary>
public sealed record PhoneTypeConfiguration(long PhoneTypeId, string Code, string Name, string? Description,
    string StatusCode, byte[] RowVersion);

/// <summary>Representa un estado informativo cuyo código técnico permanece inmutable.</summary>
public sealed record StateConfiguration(long StateId, string Entity, string StatusCode, string Name, string? Description,
    bool IsInitial, bool IsFinal, short Order, DateTime? ValidUntilUtc, byte[] RowVersion);

/// <summary>Representa un parámetro tipado; los secretos nunca incluyen su valor ni referencia.</summary>
public sealed record SystemParameterConfiguration(long ParameterId, long? ModuleId, string? ModuleCode, string Key,
    string? Value, string DataType, bool IsSecret, bool HasSecretReference, string? Description, string StatusCode,
    byte[] RowVersion);

/// <summary>Representa un módulo y su número reservado para códigos de error.</summary>
public sealed record ModuleConfiguration(long ModuleId, short ModuleNumber, string Code, string Name,
    string? Description, short Order, string StatusCode, byte[] RowVersion);

/// <summary>Representa el diagnóstico central de un código de error.</summary>
public sealed record ErrorCatalogConfiguration(long ErrorId, long ModuleId, string ModuleCode, short ErrorNumber,
    long ErrorCode, string Name, string Description, string? ProbableCause, string? RecommendedAction,
    string Severity, string StatusCode, byte[] RowVersion);

/// <summary>Representa la traducción visible de un código estable.</summary>
public sealed record CatalogTranslationConfiguration(long TranslationId, string Entity, string Code, string CultureCode,
    string Name, string? Description, string StatusCode, byte[] RowVersion);

/// <summary>Datos editables de la empresa actual.</summary>
public sealed record CompanyChange(string LegalName, string? TradeName, string? TaxId, string? Email, byte[] RowVersion);

/// <summary>Datos para crear o editar una sucursal.</summary>
public sealed record BranchChange(long? BranchId, string Code, string Name, string? Address, string? City,
    string? Province, string? PostalCode, string StatusCode, byte[]? RowVersion);

/// <summary>Datos para crear o editar una unidad operativa.</summary>
public sealed record OperatingUnitChange(long? OperatingUnitId, long BranchId, string Code, string Name,
    string? Description, string StatusCode, byte[]? RowVersion);

/// <summary>Datos para crear o editar un tipo de teléfono.</summary>
public sealed record PhoneTypeChange(long? PhoneTypeId, string Code, string Name, string? Description,
    string StatusCode, byte[]? RowVersion);

/// <summary>Datos editables de un estado informativo.</summary>
public sealed record StateChange(long StateId, string Name, string? Description, short Order,
    DateTime? ValidUntilUtc, byte[] RowVersion);

/// <summary>Datos para crear o editar un parámetro tipado.</summary>
public sealed record SystemParameterChange(long? ParameterId, long? ModuleId, string Key, string? Value,
    string DataType, bool IsSecret, string? SecretReference, string? Description, string StatusCode, byte[]? RowVersion);

/// <summary>Datos para crear o editar un módulo.</summary>
public sealed record ModuleChange(long? ModuleId, short ModuleNumber, string Code, string Name, string? Description,
    short Order, string StatusCode, byte[]? RowVersion);

/// <summary>Datos para crear un código de error central.</summary>
public sealed record CreateErrorCatalogChange(long ModuleId, string Name, string Description, string? ProbableCause,
    string? RecommendedAction, string Severity);

/// <summary>Datos editables de un código de error existente.</summary>
public sealed record UpdateErrorCatalogChange(long ErrorId, string Name, string Description, string? ProbableCause,
    string? RecommendedAction, string Severity, string StatusCode, byte[] RowVersion);

/// <summary>Datos para crear o editar una traducción.</summary>
public sealed record CatalogTranslationChange(long? TranslationId, string Entity, string Code, string CultureCode,
    string Name, string? Description, string StatusCode, byte[]? RowVersion);

/// <summary>Informa un resultado funcional controlado producido por Configuración.</summary>
public sealed class ConfigurationOperationException(long errorCode, string message) : Exception(message)
{
    /// <summary>Obtiene el código funcional registrado.</summary>
    public long ErrorCode { get; } = errorCode;
}
