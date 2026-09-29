/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Commercial.IClientStore
Archivo: IClientStore.cs | Versión: 2.1.0 | Fecha: 2026-08-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Define la consulta persistente de clientes autorizados.
Historial: 1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 2.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Ciclo completo y contexto auditable de clientes.
Historial: 2.1.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Catálogos y borrador recuperable por usuario.
===============================================================================
*/
namespace OxiTigre.BLL.Commercial;

/// <summary>Abstrae la lectura del módulo Comercial.</summary>
public interface IClientStore
{
    /// <summary>Obtiene catálogos activos para la carga controlada.</summary>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tipos de documento, condiciones y demás catálogos usados para registrar clientes.</returns>
    Task<ClientCatalogs> GetCatalogsAsync(
        string companyCode,
        long sessionId,
        long userId,
        CancellationToken cancellationToken
    );

    /// <summary>Obtiene el borrador activo del usuario, si existe.</summary>
    /// <param name="companyId">Identificador interno de la empresa.</param>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Borrador de cliente del usuario, o null si no hay uno.</returns>
    Task<SavedClientDraft?> GetDraftAsync(
        long companyId,
        string companyCode,
        long sessionId,
        long userId,
        CancellationToken cancellationToken
    );

    /// <summary>Guarda o reemplaza el borrador activo del usuario.</summary>
    /// <param name="companyId">Identificador interno de la empresa.</param>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="draft">Identificación, contacto y teléfonos del cliente que se guardarán.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    Task SaveDraftAsync(
        long companyId,
        string companyCode,
        ClientDraft draft,
        long sessionId,
        long userId,
        CancellationToken cancellationToken
    );

    /// <summary>Descarta el borrador activo del usuario.</summary>
    /// <param name="companyId">Identificador interno de la empresa.</param>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    Task DeleteDraftAsync(
        long companyId,
        string companyCode,
        long sessionId,
        long userId,
        CancellationToken cancellationToken
    );

    /// <summary>Lista clientes por empresa, estado y sesión funcional.</summary>
    /// <param name="companyId">Identificador interno de la empresa.</param>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="statusCode">Código de estado admitido por el dominio.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Clientes filtrados por el estado solicitado.</returns>
    Task<IReadOnlyList<ClientSummary>> ListAsync(
        long companyId,
        string companyCode,
        string statusCode,
        long sessionId,
        long userId,
        CancellationToken cancellationToken
    );

    /// <summary>Obtiene un cliente aislado por empresa con sus teléfonos activos.</summary>
    /// <param name="companyId">Identificador interno de la empresa.</param>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="clientId">Identificador del cliente, verificado dentro de la empresa activa.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Datos completos del cliente indicado, o null si no existe.</returns>
    Task<ClientDetails?> GetAsync(
        long companyId,
        string companyCode,
        long clientId,
        long sessionId,
        long userId,
        CancellationToken cancellationToken
    );

    /// <summary>Crea un cliente y sus teléfonos en una transacción.</summary>
    /// <param name="companyId">Identificador interno de la empresa.</param>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="draft">Identificación, contacto y teléfonos del cliente que se guardarán.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador del cliente creado.</returns>
    Task<long> CreateAsync(
        long companyId,
        string companyCode,
        ClientDraft draft,
        long sessionId,
        long userId,
        CancellationToken cancellationToken
    );

    /// <summary>Actualiza datos generales y reemplaza la colección de teléfonos activos.</summary>
    /// <param name="companyId">Identificador interno de la empresa.</param>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="clientId">Identificador del cliente, verificado dentro de la empresa activa.</param>
    /// <param name="draft">Identificación, contacto y teléfonos del cliente que se guardarán.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    Task UpdateAsync(
        long companyId,
        string companyCode,
        long clientId,
        ClientDraft draft,
        long sessionId,
        long userId,
        CancellationToken cancellationToken
    );

    /// <summary>Modifica el estado lógico del cliente.</summary>
    /// <param name="companyId">Identificador interno de la empresa.</param>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="clientId">Identificador del cliente, verificado dentro de la empresa activa.</param>
    /// <param name="statusCode">Código de estado admitido por el dominio.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    Task ChangeStatusAsync(
        long companyId,
        string companyCode,
        long clientId,
        string statusCode,
        long sessionId,
        long userId,
        CancellationToken cancellationToken
    );
}
