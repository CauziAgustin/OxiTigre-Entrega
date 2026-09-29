/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.DAL.Inventory.SqlInventoryStore
Archivo: SqlInventoryStore.cs | Versión: 1.2.0 | Fecha: 2026-08-24 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Persiste Inventario mediante SP, concurrencia, salidas controladas y auditoría central.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-21 | FABRICA | Agustin Omar Cauzi | Lectura de stock reservado y disponible.
Historial: 1.2.0 | 2026-08-24 | FABRICA | Agustin Omar Cauzi | Persistencia de la configuración de trazabilidad.
===============================================================================
*/
using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using OxiTigre.BLL.Inventory;
using OxiTigre.DAL.MultiCompany;
using OxiTigre.DAL.StoredProcedures;

namespace OxiTigre.DAL.Inventory;

/// <summary>Implementa el almacenamiento SQL de maestros, saldos y movimientos.</summary>
public sealed class SqlInventoryStore(
    CompanyDatabaseRegistry databases,
    IStoredProcedureAuditWriter auditWriter
) : IInventoryStore
{
    /// <inheritdoc />
    public async Task<InventorySnapshot> GetAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        CancellationToken cancellationToken
    )
    {
        var startedAtUtc = DateTimeOffset.UtcNow;
        await using var connection = new SqlConnection(databases.GetConnectionString(companyCode));
        await using var command = Command(connection, "[INVENTARIO].[SP_INVENTARIO_GET]");
        command.Parameters.Add("@I_ID_EMPRESA", SqlDbType.BigInt).Value = companyId;
        command.Parameters.Add("@S_ID_SESION", SqlDbType.BigInt).Value = sessionId;
        command.Parameters.Add("@S_ID_USUARIO", SqlDbType.BigInt).Value = userId;
        var outputs = AddOutputs(command);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var units = new List<MeasurementUnit>();
        while (await reader.ReadAsync(cancellationToken))
            units.Add(
                new(
                    Long(reader, "ID_UNIDAD_MEDIDA"),
                    Text(reader, "CODIGO"),
                    Text(reader, "NOMBRE"),
                    Text(reader, "SIMBOLO"),
                    Bool(reader, "PERMITE_DECIMALES"),
                    Text(reader, "CODIGO_ESTADO"),
                    Bytes(reader, "ROW_VERSION")
                )
            );
        var categories = new List<ProductCategory>();
        if (await reader.NextResultAsync(cancellationToken))
            while (await reader.ReadAsync(cancellationToken))
                categories.Add(
                    new(
                        Long(reader, "ID_CATEGORIA_PRODUCTO"),
                        Text(reader, "CODIGO"),
                        Text(reader, "NOMBRE"),
                        NullableText(reader, "DESCRIPCION"),
                        Text(reader, "CODIGO_ESTADO"),
                        Bytes(reader, "ROW_VERSION")
                    )
                );
        var products = new List<Product>();
        if (await reader.NextResultAsync(cancellationToken))
            while (await reader.ReadAsync(cancellationToken))
                products.Add(
                    new(
                        Long(reader, "ID_PRODUCTO"),
                        Long(reader, "ID_CATEGORIA_PRODUCTO"),
                        Long(reader, "ID_UNIDAD_MEDIDA"),
                        Text(reader, "CODIGO"),
                        Text(reader, "NOMBRE"),
                        NullableText(reader, "DESCRIPCION"),
                        NullableText(reader, "CODIGO_BARRAS"),
                        Text(reader, "CATEGORIA"),
                        Text(reader, "SIMBOLO"),
                        Text(reader, "CODIGO_ESTADO"),
                        Bytes(reader, "ROW_VERSION"),
                        Text(reader, "TIPO_ITEM"),
                        Text(reader, "TIPO_TRAZABILIDAD"),
                        Bool(reader, "ES_REUTILIZABLE"),
                        Bool(reader, "ADMITE_PRESTAMO"),
                        Bool(reader, "REQUIERE_MANTENIMIENTO"),
                        Bool(reader, "ADMITE_MEDICIONES")
                    )
                );
        var warehouses = new List<Warehouse>();
        if (await reader.NextResultAsync(cancellationToken))
            while (await reader.ReadAsync(cancellationToken))
                warehouses.Add(
                    new(
                        Long(reader, "ID_DEPOSITO"),
                        NullableLong(reader, "ID_SUCURSAL"),
                        Text(reader, "CODIGO"),
                        Text(reader, "NOMBRE"),
                        NullableText(reader, "DOMICILIO"),
                        NullableText(reader, "DESCRIPCION"),
                        Text(reader, "CODIGO_ESTADO"),
                        Bytes(reader, "ROW_VERSION")
                    )
                );
        var locations = new List<WarehouseLocation>();
        if (await reader.NextResultAsync(cancellationToken))
            while (await reader.ReadAsync(cancellationToken))
                locations.Add(
                    new(
                        Long(reader, "ID_UBICACION"),
                        Long(reader, "ID_DEPOSITO"),
                        Text(reader, "CODIGO"),
                        Text(reader, "NOMBRE"),
                        NullableText(reader, "DESCRIPCION"),
                        Text(reader, "CODIGO_ESTADO"),
                        Bytes(reader, "ROW_VERSION")
                    )
                );
        var stock = new List<StockBalance>();
        if (await reader.NextResultAsync(cancellationToken))
            while (await reader.ReadAsync(cancellationToken))
                stock.Add(
                    new(
                        Long(reader, "ID_EXISTENCIA"),
                        Long(reader, "ID_PRODUCTO"),
                        Long(reader, "ID_DEPOSITO"),
                        Text(reader, "CODIGO_PRODUCTO"),
                        Text(reader, "PRODUCTO"),
                        Text(reader, "DEPOSITO"),
                        Text(reader, "SIMBOLO"),
                        Decimal(reader, "CANTIDAD"),
                        Decimal(reader, "CANTIDAD_RESERVADA"),
                        Decimal(reader, "CANTIDAD_DISPONIBLE"),
                        Decimal(reader, "STOCK_MINIMO"),
                        Bool(reader, "BAJO_MINIMO"),
                        Bytes(reader, "ROW_VERSION")
                    )
                );
        var movements = new List<InventoryMovementLine>();
        if (await reader.NextResultAsync(cancellationToken))
            while (await reader.ReadAsync(cancellationToken))
                movements.Add(
                    new(
                        Long(reader, "ID_MOVIMIENTO"),
                        Text(reader, "CODIGO"),
                        Text(reader, "TIPO_MOVIMIENTO"),
                        Date(reader, "FECHA_MOVIMIENTO_UTC"),
                        NullableText(reader, "OBSERVACION"),
                        Text(reader, "CODIGO_ESTADO"),
                        Long(reader, "ID_MOVIMIENTO_DETALLE"),
                        Long(reader, "ID_PRODUCTO"),
                        Text(reader, "CODIGO_PRODUCTO"),
                        Text(reader, "PRODUCTO"),
                        NullableLong(reader, "ID_DEPOSITO_ORIGEN"),
                        NullableText(reader, "DEPOSITO_ORIGEN"),
                        NullableLong(reader, "ID_UBICACION_ORIGEN"),
                        NullableText(reader, "UBICACION_ORIGEN"),
                        NullableLong(reader, "ID_DEPOSITO_DESTINO"),
                        NullableText(reader, "DEPOSITO_DESTINO"),
                        NullableLong(reader, "ID_UBICACION_DESTINO"),
                        NullableText(reader, "UBICACION_DESTINO"),
                        Decimal(reader, "CANTIDAD"),
                        Text(reader, "SIMBOLO")
                    )
                );
        await reader.DisposeAsync();
        EnsureSuccess(outputs);
        await AuditAsync(
            companyCode,
            userId,
            sessionId,
            "INVENTARIO_CONSULTAR",
            "CONSULTAR",
            "SP_INVENTARIO_GET",
            startedAtUtc,
            products.Count + stock.Count + movements.Count,
            cancellationToken
        );
        return new(units, categories, products, warehouses, locations, stock, movements);
    }

    /// <inheritdoc />
    public Task<long> SaveMeasurementUnitAsync(
        string companyCode,
        long userId,
        long sessionId,
        MeasurementUnitChange change,
        CancellationToken cancellationToken
    ) =>
        SaveAsync(
            companyCode,
            userId,
            sessionId,
            "UNIDAD_MEDIDA_GUARDAR",
            change.MeasurementUnitId,
            "SP_UNIDAD_MEDIDA_SAVE",
            "@O_ID_UNIDAD_MEDIDA",
            command =>
            {
                AddId(command, "@I_ID_UNIDAD_MEDIDA", change.MeasurementUnitId);
                AddText(command, "@I_CODIGO", 30, change.Code);
                AddText(command, "@I_NOMBRE", 100, change.Name);
                AddText(command, "@I_SIMBOLO", 20, change.Symbol);
                command.Parameters.Add("@I_PERMITE_DECIMALES", SqlDbType.Bit).Value =
                    change.AllowsDecimals;
                AddText(command, "@I_CODIGO_ESTADO", 30, change.StatusCode);
                AddVersion(command, change.RowVersion);
            },
            cancellationToken
        );

    /// <inheritdoc />
    public Task<long> SaveCategoryAsync(
        string companyCode,
        long userId,
        long sessionId,
        ProductCategoryChange change,
        CancellationToken cancellationToken
    ) =>
        SaveAsync(
            companyCode,
            userId,
            sessionId,
            "CATEGORIA_PRODUCTO_GUARDAR",
            change.ProductCategoryId,
            "SP_CATEGORIA_PRODUCTO_SAVE",
            "@O_ID_CATEGORIA_PRODUCTO",
            command =>
            {
                AddId(command, "@I_ID_CATEGORIA_PRODUCTO", change.ProductCategoryId);
                AddText(command, "@I_CODIGO", 30, change.Code);
                AddText(command, "@I_NOMBRE", 150, change.Name);
                AddText(command, "@I_DESCRIPCION", 500, change.Description);
                AddText(command, "@I_CODIGO_ESTADO", 30, change.StatusCode);
                AddVersion(command, change.RowVersion);
            },
            cancellationToken
        );

    /// <inheritdoc />
    public Task<long> SaveProductAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        ProductChange change,
        CancellationToken cancellationToken
    ) =>
        SaveAsync(
            companyCode,
            userId,
            sessionId,
            "PRODUCTO_GUARDAR",
            change.ProductId,
            "SP_PRODUCTO_SAVE",
            "@O_ID_PRODUCTO",
            command =>
            {
                AddId(command, "@I_ID_PRODUCTO", change.ProductId);
                command.Parameters.Add("@I_ID_EMPRESA", SqlDbType.BigInt).Value = companyId;
                command.Parameters.Add("@I_ID_CATEGORIA_PRODUCTO", SqlDbType.BigInt).Value =
                    change.ProductCategoryId;
                command.Parameters.Add("@I_ID_UNIDAD_MEDIDA", SqlDbType.BigInt).Value =
                    change.MeasurementUnitId;
                AddText(command, "@I_NOMBRE", 200, change.Name);
                AddText(command, "@I_DESCRIPCION", 1000, change.Description);
                AddText(command, "@I_CODIGO_BARRAS", 80, change.Barcode);
                AddText(command, "@I_TIPO_ITEM", 20, change.ItemType);
                AddText(command, "@I_TIPO_TRAZABILIDAD", 20, change.TrackingType);
                command.Parameters.Add("@I_ES_REUTILIZABLE", SqlDbType.Bit).Value =
                    change.IsReusable;
                command.Parameters.Add("@I_ADMITE_PRESTAMO", SqlDbType.Bit).Value =
                    change.AllowsLoans;
                command.Parameters.Add("@I_REQUIERE_MANTENIMIENTO", SqlDbType.Bit).Value =
                    change.RequiresMaintenance;
                command.Parameters.Add("@I_ADMITE_MEDICIONES", SqlDbType.Bit).Value =
                    change.AllowsMeasurements;
                AddText(command, "@I_CODIGO_ESTADO", 30, change.StatusCode);
                AddVersion(command, change.RowVersion);
            },
            cancellationToken
        );

    /// <inheritdoc />
    public Task<long> SaveWarehouseAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        WarehouseChange change,
        CancellationToken cancellationToken
    ) =>
        SaveAsync(
            companyCode,
            userId,
            sessionId,
            "DEPOSITO_GUARDAR",
            change.WarehouseId,
            "SP_DEPOSITO_SAVE",
            "@O_ID_DEPOSITO",
            command =>
            {
                AddId(command, "@I_ID_DEPOSITO", change.WarehouseId);
                command.Parameters.Add("@I_ID_EMPRESA", SqlDbType.BigInt).Value = companyId;
                AddId(command, "@I_ID_SUCURSAL", change.BranchId);
                AddText(command, "@I_CODIGO", 30, change.Code);
                AddText(command, "@I_NOMBRE", 150, change.Name);
                AddText(command, "@I_DOMICILIO", 250, change.Address);
                AddText(command, "@I_DESCRIPCION", 500, change.Description);
                AddText(command, "@I_CODIGO_ESTADO", 30, change.StatusCode);
                AddVersion(command, change.RowVersion);
            },
            cancellationToken
        );

    /// <inheritdoc />
    public Task<long> SaveLocationAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        WarehouseLocationChange change,
        CancellationToken cancellationToken
    ) =>
        SaveAsync(
            companyCode,
            userId,
            sessionId,
            "UBICACION_GUARDAR",
            change.LocationId,
            "SP_UBICACION_SAVE",
            "@O_ID_UBICACION",
            command =>
            {
                AddId(command, "@I_ID_UBICACION", change.LocationId);
                command.Parameters.Add("@I_ID_EMPRESA", SqlDbType.BigInt).Value = companyId;
                command.Parameters.Add("@I_ID_DEPOSITO", SqlDbType.BigInt).Value =
                    change.WarehouseId;
                AddText(command, "@I_CODIGO", 30, change.Code);
                AddText(command, "@I_NOMBRE", 150, change.Name);
                AddText(command, "@I_DESCRIPCION", 500, change.Description);
                AddText(command, "@I_CODIGO_ESTADO", 30, change.StatusCode);
                AddVersion(command, change.RowVersion);
            },
            cancellationToken
        );

    /// <inheritdoc />
    public Task UpdateMinimumStockAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        MinimumStockChange change,
        CancellationToken cancellationToken
    ) =>
        ExecuteAsync(
            companyCode,
            userId,
            sessionId,
            "EXISTENCIA_MINIMO_ACTUALIZAR",
            "SP_EXISTENCIA_MINIMO_UPDATE",
            command =>
            {
                command.Parameters.Add("@I_ID_EXISTENCIA", SqlDbType.BigInt).Value =
                    change.StockBalanceId;
                command.Parameters.Add("@I_ID_EMPRESA", SqlDbType.BigInt).Value = companyId;
                var minimum = command.Parameters.Add("@I_STOCK_MINIMO", SqlDbType.Decimal);
                minimum.Precision = 19;
                minimum.Scale = 4;
                minimum.Value = change.MinimumStock;
                AddVersion(command, change.RowVersion);
            },
            cancellationToken
        );

    /// <inheritdoc />
    public Task<long> CreateMovementAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        InventoryMovementChange change,
        CancellationToken cancellationToken
    ) =>
        SaveAsync(
            companyCode,
            userId,
            sessionId,
            "MOVIMIENTO_CONFIRMAR",
            null,
            "SP_MOVIMIENTO_CREATE",
            "@O_ID_MOVIMIENTO",
            command =>
            {
                command.Parameters.Add("@I_ID_EMPRESA", SqlDbType.BigInt).Value = companyId;
                AddText(command, "@I_TIPO_MOVIMIENTO", 30, change.MovementType);
                command.Parameters.Add("@I_FECHA_MOVIMIENTO_UTC", SqlDbType.DateTime2).Value =
                    change.MovementDateUtc;
                AddText(command, "@I_OBSERVACION", 500, change.Observation);
                AddText(command, "@I_DETALLES_JSON", -1, JsonSerializer.Serialize(change.Details));
                command.Parameters.Add("@S_ID_SESION", SqlDbType.BigInt).Value = sessionId;
            },
            cancellationToken
        );

    /// <summary>Guarda un maestro de inventario, verifica la salida funcional y registra auditoría.</summary>
    /// <param name="companyCode">Código que selecciona la base de datos de la empresa.</param>
    /// <param name="userId">Usuario responsable del guardado.</param>
    /// <param name="sessionId">Sesión responsable registrada en auditoría.</param>
    /// <param name="functionality">Funcionalidad auditada.</param>
    /// <param name="currentId">Identificador existente; nulo cuando se crea.</param>
    /// <param name="storedProcedure">Nombre del SP del esquema INVENTARIO.</param>
    /// <param name="outputName">Nombre del parámetro de identificador resultante.</param>
    /// <param name="parameters">Acción que agrega los datos específicos requeridos por el SP.</param>
    /// <param name="cancellationToken">Cancelación de la operación de base de datos.</param>
    /// <returns>Identificador confirmado por el procedimiento.</returns>
    /// <exception cref="InventoryOperationException">El SP rechazó el guardado.</exception>
    private async Task<long> SaveAsync(
        string companyCode,
        long userId,
        long sessionId,
        string functionality,
        long? currentId,
        string storedProcedure,
        string outputName,
        Action<SqlCommand> parameters,
        CancellationToken cancellationToken
    )
    {
        var startedAtUtc = DateTimeOffset.UtcNow;
        await using var connection = new SqlConnection(databases.GetConnectionString(companyCode));
        await using var command = Command(connection, $"[INVENTARIO].[{storedProcedure}]");
        parameters(command);
        command.Parameters.Add("@S_ID_USUARIO", SqlDbType.BigInt).Value = userId;
        var id = command.Parameters.Add(outputName, SqlDbType.BigInt);
        id.Direction = ParameterDirection.Output;
        var outputs = AddOutputs(command, true);
        await connection.OpenAsync(cancellationToken);
        await command.ExecuteNonQueryAsync(cancellationToken);
        EnsureSuccess(outputs);
        var result = Convert.ToInt64(id.Value, System.Globalization.CultureInfo.InvariantCulture);
        await AuditAsync(
            companyCode,
            userId,
            sessionId,
            functionality,
            currentId is null ? "CREAR" : "MODIFICAR",
            storedProcedure,
            startedAtUtc,
            Convert.ToInt32(outputs.Rows!.Value, System.Globalization.CultureInfo.InvariantCulture),
            cancellationToken
        );
        return result;
    }

    /// <summary>Ejecuta un cambio de inventario sin identificador de retorno y registra auditoría.</summary>
    /// <param name="companyCode">Código que selecciona la base de datos de la empresa.</param>
    /// <param name="userId">Usuario responsable del cambio.</param>
    /// <param name="sessionId">Sesión responsable registrada en auditoría.</param>
    /// <param name="functionality">Funcionalidad auditada.</param>
    /// <param name="storedProcedure">Nombre del SP del esquema INVENTARIO.</param>
    /// <param name="parameters">Acción que agrega los datos específicos requeridos por el SP.</param>
    /// <param name="cancellationToken">Cancelación de la operación de base de datos.</param>
    /// <returns>Tarea que finaliza cuando el cambio y su auditoría quedan persistidos.</returns>
    /// <exception cref="InventoryOperationException">El SP rechazó el cambio.</exception>
    private async Task ExecuteAsync(
        string companyCode,
        long userId,
        long sessionId,
        string functionality,
        string storedProcedure,
        Action<SqlCommand> parameters,
        CancellationToken cancellationToken
    )
    {
        var startedAtUtc = DateTimeOffset.UtcNow;
        await using var connection = new SqlConnection(databases.GetConnectionString(companyCode));
        await using var command = Command(connection, $"[INVENTARIO].[{storedProcedure}]");
        parameters(command);
        command.Parameters.Add("@S_ID_USUARIO", SqlDbType.BigInt).Value = userId;
        var outputs = AddOutputs(command, true);
        await connection.OpenAsync(cancellationToken);
        await command.ExecuteNonQueryAsync(cancellationToken);
        EnsureSuccess(outputs);
        await AuditAsync(
            companyCode,
            userId,
            sessionId,
            functionality,
            "MODIFICAR",
            storedProcedure,
            startedAtUtc,
            Convert.ToInt32(outputs.Rows!.Value, System.Globalization.CultureInfo.InvariantCulture),
            cancellationToken
        );
    }

    /// <summary>Conserva la evidencia de una operación de inventario confirmada con usuario, sesión y SP.</summary>
    /// <param name="companyCode">Empresa cuya base procesó la operación.</param>
    /// <param name="userId">Usuario responsable.</param>
    /// <param name="sessionId">Sesión responsable.</param>
    /// <param name="functionality">Funcionalidad auditada.</param>
    /// <param name="action">Acción auditada.</param>
    /// <param name="storedProcedure">Procedimiento ejecutado.</param>
    /// <param name="startedAtUtc">Instante de inicio de la ejecución.</param>
    /// <param name="rows">Filas afectadas o consultadas.</param>
    /// <param name="cancellationToken">Cancelación del registro.</param>
    /// <returns>Tarea que finaliza al persistir la evidencia.</returns>
    private Task AuditAsync(
        string companyCode,
        long userId,
        long sessionId,
        string functionality,
        string action,
        string storedProcedure,
        DateTimeOffset startedAtUtc,
        int rows,
        CancellationToken cancellationToken
    ) =>
        auditWriter.WriteAsync(
            new StoredProcedureAuditEntry(
                companyCode,
                "INVENTARIO",
                userId,
                sessionId,
                functionality,
                action,
                "INVENTARIO",
                storedProcedure,
                "{}",
                startedAtUtc,
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

    /// <summary>Prepara un comando para un nombre de procedimiento completamente calificado.</summary>
    /// <param name="connection">Conexión de la empresa seleccionada.</param>
    /// <param name="name">Nombre del SP, incluido su esquema.</param>
    /// <returns>Comando listo para recibir parámetros.</returns>
    private static SqlCommand Command(SqlConnection connection, string name) =>
        new(name, connection) { CommandType = CommandType.StoredProcedure };

    /// <summary>Declara el error funcional y, opcionalmente, las filas afectadas devueltas por el SP.</summary>
    /// <param name="command">Comando que recibe las salidas.</param>
    /// <param name="includeRows">Indica si el SP informa filas afectadas.</param>
    /// <returns>Referencias a las salidas para comprobar el resultado.</returns>
    private static OutputParameters AddOutputs(SqlCommand command, bool includeRows = false)
    {
        var code = command.Parameters.Add("@O_CODIGO_ERROR", SqlDbType.BigInt);
        code.Direction = ParameterDirection.Output;
        var message = command.Parameters.Add("@O_MENSAJE", SqlDbType.NVarChar, 4000);
        message.Direction = ParameterDirection.Output;
        SqlParameter? rows = null;
        if (includeRows)
        {
            rows = command.Parameters.Add("@O_FILAS_AFECTADAS", SqlDbType.Int);
            rows.Direction = ParameterDirection.Output;
        }
        return new(code, message, rows);
    }

    /// <summary>Interpreta las salidas y rechaza un error funcional del SP.</summary>
    /// <param name="outputs">Salidas leídas después de ejecutar.</param>
    /// <exception cref="InventoryOperationException">El SP devolvió un código de error.</exception>
    private static void EnsureSuccess(OutputParameters outputs)
    {
        if (outputs.Code.Value is DBNull or null)
            return;
        throw new InventoryOperationException(
            Convert.ToInt64(outputs.Code.Value, System.Globalization.CultureInfo.InvariantCulture),
            Convert.ToString(
                outputs.Message.Value,
                System.Globalization.CultureInfo.InvariantCulture
            ) ?? "La operación no pudo completarse."
        );
    }

    /// <summary>Agrega texto nullable con la longitud declarada por el SP.</summary>
    /// <param name="command">Comando que recibe el parámetro.</param>
    /// <param name="name">Nombre SQL del parámetro.</param>
    /// <param name="size">Longitud máxima; -1 representa NVARCHAR(MAX).</param>
    /// <param name="value">Texto o valor nulo.</param>
    private static void AddText(SqlCommand command, string name, int size, object? value) =>
        command.Parameters.Add(name, SqlDbType.NVarChar, size).Value = value ?? DBNull.Value;

    /// <summary>Agrega un identificador BIGINT nullable.</summary>
    /// <param name="command">Comando que recibe el parámetro.</param>
    /// <param name="name">Nombre SQL del parámetro.</param>
    /// <param name="value">Identificador o nulo en un alta.</param>
    private static void AddId(SqlCommand command, string name, long? value) =>
        command.Parameters.Add(name, SqlDbType.BigInt).Value = value ?? (object)DBNull.Value;

    /// <summary>Agrega la versión de fila usada para detectar cambios concurrentes.</summary>
    /// <param name="command">Comando que recibe el parámetro.</param>
    /// <param name="value">Versión leída previamente o nulo en un alta.</param>
    private static void AddVersion(SqlCommand command, byte[]? value) =>
        command.Parameters.Add("@I_ROW_VERSION", SqlDbType.Binary, 8).Value =
            value ?? (object)DBNull.Value;

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

    /// <summary>Lee un indicador BIT obligatorio.</summary>
    /// <param name="reader">Lector situado sobre una fila.</param>
    /// <param name="name">Nombre de la columna.</param>
    /// <returns>Valor lógico leído.</returns>
    private static bool Bool(SqlDataReader reader, string name) =>
        reader.GetBoolean(reader.GetOrdinal(name));

    /// <summary>Lee una cantidad decimal obligatoria.</summary>
    /// <param name="reader">Lector situado sobre una fila.</param>
    /// <param name="name">Nombre de la columna.</param>
    /// <returns>Valor decimal leído.</returns>
    private static decimal Decimal(SqlDataReader reader, string name) =>
        reader.GetDecimal(reader.GetOrdinal(name));

    /// <summary>Lee una fecha y hora obligatoria.</summary>
    /// <param name="reader">Lector situado sobre una fila.</param>
    /// <param name="name">Nombre de la columna.</param>
    /// <returns>Fecha y hora leída.</returns>
    private static DateTime Date(SqlDataReader reader, string name) =>
        reader.GetDateTime(reader.GetOrdinal(name));

    /// <summary>Lee la versión binaria usada en control de concurrencia.</summary>
    /// <param name="reader">Lector situado sobre una fila.</param>
    /// <param name="name">Nombre de la columna.</param>
    /// <returns>Bytes de versión de la fila.</returns>
    private static byte[] Bytes(SqlDataReader reader, string name) => (byte[])reader[name];

    /// <summary>Conserva las salidas funcionales y filas afectadas de un SP de inventario.</summary>
    /// <param name="Code">Código de error; nulo indica éxito.</param>
    /// <param name="Message">Mensaje funcional devuelto por SQL.</param>
    /// <param name="Rows">Cantidad opcional de filas afectadas.</param>
    private sealed record OutputParameters(
        SqlParameter Code,
        SqlParameter Message,
        SqlParameter? Rows
    );
}
