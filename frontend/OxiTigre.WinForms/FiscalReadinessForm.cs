/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.WinForms.FiscalReadinessForm
Archivo: FiscalReadinessForm.cs | Versión: 12.0.0 | Fecha: 2026-08-27 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Diagnostica la preparación fiscal sin generar numeración, CAE ni comprobantes.
Historial: 12.0.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
using System.Globalization;
using OxiTigre.ApiClient;
using OxiTigre.Contracts.Commercial;
using OxiTigre.Contracts.Configuration;
using OxiTigre.Contracts.Security;

namespace OxiTigre.WinForms;

/// <summary>Expone requisitos verificables y ventas candidatas antes de conectar con ARCA.</summary>
internal sealed class FiscalReadinessForm : Form
{
    private const string TaxConditionKey = "FISCAL.CONDICION_IVA_EMISOR";
    private const string ServiceKey = "FISCAL.SERVICIO_ARCA";
    private const string EnvironmentKey = "FISCAL.AMBIENTE_ARCA";
    private const string PointOfSaleKey = "FISCAL.PUNTO_VENTA";
    private const string CertificateKey = "FISCAL.CERTIFICADO_ARCA";

    private readonly OxiTigreApiClient _api;
    private readonly LoginResponse _session;
    private readonly DataGridView _requirements = Grid();
    private readonly DataGridView _sales = Grid();
    private readonly Label _status = new()
    {
        Dock = DockStyle.Top,
        Height = 48,
        Padding = new Padding(14, 14, 8, 8),
        Font = new Font("Segoe UI", 10F, FontStyle.Bold)
    };

    /// <summary>Inicializa el diagnóstico fiscal para la empresa autenticada.</summary>
    /// <param name="api">Cliente HTTP compartido por la aplicación.</param>
    /// <param name="session">Sesión que define empresa y permisos.</param>
    internal FiscalReadinessForm(OxiTigreApiClient api, LoginResponse session)
    {
        _api = api;
        _session = session;
        Text = L("Facturación fiscal · Preparación", "Tax invoicing · Readiness");
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(1050, 700);
        StartPosition = FormStartPosition.CenterParent;
        UiTheme.Apply(this);

        var overview = new Label
        {
            Text = L(
                "Esta pantalla comprueba lo necesario para homologar. No genera facturas, numeración ni CAE.",
                "This screen checks homologation requirements. It does not generate invoices, numbers or CAE."),
            Dock = DockStyle.Top,
            Height = 48,
            Padding = new Padding(14, 14, 8, 8),
            BackColor = Color.FromArgb(230, 245, 235),
            ForeColor = UiTheme.Primary
        };
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 54,
            Padding = new Padding(12, 10, 8, 8),
            WrapContents = false
        };
        var refresh = UiTheme.Button(L("Actualizar diagnóstico", "Refresh readiness"), 0, 0, 180);
        refresh.Click += async (_, _) => await LoadAsync();
        var configuration = UiTheme.Button(L("Abrir configuración", "Open configuration"), 0, 0, 170, ButtonTone.Neutral);
        configuration.Enabled = _session.Permissions.Contains("CONFIGURACION.CONSULTAR", StringComparer.OrdinalIgnoreCase);
        configuration.Click += async (_, _) => await OpenConfigurationAsync();
        toolbar.Controls.AddRange([refresh, configuration, UiTheme.HelpButton(L(
            "Completá únicamente datos confirmados por el contador. El certificado y la clave privada deben quedar en un almacén externo, nunca en Git ni como valor de un parámetro.",
            "Only enter data confirmed by the accountant. The certificate and private key must remain in an external secret store, never in Git or as a parameter value."))]);

        var content = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 315
        };
        content.Panel1.Controls.Add(UiTheme.Searchable(_requirements, null,
            L("Requisitos para homologación", "Homologation requirements")));
        content.Panel2.Controls.Add(UiTheme.Searchable(_sales, null,
            L("Ventas internas candidatas", "Candidate internal sales")));

        Controls.Add(content);
        Controls.Add(_status);
        Controls.Add(toolbar);
        Controls.Add(overview);
        Shown += async (_, _) => await LoadAsync();
    }

    /// <summary>Consulta configuración y ventas, luego presenta un diagnóstico sin cambiar datos.</summary>
    /// <returns>Tarea que finaliza al actualizar ambas grillas.</returns>
    /// <exception cref="HttpRequestException">La API no está disponible o rechaza la sesión.</exception>
    private async Task LoadAsync()
    {
        try
        {
            var configurationTask = _api.GetConfigurationAsync(_session.Token);
            var salesTask = _api.GetSalesAsync(_session.Token);
            var activeClientsTask = _api.GetClientsAsync(_session.Token, "ACTIVO");
            var inactiveClientsTask = _api.GetClientsAsync(_session.Token, "INACTIVO");
            await Task.WhenAll(configurationTask, salesTask, activeClientsTask, inactiveClientsTask);

            var configuration = configurationTask.Result;
            var sales = salesTask.Result;
            var clients = activeClientsTask.Result.Concat(inactiveClientsTask.Result)
                .GroupBy(item => item.ClientId)
                .ToDictionary(group => group.Key, group => group.First());
            var checks = Checks(configuration);
            Bind(_requirements, checks);
            Bind(_sales, CandidateSales(sales, clients, checks.All(item => item.Ready)));

            var pending = checks.Count(item => !item.Ready);
            _status.Text = pending == 0
                ? L("Configuración básica completa. Aún falta ejecutar homologación real antes de emitir.",
                    "Basic setup complete. Real homologation is still required before issuing.")
                : L($"Emisión bloqueada: faltan {pending} requisitos.", $"Issuing blocked: {pending} requirements are missing.");
            _status.BackColor = pending == 0 ? Color.FromArgb(220, 252, 231) : Color.FromArgb(254, 243, 199);
            _status.ForeColor = pending == 0 ? UiTheme.Primary : Color.FromArgb(146, 64, 14);
        }
        catch (HttpRequestException exception)
        {
            MessageBox.Show(this, exception.Message, "OxiTigre", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>Abre la configuración existente y recalcula el diagnóstico al volver.</summary>
    /// <returns>Tarea completada después de recargar la información.</returns>
    private async Task OpenConfigurationAsync()
    {
        using var form = new ConfigurationForm(_api, _session.Token, _session.Permissions);
        form.ShowDialog(this);
        await LoadAsync();
    }

    /// <summary>Evalúa solo datos existentes y no supone definiciones tributarias.</summary>
    /// <param name="configuration">Configuración actual de la empresa.</param>
    /// <returns>Lista de requisitos con valor, estado y acción recomendada.</returns>
    private static IReadOnlyList<FiscalCheckRow> Checks(ConfigurationSnapshotResponse configuration)
    {
        string? Value(string key) => configuration.Parameters.FirstOrDefault(item =>
            item.ModuleCode == "FISCAL" && item.Key == key && item.StatusCode == "ACTIVO")?.Value?.Trim();

        var taxId = Digits(configuration.Company.TaxId);
        var taxCondition = Value(TaxConditionKey);
        var service = Value(ServiceKey);
        var environment = Value(EnvironmentKey);
        var pointOfSale = Value(PointOfSaleKey);
        var certificate = configuration.Parameters.FirstOrDefault(item =>
            item.ModuleCode == "FISCAL" && item.Key == CertificateKey && item.StatusCode == "ACTIVO");

        return
        [
            Check(L("CUIT del emisor", "Issuer tax ID"), FormatTaxId(taxId), taxId.Length == 11,
                L("Completalo en Configuración · Empresa.", "Complete it in Configuration · Company.")),
            Check(L("Condición frente al IVA", "VAT condition"), taxCondition, !string.IsNullOrWhiteSpace(taxCondition),
                L("Confirmala con el contador y cargala en Parámetros.", "Confirm it with the accountant and enter it in Parameters.")),
            Check(L("Servicio de ARCA", "ARCA service"), service, service is "WSFEV1" or "WSMTXCA",
                L("Definí WSFEV1 o WSMTXCA según el detalle requerido.", "Set WSFEV1 or WSMTXCA according to the required detail.")),
            Check(L("Ambiente", "Environment"), environment, environment == "HOMOLOGACION",
                L("La primera conexión debe ser HOMOLOGACION.", "The first connection must use HOMOLOGACION.")),
            Check(L("Punto de venta Web Services", "Web Services point of sale"), pointOfSale,
                int.TryParse(pointOfSale, NumberStyles.None, CultureInfo.InvariantCulture, out var number) && number > 0,
                L("Crealo en ARCA y registrá su número.", "Create it in ARCA and enter its number.")),
            Check(L("Referencia al certificado", "Certificate reference"),
                certificate?.HasSecretReference == true ? L("Configurada", "Configured") : null,
                certificate is { IsSecret: true, HasSecretReference: true },
                L("Creá el parámetro secreto FISCAL.CERTIFICADO_ARCA con una referencia externa.",
                    "Create secret parameter FISCAL.CERTIFICADO_ARCA with an external reference.")),
            Check(L("Condición fiscal de clientes", "Customer tax condition"), null, false,
                L("Todavía debe incorporarse al maestro de clientes después de confirmar categorías y comprobantes.",
                    "It must still be added to customer records after confirming categories and voucher types."))
        ];
    }

    /// <summary>Resume una vez cada venta interna y explica por qué todavía no es una factura.</summary>
    /// <param name="snapshot">Ventas internas actuales.</param>
    /// <param name="clients">Clientes disponibles por identificador.</param>
    /// <param name="globallyReady">Indica si la configuración general está completa.</param>
    /// <returns>Ventas candidatas sin duplicar sus renglones.</returns>
    private static IReadOnlyList<FiscalSaleRow> CandidateSales(SalesSnapshotResponse snapshot,
        IReadOnlyDictionary<long, ClientSummaryResponse> clients, bool globallyReady) => snapshot.Sales
        .GroupBy(item => item.SaleId)
        .Select(group => group.First())
        .OrderByDescending(item => item.SaleDateUtc)
        .Select(item =>
        {
            clients.TryGetValue(item.ClientId, out var client);
            return new FiscalSaleRow(item.SaleCode, item.SaleDateUtc, item.Client,
                client?.DocumentNumber ?? L("Sin documento", "No document"), item.Currency, item.Subtotal,
                item.TotalTax, item.Total, L("Venta interna", "Internal sale"), globallyReady
                    ? L("Pendiente de clasificar receptor", "Receiver classification pending")
                    : L("Configuración incompleta", "Setup incomplete"));
        })
        .ToArray();

    /// <summary>Construye una fila del diagnóstico fiscal con estado y acción recomendada.</summary>
    /// <param name="requirement">Requisito fiscal que se describe en el diagnóstico.</param>
    /// <param name="value">Texto que se normaliza, valida o asigna.</param>
    /// <param name="ready">Indica si el requisito fiscal está cumplido.</param>
    /// <param name="action">Código de la acción que se conserva en la auditoría.</param>
    /// <returns>Objeto construido u obtenido por la operación.</returns>
    private static FiscalCheckRow Check(string requirement, string? value, bool ready, string action) =>
        new(requirement, string.IsNullOrWhiteSpace(value) ? L("Pendiente", "Pending") : value,
            ready ? L("Completo", "Complete") : L("Falta completar", "Missing"), action, ready);

    /// <summary>Conserva únicamente los dígitos de un identificador fiscal.</summary>
    /// <param name="value">Texto que se normaliza, valida o asigna.</param>
    /// <returns>Texto normalizado, localizado o formateado por la operación.</returns>
    private static string Digits(string? value) => new((value ?? string.Empty).Where(char.IsDigit).ToArray());

    /// <summary>Presenta un CUIT o CUIL con sus separadores visuales.</summary>
    /// <param name="value">Texto que se normaliza, valida o asigna.</param>
    /// <returns>Texto normalizado, localizado o formateado por la operación.</returns>
    private static string FormatTaxId(string value) => value.Length == 11
        ? $"{value[..2]}-{value.Substring(2, 8)}-{value[10]}"
        : L("Pendiente", "Pending");

    /// <summary>Actualiza las grillas y detalles de preparación fiscal con la información cargada.</summary>
    /// <param name="grid">Grilla que se configura, consulta o actualiza.</param>
    /// <param name="rows">Cantidad de filas afectadas informada por el procedimiento.</param>
    private static void Bind<T>(DataGridView grid, IReadOnlyList<T> rows)
    {
        grid.DataSource = rows.ToList();
        foreach (DataGridViewColumn column in grid.Columns)
        {
            column.Visible = column.Name != "Ready";
            column.HeaderText = Header(column.Name);
            if (column.Name is "Subtotal" or "Tax" or "Total") column.DefaultCellStyle.Format = "N2";
            if (column.Name == "Date") column.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
        }
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
    }

    /// <summary>Crea una grilla con el estilo y comportamiento común del módulo de preparación fiscal.</summary>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private static DataGridView Grid()
    {
        var grid = new DataGridView { ReadOnly = true, AutoGenerateColumns = true };
        UiTheme.Grid(grid);
        return grid;
    }

    /// <summary>Resuelve el encabezado localizado de una columna o propiedad.</summary>
    /// <param name="name">Nombre técnico del elemento solicitado.</param>
    /// <returns>Texto normalizado, localizado o formateado por la operación.</returns>
    private static string Header(string name) => name switch
    {
        "Requirement" => L("Requisito", "Requirement"),
        "Value" => L("Valor actual", "Current value"),
        "Status" => L("Estado", "Status"),
        "Action" => L("Qué falta hacer", "Required action"),
        "Sale" => L("Venta", "Sale"),
        "Date" => L("Fecha", "Date"),
        "Client" => L("Cliente", "Customer"),
        "Document" => L("Documento", "Document"),
        "Currency" => L("Moneda", "Currency"),
        "Subtotal" => L("Subtotal", "Subtotal"),
        "Tax" => L("IVA registrado", "Recorded VAT"),
        "Total" => L("Total", "Total"),
        "InternalStatus" => L("Tipo actual", "Current type"),
        "FiscalStatus" => L("Preparación fiscal", "Tax readiness"),
        _ => name
    };

    /// <summary>Selecciona el texto en español o inglés según la cultura activa.</summary>
    /// <param name="spanish">Texto que se usa cuando la cultura activa es española.</param>
    /// <param name="english">Texto que se usa cuando la cultura activa es inglesa.</param>
    /// <returns>Texto normalizado, localizado o formateado por la operación.</returns>
    private static string L(string spanish, string english) =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en" ? english : spanish;

    private sealed record FiscalCheckRow(string Requirement, string Value, string Status, string Action, bool Ready);
    private sealed record FiscalSaleRow(string Sale, DateTime Date, string Client, string Document, string Currency,
        decimal Subtotal, decimal Tax, decimal Total, string InternalStatus, string FiscalStatus);
}
