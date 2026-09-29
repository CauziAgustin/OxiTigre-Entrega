/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Finance.IFinanceStore
Archivo: IFinanceStore.cs | Versión: 11.0.0 | Fecha: 2026-08-27 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Define el acceso mínimo a persistencia financiera.
Historial: 11.0.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
namespace OxiTigre.BLL.Finance;

/// <summary>Contrato SQL mínimo para caja, cobros y cuenta corriente.</summary>
public interface IFinanceStore
{
    /// <summary>Obtiene el snapshot financiero aislado por empresa.</summary>
    /// <param name="companyId">Empresa autenticada.</param>
    /// <param name="companyCode">Código usado para resolver la base autorizada.</param>
    /// <param name="userId">Usuario autenticado.</param>
    /// <param name="sessionId">Sesión auditada.</param>
    /// <param name="cancellationToken">Cancelación de la solicitud.</param>
    /// <returns>Estado financiero visible para la sesión.</returns>
    Task<FinanceSnapshot> GetAsync(long companyId, string companyCode, long userId, long sessionId,
        CancellationToken cancellationToken);

    /// <summary>Ejecuta una acción validada mediante el SP consolidado.</summary>
    /// <param name="companyId">Empresa autenticada.</param>
    /// <param name="companyCode">Código usado para resolver la base autorizada.</param>
    /// <param name="userId">Usuario autenticado.</param>
    /// <param name="sessionId">Sesión auditada.</param>
    /// <param name="action">Acción permitida del procedimiento.</param>
    /// <param name="values">Parámetros funcionales de la acción.</param>
    /// <param name="cancellationToken">Cancelación de la solicitud.</param>
    /// <returns>Identificador creado o modificado.</returns>
    Task<long> ExecuteAsync(long companyId, string companyCode, long userId, long sessionId, string action,
        IReadOnlyDictionary<string, object?> values, CancellationToken cancellationToken);
}
