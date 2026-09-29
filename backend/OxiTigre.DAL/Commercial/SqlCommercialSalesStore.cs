/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.DAL.Commercial.SqlCommercialSalesStore
Archivo: SqlCommercialSalesStore.cs | Versión: 1.3.0 | Fecha: 2026-08-28 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Persiste listas, promociones, pedidos, transiciones y ventas mediante procedimientos auditados.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-21 | FABRICA | Agustin Omar Cauzi | Persistencia y lectura de promociones aplicadas.
Historial: 1.2.0 | 2026-08-28 | FABRICA | Agustin Omar Cauzi | Persistencia de servicios y activos del pedido.
Historial: 1.3.0 | 2026-08-28 | FABRICA | Agustin Omar Cauzi | Persistencia de modalidades logísticas y devolución prevista.
===============================================================================
*/
using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using OxiTigre.BLL.Commercial;
using OxiTigre.DAL.MultiCompany;
using OxiTigre.DAL.StoredProcedures;

namespace OxiTigre.DAL.Commercial;

/// <summary>Implementa el flujo comercial en SQL Server y traduce sus salidas funcionales.</summary>
public sealed class SqlCommercialSalesStore(
    CompanyDatabaseRegistry databases,
    IStoredProcedureAuditWriter auditWriter
) : ICommercialSalesStore
{
    /// <inheritdoc />
    public async Task<SalesSnapshot> GetAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        CancellationToken cancellationToken
    )
    {
        var started = DateTimeOffset.UtcNow;
        await using var connection = new SqlConnection(databases.GetConnectionString(companyCode));
        await using var command = Command(connection, "SP_VENTAS_GET");
        Context(command, companyId, userId, sessionId);
        var outputs = Outputs(command);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var clients = new List<SalesClient>();
        while (await reader.ReadAsync(cancellationToken))
            clients.Add(
                new(Long(reader, "ID_CLIENTE"), Text(reader, "CODIGO"), Text(reader, "NOMBRE"))
            );
        var lists = new List<PriceList>();
        if (await reader.NextResultAsync(cancellationToken))
            while (await reader.ReadAsync(cancellationToken))
                lists.Add(
                    new(
                        Long(reader, "ID_LISTA_PRECIO"),
                        Text(reader, "CODIGO"),
                        Text(reader, "NOMBRE"),
                        Text(reader, "MONEDA"),
                        DateOnly(reader, "FECHA_VIGENCIA_DESDE"),
                        DateOnly(reader, "FECHA_VIGENCIA_HASTA"),
                        Text(reader, "CODIGO_ESTADO"),
                        Bytes(reader, "ROW_VERSION")
                    )
                );
        var prices = new List<PriceListProduct>();
        if (await reader.NextResultAsync(cancellationToken))
            while (await reader.ReadAsync(cancellationToken))
                prices.Add(
                    new(
                        Long(reader, "ID_LISTA_PRECIO_PRODUCTO"),
                        Long(reader, "ID_LISTA_PRECIO"),
                        Long(reader, "ID_PRODUCTO"),
                        Text(reader, "CODIGO_PRODUCTO"),
                        Text(reader, "PRODUCTO"),
                        Decimal(reader, "PRECIO_UNITARIO"),
                        Text(reader, "CODIGO_ESTADO"),
                        Bytes(reader, "ROW_VERSION")
                    )
                );
        var products = new List<SalesProduct>();
        if (await reader.NextResultAsync(cancellationToken))
            while (await reader.ReadAsync(cancellationToken))
                products.Add(
                    new(
                        Long(reader, "ID_PRODUCTO"),
                        Text(reader, "CODIGO"),
                        Text(reader, "NOMBRE"),
                        Text(reader, "SIMBOLO"),
                        Text(reader, "TIPO_ITEM")
                    )
                );
        var warehouses = new List<SalesWarehouse>();
        if (await reader.NextResultAsync(cancellationToken))
            while (await reader.ReadAsync(cancellationToken))
                warehouses.Add(
                    new(Long(reader, "ID_DEPOSITO"), Text(reader, "CODIGO"), Text(reader, "NOMBRE"))
                );
        var promotions = new List<Promotion>();
        if (await reader.NextResultAsync(cancellationToken))
            while (await reader.ReadAsync(cancellationToken))
                promotions.Add(
                    new(
                        Long(reader, "ID_PROMOCION"),
                        Long(reader, "ID_PRODUCTO"),
                        Text(reader, "CODIGO_PRODUCTO"),
                        Text(reader, "PRODUCTO"),
                        Text(reader, "CODIGO"),
                        Text(reader, "NOMBRE"),
                        NullableText(reader, "DESCRIPCION"),
                        Text(reader, "TIPO_PROMOCION"),
                        Decimal(reader, "CANTIDAD_REQUERIDA"),
                        NullableDecimal(reader, "CANTIDAD_PAGADA"),
                        NullableDecimal(reader, "CANTIDAD_BONIFICADA"),
                        NullableDecimal(reader, "PORCENTAJE_DESCUENTO"),
                        NullableDecimal(reader, "PRECIO_PAQUETE"),
                        DateOnly(reader, "FECHA_VIGENCIA_DESDE"),
                        DateOnly(reader, "FECHA_VIGENCIA_HASTA"),
                        Text(reader, "CODIGO_ESTADO"),
                        Bytes(reader, "ROW_VERSION")
                    )
                );
        var orders = new List<OrderLine>();
        if (await reader.NextResultAsync(cancellationToken))
            while (await reader.ReadAsync(cancellationToken))
                orders.Add(
                    new(
                        Long(reader, "ID_PEDIDO"),
                        Long(reader, "ID_CLIENTE"),
                        NullableLong(reader, "ID_LISTA_PRECIO"),
                        Text(reader, "CODIGO"),
                        Date(reader, "FECHA_PEDIDO_UTC"),
                        Text(reader, "CLIENTE"),
                        Text(reader, "MONEDA"),
                        Decimal(reader, "SUBTOTAL"),
                        Decimal(reader, "TOTAL_DESCUENTO"),
                        Decimal(reader, "TOTAL_IMPUESTO"),
                        Decimal(reader, "TOTAL"),
                        NullableText(reader, "OBSERVACION"),
                        Text(reader, "CODIGO_ESTADO"),
                        Bytes(reader, "ROW_VERSION"),
                        Long(reader, "ID_PEDIDO_DETALLE"),
                        Long(reader, "ID_PRODUCTO"),
                        Text(reader, "CODIGO_PRODUCTO"),
                        Text(reader, "PRODUCTO"),
                        NullableLong(reader, "ID_DEPOSITO"),
                        NullableText(reader, "DEPOSITO"),
                        Decimal(reader, "CANTIDAD"),
                        Decimal(reader, "PRECIO_UNITARIO"),
                        Decimal(reader, "PORCENTAJE_DESCUENTO"),
                        Decimal(reader, "PORCENTAJE_IMPUESTO"),
                        Decimal(reader, "SUBTOTAL_RENGLON"),
                        Decimal(reader, "IMPORTE_DESCUENTO"),
                        Decimal(reader, "IMPORTE_IMPUESTO"),
                        Decimal(reader, "TOTAL_RENGLON"),
                        Text(reader, "SIMBOLO"),
                        NullableLong(reader, "ID_PROMOCION"),
                        NullableText(reader, "CODIGO_PROMOCION"),
                        NullableText(reader, "NOMBRE_PROMOCION")
                    )
                );
        var sales = new List<SaleLine>();
        if (await reader.NextResultAsync(cancellationToken))
            while (await reader.ReadAsync(cancellationToken))
                sales.Add(
                    new(
                        Long(reader, "ID_VENTA"),
                        Long(reader, "ID_PEDIDO"),
                        Long(reader, "ID_CLIENTE"),
                        NullableLong(reader, "ID_MOVIMIENTO"),
                        Text(reader, "CODIGO"),
                        Date(reader, "FECHA_VENTA_UTC"),
                        Text(reader, "CLIENTE"),
                        Text(reader, "MONEDA"),
                        Decimal(reader, "SUBTOTAL"),
                        Decimal(reader, "TOTAL_DESCUENTO"),
                        Decimal(reader, "TOTAL_IMPUESTO"),
                        Decimal(reader, "TOTAL"),
                        NullableText(reader, "OBSERVACION"),
                        Text(reader, "CODIGO_ESTADO"),
                        Long(reader, "ID_VENTA_DETALLE"),
                        Long(reader, "ID_PRODUCTO"),
                        Text(reader, "CODIGO_PRODUCTO"),
                        Text(reader, "PRODUCTO"),
                        NullableLong(reader, "ID_DEPOSITO"),
                        NullableText(reader, "DEPOSITO"),
                        Decimal(reader, "CANTIDAD"),
                        Decimal(reader, "PRECIO_UNITARIO"),
                        Decimal(reader, "PORCENTAJE_DESCUENTO"),
                        Decimal(reader, "IMPORTE_DESCUENTO"),
                        Decimal(reader, "PORCENTAJE_IMPUESTO"),
                        Decimal(reader, "TOTAL_RENGLON"),
                        Text(reader, "SIMBOLO"),
                        NullableLong(reader, "ID_PROMOCION"),
                        NullableText(reader, "CODIGO_PROMOCION"),
                        NullableText(reader, "NOMBRE_PROMOCION")
                    )
                );
        var assets = new List<SalesAsset>();
        if (await reader.NextResultAsync(cancellationToken))
            while (await reader.ReadAsync(cancellationToken))
                assets.Add(
                    new(
                        Long(reader, "ID_ACTIVO"),
                        NullableLong(reader, "ID_CLIENTE_PROPIETARIO"),
                        Text(reader, "CODIGO"),
                        Text(reader, "NUMERO_SERIE"),
                        Text(reader, "PRODUCTO"),
                        Text(reader, "PROPIETARIO"),
                        Text(reader, "CONDICION_ACTUAL"),
                        Text(reader, "CODIGO_ESTADO")
                    )
                );
        var orderAssets = new List<OrderAsset>();
        if (await reader.NextResultAsync(cancellationToken))
            while (await reader.ReadAsync(cancellationToken))
                orderAssets.Add(
                    new(
                        Long(reader, "ID_PEDIDO_ACTIVO"),
                        Long(reader, "ID_PEDIDO"),
                        Long(reader, "ID_ACTIVO"),
                        Long(reader, "ID_PRODUCTO_RENGLON"),
                        Text(reader, "ACTIVO"),
                        Text(reader, "NUMERO_SERIE"),
                        Text(reader, "PRODUCTO_RENGLON"),
                        Text(reader, "TIPO_VINCULO"),
                        Text(reader, "MODALIDAD_RETORNO"),
                        Text(reader, "OBSERVACION"),
                        Text(reader, "MODALIDAD_INGRESO"),
                        DateOnly(reader, "FECHA_DEVOLUCION_PREVISTA")
                    )
                );
        await reader.DisposeAsync();
        Success(outputs);
        await Audit(
            companyCode,
            userId,
            sessionId,
            "VENTAS_CONSULTAR",
            "CONSULTAR",
            "SP_VENTAS_GET",
            started,
            orders.Count + sales.Count,
            cancellationToken
        );
        return new(
            clients,
            lists,
            prices,
            products,
            warehouses,
            promotions,
            orders,
            sales,
            assets,
            orderAssets
        );
    }

    /// <inheritdoc />
    public Task<long> SavePriceListAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        PriceListChange change,
        CancellationToken cancellationToken
    ) =>
        Save(
            companyId,
            companyCode,
            userId,
            sessionId,
            "LISTA_PRECIO_GUARDAR",
            "SP_LISTA_PRECIO_SAVE",
            "@O_ID_LISTA_PRECIO",
            command =>
            {
                Id(command, "@I_ID_LISTA_PRECIO", change.PriceListId);
                Text(command, "@I_CODIGO", 30, change.Code);
                Text(command, "@I_NOMBRE", 150, change.Name);
                Text(command, "@I_MONEDA", 3, change.Currency);
                Date(command, "@I_FECHA_VIGENCIA_DESDE", change.ValidFrom);
                Date(command, "@I_FECHA_VIGENCIA_HASTA", change.ValidUntil);
                Text(command, "@I_CODIGO_ESTADO", 30, change.StatusCode);
                Version(command, change.RowVersion);
            },
            cancellationToken
        );

    /// <inheritdoc />
    public Task<long> SavePriceAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        PriceListProductChange change,
        CancellationToken cancellationToken
    ) =>
        Save(
            companyId,
            companyCode,
            userId,
            sessionId,
            "PRECIO_GUARDAR",
            "SP_LISTA_PRECIO_PRODUCTO_SAVE",
            "@O_ID_LISTA_PRECIO_PRODUCTO",
            command =>
            {
                Id(command, "@I_ID_LISTA_PRECIO_PRODUCTO", change.PriceListProductId);
                command.Parameters.Add("@I_ID_LISTA_PRECIO", SqlDbType.BigInt).Value =
                    change.PriceListId;
                command.Parameters.Add("@I_ID_PRODUCTO", SqlDbType.BigInt).Value = change.ProductId;
                Money(command, "@I_PRECIO_UNITARIO", change.UnitPrice);
                Text(command, "@I_CODIGO_ESTADO", 30, change.StatusCode);
                Version(command, change.RowVersion);
            },
            cancellationToken
        );

    /// <inheritdoc />
    public Task<long> SavePromotionAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        PromotionChange change,
        CancellationToken cancellationToken
    ) =>
        Save(
            companyId,
            companyCode,
            userId,
            sessionId,
            "PROMOCION_GUARDAR",
            "SP_PROMOCION_SAVE",
            "@O_ID_PROMOCION",
            command =>
            {
                Id(command, "@I_ID_PROMOCION", change.PromotionId);
                command.Parameters.Add("@I_ID_PRODUCTO", SqlDbType.BigInt).Value = change.ProductId;
                Text(command, "@I_CODIGO", 30, change.Code);
                Text(command, "@I_NOMBRE", 150, change.Name);
                Text(command, "@I_DESCRIPCION", 500, change.Description);
                Text(command, "@I_TIPO_PROMOCION", 30, change.PromotionType);
                Number(command, "@I_CANTIDAD_REQUERIDA", 19, 4, change.RequiredQuantity);
                Number(command, "@I_CANTIDAD_PAGADA", 19, 4, change.PaidQuantity);
                Number(command, "@I_CANTIDAD_BONIFICADA", 19, 4, change.DiscountedQuantity);
                Number(command, "@I_PORCENTAJE_DESCUENTO", 9, 6, change.DiscountRate);
                Number(command, "@I_PRECIO_PAQUETE", 19, 4, change.PackagePrice);
                Date(command, "@I_FECHA_VIGENCIA_DESDE", change.ValidFrom);
                Date(command, "@I_FECHA_VIGENCIA_HASTA", change.ValidUntil);
                Text(command, "@I_CODIGO_ESTADO", 30, change.StatusCode);
                Version(command, change.RowVersion);
            },
            cancellationToken
        );

    /// <inheritdoc />
    public Task<long> SaveOrderAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        OrderChange change,
        CancellationToken cancellationToken
    ) =>
        Save(
            companyId,
            companyCode,
            userId,
            sessionId,
            "PEDIDO_GUARDAR",
            "SP_PEDIDO_SAVE",
            "@O_ID_PEDIDO",
            command =>
            {
                Id(command, "@I_ID_PEDIDO", change.OrderId);
                command.Parameters.Add("@I_ID_CLIENTE", SqlDbType.BigInt).Value = change.ClientId;
                Id(command, "@I_ID_LISTA_PRECIO", change.PriceListId);
                command.Parameters.Add("@I_FECHA_PEDIDO_UTC", SqlDbType.DateTime2).Value =
                    change.OrderDateUtc;
                Text(command, "@I_MONEDA", 3, change.Currency);
                Text(command, "@I_OBSERVACION", 500, change.Observation);
                Text(command, "@I_DETALLES_JSON", -1, JsonSerializer.Serialize(change.Details));
                Text(command, "@I_ACTIVOS_JSON", -1, JsonSerializer.Serialize(change.Assets ?? []));
                Version(command, change.RowVersion);
            },
            cancellationToken
        );

    /// <inheritdoc />
    public Task ConfirmOrderAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        long orderId,
        byte[] rowVersion,
        CancellationToken cancellationToken
    ) =>
        Execute(
            companyId,
            companyCode,
            userId,
            sessionId,
            "PEDIDO_CONFIRMAR",
            "SP_PEDIDO_CONFIRM",
            command =>
            {
                command.Parameters.Add("@I_ID_PEDIDO", SqlDbType.BigInt).Value = orderId;
                Version(command, rowVersion);
            },
            cancellationToken
        );

    /// <inheritdoc />
    public Task CancelOrderAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        long orderId,
        byte[] rowVersion,
        CancellationToken cancellationToken
    ) =>
        Execute(
            companyId,
            companyCode,
            userId,
            sessionId,
            "PEDIDO_CANCELAR",
            "SP_PEDIDO_CANCEL",
            command =>
            {
                command.Parameters.Add("@I_ID_PEDIDO", SqlDbType.BigInt).Value = orderId;
                Version(command, rowVersion);
            },
            cancellationToken
        );

    /// <inheritdoc />
    public Task<long> CreateSaleAsync(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        long orderId,
        DateTime saleDateUtc,
        byte[] rowVersion,
        CancellationToken cancellationToken
    ) =>
        Save(
            companyId,
            companyCode,
            userId,
            sessionId,
            "VENTA_CREAR",
            "SP_VENTA_CREATE_FROM_PEDIDO",
            "@O_ID_VENTA",
            command =>
            {
                command.Parameters.Add("@I_ID_PEDIDO", SqlDbType.BigInt).Value = orderId;
                command.Parameters.Add("@I_FECHA_VENTA_UTC", SqlDbType.DateTime2).Value =
                    saleDateUtc;
                Version(command, rowVersion);
            },
            cancellationToken
        );

    /// <summary>Ejecuta un guardado comercial mediante SP, comprueba el resultado y registra auditoría.</summary>
    /// <param name="companyId">Empresa autorizada para la operación.</param>
    /// <param name="companyCode">Código que selecciona la base de datos de la empresa.</param>
    /// <param name="userId">Usuario responsable del guardado.</param>
    /// <param name="sessionId">Sesión responsable del guardado.</param>
    /// <param name="functionality">Funcionalidad registrada en auditoría.</param>
    /// <param name="procedure">Procedimiento del esquema COMERCIAL que guarda el dato.</param>
    /// <param name="outputName">Nombre del parámetro de identificador resultante.</param>
    /// <param name="add">Acción que agrega los parámetros específicos del guardado.</param>
    /// <param name="cancellationToken">Cancelación de la operación de base de datos.</param>
    /// <returns>Identificador confirmado por el procedimiento.</returns>
    /// <exception cref="CommercialOperationException">El procedimiento rechazó el guardado.</exception>
    private async Task<long> Save(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        string functionality,
        string procedure,
        string outputName,
        Action<SqlCommand> add,
        CancellationToken cancellationToken
    )
    {
        var started = DateTimeOffset.UtcNow;
        await using var connection = new SqlConnection(databases.GetConnectionString(companyCode));
        await using var command = Command(connection, procedure);
        Context(command, companyId, userId, sessionId);
        add(command);
        var id = command.Parameters.Add(outputName, SqlDbType.BigInt);
        id.Direction = ParameterDirection.Output;
        var outputs = Outputs(command, true);
        await connection.OpenAsync(cancellationToken);
        await command.ExecuteNonQueryAsync(cancellationToken);
        Success(outputs);
        var result = Convert.ToInt64(id.Value, System.Globalization.CultureInfo.InvariantCulture);
        await Audit(
            companyCode,
            userId,
            sessionId,
            functionality,
            "GUARDAR",
            procedure,
            started,
            Convert.ToInt32(outputs.Rows!.Value, System.Globalization.CultureInfo.InvariantCulture),
            cancellationToken
        );
        return result;
    }

    /// <summary>Ejecuta una transición comercial sin identificador de retorno y registra su auditoría.</summary>
    /// <param name="companyId">Empresa autorizada para la operación.</param>
    /// <param name="companyCode">Código que selecciona la base de datos de la empresa.</param>
    /// <param name="userId">Usuario responsable del cambio.</param>
    /// <param name="sessionId">Sesión responsable del cambio.</param>
    /// <param name="functionality">Funcionalidad registrada en auditoría.</param>
    /// <param name="procedure">Procedimiento del esquema COMERCIAL que aplica el cambio.</param>
    /// <param name="add">Acción que agrega los parámetros específicos del cambio.</param>
    /// <param name="cancellationToken">Cancelación de la operación de base de datos.</param>
    /// <returns>Tarea que finaliza cuando la transición y su auditoría quedan persistidas.</returns>
    /// <exception cref="CommercialOperationException">El procedimiento rechazó la transición.</exception>
    private async Task Execute(
        long companyId,
        string companyCode,
        long userId,
        long sessionId,
        string functionality,
        string procedure,
        Action<SqlCommand> add,
        CancellationToken cancellationToken
    )
    {
        var started = DateTimeOffset.UtcNow;
        await using var connection = new SqlConnection(databases.GetConnectionString(companyCode));
        await using var command = Command(connection, procedure);
        Context(command, companyId, userId, sessionId);
        add(command);
        var outputs = Outputs(command, true);
        await connection.OpenAsync(cancellationToken);
        await command.ExecuteNonQueryAsync(cancellationToken);
        Success(outputs);
        await Audit(
            companyCode,
            userId,
            sessionId,
            functionality,
            "MODIFICAR",
            procedure,
            started,
            Convert.ToInt32(outputs.Rows!.Value, System.Globalization.CultureInfo.InvariantCulture),
            cancellationToken
        );
    }

    /// <summary>Conserva la evidencia de una operación comercial confirmada con usuario, sesión y SP.</summary>
    /// <param name="companyCode">Empresa cuya base procesó la operación.</param>
    /// <param name="userId">Usuario responsable.</param>
    /// <param name="sessionId">Sesión responsable.</param>
    /// <param name="functionality">Funcionalidad auditada.</param>
    /// <param name="action">Acción auditada.</param>
    /// <param name="procedure">Procedimiento ejecutado.</param>
    /// <param name="started">Instante de inicio de la ejecución.</param>
    /// <param name="rows">Filas afectadas o consultadas.</param>
    /// <param name="cancellationToken">Cancelación del registro.</param>
    /// <returns>Tarea que finaliza al persistir la evidencia.</returns>
    private Task Audit(
        string companyCode,
        long userId,
        long sessionId,
        string functionality,
        string action,
        string procedure,
        DateTimeOffset started,
        int rows,
        CancellationToken cancellationToken
    ) =>
        auditWriter.WriteAsync(
            new StoredProcedureAuditEntry(
                companyCode,
                "COMERCIAL",
                userId,
                sessionId,
                functionality,
                action,
                "COMERCIAL",
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

    /// <summary>Prepara un procedimiento del esquema COMERCIAL.</summary>
    /// <param name="connection">Conexión de la empresa seleccionada.</param>
    /// <param name="name">Nombre del procedimiento sin el esquema.</param>
    /// <returns>Comando listo para recibir parámetros.</returns>
    private static SqlCommand Command(SqlConnection connection, string name) =>
        new($"[COMERCIAL].[{name}]", connection) { CommandType = CommandType.StoredProcedure };

    /// <summary>Agrega la empresa, usuario y sesión exigidos por los SP comerciales.</summary>
    /// <param name="command">Comando que recibe los parámetros.</param>
    /// <param name="companyId">Empresa autorizada.</param>
    /// <param name="userId">Usuario autenticado.</param>
    /// <param name="sessionId">Sesión autenticada.</param>
    private static void Context(SqlCommand command, long companyId, long userId, long sessionId)
    {
        command.Parameters.Add("@I_ID_EMPRESA", SqlDbType.BigInt).Value = companyId;
        command.Parameters.Add("@S_ID_SESION", SqlDbType.BigInt).Value = sessionId;
        command.Parameters.Add("@S_ID_USUARIO", SqlDbType.BigInt).Value = userId;
    }

    /// <summary>Declara el error funcional y, opcionalmente, las filas afectadas devueltas por el SP.</summary>
    /// <param name="command">Comando que recibe los parámetros de salida.</param>
    /// <param name="rows">Indica si el SP informa filas afectadas.</param>
    /// <returns>Referencias a las salidas para comprobar el resultado.</returns>
    private static OutputsResult Outputs(SqlCommand command, bool rows = false)
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

    /// <summary>Interpreta las salidas y rechaza un error funcional del SP.</summary>
    /// <param name="output">Salidas leídas después de ejecutar.</param>
    /// <exception cref="CommercialOperationException">El SP devolvió un código de error.</exception>
    private static void Success(OutputsResult output)
    {
        if (output.Code.Value is null or DBNull)
            return;
        throw new CommercialOperationException(
            Convert.ToInt64(output.Code.Value, System.Globalization.CultureInfo.InvariantCulture),
            Convert.ToString(
                output.Message.Value,
                System.Globalization.CultureInfo.InvariantCulture
            ) ?? "La operación comercial no pudo completarse."
        );
    }

    /// <summary>Agrega un identificador BIGINT nullable.</summary>
    /// <param name="command">Comando que recibe el parámetro.</param>
    /// <param name="name">Nombre SQL del parámetro.</param>
    /// <param name="value">Identificador o nulo en un alta.</param>
    private static void Id(SqlCommand command, string name, long? value) =>
        command.Parameters.Add(name, SqlDbType.BigInt).Value = value ?? (object)DBNull.Value;

    /// <summary>Agrega texto nullable con la longitud declarada por el SP.</summary>
    /// <param name="command">Comando que recibe el parámetro.</param>
    /// <param name="name">Nombre SQL del parámetro.</param>
    /// <param name="size">Longitud máxima; -1 representa NVARCHAR(MAX).</param>
    /// <param name="value">Texto o valor nulo.</param>
    private static void Text(SqlCommand command, string name, int size, object? value) =>
        command.Parameters.Add(name, SqlDbType.NVarChar, size).Value = value ?? DBNull.Value;

    /// <summary>Agrega una fecha opcional sin componente horario.</summary>
    /// <param name="command">Comando que recibe el parámetro.</param>
    /// <param name="name">Nombre SQL del parámetro.</param>
    /// <param name="value">Fecha o nulo.</param>
    private static void Date(SqlCommand command, string name, DateOnly? value) =>
        command.Parameters.Add(name, SqlDbType.Date).Value =
            value?.ToDateTime(TimeOnly.MinValue) ?? (object)DBNull.Value;

    /// <summary>Agrega un importe obligatorio con precisión 19 y escala 4.</summary>
    /// <param name="command">Comando que recibe el parámetro.</param>
    /// <param name="name">Nombre SQL del parámetro.</param>
    /// <param name="value">Importe informado.</param>
    private static void Money(SqlCommand command, string name, decimal value)
    {
        var parameter = command.Parameters.Add(name, SqlDbType.Decimal);
        parameter.Precision = 19;
        parameter.Scale = 4;
        parameter.Value = value;
    }

    /// <summary>Agrega un decimal opcional con precisión y escala indicadas por el contrato SQL.</summary>
    /// <param name="command">Comando que recibe el parámetro.</param>
    /// <param name="name">Nombre SQL del parámetro.</param>
    /// <param name="precision">Dígitos totales admitidos.</param>
    /// <param name="scale">Dígitos admitidos después de la coma.</param>
    /// <param name="value">Cantidad, porcentaje o importe; nulo si no aplica.</param>
    private static void Number(
        SqlCommand command,
        string name,
        byte precision,
        byte scale,
        decimal? value
    )
    {
        var parameter = command.Parameters.Add(name, SqlDbType.Decimal);
        parameter.Precision = precision;
        parameter.Scale = scale;
        parameter.Value = value ?? (object)DBNull.Value;
    }

    /// <summary>Agrega la versión de fila usada para detectar cambios concurrentes.</summary>
    /// <param name="command">Comando que recibe el parámetro.</param>
    /// <param name="value">Versión leída previamente o nulo en un alta.</param>
    private static void Version(SqlCommand command, byte[]? value) =>
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

    /// <summary>Lee un importe o cantidad decimal obligatoria.</summary>
    /// <param name="reader">Lector situado sobre una fila.</param>
    /// <param name="name">Nombre de la columna.</param>
    /// <returns>Valor decimal leído.</returns>
    private static decimal Decimal(SqlDataReader reader, string name) =>
        reader.GetDecimal(reader.GetOrdinal(name));

    /// <summary>Lee un importe o cantidad decimal que puede ser nula.</summary>
    /// <param name="reader">Lector situado sobre una fila.</param>
    /// <param name="name">Nombre de la columna.</param>
    /// <returns>Valor decimal o nulo.</returns>
    private static decimal? NullableDecimal(SqlDataReader reader, string name)
    {
        var index = reader.GetOrdinal(name);
        return reader.IsDBNull(index) ? null : reader.GetDecimal(index);
    }

    /// <summary>Lee una fecha y hora obligatoria.</summary>
    /// <param name="reader">Lector situado sobre una fila.</param>
    /// <param name="name">Nombre de la columna.</param>
    /// <returns>Fecha y hora leída.</returns>
    private static DateTime Date(SqlDataReader reader, string name) =>
        reader.GetDateTime(reader.GetOrdinal(name));

    /// <summary>Convierte una fecha SQL nullable en DateOnly.</summary>
    /// <param name="reader">Lector situado sobre una fila.</param>
    /// <param name="name">Nombre de la columna.</param>
    /// <returns>Fecha sin hora o nulo.</returns>
    private static DateOnly? DateOnly(SqlDataReader reader, string name)
    {
        var index = reader.GetOrdinal(name);
        return reader.IsDBNull(index)
            ? null
            : System.DateOnly.FromDateTime(reader.GetDateTime(index));
    }

    /// <summary>Lee la versión binaria usada en control de concurrencia.</summary>
    /// <param name="reader">Lector situado sobre una fila.</param>
    /// <param name="name">Nombre de la columna.</param>
    /// <returns>Bytes de versión de la fila.</returns>
    private static byte[] Bytes(SqlDataReader reader, string name) => (byte[])reader[name];

    /// <summary>Conserva las salidas funcionales y filas afectadas de un SP comercial.</summary>
    /// <param name="Code">Código de error; nulo indica éxito.</param>
    /// <param name="Message">Mensaje funcional devuelto por SQL.</param>
    /// <param name="Rows">Cantidad opcional de filas afectadas.</param>
    private sealed record OutputsResult(
        SqlParameter Code,
        SqlParameter Message,
        SqlParameter? Rows
    );
}
