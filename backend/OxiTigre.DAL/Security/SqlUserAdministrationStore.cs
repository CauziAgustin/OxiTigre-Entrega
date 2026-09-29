/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.DAL.Security.SqlUserAdministrationStore
Archivo: SqlUserAdministrationStore.cs | Versión: 2.0.0 | Fecha: 2026-08-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Administra usuarios mediante SP y registra cada funcionalidad sin exponer credenciales.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Auditoría, formato y salidas homogéneas.
Historial: 2.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Edición, múltiples roles, reset y revocación.
===============================================================================
*/
using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using OxiTigre.BLL.Security;
using OxiTigre.DAL.MultiCompany;
using OxiTigre.DAL.StoredProcedures;

namespace OxiTigre.DAL.Security;

/// <summary>Implementa la administración de usuarios sobre SQL Server mediante procedimientos almacenados.</summary>
public sealed class SqlUserAdministrationStore(CompanyDatabaseRegistry databases, IStoredProcedureAuditWriter auditWriter) : IUserAdministrationStore
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<UserSummary>> ListAsync(long companyId, string companyCode, long sessionId,
        long userId, CancellationToken cancellationToken)
    {
        var startedAtUtc = DateTimeOffset.UtcNow;
        await using var connection = new SqlConnection(databases.GetConnectionString(companyCode));
        await using var command = new SqlCommand("[SEGURIDAD].[SP_USUARIO_LIST]", connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.Add("@I_ID_EMPRESA", SqlDbType.BigInt).Value = companyId;
        AddOutputs(command);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var users = new List<UserSummary>();
        while (await reader.ReadAsync(cancellationToken))
        {
            users.Add(new UserSummary(reader.GetInt64(reader.GetOrdinal("ID_USUARIO")),
                reader.GetString(reader.GetOrdinal("NOMBRE_USUARIO")), reader.GetString(reader.GetOrdinal("NOMBRES")),
                reader.GetString(reader.GetOrdinal("APELLIDO")), reader.GetString(reader.GetOrdinal("EMAIL")),
                reader.GetString(reader.GetOrdinal("CODIGO_ESTADO")),
                reader.GetString(reader.GetOrdinal("ROLES")).Split(',', StringSplitOptions.RemoveEmptyEntries)));
        }

        await AuditAsync(companyCode, userId, sessionId, "CONSULTA_USUARIOS", "CONSULTAR", "SP_USUARIO_LIST",
            "{}", startedAtUtc, users.Count, cancellationToken);
        return users;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RoleSummary>> ListRolesAsync(long companyId, string companyCode, long sessionId,
        long userId, CancellationToken cancellationToken)
    {
        var startedAtUtc = DateTimeOffset.UtcNow;
        await using var connection = new SqlConnection(databases.GetConnectionString(companyCode));
        await using var command = Command(connection, "[SEGURIDAD].[SP_ROL_LIST]");
        command.Parameters.Add("@I_ID_EMPRESA", SqlDbType.BigInt).Value = companyId;
        AddOutputs(command);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var roles = new List<RoleSummary>();
        while (await reader.ReadAsync(cancellationToken))
            roles.Add(new RoleSummary(reader.GetString(reader.GetOrdinal("CODIGO")), reader.GetString(reader.GetOrdinal("NOMBRE"))));
        await AuditAsync(companyCode, userId, sessionId, "CONSULTA_ROLES", "CONSULTAR", "SP_ROL_LIST", "{}",
            startedAtUtc, roles.Count, cancellationToken);
        return roles;
    }

    /// <inheritdoc />
    public async Task<long> CreateAsync(long companyId, string companyCode, string username, string givenNames,
        string surname, string email, IReadOnlyList<string> roleCodes, PasswordHash passwordHash, long administratorId,
        long sessionId, CancellationToken cancellationToken)
    {
        var startedAtUtc = DateTimeOffset.UtcNow;
        await using var connection = new SqlConnection(databases.GetConnectionString(companyCode));
        await using var command = new SqlCommand("[SEGURIDAD].[SP_USUARIO_CREATE]", connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.Add("@I_ID_EMPRESA", SqlDbType.BigInt).Value = companyId;
        command.Parameters.Add("@I_NOMBRE_USUARIO", SqlDbType.NVarChar, 100).Value = username;
        command.Parameters.Add("@I_NOMBRES", SqlDbType.NVarChar, 150).Value = givenNames;
        command.Parameters.Add("@I_APELLIDO", SqlDbType.NVarChar, 150).Value = surname;
        command.Parameters.Add("@I_EMAIL", SqlDbType.NVarChar, 254).Value = email;
        command.Parameters.Add("@I_ROLES_JSON", SqlDbType.NVarChar, -1).Value = JsonSerializer.Serialize(roleCodes);
        command.Parameters.Add("@I_HASH_CLAVE", SqlDbType.VarBinary, 64).Value = passwordHash.Hash;
        command.Parameters.Add("@I_SALT_CLAVE", SqlDbType.VarBinary, 32).Value = passwordHash.Salt;
        command.Parameters.Add("@I_ALGORITMO_CLAVE", SqlDbType.NVarChar, 30).Value = passwordHash.Algorithm;
        command.Parameters.Add("@I_ITERACIONES_CLAVE", SqlDbType.Int).Value = passwordHash.Iterations;
        command.Parameters.Add("@S_ID_USUARIO", SqlDbType.BigInt).Value = administratorId;
        var id = command.Parameters.Add("@O_ID_USUARIO", SqlDbType.BigInt); id.Direction = ParameterDirection.Output;
        AddOutputs(command, includeRows: true);
        await connection.OpenAsync(cancellationToken);
        await command.ExecuteNonQueryAsync(cancellationToken);
        var createdId = Convert.ToInt64(id.Value, System.Globalization.CultureInfo.InvariantCulture);
        await AuditAsync(companyCode, administratorId, sessionId, "ALTA_USUARIO", "CREAR", "SP_USUARIO_CREATE",
            JsonSerializer.Serialize(new { username, roleCodes }), startedAtUtc, 1 + roleCodes.Count, cancellationToken);
        return createdId;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(long companyId, string companyCode, long targetUserId, string givenNames,
        string surname, string email, string statusCode, IReadOnlyList<string> roleCodes, long administratorId,
        long sessionId, CancellationToken cancellationToken)
    {
        var startedAtUtc = DateTimeOffset.UtcNow;
        await using var connection = new SqlConnection(databases.GetConnectionString(companyCode));
        await using var command = Command(connection, "[SEGURIDAD].[SP_USUARIO_UPDATE]");
        command.Parameters.Add("@I_ID_EMPRESA", SqlDbType.BigInt).Value = companyId;
        command.Parameters.Add("@I_ID_USUARIO", SqlDbType.BigInt).Value = targetUserId;
        command.Parameters.Add("@I_NOMBRES", SqlDbType.NVarChar, 150).Value = givenNames;
        command.Parameters.Add("@I_APELLIDO", SqlDbType.NVarChar, 150).Value = surname;
        command.Parameters.Add("@I_EMAIL", SqlDbType.NVarChar, 254).Value = email;
        command.Parameters.Add("@I_CODIGO_ESTADO", SqlDbType.NVarChar, 30).Value = statusCode;
        command.Parameters.Add("@I_ROLES_JSON", SqlDbType.NVarChar, -1).Value = JsonSerializer.Serialize(roleCodes);
        command.Parameters.Add("@S_ID_USUARIO", SqlDbType.BigInt).Value = administratorId;
        AddOutputs(command, includeRows: true);
        await connection.OpenAsync(cancellationToken);
        await command.ExecuteNonQueryAsync(cancellationToken);
        await AuditAsync(companyCode, administratorId, sessionId, "MODIFICACION_USUARIO", "MODIFICAR",
            "SP_USUARIO_UPDATE", JsonSerializer.Serialize(new { targetUserId, statusCode, roleCodes }),
            startedAtUtc, 1 + roleCodes.Count, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ResetPasswordAsync(long companyId, string companyCode, long targetUserId,
        PasswordHash passwordHash, long administratorId, long sessionId, CancellationToken cancellationToken)
    {
        var startedAtUtc = DateTimeOffset.UtcNow;
        await using var connection = new SqlConnection(databases.GetConnectionString(companyCode));
        await using var command = Command(connection, "[SEGURIDAD].[SP_USUARIO_PASSWORD_RESET]");
        AddTargetAndCredential(command, companyId, targetUserId, passwordHash, administratorId);
        AddOutputs(command, includeRows: true);
        await connection.OpenAsync(cancellationToken);
        await command.ExecuteNonQueryAsync(cancellationToken);
        await AuditAsync(companyCode, administratorId, sessionId, "REINICIO_CONTRASENA", "MODIFICAR",
            "SP_USUARIO_PASSWORD_RESET", JsonSerializer.Serialize(new { targetUserId, credentialChanged = true }),
            startedAtUtc, 1, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RevokeSessionsAsync(long companyId, string companyCode, long targetUserId,
        long administratorId, long sessionId, CancellationToken cancellationToken)
    {
        var startedAtUtc = DateTimeOffset.UtcNow;
        await using var connection = new SqlConnection(databases.GetConnectionString(companyCode));
        await using var command = Command(connection, "[SEGURIDAD].[SP_USUARIO_SESIONES_REVOKE]");
        command.Parameters.Add("@I_ID_EMPRESA", SqlDbType.BigInt).Value = companyId;
        command.Parameters.Add("@I_ID_USUARIO", SqlDbType.BigInt).Value = targetUserId;
        command.Parameters.Add("@S_ID_USUARIO", SqlDbType.BigInt).Value = administratorId;
        AddOutputs(command, includeRows: true);
        await connection.OpenAsync(cancellationToken);
        await command.ExecuteNonQueryAsync(cancellationToken);
        await AuditAsync(companyCode, administratorId, sessionId, "REVOCACION_SESIONES", "MODIFICAR",
            "SP_USUARIO_SESIONES_REVOKE", JsonSerializer.Serialize(new { targetUserId }), startedAtUtc,
            Convert.ToInt32(command.Parameters["@O_FILAS_AFECTADAS"].Value), cancellationToken);
    }

    /// <summary>Crea un comando para ejecutar el procedimiento de administración indicado.</summary>
    /// <param name="connection">Conexión SQL abierta usada para crear el comando.</param>
    /// <param name="name">Nombre técnico del elemento solicitado.</param>
    /// <returns>Comando SQL configurado para el procedimiento solicitado.</returns>
    private static SqlCommand Command(SqlConnection connection, string name) => new(name, connection)
    {
        CommandType = CommandType.StoredProcedure
    };

    /// <summary>Agrega al comando el usuario objetivo, la credencial temporal y el administrador responsable.</summary>
    /// <param name="command">Comando SQL que recibe el parámetro o se ejecuta.</param>
    /// <param name="companyId">Identificador interno de la empresa.</param>
    /// <param name="targetUserId">Identificador del usuario afectado por el cambio.</param>
    /// <param name="passwordHash">Hash y sal usados para la contraseña temporal.</param>
    /// <param name="administratorId">Identificador del administrador que autoriza el cambio.</param>
    private static void AddTargetAndCredential(SqlCommand command, long companyId, long targetUserId,
        PasswordHash passwordHash, long administratorId)
    {
        command.Parameters.Add("@I_ID_EMPRESA", SqlDbType.BigInt).Value = companyId;
        command.Parameters.Add("@I_ID_USUARIO", SqlDbType.BigInt).Value = targetUserId;
        command.Parameters.Add("@I_HASH_CLAVE", SqlDbType.VarBinary, 64).Value = passwordHash.Hash;
        command.Parameters.Add("@I_SALT_CLAVE", SqlDbType.VarBinary, 32).Value = passwordHash.Salt;
        command.Parameters.Add("@I_ALGORITMO_CLAVE", SqlDbType.NVarChar, 30).Value = passwordHash.Algorithm;
        command.Parameters.Add("@I_ITERACIONES_CLAVE", SqlDbType.Int).Value = passwordHash.Iterations;
        command.Parameters.Add("@S_ID_USUARIO", SqlDbType.BigInt).Value = administratorId;
    }

    /// <summary>Registra la ejecución SQL de administración de usuarios con su contexto de seguridad.</summary>
    /// <param name="companyCode">Código de la empresa cuya base de datos se utiliza.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="sessionId">Identificador de la sesión responsable de la operación.</param>
    /// <param name="functionality">Funcionalidad registrada en la auditoría.</param>
    /// <param name="action">Código de la acción que se conserva en la auditoría.</param>
    /// <param name="storedProcedure">Nombre del procedimiento almacenado ejecutado.</param>
    /// <param name="parameters">Representación segura de parámetros conservada en la auditoría.</param>
    /// <param name="startedAtUtc">Instante UTC en que comenzó la ejecución auditada.</param>
    /// <param name="rows">Cantidad de filas afectadas informada por el procedimiento.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private Task AuditAsync(string companyCode, long userId, long sessionId, string functionality, string action,
        string storedProcedure, string parameters, DateTimeOffset startedAtUtc, int rows, CancellationToken cancellationToken) =>
        auditWriter.WriteAsync(new StoredProcedureAuditEntry(companyCode, "SEGURIDAD", userId, sessionId,
            functionality, action, "SEGURIDAD", storedProcedure, parameters, startedAtUtc, DateTimeOffset.UtcNow,
            rows, "EXITOSO", null, null, null, "OxiTigre.Api", Guid.NewGuid()), cancellationToken);

    /// <summary>Agrega los parámetros de salida comunes de administración de usuarios.</summary>
    /// <param name="command">Comando SQL que recibe el parámetro o se ejecuta.</param>
    /// <param name="includeRows">Indica si también debe agregarse el contador de filas afectadas.</param>
    private static void AddOutputs(SqlCommand command, bool includeRows = false)
    {
        var error = command.Parameters.Add("@O_CODIGO_ERROR", SqlDbType.BigInt); error.Direction = ParameterDirection.Output;
        var message = command.Parameters.Add("@O_MENSAJE", SqlDbType.NVarChar, 4000); message.Direction = ParameterDirection.Output;
        if (includeRows) { var rows = command.Parameters.Add("@O_FILAS_AFECTADAS", SqlDbType.Int); rows.Direction = ParameterDirection.Output; }
    }
}
