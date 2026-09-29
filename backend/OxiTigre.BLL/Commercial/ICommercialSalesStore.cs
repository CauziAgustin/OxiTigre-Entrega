/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Commercial.ICommercialSalesStore
Archivo: ICommercialSalesStore.cs | Versión: 1.1.0 | Fecha: 2026-08-21 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Define persistencia autorizada para listas, promociones, pedidos y ventas internas.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-21 | FABRICA | Agustin Omar Cauzi | Persistencia de promociones configurables.
===============================================================================
*/
namespace OxiTigre.BLL.Commercial;

/// <summary>Contrato SQL del flujo comercial valorizado.</summary>
public interface ICommercialSalesStore
{
    /// <summary>Recupera el flujo comercial aislado por empresa.</summary>
    /// <param name="companyId">Identificador interno de la empresa.</param>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Clientes, listas de precios, promociones, pedidos y ventas de la empresa.</returns>
    Task<SalesSnapshot> GetAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        CancellationToken cancellationToken
    );

    /// <summary>Guarda una lista.</summary>
    /// <param name="companyId">Identificador interno de la empresa.</param>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="change">Nombre, vigencia, moneda y estado de la lista de precios.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador de la lista de precios creada o actualizada.</returns>
    Task<long> SavePriceListAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        PriceListChange change,
        CancellationToken cancellationToken
    );

    /// <summary>Guarda un precio.</summary>
    /// <param name="companyId">Identificador interno de la empresa.</param>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="change">Producto, lista y precio que se guardará.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador del precio de producto guardado en la lista.</returns>
    Task<long> SavePriceAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        PriceListProductChange change,
        CancellationToken cancellationToken
    );

    /// <summary>Guarda una promoción.</summary>
    /// <param name="companyId">Identificador interno de la empresa.</param>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="change">Vigencia, condición y beneficio de la promoción.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador de la promoción creada o actualizada.</returns>
    Task<long> SavePromotionAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        PromotionChange change,
        CancellationToken cancellationToken
    );

    /// <summary>Guarda un pedido borrador.</summary>
    /// <param name="companyId">Identificador interno de la empresa.</param>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="change">Cliente, cabecera y renglones valorizados del pedido.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador del pedido creado o actualizado.</returns>
    Task<long> SaveOrderAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        OrderChange change,
        CancellationToken cancellationToken
    );

    /// <summary>Confirma y reserva.</summary>
    /// <param name="companyId">Identificador interno de la empresa.</param>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="orderId">Identificador del pedido que se consulta o modifica.</param>
    /// <param name="rowVersion">Versión binaria usada para detectar modificaciones concurrentes.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    Task ConfirmOrderAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        long orderId,
        byte[] rowVersion,
        CancellationToken cancellationToken
    );

    /// <summary>Cancela y libera.</summary>
    /// <param name="companyId">Identificador interno de la empresa.</param>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="orderId">Identificador del pedido que se consulta o modifica.</param>
    /// <param name="rowVersion">Versión binaria usada para detectar modificaciones concurrentes.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    Task CancelOrderAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        long orderId,
        byte[] rowVersion,
        CancellationToken cancellationToken
    );

    /// <summary>Genera una venta interna.</summary>
    /// <param name="companyId">Identificador interno de la empresa.</param>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="orderId">Identificador del pedido que se consulta o modifica.</param>
    /// <param name="saleDateUtc">Fecha UTC que quedará registrada en la venta generada.</param>
    /// <param name="rowVersion">Versión binaria usada para detectar modificaciones concurrentes.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador de la venta generada desde el pedido.</returns>
    Task<long> CreateSaleAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        long orderId,
        DateTime saleDateUtc,
        byte[] rowVersion,
        CancellationToken cancellationToken
    );
}
