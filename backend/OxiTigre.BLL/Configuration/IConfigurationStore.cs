/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Configuration.IConfigurationStore
Archivo: IConfigurationStore.cs | Versión: 1.0.0 | Fecha: 2026-08-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Define persistencia de Configuración sin exponer SQL a la lógica funcional.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
namespace OxiTigre.BLL.Configuration;

/// <summary>Contrato de almacenamiento para Configuración y catálogo de errores.</summary>
public interface IConfigurationStore
{
    /// <summary>Recupera el conjunto administrable aislado por empresa.</summary>
    /// <param name="companyId">Identificador interno de la empresa.</param>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Empresas, sucursales, catálogos, parámetros y módulos configurables.</returns>
    Task<ConfigurationSnapshot> GetAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        CancellationToken cancellationToken
    );

    /// <summary>Obtiene la cultura preferida o español cuando todavía no existe.</summary>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Código de cultura guardado para el usuario.</returns>
    Task<string> GetCultureAsync(
        string companyCode,
        long userId,
        long sessionId,
        CancellationToken cancellationToken
    );

    /// <summary>Guarda la cultura preferida del usuario.</summary>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="cultureCode">Código de cultura específico y canónico, por ejemplo es-AR.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    Task SaveCultureAsync(
        string companyCode,
        long userId,
        long sessionId,
        string cultureCode,
        CancellationToken cancellationToken
    );

    /// <summary>Actualiza la empresa de la sesión.</summary>
    /// <param name="companyId">Identificador interno de la empresa.</param>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="change">Datos legales, estado y versión de la empresa que se modificará.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    Task UpdateCompanyAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        CompanyChange change,
        CancellationToken cancellationToken
    );

    /// <summary>Crea o actualiza una sucursal y devuelve su identificador.</summary>
    /// <param name="companyId">Identificador interno de la empresa.</param>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="change">Código, nombre, domicilio, estado y versión de la sucursal.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador de la sucursal creada o actualizada.</returns>
    Task<long> SaveBranchAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        BranchChange change,
        CancellationToken cancellationToken
    );

    /// <summary>Crea o actualiza una unidad y devuelve su identificador.</summary>
    /// <param name="companyId">Identificador interno de la empresa.</param>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="change">Código, descripción y estado de la unidad operativa.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador de la unidad operativa creada o actualizada.</returns>
    Task<long> SaveOperatingUnitAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        OperatingUnitChange change,
        CancellationToken cancellationToken
    );

    /// <summary>Crea o actualiza un tipo de teléfono y devuelve su identificador.</summary>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="change">Código, nombre y estado del tipo de teléfono.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador del tipo de teléfono creado o actualizado.</returns>
    Task<long> SavePhoneTypeAsync(
        string companyCode,
        long userId,
        long sessionId,
        PhoneTypeChange change,
        CancellationToken cancellationToken
    );

    /// <summary>Actualiza un estado informativo.</summary>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="change">Código, nombre y versión del estado configurable.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    Task UpdateStateAsync(
        string companyCode,
        long userId,
        long sessionId,
        StateChange change,
        CancellationToken cancellationToken
    );

    /// <summary>Crea o actualiza un parámetro y devuelve su identificador.</summary>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="change">Clave, valor y versión del parámetro del sistema.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador del parámetro guardado.</returns>
    Task<long> SaveParameterAsync(
        string companyCode,
        long userId,
        long sessionId,
        SystemParameterChange change,
        CancellationToken cancellationToken
    );

    /// <summary>Crea o actualiza un módulo y devuelve su identificador.</summary>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="change">Módulo, estado y versión de la configuración que se guardará.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador del módulo configurado.</returns>
    Task<long> SaveModuleAsync(
        string companyCode,
        long userId,
        long sessionId,
        ModuleChange change,
        CancellationToken cancellationToken
    );

    /// <summary>Crea un diagnóstico y devuelve el código generado.</summary>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="change">Código y descripción del nuevo error funcional.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Código numérico asignado al error funcional.</returns>
    Task<long> CreateErrorAsync(
        string companyCode,
        long userId,
        long sessionId,
        CreateErrorCatalogChange change,
        CancellationToken cancellationToken
    );

    /// <summary>Actualiza el diagnóstico de un código existente.</summary>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="change">Código, descripción, estado y versión del error funcional.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    Task UpdateErrorAsync(
        string companyCode,
        long userId,
        long sessionId,
        UpdateErrorCatalogChange change,
        CancellationToken cancellationToken
    );

    /// <summary>Crea o actualiza una traducción y devuelve su identificador.</summary>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="change">Cultura, catálogo, clave y traducción que se conservarán.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador de la traducción de catálogo guardada.</returns>
    Task<long> SaveTranslationAsync(
        string companyCode,
        long userId,
        long sessionId,
        CatalogTranslationChange change,
        CancellationToken cancellationToken
    );
}
