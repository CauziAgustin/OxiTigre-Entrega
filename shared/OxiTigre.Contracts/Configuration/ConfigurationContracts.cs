/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.Contracts.Configuration.ConfigurationContracts
Archivo: ConfigurationContracts.cs | Versión: 1.0.0 | Fecha: 2026-08-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Define contratos HTTP de Configuración sin aceptar contexto de seguridad desde el cliente.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
namespace OxiTigre.Contracts.Configuration;

/// <summary>Reúne la configuración administrable de la empresa autenticada.</summary>
public sealed record ConfigurationSnapshotResponse(CompanyConfigurationResponse Company,
    IReadOnlyList<BranchConfigurationResponse> Branches, IReadOnlyList<OperatingUnitConfigurationResponse> OperatingUnits,
    IReadOnlyList<PhoneTypeConfigurationResponse> PhoneTypes, IReadOnlyList<StateConfigurationResponse> States,
    IReadOnlyList<SystemParameterConfigurationResponse> Parameters, IReadOnlyList<ModuleConfigurationResponse> Modules,
    IReadOnlyList<ErrorCatalogConfigurationResponse> Errors, IReadOnlyList<CatalogTranslationConfigurationResponse> Translations,
    string CultureCode);

/// <summary>Expone la empresa actual y su versión editable.</summary>
public sealed record CompanyConfigurationResponse(long CompanyId, string Code, string LegalName, string? TradeName,
    string? TaxId, string? Email, string StatusCode, string RowVersion);
/// <summary>Expone una sucursal.</summary>
public sealed record BranchConfigurationResponse(long BranchId, string Code, string Name, string? Address, string? City,
    string? Province, string? PostalCode, string StatusCode, string RowVersion);
/// <summary>Expone una unidad operativa.</summary>
public sealed record OperatingUnitConfigurationResponse(long OperatingUnitId, long BranchId, string Code, string Name,
    string? Description, string StatusCode, string RowVersion);
/// <summary>Expone un tipo de teléfono.</summary>
public sealed record PhoneTypeConfigurationResponse(long PhoneTypeId, string Code, string Name, string? Description,
    string StatusCode, string RowVersion);
/// <summary>Expone un estado informativo.</summary>
public sealed record StateConfigurationResponse(long StateId, string Entity, string StatusCode, string Name,
    string? Description, bool IsInitial, bool IsFinal, short Order, DateTime? ValidUntilUtc, string RowVersion);
/// <summary>Expone un parámetro sin secretos.</summary>
public sealed record SystemParameterConfigurationResponse(long ParameterId, long? ModuleId, string? ModuleCode, string Key,
    string? Value, string DataType, bool IsSecret, bool HasSecretReference, string? Description, string StatusCode, string RowVersion);
/// <summary>Expone un módulo y su prefijo numérico.</summary>
public sealed record ModuleConfigurationResponse(long ModuleId, short ModuleNumber, string Code, string Name,
    string? Description, short Order, string StatusCode, string RowVersion);
/// <summary>Expone un diagnóstico de error central.</summary>
public sealed record ErrorCatalogConfigurationResponse(long ErrorId, long ModuleId, string ModuleCode, short ErrorNumber,
    long ErrorCode, string Name, string Description, string? ProbableCause, string? RecommendedAction,
    string Severity, string StatusCode, string RowVersion);
/// <summary>Expone una traducción de catálogo.</summary>
public sealed record CatalogTranslationConfigurationResponse(long TranslationId, string Entity, string Code,
    string CultureCode, string Name, string? Description, string StatusCode, string RowVersion);
/// <summary>Expone la preferencia de idioma actual.</summary>
public sealed record LanguagePreferenceResponse(string CultureCode);

/// <summary>Solicita actualizar la empresa actual.</summary>
public sealed record UpdateCompanyConfigurationRequest(string LegalName, string? TradeName, string? TaxId, string? Email, string RowVersion);
/// <summary>Solicita crear o editar una sucursal.</summary>
public sealed record SaveBranchConfigurationRequest(long? BranchId, string Code, string Name, string? Address, string? City,
    string? Province, string? PostalCode, string StatusCode, string? RowVersion);
/// <summary>Solicita crear o editar una unidad operativa.</summary>
public sealed record SaveOperatingUnitConfigurationRequest(long? OperatingUnitId, long BranchId, string Code, string Name,
    string? Description, string StatusCode, string? RowVersion);
/// <summary>Solicita crear o editar un tipo de teléfono.</summary>
public sealed record SavePhoneTypeConfigurationRequest(long? PhoneTypeId, string Code, string Name, string? Description,
    string StatusCode, string? RowVersion);
/// <summary>Solicita editar un estado informativo.</summary>
public sealed record UpdateStateConfigurationRequest(long StateId, string Name, string? Description, short Order,
    DateTime? ValidUntilUtc, string RowVersion);
/// <summary>Solicita crear o editar un parámetro.</summary>
public sealed record SaveSystemParameterConfigurationRequest(long? ParameterId, long? ModuleId, string Key, string? Value,
    string DataType, bool IsSecret, string? SecretReference, string? Description, string StatusCode, string? RowVersion);
/// <summary>Solicita crear o editar un módulo.</summary>
public sealed record SaveModuleConfigurationRequest(long? ModuleId, short ModuleNumber, string Code, string Name,
    string? Description, short Order, string StatusCode, string? RowVersion);
/// <summary>Solicita crear un código de error.</summary>
public sealed record CreateErrorCatalogRequest(long ModuleId, string Name, string Description, string? ProbableCause,
    string? RecommendedAction, string Severity);
/// <summary>Solicita editar el diagnóstico de un código de error.</summary>
public sealed record UpdateErrorCatalogRequest(long ErrorId, string Name, string Description, string? ProbableCause,
    string? RecommendedAction, string Severity, string StatusCode, string RowVersion);
/// <summary>Solicita crear o editar una traducción.</summary>
public sealed record SaveCatalogTranslationRequest(long? TranslationId, string Entity, string Code, string CultureCode,
    string Name, string? Description, string StatusCode, string? RowVersion);
/// <summary>Solicita guardar la preferencia de idioma.</summary>
public sealed record SaveLanguagePreferenceRequest(string CultureCode);
/// <summary>Devuelve el identificador creado o actualizado.</summary>
public sealed record SavedConfigurationResponse(long Id);
/// <summary>Devuelve el código de error generado.</summary>
public sealed record CreatedErrorCodeResponse(long ErrorCode);
