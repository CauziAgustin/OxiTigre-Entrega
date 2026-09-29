/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.DAL.Logistics.SqlLogisticsStore
Archivo: SqlLogisticsStore.cs | Versión: 1.2.0 | Fecha: 2026-08-28 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Persiste Logística mediante una consulta y un comando consolidado, ambos auditados.
Historial: 1.0.0 | 2026-08-26 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Persistencia de transportistas, vehículos y patente normalizada.
Historial: 1.2.0 | 2026-08-28 | FABRICA | Agustin Omar Cauzi | Mapeo de pedidos candidatos, depósitos y activos confirmados.
===============================================================================
*/
using System.Data;
using Microsoft.Data.SqlClient;
using OxiTigre.BLL.Logistics;
using OxiTigre.DAL.MultiCompany;
using OxiTigre.DAL.StoredProcedures;
using OxiTigre.Domain.Logistics;

namespace OxiTigre.DAL.Logistics;

/// <summary>Implementa el circuito logístico sobre SQL Server.</summary>
/// <param name="databases">Registro central de conexiones operativas autorizadas.</param>
/// <param name="auditWriter">Auditor funcional de SP.</param>
public sealed class SqlLogisticsStore(
    CompanyDatabaseRegistry databases,
    IStoredProcedureAuditWriter auditWriter
) : ILogisticsStore
{
    /// <inheritdoc />
    public async Task<LogisticsSnapshot> GetAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        CancellationToken cancellationToken
    )
    {
        var started = DateTimeOffset.UtcNow;
        await using var connection = new SqlConnection(databases.GetConnectionString(companyCode));
        await using var command = Command(connection, "SP_LOGISTICA_GET");
        Context(command, companyId, userId, sessionId);
        var output = Outputs(command);

        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var addresses = new List<LogisticsAddress>();
        while (await reader.ReadAsync(cancellationToken))
        {
            addresses.Add(ReadAddress(reader));
        }

        var requests = new List<LogisticsRequest>();
        if (await reader.NextResultAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                requests.Add(ReadRequest(reader));
            }
        }

        var assets = new List<LogisticsAsset>();
        if (await reader.NextResultAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                assets.Add(ReadAsset(reader));
            }
        }

        var routes = new List<RouteSheet>();
        if (await reader.NextResultAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                routes.Add(ReadRoute(reader));
            }
        }

        var stops = new List<RouteStop>();
        if (await reader.NextResultAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                stops.Add(ReadStop(reader));
            }
        }

        var events = new List<LogisticsEvent>();
        if (await reader.NextResultAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                events.Add(ReadEvent(reader));
            }
        }

        var notifications = new List<LogisticsNotification>();
        if (await reader.NextResultAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                notifications.Add(ReadNotification(reader));
            }
        }

        var clients = new List<LogisticsClient>();
        if (await reader.NextResultAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                clients.Add(
                    new(Long(reader, "ID_CLIENTE"), Text(reader, "CODIGO"), Text(reader, "CLIENTE"))
                );
            }
        }

        var transporters = new List<Transporter>();
        if (await reader.NextResultAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                transporters.Add(ReadTransporter(reader));
            }
        }

        var vehicles = new List<LogisticsVehicle>();
        if (await reader.NextResultAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                vehicles.Add(ReadVehicle(reader));
            }
        }

        var transporterUsers = new List<TransporterUser>();
        if (await reader.NextResultAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                transporterUsers.Add(
                    new(
                        Long(reader, "ID_USUARIO"),
                        Text(reader, "NOMBRE_USUARIO"),
                        Text(reader, "NOMBRE_COMPLETO")
                    )
                );
            }
        }

        var orderCandidates = new List<LogisticsOrderCandidate>();
        if (await reader.NextResultAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                orderCandidates.Add(
                    new(
                        Long(reader, "ID_PEDIDO"),
                        Long(reader, "ID_CLIENTE"),
                        Text(reader, "PEDIDO"),
                        Text(reader, "CLIENTE"),
                        Text(reader, "OPERACION"),
                        Text(reader, "TIPO_SERVICIO"),
                        System.DateOnly.FromDateTime(
                            reader.GetDateTime(reader.GetOrdinal("FECHA_PEDIDO"))
                        ),
                        reader.GetDecimal(reader.GetOrdinal("TOTAL")),
                        NullableDecimal(reader, "SALDO_PENDIENTE"),
                        Text(reader, "ESTADO_PAGO"),
                        reader.GetInt32(reader.GetOrdinal("CANTIDAD_ACTIVOS")),
                        NullableDateOnly(reader, "FECHA_DEVOLUCION_PREVISTA"),
                        Text(reader, "RESUMEN")
                    )
                );
            }
        }

        var warehouses = new List<LogisticsWarehouse>();
        if (await reader.NextResultAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                warehouses.Add(
                    new(Long(reader, "ID_DEPOSITO"), Text(reader, "CODIGO"), Text(reader, "NOMBRE"))
                );
            }
        }

        await reader.DisposeAsync();
        Ensure(output);
        await Audit(
            companyCode,
            userId,
            sessionId,
            "LOGISTICA_CONSULTAR",
            "CONSULTAR",
            "SP_LOGISTICA_GET",
            started,
            addresses.Count + requests.Count + routes.Count,
            cancellationToken
        );

        return new(
            addresses,
            requests,
            assets,
            routes,
            stops,
            events,
            notifications,
            clients,
            transporters,
            vehicles,
            transporterUsers,
            orderCandidates,
            warehouses
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
        await using var command = Command(connection, "SP_LOGISTICA_COMMAND");

        Context(command, companyId, userId, sessionId);
        Add(command, "@I_ACCION", action);
        foreach (var value in values)
        {
            Add(command, value.Key, value.Value);
        }

        var id = command.Parameters.Add("@O_ID", SqlDbType.BigInt);
        id.Direction = ParameterDirection.Output;
        var affectedRows = command.Parameters.Add("@O_FILAS_AFECTADAS", SqlDbType.Int);
        affectedRows.Direction = ParameterDirection.Output;
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
            "SP_LOGISTICA_COMMAND",
            started,
            1,
            cancellationToken
        );

        return result;
    }

    /// <summary>Registra una operación logística confirmada con usuario, sesión, SP y duración.</summary>
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
                "LOGISTICA",
                user,
                session,
                functionality,
                action,
                "LOGISTICA",
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

    /// <summary>Prepara un procedimiento del esquema LOGISTICA.</summary>
    /// <param name="connection">Conexión de la empresa seleccionada.</param>
    /// <param name="name">Nombre del procedimiento sin el esquema.</param>
    /// <returns>Comando listo para recibir parámetros.</returns>
    private static SqlCommand Command(SqlConnection connection, string name) =>
        new($"[LOGISTICA].[{name}]", connection) { CommandType = CommandType.StoredProcedure };

    /// <summary>Agrega la identidad de empresa, usuario y sesión exigida por SQL.</summary>
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
    /// <param name="command">Comando que recibe las salidas.</param>
    /// <returns>Referencias a los dos parámetros para comprobar el resultado.</returns>
    private static (SqlParameter Code, SqlParameter Message) Outputs(SqlCommand command)
    {
        var code = command.Parameters.Add("@O_CODIGO_ERROR", SqlDbType.BigInt);
        code.Direction = ParameterDirection.Output;

        var message = command.Parameters.Add("@O_MENSAJE", SqlDbType.NVarChar, 4000);
        message.Direction = ParameterDirection.Output;

        return (code, message);
    }

    /// <summary>Convierte un error funcional informado por SQL en una excepción logística.</summary>
    /// <param name="output">Salidas leídas al terminar el SP.</param>
    /// <exception cref="LogisticsOperationException">El SP devolvió un código de error.</exception>
    private static void Ensure((SqlParameter Code, SqlParameter Message) output)
    {
        if (output.Code.Value is null or DBNull)
        {
            return;
        }

        throw new LogisticsOperationException(
            Convert.ToInt64(output.Code.Value, System.Globalization.CultureInfo.InvariantCulture),
            Convert.ToString(
                output.Message.Value,
                System.Globalization.CultureInfo.InvariantCulture
            ) ?? "La operación logística no pudo completarse."
        );
    }

    /// <summary>Agrega un parámetro del SP consolidado con tipo, tamaño y conversión de fecha definidos por su nombre.</summary>
    /// <param name="command">Comando de consulta o cambio logístico.</param>
    /// <param name="name">Nombre SQL que determina tipo y tamaño.</param>
    /// <param name="value">Valor recibido; nulo se envía como DBNull.</param>
    private static void Add(SqlCommand command, string name, object? value)
    {
        var type = name switch
        {
            "@I_ID"
            or "@I_ID_CLIENTE"
            or "@I_ID_DIRECCION"
            or "@I_ID_PEDIDO"
            or "@I_ID_DEPOSITO"
            or "@I_ID_USUARIO"
            or "@I_ID_TRANSPORTISTA"
            or "@I_ID_VEHICULO" => SqlDbType.BigInt,
            "@I_FECHA"
            or "@I_VENCIMIENTO"
            or "@I_VENCIMIENTO_SEGURO"
            or "@I_VENCIMIENTO_REVISION" => SqlDbType.Date,
            "@I_HORA_DESDE" or "@I_HORA_HASTA" => SqlDbType.Time,
            "@I_ANIO" => SqlDbType.SmallInt,
            "@I_CAPACIDAD" => SqlDbType.Decimal,
            "@I_ROW_VERSION" => SqlDbType.Binary,
            _ => SqlDbType.NVarChar,
        };
        var size = name switch
        {
            "@I_ROW_VERSION" => 8,
            "@I_JSON" => -1,
            "@I_DOMICILIO" => 300,
            "@I_CORREO" => 254,
            "@I_INSTRUCCIONES" or "@I_OBSERVACION" => 1000,
            "@I_NOMBRE" or "@I_CONTACTO" or "@I_CHOFER" => 200,
            "@I_MARCA" or "@I_MODELO" => 80,
            "@I_LICENCIA" or "@I_POLIZA" => 100,
            "@I_LOCALIDAD" or "@I_PROVINCIA" => 100,
            "@I_TELEFONO" => 50,
            _ => 30,
        };
        var parameter = type is SqlDbType.NVarChar or SqlDbType.Binary
            ? command.Parameters.Add(name, type, size)
            : command.Parameters.Add(name, type);

        if (type == SqlDbType.Decimal)
        {
            parameter.Precision = 19;
            parameter.Scale = 4;
        }

        parameter.Value = value switch
        {
            null => DBNull.Value,
            DateOnly date => date.ToDateTime(TimeOnly.MinValue),
            TimeOnly time => time.ToTimeSpan(),
            _ => value,
        };
    }

    /// <summary>Reconstruye un domicilio operativo con contacto, ventana horaria e instrucciones.</summary>
    /// <param name="reader">Fila del resultado de domicilios.</param>
    /// <returns>Domicilio versionado disponible para planificar.</returns>
    private static LogisticsAddress ReadAddress(SqlDataReader reader) =>
        new(
            Long(reader, "ID_DIRECCION"),
            Long(reader, "ID_CLIENTE"),
            Text(reader, "CODIGO"),
            Text(reader, "NOMBRE"),
            Text(reader, "CLIENTE"),
            Text(reader, "DOMICILIO"),
            NullableText(reader, "LOCALIDAD"),
            NullableText(reader, "PROVINCIA"),
            NullableText(reader, "CODIGO_POSTAL"),
            NullableDecimal(reader, "LATITUD"),
            NullableDecimal(reader, "LONGITUD"),
            Text(reader, "CONTACTO"),
            Text(reader, "TELEFONO"),
            NullableText(reader, "CORREO"),
            NullableTime(reader, "HORA_DESDE"),
            NullableTime(reader, "HORA_HASTA"),
            reader.GetInt16(reader.GetOrdinal("MINUTOS_SERVICIO")),
            NullableText(reader, "RESTRICCIONES"),
            Text(reader, "INSTRUCCIONES"),
            Text(reader, "CODIGO_ESTADO"),
            Bytes(reader, "ROW_VERSION")
        );

    /// <summary>Reconstruye una solicitud logística y su prioridad, fecha y destino.</summary>
    /// <param name="reader">Fila del resultado de solicitudes.</param>
    /// <returns>Solicitud vinculable a una hoja de ruta.</returns>
    private static LogisticsRequest ReadRequest(SqlDataReader reader) =>
        new(
            Long(reader, "ID_SOLICITUD"),
            Long(reader, "ID_CLIENTE"),
            Long(reader, "ID_DIRECCION"),
            NullableLong(reader, "ID_PEDIDO"),
            Text(reader, "CODIGO"),
            Text(reader, "CLIENTE"),
            Text(reader, "DOMICILIO"),
            Text(reader, "TIPO_SERVICIO"),
            Text(reader, "PRIORIDAD"),
            DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("FECHA_SOLICITADA"))),
            NullableTime(reader, "HORA_DESDE"),
            NullableTime(reader, "HORA_HASTA"),
            Text(reader, "INSTRUCCIONES"),
            NullableText(reader, "OBSERVACION"),
            Text(reader, "CODIGO_ESTADO"),
            Bytes(reader, "ROW_VERSION"),
            NullableLong(reader, "ID_DEPOSITO_DESTINO"),
            NullableText(reader, "DEPOSITO_DESTINO")
        );

    /// <summary>Reconstruye el activo asociado a una solicitud, incluida su custodia y devolución prevista.</summary>
    /// <param name="reader">Fila del resultado de activos de solicitudes.</param>
    /// <returns>Activo y condiciones registradas para el traslado.</returns>
    private static LogisticsAsset ReadAsset(SqlDataReader reader) =>
        new(
            Long(reader, "ID_SOLICITUD_ACTIVO"),
            Long(reader, "ID_SOLICITUD"),
            NullableLong(reader, "ID_ACTIVO"),
            Text(reader, "PROPIETARIO"),
            Text(reader, "ROL"),
            Text(reader, "NUMERO_SERIE"),
            NullableText(reader, "PRODUCTO"),
            NullableDecimal(reader, "CANTIDAD_CONTENIDO"),
            NullableText(reader, "UNIDAD"),
            Text(reader, "CONDICION"),
            NullableText(reader, "OBSERVACION"),
            Text(reader, "CODIGO_ESTADO"),
            NullableDateOnly(reader, "FECHA_DEVOLUCION_PREVISTA"),
            NullableLong(reader, "ID_PRESTAMO")
        );

    /// <summary>Reconstruye la hoja de ruta con asignación, chofer, patente, estado y horarios.</summary>
    /// <param name="reader">Fila del resultado de hojas de ruta.</param>
    /// <returns>Hoja con patente formateada para exhibición.</returns>
    private static RouteSheet ReadRoute(SqlDataReader reader) =>
        new(
            Long(reader, "ID_HOJA_RUTA"),
            NullableLong(reader, "ID_TRANSPORTISTA"),
            NullableLong(reader, "ID_VEHICULO"),
            Text(reader, "CODIGO"),
            DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("FECHA_RUTA"))),
            Text(reader, "TIPO_RUTA"),
            Text(reader, "TIPO_ASIGNACION"),
            NullableText(reader, "CHOFER"),
            ArgentineLicensePlate.Format(NullableText(reader, "PATENTE")),
            Text(reader, "OBSERVACION"),
            NullableDateTime(reader, "FECHA_ASIGNACION_UTC"),
            NullableDateTime(reader, "FECHA_SALIDA_UTC"),
            NullableDateTime(reader, "FECHA_CIERRE_UTC"),
            Text(reader, "CODIGO_ESTADO"),
            Bytes(reader, "ROW_VERSION")
        );

    /// <summary>Reconstruye una parada y la evidencia de llegada, salida y resultado.</summary>
    /// <param name="reader">Fila del resultado de paradas.</param>
    /// <returns>Parada ordenada dentro de su hoja.</returns>
    private static RouteStop ReadStop(SqlDataReader reader) =>
        new(
            Long(reader, "ID_PARADA"),
            Long(reader, "ID_HOJA_RUTA"),
            Long(reader, "ID_SOLICITUD"),
            reader.GetInt16(reader.GetOrdinal("ORDEN")),
            Text(reader, "CLIENTE"),
            Text(reader, "DOMICILIO"),
            Text(reader, "CONTACTO"),
            Text(reader, "TELEFONO"),
            NullableTime(reader, "HORA_DESDE"),
            NullableTime(reader, "HORA_HASTA"),
            Text(reader, "PRIORIDAD"),
            Text(reader, "INSTRUCCIONES"),
            NullableDateTime(reader, "FECHA_LLEGADA_UTC"),
            NullableDateTime(reader, "FECHA_SALIDA_UTC"),
            NullableText(reader, "RESULTADO"),
            NullableText(reader, "OBSERVACION_RESULTADO"),
            Text(reader, "CODIGO_ESTADO"),
            Bytes(reader, "ROW_VERSION")
        );

    /// <summary>Reconstruye un evento histórico con transición de estado y correlación.</summary>
    /// <param name="reader">Fila del resultado de eventos.</param>
    /// <returns>Evento auditado de solicitud, hoja o parada.</returns>
    private static LogisticsEvent ReadEvent(SqlDataReader reader) =>
        new(
            Long(reader, "ID_EVENTO"),
            NullableLong(reader, "ID_SOLICITUD"),
            NullableLong(reader, "ID_HOJA_RUTA"),
            NullableLong(reader, "ID_PARADA"),
            Text(reader, "TIPO_EVENTO"),
            NullableText(reader, "ESTADO_ANTERIOR"),
            NullableText(reader, "ESTADO_NUEVO"),
            Text(reader, "OBSERVACION"),
            Long(reader, "ID_USUARIO"),
            Long(reader, "ID_SESION"),
            reader.GetGuid(reader.GetOrdinal("CORRELACION")),
            reader.GetDateTime(reader.GetOrdinal("FECHA_UTC"))
        );

    /// <summary>Reconstruye un aviso con canal, destinatario, intentos y estado de envío.</summary>
    /// <param name="reader">Fila del resultado de avisos.</param>
    /// <returns>Aviso logístico persistido.</returns>
    private static LogisticsNotification ReadNotification(SqlDataReader reader) =>
        new(
            Long(reader, "ID_NOTIFICACION"),
            NullableLong(reader, "ID_SOLICITUD"),
            NullableLong(reader, "ID_HOJA_RUTA"),
            Text(reader, "TIPO_EVENTO"),
            Text(reader, "CANAL"),
            Text(reader, "DESTINATARIO"),
            Text(reader, "MENSAJE"),
            reader.GetInt16(reader.GetOrdinal("INTENTOS")),
            Text(reader, "CODIGO_ESTADO"),
            reader.GetDateTime(reader.GetOrdinal("FECHA_ALTA_UTC")),
            NullableDateTime(reader, "FECHA_ENVIO_UTC")
        );

    /// <summary>Reconstruye el perfil de transportista vinculado a un usuario y su licencia.</summary>
    /// <param name="reader">Fila del resultado de transportistas.</param>
    /// <returns>Transportista con vigencia y estado registrados.</returns>
    private static Transporter ReadTransporter(SqlDataReader reader) =>
        new(
            Long(reader, "ID_TRANSPORTISTA"),
            Long(reader, "ID_USUARIO"),
            Text(reader, "CODIGO"),
            Text(reader, "NOMBRE_USUARIO"),
            Text(reader, "NOMBRE_COMPLETO"),
            Text(reader, "TIPO_VINCULO"),
            NullableText(reader, "DOCUMENTO"),
            NullableText(reader, "TELEFONO"),
            Text(reader, "LICENCIA"),
            NullableText(reader, "CATEGORIA_LICENCIA"),
            NullableDateOnly(reader, "VENCIMIENTO_LICENCIA"),
            NullableText(reader, "OBSERVACION"),
            Text(reader, "CODIGO_ESTADO"),
            Bytes(reader, "ROW_VERSION")
        );

    /// <summary>Reconstruye un vehículo con propiedad, patente, capacidad y vencimientos.</summary>
    /// <param name="reader">Fila del resultado de vehículos.</param>
    /// <returns>Vehículo con patente argentina formateada.</returns>
    private static LogisticsVehicle ReadVehicle(SqlDataReader reader) =>
        new(
            Long(reader, "ID_VEHICULO"),
            NullableLong(reader, "ID_TRANSPORTISTA_PROPIETARIO"),
            Text(reader, "CODIGO"),
            ArgentineLicensePlate.Format(Text(reader, "PATENTE"))!,
            Text(reader, "TIPO_VEHICULO"),
            Text(reader, "TIPO_PROPIEDAD"),
            NullableText(reader, "PROPIETARIO"),
            NullableText(reader, "MARCA"),
            NullableText(reader, "MODELO"),
            NullableShort(reader, "ANIO"),
            NullableDecimal(reader, "CAPACIDAD_CARGA_KG"),
            NullableText(reader, "POLIZA_SEGURO"),
            NullableDateOnly(reader, "VENCIMIENTO_SEGURO"),
            NullableDateOnly(reader, "VENCIMIENTO_REVISION"),
            NullableText(reader, "OBSERVACION"),
            Text(reader, "CODIGO_ESTADO"),
            Bytes(reader, "ROW_VERSION")
        );

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
        var index = reader.GetOrdinal(name);
        return reader.IsDBNull(index) ? null : reader.GetInt64(index);
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
        var index = reader.GetOrdinal(name);
        return reader.IsDBNull(index) ? null : reader.GetString(index);
    }

    /// <summary>Lee un decimal nullable, como coordenada o capacidad.</summary>
    /// <param name="reader">Lector situado sobre una fila.</param>
    /// <param name="name">Nombre de la columna.</param>
    /// <returns>Decimal leído o nulo.</returns>
    private static decimal? NullableDecimal(SqlDataReader reader, string name)
    {
        var index = reader.GetOrdinal(name);
        return reader.IsDBNull(index) ? null : reader.GetDecimal(index);
    }

    /// <summary>Convierte un horario SQL nullable en TimeOnly.</summary>
    /// <param name="reader">Lector situado sobre una fila.</param>
    /// <param name="name">Nombre de la columna.</param>
    /// <returns>Hora de la ventana operativa o nulo.</returns>
    private static TimeOnly? NullableTime(SqlDataReader reader, string name)
    {
        var index = reader.GetOrdinal(name);
        return reader.IsDBNull(index) ? null : TimeOnly.FromTimeSpan(reader.GetTimeSpan(index));
    }

    /// <summary>Lee una fecha y hora que puede ser nula.</summary>
    /// <param name="reader">Lector situado sobre una fila.</param>
    /// <param name="name">Nombre de la columna.</param>
    /// <returns>Fecha y hora o nulo.</returns>
    private static DateTime? NullableDateTime(SqlDataReader reader, string name)
    {
        var index = reader.GetOrdinal(name);
        return reader.IsDBNull(index) ? null : reader.GetDateTime(index);
    }

    /// <summary>Convierte una fecha SQL nullable en DateOnly.</summary>
    /// <param name="reader">Lector situado sobre una fila.</param>
    /// <param name="name">Nombre de la columna.</param>
    /// <returns>Fecha sin hora o nulo.</returns>
    private static DateOnly? NullableDateOnly(SqlDataReader reader, string name)
    {
        var index = reader.GetOrdinal(name);
        return reader.IsDBNull(index) ? null : DateOnly.FromDateTime(reader.GetDateTime(index));
    }

    /// <summary>Lee un entero SMALLINT que puede ser nulo.</summary>
    /// <param name="reader">Lector situado sobre una fila.</param>
    /// <param name="name">Nombre de la columna.</param>
    /// <returns>Valor corto o nulo.</returns>
    private static short? NullableShort(SqlDataReader reader, string name)
    {
        var index = reader.GetOrdinal(name);
        return reader.IsDBNull(index) ? null : reader.GetInt16(index);
    }

    /// <summary>Lee la versión binaria utilizada para control de concurrencia.</summary>
    /// <param name="reader">Lector situado sobre una fila.</param>
    /// <param name="name">Nombre de la columna.</param>
    /// <returns>Bytes de versión de la fila.</returns>
    private static byte[] Bytes(SqlDataReader reader, string name) =>
        (byte[])reader.GetValue(reader.GetOrdinal(name));
}
