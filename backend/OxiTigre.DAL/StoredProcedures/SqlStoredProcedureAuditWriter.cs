/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.DAL.StoredProcedures.SqlStoredProcedureAuditWriter
Archivo: SqlStoredProcedureAuditWriter.cs | Versión: 1.0.0 | Fecha: 2026-08-19 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Persiste trazas funcionales mediante el SP central de auditoría.
Historial: 1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
using System.Data;
using Microsoft.Data.SqlClient;
using OxiTigre.DAL.MultiCompany;

namespace OxiTigre.DAL.StoredProcedures;

/// <summary>Implementa el escritor central sobre SQL Server.</summary>
public sealed class SqlStoredProcedureAuditWriter(CompanyDatabaseRegistry databases) : IStoredProcedureAuditWriter
{
    /// <inheritdoc />
    public async Task WriteAsync(StoredProcedureAuditEntry entry, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(databases.GetConnectionString(entry.CompanyCode));
        await using var command = new SqlCommand("[AUDITORIA].[SP_LOGUEO_FUNCIONALIDAD_CREATE]", connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        Add(command, "@I_CODIGO_EMPRESA", SqlDbType.NVarChar, 30, entry.CompanyCode);
        Add(command, "@I_CODIGO_MODULO", SqlDbType.NVarChar, 30, entry.ModuleCode);
        Add(command, "@I_ID_USUARIO", SqlDbType.BigInt, 0, entry.UserId);
        Add(command, "@I_ID_SESION", SqlDbType.BigInt, 0, entry.SessionId);
        Add(command, "@I_FUNCIONALIDAD", SqlDbType.NVarChar, 200, entry.Functionality);
        Add(command, "@I_ACCION", SqlDbType.NVarChar, 100, entry.Action);
        Add(command, "@I_SCHEMA_SP", SqlDbType.NVarChar, 128, entry.Schema);
        Add(command, "@I_NOMBRE_SP", SqlDbType.NVarChar, 128, entry.Procedure);
        Add(command, "@I_PARAMETROS_REDACTADOS_JSON", SqlDbType.NVarChar, -1, entry.RedactedParametersJson);
        Add(command, "@I_FECHA_INICIO_UTC", SqlDbType.DateTime2, 0, entry.StartedAtUtc.UtcDateTime);
        Add(command, "@I_FECHA_FIN_UTC", SqlDbType.DateTime2, 0, entry.FinishedAtUtc.UtcDateTime);
        Add(command, "@I_FILAS_AFECTADAS", SqlDbType.Int, 0, entry.AffectedRows);
        Add(command, "@I_RESULTADO", SqlDbType.NVarChar, 30, entry.Result);
        Add(command, "@I_CODIGO_ERROR", SqlDbType.BigInt, 0, entry.ErrorCode);
        Add(command, "@I_MENSAJE_ERROR", SqlDbType.NVarChar, 4000, entry.ErrorMessage);
        Add(command, "@S_IP_ORIGEN", SqlDbType.NVarChar, 45, entry.IpAddress);
        Add(command, "@S_APLICACION_ORIGEN", SqlDbType.NVarChar, 100, entry.Application);
        Add(command, "@S_ID_CORRELACION", SqlDbType.UniqueIdentifier, 0, entry.CorrelationId);
        AddOutput(command, "@O_ID_LOGUEO_FUNCIONALIDAD", SqlDbType.BigInt, 0);
        AddOutput(command, "@O_CODIGO_ERROR", SqlDbType.BigInt, 0);
        AddOutput(command, "@O_MENSAJE", SqlDbType.NVarChar, 4000);

        await connection.OpenAsync(cancellationToken);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>Agrega un parámetro tipado al comando de auditoría.</summary>
    /// <param name="command">Comando SQL que recibe el parámetro o se ejecuta.</param>
    /// <param name="name">Nombre del parámetro SQL que se agrega.</param>
    /// <param name="type">Tipo SQL que debe aplicarse al parámetro.</param>
    /// <param name="size">Longitud máxima admitida por el parámetro.</param>
    /// <param name="value">Valor que se convierte o asigna al destino correspondiente.</param>
    private static void Add(SqlCommand command, string name, SqlDbType type, int size, object? value)
    {
        var parameter = size == 0 ? command.Parameters.Add(name, type) : command.Parameters.Add(name, type, size);
        parameter.Value = value ?? DBNull.Value;
    }

    /// <summary>Agrega un parámetro de salida al comando de auditoría.</summary>
    /// <param name="command">Comando SQL que recibe el parámetro o se ejecuta.</param>
    /// <param name="name">Nombre del parámetro SQL que se agrega.</param>
    /// <param name="type">Tipo SQL que debe aplicarse al parámetro.</param>
    /// <param name="size">Longitud máxima admitida por el parámetro.</param>
    private static void AddOutput(SqlCommand command, string name, SqlDbType type, int size)
    {
        var parameter = size == 0 ? command.Parameters.Add(name, type) : command.Parameters.Add(name, type, size);
        parameter.Direction = ParameterDirection.Output;
    }
}
