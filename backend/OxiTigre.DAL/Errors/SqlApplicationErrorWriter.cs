/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.DAL.Errors.SqlApplicationErrorWriter
Archivo: SqlApplicationErrorWriter.cs | Versión: 1.0.0 | Fecha: 2026-08-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Persiste excepciones no controladas mediante el SP central de errores.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
using System.Data;
using Microsoft.Data.SqlClient;
using OxiTigre.DAL.MultiCompany;

namespace OxiTigre.DAL.Errors;

/// <summary>Registra una excepción técnica sin copiar cuerpos HTTP ni credenciales.</summary>
public sealed class SqlApplicationErrorWriter(CompanyDatabaseRegistry databases)
{
    /// <summary>Persiste una ocurrencia con el código interno general y su correlación.</summary>
    /// <param name="exception">Excepción técnica cuya información segura se registra.</param>
    /// <param name="component">Módulo o capa donde ocurrió el error técnico.</param>
    /// <param name="ipAddress">Dirección IP de origen registrada para la sesión.</param>
    /// <param name="correlationId">Identificador que correlaciona la operación entre capas y registros.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <param name="companyCode">Código estable de la empresa que selecciona su base de datos.</param>
    /// <returns>Tarea que finaliza cuando el error y su correlación quedan registrados.</returns>
    public async Task WriteAsync(
        Exception exception,
        string component,
        string? ipAddress,
        Guid correlationId,
        CancellationToken cancellationToken,
        string? companyCode = null
    )
    {
        var selectedCompany = companyCode ?? databases.Companies.First().Code;
        await using var connection = new SqlConnection(
            databases.GetConnectionString(selectedCompany)
        );
        await using var command = new SqlCommand(
            "[AUDITORIA].[SP_ERROR_OCURRENCIA_CREATE]",
            connection
        )
        {
            CommandType = CommandType.StoredProcedure,
        };
        command.Parameters.Add("@I_CODIGO_ERROR", SqlDbType.BigInt).Value = 70002;
        command.Parameters.Add("@I_COMPONENTE", SqlDbType.NVarChar, 200).Value = component;
        command.Parameters.Add("@I_MENSAJE", SqlDbType.NVarChar, 4000).Value = exception.Message;
        command.Parameters.Add("@I_DETALLE_TECNICO", SqlDbType.NVarChar, -1).Value =
            exception.InnerException?.Message ?? (object)DBNull.Value;
        command.Parameters.Add("@I_STACK_TRACE", SqlDbType.NVarChar, -1).Value =
            exception.StackTrace ?? (object)DBNull.Value;
        command.Parameters.Add("@S_IP_ORIGEN", SqlDbType.NVarChar, 45).Value =
            ipAddress ?? (object)DBNull.Value;
        command.Parameters.Add("@S_APLICACION_ORIGEN", SqlDbType.NVarChar, 100).Value =
            "OxiTigre.Api";
        command.Parameters.Add("@S_ID_CORRELACION", SqlDbType.UniqueIdentifier).Value =
            correlationId;
        AddOutput(command, "@O_ID_ERROR_APLICACION", SqlDbType.BigInt);
        AddOutput(command, "@O_CODIGO_ERROR", SqlDbType.BigInt);
        AddOutput(command, "@O_MENSAJE", SqlDbType.NVarChar, 4000);
        AddOutput(command, "@O_FILAS_AFECTADAS", SqlDbType.Int);
        await connection.OpenAsync(cancellationToken);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>Agrega un parámetro de salida al comando que registra errores.</summary>
    /// <param name="command">Comando SQL que recibe el parámetro o se ejecuta.</param>
    /// <param name="name">Nombre del parámetro SQL que se agrega.</param>
    /// <param name="type">Tipo SQL que debe aplicarse al parámetro.</param>
    /// <param name="size">Longitud máxima admitida por el parámetro.</param>
    private static void AddOutput(SqlCommand command, string name, SqlDbType type, int size = 0)
    {
        var parameter =
            size == 0
                ? command.Parameters.Add(name, type)
                : command.Parameters.Add(name, type, size);
        parameter.Direction = ParameterDirection.Output;
    }
}
