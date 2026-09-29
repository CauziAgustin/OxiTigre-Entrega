/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.DAL.Purchasing.SqlPurchasingStore
Archivo: SqlPurchasingStore.cs | Versión: 1.1.0 | Fecha: 2026-08-26 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Persiste proveedores, órdenes, autorizaciones y recepciones mediante SP auditados.
Historial: 1.0.0 | 2026-08-24 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-26 | FABRICA | Agustin Omar Cauzi | Snapshot histórico y ejecución de reversión compensatoria.
===============================================================================
*/
using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using OxiTigre.BLL.Purchasing;
using OxiTigre.DAL.MultiCompany;
using OxiTigre.DAL.StoredProcedures;

namespace OxiTigre.DAL.Purchasing;

/// <summary>Implementa el circuito de Compras sobre SQL Server y traduce errores funcionales.</summary>
public sealed class SqlPurchasingStore(CompanyDatabaseRegistry databases, IStoredProcedureAuditWriter auditWriter)
    : IPurchasingStore
{
    /// <inheritdoc />
    public async Task<PurchasingSnapshot> GetAsync(long companyId, string companyCode, long userId, long sessionId,
        CancellationToken cancellationToken)
    {
        var started = DateTimeOffset.UtcNow;
        await using var connection = new SqlConnection(databases.GetConnectionString(companyCode));
        await using var command = Command(connection, "SP_COMPRAS_GET");
        Context(command, companyId, userId, sessionId);
        var outputs = Outputs(command);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var suppliers = new List<Supplier>();
        while (await reader.ReadAsync(cancellationToken)) suppliers.Add(new(Long(reader, "ID_PROVEEDOR"),
            Text(reader, "CODIGO"), Text(reader, "RAZON_SOCIAL"), NullableText(reader, "NOMBRE_COMERCIAL"),
            Text(reader, "CUIT"), NullableText(reader, "CORREO"), NullableText(reader, "TELEFONO"),
            NullableText(reader, "CONDICION_PAGO"), NullableText(reader, "OBSERVACION"),
            Text(reader, "CODIGO_ESTADO"), Bytes(reader, "ROW_VERSION")));
        var products = new List<PurchasingProduct>();
        if (await reader.NextResultAsync(cancellationToken)) while (await reader.ReadAsync(cancellationToken))
            products.Add(new(Long(reader, "ID_PRODUCTO"), Text(reader, "CODIGO"), Text(reader, "NOMBRE"),
                Text(reader, "SIMBOLO"), Text(reader, "TIPO_TRAZABILIDAD"), Bool(reader, "ES_REUTILIZABLE"),
                Bool(reader, "ADMITE_PRESTAMO"), Bool(reader, "REQUIERE_MANTENIMIENTO"),
                Bool(reader, "ADMITE_MEDICIONES")));
        var warehouses = new List<PurchasingWarehouse>();
        if (await reader.NextResultAsync(cancellationToken)) while (await reader.ReadAsync(cancellationToken))
            warehouses.Add(new(Long(reader, "ID_DEPOSITO"), Text(reader, "CODIGO"), Text(reader, "NOMBRE")));
        var orders = new List<PurchaseOrderLine>();
        if (await reader.NextResultAsync(cancellationToken)) while (await reader.ReadAsync(cancellationToken))
            orders.Add(new(Long(reader, "ID_ORDEN_COMPRA"), Long(reader, "ID_PROVEEDOR"),
                Long(reader, "ID_DEPOSITO"), Text(reader, "CODIGO"), Date(reader, "FECHA_ORDEN_UTC"),
                NullableDateOnly(reader, "FECHA_ENTREGA_ESPERADA"), Text(reader, "PROVEEDOR"),
                Text(reader, "PROVEEDOR_CUIT"), NullableText(reader, "PROVEEDOR_CONDICION_PAGO"),
                Text(reader, "DEPOSITO"), Text(reader, "MONEDA"), Decimal(reader, "SUBTOTAL"),
                Decimal(reader, "TOTAL_DESCUENTO"), Decimal(reader, "TOTAL_IMPUESTO"), Decimal(reader, "TOTAL"),
                NullableText(reader, "OBSERVACION"), Text(reader, "CODIGO_ESTADO"), Bytes(reader, "ROW_VERSION"),
                Long(reader, "ID_ORDEN_COMPRA_DETALLE"), Long(reader, "ID_PRODUCTO"),
                Text(reader, "CODIGO_PRODUCTO"), Text(reader, "PRODUCTO"), Decimal(reader, "CANTIDAD_PEDIDA"),
                Decimal(reader, "CANTIDAD_RECIBIDA"), Decimal(reader, "CANTIDAD_PENDIENTE"),
                Decimal(reader, "COSTO_UNITARIO"), Decimal(reader, "PORCENTAJE_DESCUENTO"),
                Decimal(reader, "PORCENTAJE_IMPUESTO"), Decimal(reader, "SUBTOTAL_RENGLON"),
                Decimal(reader, "IMPORTE_DESCUENTO"), Decimal(reader, "IMPORTE_IMPUESTO"),
                Decimal(reader, "TOTAL_RENGLON"), Text(reader, "SIMBOLO")));
        var receipts = new List<GoodsReceiptLine>();
        if (await reader.NextResultAsync(cancellationToken)) while (await reader.ReadAsync(cancellationToken))
            receipts.Add(new(Long(reader, "ID_RECEPCION"), Long(reader, "ID_ORDEN_COMPRA"),
                NullableLong(reader, "ID_MOVIMIENTO"), NullableLong(reader, "ID_MOVIMIENTO_REVERSION"),
                Text(reader, "CODIGO"), Date(reader, "FECHA_RECEPCION_UTC"),
                NullableText(reader, "NUMERO_REMITO"), NullableText(reader, "OBSERVACION"),
                NullableText(reader, "MOTIVO_REVERSION"), NullableDate(reader, "FECHA_REVERSION_UTC"),
                NullableLong(reader, "ID_USUARIO_REVERSION"), Text(reader, "CODIGO_ESTADO"), Bytes(reader, "ROW_VERSION"),
                Long(reader, "ID_RECEPCION_DETALLE"),
                Long(reader, "ID_ORDEN_COMPRA_DETALLE"), Long(reader, "ID_PRODUCTO"),
                Text(reader, "CODIGO_PRODUCTO"), Text(reader, "PRODUCTO"), Decimal(reader, "CANTIDAD_ACEPTADA"),
                Decimal(reader, "CANTIDAD_RECHAZADA"), Decimal(reader, "CANTIDAD_DANADA"),
                NullableText(reader, "MOTIVO_DIFERENCIA"), Text(reader, "SIMBOLO")));
        var contacts = new List<SupplierContact>();
        if (await reader.NextResultAsync(cancellationToken)) while (await reader.ReadAsync(cancellationToken))
            contacts.Add(new(Long(reader, "ID_PROVEEDOR_CONTACTO"), Long(reader, "ID_PROVEEDOR"),
                Text(reader, "NOMBRE"), NullableText(reader, "CARGO"), NullableText(reader, "EMAIL"),
                NullableText(reader, "TELEFONO"), Bool(reader, "ES_PRINCIPAL"), Text(reader, "CODIGO_ESTADO"),
                Bytes(reader, "ROW_VERSION")));
        await reader.DisposeAsync();
        Ensure(outputs);
        await AuditAsync(companyCode, userId, sessionId, "COMPRAS_CONSULTAR", "CONSULTAR", "SP_COMPRAS_GET",
            started, suppliers.Count + orders.Count + receipts.Count, cancellationToken);
        return new(suppliers, contacts, products, warehouses, orders, receipts);
    }

    /// <inheritdoc />
    public Task<long> SaveSupplierAsync(long companyId, string companyCode, long userId, long sessionId,
        SupplierChange change, CancellationToken cancellationToken) => SaveAsync(companyId, companyCode, userId,
        sessionId, "PROVEEDOR_GUARDAR", "SP_PROVEEDOR_SAVE", "@O_ID_PROVEEDOR", change.SupplierId, command =>
        {
            Id(command, "@I_ID_PROVEEDOR", change.SupplierId); Text(command, "@I_RAZON_SOCIAL", 200, change.LegalName);
            Text(command, "@I_NOMBRE_COMERCIAL", 200, change.TradeName); Text(command, "@I_CUIT", 30, change.TaxId);
            Text(command, "@I_CORREO", 254, change.Email); Text(command, "@I_TELEFONO", 50, change.Phone);
            Text(command, "@I_CONDICION_PAGO", 150, change.PaymentTerms);
            Text(command, "@I_OBSERVACION", 1000, change.Observation);
            Text(command, "@I_CODIGO_ESTADO", 30, change.StatusCode); Version(command, change.RowVersion);
            Text(command, "@I_CONTACTOS_JSON", -1, JsonSerializer.Serialize(change.Contacts));
        }, cancellationToken);

    /// <inheritdoc />
    public Task<long> SaveOrderAsync(long companyId, string companyCode, long userId, long sessionId,
        PurchaseOrderChange change, CancellationToken cancellationToken) => SaveAsync(companyId, companyCode,
        userId, sessionId, "ORDEN_COMPRA_GUARDAR", "SP_ORDEN_COMPRA_SAVE", "@O_ID_ORDEN_COMPRA",
        change.PurchaseOrderId, command =>
        {
            Id(command, "@I_ID_ORDEN_COMPRA", change.PurchaseOrderId);
            command.Parameters.Add("@I_ID_PROVEEDOR", SqlDbType.BigInt).Value = change.SupplierId;
            command.Parameters.Add("@I_ID_DEPOSITO", SqlDbType.BigInt).Value = change.WarehouseId;
            command.Parameters.Add("@I_FECHA_ORDEN_UTC", SqlDbType.DateTime2).Value = change.OrderDateUtc;
            DateOnly(command, "@I_FECHA_ENTREGA_ESPERADA", change.ExpectedDeliveryDate);
            Text(command, "@I_MONEDA", 3, change.Currency); Text(command, "@I_OBSERVACION", 1000, change.Observation);
            Text(command, "@I_DETALLES_JSON", -1, JsonSerializer.Serialize(change.Details));
            Version(command, change.RowVersion);
        }, cancellationToken);

    /// <inheritdoc />
    public Task SubmitOrderAsync(long companyId, string companyCode, long userId, long sessionId,
        long purchaseOrderId, byte[] rowVersion, CancellationToken cancellationToken) => TransitionAsync(companyId,
        companyCode, userId, sessionId, "ORDEN_COMPRA_ENVIAR", "SP_ORDEN_COMPRA_SUBMIT", purchaseOrderId,
        rowVersion, cancellationToken);

    /// <inheritdoc />
    public Task ApproveOrderAsync(long companyId, string companyCode, long userId, long sessionId,
        long purchaseOrderId, byte[] rowVersion, CancellationToken cancellationToken) => TransitionAsync(companyId,
        companyCode, userId, sessionId, "ORDEN_COMPRA_APROBAR", "SP_ORDEN_COMPRA_APPROVE", purchaseOrderId,
        rowVersion, cancellationToken);

    /// <inheritdoc />
    public Task CancelOrderAsync(long companyId, string companyCode, long userId, long sessionId,
        long purchaseOrderId, byte[] rowVersion, CancellationToken cancellationToken) => TransitionAsync(companyId,
        companyCode, userId, sessionId, "ORDEN_COMPRA_CANCELAR", "SP_ORDEN_COMPRA_CANCEL", purchaseOrderId,
        rowVersion, cancellationToken);

    /// <inheritdoc />
    public Task CloseOrderAsync(long companyId, string companyCode, long userId, long sessionId,
        long purchaseOrderId, string reason, byte[] rowVersion, CancellationToken cancellationToken) =>
        TransitionAsync(companyId, companyCode, userId, sessionId, "ORDEN_COMPRA_CERRAR",
            "SP_ORDEN_COMPRA_CLOSE", purchaseOrderId, rowVersion, cancellationToken,
            command => Text(command, "@I_MOTIVO_CIERRE", 500, reason));

    /// <inheritdoc />
    public Task<long> CreateReceiptAsync(long companyId, string companyCode, long userId, long sessionId,
        GoodsReceiptChange change, CancellationToken cancellationToken) => SaveAsync(companyId, companyCode, userId,
        sessionId, "RECEPCION_CONFIRMAR", "SP_RECEPCION_CREATE", "@O_ID_RECEPCION", null, command =>
        {
            command.Parameters.Add("@I_ID_ORDEN_COMPRA", SqlDbType.BigInt).Value = change.PurchaseOrderId;
            command.Parameters.Add("@I_FECHA_RECEPCION_UTC", SqlDbType.DateTime2).Value = change.ReceiptDateUtc;
            Text(command, "@I_NUMERO_REMITO", 80, change.SupplierDocumentNumber);
            Text(command, "@I_OBSERVACION", 1000, change.Observation);
            Text(command, "@I_DETALLES_JSON", -1, JsonSerializer.Serialize(change.Details));
            Version(command, change.OrderRowVersion);
        }, cancellationToken);

    /// <inheritdoc />
    public Task ReverseReceiptAsync(long companyId, string companyCode, long userId, long sessionId,
        long goodsReceiptId, string reason, byte[] rowVersion, CancellationToken cancellationToken) =>
        TransitionAsync(companyId, companyCode, userId, sessionId, "RECEPCION_REVERSAR", "SP_RECEPCION_REVERSE",
            goodsReceiptId, rowVersion, cancellationToken, command => Text(command, "@I_MOTIVO_REVERSION", 500, reason),
            "@I_ID_RECEPCION");

    /// <summary>Ejecuta el alta o la edición de un proveedor, una orden o una recepción y registra la operación.</summary>
    /// <param name="companyId">Empresa habilitada en la sesión.</param>
    /// <param name="companyCode">Código usado para resolver la base de datos de la empresa.</param>
    /// <param name="userId">Usuario responsable de la modificación.</param>
    /// <param name="sessionId">Sesión responsable de la modificación.</param>
    /// <param name="functionality">Funcionalidad registrada en la auditoría.</param>
    /// <param name="procedure">Procedimiento de Compras que realiza el cambio.</param>
    /// <param name="outputName">Nombre del parámetro que devuelve el identificador persistido.</param>
    /// <param name="currentId">Identificador existente; nulo cuando se crea el registro.</param>
    /// <param name="parameters">Acción que incorpora los datos específicos de la operación.</param>
    /// <param name="cancellationToken">Cancelación de la operación de base de datos.</param>
    /// <returns>Identificador devuelto por el procedimiento después de confirmar el cambio.</returns>
    /// <exception cref="PurchasingOperationException">El procedimiento rechazó la operación.</exception>
    private async Task<long> SaveAsync(long companyId, string companyCode, long userId, long sessionId,
        string functionality, string procedure, string outputName, long? currentId, Action<SqlCommand> parameters,
        CancellationToken cancellationToken)
    {
        var started = DateTimeOffset.UtcNow;
        await using var connection = new SqlConnection(databases.GetConnectionString(companyCode));
        await using var command = Command(connection, procedure);
        Context(command, companyId, userId, sessionId);
        parameters(command);

        var id = command.Parameters.Add(outputName, SqlDbType.BigInt);
        id.Direction = ParameterDirection.Output;
        var outputs = Outputs(command, true);

        await connection.OpenAsync(cancellationToken);
        await command.ExecuteNonQueryAsync(cancellationToken);
        Ensure(outputs);

        var result = Convert.ToInt64(id.Value, System.Globalization.CultureInfo.InvariantCulture);
        await AuditAsync(companyCode, userId, sessionId, functionality, currentId is null ? "CREAR" : "MODIFICAR",
            procedure, started, Rows(outputs), cancellationToken);
        return result;
    }

    /// <summary>Cambia el estado de una orden o revierte una recepción con control de versión y auditoría.</summary>
    /// <param name="companyId">Empresa habilitada en la sesión.</param>
    /// <param name="companyCode">Código usado para resolver la base de datos de la empresa.</param>
    /// <param name="userId">Usuario responsable del cambio.</param>
    /// <param name="sessionId">Sesión responsable del cambio.</param>
    /// <param name="functionality">Funcionalidad registrada en la auditoría.</param>
    /// <param name="procedure">Procedimiento que efectúa la transición.</param>
    /// <param name="orderId">Identificador de la orden o recepción afectada.</param>
    /// <param name="rowVersion">Versión leída previamente para evitar cambios concurrentes.</param>
    /// <param name="cancellationToken">Cancelación de la operación de base de datos.</param>
    /// <param name="extraParameters">Parámetros adicionales, como el motivo de cierre o reversión.</param>
    /// <param name="idParameter">Nombre del parámetro de identidad requerido por el procedimiento.</param>
    /// <returns>Tarea que finaliza cuando la transición y su auditoría quedan persistidas.</returns>
    /// <exception cref="PurchasingOperationException">El procedimiento rechazó la transición.</exception>
    private async Task TransitionAsync(long companyId, string companyCode, long userId, long sessionId,
        string functionality, string procedure, long orderId, byte[] rowVersion,
        CancellationToken cancellationToken, Action<SqlCommand>? extraParameters = null,
        string idParameter = "@I_ID_ORDEN_COMPRA")
    {
        var started = DateTimeOffset.UtcNow;
        await using var connection = new SqlConnection(databases.GetConnectionString(companyCode));
        await using var command = Command(connection, procedure);
        Context(command, companyId, userId, sessionId);
        command.Parameters.Add(idParameter, SqlDbType.BigInt).Value = orderId;
        Version(command, rowVersion);
        extraParameters?.Invoke(command);

        var outputs = Outputs(command, true);

        await connection.OpenAsync(cancellationToken);
        await command.ExecuteNonQueryAsync(cancellationToken);
        Ensure(outputs);

        await AuditAsync(companyCode, userId, sessionId, functionality, "MODIFICAR", procedure, started,
            Rows(outputs), cancellationToken);
    }

    /// <summary>Guarda la evidencia de una operación de Compras confirmada por SQL Server.</summary>
    /// <param name="companyCode">Empresa en cuya base se ejecutó el procedimiento.</param>
    /// <param name="userId">Usuario que ejecutó la operación.</param>
    /// <param name="sessionId">Sesión que ejecutó la operación.</param>
    /// <param name="functionality">Funcionalidad auditada.</param>
    /// <param name="action">Acción auditada, como crear, modificar o consultar.</param>
    /// <param name="procedure">Procedimiento ejecutado.</param>
    /// <param name="started">Instante de inicio de la ejecución.</param>
    /// <param name="rows">Cantidad de filas afectadas o consultadas.</param>
    /// <param name="cancellationToken">Cancelación de la escritura de auditoría.</param>
    /// <returns>Tarea que finaliza cuando se registra la evidencia.</returns>
    private Task AuditAsync(string companyCode, long userId, long sessionId, string functionality, string action,
        string procedure, DateTimeOffset started, int rows, CancellationToken cancellationToken) =>
        auditWriter.WriteAsync(new StoredProcedureAuditEntry(companyCode, "COMPRAS", userId, sessionId,
            functionality, action, "COMPRAS", procedure, "{}", started, DateTimeOffset.UtcNow, rows, "EXITOSO",
            null, null, null, "OxiTigre.Api", Guid.NewGuid()), cancellationToken);

    /// <summary>Prepara un procedimiento del esquema COMPRAS sobre la conexión indicada.</summary>
    /// <param name="connection">Conexión de la empresa seleccionada.</param>
    /// <param name="procedure">Nombre del procedimiento sin el esquema.</param>
    /// <returns>Comando listo para recibir parámetros.</returns>
    private static SqlCommand Command(SqlConnection connection, string procedure) =>
        new($"[COMPRAS].[{procedure}]", connection) { CommandType = CommandType.StoredProcedure };

    /// <summary>Agrega la identidad de empresa, usuario y sesión exigida por los procedimientos.</summary>
    /// <param name="command">Comando al que se agregan los parámetros.</param>
    /// <param name="companyId">Empresa autorizada.</param>
    /// <param name="userId">Usuario autenticado.</param>
    /// <param name="sessionId">Sesión autenticada.</param>
    private static void Context(SqlCommand command, long companyId, long userId, long sessionId)
    {
        command.Parameters.Add("@I_ID_EMPRESA", SqlDbType.BigInt).Value = companyId;
        command.Parameters.Add("@S_ID_USUARIO", SqlDbType.BigInt).Value = userId;
        command.Parameters.Add("@S_ID_SESION", SqlDbType.BigInt).Value = sessionId;
    }

    /// <summary>Declara la salida funcional del procedimiento y, si corresponde, sus filas afectadas.</summary>
    /// <param name="command">Comando que recibe los parámetros de salida.</param>
    /// <param name="rows">Indica si el procedimiento devuelve filas afectadas.</param>
    /// <returns>Referencias a los parámetros para comprobar el resultado tras la ejecución.</returns>
    private static OutputParameters Outputs(SqlCommand command, bool rows = false)
    {
        var code = command.Parameters.Add("@O_CODIGO_ERROR", SqlDbType.BigInt);
        code.Direction = ParameterDirection.Output;

        var message = command.Parameters.Add("@O_MENSAJE", SqlDbType.NVarChar, 4000);
        message.Direction = ParameterDirection.Output;

        SqlParameter? affected = null;

        if (rows)
        {
            affected = command.Parameters.Add("@O_FILAS_AFECTADAS", SqlDbType.Int);
            affected.Direction = ParameterDirection.Output;
        }

        return new(code, message, affected);
    }

    /// <summary>Convierte un error funcional comunicado por SQL en una excepción de Compras.</summary>
    /// <param name="output">Parámetros leídos después de ejecutar el procedimiento.</param>
    /// <exception cref="PurchasingOperationException">El procedimiento devolvió un código de error.</exception>
    private static void Ensure(OutputParameters output)
    {
        if (output.Code.Value is null or DBNull)
        {
            return;
        }

        throw new PurchasingOperationException(Convert.ToInt64(output.Code.Value,
            System.Globalization.CultureInfo.InvariantCulture), Convert.ToString(output.Message.Value,
            System.Globalization.CultureInfo.InvariantCulture) ?? "La operación de Compras no pudo completarse.");
    }

    /// <summary>Obtiene las filas afectadas comunicadas por el procedimiento; usa cero si no hubo salida.</summary>
    /// <param name="outputs">Parámetros de salida de la ejecución.</param>
    /// <returns>Cantidad de filas afectadas para la auditoría.</returns>
    private static int Rows(OutputParameters outputs) => outputs.Rows?.Value is null or DBNull ? 0 :
        Convert.ToInt32(outputs.Rows.Value, System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>Agrega un identificador nullable como parámetro BIGINT de entrada.</summary>
    /// <param name="command">Comando que recibe el valor.</param>
    /// <param name="name">Nombre del parámetro.</param>
    /// <param name="value">Identificador o nulo en un alta.</param>
    private static void Id(SqlCommand command, string name, long? value) =>
        command.Parameters.Add(name, SqlDbType.BigInt).Value = value ?? (object)DBNull.Value;

    /// <summary>Agrega texto opcional respetando el tamaño declarado por el procedimiento.</summary>
    /// <param name="command">Comando que recibe el valor.</param>
    /// <param name="name">Nombre del parámetro.</param>
    /// <param name="size">Longitud de SQL; -1 indica NVARCHAR(MAX).</param>
    /// <param name="value">Texto o valor nulo.</param>
    private static void Text(SqlCommand command, string name, int size, object? value) =>
        command.Parameters.Add(name, SqlDbType.NVarChar, size).Value = value ?? DBNull.Value;

    /// <summary>Agrega una fecha opcional sin componente horario.</summary>
    /// <param name="command">Comando que recibe el valor.</param>
    /// <param name="name">Nombre del parámetro.</param>
    /// <param name="value">Fecha de entrega esperada, si se informó.</param>
    private static void DateOnly(SqlCommand command, string name, System.DateOnly? value) =>
        command.Parameters.Add(name, SqlDbType.Date).Value = value?.ToDateTime(TimeOnly.MinValue) ?? (object)DBNull.Value;

    /// <summary>Agrega la versión de fila que SQL usa para detectar ediciones concurrentes.</summary>
    /// <param name="command">Comando que recibe el valor.</param>
    /// <param name="value">Versión de ocho bytes o nulo para crear.</param>
    private static void Version(SqlCommand command, byte[]? value) =>
        command.Parameters.Add("@I_ROW_VERSION", SqlDbType.Binary, 8).Value = value ?? (object)DBNull.Value;
    /// <summary>Lee un identificador BIGINT obligatorio por nombre de columna.</summary>
    /// <param name="reader">Lector situado sobre una fila.</param>
    /// <param name="name">Nombre de la columna.</param>
    /// <returns>Identificador leído.</returns>
    private static long Long(SqlDataReader reader, string name) => reader.GetInt64(reader.GetOrdinal(name));

    /// <summary>Lee un identificador BIGINT que puede ser nulo.</summary>
    /// <param name="reader">Lector situado sobre una fila.</param>
    /// <param name="name">Nombre de la columna.</param>
    /// <returns>Identificador leído o nulo.</returns>
    private static long? NullableLong(SqlDataReader reader, string name)
    {
        var index = reader.GetOrdinal(name);
        return reader.IsDBNull(index) ? null : reader.GetInt64(index);
    }

    /// <summary>Lee una cadena obligatoria por nombre de columna.</summary>
    /// <param name="reader">Lector situado sobre una fila.</param>
    /// <param name="name">Nombre de la columna.</param>
    /// <returns>Texto leído.</returns>
    private static string Text(SqlDataReader reader, string name) => reader.GetString(reader.GetOrdinal(name));

    /// <summary>Lee una cadena que puede ser nula.</summary>
    /// <param name="reader">Lector situado sobre una fila.</param>
    /// <param name="name">Nombre de la columna.</param>
    /// <returns>Texto leído o nulo.</returns>
    private static string? NullableText(SqlDataReader reader, string name)
    {
        var index = reader.GetOrdinal(name);
        return reader.IsDBNull(index) ? null : reader.GetString(index);
    }

    /// <summary>Lee un importe o cantidad DECIMAL obligatorio.</summary>
    /// <param name="reader">Lector situado sobre una fila.</param>
    /// <param name="name">Nombre de la columna.</param>
    /// <returns>Valor decimal leído.</returns>
    private static decimal Decimal(SqlDataReader reader, string name) => reader.GetDecimal(reader.GetOrdinal(name));

    /// <summary>Lee un indicador BIT obligatorio.</summary>
    /// <param name="reader">Lector situado sobre una fila.</param>
    /// <param name="name">Nombre de la columna.</param>
    /// <returns>Valor lógico leído.</returns>
    private static bool Bool(SqlDataReader reader, string name) => reader.GetBoolean(reader.GetOrdinal(name));

    /// <summary>Lee una fecha y hora obligatoria.</summary>
    /// <param name="reader">Lector situado sobre una fila.</param>
    /// <param name="name">Nombre de la columna.</param>
    /// <returns>Fecha y hora leída.</returns>
    private static DateTime Date(SqlDataReader reader, string name) => reader.GetDateTime(reader.GetOrdinal(name));

    /// <summary>Lee una fecha y hora que puede ser nula.</summary>
    /// <param name="reader">Lector situado sobre una fila.</param>
    /// <param name="name">Nombre de la columna.</param>
    /// <returns>Fecha y hora leída o nulo.</returns>
    private static DateTime? NullableDate(SqlDataReader reader, string name)
    {
        var index = reader.GetOrdinal(name);
        return reader.IsDBNull(index) ? null : reader.GetDateTime(index);
    }

    /// <summary>Lee una fecha sin hora que puede ser nula.</summary>
    /// <param name="reader">Lector situado sobre una fila.</param>
    /// <param name="name">Nombre de la columna.</param>
    /// <returns>Fecha leída o nulo.</returns>
    private static System.DateOnly? NullableDateOnly(SqlDataReader reader, string name)
    {
        var index = reader.GetOrdinal(name);
        return reader.IsDBNull(index) ? null : System.DateOnly.FromDateTime(reader.GetDateTime(index));
    }

    /// <summary>Lee la versión binaria usada en el control de concurrencia.</summary>
    /// <param name="reader">Lector situado sobre una fila.</param>
    /// <param name="name">Nombre de la columna.</param>
    /// <returns>Versión de fila leída.</returns>
    private static byte[] Bytes(SqlDataReader reader, string name) => (byte[])reader.GetValue(reader.GetOrdinal(name));

    /// <summary>Conserva las referencias a la respuesta funcional y a las filas afectadas de un SP.</summary>
    /// <param name="Code">Código de error funcional; nulo significa éxito.</param>
    /// <param name="Message">Mensaje funcional devuelto por el SP.</param>
    /// <param name="Rows">Cantidad opcional de filas afectadas.</param>
    private sealed record OutputParameters(SqlParameter Code, SqlParameter Message, SqlParameter? Rows);
}
