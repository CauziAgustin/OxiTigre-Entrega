/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.DAL.Finance.SqlFinanceStore
Archivo: SqlFinanceStore.cs | Versión: 11.0.0 | Fecha: 2026-08-27 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Persiste Finanzas mediante una consulta y un comando consolidados y auditados.
Historial: 11.0.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
using System.Data;
using Microsoft.Data.SqlClient;
using OxiTigre.BLL.Finance;
using OxiTigre.DAL.MultiCompany;
using OxiTigre.DAL.StoredProcedures;

namespace OxiTigre.DAL.Finance;

/// <summary>Implementa caja, cobros y cuenta corriente sobre SQL Server.</summary>
/// <param name="databases">Registro de bases operativas autorizadas.</param>
/// <param name="auditWriter">Auditor de ejecuciones de SP.</param>
public sealed class SqlFinanceStore(
    CompanyDatabaseRegistry databases,
    IStoredProcedureAuditWriter auditWriter
) : IFinanceStore
{
    /// <inheritdoc />
    public async Task<FinanceSnapshot> GetAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        CancellationToken cancellationToken
    )
    {
        var started = DateTimeOffset.UtcNow;
        await using var connection = new SqlConnection(databases.GetConnectionString(companyCode));
        await using var command = Command(connection, "SP_FINANZAS_GET");
        Context(command, companyId, userId, sessionId);
        var output = Outputs(command);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var branches = new List<FinanceBranch>();
        while (await reader.ReadAsync(cancellationToken))
            branches.Add(
                new(Long(reader, "ID_SUCURSAL"), Text(reader, "CODIGO"), Text(reader, "NOMBRE"))
            );

        var clients = new List<FinanceClient>();
        if (await reader.NextResultAsync(cancellationToken))
            while (await reader.ReadAsync(cancellationToken))
                clients.Add(
                    new(
                        Long(reader, "ID_CLIENTE"),
                        Text(reader, "CODIGO"),
                        Text(reader, "CLIENTE"),
                        Decimal(reader, "SALDO")
                    )
                );

        var methods = new List<PaymentMethod>();
        if (await reader.NextResultAsync(cancellationToken))
            while (await reader.ReadAsync(cancellationToken))
                methods.Add(
                    new(
                        Long(reader, "ID_MEDIO_PAGO"),
                        Text(reader, "CODIGO"),
                        Text(reader, "NOMBRE"),
                        Text(reader, "TIPO"),
                        Bool(reader, "AFECTA_EFECTIVO"),
                        Bool(reader, "REQUIERE_REFERENCIA"),
                        Text(reader, "CODIGO_ESTADO"),
                        Bytes(reader, "ROW_VERSION")
                    )
                );

        var cashBoxes = new List<CashBox>();
        if (await reader.NextResultAsync(cancellationToken))
            while (await reader.ReadAsync(cancellationToken))
                cashBoxes.Add(
                    new(
                        Long(reader, "ID_CAJA"),
                        Long(reader, "ID_SUCURSAL"),
                        Text(reader, "CODIGO"),
                        Text(reader, "NOMBRE"),
                        Text(reader, "SUCURSAL"),
                        Text(reader, "MONEDA"),
                        Text(reader, "CODIGO_ESTADO"),
                        Bytes(reader, "ROW_VERSION")
                    )
                );

        var sessions = new List<CashSession>();
        if (await reader.NextResultAsync(cancellationToken))
            while (await reader.ReadAsync(cancellationToken))
                sessions.Add(
                    new(
                        Long(reader, "ID_SESION_CAJA"),
                        Long(reader, "ID_CAJA"),
                        Text(reader, "CAJA"),
                        Long(reader, "ID_USUARIO_APERTURA"),
                        Text(reader, "USUARIO"),
                        DateTime(reader, "FECHA_APERTURA_UTC"),
                        Decimal(reader, "IMPORTE_APERTURA"),
                        NullableDateTime(reader, "FECHA_CIERRE_UTC"),
                        NullableDecimal(reader, "IMPORTE_ESPERADO"),
                        NullableDecimal(reader, "IMPORTE_CONTADO"),
                        NullableDecimal(reader, "DIFERENCIA"),
                        NullableText(reader, "OBSERVACION_APERTURA"),
                        NullableText(reader, "OBSERVACION_CIERRE"),
                        Text(reader, "CODIGO_ESTADO"),
                        Bytes(reader, "ROW_VERSION")
                    )
                );

        var sales = new List<ReceivableSale>();
        if (await reader.NextResultAsync(cancellationToken))
            while (await reader.ReadAsync(cancellationToken))
                sales.Add(
                    new(
                        Long(reader, "ID_VENTA"),
                        Long(reader, "ID_CLIENTE"),
                        Text(reader, "CODIGO"),
                        DateTime(reader, "FECHA_VENTA_UTC"),
                        Text(reader, "CLIENTE"),
                        Text(reader, "MONEDA"),
                        Decimal(reader, "TOTAL"),
                        Decimal(reader, "SALDO_PENDIENTE"),
                        Text(reader, "CODIGO_ESTADO")
                    )
                );

        var payments = new List<CustomerPayment>();
        if (await reader.NextResultAsync(cancellationToken))
            while (await reader.ReadAsync(cancellationToken))
                payments.Add(
                    new(
                        Long(reader, "ID_PAGO"),
                        Long(reader, "ID_CLIENTE"),
                        NullableLong(reader, "ID_SESION_CAJA"),
                        Text(reader, "CODIGO"),
                        DateTime(reader, "FECHA_PAGO_UTC"),
                        Text(reader, "CLIENTE"),
                        Text(reader, "MONEDA"),
                        Decimal(reader, "TOTAL"),
                        NullableText(reader, "OBSERVACION"),
                        NullableText(reader, "MOTIVO_REVERSION"),
                        NullableDateTime(reader, "FECHA_REVERSION_UTC"),
                        Text(reader, "CODIGO_ESTADO"),
                        Bytes(reader, "ROW_VERSION")
                    )
                );

        var paymentLines = new List<CustomerPaymentMethod>();
        if (await reader.NextResultAsync(cancellationToken))
            while (await reader.ReadAsync(cancellationToken))
                paymentLines.Add(
                    new(
                        Long(reader, "ID_PAGO_MEDIO"),
                        Long(reader, "ID_PAGO"),
                        Long(reader, "ID_MEDIO_PAGO"),
                        Text(reader, "MEDIO_PAGO"),
                        Decimal(reader, "IMPORTE"),
                        NullableText(reader, "REFERENCIA"),
                        Text(reader, "CODIGO_ESTADO")
                    )
                );

        var applications = new List<PaymentApplication>();
        if (await reader.NextResultAsync(cancellationToken))
            while (await reader.ReadAsync(cancellationToken))
                applications.Add(
                    new(
                        Long(reader, "ID_APLICACION_PAGO_VENTA"),
                        Long(reader, "ID_PAGO"),
                        Long(reader, "ID_VENTA"),
                        Text(reader, "VENTA"),
                        Decimal(reader, "IMPORTE"),
                        Text(reader, "CODIGO_ESTADO")
                    )
                );

        var movements = new List<AccountMovement>();
        if (await reader.NextResultAsync(cancellationToken))
            while (await reader.ReadAsync(cancellationToken))
                movements.Add(
                    new(
                        Long(reader, "ID_MOVIMIENTO_CUENTA"),
                        Long(reader, "ID_CLIENTE"),
                        NullableLong(reader, "ID_VENTA"),
                        NullableLong(reader, "ID_PAGO"),
                        Text(reader, "TIPO_MOVIMIENTO"),
                        Text(reader, "ORIGEN"),
                        DateTime(reader, "FECHA_MOVIMIENTO_UTC"),
                        Text(reader, "MONEDA"),
                        Decimal(reader, "IMPORTE"),
                        Text(reader, "DESCRIPCION"),
                        ReaderGuid(reader, "CORRELACION"),
                        Text(reader, "CODIGO_ESTADO")
                    )
                );

        await reader.DisposeAsync();
        Ensure(output);
        await Audit(
            companyCode,
            userId,
            sessionId,
            "FINANZAS_CONSULTAR",
            "CONSULTAR",
            "SP_FINANZAS_GET",
            started,
            payments.Count + movements.Count,
            cancellationToken
        );
        return new(
            branches,
            clients,
            methods,
            cashBoxes,
            sessions,
            sales,
            payments,
            paymentLines,
            applications,
            movements
        );
    }

    /// <inheritdoc />
    public async Task<long> ExecuteAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        string action,
        IReadOnlyDictionary<string, object?> values,
        CancellationToken cancellationToken
    )
    {
        var started = DateTimeOffset.UtcNow;
        await using var connection = new SqlConnection(databases.GetConnectionString(companyCode));
        await using var command = Command(connection, "SP_FINANZAS_COMMAND");
        Context(command, companyId, userId, sessionId);
        Add(command, "@I_ACCION", action);
        foreach (var value in values)
            Add(command, value.Key, value.Value);
        var id = command.Parameters.Add("@O_ID", SqlDbType.BigInt);
        id.Direction = ParameterDirection.Output;
        var affected = command.Parameters.Add("@O_FILAS_AFECTADAS", SqlDbType.Int);
        affected.Direction = ParameterDirection.Output;
        var output = Outputs(command);
        await connection.OpenAsync(cancellationToken);
        await command.ExecuteNonQueryAsync(cancellationToken);
        Ensure(output);
        var result = id.Value is null or DBNull
            ? 0
            : Convert.ToInt64(id.Value, System.Globalization.CultureInfo.InvariantCulture);
        await Audit(
            companyCode,
            userId,
            sessionId,
            action,
            "MODIFICAR",
            "SP_FINANZAS_COMMAND",
            started,
            affected.Value is int rows ? rows : 1,
            cancellationToken
        );
        return result;
    }

    /// <summary>Registra una operación financiera confirmada con usuario, sesión, SP y duración.</summary>
    /// <param name="company">Empresa cuya base procesó la operación.</param>
    /// <param name="user">Usuario responsable.</param>
    /// <param name="session">Sesión responsable.</param>
    /// <param name="functionality">Funcionalidad auditada.</param>
    /// <param name="action">Acción auditada.</param>
    /// <param name="procedure">Procedimiento ejecutado.</param>
    /// <param name="started">Instante de inicio de la ejecución.</param>
    /// <param name="rows">Filas afectadas o consultadas.</param>
    /// <param name="cancellationToken">Cancelación del registro.</param>
    /// <returns>Tarea que finaliza al persistir la evidencia.</returns>
    private Task Audit(
        string company,
        long user,
        long session,
        string functionality,
        string action,
        string procedure,
        DateTimeOffset started,
        int rows,
        CancellationToken cancellationToken
    ) =>
        auditWriter.WriteAsync(
            new StoredProcedureAuditEntry(
                company,
                "FINANZAS",
                user,
                session,
                functionality,
                action,
                "FINANZAS",
                procedure,
                "{}",
                started,
                DateTimeOffset.UtcNow,
                rows,
                "EXITOSO",
                null,
                null,
                null,
                "OxiTigre.Api",
                Guid.NewGuid()
            ),
            cancellationToken
        );

    /// <summary>Prepara un procedimiento del esquema FINANZAS.</summary>
    /// <param name="connection">Conexión de la empresa seleccionada.</param>
    /// <param name="name">Nombre del procedimiento sin el esquema.</param>
    /// <returns>Comando listo para recibir parámetros.</returns>
    private static SqlCommand Command(SqlConnection connection, string name) =>
        new($"[FINANZAS].[{name}]", connection) { CommandType = CommandType.StoredProcedure };

    /// <summary>Agrega el contexto de empresa, usuario y sesión exigido por SQL.</summary>
    /// <param name="command">Comando que recibe los parámetros.</param>
    /// <param name="company">Empresa autorizada.</param>
    /// <param name="user">Usuario autenticado.</param>
    /// <param name="session">Sesión autenticada.</param>
    private static void Context(SqlCommand command, long company, long user, long session)
    {
        command.Parameters.Add("@I_ID_EMPRESA", SqlDbType.BigInt).Value = company;
        command.Parameters.Add("@S_ID_USUARIO", SqlDbType.BigInt).Value = user;
        command.Parameters.Add("@S_ID_SESION", SqlDbType.BigInt).Value = session;
    }

    /// <summary>Declara el código y mensaje de error funcional devueltos por el SP.</summary>
    /// <param name="command">Comando que recibe los parámetros de salida.</param>
    /// <returns>Referencias a las salidas para comprobar el resultado.</returns>
    private static (SqlParameter Code, SqlParameter Message) Outputs(SqlCommand command)
    {
        var code = command.Parameters.Add("@O_CODIGO_ERROR", SqlDbType.BigInt);
        code.Direction = ParameterDirection.Output;
        var message = command.Parameters.Add("@O_MENSAJE", SqlDbType.NVarChar, 4000);
        message.Direction = ParameterDirection.Output;
        return (code, message);
    }

    /// <summary>Convierte un error funcional informado por SQL en una excepción financiera.</summary>
    /// <param name="output">Salidas leídas al terminar el SP.</param>
    /// <exception cref="FinanceOperationException">El SP devolvió un código de error.</exception>
    private static void Ensure((SqlParameter Code, SqlParameter Message) output)
    {
        if (output.Code.Value is null or DBNull)
            return;
        throw new FinanceOperationException(
            Convert.ToInt64(output.Code.Value, System.Globalization.CultureInfo.InvariantCulture),
            Convert.ToString(
                output.Message.Value,
                System.Globalization.CultureInfo.InvariantCulture
            ) ?? "La operación financiera no pudo completarse."
        );
    }

    /// <summary>Agrega un parámetro del SP consolidado con tipo, tamaño y precisión definidos por su nombre.</summary>
    /// <param name="command">Comando de consulta o cambio financiero.</param>
    /// <param name="name">Nombre SQL que determina el tipo y tamaño.</param>
    /// <param name="value">Valor recibido; nulo se envía como DBNull.</param>
    private static void Add(SqlCommand command, string name, object? value)
    {
        var type = name switch
        {
            "@I_ID" or "@I_ID_SUCURSAL" or "@I_ID_CLIENTE" or "@I_ID_SESION_CAJA" =>
                SqlDbType.BigInt,
            "@I_FECHA_UTC" => SqlDbType.DateTime2,
            "@I_IMPORTE" or "@I_IMPORTE_CONTADO" => SqlDbType.Decimal,
            "@I_AFECTA_EFECTIVO" or "@I_REQUIERE_REFERENCIA" => SqlDbType.Bit,
            "@I_ROW_VERSION" => SqlDbType.Binary,
            "@I_MONEDA" => SqlDbType.Char,
            _ => SqlDbType.NVarChar,
        };
        var size = name switch
        {
            "@I_ROW_VERSION" => 8,
            "@I_MONEDA" => 3,
            "@I_JSON_MEDIOS" or "@I_JSON_APLICACIONES" => -1,
            "@I_NOMBRE" => 100,
            "@I_OBSERVACION" => 500,
            _ => 30,
        };
        var parameter = type is SqlDbType.NVarChar or SqlDbType.Binary or SqlDbType.Char
            ? command.Parameters.Add(name, type, size)
            : command.Parameters.Add(name, type);
        if (type == SqlDbType.Decimal)
        {
            parameter.Precision = 19;
            parameter.Scale = 4;
        }
        parameter.Value = value ?? DBNull.Value;
    }

    /// <summary>Lee un identificador BIGINT obligatorio por nombre de columna.</summary>
    /// <param name="reader">Lector situado sobre una fila.</param>
    /// <param name="name">Nombre de la columna.</param>
    /// <returns>Identificador leído.</returns>
    private static long Long(SqlDataReader reader, string name) =>
        reader.GetInt64(reader.GetOrdinal(name));

    /// <summary>Lee un identificador BIGINT que puede ser nulo.</summary>
    /// <param name="reader">Lector situado sobre una fila.</param>
    /// <param name="name">Nombre de la columna.</param>
    /// <returns>Identificador o nulo.</returns>
    private static long? NullableLong(SqlDataReader reader, string name)
    {
        var i = reader.GetOrdinal(name);
        return reader.IsDBNull(i) ? null : reader.GetInt64(i);
    }

    /// <summary>Lee una cadena obligatoria por nombre de columna.</summary>
    /// <param name="reader">Lector situado sobre una fila.</param>
    /// <param name="name">Nombre de la columna.</param>
    /// <returns>Texto leído.</returns>
    private static string Text(SqlDataReader reader, string name) =>
        reader.GetString(reader.GetOrdinal(name));

    /// <summary>Lee una cadena que puede ser nula.</summary>
    /// <param name="reader">Lector situado sobre una fila.</param>
    /// <param name="name">Nombre de la columna.</param>
    /// <returns>Texto o nulo.</returns>
    private static string? NullableText(SqlDataReader reader, string name)
    {
        var i = reader.GetOrdinal(name);
        return reader.IsDBNull(i) ? null : reader.GetString(i);
    }

    /// <summary>Lee un importe decimal obligatorio.</summary>
    /// <param name="reader">Lector situado sobre una fila.</param>
    /// <param name="name">Nombre de la columna.</param>
    /// <returns>Importe leído.</returns>
    private static decimal Decimal(SqlDataReader reader, string name) =>
        reader.GetDecimal(reader.GetOrdinal(name));

    /// <summary>Lee un importe decimal que puede ser nulo.</summary>
    /// <param name="reader">Lector situado sobre una fila.</param>
    /// <param name="name">Nombre de la columna.</param>
    /// <returns>Importe o nulo.</returns>
    private static decimal? NullableDecimal(SqlDataReader reader, string name)
    {
        var i = reader.GetOrdinal(name);
        return reader.IsDBNull(i) ? null : reader.GetDecimal(i);
    }

    /// <summary>Lee un indicador BIT obligatorio.</summary>
    /// <param name="reader">Lector situado sobre una fila.</param>
    /// <param name="name">Nombre de la columna.</param>
    /// <returns>Valor lógico leído.</returns>
    private static bool Bool(SqlDataReader reader, string name) =>
        reader.GetBoolean(reader.GetOrdinal(name));

    /// <summary>Lee una fecha y hora obligatoria.</summary>
    /// <param name="reader">Lector situado sobre una fila.</param>
    /// <param name="name">Nombre de la columna.</param>
    /// <returns>Fecha y hora leída.</returns>
    private static DateTime DateTime(SqlDataReader reader, string name) =>
        reader.GetDateTime(reader.GetOrdinal(name));

    /// <summary>Lee una fecha y hora que puede ser nula.</summary>
    /// <param name="reader">Lector situado sobre una fila.</param>
    /// <param name="name">Nombre de la columna.</param>
    /// <returns>Fecha y hora o nulo.</returns>
    private static DateTime? NullableDateTime(SqlDataReader reader, string name)
    {
        var i = reader.GetOrdinal(name);
        return reader.IsDBNull(i) ? null : reader.GetDateTime(i);
    }

    /// <summary>Lee la correlación GUID obligatoria de un movimiento.</summary>
    /// <param name="reader">Lector situado sobre una fila.</param>
    /// <param name="name">Nombre de la columna.</param>
    /// <returns>Identificador de correlación leído.</returns>
    private static Guid ReaderGuid(SqlDataReader reader, string name) =>
        reader.GetGuid(reader.GetOrdinal(name));

    /// <summary>Lee la versión binaria usada en el control de concurrencia.</summary>
    /// <param name="reader">Lector situado sobre una fila.</param>
    /// <param name="name">Nombre de la columna.</param>
    /// <returns>Bytes de versión de la fila.</returns>
    private static byte[] Bytes(SqlDataReader reader, string name) =>
        (byte[])reader.GetValue(reader.GetOrdinal(name));
}
