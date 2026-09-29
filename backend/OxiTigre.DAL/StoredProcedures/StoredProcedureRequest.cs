/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.DAL.StoredProcedures.StoredProcedureRequest
Archivo: StoredProcedureRequest.cs | Versión: 1.0.0 | Fecha: 2026-08-19 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Define una solicitud segura de ejecución de un Stored Procedure.
Historial: 1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
namespace OxiTigre.DAL.StoredProcedures;

/// <summary>
/// Contiene el nombre calificado y los parámetros de una ejecución de Stored Procedure.
/// </summary>
public sealed record StoredProcedureRequest
{
    /// <summary>Inicializa una solicitud validando schema y procedimiento.</summary>
    /// <param name="schema">Schema propietario del procedimiento.</param>
    /// <param name="procedure">Nombre del procedimiento sin schema.</param>
    /// <param name="parameters">Parámetros con sus nombres T-SQL y valores.</param>
    /// <exception cref="ArgumentException">El schema o procedimiento está vacío.</exception>
    public StoredProcedureRequest(string schema, string procedure, IReadOnlyDictionary<string, object?>? parameters = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);
        ArgumentException.ThrowIfNullOrWhiteSpace(procedure);

        Schema = schema;
        Procedure = procedure;
        Parameters = parameters ?? new Dictionary<string, object?>();
    }

    /// <summary>Obtiene el schema propietario.</summary>
    public string Schema { get; }

    /// <summary>Obtiene el nombre del procedimiento.</summary>
    public string Procedure { get; }

    /// <summary>Obtiene los parámetros de la ejecución.</summary>
    public IReadOnlyDictionary<string, object?> Parameters { get; }

    /// <summary>Obtiene el nombre protegido y calificado por schema.</summary>
    public string QualifiedName => $"[{Schema}].[{Procedure}]";
}
