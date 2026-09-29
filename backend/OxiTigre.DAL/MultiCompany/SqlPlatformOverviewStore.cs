/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.DAL.MultiCompany.SqlPlatformOverviewStore
Archivo: SqlPlatformOverviewStore.cs | Versión: 1.0.0 | Fecha: 2026-08-27 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Consulta indicadores agregados de cada base sin realizar joins entre empresas.
Historial: 1.0.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
using System.Data;
using Microsoft.Data.SqlClient;

namespace OxiTigre.DAL.MultiCompany;

/// <summary>Obtiene una vista global de solo lectura para administradores.</summary>
public sealed class SqlPlatformOverviewStore(CompanyDatabaseRegistry databases)
{
    private const string OverviewQuery = """
        SELECT
            (SELECT COUNT(*) FROM [COMERCIAL].[CLIENTES] WHERE [ID_EMPRESA] = @I_ID_EMPRESA AND [CODIGO_ESTADO] = N'ACTIVO') AS [CLIENTES_ACTIVOS],
            (SELECT COUNT(*) FROM [INVENTARIO].[PRODUCTOS] WHERE [ID_EMPRESA] = @I_ID_EMPRESA AND [CODIGO_ESTADO] = N'ACTIVO') AS [PRODUCTOS],
            (SELECT COALESCE(SUM([CANTIDAD]), 0) FROM [INVENTARIO].[EXISTENCIAS] WHERE [ID_EMPRESA] = @I_ID_EMPRESA) AS [STOCK_FISICO],
            (SELECT COUNT(*) FROM [COMERCIAL].[PEDIDOS] WHERE [ID_EMPRESA] = @I_ID_EMPRESA AND [CODIGO_ESTADO] IN (N'BORRADOR', N'CONFIRMADO')) AS [PEDIDOS_ABIERTOS],
            (SELECT COUNT(*) FROM [COMERCIAL].[VENTAS] WHERE [ID_EMPRESA] = @I_ID_EMPRESA AND [CODIGO_ESTADO] = N'CONFIRMADA') AS [VENTAS];
        """;

    /// <summary>Consulta cada empresa por separado y conserva las demás si una no está disponible.</summary>
    /// <param name="cancellationToken">Token que permite cancelar la consulta global.</param>
    /// <returns>Indicadores y estado de disponibilidad por empresa.</returns>
    public async Task<IReadOnlyList<CompanyOverview>> GetAsync(CancellationToken cancellationToken)
    {
        var result = new List<CompanyOverview>();

        foreach (var database in databases.Companies.OrderBy(item => item.Name))
        {
            try
            {
                await using var connection = new SqlConnection(database.ConnectionString);
                await using var command = new SqlCommand(OverviewQuery, connection);
                command.Parameters.Add("@I_ID_EMPRESA", SqlDbType.BigInt).Value = database.LocalCompanyId;
                await connection.OpenAsync(cancellationToken);
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                await reader.ReadAsync(cancellationToken);
                result.Add(new(database.Code, database.Name, database.SchemaVersion, "DISPONIBLE",
                    reader.GetInt32(0), reader.GetInt32(1), reader.GetDecimal(2), reader.GetInt32(3), reader.GetInt32(4)));
            }
            catch (SqlException)
            {
                result.Add(new(database.Code, database.Name, database.SchemaVersion, "NO_DISPONIBLE", 0, 0, 0, 0, 0));
            }
        }

        return result;
    }
}

/// <summary>Representa indicadores agregados leídos de una única base operativa.</summary>
public sealed record CompanyOverview(string CompanyCode, string CompanyName, string SchemaVersion, string Status,
    int ActiveClients, int Products, decimal PhysicalStock, int OpenOrders, int Sales);
