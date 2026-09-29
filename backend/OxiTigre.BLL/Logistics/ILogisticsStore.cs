/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Logistics.ILogisticsStore
Archivo: ILogisticsStore.cs | Versión: 1.0.0 | Fecha: 2026-08-26 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Define la persistencia consolidada del circuito logístico.
Historial: 1.0.0 | 2026-08-26 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
namespace OxiTigre.BLL.Logistics;

/// <summary>Contrato SQL mínimo del circuito logístico.</summary>
public interface ILogisticsStore
{
    /// <summary>Obtiene el estado operativo e histórico de la empresa.</summary>
    /// <param name="companyId">Empresa propietaria.</param>
    /// <param name="companyCode">Código de la base operativa.</param>
    /// <param name="userId">Usuario responsable.</param>
    /// <param name="sessionId">Sesión auditada.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Snapshot completo de Logística.</returns>
    Task<LogisticsSnapshot> GetAsync(long companyId, string companyCode, long userId, long sessionId,
        CancellationToken cancellationToken);

    /// <summary>Ejecuta una acción validada mediante el único SP de comandos del módulo.</summary>
    /// <param name="companyId">Empresa propietaria.</param>
    /// <param name="companyCode">Código de la base operativa.</param>
    /// <param name="userId">Usuario responsable.</param>
    /// <param name="sessionId">Sesión auditada.</param>
    /// <param name="action">Acción estable soportada por el SP.</param>
    /// <param name="values">Valores normalizados.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Identificador afectado.</returns>
    Task<long> ExecuteAsync(long companyId, string companyCode, long userId, long sessionId, string action,
        IReadOnlyDictionary<string, object?> values, CancellationToken cancellationToken);
}
