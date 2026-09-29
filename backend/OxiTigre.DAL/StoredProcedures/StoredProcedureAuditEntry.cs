/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.DAL.StoredProcedures.StoredProcedureAuditEntry
Archivo: StoredProcedureAuditEntry.cs | Versión: 1.0.0 | Fecha: 2026-08-19 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Describe una ejecución de SP lista para auditoría central.
Historial: 1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
namespace OxiTigre.DAL.StoredProcedures;

/// <summary>Contiene contexto, tiempos y resultado sin valores sensibles.</summary>
public sealed record StoredProcedureAuditEntry(
    string CompanyCode,
    string ModuleCode,
    long? UserId,
    long? SessionId,
    string Functionality,
    string Action,
    string Schema,
    string Procedure,
    string? RedactedParametersJson,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset FinishedAtUtc,
    int? AffectedRows,
    string Result,
    long? ErrorCode,
    string? ErrorMessage,
    string? IpAddress,
    string Application,
    Guid CorrelationId);
