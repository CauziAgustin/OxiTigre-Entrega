/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.DAL.StoredProcedures.IStoredProcedureAuditWriter
Archivo: IStoredProcedureAuditWriter.cs | Versión: 1.0.0 | Fecha: 2026-08-19 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Define el punto único de persistencia de auditoría de SP.
Historial: 1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
namespace OxiTigre.DAL.StoredProcedures;

/// <summary>Escribe una ejecución sin auditar recursivamente al propio escritor.</summary>
public interface IStoredProcedureAuditWriter
{
    /// <summary>Registra una entrada central con parámetros previamente redactados.</summary>
    /// <param name="entry">Ejecución, usuario, correlación y parámetros redactados que se conservarán para auditoría.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la entrada de auditoría queda persistida.</returns>
    Task WriteAsync(StoredProcedureAuditEntry entry, CancellationToken cancellationToken);
}
