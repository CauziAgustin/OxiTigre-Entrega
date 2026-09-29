/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Purchasing.IPurchasingStore
Archivo: IPurchasingStore.cs | Versión: 1.1.0 | Fecha: 2026-08-26 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Define la persistencia autorizada de proveedores, órdenes y recepciones.
Historial: 1.0.0 | 2026-08-24 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-26 | FABRICA | Agustin Omar Cauzi | Reversión compensatoria y documentación XML completa.
===============================================================================
*/
namespace OxiTigre.BLL.Purchasing;

/// <summary>Contrato SQL del circuito de Compras.</summary>
public interface IPurchasingStore
{
    /// <summary>Recupera Compras aislado por empresa.</summary>
    /// <param name="companyId">Empresa propietaria de los datos.</param>
    /// <param name="companyCode">Código de empresa utilizado para resolver su base operativa.</param>
    /// <param name="userId">Usuario responsable de la consulta.</param>
    /// <param name="sessionId">Sesión funcional que se registrará en la auditoría.</param>
    /// <param name="cancellationToken">Token que permite cancelar la consulta SQL.</param>
    /// <returns>Estado operativo actual del circuito de Compras.</returns>
    Task<PurchasingSnapshot> GetAsync(long companyId, string companyCode, long userId, long sessionId,
        CancellationToken cancellationToken);
    /// <summary>Crea o actualiza un proveedor.</summary>
    /// <param name="companyId">Empresa propietaria del proveedor.</param>
    /// <param name="companyCode">Código de empresa utilizado para resolver su base operativa.</param>
    /// <param name="userId">Usuario responsable del cambio.</param>
    /// <param name="sessionId">Sesión funcional que se registrará en la auditoría.</param>
    /// <param name="change">Datos normalizados que se persistirán.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación SQL.</param>
    /// <returns>Identificador del proveedor creado o actualizado.</returns>
    Task<long> SaveSupplierAsync(long companyId, string companyCode, long userId, long sessionId,
        SupplierChange change, CancellationToken cancellationToken);
    /// <summary>Crea o actualiza una orden en borrador.</summary>
    /// <param name="companyId">Empresa propietaria de la orden.</param>
    /// <param name="companyCode">Código de empresa utilizado para resolver su base operativa.</param>
    /// <param name="userId">Usuario responsable del cambio.</param>
    /// <param name="sessionId">Sesión funcional que se registrará en la auditoría.</param>
    /// <param name="change">Cabecera y renglones normalizados que se persistirán.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación SQL.</param>
    /// <returns>Identificador de la orden creada o actualizada.</returns>
    Task<long> SaveOrderAsync(long companyId, string companyCode, long userId, long sessionId,
        PurchaseOrderChange change, CancellationToken cancellationToken);
    /// <summary>Envía la orden a aprobación.</summary>
    /// <param name="companyId">Empresa propietaria de la orden.</param>
    /// <param name="companyCode">Código de empresa utilizado para resolver su base operativa.</param>
    /// <param name="userId">Usuario responsable de la transición.</param>
    /// <param name="sessionId">Sesión funcional que se registrará en la auditoría.</param>
    /// <param name="purchaseOrderId">Identificador de la orden.</param>
    /// <param name="rowVersion">Versión binaria utilizada para controlar concurrencia.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación SQL.</param>
    /// <returns>Tarea que finaliza cuando la transición queda confirmada.</returns>
    Task SubmitOrderAsync(long companyId, string companyCode, long userId, long sessionId,
        long purchaseOrderId, byte[] rowVersion, CancellationToken cancellationToken);
    /// <summary>Aprueba una orden pendiente.</summary>
    /// <param name="companyId">Empresa propietaria de la orden.</param>
    /// <param name="companyCode">Código de empresa utilizado para resolver su base operativa.</param>
    /// <param name="userId">Usuario responsable de la aprobación.</param>
    /// <param name="sessionId">Sesión funcional que se registrará en la auditoría.</param>
    /// <param name="purchaseOrderId">Identificador de la orden.</param>
    /// <param name="rowVersion">Versión binaria utilizada para controlar concurrencia.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación SQL.</param>
    /// <returns>Tarea que finaliza cuando la aprobación queda confirmada.</returns>
    Task ApproveOrderAsync(long companyId, string companyCode, long userId, long sessionId,
        long purchaseOrderId, byte[] rowVersion, CancellationToken cancellationToken);
    /// <summary>Cancela una orden que todavía admite la transición.</summary>
    /// <param name="companyId">Empresa propietaria de la orden.</param>
    /// <param name="companyCode">Código de empresa utilizado para resolver su base operativa.</param>
    /// <param name="userId">Usuario responsable de la cancelación.</param>
    /// <param name="sessionId">Sesión funcional que se registrará en la auditoría.</param>
    /// <param name="purchaseOrderId">Identificador de la orden.</param>
    /// <param name="rowVersion">Versión binaria utilizada para controlar concurrencia.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación SQL.</param>
    /// <returns>Tarea que finaliza cuando la cancelación queda confirmada.</returns>
    Task CancelOrderAsync(long companyId, string companyCode, long userId, long sessionId,
        long purchaseOrderId, byte[] rowVersion, CancellationToken cancellationToken);
    /// <summary>Cierra un saldo pendiente con motivo obligatorio.</summary>
    /// <param name="companyId">Empresa propietaria de la orden.</param>
    /// <param name="companyCode">Código de empresa utilizado para resolver su base operativa.</param>
    /// <param name="userId">Usuario responsable del cierre.</param>
    /// <param name="sessionId">Sesión funcional que se registrará en la auditoría.</param>
    /// <param name="purchaseOrderId">Identificador de la orden.</param>
    /// <param name="reason">Motivo normalizado del cierre.</param>
    /// <param name="rowVersion">Versión binaria utilizada para controlar concurrencia.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación SQL.</param>
    /// <returns>Tarea que finaliza cuando el cierre queda confirmado.</returns>
    Task CloseOrderAsync(long companyId, string companyCode, long userId, long sessionId,
        long purchaseOrderId, string reason, byte[] rowVersion, CancellationToken cancellationToken);
    /// <summary>Confirma una recepción y devuelve su identificador.</summary>
    /// <param name="companyId">Empresa propietaria de la recepción.</param>
    /// <param name="companyCode">Código de empresa utilizado para resolver su base operativa.</param>
    /// <param name="userId">Usuario responsable de la recepción.</param>
    /// <param name="sessionId">Sesión funcional que se registrará en la auditoría.</param>
    /// <param name="change">Documento y trazabilidad normalizados que se persistirán.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación SQL.</param>
    /// <returns>Identificador de la recepción confirmada.</returns>
    Task<long> CreateReceiptAsync(long companyId, string companyCode, long userId, long sessionId,
        GoodsReceiptChange change, CancellationToken cancellationToken);
    /// <summary>Compensa una recepción intacta sin eliminar su historial.</summary>
    /// <param name="companyId">Empresa propietaria de la recepción.</param>
    /// <param name="companyCode">Código de empresa utilizado para resolver su base operativa.</param>
    /// <param name="userId">Usuario responsable de la reversión.</param>
    /// <param name="sessionId">Sesión funcional que se registrará en la auditoría.</param>
    /// <param name="goodsReceiptId">Identificador de la recepción confirmada.</param>
    /// <param name="reason">Motivo normalizado de la reversión.</param>
    /// <param name="rowVersion">Versión binaria utilizada para controlar concurrencia.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación SQL.</param>
    /// <returns>Tarea que finaliza cuando la reversión queda confirmada.</returns>
    Task ReverseReceiptAsync(long companyId, string companyCode, long userId, long sessionId,
        long goodsReceiptId, string reason, byte[] rowVersion, CancellationToken cancellationToken);
}
