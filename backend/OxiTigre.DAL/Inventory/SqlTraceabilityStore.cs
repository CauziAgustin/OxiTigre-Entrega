/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.DAL.Inventory.SqlTraceabilityStore
Archivo: SqlTraceabilityStore.cs | Versión: 1.0.0 | Fecha: 2026-08-24 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Persiste documentos industriales mediante SP auditados y contexto de sesión.
Historial: 1.0.0 | 2026-08-24 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using OxiTigre.BLL.Inventory;
using OxiTigre.DAL.MultiCompany;
using OxiTigre.DAL.StoredProcedures;

namespace OxiTigre.DAL.Inventory;

/// <summary>Implementa la trazabilidad transversal sobre SQL Server.</summary>
public sealed class SqlTraceabilityStore(
    CompanyDatabaseRegistry databases,
    IStoredProcedureAuditWriter auditWriter
) : ITraceabilityStore
{
    /// <inheritdoc />
    public async Task<TraceabilitySnapshot> GetAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        CancellationToken cancellationToken
    )
    {
        var started = DateTimeOffset.UtcNow;
        await using var connection = new SqlConnection(databases.GetConnectionString(companyCode));
        await using var command = Command(connection, "SP_TRAZABILIDAD_GET");
        Context(command, companyId, userId, sessionId);
        var outputs = Outputs(command);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var lots = new List<TraceLot>();
        while (await reader.ReadAsync(cancellationToken))
            lots.Add(
                new(
                    Long(reader, "ID_LOTE"),
                    Long(reader, "ID_PRODUCTO"),
                    Text(reader, "CODIGO"),
                    NullableText(reader, "CODIGO_LOTE_PROVEEDOR"),
                    Text(reader, "PRODUCTO"),
                    Long(reader, "ID_DEPOSITO"),
                    Text(reader, "DEPOSITO"),
                    Decimal(reader, "CANTIDAD"),
                    NullableDateOnly(reader, "FECHA_VENCIMIENTO"),
                    Text(reader, "CODIGO_ESTADO")
                )
            );
        var assets = new List<TraceAsset>();
        if (await reader.NextResultAsync(cancellationToken))
            while (await reader.ReadAsync(cancellationToken))
                assets.Add(
                    new(
                        Long(reader, "ID_ACTIVO"),
                        Long(reader, "ID_PRODUCTO"),
                        NullableLong(reader, "ID_CLIENTE_PROPIETARIO"),
                        Text(reader, "CODIGO"),
                        Text(reader, "NUMERO_SERIE"),
                        Text(reader, "TIPO_ACTIVO"),
                        Text(reader, "PRODUCTO"),
                        NullableLong(reader, "ID_DEPOSITO"),
                        NullableText(reader, "DEPOSITO"),
                        NullableLong(reader, "ID_UBICACION"),
                        NullableText(reader, "UBICACION"),
                        Text(reader, "PROPIETARIO_NOMBRE"),
                        Text(reader, "CONDICION_ACTUAL"),
                        Text(reader, "CODIGO_ESTADO"),
                        NullableDecimal(reader, "CAPACIDAD"),
                        NullableText(reader, "UNIDAD_CAPACIDAD"),
                        NullableLong(reader, "ID_PRODUCTO_CONTENIDO"),
                        NullableText(reader, "PRODUCTO_CONTENIDO"),
                        NullableLong(reader, "ID_LOTE_CONTENIDO"),
                        NullableText(reader, "LOTE_CONTENIDO"),
                        NullableDecimal(reader, "CANTIDAD_CONTENIDO"),
                        NullableText(reader, "UNIDAD_CONTENIDO"),
                        Bytes(reader, "ROW_VERSION")
                    )
                );
        var measurements = new List<AssetMeasurement>();
        if (await reader.NextResultAsync(cancellationToken))
            while (await reader.ReadAsync(cancellationToken))
                measurements.Add(
                    new(
                        Long(reader, "ID_MEDICION_ACTIVO"),
                        Long(reader, "ID_ACTIVO"),
                        Text(reader, "ACTIVO"),
                        Date(reader, "FECHA_MEDICION_UTC"),
                        Text(reader, "TIPO_MEDICION"),
                        Decimal(reader, "VALOR"),
                        Text(reader, "UNIDAD"),
                        Text(reader, "METODO_MEDICION"),
                        Text(reader, "ORIGEN_MEDICION"),
                        Bool(reader, "ES_ESTIMADA"),
                        NullableText(reader, "REFERENCIA_DISPOSITIVO"),
                        NullableText(reader, "OBSERVACION")
                    )
                );
        var transformations = new List<TransformationLine>();
        if (await reader.NextResultAsync(cancellationToken))
            while (await reader.ReadAsync(cancellationToken))
                transformations.Add(
                    new(
                        Long(reader, "ID_TRANSFORMACION"),
                        Text(reader, "CODIGO"),
                        Date(reader, "FECHA_TRANSFORMACION_UTC"),
                        Long(reader, "ID_LOTE_ORIGEN"),
                        Text(reader, "LOTE_ORIGEN"),
                        NullableLong(reader, "ID_ACTIVO_ORIGEN"),
                        NullableText(reader, "ACTIVO_ORIGEN"),
                        Text(reader, "PRODUCTO"),
                        Decimal(reader, "CANTIDAD_ORIGEN"),
                        Decimal(reader, "CANTIDAD_MERMA"),
                        Text(reader, "METODO_MEDICION"),
                        NullableText(reader, "MOTIVO_MERMA"),
                        Long(reader, "ID_ACTIVO_DESTINO"),
                        Text(reader, "ACTIVO_DESTINO"),
                        Decimal(reader, "CANTIDAD_CARGADA")
                    )
                );
        var incidents = new List<IndustrialIncident>();
        if (await reader.NextResultAsync(cancellationToken))
            while (await reader.ReadAsync(cancellationToken))
                incidents.Add(
                    new(
                        Long(reader, "ID_INCIDENTE"),
                        Text(reader, "CODIGO"),
                        Date(reader, "FECHA_INCIDENTE_UTC"),
                        Text(reader, "TIPO_INCIDENTE"),
                        Text(reader, "PRODUCTO"),
                        Text(reader, "DEPOSITO"),
                        NullableLong(reader, "ID_ACTIVO"),
                        NullableText(reader, "ACTIVO"),
                        Decimal(reader, "CANTIDAD_PERDIDA"),
                        Bool(reader, "ES_ESTIMADA"),
                        Text(reader, "CAUSA"),
                        NullableText(reader, "ACCION_TOMADA"),
                        Text(reader, "CODIGO_ESTADO")
                    )
                );
        var maintenances = new List<AssetMaintenance>();
        if (await reader.NextResultAsync(cancellationToken))
            while (await reader.ReadAsync(cancellationToken))
                maintenances.Add(
                    new(
                        Long(reader, "ID_MANTENIMIENTO"),
                        Text(reader, "CODIGO"),
                        Long(reader, "ID_ACTIVO"),
                        Text(reader, "ACTIVO"),
                        Text(reader, "TIPO_MANTENIMIENTO"),
                        Date(reader, "FECHA_INICIO_UTC"),
                        NullableDate(reader, "FECHA_FIN_UTC"),
                        Text(reader, "DESCRIPCION_TRABAJO"),
                        NullableText(reader, "RESULTADO"),
                        NullableDecimal(reader, "COSTO"),
                        NullableDateOnly(reader, "FECHA_PROXIMA_REVISION"),
                        Text(reader, "CODIGO_ESTADO"),
                        Bytes(reader, "ROW_VERSION")
                    )
                );
        var loans = new List<AssetLoanLine>();
        if (await reader.NextResultAsync(cancellationToken))
            while (await reader.ReadAsync(cancellationToken))
                loans.Add(
                    new(
                        Long(reader, "ID_PRESTAMO"),
                        Text(reader, "CODIGO"),
                        Text(reader, "DESTINO"),
                        Text(reader, "MODALIDAD_ENTREGA"),
                        Date(reader, "FECHA_SALIDA_UTC"),
                        NullableDate(reader, "FECHA_DEVOLUCION_PREVISTA"),
                        NullableDate(reader, "FECHA_DEVOLUCION_REAL_UTC"),
                        Text(reader, "CODIGO_ESTADO"),
                        Bytes(reader, "ROW_VERSION"),
                        Long(reader, "ID_PRESTAMO_DETALLE"),
                        Long(reader, "ID_ACTIVO"),
                        Text(reader, "ACTIVO"),
                        NullableDecimal(reader, "CANTIDAD_SALIDA"),
                        Text(reader, "CONDICION_SALIDA"),
                        NullableDecimal(reader, "CANTIDAD_DEVUELTA"),
                        NullableText(reader, "CONDICION_DEVOLUCION")
                    )
                );
        var events = new List<AssetEvent>();
        if (await reader.NextResultAsync(cancellationToken))
            while (await reader.ReadAsync(cancellationToken))
                events.Add(
                    new(
                        Long(reader, "ID_ACTIVO_EVENTO"),
                        Long(reader, "ID_ACTIVO"),
                        Text(reader, "ACTIVO"),
                        Text(reader, "TIPO_EVENTO"),
                        Date(reader, "FECHA_EVENTO_UTC"),
                        NullableText(reader, "CODIGO_ESTADO_ANTES"),
                        Text(reader, "CODIGO_ESTADO_DESPUES"),
                        NullableText(reader, "CONDICION_ANTES"),
                        Text(reader, "CONDICION_DESPUES"),
                        NullableDecimal(reader, "CANTIDAD_ANTES"),
                        NullableDecimal(reader, "CANTIDAD_DESPUES"),
                        NullableText(reader, "OBSERVACION"),
                        reader.GetGuid(reader.GetOrdinal("ID_CORRELACION")),
                        Long(reader, "ID_USUARIO_ALTA")
                    )
                );
        var branches = new List<TraceDestination>();
        if (await reader.NextResultAsync(cancellationToken))
            while (await reader.ReadAsync(cancellationToken))
                branches.Add(
                    new(Long(reader, "ID_DESTINO"), Text(reader, "CODIGO"), Text(reader, "NOMBRE"))
                );
        var clients = new List<TraceDestination>();
        if (await reader.NextResultAsync(cancellationToken))
            while (await reader.ReadAsync(cancellationToken))
                clients.Add(
                    new(Long(reader, "ID_DESTINO"), Text(reader, "CODIGO"), Text(reader, "NOMBRE"))
                );
        await reader.DisposeAsync();
        Ensure(outputs);
        await Audit(
            companyCode,
            userId,
            sessionId,
            "TRAZABILIDAD_CONSULTAR",
            "CONSULTAR",
            "SP_TRAZABILIDAD_GET",
            started,
            lots.Count + assets.Count + events.Count,
            cancellationToken
        );
        return new(
            lots,
            assets,
            measurements,
            transformations,
            incidents,
            maintenances,
            loans,
            events,
            branches,
            clients
        );
    }

    /// <inheritdoc />
    public Task<long> SaveClientAssetAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        ClientAssetChange change,
        CancellationToken cancellationToken
    ) =>
        Save(
            companyId,
            companyCode,
            userId,
            sessionId,
            "ACTIVO_CLIENTE_REGISTRAR",
            "SP_ACTIVO_CLIENTE_SAVE",
            "@O_ID_ACTIVO",
            command =>
            {
                Id(command, "@I_ID_CLIENTE", change.ClientId);
                Id(command, "@I_ID_PRODUCTO", change.ProductId);
                Id(command, "@I_ID_DEPOSITO", change.WarehouseId);
                Text(command, "@I_NUMERO_SERIE", 100, change.SerialNumber);
                Text(command, "@I_TIPO_ACTIVO", 30, change.AssetType);
                NullableMoney(command, "@I_CAPACIDAD", change.Capacity);
                Text(command, "@I_UNIDAD_CAPACIDAD", 20, change.CapacityUnit);
                Text(command, "@I_CONDICION", 30, change.ConditionCode);
                Bit(command, "@I_EN_CUSTODIA", change.IsInCustody);
                Text(command, "@I_OBSERVACION", 500, change.Observation);
            },
            cancellationToken
        );

    /// <inheritdoc />
    public Task<long> ChangeClientAssetCustodyAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        ClientAssetCustodyChange change,
        CancellationToken cancellationToken
    ) =>
        Save(
            companyId,
            companyCode,
            userId,
            sessionId,
            "ACTIVO_CLIENTE_CUSTODIA",
            "SP_ACTIVO_CLIENTE_CUSTODIA",
            "@O_ID_ACTIVO",
            command =>
            {
                Id(command, "@I_ID_ACTIVO", change.AssetId);
                Text(command, "@I_ACCION", 30, change.Action);
                Id(command, "@I_ID_DEPOSITO", change.WarehouseId);
                Text(command, "@I_OBSERVACION", 500, change.Observation);
                Version(command, change.RowVersion);
            },
            cancellationToken
        );

    /// <inheritdoc />
    public Task<long> CreateMeasurementAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        AssetMeasurementChange change,
        CancellationToken cancellationToken
    ) =>
        Save(
            companyId,
            companyCode,
            userId,
            sessionId,
            "MEDICION_REGISTRAR",
            "SP_MEDICION_ACTIVO_CREATE",
            "@O_ID_MEDICION_ACTIVO",
            c =>
            {
                Id(c, "@I_ID_ACTIVO", change.AssetId);
                Date(c, "@I_FECHA_MEDICION_UTC", change.MeasurementDateUtc);
                Text(c, "@I_TIPO_MEDICION", 30, change.MeasurementType);
                Money(c, "@I_VALOR", change.Value);
                Text(c, "@I_UNIDAD", 20, change.Unit);
                Text(c, "@I_METODO_MEDICION", 30, change.Method);
                Text(c, "@I_ORIGEN_MEDICION", 30, change.Source);
                Bit(c, "@I_ES_ESTIMADA", change.IsEstimated);
                Text(c, "@I_REFERENCIA_DISPOSITIVO", 100, change.DeviceReference);
                Text(c, "@I_OBSERVACION", 500, change.Observation);
            },
            cancellationToken
        );

    /// <inheritdoc />
    public Task<long> CreateTransformationAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        TransformationChange change,
        CancellationToken cancellationToken
    ) =>
        Save(
            companyId,
            companyCode,
            userId,
            sessionId,
            "FRACCIONAMIENTO_CONFIRMAR",
            "SP_TRANSFORMACION_CREATE",
            "@O_ID_TRANSFORMACION",
            c =>
            {
                Id(c, "@I_ID_LOTE_ORIGEN", change.SourceLotId);
                Id(c, "@I_ID_ACTIVO_ORIGEN", change.SourceAssetId);
                Id(c, "@I_ID_PRODUCTO_CONTENIDO", change.ContentProductId);
                Date(c, "@I_FECHA_TRANSFORMACION_UTC", change.TransformationDateUtc);
                Money(c, "@I_CANTIDAD_ORIGEN", change.SourceQuantity);
                Money(c, "@I_CANTIDAD_MERMA", change.LossQuantity);
                Text(c, "@I_METODO_MEDICION", 30, change.Method);
                Text(c, "@I_MOTIVO_MERMA", 500, change.LossReason);
                Text(c, "@I_OBSERVACION", 1000, change.Observation);
                Text(c, "@I_DESTINOS_JSON", -1, JsonSerializer.Serialize(change.Destinations));
            },
            cancellationToken
        );

    /// <inheritdoc />
    public Task<long> CreateIncidentAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        IncidentChange change,
        CancellationToken cancellationToken
    ) =>
        Save(
            companyId,
            companyCode,
            userId,
            sessionId,
            "INCIDENTE_CONFIRMAR",
            "SP_INCIDENTE_CREATE",
            "@O_ID_INCIDENTE",
            c =>
            {
                Id(c, "@I_ID_PRODUCTO", change.ProductId);
                Id(c, "@I_ID_DEPOSITO", change.WarehouseId);
                Id(c, "@I_ID_ACTIVO", change.AssetId);
                Id(c, "@I_ID_LOTE", change.LotId);
                Text(c, "@I_TIPO_INCIDENTE", 30, change.IncidentType);
                Date(c, "@I_FECHA_INCIDENTE_UTC", change.IncidentDateUtc);
                NullableMoney(c, "@I_CANTIDAD_ANTES", change.QuantityBefore);
                Money(c, "@I_CANTIDAD_PERDIDA", change.LossQuantity);
                NullableMoney(c, "@I_CANTIDAD_DESPUES", change.QuantityAfter);
                Bit(c, "@I_ES_ESTIMADA", change.IsEstimated);
                Text(c, "@I_METODO_MEDICION", 30, change.MeasurementMethod);
                Text(c, "@I_CAUSA", 1000, change.Cause);
                Text(c, "@I_ACCION_TOMADA", 1000, change.ActionTaken);
                Text(c, "@I_EVIDENCIA_REFERENCIA", 500, change.EvidenceReference);
            },
            cancellationToken
        );

    /// <inheritdoc />
    public Task<long> CreateMaintenanceAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        MaintenanceChange change,
        CancellationToken cancellationToken
    ) =>
        Save(
            companyId,
            companyCode,
            userId,
            sessionId,
            "MANTENIMIENTO_INICIAR",
            "SP_MANTENIMIENTO_CREATE",
            "@O_ID_MANTENIMIENTO",
            c =>
            {
                Id(c, "@I_ID_ACTIVO", change.AssetId);
                Id(c, "@I_ID_INCIDENTE", change.IncidentId);
                Id(c, "@I_ID_PROVEEDOR", change.SupplierId);
                Text(c, "@I_TIPO_MANTENIMIENTO", 30, change.MaintenanceType);
                Date(c, "@I_FECHA_INICIO_UTC", change.StartDateUtc);
                Text(c, "@I_DESCRIPCION_TRABAJO", 1000, change.WorkDescription);
                NullableMoney(c, "@I_COSTO", change.Cost);
            },
            cancellationToken
        );

    /// <inheritdoc />
    public Task CompleteMaintenanceAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        MaintenanceCompletion change,
        CancellationToken cancellationToken
    ) =>
        Execute(
            companyId,
            companyCode,
            userId,
            sessionId,
            "MANTENIMIENTO_COMPLETAR",
            "SP_MANTENIMIENTO_COMPLETE",
            c =>
            {
                Id(c, "@I_ID_MANTENIMIENTO", change.MaintenanceId);
                Date(c, "@I_FECHA_FIN_UTC", change.EndDateUtc);
                Text(c, "@I_RESULTADO", 1000, change.Result);
                Text(c, "@I_COMPONENTE_ANTERIOR", 200, change.OldComponent);
                Text(c, "@I_COMPONENTE_NUEVO", 200, change.NewComponent);
                NullableMoney(c, "@I_COSTO", change.Cost);
                Text(c, "@I_CERTIFICADO_REFERENCIA", 500, change.CertificateReference);
                DateOnly(c, "@I_FECHA_PROXIMA_REVISION", change.NextReviewDate);
                Version(c, change.RowVersion);
            },
            cancellationToken
        );

    /// <inheritdoc />
    public Task<long> CreateLoanAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        LoanChange change,
        CancellationToken cancellationToken
    ) =>
        Save(
            companyId,
            companyCode,
            userId,
            sessionId,
            "PRESTAMO_CONFIRMAR",
            "SP_PRESTAMO_CREATE",
            "@O_ID_PRESTAMO",
            c =>
            {
                Text(c, "@I_TIPO_DESTINO", 30, change.DestinationType);
                Id(c, "@I_ID_SUCURSAL_DESTINO", change.DestinationBranchId);
                Id(c, "@I_ID_CLIENTE_DESTINO", change.DestinationClientId);
                Text(c, "@I_DESTINO_EXTERNO", 200, change.ExternalDestination);
                Text(c, "@I_MODALIDAD_ENTREGA", 30, change.DeliveryMode);
                Date(c, "@I_FECHA_SALIDA_UTC", change.DepartureDateUtc);
                NullableDate(c, "@I_FECHA_DEVOLUCION_PREVISTA", change.ExpectedReturnDateUtc);
                Text(c, "@I_OBSERVACION", 1000, change.Observation);
                Text(c, "@I_EMPRESA_DESTINO_CODIGO", 30, change.DestinationCompanyCode);
                Guid(c, "@I_ID_CORRELACION_INTEREMPRESA", change.IntercompanyCorrelationId);
                Text(c, "@I_ACTIVOS_JSON", -1, JsonSerializer.Serialize(change.Assets));
            },
            cancellationToken
        );

    /// <inheritdoc />
    public Task ReturnLoanAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        LoanReturnChange change,
        CancellationToken cancellationToken
    ) =>
        Execute(
            companyId,
            companyCode,
            userId,
            sessionId,
            "PRESTAMO_DEVOLVER",
            "SP_PRESTAMO_RETURN",
            c =>
            {
                Id(c, "@I_ID_PRESTAMO", change.LoanId);
                Date(c, "@I_FECHA_DEVOLUCION_UTC", change.ReturnDateUtc);
                Text(c, "@I_OBSERVACION", 1000, change.Observation);
                Text(c, "@I_ACTIVOS_JSON", -1, JsonSerializer.Serialize(change.Assets));
                Version(c, change.RowVersion);
            },
            cancellationToken
        );

    /// <summary>Ejecuta un alta trazable y devuelve el identificador asignado por SQL Server.</summary>
    /// <param name="companyId">Empresa autorizada para la operación.</param>
    /// <param name="companyCode">Código que selecciona la base de datos de la empresa.</param>
    /// <param name="userId">Usuario responsable del alta.</param>
    /// <param name="sessionId">Sesión responsable del alta.</param>
    /// <param name="function">Funcionalidad registrada en auditoría.</param>
    /// <param name="procedure">Procedimiento del esquema INVENTARIO que realiza el alta.</param>
    /// <param name="output">Nombre del parámetro de identificador resultante.</param>
    /// <param name="values">Acción que agrega los datos específicos del alta.</param>
    /// <param name="token">Cancelación de la operación de base de datos.</param>
    /// <returns>Identificador generado para el registro trazable.</returns>
    /// <exception cref="InventoryOperationException">El procedimiento rechazó el alta.</exception>
    private async Task<long> Save(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        string function,
        string procedure,
        string output,
        Action<SqlCommand> values,
        CancellationToken token
    )
    {
        var started = DateTimeOffset.UtcNow;
        await using var connection = new SqlConnection(databases.GetConnectionString(companyCode));
        await using var command = Command(connection, procedure);
        Context(command, companyId, userId, sessionId);
        values(command);
        var id = command.Parameters.Add(output, SqlDbType.BigInt);
        id.Direction = ParameterDirection.Output;
        var outputs = Outputs(command, true);
        await connection.OpenAsync(token);
        await command.ExecuteNonQueryAsync(token);
        Ensure(outputs);
        var result = Convert.ToInt64(id.Value, System.Globalization.CultureInfo.InvariantCulture);
        await Audit(
            companyCode,
            userId,
            sessionId,
            function,
            "CREAR",
            procedure,
            started,
            Rows(outputs),
            token
        );
        return result;
    }

    /// <summary>Ejecuta un cambio trazable sin identificador de retorno y registra su auditoría.</summary>
    /// <param name="companyId">Empresa autorizada para la operación.</param>
    /// <param name="companyCode">Código que selecciona la base de datos de la empresa.</param>
    /// <param name="userId">Usuario responsable del cambio.</param>
    /// <param name="sessionId">Sesión responsable del cambio.</param>
    /// <param name="function">Funcionalidad registrada en auditoría.</param>
    /// <param name="procedure">Procedimiento del esquema INVENTARIO que aplica el cambio.</param>
    /// <param name="values">Acción que agrega los datos específicos del cambio.</param>
    /// <param name="token">Cancelación de la operación de base de datos.</param>
    /// <returns>Tarea que finaliza cuando el cambio trazable y su auditoría quedan persistidos.</returns>
    /// <exception cref="InventoryOperationException">El procedimiento rechazó el cambio.</exception>
    private async Task Execute(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        string function,
        string procedure,
        Action<SqlCommand> values,
        CancellationToken token
    )
    {
        var started = DateTimeOffset.UtcNow;
        await using var connection = new SqlConnection(databases.GetConnectionString(companyCode));
        await using var command = Command(connection, procedure);
        Context(command, companyId, userId, sessionId);
        values(command);
        var outputs = Outputs(command, true);
        await connection.OpenAsync(token);
        await command.ExecuteNonQueryAsync(token);
        Ensure(outputs);
        await Audit(
            companyCode,
            userId,
            sessionId,
            function,
            "MODIFICAR",
            procedure,
            started,
            Rows(outputs),
            token
        );
    }

    /// <summary>Registra la ejecución exitosa del SP con usuario, sesión, duración y filas afectadas.</summary>
    /// <param name="companyCode">Empresa en cuya base se ejecutó el SP.</param>
    /// <param name="userId">Usuario responsable.</param>
    /// <param name="sessionId">Sesión responsable.</param>
    /// <param name="function">Funcionalidad auditada.</param>
    /// <param name="action">Acción auditada, como crear o modificar.</param>
    /// <param name="procedure">Procedimiento ejecutado.</param>
    /// <param name="started">Instante de inicio de la operación.</param>
    /// <param name="rows">Cantidad de filas afectadas.</param>
    /// <param name="token">Cancelación de la escritura de auditoría.</param>
    /// <returns>Tarea que finaliza al guardar la evidencia.</returns>
    private Task Audit(
        string companyCode,
        long userId,
        long sessionId,
        string function,
        string action,
        string procedure,
        DateTimeOffset started,
        int rows,
        CancellationToken token
    ) =>
        auditWriter.WriteAsync(
            new StoredProcedureAuditEntry(
                companyCode,
                "INVENTARIO",
                userId,
                sessionId,
                function,
                action,
                "INVENTARIO",
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
                System.Guid.NewGuid()
            ),
            token
        );

    /// <summary>Prepara un procedimiento del esquema INVENTARIO.</summary>
    /// <param name="c">Conexión de la empresa seleccionada.</param>
    /// <param name="p">Nombre del procedimiento sin el esquema.</param>
    /// <returns>Comando listo para recibir parámetros.</returns>
    private static SqlCommand Command(SqlConnection c, string p) =>
        new($"[INVENTARIO].[{p}]", c) { CommandType = CommandType.StoredProcedure };

    /// <summary>Incorpora el contexto de empresa, usuario y sesión requerido por SQL.</summary>
    /// <param name="c">Comando al que se agregan los parámetros.</param>
    /// <param name="company">Empresa autorizada.</param>
    /// <param name="user">Usuario autenticado.</param>
    /// <param name="session">Sesión autenticada.</param>
    private static void Context(SqlCommand c, long company, long user, long session)
    {
        c.Parameters.Add("@I_ID_EMPRESA", SqlDbType.BigInt).Value = company;
        c.Parameters.Add("@S_ID_USUARIO", SqlDbType.BigInt).Value = user;
        c.Parameters.Add("@S_ID_SESION", SqlDbType.BigInt).Value = session;
    }

    /// <summary>Declara las salidas funcionales de error y, opcionalmente, filas afectadas.</summary>
    /// <param name="c">Comando que recibe los parámetros de salida.</param>
    /// <param name="rows">Indica si el procedimiento informa filas afectadas.</param>
    /// <returns>Referencias a las salidas para comprobarlas después de ejecutar.</returns>
    private static OutputParameters Outputs(SqlCommand c, bool rows = false)
    {
        var code = c.Parameters.Add("@O_CODIGO_ERROR", SqlDbType.BigInt);
        code.Direction = ParameterDirection.Output;
        var message = c.Parameters.Add("@O_MENSAJE", SqlDbType.NVarChar, 4000);
        message.Direction = ParameterDirection.Output;
        SqlParameter? affected = null;
        if (rows)
        {
            affected = c.Parameters.Add("@O_FILAS_AFECTADAS", SqlDbType.Int);
            affected.Direction = ParameterDirection.Output;
        }
        return new(code, message, affected);
    }

    /// <summary>Traduce un error funcional del SP a una excepción de inventario.</summary>
    /// <param name="o">Salidas leídas al terminar el procedimiento.</param>
    /// <exception cref="InventoryOperationException">El SP devolvió un código de error.</exception>
    private static void Ensure(OutputParameters o)
    {
        if (o.Code.Value is null or DBNull)
            return;
        throw new InventoryOperationException(
            Convert.ToInt64(o.Code.Value, System.Globalization.CultureInfo.InvariantCulture),
            Convert.ToString(o.Message.Value, System.Globalization.CultureInfo.InvariantCulture)
                ?? "La operación no pudo completarse."
        );
    }

    /// <summary>Extrae las filas afectadas para el registro de auditoría.</summary>
    /// <param name="o">Salidas de la operación.</param>
    /// <returns>Filas afectadas, o cero cuando no se informó la salida.</returns>
    private static int Rows(OutputParameters o) =>
        o.Rows?.Value is null or DBNull
            ? 0
            : Convert.ToInt32(o.Rows.Value, System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>Agrega un texto nullable con la longitud SQL declarada.</summary>
    /// <param name="c">Comando que recibe el parámetro.</param>
    /// <param name="n">Nombre SQL del parámetro.</param>
    /// <param name="s">Longitud máxima; -1 representa NVARCHAR(MAX).</param>
    /// <param name="v">Valor recibido o nulo.</param>
    private static void Text(SqlCommand c, string n, int s, object? v) =>
        c.Parameters.Add(n, SqlDbType.NVarChar, s).Value = v ?? DBNull.Value;

    /// <summary>Agrega un identificador BIGINT nullable.</summary>
    /// <param name="c">Comando que recibe el parámetro.</param>
    /// <param name="n">Nombre SQL del parámetro.</param>
    /// <param name="v">Identificador o nulo.</param>
    private static void Id(SqlCommand c, string n, long? v) =>
        c.Parameters.Add(n, SqlDbType.BigInt).Value = v ?? (object)DBNull.Value;

    /// <summary>Agrega un decimal obligatorio con precisión 19 y escala 4.</summary>
    /// <param name="c">Comando que recibe el parámetro.</param>
    /// <param name="n">Nombre SQL del parámetro.</param>
    /// <param name="v">Cantidad o importe informado.</param>
    private static void Money(SqlCommand c, string n, decimal v)
    {
        var p = c.Parameters.Add(n, SqlDbType.Decimal);
        p.Precision = 19;
        p.Scale = 4;
        p.Value = v;
    }

    /// <summary>Agrega un decimal opcional con precisión 19 y escala 4.</summary>
    /// <param name="c">Comando que recibe el parámetro.</param>
    /// <param name="n">Nombre SQL del parámetro.</param>
    /// <param name="v">Cantidad o importe, si se informó.</param>
    private static void NullableMoney(SqlCommand c, string n, decimal? v)
    {
        var p = c.Parameters.Add(n, SqlDbType.Decimal);
        p.Precision = 19;
        p.Scale = 4;
        p.Value = v ?? (object)DBNull.Value;
    }

    /// <summary>Agrega una fecha y hora obligatoria como DATETIME2.</summary>
    /// <param name="c">Comando que recibe el parámetro.</param>
    /// <param name="n">Nombre SQL del parámetro.</param>
    /// <param name="v">Fecha y hora informada.</param>
    private static void Date(SqlCommand c, string n, DateTime v) =>
        c.Parameters.Add(n, SqlDbType.DateTime2).Value = v;

    /// <summary>Agrega una fecha y hora opcional como DATETIME2.</summary>
    /// <param name="c">Comando que recibe el parámetro.</param>
    /// <param name="n">Nombre SQL del parámetro.</param>
    /// <param name="v">Fecha y hora o nulo.</param>
    private static void NullableDate(SqlCommand c, string n, DateTime? v) =>
        c.Parameters.Add(n, SqlDbType.DateTime2).Value = v ?? (object)DBNull.Value;

    /// <summary>Agrega una fecha opcional sin componente horario.</summary>
    /// <param name="c">Comando que recibe el parámetro.</param>
    /// <param name="n">Nombre SQL del parámetro.</param>
    /// <param name="v">Fecha o nulo.</param>
    private static void DateOnly(SqlCommand c, string n, System.DateOnly? v) =>
        c.Parameters.Add(n, SqlDbType.Date).Value =
            v?.ToDateTime(TimeOnly.MinValue) ?? (object)DBNull.Value;

    /// <summary>Agrega un indicador lógico como BIT.</summary>
    /// <param name="c">Comando que recibe el parámetro.</param>
    /// <param name="n">Nombre SQL del parámetro.</param>
    /// <param name="v">Valor lógico informado.</param>
    private static void Bit(SqlCommand c, string n, bool v) =>
        c.Parameters.Add(n, SqlDbType.Bit).Value = v;

    /// <summary>Agrega una correlación GUID opcional.</summary>
    /// <param name="c">Comando que recibe el parámetro.</param>
    /// <param name="n">Nombre SQL del parámetro.</param>
    /// <param name="v">Identificador de correlación o nulo.</param>
    private static void Guid(SqlCommand c, string n, System.Guid? v) =>
        c.Parameters.Add(n, SqlDbType.UniqueIdentifier).Value = v ?? (object)DBNull.Value;

    /// <summary>Agrega la versión binaria de fila para detectar conflictos de concurrencia.</summary>
    /// <param name="c">Comando que recibe el parámetro.</param>
    /// <param name="v">Versión de ocho bytes leída previamente.</param>
    private static void Version(SqlCommand c, byte[] v) =>
        c.Parameters.Add("@I_ROW_VERSION", SqlDbType.Binary, 8).Value = v;

    /// <summary>Lee un identificador BIGINT obligatorio por nombre de columna.</summary>
    /// <param name="r">Lector situado sobre una fila.</param>
    /// <param name="n">Nombre de la columna.</param>
    /// <returns>Identificador leído.</returns>
    private static long Long(SqlDataReader r, string n) => r.GetInt64(r.GetOrdinal(n));

    /// <summary>Lee un identificador BIGINT que puede ser nulo.</summary>
    /// <param name="r">Lector situado sobre una fila.</param>
    /// <param name="n">Nombre de la columna.</param>
    /// <returns>Identificador leído o nulo.</returns>
    private static long? NullableLong(SqlDataReader r, string n)
    {
        var i = r.GetOrdinal(n);
        return r.IsDBNull(i) ? null : r.GetInt64(i);
    }

    /// <summary>Lee una cadena obligatoria por nombre de columna.</summary>
    /// <param name="r">Lector situado sobre una fila.</param>
    /// <param name="n">Nombre de la columna.</param>
    /// <returns>Texto leído.</returns>
    private static string Text(SqlDataReader r, string n) => r.GetString(r.GetOrdinal(n));

    /// <summary>Lee una cadena que puede ser nula.</summary>
    /// <param name="r">Lector situado sobre una fila.</param>
    /// <param name="n">Nombre de la columna.</param>
    /// <returns>Texto leído o nulo.</returns>
    private static string? NullableText(SqlDataReader r, string n)
    {
        var i = r.GetOrdinal(n);
        return r.IsDBNull(i) ? null : r.GetString(i);
    }

    /// <summary>Lee un decimal obligatorio, como cantidad o importe.</summary>
    /// <param name="r">Lector situado sobre una fila.</param>
    /// <param name="n">Nombre de la columna.</param>
    /// <returns>Valor decimal leído.</returns>
    private static decimal Decimal(SqlDataReader r, string n) => r.GetDecimal(r.GetOrdinal(n));

    /// <summary>Lee un decimal que puede ser nulo.</summary>
    /// <param name="r">Lector situado sobre una fila.</param>
    /// <param name="n">Nombre de la columna.</param>
    /// <returns>Valor decimal leído o nulo.</returns>
    private static decimal? NullableDecimal(SqlDataReader r, string n)
    {
        var i = r.GetOrdinal(n);
        return r.IsDBNull(i) ? null : r.GetDecimal(i);
    }

    /// <summary>Lee un indicador BIT obligatorio.</summary>
    /// <param name="r">Lector situado sobre una fila.</param>
    /// <param name="n">Nombre de la columna.</param>
    /// <returns>Valor lógico leído.</returns>
    private static bool Bool(SqlDataReader r, string n) => r.GetBoolean(r.GetOrdinal(n));

    /// <summary>Lee una fecha y hora obligatoria.</summary>
    /// <param name="r">Lector situado sobre una fila.</param>
    /// <param name="n">Nombre de la columna.</param>
    /// <returns>Fecha y hora leída.</returns>
    private static DateTime Date(SqlDataReader r, string n) => r.GetDateTime(r.GetOrdinal(n));

    /// <summary>Lee una fecha y hora que puede ser nula.</summary>
    /// <param name="r">Lector situado sobre una fila.</param>
    /// <param name="n">Nombre de la columna.</param>
    /// <returns>Fecha y hora leída o nulo.</returns>
    private static DateTime? NullableDate(SqlDataReader r, string n)
    {
        var i = r.GetOrdinal(n);
        return r.IsDBNull(i) ? null : r.GetDateTime(i);
    }

    /// <summary>Convierte una fecha SQL nullable en DateOnly.</summary>
    /// <param name="r">Lector situado sobre una fila.</param>
    /// <param name="n">Nombre de la columna.</param>
    /// <returns>Fecha sin hora o nulo.</returns>
    private static System.DateOnly? NullableDateOnly(SqlDataReader r, string n)
    {
        var i = r.GetOrdinal(n);
        return r.IsDBNull(i) ? null : System.DateOnly.FromDateTime(r.GetDateTime(i));
    }

    /// <summary>Lee la versión binaria usada en el control de concurrencia.</summary>
    /// <param name="r">Lector situado sobre una fila.</param>
    /// <param name="n">Nombre de la columna.</param>
    /// <returns>Bytes de versión de la fila.</returns>
    private static byte[] Bytes(SqlDataReader r, string n) => (byte[])r.GetValue(r.GetOrdinal(n));

    /// <summary>Conserva las salidas funcionales devueltas por los SP de inventario.</summary>
    /// <param name="Code">Código de error funcional; nulo indica éxito.</param>
    /// <param name="Message">Mensaje funcional devuelto por SQL.</param>
    /// <param name="Rows">Cantidad opcional de filas afectadas.</param>
    private sealed record OutputParameters(
        SqlParameter Code,
        SqlParameter Message,
        SqlParameter? Rows
    );
}
