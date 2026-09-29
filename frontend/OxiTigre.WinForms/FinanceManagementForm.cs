/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.WinForms.FinanceManagementForm
Archivo: FinanceManagementForm.cs | Versión: 11.0.2 | Fecha: 2026-08-27 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Permite administrar cajas, cobros y cuenta corriente no fiscal.
Historial: 11.0.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 11.0.1 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Grillas funcionales localizadas y cuenta corriente reorganizada.
Historial: 11.0.2 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Cobro proporcional, total automático y ayuda para saldos a favor.
===============================================================================
*/
using System.Globalization;
using OxiTigre.ApiClient;
using OxiTigre.Contracts.Finance;
using OxiTigre.Contracts.Security;

namespace OxiTigre.WinForms;

/// <summary>Presenta el circuito financiero local y evita confundir recibos internos con facturas fiscales.</summary>
internal sealed class FinanceManagementForm : Form
{
    private readonly OxiTigreApiClient _api;
    private readonly LoginResponse _session;
    private readonly DataGridView _cashBoxes = Grid();
    private readonly DataGridView _cashSessions = Grid();
    private readonly DataGridView _payments = Grid();
    private readonly DataGridView _paymentLines = Grid();
    private readonly DataGridView _applications = Grid();
    private readonly DataGridView _clients = Grid();
    private readonly DataGridView _sales = Grid();
    private readonly DataGridView _movements = Grid();
    private readonly DataGridView _methods = Grid();
    private FinanceSnapshotResponse? _snapshot;

    /// <summary>Inicializa la pantalla según los permisos de la sesión autenticada.</summary>
    /// <param name="api">Cliente usado para consultar y modificar la información financiera.</param>
    /// <param name="session">Sesión autenticada que delimita empresa, sucursal y permisos.</param>
    internal FinanceManagementForm(OxiTigreApiClient api, LoginResponse session)
    {
        _api = api;
        _session = session;
        Text = Localization.Text("Finance_Title");
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(1050, 700);
        StartPosition = FormStartPosition.CenterParent;
        UiTheme.Apply(this);

        var overview = new Label
        {
            Text = Localization.Text("Finance_Overview"),
            Dock = DockStyle.Top,
            Height = 48,
            Padding = new Padding(14, 14, 8, 8),
            BackColor = Color.FromArgb(230, 245, 235),
            ForeColor = UiTheme.Primary,
        };
        var tabs = new TabControl { Dock = DockStyle.Fill };
        UiTheme.Tabs(tabs);
        tabs.TabPages.Add(CashPage());
        tabs.TabPages.Add(PaymentsPage());
        tabs.TabPages.Add(AccountPage());
        tabs.TabPages.Add(MethodsPage());
        Controls.Add(tabs);
        Controls.Add(overview);
        Shown += async (_, _) => await LoadAsync();
    }

    /// <summary>Construye la pestaña de cajas, aperturas y cierres.</summary>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private TabPage CashPage()
    {
        var page = new TabPage(Localization.Text("Finance_Cash"));
        var toolbar = Toolbar();
        var add = Action(
            Localization.Text("Finance_NewCashBox"),
            Can("FINANZAS.CONFIGURAR"),
            async () => await SaveCashBoxAsync(null)
        );
        var edit = Action(
            Localization.Text("Finance_EditCashBox"),
            Can("FINANZAS.CONFIGURAR"),
            async () =>
                await SaveCashBoxAsync(_cashBoxes.CurrentRow?.DataBoundItem as CashBoxResponse)
        );
        var open = Action(
            Localization.Text("Finance_OpenCash"),
            Can("FINANZAS.CAJA_GESTIONAR"),
            OpenCashAsync
        );
        var close = Action(
            Localization.Text("Finance_CloseCash"),
            Can("FINANZAS.CAJA_GESTIONAR"),
            CloseCashAsync
        );
        var refresh = Action(Localization.Text("Finance_Refresh"), true, LoadAsync);
        toolbar.Controls.AddRange([
            add,
            edit,
            open,
            close,
            refresh,
            UiTheme.HelpButton(Localization.Text("Finance_CashHelp")),
        ]);
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 270,
        };
        split.Panel1.Controls.Add(
            UiTheme.Searchable(
                _cashBoxes,
                Localization.Text("Finance_CashHelp"),
                L("Cajas físicas", "Cash boxes")
            )
        );
        split.Panel2.Controls.Add(
            UiTheme.Searchable(
                _cashSessions,
                Localization.Text("Finance_CashHelp"),
                L("Aperturas y cierres", "Cash shifts")
            )
        );
        page.Controls.Add(split);
        page.Controls.Add(toolbar);
        return page;
    }

    /// <summary>Construye la pestaña de cobros y sus aplicaciones.</summary>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private TabPage PaymentsPage()
    {
        var page = new TabPage(Localization.Text("Finance_Payments"));
        var toolbar = Toolbar();
        toolbar.Controls.AddRange([
            Action(
                Localization.Text("Finance_NewPayment"),
                Can("FINANZAS.COBRAR"),
                NewPaymentAsync
            ),
            Action(
                Localization.Text("Finance_ReversePayment"),
                Can("FINANZAS.REVERSAR"),
                ReversePaymentAsync
            ),
            Action(Localization.Text("Finance_Refresh"), true, LoadAsync),
            UiTheme.HelpButton(Localization.Text("Finance_PaymentHelp")),
        ]);
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 360,
        };
        split.Panel1.Controls.Add(
            UiTheme.Searchable(
                _payments,
                Localization.Text("Finance_PaymentHelp"),
                L("Recibos internos", "Internal receipts")
            )
        );
        var details = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 600 };
        details.Panel1.Controls.Add(
            UiTheme.Searchable(_paymentLines, null, L("Medios del cobro", "Payment methods"))
        );
        details.Panel2.Controls.Add(
            UiTheme.Searchable(_applications, null, L("Ventas aplicadas", "Applied sales"))
        );
        split.Panel2.Controls.Add(details);
        _payments.SelectionChanged += (_, _) => ShowPaymentDetails();
        page.Controls.Add(split);
        page.Controls.Add(toolbar);
        return page;
    }

    /// <summary>Construye la pestaña de cuenta corriente por cliente.</summary>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private TabPage AccountPage()
    {
        var page = new TabPage(Localization.Text("Finance_Account"));
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(4),
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 35));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 65));
        var clients = UiTheme.Searchable(
            _clients,
            null,
            L("Clientes y saldo de cuenta corriente (ARS)", "Customers and account balance (ARS)")
        );
        layout.Controls.Add(clients, 0, 0);
        layout.SetColumnSpan(clients, 2);
        layout.Controls.Add(
            UiTheme.Searchable(
                _sales,
                null,
                L("Ventas del cliente y saldo pendiente", "Customer sales and outstanding balance")
            ),
            0,
            1
        );
        layout.Controls.Add(
            UiTheme.Searchable(
                _movements,
                null,
                L("Historial de débitos y créditos", "Debit and credit history")
            ),
            1,
            1
        );
        _clients.SelectionChanged += (_, _) => ShowClientAccount();
        page.Controls.Add(layout);
        return page;
    }

    /// <summary>Construye la pestaña de medios de pago configurables.</summary>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private TabPage MethodsPage()
    {
        var page = new TabPage(Localization.Text("Finance_Methods"));
        var toolbar = Toolbar();
        toolbar.Controls.AddRange([
            Action(
                Localization.Text("Finance_NewMethod"),
                Can("FINANZAS.CONFIGURAR"),
                async () => await SaveMethodAsync(null)
            ),
            Action(
                Localization.Text("Finance_EditMethod"),
                Can("FINANZAS.CONFIGURAR"),
                async () =>
                    await SaveMethodAsync(
                        _methods.CurrentRow?.DataBoundItem as PaymentMethodResponse
                    )
            ),
            Action(Localization.Text("Finance_Refresh"), true, LoadAsync),
        ]);
        page.Controls.Add(UiTheme.Searchable(_methods, null, Localization.Text("Finance_Methods")));
        page.Controls.Add(toolbar);
        return page;
    }

    /// <summary>Recupera el estado financiero y reinicia los detalles dependientes de la selección anterior.</summary>
    /// <returns>Tarea que termina al enlazar las grillas o mostrar un error de conexión.</returns>
    private async Task LoadAsync()
    {
        try
        {
            _snapshot = await _api.GetFinanceAsync(_session.Token);
            Bind(_cashBoxes, _snapshot.CashBoxes);
            Bind(_cashSessions, _snapshot.CashSessions);
            Bind(_payments, _snapshot.Payments);
            Bind(_clients, _snapshot.Clients);
            Bind(_methods, _snapshot.PaymentMethods);
            Bind(_paymentLines, Array.Empty<CustomerPaymentMethodResponse>());
            Bind(_applications, Array.Empty<PaymentApplicationResponse>());
            Bind(_sales, Array.Empty<ReceivableSaleResponse>());
            Bind(_movements, Array.Empty<AccountMovementResponse>());
        }
        catch (HttpRequestException exception)
        {
            Error(exception);
        }
    }

    /// <summary>Da de alta o modifica una caja en una sucursal, conservando su versión para concurrencia.</summary>
    /// <param name="current">Caja existente; <see langword="null"/> para crear una.</param>
    /// <returns>Tarea que termina al cancelar el formulario o guardar y recargar las cajas.</returns>
    private async Task SaveCashBoxAsync(CashBoxResponse? current)
    {
        if (_snapshot is null || current is null && _snapshot.Branches.Count == 0)
        {
            Warn();
            return;
        }
        var branchOptions = _snapshot
            .Branches.Select(x => new KeyValuePair<string, string>(x.BranchId.ToString(), x.Name))
            .ToList();
        using var form = new SimpleEntryForm(
            current is null
                ? Localization.Text("Finance_NewCashBox")
                : Localization.Text("Finance_EditCashBox"),
            new(
                "Branch",
                L("Sucursal", "Branch"),
                current?.BranchId.ToString(),
                branchOptions,
                true
            ),
            new("Code", L("Código", "Code"), current?.Code, Required: true),
            new("Name", L("Nombre", "Name"), current?.Name, Required: true),
            new("Currency", L("Moneda", "Currency"), current?.Currency ?? "ARS", Required: true),
            new(
                "Status",
                L("Estado", "Status"),
                current?.StatusCode ?? "ACTIVO",
                Options: StatusOptions(),
                Required: true
            )
        );
        if (form.ShowDialog(this) != DialogResult.OK)
            return;
        try
        {
            await _api.SaveCashBoxAsync(
                _session.Token,
                new(
                    current?.CashBoxId,
                    long.Parse(form["Branch"]),
                    form["Code"],
                    form["Name"],
                    form["Currency"],
                    form["Status"],
                    current?.RowVersion
                )
            );
            await LoadAsync();
        }
        catch (HttpRequestException exception)
        {
            Error(exception);
        }
    }

    /// <summary>Configura un medio de pago y sus reglas de efectivo y referencia.</summary>
    /// <param name="current">Medio existente; <see langword="null"/> para crear uno nuevo.</param>
    /// <returns>Tarea que termina al cancelar el formulario o guardar y recargar los medios.</returns>
    private async Task SaveMethodAsync(PaymentMethodResponse? current)
    {
        using var form = new SimpleEntryForm(
            current is null
                ? Localization.Text("Finance_NewMethod")
                : Localization.Text("Finance_EditMethod"),
            new("Code", L("Código", "Code"), current?.Code, Required: true),
            new("Name", L("Nombre", "Name"), current?.Name, Required: true),
            new(
                "Type",
                L("Tipo", "Type"),
                current?.Type ?? "EFECTIVO",
                Options: Types(),
                Required: true
            ),
            new(
                "Cash",
                L("Afecta efectivo", "Affects cash"),
                current?.AffectsCash == true ? "1" : "0",
                Options: YesNo(),
                Required: true
            ),
            new(
                "Reference",
                L("Requiere referencia", "Requires reference"),
                current?.RequiresReference == true ? "1" : "0",
                Options: YesNo(),
                Required: true
            ),
            new(
                "Status",
                L("Estado", "Status"),
                current?.StatusCode ?? "ACTIVO",
                Options: StatusOptions(),
                Required: true
            )
        );
        if (form.ShowDialog(this) != DialogResult.OK)
            return;
        try
        {
            await _api.SavePaymentMethodAsync(
                _session.Token,
                new(
                    current?.PaymentMethodId,
                    form["Code"],
                    form["Name"],
                    form["Type"],
                    form["Cash"] == "1",
                    form["Reference"] == "1",
                    form["Status"],
                    current?.RowVersion
                )
            );
            await LoadAsync();
        }
        catch (HttpRequestException exception)
        {
            Error(exception);
        }
    }

    /// <summary>Abre una caja activa con el efectivo inicial declarado por el usuario.</summary>
    /// <returns>Tarea que termina al cancelar el formulario o registrar la apertura.</returns>
    private async Task OpenCashAsync()
    {
        if (
            _cashBoxes.CurrentRow?.DataBoundItem is not CashBoxResponse box
            || box.StatusCode != "ACTIVO"
        )
        {
            Warn();
            return;
        }
        using var form = new SimpleEntryForm(
            Localization.Text("Finance_OpenCash"),
            new(
                "Amount",
                Localization.Text("Finance_OpeningAmount"),
                "0",
                Required: true,
                Numeric: true
            ),
            new("Observation", Localization.Text("Finance_Observation"), Multiline: true)
        );
        if (
            form.ShowDialog(this) != DialogResult.OK
            || !decimal.TryParse(form["Amount"], out var amount)
        )
            return;
        try
        {
            await _api.OpenCashSessionAsync(
                _session.Token,
                new(box.CashBoxId, amount, form["Observation"])
            );
            await LoadAsync();
        }
        catch (HttpRequestException exception)
        {
            Error(exception);
        }
    }

    /// <summary>Cierra únicamente una apertura propia y registra el efectivo contado y su versión vigente.</summary>
    /// <returns>Tarea que termina al cancelar el arqueo o registrar el cierre.</returns>
    private async Task CloseCashAsync()
    {
        if (
            _cashSessions.CurrentRow?.DataBoundItem is not CashSessionResponse session
            || session.StatusCode != "ABIERTA"
            || session.OpeningUserId != _session.UserId
        )
        {
            Warn();
            return;
        }
        using var form = new SimpleEntryForm(
            Localization.Text("Finance_CloseCash"),
            new(
                "Amount",
                Localization.Text("Finance_CountedAmount"),
                "0",
                Required: true,
                Numeric: true
            ),
            new("Observation", Localization.Text("Finance_Observation"), Multiline: true)
        );
        if (
            form.ShowDialog(this) != DialogResult.OK
            || !decimal.TryParse(form["Amount"], out var amount)
        )
            return;
        try
        {
            await _api.CloseCashSessionAsync(
                _session.Token,
                session.CashSessionId,
                new(amount, form["Observation"], session.RowVersion)
            );
            await LoadAsync();
        }
        catch (HttpRequestException exception)
        {
            Error(exception);
        }
    }

    /// <summary>Registra un cobro con medios de pago y aplicaciones a ventas definidos en el formulario.</summary>
    /// <returns>Tarea que termina al cancelar el formulario o guardar el cobro y recargar los saldos.</returns>
    private async Task NewPaymentAsync()
    {
        if (_snapshot is null)
            return;
        using var form = new CustomerPaymentForm(_snapshot, _session.UserId);
        if (form.ShowDialog(this) != DialogResult.OK || form.Request is null)
            return;
        try
        {
            await _api.RegisterCustomerPaymentAsync(_session.Token, form.Request);
            await LoadAsync();
        }
        catch (HttpRequestException exception)
        {
            Error(exception);
        }
    }

    /// <summary>Revierte un cobro confirmado mediante una compensación sin borrar el recibo original.</summary>
    /// <returns>Tarea que termina al cancelar el motivo o guardar la reversión y actualizar los saldos.</returns>
    private async Task ReversePaymentAsync()
    {
        if (
            _payments.CurrentRow?.DataBoundItem is not CustomerPaymentResponse payment
            || payment.StatusCode != "CONFIRMADO"
        )
        {
            Warn();
            return;
        }
        using var form = new SimpleEntryForm(
            Localization.Text("Finance_ReversePayment"),
            new EntryField(
                "Reason",
                Localization.Text("Finance_ReversalReason"),
                Required: true,
                Multiline: true,
                Help: L(
                    "La reversión genera un débito compensatorio y conserva el recibo original.",
                    "Reversal creates a compensating debit and preserves the original receipt."
                )
            )
        );
        if (form.ShowDialog(this) != DialogResult.OK)
            return;
        try
        {
            await _api.ReverseCustomerPaymentAsync(
                _session.Token,
                payment.PaymentId,
                new(form["Reason"], payment.RowVersion)
            );
            await LoadAsync();
        }
        catch (HttpRequestException exception)
        {
            Error(exception);
        }
    }

    /// <summary>Filtra los medios y aplicaciones del cobro seleccionado sin modificar los movimientos.</summary>
    private void ShowPaymentDetails()
    {
        if (
            _snapshot is null
            || _payments.CurrentRow?.DataBoundItem is not CustomerPaymentResponse payment
        )
            return;
        Bind(
            _paymentLines,
            _snapshot.PaymentLines.Where(x => x.PaymentId == payment.PaymentId).ToList()
        );
        Bind(
            _applications,
            _snapshot.Applications.Where(x => x.PaymentId == payment.PaymentId).ToList()
        );
    }

    /// <summary>Presenta ventas pendientes y movimientos inmutables del cliente seleccionado.</summary>
    private void ShowClientAccount()
    {
        if (
            _snapshot is null
            || _clients.CurrentRow?.DataBoundItem is not FinanceClientResponse client
        )
            return;
        Bind(_sales, _snapshot.Sales.Where(x => x.ClientId == client.ClientId).ToList());
        Bind(
            _movements,
            _snapshot.AccountMovements.Where(x => x.ClientId == client.ClientId).ToList()
        );
    }

    /// <summary>Consulta los permisos de la sesión para habilitar acciones financieras.</summary>
    /// <param name="permission">Código del permiso requerido.</param>
    /// <returns><see langword="true"/> si el usuario tiene el permiso.</returns>
    private bool Can(string permission) =>
        _session.Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);

    /// <summary>Crea una barra de acciones coherente con el módulo de finanzas.</summary>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private static FlowLayoutPanel Toolbar() =>
        new()
        {
            Dock = DockStyle.Top,
            Height = 52,
            Padding = new Padding(8),
            WrapContents = false,
        };

    /// <summary>Crea un botón de acción y conecta su operación asincrónica.</summary>
    /// <param name="text">Texto que se muestra, interpreta o transforma.</param>
    /// <param name="enabled">Indica si el control o la acción quedan habilitados.</param>
    /// <param name="action">Operación que se ejecuta cuando el usuario confirma la acción.</param>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private Button Action(string text, bool enabled, Func<Task> action)
    {
        var button = UiTheme.Button(text, 0, 0, 150);
        button.Enabled = enabled;
        button.Click += async (_, _) => await action();
        return button;
    }

    /// <summary>Crea una grilla con el estilo y comportamiento común del módulo de finanzas.</summary>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private static DataGridView Grid()
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AutoGenerateColumns = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells,
        };
        UiTheme.Grid(grid);
        return grid;
    }

    /// <summary>Enlaza datos financieros ocultando identificadores técnicos y traduciendo encabezados.</summary>
    /// <typeparam name="T">Tipo de registro mostrado.</typeparam>
    /// <param name="grid">Grilla que recibe los registros.</param>
    /// <param name="values">Registros visibles para el usuario.</param>
    private static void Bind<T>(DataGridView grid, IEnumerable<T> values)
    {
        grid.DataSource = null;
        grid.DataSource = values.ToList();
        ConfigureColumns(grid);
        grid.ClearSelection();
    }

    /// <summary>Conserva en la interfaz solo información funcional y aplica formatos legibles.</summary>
    /// <param name="grid">Grilla financiera ya enlazada.</param>
    private static void ConfigureColumns(DataGridView grid)
    {
        foreach (DataGridViewColumn column in grid.Columns)
        {
            column.Visible =
                column.Name != "RowVersion"
                && column.Name != "Correlation"
                && !column.Name.EndsWith("Id", StringComparison.Ordinal);
            column.HeaderText = Header(column.Name);

            if (
                column.Name
                is "Balance"
                    or "Total"
                    or "Outstanding"
                    or "Amount"
                    or "OpeningAmount"
                    or "ExpectedAmount"
                    or "CountedAmount"
                    or "Difference"
            )
            {
                column.DefaultCellStyle.Format = "N2";
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }

            if (column.Name.EndsWith("Utc", StringComparison.Ordinal))
            {
                column.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
            }
        }

        var visibleCount = grid.Columns.Cast<DataGridViewColumn>().Count(column => column.Visible);
        grid.AutoSizeColumnsMode =
            visibleCount <= 7
                ? DataGridViewAutoSizeColumnsMode.Fill
                : DataGridViewAutoSizeColumnsMode.DisplayedCells;
    }

    /// <summary>Traduce el nombre técnico del contrato a un encabezado funcional.</summary>
    /// <param name="name">Nombre de la propiedad enlazada.</param>
    /// <returns>Texto visible según el idioma actual.</returns>
    private static string Header(string name) =>
        name switch
        {
            "Code" => L("Código", "Code"),
            "Name" => L("Nombre", "Name"),
            "Branch" => L("Sucursal", "Branch"),
            "CashBox" => L("Caja", "Cash box"),
            "User" => L("Usuario", "User"),
            "Client" => L("Cliente", "Customer"),
            "Currency" => L("Moneda", "Currency"),
            "Type" => L("Tipo", "Type"),
            "AffectsCash" => L("Afecta efectivo", "Affects cash"),
            "RequiresReference" => L("Requiere referencia", "Requires reference"),
            "OpenedUtc" => L("Apertura", "Opened"),
            "ClosedUtc" => L("Cierre", "Closed"),
            "OpeningAmount" => L("Importe inicial", "Opening amount"),
            "ExpectedAmount" => L("Efectivo esperado", "Expected cash"),
            "CountedAmount" => L("Efectivo contado", "Counted cash"),
            "Difference" => L("Diferencia", "Difference"),
            "OpeningObservation" => L("Observación de apertura", "Opening observation"),
            "ClosingObservation" => L("Observación de cierre", "Closing observation"),
            "SaleDateUtc" => L("Fecha de venta", "Sale date"),
            "PaymentDateUtc" => L("Fecha del cobro", "Payment date"),
            "ReversedUtc" => L("Fecha de reversión", "Reversed"),
            "PaymentMethod" => L("Medio de pago", "Payment method"),
            "Sale" => L("Venta", "Sale"),
            "Balance" => L("Saldo", "Balance"),
            "Total" => L("Total", "Total"),
            "Outstanding" => L("Saldo pendiente", "Outstanding"),
            "Amount" => L("Importe", "Amount"),
            "Reference" => L("Referencia", "Reference"),
            "Observation" => L("Observación", "Observation"),
            "ReversalReason" => L("Motivo de reversión", "Reversal reason"),
            "MovementType" => L("Movimiento", "Movement"),
            "Source" => L("Origen", "Source"),
            "MovementDateUtc" => L("Fecha", "Date"),
            "Description" => L("Descripción", "Description"),
            "StatusCode" => L("Estado", "Status"),
            _ => name,
        };

    /// <summary>Devuelve los estados disponibles para las operaciones financieras.</summary>
    /// <returns>Colección de registros u opciones obtenida por la operación.</returns>
    private static IReadOnlyList<KeyValuePair<string, string>> StatusOptions() =>
        [new("ACTIVO", L("Activo", "Active")), new("INACTIVO", L("Inactivo", "Inactive"))];

    /// <summary>Devuelve las opciones localizadas para valores afirmativos y negativos.</summary>
    /// <returns>Colección de registros u opciones obtenida por la operación.</returns>
    private static IReadOnlyList<KeyValuePair<string, string>> YesNo() =>
        [new("1", L("Sí", "Yes")), new("0", "No")];

    /// <summary>Devuelve los tipos de medios de pago admitidos.</summary>
    /// <returns>Colección de registros u opciones obtenida por la operación.</returns>
    private static IReadOnlyList<KeyValuePair<string, string>> Types() =>
        [
            new("EFECTIVO", L("Efectivo", "Cash")),
            new("TRANSFERENCIA", L("Transferencia", "Transfer")),
            new("TARJETA", L("Tarjeta", "Card")),
            new("CHEQUE", L("Cheque", "Check")),
            new("OTRO", L("Otro", "Other")),
        ];

    /// <summary>Muestra una advertencia funcional al usuario.</summary>
    private void Warn() =>
        MessageBox.Show(
            this,
            Localization.Text("Finance_SelectRecord"),
            "OxiTigre",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning
        );

    /// <summary>Muestra un error funcional sin exponer detalles técnicos.</summary>
    /// <param name="exception">Excepción cuya descripción segura se presenta al usuario.</param>
    private void Error(Exception exception) =>
        MessageBox.Show(
            this,
            exception.Message,
            "OxiTigre",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error
        );

    /// <summary>Selecciona el texto en español o inglés según la cultura activa.</summary>
    /// <param name="spanish">Texto que se usa cuando la cultura activa es española.</param>
    /// <param name="english">Texto que se usa cuando la cultura activa es inglesa.</param>
    /// <returns>Texto normalizado, localizado o formateado por la operación.</returns>
    private static string L(string spanish, string english) =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en" ? english : spanish;
}

/// <summary>Captura un cobro con múltiples medios y aplicaciones sin agregar dependencias visuales.</summary>
internal sealed class CustomerPaymentForm : Form
{
    private readonly FinanceSnapshotResponse _snapshot;
    private readonly ComboBox _client = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = 310,
    };
    private readonly ComboBox _session = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = 310,
    };
    private readonly NumericUpDown _total = UiTheme.Number(2, 160);
    private readonly DateTimePicker _date = new()
    {
        Width = 210,
        Format = DateTimePickerFormat.Custom,
        CustomFormat = "dd/MM/yyyy HH:mm",
    };
    private readonly TextBox _observation = new()
    {
        Width = 500,
        PlaceholderText = Localization.Text("Finance_Observation"),
    };
    private readonly DataGridView _methods = new()
    {
        Dock = DockStyle.Fill,
        AllowUserToAddRows = false,
    };
    private readonly DataGridView _applications = new()
    {
        Dock = DockStyle.Fill,
        AllowUserToAddRows = false,
    };
    private readonly Label _applicationHint = new()
    {
        Dock = DockStyle.Bottom,
        Height = 34,
        Padding = new Padding(8),
        ForeColor = Color.DimGray,
    };
    private Button? _addApplication;

    /// <summary>Solicitud validada cuando el usuario confirma.</summary>
    internal RegisterCustomerPaymentRequest? Request { get; private set; }

    /// <summary>Inicializa el editor con clientes, ventas, medios y caja propia disponibles.</summary>
    /// <param name="snapshot">Información financiera vigente usada por el editor.</param>
    /// <param name="userId">Usuario responsable que debe poseer una caja abierta.</param>
    internal CustomerPaymentForm(FinanceSnapshotResponse snapshot, long userId)
    {
        _snapshot = snapshot;
        Text = Localization.Text("Finance_NewPayment");
        Width = 1050;
        Height = 760;
        MinimumSize = new Size(900, 650);
        StartPosition = FormStartPosition.CenterParent;
        UiTheme.Apply(this);
        UiTheme.Grid(_methods);
        UiTheme.Grid(_applications);
        _client.DisplayMember = nameof(FinanceClientResponse.Name);
        _client.ValueMember = nameof(FinanceClientResponse.ClientId);
        _client.DataSource = snapshot.Clients.ToList();
        _session.DisplayMember = nameof(CashSessionChoice.Name);
        _session.ValueMember = nameof(CashSessionChoice.Id);
        _session.DataSource = new[]
        {
            new CashSessionChoice(
                null,
                L("Sin caja (solo medios no efectivos)", "No cash shift (non-cash methods only)")
            ),
        }
            .Concat(
                snapshot
                    .CashSessions.Where(x => x.StatusCode == "ABIERTA" && x.OpeningUserId == userId)
                    .Select(x => new CashSessionChoice(
                        x.CashSessionId,
                        $"{x.CashBox} · {x.OpenedUtc.ToLocalTime():dd/MM HH:mm}"
                    ))
            )
            .ToList();

        _total.ReadOnly = true;
        _total.TabStop = false;
        BuildMethodColumns();
        BuildApplicationColumns();
        var header = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 95,
            Padding = new Padding(12),
            WrapContents = true,
        };
        header.Controls.AddRange([
            Label(L("Cliente", "Customer")),
            _client,
            Label(L("Caja abierta", "Open cash")),
            _session,
            Label(L("Fecha", "Date")),
            _date,
            Label(L("Total ARS", "ARS total")),
            _total,
            _observation,
            UiTheme.HelpButton(Localization.Text("Finance_PaymentHelp")),
        ]);
        var grids = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(8),
        };
        grids.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grids.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grids.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var methodsPanel = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 6, 0) };
        methodsPanel.Controls.Add(_methods);
        methodsPanel.Controls.Add(GridToolbar(true));
        var applicationsPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(6, 0, 0, 0),
        };
        applicationsPanel.Controls.Add(_applications);
        applicationsPanel.Controls.Add(_applicationHint);
        applicationsPanel.Controls.Add(GridToolbar(false));
        grids.Controls.Add(methodsPanel, 0, 0);
        grids.Controls.Add(applicationsPanel, 1, 0);
        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 56,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(10),
        };
        var save = UiTheme.Button(Localization.Text("Common_Save"), 0, 0, 130);
        var cancel = UiTheme.Button(Localization.Text("Common_Cancel"), 0, 0, 130);
        cancel.DialogResult = DialogResult.Cancel;
        save.Click += (_, _) => Save();
        footer.Controls.AddRange([save, cancel]);
        Controls.Add(grids);
        Controls.Add(footer);
        Controls.Add(header);
        CancelButton = cancel;
        _client.SelectedIndexChanged += (_, _) => RefreshSales();
        _methods.CellValueChanged += (_, eventArgs) =>
        {
            if (
                eventArgs.ColumnIndex >= 0
                && _methods.Columns[eventArgs.ColumnIndex].Name == "Amount"
            )
                RecalculateTotal();
        };
        _methods.RowsRemoved += (_, _) => RecalculateTotal();
        _methods.Rows.Add();
        RefreshSales();
    }

    /// <summary>Configura medios, importes y referencias de cada parte del cobro.</summary>
    private void BuildMethodColumns()
    {
        _methods.Columns.Add(
            new DataGridViewComboBoxColumn
            {
                Name = "Method",
                HeaderText = L("Medio de pago", "Payment method"),
                Width = 210,
                DataSource = _snapshot.PaymentMethods.Where(x => x.StatusCode == "ACTIVO").ToList(),
                DisplayMember = nameof(PaymentMethodResponse.Name),
                ValueMember = nameof(PaymentMethodResponse.PaymentMethodId),
            }
        );
        _methods.Columns.Add(
            new DataGridViewTextBoxColumn
            {
                Name = "Amount",
                HeaderText = L("Importe", "Amount"),
                Width = 130,
            }
        );
        _methods.Columns.Add(
            new DataGridViewTextBoxColumn
            {
                Name = "Reference",
                HeaderText = L("Referencia", "Reference"),
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            }
        );
    }

    /// <summary>Configura la distribución opcional del cobro entre ventas pendientes.</summary>
    private void BuildApplicationColumns()
    {
        _applications.Columns.Add(
            new DataGridViewComboBoxColumn
            {
                Name = "Sale",
                HeaderText = L("Venta pendiente", "Outstanding sale"),
                Width = 300,
            }
        );
        _applications.Columns.Add(
            new DataGridViewTextBoxColumn
            {
                Name = "Amount",
                HeaderText = L("Importe aplicado", "Applied amount"),
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            }
        );
    }

    /// <summary>Permite agregar o quitar medios o aplicaciones de la grilla correspondiente.</summary>
    /// <param name="methods">Selecciona la grilla de medios; <see langword="false"/> selecciona ventas.</param>
    /// <returns>Barra con título y acciones para la grilla elegida.</returns>
    private Control GridToolbar(bool methods)
    {
        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 48,
            Padding = new Padding(6),
            WrapContents = false,
        };
        bar.Controls.Add(
            new Label
            {
                Text = methods
                    ? L("Cómo se recibió", "How it was received")
                    : L("A qué venta se aplica", "Sale allocation"),
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = UiTheme.Primary,
                Margin = new Padding(3, 8, 12, 0),
            }
        );
        var add = UiTheme.Button(L("Agregar", "Add"), 0, 0, 100);
        var remove = UiTheme.Button(L("Quitar", "Remove"), 0, 0, 100);
        var grid = methods ? _methods : _applications;
        add.Click += (_, _) => grid.Rows.Add();
        remove.Click += (_, _) =>
        {
            if (grid.CurrentRow is { IsNewRow: false } row)
                grid.Rows.Remove(row);
        };
        if (!methods)
            _addApplication = add;
        bar.Controls.AddRange([add, remove]);
        return bar;
    }

    /// <summary>Ofrece solo ventas pendientes del cliente elegido y explica cuándo el cobro queda a favor.</summary>
    /// <exception cref="InvalidOperationException">La grilla perdió la columna de ventas necesaria para el enlace.</exception>
    private void RefreshSales()
    {
        if (_client.SelectedValue is not long clientId)
            return;
        var choices = _snapshot
            .Sales.Where(x => x.ClientId == clientId && x.Outstanding > 0)
            .Select(x => new SaleChoice(x.SaleId, $"{x.Code} · {x.Outstanding:N2} {x.Currency}"))
            .ToList();
        var column =
            _applications.Columns["Sale"] as DataGridViewComboBoxColumn
            ?? throw new InvalidOperationException("No se encontró la columna de ventas.");
        column.DataSource = choices;
        column.DisplayMember = nameof(SaleChoice.Name);
        column.ValueMember = nameof(SaleChoice.Id);
        _applications.Rows.Clear();
        if (_addApplication is not null)
            _addApplication.Enabled = choices.Count > 0;
        _applicationHint.Text =
            choices.Count == 0
                ? L(
                    "El cliente no tiene ventas pendientes. El cobro completo quedará como saldo a favor.",
                    "The customer has no outstanding sales. The full payment will remain as credit."
                )
                : L(
                    "Aplicar es opcional. Lo no aplicado queda como saldo a favor del cliente.",
                    "Allocation is optional. Any unapplied amount remains as customer credit."
                );
    }

    /// <summary>Calcula el total desde los medios para evitar que el empleado lo escriba dos veces.</summary>
    private void RecalculateTotal()
    {
        var total = _methods
            .Rows.Cast<DataGridViewRow>()
            .Where(row => !row.IsNewRow)
            .Select(row =>
                decimal.TryParse(
                    Convert.ToString(row.Cells["Amount"].Value),
                    NumberStyles.Number,
                    CultureInfo.CurrentCulture,
                    out var amount
                )
                    ? amount
                    : 0
            )
            .Sum();
        _total.Value = Math.Clamp(total, _total.Minimum, _total.Maximum);
    }

    /// <summary>Valida importes, referencias, caja y aplicaciones antes de construir la solicitud de cobro.</summary>
    private void Save()
    {
        try
        {
            if (_client.SelectedValue is not long clientId)
                throw new InvalidOperationException(
                    L("Seleccioná un cliente.", "Select a customer.")
                );
            var methods = _methods
                .Rows.Cast<DataGridViewRow>()
                .Where(x => !x.IsNewRow && x.Cells["Method"].Value is not null)
                .Select(x => new PaymentMethodAmountRequest(
                    Convert.ToInt64(x.Cells["Method"].Value),
                    Parse(x.Cells["Amount"].Value),
                    Convert.ToString(x.Cells["Reference"].Value)
                ))
                .ToList();
            var applications = _applications
                .Rows.Cast<DataGridViewRow>()
                .Where(x => !x.IsNewRow && x.Cells["Sale"].Value is not null)
                .Select(x => new SaleApplicationAmountRequest(
                    Convert.ToInt64(x.Cells["Sale"].Value),
                    Parse(x.Cells["Amount"].Value)
                ))
                .ToList();
            if (
                methods.Count == 0
                || methods.Any(x => x.Amount <= 0)
                || Math.Abs(methods.Sum(x => x.Amount) - _total.Value) > 0.0001m
            )
                throw new InvalidOperationException(
                    L(
                        "Los medios deben sumar exactamente el total.",
                        "Payment methods must total the payment."
                    )
                );
            if (
                applications.Any(x => x.Amount <= 0)
                || applications.Sum(x => x.Amount) > _total.Value
            )
                throw new InvalidOperationException(
                    L(
                        "Las aplicaciones no pueden superar el total.",
                        "Applications cannot exceed the total."
                    )
                );
            foreach (var method in methods)
            {
                var definition = _snapshot.PaymentMethods.Single(x =>
                    x.PaymentMethodId == method.PaymentMethodId
                );
                if (definition.RequiresReference && string.IsNullOrWhiteSpace(method.Reference))
                    throw new InvalidOperationException(
                        L(
                            $"{definition.Name} requiere referencia.",
                            $"{definition.Name} requires a reference."
                        )
                    );
                if (definition.AffectsCash && _session.SelectedValue is null)
                    throw new InvalidOperationException(
                        L("El efectivo requiere una caja abierta.", "Cash requires an open shift.")
                    );
            }
            Request = new(
                clientId,
                _session.SelectedValue as long?,
                _date.Value.ToUniversalTime(),
                "ARS",
                _total.Value,
                _observation.Text.Trim(),
                methods,
                applications
            );
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception exception) when (exception is InvalidOperationException or FormatException)
        {
            MessageBox.Show(
                this,
                exception.Message,
                "OxiTigre",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
        }
    }

    /// <summary>Interpreta un importe con la cultura visible para el empleado.</summary>
    /// <param name="value">Valor escrito en una celda de importe.</param>
    /// <returns>Importe decimal válido.</returns>
    /// <exception cref="FormatException">El valor no representa un importe en la cultura actual.</exception>
    private static decimal Parse(object? value) =>
        decimal.TryParse(
            Convert.ToString(value),
            NumberStyles.Number,
            CultureInfo.CurrentCulture,
            out var result
        )
            ? result
            : throw new FormatException(L("Ingresá un importe válido.", "Enter a valid amount."));

    /// <summary>Crea una etiqueta visual para el formulario de cobro.</summary>
    /// <param name="text">Texto que se muestra, interpreta o transforma.</param>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private static Label Label(string text) =>
        new()
        {
            Text = text,
            AutoSize = true,
            Margin = new Padding(8, 8, 3, 0),
        };

    /// <summary>Selecciona el texto en español o inglés según la cultura activa.</summary>
    /// <param name="spanish">Texto que se usa cuando la cultura activa es española.</param>
    /// <param name="english">Texto que se usa cuando la cultura activa es inglesa.</param>
    /// <returns>Texto normalizado, localizado o formateado por la operación.</returns>
    private static string L(string spanish, string english) =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en" ? english : spanish;

    private sealed record CashSessionChoice(long? Id, string Name);

    private sealed record SaleChoice(long Id, string Name);
}
