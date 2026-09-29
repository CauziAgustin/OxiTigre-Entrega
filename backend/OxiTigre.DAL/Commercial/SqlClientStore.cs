/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.DAL.Commercial.SqlClientStore
Archivo: SqlClientStore.cs | Versión: 2.2.0 | Fecha: 2026-08-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Persiste el ciclo completo de clientes mediante SP y registra cada ejecución.
Historial: 1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Consulta inicial de clientes.
Historial: 2.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Alta, detalle, edición, estado y teléfonos.
Historial: 2.1.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Catálogos y borrador persistente por usuario.
Historial: 2.2.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Catálogos localizados por preferencia de usuario.
===============================================================================
*/
using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using OxiTigre.BLL.Commercial;
using OxiTigre.DAL.MultiCompany;
using OxiTigre.DAL.StoredProcedures;

namespace OxiTigre.DAL.Commercial;

/// <summary>Implementa el almacenamiento del módulo Comercial exclusivamente mediante procedimientos almacenados.</summary>
public sealed class SqlClientStore(CompanyDatabaseRegistry databases, IStoredProcedureAuditWriter auditWriter) : IClientStore
{
    /// <inheritdoc />
    public async Task<ClientCatalogs> GetCatalogsAsync(string companyCode, long sessionId, long userId,
        CancellationToken cancellationToken)
    {
        var startedAtUtc = DateTimeOffset.UtcNow;
        await using var connection = new SqlConnection(databases.GetConnectionString(companyCode));
        await using var command = StoredProcedure("[CONFIGURACION].[SP_CLIENTE_CATALOGOS_GET]");
        command.Connection = connection;
        command.Parameters.Add("@S_ID_USUARIO", SqlDbType.BigInt).Value = userId;
        AddOutputs(command);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var documents = new List<DocumentTypeOption>();
        while (await reader.ReadAsync(cancellationToken)) documents.Add(new DocumentTypeOption(
            reader.GetString(reader.GetOrdinal("CODIGO")), reader.GetString(reader.GetOrdinal("NOMBRE")),
            reader.GetBoolean(reader.GetOrdinal("APLICA_PERSONA_FISICA")), reader.GetBoolean(reader.GetOrdinal("APLICA_PERSONA_JURIDICA"))));
        var countries = new List<CountryOption>();
        if (await reader.NextResultAsync(cancellationToken)) while (await reader.ReadAsync(cancellationToken))
            countries.Add(new CountryOption(reader.GetString(reader.GetOrdinal("CODIGO")),
                reader.GetString(reader.GetOrdinal("NOMBRE")), GetNullableString(reader, "CODIGO_TELEFONICO"),
                reader.GetBoolean(reader.GetOrdinal("ES_PREDETERMINADO"))));
        var phoneTypes = new List<PhoneTypeOption>();
        if (await reader.NextResultAsync(cancellationToken)) while (await reader.ReadAsync(cancellationToken))
            phoneTypes.Add(new PhoneTypeOption(reader.GetString(reader.GetOrdinal("CODIGO")), reader.GetString(reader.GetOrdinal("NOMBRE"))));
        await AuditAsync(companyCode, userId, sessionId, "CONSULTA_CATALOGOS_CLIENTE", "CONSULTAR",
            "SP_CLIENTE_CATALOGOS_GET", "{}", startedAtUtc, documents.Count + countries.Count + phoneTypes.Count,
            cancellationToken, "CONFIGURACION");
        return new ClientCatalogs(documents, countries, phoneTypes);
    }

    /// <inheritdoc />
    public async Task<SavedClientDraft?> GetDraftAsync(long companyId, string companyCode, long sessionId,
        long userId, CancellationToken cancellationToken)
    {
        var startedAtUtc = DateTimeOffset.UtcNow;
        await using var connection = new SqlConnection(databases.GetConnectionString(companyCode));
        await using var command = StoredProcedure("[COMERCIAL].[SP_CLIENTE_BORRADOR_GET]");
        command.Connection = connection;
        command.Parameters.Add("@I_ID_EMPRESA", SqlDbType.BigInt).Value = companyId;
        command.Parameters.Add("@S_ID_USUARIO", SqlDbType.BigInt).Value = userId;
        AddOutputs(command);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        var draft = JsonSerializer.Deserialize<ClientDraft>(reader.GetString(reader.GetOrdinal("CONTENIDO_JSON")))
            ?? throw new InvalidDataException("El borrador guardado no tiene un formato válido.");
        var saved = new SavedClientDraft(draft,
            new DateTimeOffset(reader.GetDateTime(reader.GetOrdinal("FECHA_ULTIMO_GUARDADO_UTC")), TimeSpan.Zero));
        await AuditAsync(companyCode, userId, sessionId, "CONSULTA_BORRADOR_CLIENTE", "CONSULTAR",
            "SP_CLIENTE_BORRADOR_GET", "{}", startedAtUtc, 1, cancellationToken);
        return saved;
    }

    /// <inheritdoc />
    public async Task SaveDraftAsync(long companyId, string companyCode, ClientDraft draft, long sessionId,
        long userId, CancellationToken cancellationToken)
    {
        var startedAtUtc = DateTimeOffset.UtcNow;
        await using var connection = new SqlConnection(databases.GetConnectionString(companyCode));
        await using var command = StoredProcedure("[COMERCIAL].[SP_CLIENTE_BORRADOR_SAVE]");
        command.Connection = connection;
        command.Parameters.Add("@I_ID_EMPRESA", SqlDbType.BigInt).Value = companyId;
        command.Parameters.Add("@I_CONTENIDO_JSON", SqlDbType.NVarChar, -1).Value = JsonSerializer.Serialize(draft);
        command.Parameters.Add("@S_ID_SESION", SqlDbType.BigInt).Value = sessionId;
        command.Parameters.Add("@S_ID_USUARIO", SqlDbType.BigInt).Value = userId;
        AddOutputs(command, includeRows: true);
        await connection.OpenAsync(cancellationToken);
        await command.ExecuteNonQueryAsync(cancellationToken);
        await AuditAsync(companyCode, userId, sessionId, "GUARDADO_BORRADOR_CLIENTE", "MODIFICAR",
            "SP_CLIENTE_BORRADOR_SAVE", "{\"contenido\":\"[REDACTADO]\"}", startedAtUtc, 1, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteDraftAsync(long companyId, string companyCode, long sessionId, long userId,
        CancellationToken cancellationToken)
    {
        var startedAtUtc = DateTimeOffset.UtcNow;
        await using var connection = new SqlConnection(databases.GetConnectionString(companyCode));
        await using var command = StoredProcedure("[COMERCIAL].[SP_CLIENTE_BORRADOR_DELETE]");
        command.Connection = connection;
        command.Parameters.Add("@I_ID_EMPRESA", SqlDbType.BigInt).Value = companyId;
        command.Parameters.Add("@S_ID_SESION", SqlDbType.BigInt).Value = sessionId;
        command.Parameters.Add("@S_ID_USUARIO", SqlDbType.BigInt).Value = userId;
        AddOutputs(command, includeRows: true);
        await connection.OpenAsync(cancellationToken);
        await command.ExecuteNonQueryAsync(cancellationToken);
        await AuditAsync(companyCode, userId, sessionId, "DESCARTE_BORRADOR_CLIENTE", "MODIFICAR",
            "SP_CLIENTE_BORRADOR_DELETE", "{}", startedAtUtc, 1, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ClientSummary>> ListAsync(long companyId, string companyCode, string statusCode,
        long sessionId, long userId, CancellationToken cancellationToken)
    {
        var startedAtUtc = DateTimeOffset.UtcNow;
        var clients = new List<ClientSummary>();
        await using var connection = new SqlConnection(databases.GetConnectionString(companyCode));
        await using var command = StoredProcedure("[COMERCIAL].[SP_CLIENTE_LIST]");
        command.Connection = connection;
        command.Parameters.Add("@I_ID_EMPRESA", SqlDbType.BigInt).Value = companyId;
        command.Parameters.Add("@I_CODIGO_ESTADO", SqlDbType.NVarChar, 30).Value = statusCode;
        command.Parameters.Add("@S_ID_SESION", SqlDbType.BigInt).Value = sessionId;
        AddOutputs(command);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            clients.Add(new ClientSummary(reader.GetInt64(reader.GetOrdinal("ID_CLIENTE")),
                reader.GetString(reader.GetOrdinal("CODIGO")), reader.GetString(reader.GetOrdinal("TIPO_PERSONA")),
                reader.GetString(reader.GetOrdinal("NOMBRE_RAZON_SOCIAL")), GetNullableString(reader, "APELLIDO"),
                GetNullableString(reader, "NUMERO_DOCUMENTO"), GetNullableString(reader, "EMAIL"),
                reader.GetString(reader.GetOrdinal("CODIGO_ESTADO")), GetNullableString(reader, "TELEFONO_PRINCIPAL"),
                reader.GetInt64(reader.GetOrdinal("CANTIDAD_TELEFONOS"))));
        }

        await AuditAsync(companyCode, userId, sessionId, "CONSULTA_CLIENTES", "CONSULTAR", "SP_CLIENTE_LIST",
            JsonSerializer.Serialize(new { statusCode }), startedAtUtc, clients.Count, cancellationToken);
        return clients;
    }

    /// <inheritdoc />
    public async Task<ClientDetails?> GetAsync(long companyId, string companyCode, long clientId, long sessionId,
        long userId, CancellationToken cancellationToken)
    {
        var startedAtUtc = DateTimeOffset.UtcNow;
        await using var connection = new SqlConnection(databases.GetConnectionString(companyCode));
        await using var command = StoredProcedure("[COMERCIAL].[SP_CLIENTE_GET]");
        command.Connection = connection;
        command.Parameters.Add("@I_ID_EMPRESA", SqlDbType.BigInt).Value = companyId;
        command.Parameters.Add("@I_ID_CLIENTE", SqlDbType.BigInt).Value = clientId;
        AddOutputs(command);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;

        var details = new ClientDetails(reader.GetInt64(reader.GetOrdinal("ID_CLIENTE")),
            reader.GetString(reader.GetOrdinal("CODIGO")), reader.GetString(reader.GetOrdinal("TIPO_PERSONA")),
            reader.GetString(reader.GetOrdinal("NOMBRE_RAZON_SOCIAL")), GetNullableString(reader, "APELLIDO"),
            GetNullableString(reader, "TIPO_DOCUMENTO"), GetNullableString(reader, "NUMERO_DOCUMENTO"),
            GetNullableString(reader, "EMAIL"), GetNullableString(reader, "OBSERVACION"),
            reader.GetString(reader.GetOrdinal("CODIGO_ESTADO")), await ReadPhonesAsync(reader, cancellationToken));

        await AuditAsync(companyCode, userId, sessionId, "CONSULTA_CLIENTE", "CONSULTAR", "SP_CLIENTE_GET",
            JsonSerializer.Serialize(new { clientId }), startedAtUtc, 1, cancellationToken);
        return details;
    }

    /// <inheritdoc />
    public async Task<long> CreateAsync(long companyId, string companyCode, ClientDraft draft, long sessionId,
        long userId, CancellationToken cancellationToken)
    {
        var startedAtUtc = DateTimeOffset.UtcNow;
        await using var connection = new SqlConnection(databases.GetConnectionString(companyCode));
        await using var command = StoredProcedure("[COMERCIAL].[SP_CLIENTE_CREATE]");
        command.Connection = connection;
        AddDraftParameters(command, companyId, draft, sessionId, userId);
        var clientId = command.Parameters.Add("@O_ID_CLIENTE", SqlDbType.BigInt);
        clientId.Direction = ParameterDirection.Output;
        AddOutputs(command, includeRows: true);
        await connection.OpenAsync(cancellationToken);
        await command.ExecuteNonQueryAsync(cancellationToken);
        var createdId = Convert.ToInt64(clientId.Value, System.Globalization.CultureInfo.InvariantCulture);
        await AuditAsync(companyCode, userId, sessionId, "ALTA_CLIENTE", "CREAR", "SP_CLIENTE_CREATE",
            JsonSerializer.Serialize(new { clientId = createdId, phoneCount = draft.Phones.Count }), startedAtUtc,
            1 + draft.Phones.Count, cancellationToken);
        return createdId;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(long companyId, string companyCode, long clientId, ClientDraft draft,
        long sessionId, long userId, CancellationToken cancellationToken)
    {
        var startedAtUtc = DateTimeOffset.UtcNow;
        await using var connection = new SqlConnection(databases.GetConnectionString(companyCode));
        await using var command = StoredProcedure("[COMERCIAL].[SP_CLIENTE_UPDATE]");
        command.Connection = connection;
        AddDraftParameters(command, companyId, draft, sessionId, userId);
        command.Parameters.Add("@I_ID_CLIENTE", SqlDbType.BigInt).Value = clientId;
        AddOutputs(command, includeRows: true);
        await connection.OpenAsync(cancellationToken);
        await command.ExecuteNonQueryAsync(cancellationToken);
        await AuditAsync(companyCode, userId, sessionId, "MODIFICACION_CLIENTE", "MODIFICAR", "SP_CLIENTE_UPDATE",
            JsonSerializer.Serialize(new { clientId, phoneCount = draft.Phones.Count }), startedAtUtc,
            1 + draft.Phones.Count, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ChangeStatusAsync(long companyId, string companyCode, long clientId, string statusCode,
        long sessionId, long userId, CancellationToken cancellationToken)
    {
        var startedAtUtc = DateTimeOffset.UtcNow;
        await using var connection = new SqlConnection(databases.GetConnectionString(companyCode));
        await using var command = StoredProcedure("[COMERCIAL].[SP_CLIENTE_STATUS_CHANGE]");
        command.Connection = connection;
        command.Parameters.Add("@I_ID_EMPRESA", SqlDbType.BigInt).Value = companyId;
        command.Parameters.Add("@I_ID_CLIENTE", SqlDbType.BigInt).Value = clientId;
        command.Parameters.Add("@I_CODIGO_ESTADO", SqlDbType.NVarChar, 30).Value = statusCode;
        command.Parameters.Add("@S_ID_SESION", SqlDbType.BigInt).Value = sessionId;
        command.Parameters.Add("@S_ID_USUARIO", SqlDbType.BigInt).Value = userId;
        AddOutputs(command, includeRows: true);
        await connection.OpenAsync(cancellationToken);
        await command.ExecuteNonQueryAsync(cancellationToken);
        await AuditAsync(companyCode, userId, sessionId, "CAMBIO_ESTADO_CLIENTE", "MODIFICAR",
            "SP_CLIENTE_STATUS_CHANGE", JsonSerializer.Serialize(new { clientId, statusCode }), startedAtUtc, 1, cancellationToken);
    }

    /// <summary>Crea un comando configurado para ejecutar un procedimiento almacenado de clientes.</summary>
    /// <param name="name">Nombre técnico del elemento solicitado.</param>
    /// <returns>Comando SQL configurado para el procedimiento solicitado.</returns>
    private static SqlCommand StoredProcedure(string name) => new(name) { CommandType = CommandType.StoredProcedure };

    /// <summary>Agrega al comando los parámetros necesarios para guardar un borrador de cliente.</summary>
    /// <param name="command">Comando SQL que recibe el parámetro o se ejecuta.</param>
    /// <param name="companyId">Identificador interno de la empresa.</param>
    /// <param name="draft">Borrador de cliente cuyos datos se envían al procedimiento.</param>
    /// <param name="sessionId">Identificador de la sesión responsable de la operación.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    private static void AddDraftParameters(SqlCommand command, long companyId, ClientDraft draft, long sessionId, long userId)
    {
        command.Parameters.Add("@I_ID_EMPRESA", SqlDbType.BigInt).Value = companyId;
        command.Parameters.Add("@I_TIPO_PERSONA", SqlDbType.Char, 1).Value = draft.PersonType;
        Add(command, "@I_NOMBRE_RAZON_SOCIAL", SqlDbType.NVarChar, 200, draft.NameOrBusinessName);
        Add(command, "@I_APELLIDO", SqlDbType.NVarChar, 150, draft.Surname);
        Add(command, "@I_TIPO_DOCUMENTO", SqlDbType.NVarChar, 20, draft.DocumentType);
        Add(command, "@I_NUMERO_DOCUMENTO", SqlDbType.NVarChar, 30, draft.DocumentNumber);
        Add(command, "@I_EMAIL", SqlDbType.NVarChar, 254, draft.Email);
        Add(command, "@I_OBSERVACION", SqlDbType.NVarChar, 1000, draft.Observation);
        command.Parameters.Add("@I_TELEFONOS_JSON", SqlDbType.NVarChar, -1).Value = JsonSerializer.Serialize(draft.Phones);
        command.Parameters.Add("@S_ID_SESION", SqlDbType.BigInt).Value = sessionId;
        command.Parameters.Add("@S_ID_USUARIO", SqlDbType.BigInt).Value = userId;
    }

    /// <summary>Lee todos los teléfonos devueltos por el procedimiento de clientes.</summary>
    /// <param name="reader">Lector posicionado sobre el conjunto de resultados correspondiente.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Teléfonos leídos del resultado del procedimiento.</returns>
    private static async Task<IReadOnlyList<ClientPhone>> ReadPhonesAsync(SqlDataReader reader, CancellationToken cancellationToken)
    {
        var phones = new List<ClientPhone>();
        if (!await reader.NextResultAsync(cancellationToken)) return phones;
        while (await reader.ReadAsync(cancellationToken))
        {
            phones.Add(new ClientPhone(reader.GetInt64(reader.GetOrdinal("ID_CLIENTE_TELEFONO")),
                reader.GetString(reader.GetOrdinal("CODIGO_TIPO")), reader.GetString(reader.GetOrdinal("NOMBRE_TIPO")),
                GetNullableString(reader, "CODIGO_PAIS"), GetNullableString(reader, "CODIGO_AREA"),
                reader.GetString(reader.GetOrdinal("NUMERO")), GetNullableString(reader, "INTERNO"),
                reader.GetInt16(reader.GetOrdinal("ORDEN")), reader.GetBoolean(reader.GetOrdinal("ES_PRINCIPAL")),
                reader.GetBoolean(reader.GetOrdinal("PERMITE_WHATSAPP")), GetNullableString(reader, "OBSERVACION"),
                reader.GetString(reader.GetOrdinal("CODIGO_ESTADO"))));
        }
        return phones;
    }

    /// <summary>Registra la ejecución SQL de clientes con usuario, sesión, parámetros y duración.</summary>
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
    /// <param name="schema">Esquema SQL propietario del procedimiento.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private Task AuditAsync(string companyCode, long userId, long sessionId, string functionality, string action,
        string storedProcedure, string parameters, DateTimeOffset startedAtUtc, int rows, CancellationToken cancellationToken,
        string schema = "COMERCIAL") =>
        auditWriter.WriteAsync(new StoredProcedureAuditEntry(companyCode, "COMERCIAL", userId, sessionId,
            functionality, action, schema, storedProcedure, parameters, startedAtUtc, DateTimeOffset.UtcNow,
            rows, "EXITOSO", null, null, null, "OxiTigre.Api", Guid.NewGuid()), cancellationToken);

    /// <summary>Agrega un parámetro tipado al comando y representa los valores nulos con DBNull.</summary>
    /// <param name="command">Comando SQL que recibe el parámetro o se ejecuta.</param>
    /// <param name="name">Nombre del parámetro SQL que se agrega.</param>
    /// <param name="type">Tipo SQL que debe aplicarse al parámetro.</param>
    /// <param name="size">Longitud máxima admitida por el parámetro.</param>
    /// <param name="value">Valor que se convierte o asigna al destino correspondiente.</param>
    private static void Add(SqlCommand command, string name, SqlDbType type, int size, object? value) =>
        command.Parameters.Add(name, type, size).Value = value ?? DBNull.Value;

    /// <summary>Agrega los parámetros de salida comunes de los procedimientos de clientes.</summary>
    /// <param name="command">Comando SQL que recibe el parámetro o se ejecuta.</param>
    /// <param name="includeRows">Indica si también debe agregarse el contador de filas afectadas.</param>
    private static void AddOutputs(SqlCommand command, bool includeRows = false)
    {
        var error = command.Parameters.Add("@O_CODIGO_ERROR", SqlDbType.BigInt); error.Direction = ParameterDirection.Output;
        var message = command.Parameters.Add("@O_MENSAJE", SqlDbType.NVarChar, 4000); message.Direction = ParameterDirection.Output;
        if (includeRows) { var rows = command.Parameters.Add("@O_FILAS_AFECTADAS", SqlDbType.Int); rows.Direction = ParameterDirection.Output; }
    }

    /// <summary>Lee una columna de texto y devuelve nulo cuando contiene DBNull.</summary>
    /// <param name="reader">Lector posicionado sobre el conjunto de resultados correspondiente.</param>
    /// <param name="columnName">Nombre de la columna que se lee.</param>
    /// <returns>Texto normalizado, localizado o formateado por la operación.</returns>
    private static string? GetNullableString(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }
}
