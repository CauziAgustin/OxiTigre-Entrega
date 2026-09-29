/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.DAL.MultiCompany.CompanyDatabaseRegistry
Archivo: CompanyDatabaseRegistry.cs | Versión: 1.0.0 | Fecha: 2026-08-27 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Carga y resuelve las bases operativas registradas en OxiTigre_Platform.
Historial: 1.0.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
using System.Data;
using Microsoft.Data.SqlClient;

namespace OxiTigre.DAL.MultiCompany;

/// <summary>Registro inmutable de bases autorizadas cargado al iniciar la API.</summary>
public sealed class CompanyDatabaseRegistry
{
    private readonly IReadOnlyDictionary<string, CompanyDatabase> _databases;

    /// <summary>Inicializa el registro inmutable de bases habilitadas por empresa.</summary>
    /// <param name="databases">Bases de datos habilitadas, indexadas por empresa.</param>
    private CompanyDatabaseRegistry(IReadOnlyList<CompanyDatabase> databases)
    {
        _databases = databases.ToDictionary(item => item.Code, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Carga empresas activas desde la base central y valida destinos duplicados.</summary>
    /// <param name="platformConnectionString">Conexión técnica a OxiTigre_Platform.</param>
    /// <param name="cancellationToken">Token que permite cancelar la carga inicial.</param>
    /// <returns>Registro listo para resolver conexiones operativas.</returns>
    /// <exception cref="InvalidOperationException">No existen empresas o se configuró un secreto sin proveedor.</exception>
    public static async Task<CompanyDatabaseRegistry> LoadAsync(string platformConnectionString,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(platformConnectionString);
        await using var command = new SqlCommand("[PLATAFORMA].[SP_EMPRESA_BASE_LIST]", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var databases = new List<CompanyDatabase>();

        while (await reader.ReadAsync(cancellationToken))
        {
            if (!reader.IsDBNull(reader.GetOrdinal("SECRETO_REFERENCIA")))
                throw new InvalidOperationException("La empresa requiere un proveedor de secretos todavía no configurado.");

            var builder = new SqlConnectionStringBuilder
            {
                DataSource = reader.GetString(reader.GetOrdinal("SERVIDOR_SQL")),
                InitialCatalog = reader.GetString(reader.GetOrdinal("BASE_DATOS")),
                IntegratedSecurity = true,
                Encrypt = reader.GetBoolean(reader.GetOrdinal("CIFRAR_CONEXION")),
                TrustServerCertificate = reader.GetBoolean(reader.GetOrdinal("CONFIAR_CERTIFICADO"))
            };

            databases.Add(new CompanyDatabase(
                reader.GetInt64(reader.GetOrdinal("ID_EMPRESA_PLATAFORMA")),
                reader.GetString(reader.GetOrdinal("CODIGO")),
                reader.GetString(reader.GetOrdinal("RAZON_SOCIAL")),
                reader.GetInt64(reader.GetOrdinal("ID_EMPRESA_LOCAL")),
                reader.GetString(reader.GetOrdinal("VERSION_ESQUEMA")),
                builder.ConnectionString));
        }

        if (databases.Count == 0)
            throw new InvalidOperationException("OxiTigre_Platform no posee empresas activas.");

        // ponytail: el registro se recarga al reiniciar; agregar refresco solo si las altas deben aplicarse sin reinicio.
        return new CompanyDatabaseRegistry(databases);
    }

    /// <summary>Obtiene todas las empresas operativas habilitadas.</summary>
    public IReadOnlyCollection<CompanyDatabase> Companies => _databases.Values.ToArray();

    /// <summary>Resuelve la conexión de una empresa sin aceptar destinos enviados por el cliente.</summary>
    /// <param name="companyCode">Código autorizado proveniente de sesión o selección validada.</param>
    /// <returns>Cadena construida desde el registro central.</returns>
    /// <exception cref="KeyNotFoundException">El código no está habilitado.</exception>
    public string GetConnectionString(string companyCode) =>
        _databases.TryGetValue(companyCode, out var database)
            ? database.ConnectionString
            : throw new KeyNotFoundException("La empresa seleccionada no está habilitada.");
}

/// <summary>Describe una base operativa autorizada sin exponer su conexión al exterior.</summary>
public sealed record CompanyDatabase(long PlatformCompanyId, string Code, string Name, long LocalCompanyId,
    string SchemaVersion, string ConnectionString);
