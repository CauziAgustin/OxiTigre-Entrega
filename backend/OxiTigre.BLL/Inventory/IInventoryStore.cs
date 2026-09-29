/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Inventory.IInventoryStore
Archivo: IInventoryStore.cs | Versión: 1.0.0 | Fecha: 2026-08-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Define la persistencia autorizada del módulo Inventario.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
namespace OxiTigre.BLL.Inventory;

/// <summary>Contrato de almacenamiento de maestros, saldos y movimientos.</summary>
public interface IInventoryStore
{
    /// <summary>Recupera el inventario aislado por empresa.</summary>
    /// <param name="companyId">Identificador interno de la empresa.</param>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Unidades, categorías, productos, depósitos, ubicaciones, existencias y movimientos de la empresa.</returns>
    Task<InventorySnapshot> GetAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        CancellationToken cancellationToken
    );

    /// <summary>Crea o edita una unidad y devuelve su identificador.</summary>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="change">Código, descripción y estado de la unidad de medida.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador de la unidad de medida creada o actualizada.</returns>
    Task<long> SaveMeasurementUnitAsync(
        string companyCode,
        long userId,
        long sessionId,
        MeasurementUnitChange change,
        CancellationToken cancellationToken
    );

    /// <summary>Crea o edita una categoría y devuelve su identificador.</summary>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="change">Código, nombre y estado de la categoría de productos.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador de la categoría creada o actualizada.</returns>
    Task<long> SaveCategoryAsync(
        string companyCode,
        long userId,
        long sessionId,
        ProductCategoryChange change,
        CancellationToken cancellationToken
    );

    /// <summary>Crea o edita un producto y devuelve su identificador.</summary>
    /// <param name="companyId">Identificador interno de la empresa.</param>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="change">Código, categoría, unidad y datos comerciales del producto.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador del producto creado o actualizado.</returns>
    Task<long> SaveProductAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        ProductChange change,
        CancellationToken cancellationToken
    );

    /// <summary>Crea o edita un depósito y devuelve su identificador.</summary>
    /// <param name="companyId">Identificador interno de la empresa.</param>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="change">Código, nombre, domicilio y estado del depósito.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador del depósito creado o actualizado.</returns>
    Task<long> SaveWarehouseAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        WarehouseChange change,
        CancellationToken cancellationToken
    );

    /// <summary>Crea o edita una ubicación y devuelve su identificador.</summary>
    /// <param name="companyId">Identificador interno de la empresa.</param>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="change">Depósito, código, nombre y estado de la ubicación.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador de la ubicación creada o actualizada.</returns>
    Task<long> SaveLocationAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        WarehouseLocationChange change,
        CancellationToken cancellationToken
    );

    /// <summary>Actualiza el mínimo de alerta.</summary>
    /// <param name="companyId">Identificador interno de la empresa.</param>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="change">Producto, depósito y cantidad mínima que activa una alerta de reposición.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    Task UpdateMinimumStockAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        MinimumStockChange change,
        CancellationToken cancellationToken
    );

    /// <summary>Confirma el movimiento y devuelve su identificador.</summary>
    /// <param name="companyId">Identificador interno de la empresa.</param>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="sessionId">Identificador de la sesión autenticada y auditable.</param>
    /// <param name="change">Producto, depósito, cantidad, motivo y referencias del movimiento.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador del movimiento de inventario confirmado.</returns>
    Task<long> CreateMovementAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        InventoryMovementChange change,
        CancellationToken cancellationToken
    );
}
