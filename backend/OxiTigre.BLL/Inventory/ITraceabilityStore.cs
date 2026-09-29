/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Inventory.ITraceabilityStore
Archivo: ITraceabilityStore.cs | Versión: 1.0.0 | Fecha: 2026-08-24 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Define persistencia de mediciones, genealogía, incidentes, mantenimiento y préstamos.
Historial: 1.0.0 | 2026-08-24 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
namespace OxiTigre.BLL.Inventory;

/// <summary>Contrato de almacenamiento de trazabilidad industrial.</summary>
public interface ITraceabilityStore
{
    /// <summary>Recupera el estado actual y su historial.</summary>
    /// <param name="companyId">Identificador interno de la empresa.</param>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Lotes, activos, mediciones, transformaciones, incidencias, mantenimientos y préstamos de la empresa.</returns>
    Task<TraceabilitySnapshot> GetAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        CancellationToken cancellationToken
    );

    /// <summary>Registra un activo propiedad de un cliente y su ubicación inicial.</summary>
    /// <param name="companyId">Identificador interno de la empresa.</param>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="change">Identificación, propietario, tipo y estado del activo del cliente.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador del tubo o activo del cliente registrado.</returns>
    Task<long> SaveClientAssetAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        ClientAssetChange change,
        CancellationToken cancellationToken
    );

    /// <summary>Confirma el ingreso o la entrega de un activo propiedad de un cliente.</summary>
    /// <param name="companyId">Identificador interno de la empresa.</param>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="change">Activo, acción de recibir o entregar, depósito aplicable, observación y versión leída.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador del activo cuya custodia se actualizó.</returns>
    Task<long> ChangeClientAssetCustodyAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        ClientAssetCustodyChange change,
        CancellationToken cancellationToken
    );

    /// <summary>Registra una medición.</summary>
    /// <param name="companyId">Identificador interno de la empresa.</param>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="change">Activo, fecha, valor, unidad, método y origen de la medición que se registrará.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador de la medición registrada.</returns>
    Task<long> CreateMeasurementAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        AssetMeasurementChange change,
        CancellationToken cancellationToken
    );

    /// <summary>Confirma un fraccionamiento.</summary>
    /// <param name="companyId">Identificador interno de la empresa.</param>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="change">Origen, cantidad consumida, merma y destinos del fraccionamiento.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador de la transformación o fraccionamiento confirmado.</returns>
    Task<long> CreateTransformationAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        TransformationChange change,
        CancellationToken cancellationToken
    );

    /// <summary>Confirma un incidente.</summary>
    /// <param name="companyId">Identificador interno de la empresa.</param>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="change">Activo o lote afectado, tipo, fecha, motivo y observación de la incidencia.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador de la incidencia registrada.</returns>
    Task<long> CreateIncidentAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        IncidentChange change,
        CancellationToken cancellationToken
    );

    /// <summary>Inicia un mantenimiento.</summary>
    /// <param name="companyId">Identificador interno de la empresa.</param>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="change">Activo, tipo, fechas y observación del mantenimiento.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador del mantenimiento registrado.</returns>
    Task<long> CreateMaintenanceAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        MaintenanceChange change,
        CancellationToken cancellationToken
    );

    /// <summary>Completa un mantenimiento.</summary>
    /// <param name="companyId">Identificador interno de la empresa.</param>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="change">Fecha de cierre, resultado, componentes reemplazados, costo y versión del mantenimiento.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    Task CompleteMaintenanceAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        MaintenanceCompletion change,
        CancellationToken cancellationToken
    );

    /// <summary>Confirma un préstamo.</summary>
    /// <param name="companyId">Identificador interno de la empresa.</param>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="change">Destino, entrega, devolución prevista y activos incluidos en el préstamo.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador del préstamo registrado.</returns>
    Task<long> CreateLoanAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        LoanChange change,
        CancellationToken cancellationToken
    );

    /// <summary>Confirma una devolución.</summary>
    /// <param name="companyId">Identificador interno de la empresa.</param>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="change">Préstamo, fecha, activos devueltos y observación de la devolución.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    Task ReturnLoanAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        LoanReturnChange change,
        CancellationToken cancellationToken
    );
}
