/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.DAL.Security.SqlAuthenticationStore
Archivo: SqlAuthenticationStore.cs | Versión: 9.0.0 | Fecha: 2026-08-27 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Persiste autenticación y sesiones exclusivamente mediante Stored Procedures.
Historial: 1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Consulta de empresas para el login posterior a credenciales.
Historial: 2.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Intentos fallidos y cierre auditable de sesión.
Historial: 9.0.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Resolución aislada de autenticación por empresa.
===============================================================================
*/
using System.Data;
using Microsoft.Data.SqlClient;
using OxiTigre.BLL.Security;
using OxiTigre.DAL.MultiCompany;
using OxiTigre.DAL.StoredProcedures;

namespace OxiTigre.DAL.Security;

/// <summary>Implementa el almacenamiento de autenticación sobre SQL Server.</summary>
public sealed class SqlAuthenticationStore(
    CompanyDatabaseRegistry databases,
    IStoredProcedureAuditWriter auditWriter
) : IAuthenticationStore
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<CompanyLoginCandidate>> FindCompanyCandidatesAsync(
        string username,
        CancellationToken cancellationToken
    )
    {
        var candidates = new List<CompanyLoginCandidate>();

        foreach (var database in databases.Companies)
        {
            try
            {
                await using var connection = new SqlConnection(database.ConnectionString);
                await using var command = new SqlCommand(
                    "[SEGURIDAD].[SP_USUARIO_EMPRESA_AUTH_LIST]",
                    connection
                )
                {
                    CommandType = CommandType.StoredProcedure,
                };
                command.Parameters.Add("@I_NOMBRE_USUARIO", SqlDbType.NVarChar, 100).Value =
                    username;
                AddOutputParameters(command);

                await connection.OpenAsync(cancellationToken);
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                {
                    continue;
                }

                var account = new AuthenticationAccount(
                    reader.GetInt64(reader.GetOrdinal("ID_USUARIO")),
                    reader.GetInt64(reader.GetOrdinal("ID_EMPRESA")),
                    reader.GetString(reader.GetOrdinal("NOMBRE_USUARIO")),
                    reader.GetString(reader.GetOrdinal("NOMBRES")),
                    reader.GetString(reader.GetOrdinal("APELLIDO")),
                    reader.GetString(reader.GetOrdinal("EMAIL")),
                    (byte[])reader["HASH_CLAVE"],
                    (byte[])reader["SALT_CLAVE"],
                    reader.GetString(reader.GetOrdinal("ALGORITMO_CLAVE")),
                    reader.GetInt32(reader.GetOrdinal("ITERACIONES_CLAVE")),
                    reader.GetBoolean(reader.GetOrdinal("DEBE_CAMBIAR_CLAVE")),
                    reader.GetInt16(reader.GetOrdinal("INTENTOS_FALLIDOS")),
                    GetNullableUtc(reader, "FECHA_BLOQUEO_UTC"),
                    [],
                    [],
                    []
                );
                account = account with
                {
                    Branches = await ReadBranchesAsync(reader, cancellationToken),
                };
                candidates.Add(new CompanyLoginCandidate(database.Code, database.Name, account));
            }
            catch (SqlException)
            {
                // ponytail: una base no disponible no debe bloquear el ingreso a las restantes.
            }
        }

        return candidates;
    }

    /// <inheritdoc />
    public async Task<AuthenticationAccount?> FindAccountAsync(
        string companyCode,
        string username,
        CancellationToken cancellationToken
    )
    {
        await using var connection = new SqlConnection(databases.GetConnectionString(companyCode));
        await using var command = new SqlCommand("[SEGURIDAD].[SP_USUARIO_AUTH_GET]", connection)
        {
            CommandType = CommandType.StoredProcedure,
        };
        command.Parameters.Add("@I_CODIGO_EMPRESA", SqlDbType.NVarChar, 30).Value = companyCode;
        command.Parameters.Add("@I_NOMBRE_USUARIO", SqlDbType.NVarChar, 100).Value = username;
        AddOutputParameters(command);

        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var userId = reader.GetInt64(reader.GetOrdinal("ID_USUARIO"));
        var companyId = reader.GetInt64(reader.GetOrdinal("ID_EMPRESA"));
        var storedUsername = reader.GetString(reader.GetOrdinal("NOMBRE_USUARIO"));
        var givenNames = reader.GetString(reader.GetOrdinal("NOMBRES"));
        var surname = reader.GetString(reader.GetOrdinal("APELLIDO"));
        var email = reader.GetString(reader.GetOrdinal("EMAIL"));
        var passwordHash = (byte[])reader["HASH_CLAVE"];
        var passwordSalt = (byte[])reader["SALT_CLAVE"];
        var algorithm = reader.GetString(reader.GetOrdinal("ALGORITMO_CLAVE"));
        var iterations = reader.GetInt32(reader.GetOrdinal("ITERACIONES_CLAVE"));
        var mustChangePassword = reader.GetBoolean(reader.GetOrdinal("DEBE_CAMBIAR_CLAVE"));
        var failedAttempts = reader.GetInt16(reader.GetOrdinal("INTENTOS_FALLIDOS"));
        var blockedAtUtc = GetNullableUtc(reader, "FECHA_BLOQUEO_UTC");

        var roles = new List<string>();
        if (await reader.NextResultAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                roles.Add(reader.GetString(0));
            }
        }

        var permissions = new List<string>();
        if (await reader.NextResultAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                permissions.Add(reader.GetString(0));
            }
        }

        var branches = await ReadBranchesAsync(reader, cancellationToken);

        return new AuthenticationAccount(
            userId,
            companyId,
            storedUsername,
            givenNames,
            surname,
            email,
            passwordHash,
            passwordSalt,
            algorithm,
            iterations,
            mustChangePassword,
            failedAttempts,
            blockedAtUtc,
            roles,
            permissions,
            branches
        );
    }

    /// <inheritdoc />
    public async Task<long> CreateSessionAsync(
        string companyCode,
        long userId,
        long? branchId,
        byte[] tokenHash,
        DateTimeOffset expiresAtUtc,
        string? ipAddress,
        string application,
        Guid correlationId,
        CancellationToken cancellationToken
    )
    {
        var startedAtUtc = DateTimeOffset.UtcNow;
        await using var connection = new SqlConnection(databases.GetConnectionString(companyCode));
        await using var command = new SqlCommand("[SEGURIDAD].[SP_SESION_CREATE]", connection)
        {
            CommandType = CommandType.StoredProcedure,
        };
        command.Parameters.Add("@I_ID_USUARIO", SqlDbType.BigInt).Value = userId;
        command.Parameters.Add("@I_ID_SUCURSAL", SqlDbType.BigInt).Value =
            (object?)branchId ?? DBNull.Value;
        command.Parameters.Add("@I_HASH_TOKEN", SqlDbType.VarBinary, 64).Value = tokenHash;
        command.Parameters.Add("@I_FECHA_EXPIRACION_UTC", SqlDbType.DateTime2).Value =
            expiresAtUtc.UtcDateTime;
        command.Parameters.Add("@S_IP_ORIGEN", SqlDbType.NVarChar, 45).Value =
            (object?)ipAddress ?? DBNull.Value;
        command.Parameters.Add("@S_APLICACION_ORIGEN", SqlDbType.NVarChar, 100).Value = application;
        command.Parameters.Add("@S_ID_CORRELACION", SqlDbType.UniqueIdentifier).Value =
            correlationId;
        var sessionId = command.Parameters.Add("@O_ID_SESION", SqlDbType.BigInt);
        sessionId.Direction = ParameterDirection.Output;
        AddOutputParameters(command, includeRows: true);

        await connection.OpenAsync(cancellationToken);
        await command.ExecuteNonQueryAsync(cancellationToken);
        var createdSessionId = Convert.ToInt64(
            sessionId.Value,
            System.Globalization.CultureInfo.InvariantCulture
        );
        await auditWriter.WriteAsync(
            new StoredProcedureAuditEntry(
                companyCode,
                "SEGURIDAD",
                userId,
                createdSessionId,
                "INICIO_SESION",
                "CREAR",
                "SEGURIDAD",
                "SP_SESION_CREATE",
                "{\"token\":\"[REDACTADO]\"}",
                startedAtUtc,
                DateTimeOffset.UtcNow,
                1,
                "EXITOSO",
                null,
                null,
                ipAddress,
                application,
                correlationId
            ),
            cancellationToken
        );
        return createdSessionId;
    }

    /// <inheritdoc />
    public async Task<SessionIdentity?> ValidateSessionAsync(
        string companyCode,
        byte[] tokenHash,
        CancellationToken cancellationToken
    )
    {
        await using var connection = new SqlConnection(databases.GetConnectionString(companyCode));
        await using var command = new SqlCommand("[SEGURIDAD].[SP_SESION_VALIDATE]", connection)
        {
            CommandType = CommandType.StoredProcedure,
        };
        command.Parameters.Add("@I_HASH_TOKEN", SqlDbType.VarBinary, 64).Value = tokenHash;
        AddOutputParameters(command);

        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var sessionId = reader.GetInt64(reader.GetOrdinal("ID_SESION"));
        var correlationId = reader.GetGuid(reader.GetOrdinal("ID_CORRELACION"));
        var expiresAtUtc = new DateTimeOffset(
            reader.GetDateTime(reader.GetOrdinal("FECHA_EXPIRACION_UTC")),
            TimeSpan.Zero
        );
        var userId = reader.GetInt64(reader.GetOrdinal("ID_USUARIO"));
        var companyId = reader.GetInt64(reader.GetOrdinal("ID_EMPRESA"));
        var storedCompanyCode = reader.GetString(reader.GetOrdinal("CODIGO_EMPRESA"));
        if (!string.Equals(storedCompanyCode, companyCode, StringComparison.OrdinalIgnoreCase))
            return null;
        var username = reader.GetString(reader.GetOrdinal("NOMBRE_USUARIO"));
        var displayName =
            $"{reader.GetString(reader.GetOrdinal("NOMBRES"))} {reader.GetString(reader.GetOrdinal("APELLIDO"))}";
        var mustChangePassword = reader.GetBoolean(reader.GetOrdinal("DEBE_CAMBIAR_CLAVE"));
        var branchId = GetNullableInt64(reader, "ID_SUCURSAL");
        var branchCode = GetNullableString(reader, "CODIGO_SUCURSAL");
        var branchName = GetNullableString(reader, "NOMBRE_SUCURSAL");

        var roles = new List<string>();
        if (await reader.NextResultAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
                roles.Add(reader.GetString(0));
        }

        var permissions = new List<string>();
        if (await reader.NextResultAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
                permissions.Add(reader.GetString(0));
        }

        return new SessionIdentity(
            sessionId,
            userId,
            companyId,
            storedCompanyCode,
            username,
            displayName,
            expiresAtUtc,
            mustChangePassword,
            roles,
            permissions,
            correlationId,
            branchId,
            branchCode,
            branchName
        );
    }

    /// <inheritdoc />
    public async Task ChangePasswordAsync(
        string companyCode,
        long userId,
        PasswordHash passwordHash,
        CancellationToken cancellationToken
    )
    {
        var startedAtUtc = DateTimeOffset.UtcNow;
        await using var connection = new SqlConnection(databases.GetConnectionString(companyCode));
        await using var command = new SqlCommand(
            "[SEGURIDAD].[SP_USUARIO_PASSWORD_CHANGE]",
            connection
        )
        {
            CommandType = CommandType.StoredProcedure,
        };
        command.Parameters.Add("@I_ID_USUARIO", SqlDbType.BigInt).Value = userId;
        command.Parameters.Add("@I_HASH_CLAVE", SqlDbType.VarBinary, 64).Value = passwordHash.Hash;
        command.Parameters.Add("@I_SALT_CLAVE", SqlDbType.VarBinary, 32).Value = passwordHash.Salt;
        command.Parameters.Add("@I_ALGORITMO_CLAVE", SqlDbType.NVarChar, 30).Value =
            passwordHash.Algorithm;
        command.Parameters.Add("@I_ITERACIONES_CLAVE", SqlDbType.Int).Value =
            passwordHash.Iterations;
        AddOutputParameters(command, includeRows: true);

        await connection.OpenAsync(cancellationToken);
        await command.ExecuteNonQueryAsync(cancellationToken);
        await auditWriter.WriteAsync(
            new StoredProcedureAuditEntry(
                companyCode,
                "SEGURIDAD",
                userId,
                null,
                "CAMBIO_CONTRASENA",
                "MODIFICAR",
                "SEGURIDAD",
                "SP_USUARIO_PASSWORD_CHANGE",
                "{\"clave\":\"[REDACTADO]\"}",
                startedAtUtc,
                DateTimeOffset.UtcNow,
                1,
                "EXITOSO",
                null,
                null,
                null,
                "OxiTigre.Api",
                Guid.NewGuid()
            ),
            cancellationToken
        );
    }

    /// <inheritdoc />
    public async Task RecordLoginResultAsync(
        string companyCode,
        long userId,
        bool succeeded,
        CancellationToken cancellationToken
    )
    {
        await using var connection = new SqlConnection(databases.GetConnectionString(companyCode));
        await using var command = new SqlCommand(
            "[SEGURIDAD].[SP_USUARIO_LOGIN_RESULT]",
            connection
        )
        {
            CommandType = CommandType.StoredProcedure,
        };
        command.Parameters.Add("@I_ID_USUARIO", SqlDbType.BigInt).Value = userId;
        command.Parameters.Add("@I_ES_EXITOSO", SqlDbType.Bit).Value = succeeded;
        AddOutputParameters(command, includeRows: true);
        await connection.OpenAsync(cancellationToken);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task CloseSessionAsync(
        string companyCode,
        long sessionId,
        long userId,
        CancellationToken cancellationToken
    )
    {
        var startedAtUtc = DateTimeOffset.UtcNow;
        await using var connection = new SqlConnection(databases.GetConnectionString(companyCode));
        await using var command = new SqlCommand("[SEGURIDAD].[SP_SESION_CLOSE]", connection)
        {
            CommandType = CommandType.StoredProcedure,
        };
        command.Parameters.Add("@I_ID_SESION", SqlDbType.BigInt).Value = sessionId;
        command.Parameters.Add("@S_ID_USUARIO", SqlDbType.BigInt).Value = userId;
        AddOutputParameters(command, includeRows: true);
        await connection.OpenAsync(cancellationToken);
        await command.ExecuteNonQueryAsync(cancellationToken);
        await auditWriter.WriteAsync(
            new StoredProcedureAuditEntry(
                companyCode,
                "SEGURIDAD",
                userId,
                sessionId,
                "CIERRE_SESION",
                "MODIFICAR",
                "SEGURIDAD",
                "SP_SESION_CLOSE",
                "{}",
                startedAtUtc,
                DateTimeOffset.UtcNow,
                1,
                "EXITOSO",
                null,
                null,
                null,
                "OxiTigre.Api",
                Guid.NewGuid()
            ),
            cancellationToken
        );
    }

    /// <summary>Lee una fecha UTC opcional desde la columna indicada.</summary>
    /// <param name="reader">Lector posicionado sobre el conjunto de resultados correspondiente.</param>
    /// <param name="column">Nombre de la columna que se lee.</param>
    /// <returns>Objeto construido u obtenido por la operación.</returns>
    private static DateTimeOffset? GetNullableUtc(SqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal)
            ? null
            : new DateTimeOffset(reader.GetDateTime(ordinal), TimeSpan.Zero);
    }

    /// <summary>Lee el siguiente resultado como catálogo de sucursales activas.</summary>
    /// <param name="reader">Lector posicionado en el resultado anterior.</param>
    /// <param name="cancellationToken">Token que permite cancelar la lectura.</param>
    /// <returns>Sucursales activas devueltas por el procedimiento.</returns>
    private static async Task<IReadOnlyList<BranchLoginOption>> ReadBranchesAsync(
        SqlDataReader reader,
        CancellationToken cancellationToken
    )
    {
        var branches = new List<BranchLoginOption>();
        if (!await reader.NextResultAsync(cancellationToken))
        {
            return branches;
        }

        while (await reader.ReadAsync(cancellationToken))
        {
            branches.Add(
                new BranchLoginOption(
                    reader.GetInt64(reader.GetOrdinal("ID_SUCURSAL")),
                    reader.GetString(reader.GetOrdinal("CODIGO")),
                    reader.GetString(reader.GetOrdinal("NOMBRE"))
                )
            );
        }

        return branches;
    }

    /// <summary>Lee un entero largo opcional desde la columna indicada.</summary>
    /// <param name="reader">Lector posicionado sobre el conjunto de resultados correspondiente.</param>
    /// <param name="column">Nombre de la columna que se lee.</param>
    /// <returns>Valor obtenido después de aplicar la conversión o búsqueda.</returns>
    private static long? GetNullableInt64(SqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt64(ordinal);
    }

    /// <summary>Lee texto opcional desde la columna indicada.</summary>
    /// <param name="reader">Lector posicionado sobre el conjunto de resultados correspondiente.</param>
    /// <param name="column">Nombre de la columna que se lee.</param>
    /// <returns>Texto normalizado, localizado o formateado por la operación.</returns>
    private static string? GetNullableString(SqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    /// <summary>Agrega los parámetros de salida comunes de los procedimientos de seguridad.</summary>
    /// <param name="command">Comando SQL que recibe el parámetro o se ejecuta.</param>
    /// <param name="includeRows">Indica si también debe agregarse el contador de filas afectadas.</param>
    private static void AddOutputParameters(SqlCommand command, bool includeRows = false)
    {
        var errorCode = command.Parameters.Add("@O_CODIGO_ERROR", SqlDbType.BigInt);
        errorCode.Direction = ParameterDirection.Output;
        var message = command.Parameters.Add("@O_MENSAJE", SqlDbType.NVarChar, 4000);
        message.Direction = ParameterDirection.Output;
        if (includeRows)
        {
            var rows = command.Parameters.Add("@O_FILAS_AFECTADAS", SqlDbType.Int);
            rows.Direction = ParameterDirection.Output;
        }
    }
}
