/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.DAL.Configuration.SqlConfigurationStore
Archivo: SqlConfigurationStore.cs | Versión: 1.0.0 | Fecha: 2026-08-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Persiste Configuración mediante SP, salidas controladas y auditoría central.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
using System.Data;
using Microsoft.Data.SqlClient;
using OxiTigre.BLL.Configuration;
using OxiTigre.DAL.MultiCompany;
using OxiTigre.DAL.StoredProcedures;

namespace OxiTigre.DAL.Configuration;

/// <summary>Implementa el almacenamiento administrativo sin exponer valores ni referencias secretas.</summary>
public sealed class SqlConfigurationStore(CompanyDatabaseRegistry databases, IStoredProcedureAuditWriter auditWriter) : IConfigurationStore
{
    /// <inheritdoc />
    public async Task<ConfigurationSnapshot> GetAsync(long companyId, string companyCode, long userId, long sessionId,
        CancellationToken cancellationToken)
    {
        var startedAtUtc = DateTimeOffset.UtcNow;
        await using var connection = new SqlConnection(databases.GetConnectionString(companyCode));
        await using var command = Command(connection, "[CONFIGURACION].[SP_CONFIGURACION_GET]");
        command.Parameters.Add("@I_ID_EMPRESA", SqlDbType.BigInt).Value = companyId;
        command.Parameters.Add("@S_ID_USUARIO", SqlDbType.BigInt).Value = userId;
        var outputs = AddOutputs(command);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        CompanyConfiguration? company = null;
        if (await reader.ReadAsync(cancellationToken)) company = new CompanyConfiguration(
            Long(reader, "ID_EMPRESA"), Text(reader, "CODIGO"), Text(reader, "RAZON_SOCIAL"), NullableText(reader, "NOMBRE_FANTASIA"),
            NullableText(reader, "CUIT"), NullableText(reader, "EMAIL"), Text(reader, "CODIGO_ESTADO"), Bytes(reader, "ROW_VERSION"));

        var branches = new List<BranchConfiguration>();
        if (await reader.NextResultAsync(cancellationToken)) while (await reader.ReadAsync(cancellationToken)) branches.Add(new BranchConfiguration(
            Long(reader, "ID_SUCURSAL"), Text(reader, "CODIGO"), Text(reader, "NOMBRE"), NullableText(reader, "DOMICILIO"),
            NullableText(reader, "LOCALIDAD"), NullableText(reader, "PROVINCIA"), NullableText(reader, "CODIGO_POSTAL"),
            Text(reader, "CODIGO_ESTADO"), Bytes(reader, "ROW_VERSION")));

        var units = new List<OperatingUnitConfiguration>();
        if (await reader.NextResultAsync(cancellationToken)) while (await reader.ReadAsync(cancellationToken)) units.Add(new OperatingUnitConfiguration(
            Long(reader, "ID_UNIDAD_OPERATIVA"), Long(reader, "ID_SUCURSAL"), Text(reader, "CODIGO"), Text(reader, "NOMBRE"),
            NullableText(reader, "DESCRIPCION"), Text(reader, "CODIGO_ESTADO"), Bytes(reader, "ROW_VERSION")));

        var phoneTypes = new List<PhoneTypeConfiguration>();
        if (await reader.NextResultAsync(cancellationToken)) while (await reader.ReadAsync(cancellationToken)) phoneTypes.Add(new PhoneTypeConfiguration(
            Long(reader, "ID_TIPO_TELEFONO"), Text(reader, "CODIGO"), Text(reader, "NOMBRE"), NullableText(reader, "DESCRIPCION"),
            Text(reader, "CODIGO_ESTADO"), Bytes(reader, "ROW_VERSION")));

        var states = new List<StateConfiguration>();
        if (await reader.NextResultAsync(cancellationToken)) while (await reader.ReadAsync(cancellationToken)) states.Add(new StateConfiguration(
            Long(reader, "ID_ESTADO"), Text(reader, "ENTIDAD"), Text(reader, "CODIGO_ESTADO"), Text(reader, "NOMBRE"),
            NullableText(reader, "DESCRIPCION"), Bool(reader, "ES_INICIAL"), Bool(reader, "ES_FINAL"), Short(reader, "ORDEN"),
            NullableDate(reader, "FECHA_VIGENCIA_HASTA_UTC"), Bytes(reader, "ROW_VERSION")));

        var parameters = new List<SystemParameterConfiguration>();
        if (await reader.NextResultAsync(cancellationToken)) while (await reader.ReadAsync(cancellationToken)) parameters.Add(new SystemParameterConfiguration(
            Long(reader, "ID_PARAMETRO_SISTEMA"), NullableLong(reader, "ID_MODULO"), NullableText(reader, "CODIGO_MODULO"),
            Text(reader, "CLAVE"), NullableText(reader, "VALOR"), Text(reader, "TIPO_DATO"), Bool(reader, "ES_SECRETO"),
            Bool(reader, "TIENE_REFERENCIA_SECRETO"), NullableText(reader, "DESCRIPCION"), Text(reader, "CODIGO_ESTADO"), Bytes(reader, "ROW_VERSION")));

        var modules = new List<ModuleConfiguration>();
        if (await reader.NextResultAsync(cancellationToken)) while (await reader.ReadAsync(cancellationToken)) modules.Add(new ModuleConfiguration(
            Long(reader, "ID_MODULO"), Short(reader, "NUMERO_MODULO"), Text(reader, "CODIGO"), Text(reader, "NOMBRE"),
            NullableText(reader, "DESCRIPCION"), Short(reader, "ORDEN"), Text(reader, "CODIGO_ESTADO"), Bytes(reader, "ROW_VERSION")));

        var errors = new List<ErrorCatalogConfiguration>();
        if (await reader.NextResultAsync(cancellationToken)) while (await reader.ReadAsync(cancellationToken)) errors.Add(new ErrorCatalogConfiguration(
            Long(reader, "ID_CATALOGO_ERROR"), Long(reader, "ID_MODULO"), Text(reader, "CODIGO_MODULO"), Short(reader, "NUMERO_ERROR"),
            Long(reader, "CODIGO_ERROR"), Text(reader, "NOMBRE"), Text(reader, "DESCRIPCION"), NullableText(reader, "CAUSA_PROBABLE"),
            NullableText(reader, "ACCION_RECOMENDADA"), Text(reader, "SEVERIDAD"), Text(reader, "CODIGO_ESTADO"), Bytes(reader, "ROW_VERSION")));

        var translations = new List<CatalogTranslationConfiguration>();
        if (await reader.NextResultAsync(cancellationToken)) while (await reader.ReadAsync(cancellationToken)) translations.Add(new CatalogTranslationConfiguration(
            Long(reader, "ID_TRADUCCION_CATALOGO"), Text(reader, "ENTIDAD"), Text(reader, "CODIGO"), Text(reader, "CULTURA"),
            Text(reader, "NOMBRE"), NullableText(reader, "DESCRIPCION"), Text(reader, "CODIGO_ESTADO"), Bytes(reader, "ROW_VERSION")));

        var cultureCode = "es-AR";
        if (await reader.NextResultAsync(cancellationToken) && await reader.ReadAsync(cancellationToken)) cultureCode = Text(reader, "CULTURA");
        await reader.DisposeAsync();
        EnsureSuccess(outputs);
        var result = new ConfigurationSnapshot(company ?? throw new ConfigurationOperationException(20002, "No se encontró la empresa de la sesión."),
            branches, units, phoneTypes, states, parameters, modules, errors, translations, cultureCode);
        await AuditAsync(companyCode, userId, sessionId, "CONSULTA_CONFIGURACION", "CONSULTAR", "CONFIGURACION",
            "SP_CONFIGURACION_GET", startedAtUtc, 1, cancellationToken);
        return result;
    }

    /// <inheritdoc />
    public async Task<string> GetCultureAsync(string companyCode, long userId, long sessionId, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(databases.GetConnectionString(companyCode));
        await using var command = Command(connection, "[CONFIGURACION].[SP_USUARIO_PREFERENCIA_GET]");
        command.Parameters.Add("@S_ID_USUARIO", SqlDbType.BigInt).Value = userId;
        var outputs = AddOutputs(command);
        await connection.OpenAsync(cancellationToken);
        var culture = Convert.ToString(await command.ExecuteScalarAsync(cancellationToken), System.Globalization.CultureInfo.InvariantCulture) ?? "es-AR";
        EnsureSuccess(outputs);
        return culture;
    }

    /// <inheritdoc />
    public Task SaveCultureAsync(string companyCode, long userId, long sessionId, string cultureCode, CancellationToken cancellationToken) =>
        ExecuteAsync(companyCode, userId, sessionId, "CAMBIO_IDIOMA", "MODIFICAR", "CONFIGURACION", "SP_USUARIO_PREFERENCIA_SAVE",
            command => Add(command, "@I_CULTURA", SqlDbType.NVarChar, 10, cultureCode), cancellationToken);

    /// <inheritdoc />
    public Task UpdateCompanyAsync(long companyId, string companyCode, long userId, long sessionId, CompanyChange change,
        CancellationToken cancellationToken) => ExecuteAsync(companyCode, userId, sessionId, "MODIFICACION_EMPRESA", "MODIFICAR",
            "CONFIGURACION", "SP_EMPRESA_UPDATE", command =>
            {
                command.Parameters.Add("@I_ID_EMPRESA", SqlDbType.BigInt).Value = companyId;
                Add(command, "@I_RAZON_SOCIAL", SqlDbType.NVarChar, 200, change.LegalName);
                Add(command, "@I_NOMBRE_FANTASIA", SqlDbType.NVarChar, 200, change.TradeName);
                Add(command, "@I_CUIT", SqlDbType.Char, 11, change.TaxId);
                Add(command, "@I_EMAIL", SqlDbType.NVarChar, 254, change.Email);
                AddVersion(command, change.RowVersion);
            }, cancellationToken);

    /// <inheritdoc />
    public Task<long> SaveBranchAsync(long companyId, string companyCode, long userId, long sessionId, BranchChange change,
        CancellationToken cancellationToken) => ExecuteSaveAsync(companyCode, userId, sessionId, "SUCURSAL", change.BranchId,
            "CONFIGURACION", "SP_SUCURSAL_SAVE", "@O_ID_SUCURSAL", command =>
            {
                AddNullableId(command, "@I_ID_SUCURSAL", change.BranchId);
                command.Parameters.Add("@I_ID_EMPRESA", SqlDbType.BigInt).Value = companyId;
                Add(command, "@I_CODIGO", SqlDbType.NVarChar, 30, change.Code); Add(command, "@I_NOMBRE", SqlDbType.NVarChar, 200, change.Name);
                Add(command, "@I_DOMICILIO", SqlDbType.NVarChar, 250, change.Address); Add(command, "@I_LOCALIDAD", SqlDbType.NVarChar, 150, change.City);
                Add(command, "@I_PROVINCIA", SqlDbType.NVarChar, 150, change.Province); Add(command, "@I_CODIGO_POSTAL", SqlDbType.NVarChar, 20, change.PostalCode);
                Add(command, "@I_CODIGO_ESTADO", SqlDbType.NVarChar, 30, change.StatusCode); AddVersion(command, change.RowVersion);
            }, cancellationToken);

    /// <inheritdoc />
    public Task<long> SaveOperatingUnitAsync(long companyId, string companyCode, long userId, long sessionId,
        OperatingUnitChange change, CancellationToken cancellationToken) => ExecuteSaveAsync(companyCode, userId, sessionId,
            "UNIDAD_OPERATIVA", change.OperatingUnitId, "CONFIGURACION", "SP_UNIDAD_OPERATIVA_SAVE", "@O_ID_UNIDAD_OPERATIVA", command =>
            {
                AddNullableId(command, "@I_ID_UNIDAD_OPERATIVA", change.OperatingUnitId);
                command.Parameters.Add("@I_ID_EMPRESA", SqlDbType.BigInt).Value = companyId;
                command.Parameters.Add("@I_ID_SUCURSAL", SqlDbType.BigInt).Value = change.BranchId;
                Add(command, "@I_CODIGO", SqlDbType.NVarChar, 30, change.Code); Add(command, "@I_NOMBRE", SqlDbType.NVarChar, 200, change.Name);
                Add(command, "@I_DESCRIPCION", SqlDbType.NVarChar, 500, change.Description);
                Add(command, "@I_CODIGO_ESTADO", SqlDbType.NVarChar, 30, change.StatusCode); AddVersion(command, change.RowVersion);
            }, cancellationToken);

    /// <inheritdoc />
    public Task<long> SavePhoneTypeAsync(string companyCode, long userId, long sessionId, PhoneTypeChange change,
        CancellationToken cancellationToken) => ExecuteSaveAsync(companyCode, userId, sessionId, "TIPO_TELEFONO", change.PhoneTypeId,
            "CONFIGURACION", "SP_TIPO_TELEFONO_SAVE", "@O_ID_TIPO_TELEFONO", command =>
            {
                AddNullableId(command, "@I_ID_TIPO_TELEFONO", change.PhoneTypeId);
                Add(command, "@I_CODIGO", SqlDbType.NVarChar, 30, change.Code); Add(command, "@I_NOMBRE", SqlDbType.NVarChar, 100, change.Name);
                Add(command, "@I_DESCRIPCION", SqlDbType.NVarChar, 500, change.Description);
                Add(command, "@I_CODIGO_ESTADO", SqlDbType.NVarChar, 30, change.StatusCode); AddVersion(command, change.RowVersion);
            }, cancellationToken);

    /// <inheritdoc />
    public Task UpdateStateAsync(string companyCode, long userId, long sessionId, StateChange change, CancellationToken cancellationToken) =>
        ExecuteAsync(companyCode, userId, sessionId, "MODIFICACION_ESTADO", "MODIFICAR", "CONFIGURACION", "SP_ESTADO_UPDATE", command =>
        {
            command.Parameters.Add("@I_ID_ESTADO", SqlDbType.BigInt).Value = change.StateId;
            Add(command, "@I_NOMBRE", SqlDbType.NVarChar, 100, change.Name); Add(command, "@I_DESCRIPCION", SqlDbType.NVarChar, 500, change.Description);
            command.Parameters.Add("@I_ORDEN", SqlDbType.SmallInt).Value = change.Order;
            command.Parameters.Add("@I_FECHA_VIGENCIA_HASTA_UTC", SqlDbType.DateTime2).Value = change.ValidUntilUtc ?? (object)DBNull.Value;
            AddVersion(command, change.RowVersion);
        }, cancellationToken);

    /// <inheritdoc />
    public Task<long> SaveParameterAsync(string companyCode, long userId, long sessionId, SystemParameterChange change,
        CancellationToken cancellationToken) => ExecuteSaveAsync(companyCode, userId, sessionId, "PARAMETRO", change.ParameterId,
            "CONFIGURACION", "SP_PARAMETRO_SISTEMA_SAVE", "@O_ID_PARAMETRO_SISTEMA", command =>
            {
                AddNullableId(command, "@I_ID_PARAMETRO_SISTEMA", change.ParameterId); AddNullableId(command, "@I_ID_MODULO", change.ModuleId);
                Add(command, "@I_CLAVE", SqlDbType.NVarChar, 100, change.Key); Add(command, "@I_VALOR", SqlDbType.NVarChar, 2000, change.Value);
                Add(command, "@I_TIPO_DATO", SqlDbType.NVarChar, 30, change.DataType); command.Parameters.Add("@I_ES_SECRETO", SqlDbType.Bit).Value = change.IsSecret;
                Add(command, "@I_REFERENCIA_SECRETO", SqlDbType.NVarChar, 250, change.SecretReference);
                Add(command, "@I_DESCRIPCION", SqlDbType.NVarChar, 500, change.Description);
                Add(command, "@I_CODIGO_ESTADO", SqlDbType.NVarChar, 30, change.StatusCode); AddVersion(command, change.RowVersion);
            }, cancellationToken);

    /// <inheritdoc />
    public Task<long> SaveModuleAsync(string companyCode, long userId, long sessionId, ModuleChange change,
        CancellationToken cancellationToken) => ExecuteSaveAsync(companyCode, userId, sessionId, "MODULO", change.ModuleId,
            "CONFIGURACION", "SP_MODULO_SAVE", "@O_ID_MODULO", command =>
            {
                AddNullableId(command, "@I_ID_MODULO", change.ModuleId); command.Parameters.Add("@I_NUMERO_MODULO", SqlDbType.SmallInt).Value = change.ModuleNumber;
                Add(command, "@I_CODIGO", SqlDbType.NVarChar, 30, change.Code); Add(command, "@I_NOMBRE", SqlDbType.NVarChar, 150, change.Name);
                Add(command, "@I_DESCRIPCION", SqlDbType.NVarChar, 500, change.Description); command.Parameters.Add("@I_ORDEN", SqlDbType.SmallInt).Value = change.Order;
                Add(command, "@I_CODIGO_ESTADO", SqlDbType.NVarChar, 30, change.StatusCode); AddVersion(command, change.RowVersion);
            }, cancellationToken);

    /// <inheritdoc />
    public Task<long> CreateErrorAsync(string companyCode, long userId, long sessionId, CreateErrorCatalogChange change,
        CancellationToken cancellationToken) => ExecuteSaveAsync(companyCode, userId, sessionId, "ERROR_CATALOGO", null,
            "AUDITORIA", "SP_ERROR_CREATE", "@O_CODIGO_GENERADO", command =>
            {
                command.Parameters.Add("@I_ID_MODULO", SqlDbType.BigInt).Value = change.ModuleId;
                Add(command, "@I_NOMBRE", SqlDbType.NVarChar, 200, change.Name); Add(command, "@I_DESCRIPCION", SqlDbType.NVarChar, 1000, change.Description);
                Add(command, "@I_CAUSA_PROBABLE", SqlDbType.NVarChar, 1000, change.ProbableCause);
                Add(command, "@I_ACCION_RECOMENDADA", SqlDbType.NVarChar, 2000, change.RecommendedAction);
                Add(command, "@I_SEVERIDAD", SqlDbType.NVarChar, 30, change.Severity);
            }, cancellationToken);

    /// <inheritdoc />
    public Task UpdateErrorAsync(string companyCode, long userId, long sessionId, UpdateErrorCatalogChange change,
        CancellationToken cancellationToken) => ExecuteAsync(companyCode, userId, sessionId, "MODIFICACION_ERROR_CATALOGO", "MODIFICAR",
            "AUDITORIA", "SP_ERROR_UPDATE", command =>
            {
                command.Parameters.Add("@I_ID_CATALOGO_ERROR", SqlDbType.BigInt).Value = change.ErrorId;
                Add(command, "@I_NOMBRE", SqlDbType.NVarChar, 200, change.Name); Add(command, "@I_DESCRIPCION", SqlDbType.NVarChar, 1000, change.Description);
                Add(command, "@I_CAUSA_PROBABLE", SqlDbType.NVarChar, 1000, change.ProbableCause);
                Add(command, "@I_ACCION_RECOMENDADA", SqlDbType.NVarChar, 2000, change.RecommendedAction);
                Add(command, "@I_SEVERIDAD", SqlDbType.NVarChar, 30, change.Severity);
                Add(command, "@I_CODIGO_ESTADO", SqlDbType.NVarChar, 30, change.StatusCode); AddVersion(command, change.RowVersion);
            }, cancellationToken);

    /// <inheritdoc />
    public Task<long> SaveTranslationAsync(string companyCode, long userId, long sessionId, CatalogTranslationChange change,
        CancellationToken cancellationToken) => ExecuteSaveAsync(companyCode, userId, sessionId, "TRADUCCION", change.TranslationId,
            "CONFIGURACION", "SP_TRADUCCION_CATALOGO_SAVE", "@O_ID_TRADUCCION_CATALOGO", command =>
            {
                AddNullableId(command, "@I_ID_TRADUCCION_CATALOGO", change.TranslationId);
                Add(command, "@I_ENTIDAD", SqlDbType.NVarChar, 100, change.Entity); Add(command, "@I_CODIGO", SqlDbType.NVarChar, 100, change.Code);
                Add(command, "@I_CULTURA", SqlDbType.NVarChar, 10, change.CultureCode); Add(command, "@I_NOMBRE", SqlDbType.NVarChar, 200, change.Name);
                Add(command, "@I_DESCRIPCION", SqlDbType.NVarChar, 1000, change.Description);
                Add(command, "@I_CODIGO_ESTADO", SqlDbType.NVarChar, 30, change.StatusCode); AddVersion(command, change.RowVersion);
            }, cancellationToken);

    /// <summary>Ejecuta un comando de configuración, valida su salida y registra la auditoría.</summary>
    /// <param name="companyCode">Código de la empresa cuya base de datos se utiliza.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="sessionId">Identificador de la sesión responsable de la operación.</param>
    /// <param name="functionality">Funcionalidad registrada en la auditoría.</param>
    /// <param name="action">Código de la acción que se conserva en la auditoría.</param>
    /// <param name="schema">Esquema SQL propietario del procedimiento.</param>
    /// <param name="storedProcedure">Nombre del procedimiento almacenado ejecutado.</param>
    /// <param name="parameters">Acción que agrega al comando sus parámetros específicos.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private async Task ExecuteAsync(string companyCode, long userId, long sessionId, string functionality, string action,
        string schema, string storedProcedure, Action<SqlCommand> parameters, CancellationToken cancellationToken)
    {
        var startedAtUtc = DateTimeOffset.UtcNow;
        await using var connection = new SqlConnection(databases.GetConnectionString(companyCode));
        await using var command = Command(connection, $"[{schema}].[{storedProcedure}]");
        parameters(command); command.Parameters.Add("@S_ID_USUARIO", SqlDbType.BigInt).Value = userId;
        var outputs = AddOutputs(command, includeRows: true);
        await connection.OpenAsync(cancellationToken); await command.ExecuteNonQueryAsync(cancellationToken); EnsureSuccess(outputs);
        await AuditAsync(companyCode, userId, sessionId, functionality, action, schema, storedProcedure, startedAtUtc,
            Convert.ToInt32(outputs.Rows!.Value, System.Globalization.CultureInfo.InvariantCulture), cancellationToken);
    }

    /// <summary>Ejecuta un alta o modificación de configuración y devuelve su identificador.</summary>
    /// <param name="companyCode">Código de la empresa cuya base de datos se utiliza.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="sessionId">Identificador de la sesión responsable de la operación.</param>
    /// <param name="functionality">Funcionalidad registrada en la auditoría.</param>
    /// <param name="currentId">Identificador actual del registro, o nulo para un alta.</param>
    /// <param name="schema">Esquema SQL propietario del procedimiento.</param>
    /// <param name="storedProcedure">Nombre del procedimiento almacenado ejecutado.</param>
    /// <param name="outputName">Nombre del parámetro que devuelve el identificador creado.</param>
    /// <param name="parameters">Acción que agrega al comando sus parámetros específicos.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador del registro guardado.</returns>
    private async Task<long> ExecuteSaveAsync(string companyCode, long userId, long sessionId, string functionality, long? currentId,
        string schema, string storedProcedure, string outputName, Action<SqlCommand> parameters, CancellationToken cancellationToken)
    {
        var startedAtUtc = DateTimeOffset.UtcNow;
        await using var connection = new SqlConnection(databases.GetConnectionString(companyCode));
        await using var command = Command(connection, $"[{schema}].[{storedProcedure}]");
        parameters(command); command.Parameters.Add("@S_ID_USUARIO", SqlDbType.BigInt).Value = userId;
        var output = command.Parameters.Add(outputName, SqlDbType.BigInt); output.Direction = ParameterDirection.Output;
        var outputs = AddOutputs(command, includeRows: true);
        await connection.OpenAsync(cancellationToken); await command.ExecuteNonQueryAsync(cancellationToken); EnsureSuccess(outputs);
        var result = Convert.ToInt64(output.Value, System.Globalization.CultureInfo.InvariantCulture);
        await AuditAsync(companyCode, userId, sessionId, functionality, currentId is null ? "CREAR" : "MODIFICAR", schema,
            storedProcedure, startedAtUtc, Convert.ToInt32(outputs.Rows!.Value, System.Globalization.CultureInfo.InvariantCulture), cancellationToken);
        return result;
    }

    /// <summary>Registra la ejecución SQL de configuración con su contexto de seguridad.</summary>
    /// <param name="companyCode">Código de la empresa cuya base de datos se utiliza.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="sessionId">Identificador de la sesión responsable de la operación.</param>
    /// <param name="functionality">Funcionalidad registrada en la auditoría.</param>
    /// <param name="action">Código de la acción que se conserva en la auditoría.</param>
    /// <param name="schema">Esquema SQL propietario del procedimiento.</param>
    /// <param name="storedProcedure">Nombre del procedimiento almacenado ejecutado.</param>
    /// <param name="startedAtUtc">Instante UTC en que comenzó la ejecución auditada.</param>
    /// <param name="rows">Cantidad de filas afectadas informada por el procedimiento.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private Task AuditAsync(string companyCode, long userId, long sessionId, string functionality, string action,
        string schema, string storedProcedure, DateTimeOffset startedAtUtc, int rows, CancellationToken cancellationToken) =>
        auditWriter.WriteAsync(new StoredProcedureAuditEntry(companyCode, "CONFIGURACION", userId, sessionId, functionality, action,
            schema, storedProcedure, "{}", startedAtUtc, DateTimeOffset.UtcNow, rows, "EXITOSO", null, null, null,
            "OxiTigre.Api", Guid.NewGuid()), cancellationToken);

    /// <summary>Crea un comando asociado a la conexión y al procedimiento almacenado indicado.</summary>
    /// <param name="connection">Conexión SQL abierta usada para crear el comando.</param>
    /// <param name="name">Nombre técnico del elemento solicitado.</param>
    /// <returns>Comando SQL configurado para el procedimiento solicitado.</returns>
    private static SqlCommand Command(SqlConnection connection, string name) => new(name, connection) { CommandType = CommandType.StoredProcedure };
    /// <summary>Agrega y devuelve los parámetros de salida comunes de configuración.</summary>
    /// <param name="command">Comando SQL que recibe el parámetro o se ejecuta.</param>
    /// <param name="includeRows">Indica si también debe agregarse el contador de filas afectadas.</param>
    /// <returns>Objeto construido u obtenido por la operación.</returns>
    private static OutputParameters AddOutputs(SqlCommand command, bool includeRows = false)
    {
        var code = command.Parameters.Add("@O_CODIGO_ERROR", SqlDbType.BigInt); code.Direction = ParameterDirection.Output;
        var message = command.Parameters.Add("@O_MENSAJE", SqlDbType.NVarChar, 4000); message.Direction = ParameterDirection.Output;
        SqlParameter? rows = null;
        if (includeRows) { rows = command.Parameters.Add("@O_FILAS_AFECTADAS", SqlDbType.Int); rows.Direction = ParameterDirection.Output; }
        return new OutputParameters(code, message, rows);
    }
    /// <summary>Convierte una salida funcional fallida del procedimiento en una excepción controlada.</summary>
    /// <param name="outputs">Parámetros de salida devueltos por el procedimiento.</param>
    /// <exception cref="ConfigurationOperationException">Se produce cuando una respuesta o estado inválido impide completar la operación.</exception>
    private static void EnsureSuccess(OutputParameters outputs)
    {
        if (outputs.Code.Value is DBNull or null) return;
        throw new ConfigurationOperationException(Convert.ToInt64(outputs.Code.Value, System.Globalization.CultureInfo.InvariantCulture),
            Convert.ToString(outputs.Message.Value, System.Globalization.CultureInfo.InvariantCulture) ?? "La operación no pudo completarse.");
    }
    /// <summary>Agrega un parámetro tipado al comando y representa los valores nulos con DBNull.</summary>
    /// <param name="command">Comando SQL que recibe el parámetro o se ejecuta.</param>
    /// <param name="name">Nombre del parámetro SQL que se agrega.</param>
    /// <param name="type">Tipo SQL que debe aplicarse al parámetro.</param>
    /// <param name="size">Longitud máxima admitida por el parámetro.</param>
    /// <param name="value">Valor que se convierte o asigna al destino correspondiente.</param>
    private static void Add(SqlCommand command, string name, SqlDbType type, int size, object? value) =>
        command.Parameters.Add(name, type, size).Value = value ?? DBNull.Value;
    /// <summary>Agrega un identificador opcional al comando SQL.</summary>
    /// <param name="command">Comando SQL que recibe el parámetro o se ejecuta.</param>
    /// <param name="name">Nombre del parámetro SQL que se agrega.</param>
    /// <param name="value">Valor que se convierte o asigna al destino correspondiente.</param>
    private static void AddNullableId(SqlCommand command, string name, long? value) =>
        command.Parameters.Add(name, SqlDbType.BigInt).Value = value ?? (object)DBNull.Value;
    /// <summary>Agrega la versión de concurrencia opcional al comando SQL.</summary>
    /// <param name="command">Comando SQL que recibe el parámetro o se ejecuta.</param>
    /// <param name="version">Versión binaria usada para controlar concurrencia.</param>
    private static void AddVersion(SqlCommand command, byte[]? version) =>
        command.Parameters.Add("@I_ROW_VERSION", SqlDbType.Binary, 8).Value = version ?? (object)DBNull.Value;
    /// <summary>Lee un entero largo obligatorio desde la columna indicada.</summary>
    /// <param name="reader">Lector posicionado sobre el conjunto de resultados correspondiente.</param>
    /// <param name="name">Nombre de la columna que se lee.</param>
    /// <returns>Valor obtenido después de aplicar la conversión o búsqueda.</returns>
    private static long Long(SqlDataReader reader, string name) => reader.GetInt64(reader.GetOrdinal(name));
    /// <summary>Lee un entero largo opcional desde la columna indicada.</summary>
    /// <param name="reader">Lector posicionado sobre el conjunto de resultados correspondiente.</param>
    /// <param name="name">Nombre de la columna que se lee.</param>
    /// <returns>Valor obtenido después de aplicar la conversión o búsqueda.</returns>
    private static long? NullableLong(SqlDataReader reader, string name) { var index = reader.GetOrdinal(name); return reader.IsDBNull(index) ? null : reader.GetInt64(index); }
    /// <summary>Lee un entero corto obligatorio desde la columna indicada.</summary>
    /// <param name="reader">Lector posicionado sobre el conjunto de resultados correspondiente.</param>
    /// <param name="name">Nombre de la columna que se lee.</param>
    /// <returns>Valor obtenido después de aplicar la conversión o búsqueda.</returns>
    private static short Short(SqlDataReader reader, string name) => reader.GetInt16(reader.GetOrdinal(name));
    /// <summary>Lee texto obligatorio desde la columna indicada.</summary>
    /// <param name="reader">Lector posicionado sobre el conjunto de resultados correspondiente.</param>
    /// <param name="name">Nombre de la columna que se lee.</param>
    /// <returns>Texto normalizado, localizado o formateado por la operación.</returns>
    private static string Text(SqlDataReader reader, string name) => reader.GetString(reader.GetOrdinal(name));
    /// <summary>Lee texto opcional desde la columna indicada.</summary>
    /// <param name="reader">Lector posicionado sobre el conjunto de resultados correspondiente.</param>
    /// <param name="name">Nombre de la columna que se lee.</param>
    /// <returns>Texto normalizado, localizado o formateado por la operación.</returns>
    private static string? NullableText(SqlDataReader reader, string name) { var index = reader.GetOrdinal(name); return reader.IsDBNull(index) ? null : reader.GetString(index); }
    /// <summary>Lee un indicador booleano desde la columna indicada.</summary>
    /// <param name="reader">Lector posicionado sobre el conjunto de resultados correspondiente.</param>
    /// <param name="name">Nombre de la columna que se lee.</param>
    /// <returns>Verdadero cuando se cumple la condición evaluada; en caso contrario, falso.</returns>
    private static bool Bool(SqlDataReader reader, string name) => reader.GetBoolean(reader.GetOrdinal(name));
    /// <summary>Lee una fecha opcional desde la columna indicada.</summary>
    /// <param name="reader">Lector posicionado sobre el conjunto de resultados correspondiente.</param>
    /// <param name="name">Nombre de la columna que se lee.</param>
    /// <returns>Valor obtenido después de aplicar la conversión o búsqueda.</returns>
    private static DateTime? NullableDate(SqlDataReader reader, string name) { var index = reader.GetOrdinal(name); return reader.IsDBNull(index) ? null : reader.GetDateTime(index); }
    /// <summary>Lee el valor binario obligatorio de la columna indicada.</summary>
    /// <param name="reader">Lector posicionado sobre el conjunto de resultados correspondiente.</param>
    /// <param name="name">Nombre de la columna que se lee.</param>
    /// <returns>Colección de registros u opciones obtenida por la operación.</returns>
    private static byte[] Bytes(SqlDataReader reader, string name) => (byte[])reader[name];
    private sealed record OutputParameters(SqlParameter Code, SqlParameter Message, SqlParameter? Rows);
}
