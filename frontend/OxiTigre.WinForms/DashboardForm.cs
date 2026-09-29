/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.WinForms.DashboardForm
Archivo: DashboardForm.cs | Versión: 2.6.0 | Fecha: 2026-09-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Presenta el shell persistente y los resúmenes operativos del escritorio.
Historial: 2.0.0 | 2026-09-03 | FABRICA | Agustin Omar Cauzi | Implementación del Diseño 1 aprobado.
Historial: 2.1.0 | 2026-09-03 | FABRICA | Agustin Omar Cauzi | Corrección adaptable de encabezados, tarjetas y listas.
Historial: 2.2.0 | 2026-09-03 | FABRICA | Agustin Omar Cauzi | Marca oficial, iconografía legible y tablero Comercial.
Historial: 2.3.0 | 2026-09-03 | FABRICA | Agustin Omar Cauzi | Retorno operativo visible y tablero Inventario.
Historial: 2.4.0 | 2026-09-03 | FABRICA | Agustin Omar Cauzi | Icono centrado y tablero operativo de Compras.
Historial: 2.5.0 | 2026-09-20 | FABRICA | Agustin Omar Cauzi | Tablero operativo e icono seguro de Trazabilidad.
Historial: 2.6.0 | 2026-09-20 | FABRICA | Agustin Omar Cauzi | Tableros especializados de Logística, Finanzas, Seguridad y Configuración.
===============================================================================
*/
using System.Globalization;
using OxiTigre.ApiClient;
using OxiTigre.Contracts.Commercial;
using OxiTigre.Contracts.Finance;
using OxiTigre.Contracts.Inventory;
using OxiTigre.Contracts.Logistics;
using OxiTigre.Contracts.Purchasing;
using OxiTigre.Contracts.Security;

namespace OxiTigre.WinForms;

/// <summary>
/// Mantiene la navegación global visible y resume información real de los módulos autorizados.
/// </summary>
internal sealed class DashboardForm : Form
{
    private static readonly Color BrandGreen = Color.FromArgb(5, 78, 55);
    private static readonly Color BrandGreenLight = Color.FromArgb(17, 104, 74);
    private static readonly Color BrandGold = Color.FromArgb(239, 174, 25);
    private static readonly Color MutedText = Color.FromArgb(100, 116, 139);
    private static readonly Color Border = Color.FromArgb(218, 226, 221);
    private static readonly Color SoftGreen = Color.FromArgb(232, 245, 237);
    private static readonly Color SoftGold = Color.FromArgb(255, 248, 227);
    private static readonly Color SoftRed = Color.FromArgb(254, 242, 242);

    private readonly OxiTigreApiClient _apiClient;
    private readonly LoginResponse _session;
    private readonly string _workplaceName;
    private readonly System.Windows.Forms.Timer _sessionMonitor = new() { Interval = 5_000 };
    private readonly Panel _contentHost = new()
    {
        Dock = DockStyle.Fill,
        BackColor = UiTheme.Background,
    };
    private readonly Label _pageTitle = new();
    private readonly Label _connectionStatus = new();
    private readonly Label _updatedStatus = new();
    private readonly TextBox _globalSearch = new();
    private readonly ContextMenuStrip _searchMenu = new();
    private readonly NotificationButton _notificationButton = new();
    private readonly Dictionary<DashboardModule, SidebarButton> _navigation = [];
    private readonly List<SearchEntry> _searchEntries = [];
    private DashboardData _data = DashboardData.Empty;
    private DashboardModule _selectedModule = DashboardModule.Home;
    private Form? _embeddedForm;
    private bool _checkingSession;
    private bool _endingSession;
    private bool _sessionClosed;

    /// <summary>Indica que el usuario cerró sesión para volver a la pantalla de acceso.</summary>
    internal bool LoginAgain { get; private set; }

    /// <summary>Indica que la ventana debe reconstruirse en otro idioma conservando la sesión.</summary>
    internal bool ReloadInterface { get; private set; }

    /// <summary>Inicializa el panel principal según la empresa, sucursal y permisos autenticados.</summary>
    /// <param name="apiClient">Cliente HTTP que obtiene información autorizada desde la API.</param>
    /// <param name="session">Sesión autenticada con empresa, sucursal, roles y permisos.</param>
    /// <param name="companyName">Nombre visible de la empresa y la sucursal seleccionadas.</param>
    internal DashboardForm(OxiTigreApiClient apiClient, LoginResponse session, string companyName)
    {
        _apiClient = apiClient;
        _session = session;
        _workplaceName = companyName;

        Text = Localization.Text("Dashboard_Title");
        ClientSize = new Size(1440, 880);
        MinimumSize = new Size(1180, 720);
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;
        KeyPreview = true;
        UiTheme.Apply(this);

        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = UiTheme.Background,
        };
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 238));
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        shell.Controls.Add(BuildSidebar(), 0, 0);
        shell.Controls.Add(BuildWorkspace(), 1, 0);
        Controls.Add(shell);

        BuildSearchEntries();
        _globalSearch.TextChanged += (_, _) => UpdateSearchSuggestions();
        _globalSearch.KeyDown += GlobalSearch_KeyDown;
        _sessionMonitor.Tick += SessionMonitor_Tick;
        Shown += DashboardForm_Shown;
        FormClosing += DashboardForm_FormClosing;

        ShowHome();
    }

    /// <summary>Construye la barra lateral persistente con los módulos autorizados.</summary>
    /// <returns>Panel oscuro que permanece visible durante toda la sesión.</returns>
    private Control BuildSidebar()
    {
        var sidebar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = BrandGreen,
            Padding = Padding.Empty,
            Margin = Padding.Empty,
            ColumnCount = 1,
            RowCount = 3,
        };
        sidebar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 102));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        sidebar.Controls.Add(BuildBrandArea(), 0, 0);
        sidebar.Controls.Add(BuildNavigationArea(), 0, 1);
        sidebar.Controls.Add(BuildLogoutArea(), 0, 2);
        return sidebar;
    }

    /// <summary>Construye la identificación visual compacta de OxiTigre.</summary>
    /// <returns>Encabezado de la barra lateral.</returns>
    private static Control BuildBrandArea()
    {
        var brand = new Panel { Dock = DockStyle.Fill, BackColor = BrandGreen };
        var mark = new BrandMark { Location = new Point(18, 16), Size = new Size(58, 58) };
        brand.Controls.Add(mark);
        brand.Controls.Add(
            new Label
            {
                Text = "OxiTigre",
                Left = 80,
                Top = 24,
                Width = 135,
                Height = 32,
                Font = new Font("Segoe UI", 19F, FontStyle.Bold),
                ForeColor = Color.White,
            }
        );
        brand.Controls.Add(
            new Label
            {
                Text = L("Gestión industrial", "Industrial management"),
                Left = 82,
                Top = 57,
                Width = 132,
                Height = 20,
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(192, 223, 210),
            }
        );
        return brand;
    }

    /// <summary>Construye las opciones principales respetando los permisos de la sesión.</summary>
    /// <returns>Contenedor desplazable con una opción por módulo.</returns>
    private Control BuildNavigationArea()
    {
        var container = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = BrandGreen,
            Padding = new Padding(10, 12, 10, 8),
        };
        var list = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 10,
            BackColor = BrandGreen,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
        };
        list.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddNavigation(list, DashboardModule.Home, L("Inicio", "Home"), true);
        AddNavigation(
            list,
            DashboardModule.Commercial,
            L("Comercial", "Commercial"),
            Can("COMERCIAL.CONSULTAR")
        );
        AddNavigation(
            list,
            DashboardModule.Inventory,
            L("Inventario", "Inventory"),
            Can("INVENTARIO.CONSULTAR")
        );
        AddNavigation(
            list,
            DashboardModule.Purchasing,
            L("Compras", "Purchasing"),
            Can("COMPRAS.CONSULTAR")
        );
        AddNavigation(
            list,
            DashboardModule.Traceability,
            L("Trazabilidad", "Traceability"),
            Can("INVENTARIO.CONSULTAR")
        );
        AddNavigation(
            list,
            DashboardModule.Logistics,
            L("Logística", "Logistics"),
            Can("LOGISTICA.CONSULTAR")
        );
        AddNavigation(
            list,
            DashboardModule.Finance,
            L("Finanzas", "Finance"),
            Can("FINANZAS.CONSULTAR")
        );
        AddNavigation(
            list,
            DashboardModule.Security,
            L("Seguridad", "Security"),
            IsAdministrator()
        );
        AddNavigation(
            list,
            DashboardModule.Configuration,
            L("Configuración", "Configuration"),
            Can("CONFIGURACION.CONSULTAR")
        );

        container.Controls.Add(list);
        return container;
    }

    /// <summary>Agrega un botón accesible al listado lateral.</summary>
    /// <param name="list">Contenedor vertical de navegación.</param>
    /// <param name="module">Módulo asociado al botón.</param>
    /// <param name="text">Nombre visible del módulo.</param>
    /// <param name="enabled">Indica si la sesión posee acceso.</param>
    private void AddNavigation(
        TableLayoutPanel list,
        DashboardModule module,
        string text,
        bool enabled
    )
    {
        var button = new SidebarButton(module)
        {
            Text = text,
            Dock = DockStyle.Top,
            Height = 48,
            Font = new Font("Segoe UI", 9.5F),
            Margin = new Padding(0, 2, 0, 2),
            Enabled = enabled,
            AccessibleName = text,
        };
        button.Click += (_, _) => ShowModuleOverview(module);
        _navigation[module] = button;
        list.Controls.Add(button);
    }

    /// <summary>Construye el cierre de sesión discreto situado al pie de la barra.</summary>
    /// <returns>Panel inferior con identidad y cierre de sesión.</returns>
    private Control BuildLogoutArea()
    {
        var area = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(4, 66, 47),
            Padding = new Padding(16, 12, 16, 16),
        };
        var user = new Label
        {
            Dock = DockStyle.Top,
            Height = 34,
            Text = _session.DisplayName,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.White,
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        var logout = UiTheme.Button(L("Cerrar sesión", "Sign out"), 0, 0, 190, ButtonTone.Danger);
        logout.Dock = DockStyle.Bottom;
        logout.Height = 38;
        logout.Click += (_, _) =>
        {
            LoginAgain = true;
            Close();
        };
        area.Controls.Add(logout);
        area.Controls.Add(user);
        return area;
    }

    /// <summary>Construye el encabezado, el contenido intercambiable y el estado inferior.</summary>
    /// <returns>Área principal situada a la derecha de la navegación.</returns>
    private Control BuildWorkspace()
    {
        var workspace = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = UiTheme.Background,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
        };
        workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        workspace.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        workspace.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        workspace.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        workspace.Controls.Add(BuildTopBar(), 0, 0);
        workspace.Controls.Add(_contentHost, 0, 1);
        workspace.Controls.Add(BuildStatusBar(), 0, 2);
        return workspace;
    }

    /// <summary>Construye el encabezado global del sistema.</summary>
    /// <returns>Barra con título, búsqueda, sucursal, avisos y usuario.</returns>
    private Control BuildTopBar()
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            RowCount = 1,
            BackColor = Color.White,
            Margin = Padding.Empty,
            Padding = new Padding(22, 14, 18, 12),
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 155));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 214));
        header.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _pageTitle.Dock = DockStyle.Fill;
        _pageTitle.Text = L("Inicio", "Home");
        _pageTitle.Font = new Font("Segoe UI", 17F, FontStyle.Bold);
        _pageTitle.ForeColor = BrandGreen;
        _pageTitle.TextAlign = ContentAlignment.MiddleLeft;
        header.Controls.Add(_pageTitle, 0, 0);

        var searchArea = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(8, 7, 18, 7),
        };
        _globalSearch.Dock = DockStyle.Fill;
        _globalSearch.Font = new Font("Segoe UI", 10.5F);
        _globalSearch.PlaceholderText = L(
            "Buscar módulos y acciones...",
            "Search modules and actions..."
        );
        _globalSearch.AccessibleName = L("Búsqueda global", "Global search");
        searchArea.Controls.Add(_globalSearch);
        header.Controls.Add(searchArea, 1, 0);

        var workplace = new Button
        {
            Dock = DockStyle.Fill,
            Text = _workplaceName + "  ▾",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 6, 0),
            BackColor = Color.White,
            ForeColor = BrandGreen,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            AutoEllipsis = true,
            AccessibleName = L("Empresa y sucursal activa", "Active company and branch"),
        };
        workplace.FlatAppearance.BorderColor = Border;
        workplace.FlatAppearance.MouseOverBackColor = SoftGreen;
        workplace.Click += (_, _) => ChangeWorkplace();
        header.Controls.Add(workplace, 2, 0);

        _notificationButton.Dock = DockStyle.Fill;
        _notificationButton.Margin = new Padding(6, 0, 6, 0);
        _notificationButton.AccessibleName = L(
            "Avisos que requieren atención",
            "Notifications requiring attention"
        );
        _notificationButton.Click += (_, _) => ShowHome(true);
        header.Controls.Add(_notificationButton, 3, 0);
        header.Controls.Add(BuildUserBadge(), 4, 0);
        return header;
    }

    /// <summary>Construye la identificación del usuario conectado.</summary>
    /// <returns>Panel con iniciales, nombre y rol principal.</returns>
    private Control BuildUserBadge()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        var initials = new InitialsBadge(GetInitials(_session.DisplayName))
        {
            Left = 4,
            Top = 5,
            Size = new Size(40, 40),
        };
        panel.Controls.Add(initials);
        var name = new Label
        {
            Text = _session.DisplayName,
            Left = 52,
            Top = 4,
            Width = 154,
            Height = 23,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = UiTheme.Text,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            AutoEllipsis = true,
        };
        var role = new Label
        {
            Text = _session.Roles.FirstOrDefault() ?? L("Usuario", "User"),
            Left = 52,
            Top = 27,
            Width = 154,
            Height = 20,
            ForeColor = MutedText,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            AutoEllipsis = true,
        };
        panel.Controls.Add(name);
        panel.Controls.Add(role);
        panel.Resize += (_, _) =>
        {
            name.Width = Math.Max(70, panel.ClientSize.Width - name.Left);
            role.Width = name.Width;
        };
        return panel;
    }

    /// <summary>Construye la franja inferior con sincronización y disponibilidad.</summary>
    /// <returns>Barra de estado compartida por todas las pantallas.</returns>
    private Control BuildStatusBar()
    {
        var bar = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(22, 0, 18, 0),
        };
        _connectionStatus.Dock = DockStyle.Left;
        _connectionStatus.Width = 390;
        _connectionStatus.Text = L(
            "Comprobando conexión con la API...",
            "Checking API connection..."
        );
        _connectionStatus.ForeColor = MutedText;
        _connectionStatus.TextAlign = ContentAlignment.MiddleLeft;
        _updatedStatus.Dock = DockStyle.Right;
        _updatedStatus.Width = 240;
        _updatedStatus.TextAlign = ContentAlignment.MiddleRight;
        _updatedStatus.ForeColor = MutedText;
        bar.Controls.Add(_updatedStatus);
        bar.Controls.Add(_connectionStatus);
        return bar;
    }

    /// <summary>Presenta el resumen general del día.</summary>
    /// <param name="focusDecisions">Indica si debe desplazar la vista hasta las decisiones pendientes.</param>
    private void ShowHome(bool focusDecisions = false)
    {
        CloseEmbeddedForm();
        SelectNavigation(DashboardModule.Home);
        _pageTitle.Text = L("Inicio", "Home");
        _contentHost.Controls.Clear();

        var home = BuildHomeView(out var decisions);
        _contentHost.Controls.Add(home);
        if (focusDecisions)
        {
            home.PerformLayout();
            home.ScrollControlIntoView(decisions);
        }
    }

    /// <summary>Construye el Home con flujo del negocio, decisiones y trazabilidad reciente.</summary>
    /// <param name="decisionsPanel">Devuelve el panel al que apunta el botón de avisos.</param>
    /// <returns>Vista desplazable y adaptable del resumen general.</returns>
    private Panel BuildHomeView(out Control decisionsPanel)
    {
        var scroll = CreateScrollablePage();
        var page = CreatePageLayout();
        page.Controls.Add(BuildHomeHeading(), 0, 0);
        page.Controls.Add(BuildBusinessFlow(), 0, 1);

        var lower = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 10, 0, 0),
            BackColor = UiTheme.Background,
        };
        lower.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        lower.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        lower.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        lower.Controls.Add(BuildDecisionSection(), 0, 0);
        lower.Controls.Add(BuildActivitySection(), 1, 0);
        decisionsPanel = lower.GetControlFromPosition(0, 0)!;
        page.Controls.Add(lower, 0, 2);
        scroll.Controls.Add(page);
        ResizePageWith(scroll, page);
        return scroll;
    }

    /// <summary>Construye el saludo y el estado de actualización del Home.</summary>
    /// <returns>Encabezado introductorio del resumen.</returns>
    private Control BuildHomeHeading()
    {
        var decisions = BuildDecisions();
        var loading = ReferenceEquals(_data, DashboardData.Empty);
        var title =
            loading ? L("Cargando información", "Loading information")
            : decisions.Any(item => item.Tone == DecisionTone.Danger)
                ? L("Hay prioridades para revisar", "There are priorities to review")
            : decisions.Count > 0 ? L("Hay tareas para continuar", "There are tasks to continue")
            : L("Todo bajo control", "Everything under control");
        var subtitle =
            loading
                ? L(
                    "Estamos consultando el estado operativo de la sucursal.",
                    "We are loading the branch operating status."
                )
            : decisions.Count > 0
                ? L(
                    $"Hola, {_session.DisplayName}. Detectamos {decisions.Count:N0} asuntos que requieren atención.",
                    $"Hello, {_session.DisplayName}. We found {decisions.Count:N0} items requiring attention."
                )
            : L(
                $"Hola, {_session.DisplayName}. No hay decisiones urgentes con la información disponible.",
                $"Hello, {_session.DisplayName}. There are no urgent decisions in the available information."
            );
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.Background,
            Margin = new Padding(0, 0, 0, 8),
        };
        panel.Controls.Add(
            new Label
            {
                Text = title,
                Left = 0,
                Top = 4,
                Width = 720,
                Height = 43,
                Font = new Font("Segoe UI", 27F, FontStyle.Bold),
                ForeColor = BrandGreen,
            }
        );
        panel.Controls.Add(
            new Label
            {
                Text = subtitle,
                Left = 2,
                Top = 51,
                Width = 620,
                Height = 26,
                Font = new Font("Segoe UI", 11F),
                ForeColor = MutedText,
            }
        );
        var refresh = UiTheme.Button(
            L("Actualizar datos", "Refresh data"),
            0,
            0,
            138,
            ButtonTone.Neutral
        );
        refresh.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        refresh.Top = 18;
        refresh.Left = 930;
        refresh.Click += async (_, _) => await LoadDashboardAsync();
        panel.Controls.Add(refresh);
        panel.Resize += (_, _) =>
            refresh.Left = Math.Max(650, panel.ClientSize.Width - refresh.Width);
        return panel;
    }

    /// <summary>Construye el recorrido horizontal del negocio con cantidades reales.</summary>
    /// <returns>Tarjeta con Pedidos, Stock, Despacho, Entrega y Cobro.</returns>
    private Control BuildBusinessFlow()
    {
        var card = CreateCard();
        card.Margin = new Padding(0, 0, 0, 8);
        var title = SectionTitle(L("Flujo operativo", "Operating flow"));
        title.Dock = DockStyle.Top;
        title.Height = 37;
        var flow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 9,
            RowCount = 1,
            BackColor = Color.White,
            Padding = new Padding(0, 5, 0, 0),
        };
        for (var index = 0; index < 9; index++)
            flow.ColumnStyles.Add(
                index % 2 == 0
                    ? new ColumnStyle(SizeType.Percent, 20)
                    : new ColumnStyle(SizeType.Absolute, 24)
            );
        flow.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var metrics = GetFlowMetrics();
        for (var index = 0; index < metrics.Count; index++)
        {
            flow.Controls.Add(CreateFlowMetric(metrics[index]), index * 2, 0);
            if (index < metrics.Count - 1)
                flow.Controls.Add(
                    new Label
                    {
                        Dock = DockStyle.Fill,
                        Text = "›",
                        Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                        ForeColor = BrandGold,
                        TextAlign = ContentAlignment.MiddleCenter,
                    },
                    index * 2 + 1,
                    0
                );
        }
        card.Controls.Add(flow);
        card.Controls.Add(title);
        return card;
    }

    /// <summary>Obtiene las cinco métricas principales sin mezclar unidades de stock.</summary>
    /// <returns>Lista ordenada según el flujo normal del negocio.</returns>
    private IReadOnlyList<MetricInfo> GetFlowMetrics()
    {
        var openOrders =
            _data
                .Sales?.Orders.DistinctBy(item => item.OrderId)
                .Count(item => DashboardSummaryLogic.IsOpenOrder(item.StatusCode))
            ?? 0;
        var availablePositions =
            _data.Inventory?.Stock.Count(item => item.AvailableQuantity > 0) ?? 0;
        var todayRoutes =
            _data.Logistics?.Routes.Count(item =>
                item.RouteDate == DateOnly.FromDateTime(DateTime.Today)
                && DashboardSummaryLogic.IsActiveRoute(item.StatusCode)
            )
            ?? 0;
        var deliveredStops = CountDeliveredStopsToday();
        var pendingCollections = _data.Finance?.Sales.Count(item => item.Outstanding > 0) ?? 0;

        return
        [
            new(
                L("Pedidos", "Orders"),
                openOrders.ToString("N0"),
                L("abiertos", "open"),
                DashboardModule.Commercial
            ),
            new(
                L("Stock", "Stock"),
                availablePositions.ToString("N0"),
                L("posiciones disponibles", "available positions"),
                DashboardModule.Inventory
            ),
            new(
                L("Despacho", "Dispatch"),
                todayRoutes.ToString("N0"),
                L("hojas activas hoy", "active routes today"),
                DashboardModule.Logistics
            ),
            new(
                L("Entrega", "Delivery"),
                deliveredStops.ToString("N0"),
                L("paradas completadas", "completed stops"),
                DashboardModule.Logistics
            ),
            new(
                L("Cobro", "Collection"),
                pendingCollections.ToString("N0"),
                L("ventas con saldo", "sales with balance"),
                DashboardModule.Finance
            ),
        ];
    }

    /// <summary>Cuenta paradas completadas correspondientes a hojas del día actual.</summary>
    /// <returns>Cantidad de entregas con resultado registrado.</returns>
    private int CountDeliveredStopsToday()
    {
        if (_data.Logistics is null)
            return 0;
        var routeIds = _data
            .Logistics.Routes.Where(item => item.RouteDate == DateOnly.FromDateTime(DateTime.Today))
            .Select(item => item.RouteId)
            .ToHashSet();
        return _data.Logistics.Stops.Count(item =>
            routeIds.Contains(item.RouteId)
            && (
                !string.IsNullOrWhiteSpace(item.Result)
                || DashboardSummaryLogic.IsCompleted(item.StatusCode)
            )
        );
    }

    /// <summary>Construye una métrica clickeable del flujo operativo.</summary>
    /// <param name="metric">Texto, valor y destino del indicador.</param>
    /// <returns>Botón visual con apariencia de tarjeta.</returns>
    private Control CreateFlowMetric(MetricInfo metric)
    {
        var button = new FlowMetricButton(metric.Title, metric.Value, metric.Detail)
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(4, 2, 4, 2),
            AccessibleName = $"{metric.Title}: {metric.Value} {metric.Detail}",
        };
        button.Click += (_, _) => ShowModuleOverview(metric.Module);
        return button;
    }

    /// <summary>Construye las alertas que requieren una decisión humana.</summary>
    /// <param name="source">Alertas específicas; si se omiten se usa el resumen general.</param>
    /// <param name="title">Título opcional de la sección.</param>
    /// <param name="subtitle">Explicación opcional de la sección.</param>
    /// <param name="emptyText">Mensaje opcional cuando no existen pendientes.</param>
    /// <returns>Tarjeta con hasta cuatro asuntos ordenados por criticidad.</returns>
    private Control BuildDecisionSection(
        IReadOnlyList<DecisionInfo>? source = null,
        string? title = null,
        string? subtitle = null,
        string? emptyText = null
    )
    {
        var card = CreateCard();
        card.Margin = new Padding(0, 0, 8, 0);
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(4),
            BackColor = Color.White,
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(
            CreateSectionHeader(
                title ?? L("Lo que necesita una decisión", "What needs a decision"),
                subtitle
                    ?? L(
                        "Asuntos operativos que conviene revisar primero.",
                        "Operational items worth reviewing first."
                    )
            ),
            0,
            0
        );

        var rows = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            AutoScroll = true,
            Padding = Padding.Empty,
            BackColor = Color.White,
        };
        rows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var decisions = (source ?? BuildDecisions()).Take(4).ToList();
        if (decisions.Count == 0)
        {
            rows.RowCount = 1;
            rows.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            rows.Controls.Add(
                CreateEmptyState(
                    emptyText
                        ?? L(
                            "No hay decisiones urgentes con la información disponible.",
                            "There are no urgent decisions in the available information."
                        )
                )
            );
        }
        else
        {
            rows.RowCount = decisions.Count + 1;
            for (var index = 0; index < decisions.Count; index++)
            {
                rows.RowStyles.Add(new RowStyle(SizeType.Absolute, 75));
                rows.Controls.Add(CreateDecisionRow(decisions[index]), 0, index);
            }
            rows.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        }
        layout.Controls.Add(rows, 0, 1);
        card.Controls.Add(layout);
        return card;
    }

    /// <summary>Calcula alertas reales a partir de los snapshots autorizados.</summary>
    /// <returns>Asuntos ordenados por severidad y cantidad.</returns>
    private IReadOnlyList<DecisionInfo> BuildDecisions()
    {
        var decisions = new List<DecisionInfo>();
        var belowMinimum = _data.Inventory?.Stock.Count(item => item.IsBelowMinimum) ?? 0;
        if (belowMinimum > 0)
            decisions.Add(
                new(
                    L("Stock por debajo del mínimo", "Stock below minimum"),
                    L(
                        $"{belowMinimum:N0} posiciones necesitan reposición.",
                        $"{belowMinimum:N0} positions need replenishment."
                    ),
                    L("Revisar", "Review"),
                    DashboardModule.Inventory,
                    DecisionTone.Danger
                )
            );

        var openPurchases =
            _data
                .Purchasing?.Orders.DistinctBy(item => item.PurchaseOrderId)
                .Count(item => DashboardSummaryLogic.IsOpenPurchaseOrder(item.StatusCode))
            ?? 0;
        if (openPurchases > 0)
            decisions.Add(
                new(
                    L("Compras en curso", "Purchases in progress"),
                    L(
                        $"{openPurchases:N0} órdenes todavía no están cerradas.",
                        $"{openPurchases:N0} orders are not closed yet."
                    ),
                    L("Ver compras", "View purchases"),
                    DashboardModule.Purchasing,
                    DecisionTone.Warning
                )
            );

        var plannedRequests =
            _data.Logistics?.Requests.Count(item => item.StatusCode is "PENDIENTE" or "PLANIFICADA")
            ?? 0;
        if (plannedRequests > 0)
            decisions.Add(
                new(
                    L("Solicitudes por planificar", "Requests to schedule"),
                    L(
                        $"{plannedRequests:N0} solicitudes requieren hoja de ruta.",
                        $"{plannedRequests:N0} requests need a route sheet."
                    ),
                    L("Planificar", "Schedule"),
                    DashboardModule.Logistics,
                    DecisionTone.Warning
                )
            );

        var overdueLoans =
            _data
                .Traceability?.Loans.DistinctBy(item => item.LoanId)
                .Count(item =>
                    DashboardSummaryLogic.IsOverdueLoan(
                        item.StatusCode,
                        item.ExpectedReturnDateUtc,
                        item.ActualReturnDateUtc,
                        DateTime.Today
                    )
                )
            ?? 0;
        if (overdueLoans > 0)
            decisions.Add(
                new(
                    L("Préstamos vencidos", "Overdue loans"),
                    L(
                        $"{overdueLoans:N0} préstamos no registran devolución.",
                        $"{overdueLoans:N0} loans have no recorded return."
                    ),
                    L("Trazar", "Trace"),
                    DashboardModule.Traceability,
                    DecisionTone.Danger
                )
            );

        var pendingAmount =
            _data.Finance?.Sales.Where(item => item.Outstanding > 0).Sum(item => item.Outstanding)
            ?? 0;
        if (pendingAmount > 0)
            decisions.Add(
                new(
                    L("Cobros pendientes", "Pending collections"),
                    L(
                        $"Saldo total ARS {pendingAmount:N2}.",
                        $"Total balance ARS {pendingAmount:N2}."
                    ),
                    L("Ver saldos", "View balances"),
                    DashboardModule.Finance,
                    DecisionTone.Neutral
                )
            );

        var failedNotifications =
            _data.Logistics?.Notifications.Count(item => item.StatusCode is "ERROR" or "FALLIDA")
            ?? 0;
        if (failedNotifications > 0)
            decisions.Add(
                new(
                    L("Avisos sin enviar", "Unsent notifications"),
                    L(
                        $"{failedNotifications:N0} comunicaciones necesitan revisión.",
                        $"{failedNotifications:N0} messages need review."
                    ),
                    L("Revisar", "Review"),
                    DashboardModule.Logistics,
                    DecisionTone.Danger
                )
            );

        decisions.AddRange(BuildCommercialDecisions());

        return decisions.OrderByDescending(item => item.Tone).ThenBy(item => item.Title).ToList();
    }

    /// <summary>Construye una fila de decisión con destino operativo.</summary>
    /// <param name="decision">Asunto que se mostrará.</param>
    /// <returns>Fila con severidad, explicación y acción.</returns>
    private Control CreateDecisionRow(DecisionInfo decision)
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Height = 68,
            ColumnCount = 3,
            Margin = new Padding(0, 0, 0, 7),
            Padding = new Padding(0),
            BackColor = decision.Tone switch
            {
                DecisionTone.Danger => SoftRed,
                DecisionTone.Warning => SoftGold,
                _ => Color.FromArgb(248, 250, 249),
            },
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 7));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
        row.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        row.Controls.Add(
            new Panel
            {
                Dock = DockStyle.Fill,
                BackColor =
                    decision.Tone == DecisionTone.Danger ? Color.FromArgb(190, 35, 35)
                    : decision.Tone == DecisionTone.Warning ? BrandGold
                    : BrandGreenLight,
            },
            0,
            0
        );
        row.Controls.Add(CreateDecisionText(decision), 1, 0);
        var button = UiTheme.Button(
            decision.Action,
            0,
            0,
            94,
            decision.Tone == DecisionTone.Danger ? ButtonTone.Danger : ButtonTone.Neutral
        );
        button.Anchor = AnchorStyles.None;
        button.Click += (_, _) =>
        {
            if (_selectedModule == decision.Module && decision.Module != DashboardModule.Home)
                OpenModuleDetail(decision.Module, DefaultTarget(decision.Module));
            else
                ShowModuleOverview(decision.Module);
        };
        row.Controls.Add(button, 2, 0);
        return row;
    }

    /// <summary>Obtiene la pantalla operativa principal de un módulo.</summary>
    /// <param name="module">Módulo que requiere abrir su detalle.</param>
    /// <returns>Identificador interno de una pantalla existente.</returns>
    /// <exception cref="ArgumentOutOfRangeException">El módulo no posee una pantalla operativa.</exception>
    private static string DefaultTarget(DashboardModule module) =>
        module switch
        {
            DashboardModule.Commercial => "SALES",
            DashboardModule.Inventory => "INVENTORY",
            DashboardModule.Purchasing => "PURCHASING",
            DashboardModule.Traceability => "TRACEABILITY",
            DashboardModule.Logistics => "LOGISTICS",
            DashboardModule.Finance => "FINANCE",
            DashboardModule.Security => "USERS",
            DashboardModule.Configuration => "CONFIGURATION",
            _ => throw new ArgumentOutOfRangeException(
                nameof(module),
                module,
                "El módulo no posee pantalla operativa."
            ),
        };

    /// <summary>Construye los textos principal y secundario de una decisión.</summary>
    /// <param name="decision">Información visible de la decisión.</param>
    /// <returns>Panel textual adaptable.</returns>
    private static Control CreateDecisionText(DecisionInfo decision)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Padding = new Padding(12, 7, 6, 5),
        };
        panel.Controls.Add(
            new Label
            {
                Text = decision.Title,
                Dock = DockStyle.Top,
                Height = 24,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = UiTheme.Text,
                AutoEllipsis = true,
            }
        );
        panel.Controls.Add(
            new Label
            {
                Text = decision.Detail,
                Dock = DockStyle.Bottom,
                Height = 23,
                ForeColor = MutedText,
                AutoEllipsis = true,
            }
        );
        return panel;
    }

    /// <summary>Construye la actividad inmutable más reciente disponible.</summary>
    /// <param name="source">Actividad específica; si se omite se combinan todos los módulos.</param>
    /// <param name="title">Título opcional de la sección.</param>
    /// <param name="subtitle">Explicación opcional de la sección.</param>
    /// <param name="emptyText">Mensaje opcional cuando no existen eventos.</param>
    /// <returns>Tarjeta cronológica con eventos de distintos módulos.</returns>
    private Control BuildActivitySection(
        IReadOnlyList<ActivityInfo>? source = null,
        string? title = null,
        string? subtitle = null,
        string? emptyText = null
    )
    {
        var card = CreateCard();
        card.Margin = new Padding(8, 0, 0, 0);
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(4),
            BackColor = Color.White,
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(
            CreateSectionHeader(
                title ?? L("Actividad reciente", "Recent activity"),
                subtitle
                    ?? L(
                        "Últimos hechos confirmados, ordenados por fecha.",
                        "Latest confirmed events, ordered by date."
                    )
            ),
            0,
            0
        );

        var list = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            AutoScroll = true,
            BackColor = Color.White,
        };
        list.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var events = (source ?? BuildActivity()).Take(5).ToList();
        if (events.Count == 0)
        {
            list.RowCount = 1;
            list.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            list.Controls.Add(
                CreateEmptyState(
                    emptyText
                        ?? L(
                            "Todavía no hay actividad reciente disponible.",
                            "There is no recent activity available yet."
                        )
                )
            );
        }
        else
        {
            list.RowCount = events.Count + 1;
            for (var index = 0; index < events.Count; index++)
            {
                list.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
                list.Controls.Add(CreateActivityRow(events[index]), 0, index);
            }
            list.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        }
        layout.Controls.Add(list, 0, 1);
        card.Controls.Add(layout);
        return card;
    }

    /// <summary>Combina eventos recientes sin modificar sus fuentes originales.</summary>
    /// <returns>Actividad ordenada desde la más reciente.</returns>
    private IReadOnlyList<ActivityInfo> BuildActivity()
    {
        var activity = new List<ActivityInfo>();

        if (_data.Logistics is not null)
            activity.AddRange(
                _data.Logistics.Events.Select(item => new ActivityInfo(
                    ToLocal(item.DateUtc),
                    L("Logística", "Logistics"),
                    L(
                        $"{item.EventType}: {item.Observation}",
                        $"{item.EventType}: {item.Observation}"
                    )
                ))
            );

        if (_data.Traceability is not null)
            activity.AddRange(
                _data.Traceability.Events.Select(item => new ActivityInfo(
                    ToLocal(item.EventDateUtc),
                    L("Trazabilidad", "Traceability"),
                    L($"{item.Asset}: {item.EventType}", $"{item.Asset}: {item.EventType}")
                ))
            );

        if (_data.Finance is not null)
            activity.AddRange(
                _data.Finance.AccountMovements.Select(item => new ActivityInfo(
                    ToLocal(item.MovementDateUtc),
                    L("Finanzas", "Finance"),
                    item.Description
                ))
            );

        if (_data.Inventory is not null)
            activity.AddRange(
                _data
                    .Inventory.Movements.DistinctBy(item => item.MovementId)
                    .Select(item => new ActivityInfo(
                        ToLocal(item.MovementDateUtc),
                        L("Inventario", "Inventory"),
                        $"{item.MovementCode}: {item.MovementType}"
                    ))
            );

        if (_data.Sales is not null)
            activity.AddRange(
                _data
                    .Sales.Sales.DistinctBy(item => item.SaleId)
                    .Select(item => new ActivityInfo(
                        ToLocal(item.SaleDateUtc),
                        L("Comercial", "Commercial"),
                        L(
                            $"Venta {item.SaleCode} · {item.Client}",
                            $"Sale {item.SaleCode} · {item.Client}"
                        )
                    ))
            );

        return activity.OrderByDescending(item => item.When).ToList();
    }

    /// <summary>Construye una línea compacta de actividad.</summary>
    /// <param name="activity">Evento que se mostrará.</param>
    /// <returns>Fila cronológica legible.</returns>
    private static Control CreateActivityRow(ActivityInfo activity)
    {
        var row = new Panel
        {
            Dock = DockStyle.Fill,
            Height = 58,
            BackColor = Color.White,
            Margin = new Padding(0, 0, 0, 4),
        };
        row.Controls.Add(
            new TimelineDot
            {
                Left = 4,
                Top = 12,
                Size = new Size(20, 20),
            }
        );
        row.Controls.Add(
            new Label
            {
                Text = activity.Module,
                Left = 34,
                Top = 5,
                Width = 105,
                Height = 21,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = BrandGreen,
            }
        );
        row.Controls.Add(
            new Label
            {
                Text = DashboardSummaryLogic.FormatRelativeTime(activity.When, DateTime.Now),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                TextAlign = ContentAlignment.TopRight,
                Left = 300,
                Top = 5,
                Width = 130,
                Height = 21,
                ForeColor = MutedText,
            }
        );
        var description = new Label
        {
            Text = activity.Description,
            Left = 34,
            Top = 28,
            Width = 392,
            Height = 24,
            ForeColor = UiTheme.Text,
            AutoEllipsis = true,
        };
        row.Controls.Add(description);
        row.Resize += (_, _) =>
        {
            description.Width = Math.Max(100, row.ClientSize.Width - 42);
            if (row.Controls[2] is Label time)
                time.Left = Math.Max(150, row.ClientSize.Width - time.Width - 4);
        };
        return row;
    }

    /// <summary>Presenta primero el resumen del módulo elegido antes de entrar a sus operaciones.</summary>
    /// <param name="module">Módulo autorizado que se mostrará.</param>
    private void ShowModuleOverview(DashboardModule module)
    {
        if (!_navigation.TryGetValue(module, out var button) || !button.Enabled)
            return;
        if (module == DashboardModule.Home)
        {
            ShowHome();
            return;
        }

        CloseEmbeddedForm();
        SelectNavigation(module);
        var presentation = GetModulePresentation(module);
        _pageTitle.Text = presentation.Title;
        _contentHost.Controls.Clear();
        _contentHost.Controls.Add(
            module switch
            {
                DashboardModule.Commercial => BuildCommercialView(presentation),
                DashboardModule.Inventory => BuildInventoryView(presentation),
                DashboardModule.Purchasing => BuildPurchasingView(presentation),
                DashboardModule.Traceability => BuildTraceabilityView(presentation),
                DashboardModule.Logistics => BuildLogisticsView(presentation),
                DashboardModule.Finance => BuildFinanceView(presentation),
                DashboardModule.Security => BuildSecurityView(presentation),
                DashboardModule.Configuration => BuildConfigurationView(presentation),
                _ => BuildModuleView(module, presentation),
            }
        );
    }

    /// <summary>Construye el tablero propio de Comercial.</summary>
    /// <param name="presentation">Textos y accesos autorizados del módulo.</param>
    /// <returns>Vista con indicadores, circuito, prioridades y actividad comercial.</returns>
    private Control BuildCommercialView(ModulePresentation presentation)
    {
        var sales = _data.Sales;
        var today = DateOnly.FromDateTime(DateTime.Today);
        return BuildFocusedModuleView(
            DashboardModule.Commercial,
            presentation,
            L("Circuito comercial", "Commercial flow"),
            [
                new(
                    L("Clientes", "Customers"),
                    (sales?.Clients.Count ?? 0).ToString("N0"),
                    L("disponibles", "available"),
                    "CLIENTS"
                ),
                new(
                    L("Precios", "Prices"),
                    (
                        sales?.PriceLists.Count(item =>
                            DashboardSummaryLogic.IsEffective(
                                item.StatusCode,
                                item.ValidFrom,
                                item.ValidUntil,
                                today
                            )
                        )
                        ?? 0
                    ).ToString("N0"),
                    L("listas vigentes", "active lists"),
                    "SALES"
                ),
                new(
                    L("Borradores", "Drafts"),
                    CountOrders("BORRADOR").ToString("N0"),
                    L("sin confirmar", "not confirmed"),
                    "SALES"
                ),
                new(
                    L("Por vender", "Ready for sale"),
                    CountOrders("CONFIRMADO").ToString("N0"),
                    L("confirmados", "confirmed"),
                    "SALES"
                ),
                new(
                    L("Ventas hoy", "Sales today"),
                    CountSalesToday().ToString("N0"),
                    L("comprobantes", "transactions"),
                    "SALES"
                ),
            ],
            BuildCommercialDecisions(),
            BuildCommercialActivity(),
            L("Prioridades comerciales", "Commercial priorities"),
            L("Pedidos y precios que necesitan una acción.", "Orders and prices requiring action."),
            L("Actividad comercial", "Commercial activity"),
            L("Últimos pedidos y ventas registrados.", "Latest registered orders and sales."),
            L("No hay pendientes comerciales críticos.", "There are no critical commercial items."),
            L(
                "Todavía no hay pedidos ni ventas para mostrar.",
                "There are no orders or sales to show yet."
            )
        );
    }

    /// <summary>Construye el tablero propio de Inventario sin sumar cantidades de unidades diferentes.</summary>
    /// <param name="presentation">Textos y accesos autorizados del módulo.</param>
    /// <returns>Vista con estado del catálogo, existencias, alertas y movimientos.</returns>
    private Control BuildInventoryView(ModulePresentation presentation)
    {
        var inventory = _data.Inventory;
        return BuildFocusedModuleView(
            DashboardModule.Inventory,
            presentation,
            L("Flujo del inventario", "Inventory flow"),
            [
                new(
                    L("Productos", "Products"),
                    (inventory?.Products.Count(item => item.StatusCode == "ACTIVO") ?? 0).ToString(
                        "N0"
                    ),
                    L("activos", "active"),
                    "INVENTORY"
                ),
                new(
                    L("Stock físico", "Physical stock"),
                    (inventory?.Stock.Count(item => item.Quantity > 0) ?? 0).ToString("N0"),
                    L("posiciones", "positions"),
                    "INVENTORY"
                ),
                new(
                    L("Reservado", "Reserved"),
                    (inventory?.Stock.Count(item => item.ReservedQuantity > 0) ?? 0).ToString("N0"),
                    L("posiciones", "positions"),
                    "INVENTORY"
                ),
                new(
                    L("Disponible", "Available"),
                    (inventory?.Stock.Count(item => item.AvailableQuantity > 0) ?? 0).ToString(
                        "N0"
                    ),
                    L("posiciones", "positions"),
                    "INVENTORY"
                ),
                new(
                    L("Movimientos hoy", "Movements today"),
                    CountInventoryMovementsToday().ToString("N0"),
                    L("confirmados", "confirmed"),
                    "INVENTORY"
                ),
            ],
            BuildInventoryDecisions(),
            BuildInventoryActivity(),
            L("Prioridades de inventario", "Inventory priorities"),
            L(
                "Existencias y maestros que necesitan revisión.",
                "Balances and master data requiring review."
            ),
            L("Actividad de inventario", "Inventory activity"),
            L(
                "Últimos movimientos confirmados sin duplicar renglones.",
                "Latest confirmed movements without duplicate lines."
            ),
            L(
                "No hay alertas de inventario con los datos actuales.",
                "There are no inventory alerts in current data."
            ),
            L("Todavía no hay movimientos para mostrar.", "There are no movements to show yet.")
        );
    }

    /// <summary>Construye el tablero propio del circuito de Compras.</summary>
    /// <param name="presentation">Textos y accesos autorizados del módulo.</param>
    /// <returns>Vista con órdenes, recepciones, pendientes y actividad documental.</returns>
    private Control BuildPurchasingView(ModulePresentation presentation)
    {
        var purchasing = _data.Purchasing;
        return BuildFocusedModuleView(
            DashboardModule.Purchasing,
            presentation,
            L("Circuito de abastecimiento", "Purchasing flow"),
            [
                new(
                    L("Proveedores", "Suppliers"),
                    (
                        purchasing?.Suppliers.Count(item => item.StatusCode == "ACTIVO") ?? 0
                    ).ToString("N0"),
                    L("activos", "active"),
                    "PURCHASING"
                ),
                new(
                    L("Borradores", "Drafts"),
                    CountPurchaseOrders("BORRADOR").ToString("N0"),
                    L("editables", "editable"),
                    "PURCHASING"
                ),
                new(
                    L("Por aprobar", "Awaiting approval"),
                    CountPurchaseOrders("PENDIENTE_APROBACION").ToString("N0"),
                    L("órdenes", "orders"),
                    "PURCHASING"
                ),
                new(
                    L("Por recibir", "Awaiting receipt"),
                    CountReceivablePurchaseOrders().ToString("N0"),
                    L("órdenes", "orders"),
                    "PURCHASING"
                ),
                new(
                    L("Recepciones hoy", "Receipts today"),
                    CountPurchasingReceiptsToday().ToString("N0"),
                    L("confirmadas", "confirmed"),
                    "PURCHASING"
                ),
            ],
            BuildPurchasingDecisions(),
            BuildPurchasingActivity(),
            L("Prioridades de compras", "Purchasing priorities"),
            L(
                "Órdenes y recepciones que necesitan intervención.",
                "Orders and receipts requiring action."
            ),
            L("Actividad de compras", "Purchasing activity"),
            L(
                "Últimas órdenes y recepciones sin repetir renglones.",
                "Latest orders and receipts without duplicate lines."
            ),
            L("No hay pendientes críticos en Compras.", "There are no critical purchasing items."),
            L(
                "Todavía no hay órdenes ni recepciones para mostrar.",
                "There are no orders or receipts to show yet."
            )
        );
    }

    /// <summary>Construye el tablero propio de la trazabilidad industrial.</summary>
    /// <param name="presentation">Textos y acceso autorizado del módulo.</param>
    /// <returns>Vista con activos, contenido, custodia, prioridades e historial inmutable.</returns>
    private Control BuildTraceabilityView(ModulePresentation presentation)
    {
        var traceability = _data.Traceability;
        return BuildFocusedModuleView(
            DashboardModule.Traceability,
            presentation,
            L("Circuito de activos y contenido", "Asset and content flow"),
            [
                new(
                    L("Lotes", "Lots"),
                    (traceability?.Lots.Count(item => item.Quantity > 0) ?? 0).ToString("N0"),
                    L("con saldo", "with balance"),
                    "TRACEABILITY"
                ),
                new(
                    L("Activos", "Assets"),
                    (
                        traceability?.Assets.Count(item => item.StatusCode == "DISPONIBLE") ?? 0
                    ).ToString("N0"),
                    L("disponibles", "available"),
                    "TRACEABILITY"
                ),
                new(
                    L("Fraccionamientos", "Transformations"),
                    CountTransformationsToday().ToString("N0"),
                    L("confirmados hoy", "confirmed today"),
                    "TRACEABILITY"
                ),
                new(
                    L("Préstamos", "Loans"),
                    CountOpenLoans().ToString("N0"),
                    L("en custodia externa", "in external custody"),
                    "TRACEABILITY"
                ),
                new(
                    L("Historial", "History"),
                    CountTraceabilityEventsToday().ToString("N0"),
                    L("eventos hoy", "events today"),
                    "TRACEABILITY"
                ),
            ],
            BuildTraceabilityDecisions(),
            BuildTraceabilityActivity(),
            L("Prioridades de trazabilidad", "Traceability priorities"),
            L(
                "Activos, mantenimientos y préstamos que necesitan intervención.",
                "Assets, maintenance and loans requiring action."
            ),
            L("Historial inmutable reciente", "Recent immutable history"),
            L(
                "Últimos cambios confirmados con su estado y cantidad.",
                "Latest confirmed changes with their status and quantity."
            ),
            L(
                "No hay alertas críticas de trazabilidad.",
                "There are no critical traceability alerts."
            ),
            L(
                "Todavía no hay eventos trazables para mostrar.",
                "There are no traceability events to show yet."
            )
        );
    }

    /// <summary>Construye el tablero operativo de Logística con agenda, recorridos y avisos.</summary>
    /// <param name="presentation">Textos y acceso autorizado al módulo.</param>
    /// <returns>Vista con circuito, prioridades y eventos logísticos.</returns>
    private Control BuildLogisticsView(ModulePresentation presentation)
    {
        var logistics = _data.Logistics;
        return BuildFocusedModuleView(
            DashboardModule.Logistics,
            presentation,
            L("Circuito de despacho y entrega", "Dispatch and delivery flow"),
            [
                new(
                    L("Pedidos", "Orders"),
                    (logistics?.OrderCandidates.Count ?? 0).ToString("N0"),
                    L("para planificar", "to schedule"),
                    "LOGISTICS"
                ),
                new(
                    L("Solicitudes", "Requests"),
                    (
                        logistics?.Requests.Count(item => item.StatusCode == "PENDIENTE") ?? 0
                    ).ToString("N0"),
                    L("pendientes", "pending"),
                    "LOGISTICS"
                ),
                new(
                    L("Planificadas", "Planned"),
                    (
                        logistics?.Routes.Count(item => item.StatusCode == "PLANIFICADA") ?? 0
                    ).ToString("N0"),
                    L("hojas", "routes"),
                    "LOGISTICS"
                ),
                new(
                    L("En recorrido", "On route"),
                    (
                        logistics?.Routes.Count(item =>
                            item.StatusCode is "DESPACHADA" or "PAUSADA"
                        ) ?? 0
                    ).ToString("N0"),
                    L("hojas activas", "active routes"),
                    "LOGISTICS"
                ),
                new(
                    L("Entregadas hoy", "Delivered today"),
                    CountCompletedStopsToday().ToString("N0"),
                    L("paradas", "stops"),
                    "LOGISTICS"
                ),
            ],
            BuildLogisticsDecisions(),
            BuildLogisticsActivity(),
            L("Prioridades logísticas", "Logistics priorities"),
            L(
                "Agenda, documentación y avisos que necesitan intervención.",
                "Schedule, documents and notices requiring action."
            ),
            L("Actividad logística", "Logistics activity"),
            L(
                "Últimos eventos confirmados de solicitudes, hojas y paradas.",
                "Latest confirmed request, route and stop events."
            ),
            L("No hay alertas logísticas críticas.", "There are no critical logistics alerts."),
            L(
                "Todavía no hay eventos logísticos para mostrar.",
                "There are no logistics events to show yet."
            )
        );
    }

    /// <summary>Construye el tablero financiero interno sin presentarlo como facturación fiscal.</summary>
    /// <param name="presentation">Textos y accesos autorizados.</param>
    /// <returns>Vista de caja, saldos, cobros y cuenta corriente.</returns>
    private Control BuildFinanceView(ModulePresentation presentation)
    {
        var finance = _data.Finance;
        return BuildFocusedModuleView(
            DashboardModule.Finance,
            presentation,
            L("Circuito de caja y cobro interno", "Internal cash and collection flow"),
            [
                new(
                    L("Clientes", "Customers"),
                    (finance?.Clients.Count ?? 0).ToString("N0"),
                    L("con cuenta", "with account"),
                    "FINANCE"
                ),
                new(
                    L("Ventas", "Sales"),
                    (finance?.Sales.Count(item => item.Outstanding > 0) ?? 0).ToString("N0"),
                    L("con saldo", "with balance"),
                    "FINANCE"
                ),
                new(
                    L("Cajas", "Cash boxes"),
                    (
                        finance?.CashSessions.Count(item => item.StatusCode == "ABIERTA") ?? 0
                    ).ToString("N0"),
                    L("abiertas", "open"),
                    "FINANCE"
                ),
                new(
                    L("Cobros hoy", "Payments today"),
                    CountPaymentsToday().ToString("N0"),
                    L("confirmados", "confirmed"),
                    "FINANCE"
                ),
                new(
                    L("Saldo", "Balance"),
                    (finance?.Sales.Sum(item => item.Outstanding) ?? 0).ToString("N2"),
                    L("ARS pendiente", "ARS outstanding"),
                    "FINANCE"
                ),
            ],
            BuildFinanceDecisions(),
            BuildFinanceActivity(),
            L("Prioridades financieras", "Finance priorities"),
            L(
                "Cajas, cobros y saldos que conviene revisar.",
                "Cash, collections and balances to review."
            ),
            L("Movimientos de cuenta", "Account movements"),
            L(
                "Últimos débitos, créditos y compensaciones confirmados.",
                "Latest confirmed debits, credits and compensations."
            ),
            L("No hay alertas financieras críticas.", "There are no critical finance alerts."),
            L(
                "Todavía no hay movimientos de cuenta para mostrar.",
                "There are no account movements to show yet."
            )
        );
    }

    /// <summary>Construye el tablero de Seguridad con una lectura simple de accesos vigentes.</summary>
    /// <param name="presentation">Textos y acceso administrativo.</param>
    /// <returns>Vista de usuarios, roles y contexto de sesión.</returns>
    private Control BuildSecurityView(ModulePresentation presentation)
    {
        var users = _data.Users ?? [];
        return BuildFocusedModuleView(
            DashboardModule.Security,
            presentation,
            L("Circuito de acceso", "Access flow"),
            [
                new(
                    L("Usuarios", "Users"),
                    users.Count.ToString("N0"),
                    L("registrados", "registered"),
                    "USERS"
                ),
                new(
                    L("Activos", "Active"),
                    users.Count(item => item.StatusCode == "ACTIVO").ToString("N0"),
                    L("habilitados", "enabled"),
                    "USERS"
                ),
                new(
                    L("Roles", "Roles"),
                    users.SelectMany(item => item.Roles).Distinct().Count().ToString("N0"),
                    L("en uso", "in use"),
                    "USERS"
                ),
                new(
                    L("Permisos", "Permissions"),
                    _session.Permissions.Count.ToString("N0"),
                    L("en tu sesión", "in your session"),
                    "USERS"
                ),
                new(
                    L("Vencimiento", "Expiration"),
                    _session.ExpiresAtUtc.ToLocalTime().ToString("HH:mm"),
                    L("hora local", "local time"),
                    "USERS"
                ),
            ],
            BuildSecurityDecisions(),
            [],
            L("Prioridades de acceso", "Access priorities"),
            L("Cuentas sin rol o fuera de servicio.", "Accounts without roles or out of service."),
            L("Auditoría de seguridad", "Security audit"),
            L(
                "El detalle de accesos y sesiones se consulta desde la administración.",
                "Access and session details are available in administration."
            ),
            L(
                "No hay alertas de usuarios con la información actual.",
                "There are no user alerts in current data."
            ),
            L(
                "Abrí Administrar usuarios para consultar o revocar accesos.",
                "Open Manage users to inspect or revoke access."
            )
        );
    }

    /// <summary>Construye el tablero de Configuración usando el contexto efectivo de la sesión.</summary>
    /// <param name="presentation">Textos y acceso autorizado.</param>
    /// <returns>Vista de empresa, sucursal, idioma y permisos configurables.</returns>
    private Control BuildConfigurationView(ModulePresentation presentation)
    {
        var languages = Localization.AvailableLanguages();
        return BuildFocusedModuleView(
            DashboardModule.Configuration,
            presentation,
            L("Circuito de configuración", "Configuration flow"),
            [
                new(
                    L("Empresa", "Company"),
                    _workplaceName,
                    L("contexto activo", "active context"),
                    "CONFIGURATION"
                ),
                new(
                    L("Sucursal", "Branch"),
                    _session.BranchCode ?? "—",
                    L("seleccionada", "selected"),
                    "CONFIGURATION"
                ),
                new(
                    L("Idioma", "Language"),
                    CultureInfo.CurrentUICulture.Name,
                    L("preferencia", "preference"),
                    "CONFIGURATION"
                ),
                new(
                    L("Idiomas", "Languages"),
                    languages.Count.ToString("N0"),
                    L("disponibles", "available"),
                    "CONFIGURATION"
                ),
                new(
                    L("Permisos", "Permissions"),
                    _session
                        .Permissions.Count(item =>
                            item.StartsWith("CONFIGURACION.", StringComparison.OrdinalIgnoreCase)
                        )
                        .ToString("N0"),
                    L("habilitados", "enabled"),
                    "CONFIGURATION"
                ),
            ],
            BuildConfigurationDecisions(languages),
            [],
            L("Estado de configuración", "Configuration status"),
            L(
                "Contexto y capacidades disponibles para esta sesión.",
                "Context and capabilities available to this session."
            ),
            L("Cambios administrados", "Managed changes"),
            L(
                "Las modificaciones quedan auditadas en la empresa activa.",
                "Changes remain audited in the active company."
            ),
            L(
                "La configuración disponible no presenta advertencias.",
                "Available configuration has no warnings."
            ),
            L(
                "Abrí Configuración para consultar estructura, catálogos, parámetros e idiomas.",
                "Open Configuration to inspect structure, catalogs, parameters and languages."
            )
        );
    }

    /// <summary>Detecta agenda urgente, ofertas, documentación y avisos logísticos pendientes.</summary>
    /// <returns>Prioridades ordenadas por severidad y descripción.</returns>
    private IReadOnlyList<DecisionInfo> BuildLogisticsDecisions()
    {
        if (_data.Logistics is not { } logistics)
            return [];
        var decisions = new List<DecisionInfo>();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var limit = today.AddDays(30);

        var urgent = logistics.Requests.Count(item =>
            item.StatusCode == "PENDIENTE" && item.Priority is "ALTA" or "EMERGENCIA"
        );
        if (urgent > 0)
            decisions.Add(
                new(
                    L("Solicitudes urgentes sin planificar", "Urgent unscheduled requests"),
                    L(
                        $"{urgent:N0} solicitudes necesitan una hoja de ruta.",
                        $"{urgent:N0} requests need a route sheet."
                    ),
                    L("Planificar", "Schedule"),
                    DashboardModule.Logistics,
                    DecisionTone.Danger
                )
            );

        var offers = logistics.Routes.Count(item =>
            item.AssignmentType == "OFERTA"
            && item.TransporterId is null
            && item.StatusCode == "PLANIFICADA"
        );
        if (offers > 0)
            decisions.Add(
                new(
                    L("Hojas ofrecidas sin transportista", "Offered routes without a driver"),
                    L(
                        $"{offers:N0} recorridos todavía no fueron tomados ni asignados.",
                        $"{offers:N0} routes have not been claimed or assigned."
                    ),
                    L("Asignar", "Assign"),
                    DashboardModule.Logistics,
                    DecisionTone.Warning
                )
            );

        var driverDocuments = logistics.Transporters.Count(item =>
            item.StatusCode == "ACTIVO"
            && item.LicenseExpiration is not null
            && item.LicenseExpiration <= limit
        );
        var vehicleDocuments = logistics.Vehicles.Count(item =>
            item.StatusCode == "ACTIVO"
            && (
                item.InsuranceExpiration is not null && item.InsuranceExpiration <= limit
                || item.InspectionExpiration is not null && item.InspectionExpiration <= limit
            )
        );
        if (driverDocuments + vehicleDocuments > 0)
            decisions.Add(
                new(
                    L("Documentación próxima a vencer", "Documents nearing expiration"),
                    L(
                        $"{driverDocuments:N0} licencias · {vehicleDocuments:N0} vehículos dentro de 30 días.",
                        $"{driverDocuments:N0} licenses · {vehicleDocuments:N0} vehicles within 30 days."
                    ),
                    L("Revisar", "Review"),
                    DashboardModule.Logistics,
                    logistics.Transporters.Any(item => item.LicenseExpiration < today)
                    || logistics.Vehicles.Any(item =>
                        item.InsuranceExpiration < today || item.InspectionExpiration < today
                    )
                        ? DecisionTone.Danger
                        : DecisionTone.Warning
                )
            );

        var failedNotices = logistics.Notifications.Count(item =>
            item.StatusCode is "ERROR" or "FALLIDA"
        );
        if (failedNotices > 0)
            decisions.Add(
                new(
                    L("Avisos con error", "Failed notices"),
                    L(
                        $"{failedNotices:N0} notificaciones necesitan revisión.",
                        $"{failedNotices:N0} notifications need review."
                    ),
                    L("Revisar", "Review"),
                    DashboardModule.Logistics,
                    DecisionTone.Warning
                )
            );

        return decisions.OrderByDescending(item => item.Tone).ThenBy(item => item.Title).ToList();
    }

    /// <summary>Convierte eventos logísticos inmutables en una actividad legible.</summary>
    /// <returns>Eventos más recientes, sin duplicados.</returns>
    private IReadOnlyList<ActivityInfo> BuildLogisticsActivity() =>
        _data
            .Logistics?.Events.DistinctBy(item => item.EventId)
            .Select(item => new ActivityInfo(
                ToLocal(item.DateUtc),
                item.EventType.Replace('_', ' '),
                item.Observation
            ))
            .OrderByDescending(item => item.When)
            .ToList()
        ?? [];

    /// <summary>Cuenta paradas completadas durante el día local.</summary>
    /// <returns>Cantidad de paradas únicas con salida registrada hoy.</returns>
    private int CountCompletedStopsToday() =>
        _data.Logistics?.Stops.Count(item =>
            DashboardSummaryLogic.IsCompleted(item.StatusCode)
            && item.DepartureUtc is not null
            && ToLocal(item.DepartureUtc.Value).Date == DateTime.Today
        )
        ?? 0;

    /// <summary>Detecta cajas, saldos, reversas y cobros sin aplicación completa.</summary>
    /// <returns>Prioridades financieras ordenadas.</returns>
    private IReadOnlyList<DecisionInfo> BuildFinanceDecisions()
    {
        if (_data.Finance is not { } finance)
            return [];
        var decisions = new List<DecisionInfo>();
        if (
            finance.CashBoxes.Any(item => item.StatusCode == "ACTIVO")
            && !finance.CashSessions.Any(item => item.StatusCode == "ABIERTA")
        )
            decisions.Add(
                new(
                    L("No hay una caja abierta", "There is no open cash session"),
                    L(
                        "Abrí una caja antes de registrar cobros en efectivo.",
                        "Open a cash session before recording cash payments."
                    ),
                    L("Abrir caja", "Open cash"),
                    DashboardModule.Finance,
                    DecisionTone.Warning
                )
            );

        var debtors = finance.Clients.Count(item => item.Balance > 0);
        if (debtors > 0)
            decisions.Add(
                new(
                    L("Clientes con saldo pendiente", "Customers with outstanding balance"),
                    L(
                        $"{debtors:N0} cuentas conservan deuda interna.",
                        $"{debtors:N0} accounts retain internal debt."
                    ),
                    L("Ver saldos", "View balances"),
                    DashboardModule.Finance,
                    DecisionTone.Neutral
                )
            );

        var appliedByPayment = finance
            .Applications.Where(item => item.StatusCode == "ACTIVO")
            .GroupBy(item => item.PaymentId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Amount));
        var unapplied = finance.Payments.Count(item =>
            item.StatusCode == "CONFIRMADO"
            && appliedByPayment.GetValueOrDefault(item.PaymentId) < item.Total
        );
        if (unapplied > 0)
            decisions.Add(
                new(
                    L("Cobros con importe a favor", "Payments with unapplied amount"),
                    L(
                        $"{unapplied:N0} cobros no están aplicados por completo a ventas.",
                        $"{unapplied:N0} payments are not fully applied to sales."
                    ),
                    L("Aplicar", "Apply"),
                    DashboardModule.Finance,
                    DecisionTone.Warning
                )
            );

        var reversedToday = finance.Payments.Count(item =>
            item.StatusCode == "REVERSADO"
            && item.ReversedUtc is not null
            && ToLocal(item.ReversedUtc.Value).Date == DateTime.Today
        );
        if (reversedToday > 0)
            decisions.Add(
                new(
                    L("Cobros reversados hoy", "Payments reversed today"),
                    L(
                        $"{reversedToday:N0} compensaciones requieren control documental.",
                        $"{reversedToday:N0} compensations require document review."
                    ),
                    L("Revisar", "Review"),
                    DashboardModule.Finance,
                    DecisionTone.Neutral
                )
            );

        return decisions.OrderByDescending(item => item.Tone).ThenBy(item => item.Title).ToList();
    }

    /// <summary>Presenta los últimos movimientos inmutables de cuenta corriente.</summary>
    /// <returns>Movimientos ordenados desde el más reciente.</returns>
    private IReadOnlyList<ActivityInfo> BuildFinanceActivity() =>
        _data
            .Finance?.AccountMovements.DistinctBy(item => item.AccountMovementId)
            .Select(item => new ActivityInfo(
                ToLocal(item.MovementDateUtc),
                item.MovementType.Replace('_', ' '),
                $"{item.Description} · {item.Currency} {item.Amount:N2}"
            ))
            .OrderByDescending(item => item.When)
            .ToList()
        ?? [];

    /// <summary>Cuenta cobros confirmados durante el día local.</summary>
    /// <returns>Cantidad de comprobantes internos de cobro.</returns>
    private int CountPaymentsToday() =>
        _data.Finance?.Payments.Count(item =>
            item.StatusCode == "CONFIRMADO" && ToLocal(item.PaymentDateUtc).Date == DateTime.Today
        )
        ?? 0;

    /// <summary>Detecta usuarios activos sin rol y cuentas inactivas.</summary>
    /// <returns>Prioridades administrativas de seguridad.</returns>
    private IReadOnlyList<DecisionInfo> BuildSecurityDecisions()
    {
        var users = _data.Users ?? [];
        var decisions = new List<DecisionInfo>();
        var withoutRoles = users.Count(item =>
            item.StatusCode == "ACTIVO" && item.Roles.Count == 0
        );
        if (withoutRoles > 0)
            decisions.Add(
                new(
                    L("Usuarios activos sin rol", "Active users without a role"),
                    L(
                        $"{withoutRoles:N0} cuentas no poseen permisos funcionales.",
                        $"{withoutRoles:N0} accounts have no functional permissions."
                    ),
                    L("Asignar", "Assign"),
                    DashboardModule.Security,
                    DecisionTone.Danger
                )
            );

        var inactive = users.Count(item => item.StatusCode != "ACTIVO");
        if (inactive > 0)
            decisions.Add(
                new(
                    L("Cuentas fuera de servicio", "Disabled accounts"),
                    L(
                        $"{inactive:N0} usuarios permanecen inactivos para preservar su historial.",
                        $"{inactive:N0} users remain disabled to preserve their history."
                    ),
                    L("Revisar", "Review"),
                    DashboardModule.Security,
                    DecisionTone.Neutral
                )
            );

        return decisions;
    }

    /// <summary>Explica limitaciones del contexto de configuración actual.</summary>
    /// <param name="languages">Idiomas válidos instalados en este equipo.</param>
    /// <returns>Advertencias de contexto sin inventar actividad histórica.</returns>
    private IReadOnlyList<DecisionInfo> BuildConfigurationDecisions(
        IReadOnlyList<LanguageOption> languages
    )
    {
        var decisions = new List<DecisionInfo>();
        if (_session.BranchId is null)
            decisions.Add(
                new(
                    L("Sesión sin sucursal seleccionada", "Session without a selected branch"),
                    L(
                        "Algunas operaciones requieren elegir una sucursal al ingresar.",
                        "Some operations require selecting a branch at sign-in."
                    ),
                    L("Cambiar acceso", "Change access"),
                    DashboardModule.Configuration,
                    DecisionTone.Warning
                )
            );
        if (!Can("CONFIGURACION.GESTIONAR"))
            decisions.Add(
                new(
                    L("Configuración en modo consulta", "Read-only configuration"),
                    L(
                        "Tu sesión puede ver datos, pero no modificarlos.",
                        "Your session can view data but cannot modify it."
                    ),
                    L("Consultar", "View"),
                    DashboardModule.Configuration,
                    DecisionTone.Neutral
                )
            );
        if (languages.Count == 2)
            decisions.Add(
                new(
                    L("Idiomas incluidos", "Built-in languages"),
                    L(
                        "Podés importar un paquete JSON validado desde Configuración.",
                        "You can import a validated JSON package from Configuration."
                    ),
                    L("Abrir", "Open"),
                    DashboardModule.Configuration,
                    DecisionTone.Neutral
                )
            );
        return decisions;
    }

    /// <summary>Compone un tablero especializado reutilizable por los módulos ya desarrollados.</summary>
    /// <param name="module">Módulo al que pertenecen indicadores y destinos.</param>
    /// <param name="presentation">Textos y acciones autorizadas.</param>
    /// <param name="flowTitle">Título visible del circuito.</param>
    /// <param name="stages">Etapas ordenadas del circuito.</param>
    /// <param name="decisions">Pendientes propios del módulo.</param>
    /// <param name="activity">Eventos propios del módulo.</param>
    /// <param name="decisionTitle">Título de prioridades.</param>
    /// <param name="decisionSubtitle">Explicación de prioridades.</param>
    /// <param name="activityTitle">Título de actividad.</param>
    /// <param name="activitySubtitle">Explicación de actividad.</param>
    /// <param name="emptyDecision">Mensaje sin prioridades.</param>
    /// <param name="emptyActivity">Mensaje sin actividad.</param>
    /// <returns>Vista adaptable de cuatro filas.</returns>
    private Control BuildFocusedModuleView(
        DashboardModule module,
        ModulePresentation presentation,
        string flowTitle,
        IReadOnlyList<FlowStageInfo> stages,
        IReadOnlyList<DecisionInfo> decisions,
        IReadOnlyList<ActivityInfo> activity,
        string decisionTitle,
        string decisionSubtitle,
        string activityTitle,
        string activitySubtitle,
        string emptyDecision,
        string emptyActivity
    )
    {
        var scroll = CreateScrollablePage();
        var page = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 820,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = UiTheme.Background,
            Padding = new Padding(28, 22, 28, 28),
            Margin = Padding.Empty,
        };
        page.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 154));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 166));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        page.Controls.Add(BuildFocusedModuleHeading(module, presentation), 0, 0);
        page.Controls.Add(BuildModuleMetrics(module), 0, 1);
        page.Controls.Add(BuildModuleFlow(module, flowTitle, stages), 0, 2);

        var lower = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 10, 0, 0),
            BackColor = UiTheme.Background,
        };
        lower.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        lower.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        lower.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        lower.Controls.Add(
            BuildDecisionSection(decisions, decisionTitle, decisionSubtitle, emptyDecision),
            0,
            0
        );
        lower.Controls.Add(
            BuildActivitySection(activity, activityTitle, activitySubtitle, emptyActivity),
            1,
            0
        );
        page.Controls.Add(lower, 0, 3);

        scroll.Controls.Add(page);
        ResizePageWith(scroll, page);
        return scroll;
    }

    /// <summary>Presenta el propósito del módulo y sus accesos principales.</summary>
    /// <param name="module">Módulo usado por los destinos.</param>
    /// <param name="presentation">Descripción y acciones autorizadas.</param>
    /// <returns>Encabezado con ayuda y botones operativos.</returns>
    private Control BuildFocusedModuleHeading(
        DashboardModule module,
        ModulePresentation presentation
    )
    {
        var heading = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = UiTheme.Background,
            Margin = Padding.Empty,
        };
        heading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        heading.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 390));
        heading.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        heading.Controls.Add(BuildModuleHeading(presentation), 0, 0);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(8, 28, 0, 0),
            BackColor = UiTheme.Background,
        };
        foreach (var action in presentation.Actions)
        {
            var button = UiTheme.Button(
                action.Text,
                0,
                0,
                presentation.Actions.Count == 1 ? 230 : 176,
                action.Tone
            );
            button.Height = 44;
            button.Margin = new Padding(0, 0, 10, 0);
            button.Click += (_, _) => OpenModuleDetail(module, action.Target);
            actions.Controls.Add(button);
        }
        heading.Controls.Add(actions, 1, 0);
        return heading;
    }

    /// <summary>Construye un circuito de cinco etapas clickeables.</summary>
    /// <param name="module">Módulo dueño del circuito.</param>
    /// <param name="titleText">Título de la tarjeta.</param>
    /// <param name="stages">Etapas que se mostrarán en orden.</param>
    /// <returns>Tarjeta horizontal navegable.</returns>
    private Control BuildModuleFlow(
        DashboardModule module,
        string titleText,
        IReadOnlyList<FlowStageInfo> stages
    )
    {
        var card = CreateCard();
        card.Margin = new Padding(0, 0, 0, 8);
        var title = SectionTitle(titleText);
        title.Dock = DockStyle.Top;
        title.Height = 37;
        var flow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = (stages.Count * 2) - 1,
            RowCount = 1,
            BackColor = Color.White,
            Padding = new Padding(0, 5, 0, 0),
        };
        for (var index = 0; index < flow.ColumnCount; index++)
            flow.ColumnStyles.Add(
                index % 2 == 0
                    ? new ColumnStyle(SizeType.Percent, 100F / stages.Count)
                    : new ColumnStyle(SizeType.Absolute, 24)
            );
        flow.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        for (var index = 0; index < stages.Count; index++)
        {
            var stage = stages[index];
            var button = new FlowMetricButton(stage.Title, stage.Value, stage.Detail)
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(4, 2, 4, 2),
                AccessibleName = $"{stage.Title}: {stage.Value} {stage.Detail}",
            };
            button.Click += (_, _) => OpenModuleDetail(module, stage.Target);
            flow.Controls.Add(button, index * 2, 0);
            if (index < stages.Count - 1)
                flow.Controls.Add(
                    new Label
                    {
                        Dock = DockStyle.Fill,
                        Text = "›",
                        Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                        ForeColor = BrandGold,
                        TextAlign = ContentAlignment.MiddleCenter,
                    },
                    index * 2 + 1,
                    0
                );
        }
        card.Controls.Add(flow);
        card.Controls.Add(title);
        return card;
    }

    /// <summary>Calcula asuntos comerciales accionables.</summary>
    /// <returns>Pendientes ordenados por severidad.</returns>
    private IReadOnlyList<DecisionInfo> BuildCommercialDecisions()
    {
        if (_data.Sales is not { } sales)
            return [];

        var decisions = new List<DecisionInfo>();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var activeLists = sales
            .PriceLists.Where(item =>
                DashboardSummaryLogic.IsEffective(
                    item.StatusCode,
                    item.ValidFrom,
                    item.ValidUntil,
                    today
                )
            )
            .Select(item => item.PriceListId)
            .ToHashSet();
        if (activeLists.Count == 0)
            decisions.Add(
                new(
                    L("No hay una lista de precios vigente", "There is no active price list"),
                    L(
                        "Los pedidos nuevos necesitan una lista activa para calcular precios.",
                        "New orders need an active list to calculate prices."
                    ),
                    L("Configurar", "Configure"),
                    DashboardModule.Commercial,
                    DecisionTone.Danger
                )
            );
        else if (
            !sales.Prices.Any(item =>
                item.StatusCode == "ACTIVO" && activeLists.Contains(item.PriceListId)
            )
        )
            decisions.Add(
                new(
                    L(
                        "Las listas vigentes no tienen precios activos",
                        "Active lists have no active prices"
                    ),
                    L(
                        "Cargá al menos un producto antes de registrar pedidos.",
                        "Add at least one product before registering orders."
                    ),
                    L("Cargar precios", "Add prices"),
                    DashboardModule.Commercial,
                    DecisionTone.Danger
                )
            );

        var drafts = CountOrders("BORRADOR");
        if (drafts > 0)
            decisions.Add(
                new(
                    L("Pedidos pendientes de confirmación", "Orders awaiting confirmation"),
                    L(
                        $"{drafts:N0} pedidos todavía son borradores y no reservan stock.",
                        $"{drafts:N0} orders are still drafts and do not reserve stock."
                    ),
                    L("Ver borradores", "View drafts"),
                    DashboardModule.Commercial,
                    DecisionTone.Warning
                )
            );

        var confirmed = CountOrders("CONFIRMADO");
        if (confirmed > 0)
            decisions.Add(
                new(
                    L("Pedidos listos para generar venta", "Orders ready for sale"),
                    L(
                        $"{confirmed:N0} pedidos están confirmados y mantienen stock reservado.",
                        $"{confirmed:N0} orders are confirmed and reserve stock."
                    ),
                    L("Generar venta", "Create sale"),
                    DashboardModule.Commercial,
                    DecisionTone.Warning
                )
            );

        var expiringPromotions = sales.Promotions.Count(item =>
            DashboardSummaryLogic.IsEffective(
                item.StatusCode,
                item.ValidFrom,
                item.ValidUntil,
                today
            )
            && item.ValidUntil is { } end
            && end <= today.AddDays(7)
        );
        if (expiringPromotions > 0)
            decisions.Add(
                new(
                    L("Promociones próximas a vencer", "Promotions expiring soon"),
                    L(
                        $"{expiringPromotions:N0} promociones terminan durante los próximos siete días.",
                        $"{expiringPromotions:N0} promotions end within seven days."
                    ),
                    L("Revisar", "Review"),
                    DashboardModule.Commercial,
                    DecisionTone.Neutral
                )
            );

        return decisions.OrderByDescending(item => item.Tone).ThenBy(item => item.Title).ToList();
    }

    /// <summary>Combina los últimos pedidos y ventas sin duplicar renglones.</summary>
    /// <returns>Actividad comercial ordenada desde la más reciente.</returns>
    private IReadOnlyList<ActivityInfo> BuildCommercialActivity()
    {
        if (_data.Sales is not { } sales)
            return [];

        return sales
            .Orders.DistinctBy(item => item.OrderId)
            .Select(item => new ActivityInfo(
                ToLocal(item.OrderDateUtc),
                L("Pedido", "Order"),
                $"{item.OrderCode} · {item.Client} · {item.Currency} {item.Total:N2} · {item.StatusCode.Replace('_', ' ')}"
            ))
            .Concat(
                sales
                    .Sales.DistinctBy(item => item.SaleId)
                    .Select(item => new ActivityInfo(
                        ToLocal(item.SaleDateUtc),
                        L("Venta", "Sale"),
                        $"{item.SaleCode} · {item.Client} · {item.Currency} {item.Total:N2}"
                    ))
            )
            .OrderByDescending(item => item.When)
            .ToList();
    }

    /// <summary>Calcula alertas propias del catálogo y sus posiciones de stock.</summary>
    /// <returns>Pendientes de Inventario ordenados por severidad.</returns>
    private IReadOnlyList<DecisionInfo> BuildInventoryDecisions()
    {
        if (_data.Inventory is not { } inventory)
            return [];

        var decisions = new List<DecisionInfo>();
        var belowMinimum = inventory.Stock.Count(item => item.IsBelowMinimum);
        if (belowMinimum > 0)
            decisions.Add(
                new(
                    L("Stock por debajo del mínimo", "Stock below minimum"),
                    L(
                        $"{belowMinimum:N0} posiciones necesitan reposición.",
                        $"{belowMinimum:N0} positions need replenishment."
                    ),
                    L("Reponer", "Replenish"),
                    DashboardModule.Inventory,
                    DecisionTone.Danger
                )
            );

        var inconsistent = inventory.Stock.Count(item =>
            DashboardSummaryLogic.IsInvalidStockBalance(
                item.Quantity,
                item.ReservedQuantity,
                item.AvailableQuantity
            )
        );
        if (inconsistent > 0)
            decisions.Add(
                new(
                    L("Existencias para auditar", "Balances requiring audit"),
                    L(
                        $"{inconsistent:N0} posiciones presentan cantidades incompatibles.",
                        $"{inconsistent:N0} positions contain incompatible quantities."
                    ),
                    L("Auditar", "Audit"),
                    DashboardModule.Inventory,
                    DecisionTone.Danger
                )
            );

        var stockedProducts = inventory.Stock.Select(item => item.ProductId).ToHashSet();
        var productsWithoutPosition = inventory.Products.Count(item =>
            item.StatusCode == "ACTIVO"
            && item.ItemType != "SERVICIO"
            && !stockedProducts.Contains(item.ProductId)
        );
        if (productsWithoutPosition > 0)
            decisions.Add(
                new(
                    L("Productos todavía sin posición", "Products without a stock position"),
                    L(
                        $"{productsWithoutPosition:N0} productos activos aún no registran existencias.",
                        $"{productsWithoutPosition:N0} active products do not have a balance yet."
                    ),
                    L("Revisar", "Review"),
                    DashboardModule.Inventory,
                    DecisionTone.Warning
                )
            );

        return decisions.OrderByDescending(item => item.Tone).ThenBy(item => item.Title).ToList();
    }

    /// <summary>Resume los últimos movimientos de inventario por cabecera.</summary>
    /// <returns>Actividad ordenada sin duplicar los renglones de cada movimiento.</returns>
    private IReadOnlyList<ActivityInfo> BuildInventoryActivity()
    {
        if (_data.Inventory is not { } inventory)
            return [];

        return inventory
            .Movements.GroupBy(item => item.MovementId)
            .Select(group =>
            {
                var movement = group.First();
                return new ActivityInfo(
                    ToLocal(movement.MovementDateUtc),
                    L("Inventario", "Inventory"),
                    L(
                        $"{movement.MovementCode} · {InventoryMovementLabel(movement.MovementType)} · {group.Count():N0} renglones",
                        $"{movement.MovementCode} · {InventoryMovementLabel(movement.MovementType)} · {group.Count():N0} lines"
                    )
                );
            })
            .OrderByDescending(item => item.When)
            .ToList();
    }

    /// <summary>Cuenta movimientos únicos confirmados durante el día local.</summary>
    /// <returns>Cantidad de cabeceras de movimiento del día.</returns>
    private int CountInventoryMovementsToday() =>
        _data
            .Inventory?.Movements.Where(item =>
                ToLocal(item.MovementDateUtc).Date == DateTime.Today
            )
            .DistinctBy(item => item.MovementId)
            .Count()
        ?? 0;

    /// <summary>Cuenta pedidos únicos en un estado concreto.</summary>
    /// <param name="statusCode">Estado comercial persistido.</param>
    /// <returns>Cantidad de encabezados sin repetir renglones.</returns>
    private int CountOrders(string statusCode) =>
        _data
            .Sales?.Orders.DistinctBy(item => item.OrderId)
            .Count(item => item.StatusCode == statusCode)
        ?? 0;

    /// <summary>Cuenta ventas únicas registradas durante el día local.</summary>
    /// <returns>Cantidad de ventas del día sin repetir renglones.</returns>
    private int CountSalesToday() =>
        _data
            .Sales?.Sales.DistinctBy(item => item.SaleId)
            .Count(item => ToLocal(item.SaleDateUtc).Date == DateTime.Today)
        ?? 0;

    /// <summary>Traduce el tipo persistido de movimiento para su lectura operativa.</summary>
    /// <param name="movementType">Código del movimiento.</param>
    /// <returns>Nombre visible según el idioma activo.</returns>
    private string InventoryMovementLabel(string movementType) =>
        movementType switch
        {
            "ENTRADA" => L("Entrada", "Inbound"),
            "SALIDA" => L("Salida", "Outbound"),
            "TRANSFERENCIA" => L("Transferencia", "Transfer"),
            "AJUSTE_ENTRADA" => L("Ajuste de entrada", "Inbound adjustment"),
            "AJUSTE_SALIDA" => L("Ajuste de salida", "Outbound adjustment"),
            _ => movementType.Replace('_', ' '),
        };

    /// <summary>Calcula asuntos accionables del circuito de Compras.</summary>
    /// <returns>Pendientes ordenados por severidad.</returns>
    private IReadOnlyList<DecisionInfo> BuildPurchasingDecisions()
    {
        if (_data.Purchasing is not { } purchasing)
            return [];

        var decisions = new List<DecisionInfo>();
        if (!purchasing.Suppliers.Any(item => item.StatusCode == "ACTIVO"))
            decisions.Add(
                new(
                    L("No hay proveedores activos", "There are no active suppliers"),
                    L(
                        "Se necesita al menos uno para registrar una orden nueva.",
                        "At least one is required to create an order."
                    ),
                    L("Configurar", "Configure"),
                    DashboardModule.Purchasing,
                    DecisionTone.Danger
                )
            );

        var awaitingApproval = CountPurchaseOrders("PENDIENTE_APROBACION");
        if (awaitingApproval > 0)
            decisions.Add(
                new(
                    L("Órdenes esperando aprobación", "Orders awaiting approval"),
                    L(
                        $"{awaitingApproval:N0} órdenes todavía no pueden recibirse.",
                        $"{awaitingApproval:N0} orders cannot be received yet."
                    ),
                    L("Aprobar", "Approve"),
                    DashboardModule.Purchasing,
                    DecisionTone.Warning
                )
            );

        var today = DateOnly.FromDateTime(DateTime.Today);
        var overdue = purchasing
            .Orders.GroupBy(item => item.PurchaseOrderId)
            .Count(group =>
            {
                var order = group.First();
                return DashboardSummaryLogic.IsOverduePurchaseOrder(
                    order.StatusCode,
                    order.ExpectedDeliveryDate,
                    group.Any(line => line.PendingQuantity > 0),
                    today
                );
            });
        if (overdue > 0)
            decisions.Add(
                new(
                    L("Entregas de proveedor vencidas", "Overdue supplier deliveries"),
                    L(
                        $"{overdue:N0} órdenes conservan cantidades pendientes después de la fecha prevista.",
                        $"{overdue:N0} orders retain pending quantities after their expected date."
                    ),
                    L("Revisar", "Review"),
                    DashboardModule.Purchasing,
                    DecisionTone.Danger
                )
            );

        var partial = CountPurchaseOrders("RECIBIDA_PARCIAL");
        if (partial > 0)
            decisions.Add(
                new(
                    L("Recepciones parciales abiertas", "Open partial receipts"),
                    L(
                        $"{partial:N0} órdenes aún esperan mercadería o cierre de saldo.",
                        $"{partial:N0} orders still await goods or balance closure."
                    ),
                    L("Continuar", "Continue"),
                    DashboardModule.Purchasing,
                    DecisionTone.Warning
                )
            );

        var receiptsWithDifferences = purchasing
            .Receipts.Where(item =>
                item.StatusCode == "CONFIRMADA"
                && ToLocal(item.ReceiptDateUtc).Date == DateTime.Today
                && (item.RejectedQuantity > 0 || item.DamagedQuantity > 0)
            )
            .Select(item => item.GoodsReceiptId)
            .Distinct()
            .Count();
        if (receiptsWithDifferences > 0)
            decisions.Add(
                new(
                    L("Recepciones con diferencias hoy", "Receipts with differences today"),
                    L(
                        $"{receiptsWithDifferences:N0} recepciones informan material rechazado o dañado.",
                        $"{receiptsWithDifferences:N0} receipts report rejected or damaged goods."
                    ),
                    L("Ver detalle", "View details"),
                    DashboardModule.Purchasing,
                    DecisionTone.Neutral
                )
            );

        return decisions.OrderByDescending(item => item.Tone).ThenBy(item => item.Title).ToList();
    }

    /// <summary>Combina las últimas órdenes y recepciones por documento.</summary>
    /// <returns>Actividad de Compras ordenada desde la más reciente.</returns>
    private IReadOnlyList<ActivityInfo> BuildPurchasingActivity()
    {
        if (_data.Purchasing is not { } purchasing)
            return [];

        var supplierByOrder = purchasing
            .Orders.GroupBy(item => item.PurchaseOrderId)
            .ToDictionary(group => group.Key, group => group.First().Supplier);
        var orders = purchasing
            .Orders.GroupBy(item => item.PurchaseOrderId)
            .Select(group =>
            {
                var order = group.First();
                return new ActivityInfo(
                    ToLocal(order.OrderDateUtc),
                    L("Orden", "Order"),
                    $"{order.OrderCode} · {order.Supplier} · {order.Currency} {order.Total:N2} · {PurchaseStatusLabel(order.StatusCode)}"
                );
            });
        var receipts = purchasing
            .Receipts.GroupBy(item => item.GoodsReceiptId)
            .Select(group =>
            {
                var receipt = group.First();
                var supplier = supplierByOrder.GetValueOrDefault(
                    receipt.PurchaseOrderId,
                    L("Proveedor", "Supplier")
                );
                return new ActivityInfo(
                    ToLocal(receipt.ReceiptDateUtc),
                    L("Recepción", "Receipt"),
                    L(
                        $"{receipt.ReceiptCode} · {supplier} · {group.Count():N0} renglones · {PurchaseStatusLabel(receipt.StatusCode)}",
                        $"{receipt.ReceiptCode} · {supplier} · {group.Count():N0} lines · {PurchaseStatusLabel(receipt.StatusCode)}"
                    )
                );
            });
        return orders.Concat(receipts).OrderByDescending(item => item.When).ToList();
    }

    /// <summary>Cuenta órdenes únicas en un estado concreto.</summary>
    /// <param name="statusCode">Estado persistido de la orden.</param>
    /// <returns>Cantidad de cabeceras sin repetir renglones.</returns>
    private int CountPurchaseOrders(string statusCode) =>
        _data
            .Purchasing?.Orders.DistinctBy(item => item.PurchaseOrderId)
            .Count(item => item.StatusCode == statusCode)
        ?? 0;

    /// <summary>Cuenta órdenes aprobadas o parcialmente recibidas.</summary>
    /// <returns>Cantidad de órdenes habilitadas para recepción.</returns>
    private int CountReceivablePurchaseOrders() =>
        _data
            .Purchasing?.Orders.DistinctBy(item => item.PurchaseOrderId)
            .Count(item => DashboardSummaryLogic.IsReceivablePurchaseOrder(item.StatusCode))
        ?? 0;

    /// <summary>Cuenta recepciones confirmadas durante el día local.</summary>
    /// <returns>Cantidad de comprobantes de recepción sin repetir renglones.</returns>
    private int CountPurchasingReceiptsToday() =>
        _data
            .Purchasing?.Receipts.Where(item =>
                item.StatusCode == "CONFIRMADA"
                && ToLocal(item.ReceiptDateUtc).Date == DateTime.Today
            )
            .DistinctBy(item => item.GoodsReceiptId)
            .Count()
        ?? 0;

    /// <summary>Traduce estados de órdenes y recepciones para el tablero.</summary>
    /// <param name="statusCode">Estado persistido.</param>
    /// <returns>Nombre visible según el idioma activo.</returns>
    private string PurchaseStatusLabel(string statusCode) =>
        statusCode switch
        {
            "BORRADOR" => L("Borrador", "Draft"),
            "PENDIENTE_APROBACION" => L("Pendiente de aprobación", "Awaiting approval"),
            "APROBADA" => L("Aprobada", "Approved"),
            "RECIBIDA_PARCIAL" => L("Recibida parcialmente", "Partially received"),
            "RECIBIDA" => L("Recibida", "Received"),
            "CERRADA" => L("Cerrada", "Closed"),
            "CANCELADA" => L("Cancelada", "Cancelled"),
            "CONFIRMADA" => L("Confirmada", "Confirmed"),
            "REVERSADA" => L("Reversada", "Reversed"),
            _ => statusCode.Replace('_', ' '),
        };

    /// <summary>Calcula asuntos accionables de activos, mantenimiento y custodia externa.</summary>
    /// <returns>Pendientes ordenados por severidad.</returns>
    private IReadOnlyList<DecisionInfo> BuildTraceabilityDecisions()
    {
        if (_data.Traceability is not { } traceability)
            return [];

        var decisions = new List<DecisionInfo>();
        var assetsRequiringAttention = traceability.Assets.Count(item =>
            DashboardSummaryLogic.NeedsAssetAttention(item.StatusCode, item.Condition)
        );
        if (assetsRequiringAttention > 0)
            decisions.Add(
                new(
                    L("Activos no disponibles", "Unavailable assets"),
                    L(
                        $"{assetsRequiringAttention:N0} activos están bloqueados, dañados o en revisión.",
                        $"{assetsRequiringAttention:N0} assets are blocked, damaged or under review."
                    ),
                    L("Revisar", "Review"),
                    DashboardModule.Traceability,
                    DecisionTone.Danger
                )
            );

        var today = DateTime.Today;
        var openLoans = traceability
            .Loans.Where(item => item.StatusCode == "PRESTADO")
            .DistinctBy(item => item.LoanId)
            .ToList();
        var overdueLoans = openLoans.Count(item =>
            DashboardSummaryLogic.IsOverdueLoan(
                item.StatusCode,
                item.ExpectedReturnDateUtc,
                item.ActualReturnDateUtc,
                today
            )
        );
        var loansWithoutReturnDate = openLoans.Count(item => item.ExpectedReturnDateUtc is null);
        if (overdueLoans > 0 || loansWithoutReturnDate > 0)
            decisions.Add(
                new(
                    L("Préstamos que necesitan seguimiento", "Loans requiring follow-up"),
                    L(
                        $"{overdueLoans:N0} vencidos · {loansWithoutReturnDate:N0} sin fecha prevista de devolución.",
                        $"{overdueLoans:N0} overdue · {loansWithoutReturnDate:N0} without an expected return date."
                    ),
                    L("Trazar", "Trace"),
                    DashboardModule.Traceability,
                    overdueLoans > 0 ? DecisionTone.Danger : DecisionTone.Warning
                )
            );

        var activeMaintenance = traceability.Maintenances.Count(item =>
            item.StatusCode == "EN_CURSO"
        );
        var reviewDue = traceability.Maintenances.Count(item =>
            DashboardSummaryLogic.IsMaintenanceReviewDue(
                item.StatusCode,
                item.NextReviewDate,
                DateOnly.FromDateTime(today)
            )
        );
        if (activeMaintenance > 0 || reviewDue > 0)
            decisions.Add(
                new(
                    L("Mantenimientos por controlar", "Maintenance requiring review"),
                    L(
                        $"{activeMaintenance:N0} en curso · {reviewDue:N0} revisiones alcanzaron su fecha.",
                        $"{activeMaintenance:N0} in progress · {reviewDue:N0} reviews reached their due date."
                    ),
                    L("Controlar", "Review"),
                    DashboardModule.Traceability,
                    reviewDue > 0 ? DecisionTone.Danger : DecisionTone.Warning
                )
            );

        var inconsistentContent = traceability.Assets.Count(item =>
            DashboardSummaryLogic.IsTraceabilityContentInconsistent(
                item.ContentQuantity,
                item.ContentProductId,
                item.ContentLotId
            )
        );
        if (inconsistentContent > 0)
            decisions.Add(
                new(
                    L("Contenido sin origen completo", "Content with incomplete origin"),
                    L(
                        $"{inconsistentContent:N0} activos requieren reconstruir producto o lote de contenido.",
                        $"{inconsistentContent:N0} assets require their content product or lot to be restored."
                    ),
                    L("Auditar", "Audit"),
                    DashboardModule.Traceability,
                    DecisionTone.Danger
                )
            );

        return decisions.OrderByDescending(item => item.Tone).ThenBy(item => item.Title).ToList();
    }

    /// <summary>Presenta los últimos eventos inmutables de los activos sin duplicarlos.</summary>
    /// <returns>Eventos ordenados desde el más reciente.</returns>
    private IReadOnlyList<ActivityInfo> BuildTraceabilityActivity()
    {
        if (_data.Traceability is not { } traceability)
            return [];

        return traceability
            .Events.DistinctBy(item => item.EventId)
            .Select(item => new ActivityInfo(
                ToLocal(item.EventDateUtc),
                TraceabilityEventLabel(item.EventType),
                BuildTraceabilityEventDescription(item)
            ))
            .OrderByDescending(item => item.When)
            .ToList();
    }

    /// <summary>Describe el cambio confirmado de un activo.</summary>
    /// <param name="item">Evento inmutable persistido.</param>
    /// <returns>Activo, transición de estado y, cuando existe, transición de cantidad.</returns>
    private string BuildTraceabilityEventDescription(AssetEventResponse item)
    {
        var previous = string.IsNullOrWhiteSpace(item.StatusBefore)
            ? L("Inicial", "Initial")
            : TraceabilityStateLabel(item.StatusBefore);
        var description = $"{item.Asset} · {previous} → {TraceabilityStateLabel(item.StatusAfter)}";
        if (item.QuantityBefore is not null || item.QuantityAfter is not null)
            description += L(
                $" · Cantidad {item.QuantityBefore?.ToString("N4") ?? "—"} → {item.QuantityAfter?.ToString("N4") ?? "—"}",
                $" · Quantity {item.QuantityBefore?.ToString("N4") ?? "—"} → {item.QuantityAfter?.ToString("N4") ?? "—"}"
            );
        return description;
    }

    /// <summary>Traduce el tipo técnico de un evento trazable.</summary>
    /// <param name="eventType">Código persistido del evento.</param>
    /// <returns>Nombre breve según el idioma activo.</returns>
    private string TraceabilityEventLabel(string eventType) =>
        eventType switch
        {
            "CLIENTE_ALTA" => L("Alta de activo", "Asset registration"),
            "CLIENTE_INGRESO" => L("Ingreso en custodia", "Custody intake"),
            "CLIENTE_ENTREGA" => L("Entrega al cliente", "Customer delivery"),
            "MEDICION" => L("Medición", "Measurement"),
            "FRACCIONAMIENTO" => L("Fraccionamiento", "Transformation"),
            "INCIDENTE" => L("Incidente", "Incident"),
            "MANTENIMIENTO_INICIO" => L("Inicio de mantenimiento", "Maintenance started"),
            "MANTENIMIENTO_FIN" => L("Fin de mantenimiento", "Maintenance completed"),
            "PRESTAMO_SALIDA" => L("Salida en préstamo", "Loan departure"),
            "PRESTAMO_DEVOLUCION" => L("Devolución de préstamo", "Loan return"),
            _ => eventType.Replace('_', ' '),
        };

    /// <summary>Traduce estados frecuentes de activos para la actividad.</summary>
    /// <param name="statusCode">Código persistido.</param>
    /// <returns>Estado visible según el idioma activo.</returns>
    private string TraceabilityStateLabel(string statusCode) =>
        statusCode switch
        {
            "ACTIVO" => L("Activo", "Active"),
            "DISPONIBLE" => L("Disponible", "Available"),
            "BLOQUEADO" => L("Bloqueado", "Blocked"),
            "PRESTADO" => L("Prestado", "On loan"),
            "EN_CLIENTE" => L("En cliente", "At customer"),
            "DEVUELTO" => L("Devuelto", "Returned"),
            _ => statusCode.Replace('_', ' '),
        };

    /// <summary>Cuenta préstamos únicos que conservan activos fuera de la empresa.</summary>
    /// <returns>Cantidad de préstamos abiertos sin repetir renglones.</returns>
    private int CountOpenLoans() =>
        _data
            .Traceability?.Loans.DistinctBy(item => item.LoanId)
            .Count(item => item.StatusCode == "PRESTADO")
        ?? 0;

    /// <summary>Cuenta fraccionamientos confirmados durante el día local.</summary>
    /// <returns>Cantidad de cabeceras sin repetir activos de destino.</returns>
    private int CountTransformationsToday() =>
        _data
            .Traceability?.Transformations.Where(item =>
                ToLocal(item.TransformationDateUtc).Date == DateTime.Today
            )
            .DistinctBy(item => item.TransformationId)
            .Count()
        ?? 0;

    /// <summary>Cuenta eventos inmutables ocurridos durante el día local.</summary>
    /// <returns>Cantidad de eventos únicos.</returns>
    private int CountTraceabilityEventsToday() =>
        _data
            .Traceability?.Events.Where(item => ToLocal(item.EventDateUtc).Date == DateTime.Today)
            .DistinctBy(item => item.EventId)
            .Count()
        ?? 0;

    /// <summary>Construye un paneo general consistente para un módulo.</summary>
    /// <param name="module">Módulo cuyo resumen se mostrará.</param>
    /// <param name="presentation">Textos y acciones disponibles.</param>
    /// <returns>Vista desplazable con métricas y accesos operativos.</returns>
    private Control BuildModuleView(DashboardModule module, ModulePresentation presentation)
    {
        var scroll = CreateScrollablePage();
        var page = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = false,
            Height = 610,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = UiTheme.Background,
            Padding = new Padding(28, 24, 28, 28),
            Margin = Padding.Empty,
        };
        page.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 116));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 154));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        page.Controls.Add(BuildModuleHeading(presentation), 0, 0);
        page.Controls.Add(BuildModuleMetrics(module), 0, 1);
        page.Controls.Add(BuildModuleBottom(module, presentation), 0, 2);
        scroll.Controls.Add(page);
        ResizePageWith(scroll, page);
        return scroll;
    }

    /// <summary>Combina las acciones y los asuntos pendientes del módulo.</summary>
    /// <param name="module">Módulo activo.</param>
    /// <param name="presentation">Acciones disponibles.</param>
    /// <returns>Fila inferior en dos columnas.</returns>
    private Control BuildModuleBottom(DashboardModule module, ModulePresentation presentation)
    {
        var lower = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = UiTheme.Background,
            Margin = new Padding(0, 12, 0, 0),
        };
        lower.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        lower.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        lower.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        lower.Controls.Add(BuildModuleActions(module, presentation), 0, 0);
        lower.Controls.Add(BuildModuleAttention(module), 1, 0);
        return lower;
    }

    /// <summary>Presenta únicamente las decisiones relacionadas con el módulo activo.</summary>
    /// <param name="module">Módulo usado para filtrar.</param>
    /// <returns>Tarjeta con pendientes o estado saludable.</returns>
    private Control BuildModuleAttention(DashboardModule module)
    {
        var card = CreateCard();
        card.Margin = new Padding(8, 0, 0, 0);
        card.Padding = new Padding(20, 16, 20, 16);
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.White,
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 49));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(
            CreateSectionHeader(
                L("Atención del módulo", "Module attention"),
                L(
                    "Pendientes detectados con la información actual.",
                    "Pending items detected in current data."
                )
            ),
            0,
            0
        );

        var rows = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            AutoScroll = true,
            BackColor = Color.White,
        };
        rows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var decisions = BuildDecisions().Where(item => item.Module == module).Take(3).ToList();
        if (decisions.Count == 0)
        {
            rows.RowCount = 1;
            rows.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            rows.Controls.Add(
                CreateEmptyState(
                    L(
                        "No hay alertas críticas para este módulo.",
                        "There are no critical alerts for this module."
                    )
                )
            );
        }
        else
        {
            rows.RowCount = decisions.Count + 1;
            for (var index = 0; index < decisions.Count; index++)
            {
                rows.RowStyles.Add(new RowStyle(SizeType.Absolute, 75));
                rows.Controls.Add(CreateDecisionRow(decisions[index]), 0, index);
            }
            rows.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        }
        layout.Controls.Add(rows, 0, 1);
        card.Controls.Add(layout);
        return card;
    }

    /// <summary>Construye el propósito visible del módulo.</summary>
    /// <param name="presentation">Título y explicación del módulo.</param>
    /// <returns>Encabezado del paneo modular.</returns>
    private static Control BuildModuleHeading(ModulePresentation presentation)
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Background };
        var title = new Label
        {
            Text = presentation.Title,
            Left = 0,
            Top = 4,
            Height = 42,
            Font = new Font("Segoe UI", 26F, FontStyle.Bold),
            ForeColor = BrandGreen,
            AutoEllipsis = true,
        };
        var description = new Label
        {
            Text = presentation.Description,
            Left = 2,
            Top = 52,
            Height = 50,
            Font = new Font("Segoe UI", 11F),
            ForeColor = MutedText,
        };
        panel.Controls.Add(title);
        panel.Controls.Add(description);
        panel.Resize += (_, _) =>
        {
            title.Width = Math.Max(1, panel.ClientSize.Width);
            description.Width = Math.Max(1, panel.ClientSize.Width - 4);
        };
        return panel;
    }

    /// <summary>Construye las métricas específicas del módulo.</summary>
    /// <param name="module">Módulo del que se obtendrán los valores.</param>
    /// <returns>Fila adaptable de tarjetas.</returns>
    private Control BuildModuleMetrics(DashboardModule module)
    {
        var metrics = GetModuleMetrics(module);
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = metrics.Count,
            RowCount = 1,
            BackColor = UiTheme.Background,
            Padding = new Padding(0, 4, 0, 8),
            Margin = Padding.Empty,
        };
        row.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        for (var index = 0; index < metrics.Count; index++)
        {
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / metrics.Count));
            var card = CreateModuleMetric(metrics[index]);
            card.Margin = new Padding(0, 0, index == metrics.Count - 1 ? 0 : 14, 0);
            row.Controls.Add(card, index, 0);
        }
        return row;
    }

    /// <summary>Calcula indicadores del módulo seleccionado.</summary>
    /// <param name="module">Módulo activo.</param>
    /// <returns>Tres o cuatro valores operativos.</returns>
    private IReadOnlyList<MetricInfo> GetModuleMetrics(DashboardModule module)
    {
        return module switch
        {
            DashboardModule.Commercial =>
            [
                Metric(
                    L("Clientes disponibles", "Available customers"),
                    _data.Sales?.Clients.Count ?? 0
                ),
                Metric(
                    L("Listas de precios vigentes", "Active price lists"),
                    _data.Sales?.PriceLists.Count(x =>
                        DashboardSummaryLogic.IsEffective(
                            x.StatusCode,
                            x.ValidFrom,
                            x.ValidUntil,
                            DateOnly.FromDateTime(DateTime.Today)
                        )
                    )
                        ?? 0
                ),
                Metric(
                    L("Promociones vigentes", "Active promotions"),
                    _data.Sales?.Promotions.Count(x =>
                        DashboardSummaryLogic.IsEffective(
                            x.StatusCode,
                            x.ValidFrom,
                            x.ValidUntil,
                            DateOnly.FromDateTime(DateTime.Today)
                        )
                    )
                        ?? 0
                ),
                Metric(L("Ventas de hoy", "Today's sales"), CountSalesToday()),
            ],
            DashboardModule.Inventory =>
            [
                Metric(
                    L("Productos activos", "Active products"),
                    _data.Inventory?.Products.Count(x => x.StatusCode == "ACTIVO") ?? 0
                ),
                Metric(
                    L("Depósitos activos", "Active warehouses"),
                    _data.Inventory?.Warehouses.Count(x => x.StatusCode == "ACTIVO") ?? 0
                ),
                Metric(
                    L("Posiciones disponibles", "Available positions"),
                    _data.Inventory?.Stock.Count(x => x.AvailableQuantity > 0) ?? 0
                ),
                Metric(
                    L("Bajo mínimo", "Below minimum"),
                    _data.Inventory?.Stock.Count(x => x.IsBelowMinimum) ?? 0
                ),
            ],
            DashboardModule.Purchasing =>
            [
                Metric(
                    L("Proveedores activos", "Active suppliers"),
                    _data.Purchasing?.Suppliers.Count(x => x.StatusCode == "ACTIVO") ?? 0
                ),
                Metric(
                    L("Órdenes en curso", "Orders in progress"),
                    _data
                        .Purchasing?.Orders.DistinctBy(x => x.PurchaseOrderId)
                        .Count(x => DashboardSummaryLogic.IsOpenPurchaseOrder(x.StatusCode))
                        ?? 0
                ),
                Metric(
                    L("Órdenes por recibir", "Orders awaiting receipt"),
                    CountReceivablePurchaseOrders()
                ),
                Metric(
                    L("Renglones por recibir", "Lines awaiting receipt"),
                    _data.Purchasing?.Orders.Count(x =>
                        x.PendingQuantity > 0
                        && DashboardSummaryLogic.IsReceivablePurchaseOrder(x.StatusCode)
                    )
                        ?? 0
                ),
            ],
            DashboardModule.Traceability =>
            [
                Metric(
                    L("Activos trazados", "Tracked assets"),
                    _data.Traceability?.Assets.Count ?? 0
                ),
                Metric(
                    L("Lotes con saldo", "Lots with balance"),
                    _data.Traceability?.Lots.Count(item => item.Quantity > 0) ?? 0
                ),
                Metric(
                    L("Activos con alerta", "Assets requiring attention"),
                    _data.Traceability?.Assets.Count(item =>
                        DashboardSummaryLogic.NeedsAssetAttention(item.StatusCode, item.Condition)
                    )
                        ?? 0
                ),
                Metric(L("Préstamos abiertos", "Open loans"), CountOpenLoans()),
            ],
            DashboardModule.Logistics =>
            [
                Metric(
                    L("Solicitudes pendientes", "Pending requests"),
                    _data.Logistics?.Requests.Count(x => x.StatusCode == "PENDIENTE") ?? 0
                ),
                Metric(
                    L("Hojas activas", "Active routes"),
                    _data.Logistics?.Routes.Count(x =>
                        DashboardSummaryLogic.IsActiveRoute(x.StatusCode)
                    )
                        ?? 0
                ),
                Metric(
                    L("Paradas pendientes", "Pending stops"),
                    _data.Logistics?.Stops.Count(x =>
                        !DashboardSummaryLogic.IsCompleted(x.StatusCode)
                    )
                        ?? 0
                ),
                Metric(
                    L("Avisos pendientes", "Pending notices"),
                    _data.Logistics?.Notifications.Count(x => x.StatusCode == "PENDIENTE") ?? 0
                ),
            ],
            DashboardModule.Finance =>
            [
                Metric(
                    L("Cajas abiertas", "Open cash sessions"),
                    _data.Finance?.CashSessions.Count(x => x.StatusCode == "ABIERTA") ?? 0
                ),
                Metric(
                    L("Ventas con saldo", "Sales with balance"),
                    _data.Finance?.Sales.Count(x => x.Outstanding > 0) ?? 0
                ),
                new(
                    L("Saldo pendiente", "Outstanding balance"),
                    (_data.Finance?.Sales.Sum(x => x.Outstanding) ?? 0).ToString("N2"),
                    "ARS",
                    DashboardModule.Finance
                ),
                Metric(
                    L("Cobros confirmados", "Confirmed payments"),
                    _data.Finance?.Payments.Count(x => x.StatusCode == "CONFIRMADO") ?? 0
                ),
            ],
            DashboardModule.Security =>
            [
                Metric(L("Usuarios", "Users"), _data.Users?.Count ?? 0),
                Metric(L("Roles de tu sesión", "Your session roles"), _session.Roles.Count),
                Metric(
                    L("Permisos habilitados", "Enabled permissions"),
                    _session.Permissions.Count
                ),
                new(
                    L("Sesión válida hasta", "Session valid until"),
                    _session.ExpiresAtUtc.ToLocalTime().ToString("HH:mm"),
                    L("hora local", "local time"),
                    DashboardModule.Security
                ),
            ],
            DashboardModule.Configuration =>
            [
                new(
                    L("Empresa y sucursal", "Company and branch"),
                    "1",
                    _workplaceName,
                    DashboardModule.Configuration
                ),
                new(
                    L("Idioma", "Language"),
                    CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToUpperInvariant(),
                    L("preferencia activa", "active preference"),
                    DashboardModule.Configuration
                ),
                Metric(
                    L("Permisos de configuración", "Configuration permissions"),
                    _session.Permissions.Count(x =>
                        x.StartsWith("CONFIGURACION.", StringComparison.OrdinalIgnoreCase)
                    )
                ),
            ],
            _ => [],
        };
    }

    /// <summary>Crea una métrica numérica usando el módulo actualmente seleccionado.</summary>
    /// <param name="title">Título visible.</param>
    /// <param name="value">Valor entero.</param>
    /// <returns>Indicador formateado con separador de miles.</returns>
    private MetricInfo Metric(string title, int value) =>
        new(title, value.ToString("N0"), L("registros", "records"), _selectedModule);

    /// <summary>Construye una tarjeta de indicador modular.</summary>
    /// <param name="metric">Indicador calculado.</param>
    /// <returns>Tarjeta blanca de tamaño uniforme.</returns>
    private static Control CreateModuleMetric(MetricInfo metric)
    {
        var card = CreateCard();
        card.Dock = DockStyle.Fill;
        card.Padding = new Padding(18, 14, 18, 12);
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.White,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 27));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 23));
        layout.Controls.Add(
            new Label
            {
                Text = metric.Title,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = UiTheme.Text,
                AutoEllipsis = true,
            },
            0,
            0
        );
        layout.Controls.Add(
            new Label
            {
                Text = metric.Value,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 25F, FontStyle.Bold),
                ForeColor = BrandGreen,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
            },
            0,
            1
        );
        layout.Controls.Add(
            new Label
            {
                Text = metric.Detail,
                Dock = DockStyle.Fill,
                ForeColor = MutedText,
                Font = new Font("Segoe UI", 9.5F),
                AutoEllipsis = true,
            },
            0,
            2
        );
        card.Controls.Add(layout);
        return card;
    }

    /// <summary>Construye el bloque de acciones y la explicación de uso del módulo.</summary>
    /// <param name="module">Módulo activo.</param>
    /// <param name="presentation">Acciones disponibles.</param>
    /// <returns>Tarjeta con accesos a las pantallas operativas.</returns>
    private Control BuildModuleActions(DashboardModule module, ModulePresentation presentation)
    {
        var card = CreateCard();
        card.Margin = new Padding(0, 0, 8, 0);
        card.Padding = new Padding(24, 20, 24, 20);
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.White,
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(SectionTitle(L("Acciones del módulo", "Module actions")), 0, 0);
        layout.Controls.Add(
            new Label
            {
                Text = presentation.Guidance,
                Dock = DockStyle.Fill,
                ForeColor = MutedText,
                Font = new Font("Segoe UI", 10F),
            },
            0,
            1
        );

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(0, 10, 0, 0),
            BackColor = Color.White,
        };
        foreach (var action in presentation.Actions)
        {
            var button = UiTheme.Button(action.Text, 0, 0, action.Width, action.Tone);
            button.Height = 44;
            button.Margin = new Padding(0, 0, 12, 10);
            button.Click += (_, _) => OpenModuleDetail(module, action.Target);
            actions.Controls.Add(button);
        }
        layout.Controls.Add(actions, 0, 2);
        card.Controls.Add(layout);
        return card;
    }

    /// <summary>Obtiene textos y acciones de cada módulo sin crear formularios especulativos.</summary>
    /// <param name="module">Módulo seleccionado.</param>
    /// <returns>Presentación localizada del módulo.</returns>
    /// <exception cref="ArgumentOutOfRangeException">El módulo no pertenece al catálogo del tablero.</exception>
    private ModulePresentation GetModulePresentation(DashboardModule module)
    {
        return module switch
        {
            DashboardModule.Commercial => new(
                L("Comercial", "Commercial"),
                L(
                    "Clientes, precios, promociones, pedidos y ventas en un mismo circuito.",
                    "Customers, prices, promotions, orders and sales in one flow."
                ),
                L(
                    "Revisá primero el estado general y después ingresá a la tarea que vas a realizar.",
                    "Review the summary first, then enter the task you need."
                ),
                [
                    new(L("Administrar clientes", "Manage customers"), "CLIENTS", 190),
                    new(L("Pedidos y ventas", "Orders and sales"), "SALES", 190),
                ]
            ),
            DashboardModule.Inventory => new(
                L("Inventario", "Inventory"),
                L(
                    "Productos, depósitos, existencias y movimientos con stock físico, reservado y disponible.",
                    "Products, warehouses, balances and movements with physical, reserved and available stock."
                ),
                L(
                    "Usá la gestión completa para modificar maestros o confirmar movimientos.",
                    "Use full management to edit master data or confirm movements."
                ),
                [
                    new(
                        L("Abrir gestión de inventario", "Open inventory management"),
                        "INVENTORY",
                        230
                    ),
                ]
            ),
            DashboardModule.Purchasing => new(
                L("Compras", "Purchasing"),
                L(
                    "Proveedores, órdenes y recepciones parciales conservando pendientes y reversas auditables.",
                    "Suppliers, orders and partial receipts with pending balances and auditable reversals."
                ),
                L(
                    "Las cantidades aceptadas aumentan stock; faltantes, daños y rechazos quedan documentados.",
                    "Accepted quantities increase stock; shortages, damage and rejections remain documented."
                ),
                [
                    new(
                        L("Abrir gestión de compras", "Open purchasing management"),
                        "PURCHASING",
                        220
                    ),
                ]
            ),
            DashboardModule.Traceability => new(
                L("Trazabilidad", "Traceability"),
                L(
                    "Lotes, tubos, contenido, fraccionamientos, incidentes, mantenimientos y préstamos.",
                    "Lots, cylinders, content, transformations, incidents, maintenance and loans."
                ),
                L(
                    "Cada cambio conserva fecha, usuario, documento, cantidades y correlación.",
                    "Every change keeps date, user, document, quantities and correlation."
                ),
                [
                    new(
                        L("Abrir trazabilidad industrial", "Open industrial traceability"),
                        "TRACEABILITY",
                        235
                    ),
                ]
            ),
            DashboardModule.Logistics => new(
                L("Logística", "Logistics"),
                L(
                    "Solicitudes, prioridades, ventanas horarias, transportistas, vehículos y hojas de ruta.",
                    "Requests, priorities, time windows, drivers, vehicles and route sheets."
                ),
                L(
                    "Planificá manualmente con información guardada; la optimización automática queda preparada para una integración futura.",
                    "Plan manually with stored information; automatic optimization remains ready for a future integration."
                ),
                [new(L("Abrir logística y rutas", "Open logistics and routes"), "LOGISTICS", 220)]
            ),
            DashboardModule.Finance => new(
                L("Finanzas", "Finance"),
                L(
                    "Cajas, cobros, medios de pago, aplicaciones y cuenta corriente del cliente.",
                    "Cash boxes, collections, payment methods, applications and customer accounts."
                ),
                L(
                    "Los recibos son internos y no reemplazan la futura factura fiscal.",
                    "Receipts are internal and do not replace the future tax invoice."
                ),
                FinanceActions()
            ),
            DashboardModule.Security => new(
                L("Seguridad", "Security"),
                L(
                    "Usuarios, roles, sesiones y accesos protegidos por empresa y sucursal.",
                    "Users, roles, sessions and access protected by company and branch."
                ),
                L(
                    "Administrá solamente los accesos necesarios y revocá sesiones cuando corresponda.",
                    "Grant only needed access and revoke sessions when appropriate."
                ),
                [new(L("Administrar usuarios", "Manage users"), "USERS", 205)]
            ),
            DashboardModule.Configuration => new(
                L("Configuración", "Configuration"),
                L(
                    "Datos de empresa, sucursales, parámetros operativos, idioma y notificaciones.",
                    "Company data, branches, operating parameters, language and notifications."
                ),
                L(
                    "Los cambios se aplican dentro de la empresa autenticada y quedan auditados.",
                    "Changes apply within the authenticated company and remain audited."
                ),
                [new(L("Abrir configuración", "Open configuration"), "CONFIGURATION", 200)]
            ),
            _ => throw new ArgumentOutOfRangeException(
                nameof(module),
                module,
                "El módulo no posee presentación."
            ),
        };
    }

    /// <summary>Obtiene las acciones financieras permitidas por la sesión.</summary>
    /// <returns>Gestión financiera y, cuando corresponde, preparación fiscal.</returns>
    private IReadOnlyList<ModuleAction> FinanceActions()
    {
        var actions = new List<ModuleAction>
        {
            new(L("Caja, cobros y saldos", "Cash, collections and balances"), "FINANCE", 220),
        };
        if (Can("FISCAL.CONSULTAR"))
            actions.Add(
                new(L("Preparación fiscal", "Tax readiness"), "FISCAL", 190, ButtonTone.Neutral)
            );
        return actions;
    }

    /// <summary>Abre una pantalla operativa dentro del shell persistente.</summary>
    /// <param name="module">Módulo que conservará la selección lateral.</param>
    /// <param name="target">Pantalla concreta dentro del módulo.</param>
    /// <exception cref="ArgumentOutOfRangeException">La pantalla solicitada no pertenece al catálogo.</exception>
    private void OpenModuleDetail(DashboardModule module, string target)
    {
        Form form = target switch
        {
            "CLIENTS" => new ClientManagementForm(_apiClient, _session),
            "SALES" => new SalesManagementForm(_apiClient, _session),
            "INVENTORY" => new InventoryManagementForm(_apiClient, _session),
            "PURCHASING" => new PurchasingManagementForm(_apiClient, _session),
            "TRACEABILITY" => new TraceabilityManagementForm(_apiClient, _session),
            "LOGISTICS" => new LogisticsManagementForm(_apiClient, _session),
            "FINANCE" => new FinanceManagementForm(_apiClient, _session),
            "FISCAL" => new FiscalReadinessForm(_apiClient, _session),
            "USERS" => new UserManagementForm(_apiClient, _session.Token, _session.UserId),
            "CONFIGURATION" => new ConfigurationForm(
                _apiClient,
                _session.Token,
                _session.Permissions
            ),
            _ => throw new ArgumentOutOfRangeException(
                nameof(target),
                target,
                "La pantalla solicitada no existe."
            ),
        };

        EmbedForm(form, module);
    }

    /// <summary>Integra un formulario existente sin ocultar la navegación ni el encabezado global.</summary>
    /// <param name="form">Formulario operativo que se alojará.</param>
    /// <param name="module">Módulo al que pertenece.</param>
    private void EmbedForm(Form form, DashboardModule module)
    {
        CloseEmbeddedForm();
        SelectNavigation(module);
        var presentation = GetModulePresentation(module);
        _pageTitle.Text = presentation.Title;
        _contentHost.Controls.Clear();

        var container = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = UiTheme.Background,
        };
        container.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        container.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        container.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        container.Controls.Add(BuildEmbeddedNavigation(module, presentation, form.Text), 0, 0);

        form.TopLevel = false;
        form.FormBorderStyle = FormBorderStyle.None;
        form.WindowState = FormWindowState.Normal;
        form.Dock = DockStyle.Fill;
        form.MinimumSize = Size.Empty;
        form.FormClosed += EmbeddedForm_FormClosed;
        _embeddedForm = form;
        container.Controls.Add(form, 0, 1);
        _contentHost.Controls.Add(container);
        form.Show();
    }

    /// <summary>Crea una ruta de regreso visible sobre cualquier pantalla operativa incrustada.</summary>
    /// <param name="module">Módulo cuyo resumen se recuperará.</param>
    /// <param name="presentation">Nombre localizado del módulo.</param>
    /// <param name="detailTitle">Título de la operación abierta.</param>
    /// <returns>Barra con contexto y botón de regreso accesible.</returns>
    private Control BuildEmbeddedNavigation(
        DashboardModule module,
        ModulePresentation presentation,
        string detailTitle
    )
    {
        var bar = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(18, 8, 18, 8),
            Margin = Padding.Empty,
        };
        var back = UiTheme.Button(
            L(
                $"Volver al resumen de {presentation.Title}",
                $"Back to {presentation.Title} overview"
            ),
            18,
            8,
            250,
            ButtonTone.Neutral
        );
        back.Height = 38;
        back.AccessibleName = L(
            $"Volver al tablero de {presentation.Title}",
            $"Back to {presentation.Title} dashboard"
        );
        back.Click += (_, _) => ShowModuleOverview(module);
        bar.Controls.Add(back);
        var context = new Label
        {
            Text = L($"Estás en: {detailTitle}", $"You are in: {detailTitle}"),
            Left = 286,
            Top = 17,
            Height = 24,
            Width = 520,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = BrandGreen,
            AutoEllipsis = true,
        };
        bar.Controls.Add(context);
        bar.Resize += (_, _) =>
            context.Width = Math.Max(120, bar.ClientSize.Width - context.Left - 18);
        return bar;
    }

    /// <summary>Responde a cierres solicitados por Seguridad o Configuración.</summary>
    /// <param name="sender">Formulario operativo cerrado.</param>
    /// <param name="e">Datos del cierre.</param>
    private void EmbeddedForm_FormClosed(object? sender, FormClosedEventArgs e)
    {
        if (sender is UserManagementForm { DialogResult: DialogResult.Abort })
        {
            BeginRemoteClosure();
            return;
        }
        if (sender is ConfigurationForm { DialogResult: DialogResult.Retry })
        {
            ReloadInterface = true;
            Close();
            return;
        }
        if (!_endingSession && !IsDisposed)
            ShowModuleOverview(_selectedModule);
    }

    /// <summary>Cierra y libera únicamente el formulario operativo actualmente incrustado.</summary>
    private void CloseEmbeddedForm()
    {
        if (_embeddedForm is null)
            return;
        var form = _embeddedForm;
        _embeddedForm = null;
        form.FormClosed -= EmbeddedForm_FormClosed;
        if (!form.IsDisposed)
            form.Dispose();
    }

    /// <summary>Marca visualmente un único módulo en la barra lateral.</summary>
    /// <param name="module">Módulo que pasa a estar activo.</param>
    private void SelectNavigation(DashboardModule module)
    {
        _selectedModule = module;
        foreach (var item in _navigation)
            item.Value.Selected = item.Key == module;
    }

    /// <summary>Solicita volver al acceso para cambiar empresa o sucursal de manera segura.</summary>
    private void ChangeWorkplace()
    {
        var result = MessageBox.Show(
            L(
                "Para cambiar de empresa o sucursal hay que volver al acceso. ¿Querés continuar?",
                "To change company or branch you must return to sign in. Do you want to continue?"
            ),
            L("Cambiar empresa o sucursal", "Change company or branch"),
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question
        );
        if (result != DialogResult.Yes)
            return;
        LoginAgain = true;
        Close();
    }

    /// <summary>Carga en paralelo solamente los módulos autorizados y conserva resultados parciales.</summary>
    /// <returns>Tarea que finaliza cuando el tablero muestra los datos disponibles.</returns>
    private async Task LoadDashboardAsync()
    {
        _connectionStatus.Text = L(
            "Actualizando información operativa...",
            "Updating operating information..."
        );
        _connectionStatus.ForeColor = MutedText;

        var sales = LoadWhenAllowedAsync(
            Can("COMERCIAL.CONSULTAR"),
            () => _apiClient.GetSalesAsync(_session.Token)
        );
        var inventory = LoadWhenAllowedAsync(
            Can("INVENTARIO.CONSULTAR"),
            () => _apiClient.GetInventoryAsync(_session.Token)
        );
        var purchasing = LoadWhenAllowedAsync(
            Can("COMPRAS.CONSULTAR"),
            () => _apiClient.GetPurchasingAsync(_session.Token)
        );
        var traceability = LoadWhenAllowedAsync(
            Can("INVENTARIO.CONSULTAR"),
            () => _apiClient.GetTraceabilityAsync(_session.Token)
        );
        var logistics = LoadWhenAllowedAsync(
            Can("LOGISTICA.CONSULTAR"),
            () => _apiClient.GetLogisticsAsync(_session.Token)
        );
        var finance = LoadWhenAllowedAsync(
            Can("FINANZAS.CONSULTAR"),
            () => _apiClient.GetFinanceAsync(_session.Token)
        );
        var users = LoadWhenAllowedAsync(
            IsAdministrator(),
            () => _apiClient.GetUsersAsync(_session.Token)
        );

        await Task.WhenAll(sales, inventory, purchasing, traceability, logistics, finance, users);
        _data = new DashboardData(
            sales.Result.Value,
            inventory.Result.Value,
            purchasing.Result.Value,
            traceability.Result.Value,
            logistics.Result.Value,
            finance.Result.Value,
            users.Result.Value,
            sales.Result.Failed
                || inventory.Result.Failed
                || purchasing.Result.Failed
                || traceability.Result.Failed
                || logistics.Result.Failed
                || finance.Result.Failed
                || users.Result.Failed
        );

        var decisions = BuildDecisions();
        _notificationButton.Count = decisions.Count;
        _updatedStatus.Text = L(
            $"Actualizado {DateTime.Now:HH:mm}",
            $"Updated {DateTime.Now:HH:mm}"
        );
        _connectionStatus.Text = _data.HasFailures
            ? L(
                "Operación disponible · algunos datos no respondieron",
                "Operation available · some data did not respond"
            )
            : L(
                "Operación sincronizada · API disponible · sin errores pendientes",
                "Operation synchronized · API available · no pending errors"
            );
        _connectionStatus.ForeColor = _data.HasFailures ? Color.FromArgb(180, 83, 9) : BrandGreen;

        if (_embeddedForm is null)
        {
            if (_selectedModule == DashboardModule.Home)
                ShowHome();
            else
                ShowModuleOverview(_selectedModule);
        }
    }

    /// <summary>Ejecuta una consulta autorizada sin impedir que los demás módulos carguen.</summary>
    /// <typeparam name="T">Tipo de snapshot esperado.</typeparam>
    /// <param name="allowed">Indica si la sesión puede consultar el módulo.</param>
    /// <param name="load">Consulta HTTP que obtiene el snapshot.</param>
    /// <returns>Resultado, ausencia por permiso o falla controlada.</returns>
    private static async Task<LoadResult<T>> LoadWhenAllowedAsync<T>(
        bool allowed,
        Func<Task<T>> load
    )
        where T : class
    {
        if (!allowed)
            return new(null, false);
        try
        {
            return new(await load(), false);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidDataException)
        {
            return new(null, true);
        }
    }

    /// <summary>Inicializa el monitoreo y carga el primer estado real.</summary>
    /// <param name="sender">Formulario mostrado.</param>
    /// <param name="e">Datos del evento.</param>
    private async void DashboardForm_Shown(object? sender, EventArgs e)
    {
        _sessionMonitor.Start();
        await LoadDashboardAsync();
    }

    /// <summary>Comprueba la vigencia remota sin cerrar por una falla de red temporal.</summary>
    /// <param name="sender">Temporizador de sesión.</param>
    /// <param name="e">Datos del evento.</param>
    private async void SessionMonitor_Tick(object? sender, EventArgs e)
    {
        if (_checkingSession || _endingSession)
            return;
        _checkingSession = true;
        try
        {
            if (await _apiClient.GetSessionAsync(_session.Token) is null)
                BeginRemoteClosure();
        }
        catch (HttpRequestException)
        {
            // La red puede recuperarse; no invalida por sí sola una sesión local vigente.
        }
        finally
        {
            _checkingSession = false;
        }
    }

    /// <summary>Muestra el aviso de revocación y cierra todas las pantallas de la sesión.</summary>
    private void BeginRemoteClosure()
    {
        if (_endingSession)
            return;
        _endingSession = true;
        _sessionMonitor.Stop();
        using var notice = new SessionEndedForm();
        notice.ShowDialog(Form.ActiveForm ?? this);
        _sessionClosed = true;
        foreach (
            Form form in Application.OpenForms.Cast<Form>().Where(form => form != this).ToArray()
        )
            form.Close();
        Close();
    }

    /// <summary>Cierra la sesión remota antes de finalizar el formulario principal.</summary>
    /// <param name="sender">Formulario principal.</param>
    /// <param name="e">Permite cancelar el primer intento mientras termina el cierre remoto.</param>
    private async void DashboardForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        _sessionMonitor.Stop();
        CloseEmbeddedForm();
        if (ReloadInterface || _sessionClosed || _endingSession)
            return;
        e.Cancel = true;
        try
        {
            await _apiClient.LogoutAsync(_session.Token);
        }
        catch (HttpRequestException)
        {
            // El cierre local debe continuar aunque la API ya no esté disponible.
        }
        _sessionClosed = true;
        Close();
    }

    /// <summary>Registra módulos y acciones disponibles en la búsqueda global.</summary>
    private void BuildSearchEntries()
    {
        AddSearch(L("Inicio · resumen general", "Home · general overview"), DashboardModule.Home);
        AddSearch(L("Comercial · resumen", "Commercial · overview"), DashboardModule.Commercial);
        AddSearch(L("Clientes", "Customers"), DashboardModule.Commercial, "CLIENTS");
        AddSearch(
            L("Pedidos, ventas, listas y promociones", "Orders, sales, lists and promotions"),
            DashboardModule.Commercial,
            "SALES"
        );
        AddSearch(
            L(
                "Inventario · productos, depósitos y movimientos",
                "Inventory · products, warehouses and movements"
            ),
            DashboardModule.Inventory,
            "INVENTORY"
        );
        AddSearch(
            L(
                "Compras · proveedores, órdenes y recepciones",
                "Purchasing · suppliers, orders and receipts"
            ),
            DashboardModule.Purchasing,
            "PURCHASING"
        );
        AddSearch(
            L("Trazabilidad · activos, lotes y préstamos", "Traceability · assets, lots and loans"),
            DashboardModule.Traceability,
            "TRACEABILITY"
        );
        AddSearch(
            L("Logística · solicitudes y hojas de ruta", "Logistics · requests and route sheets"),
            DashboardModule.Logistics,
            "LOGISTICS"
        );
        AddSearch(
            L("Finanzas · caja, cobros y saldos", "Finance · cash, collections and balances"),
            DashboardModule.Finance,
            "FINANCE"
        );
        if (Can("FISCAL.CONSULTAR"))
            AddSearch(L("Preparación fiscal", "Tax readiness"), DashboardModule.Finance, "FISCAL");
        AddSearch(
            L("Seguridad · usuarios y accesos", "Security · users and access"),
            DashboardModule.Security,
            "USERS"
        );
        AddSearch(
            L("Configuración", "Configuration"),
            DashboardModule.Configuration,
            "CONFIGURATION"
        );
    }

    /// <summary>Agrega una opción buscable solamente cuando el módulo está habilitado.</summary>
    /// <param name="label">Texto que puede escribir el usuario.</param>
    /// <param name="module">Módulo destino.</param>
    /// <param name="target">Pantalla operativa opcional.</param>
    private void AddSearch(string label, DashboardModule module, string? target = null)
    {
        if (!_navigation.TryGetValue(module, out var button) || !button.Enabled)
            return;
        _searchEntries.Add(new(label, module, target));
    }

    /// <summary>Muestra sugerencias reales de navegación debajo del campo global.</summary>
    private void UpdateSearchSuggestions()
    {
        _searchMenu.Items.Clear();
        var text = _globalSearch.Text.Trim();
        if (text.Length == 0)
        {
            _searchMenu.Close();
            return;
        }

        var matches = _searchEntries
            .Where(item =>
                CultureInfo.CurrentCulture.CompareInfo.IndexOf(
                    item.Label,
                    text,
                    CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace
                ) >= 0
            )
            .Take(7)
            .ToList();
        if (matches.Count == 0)
        {
            _searchMenu
                .Items.Add(
                    L("No hay módulos o acciones coincidentes", "No matching modules or actions")
                )
                .Enabled = false;
        }
        else
        {
            foreach (var match in matches)
            {
                var item = _searchMenu.Items.Add(match.Label);
                item.Click += (_, _) => ExecuteSearch(match);
            }
        }
        _searchMenu.Show(_globalSearch, new Point(0, _globalSearch.Height));
    }

    /// <summary>Ejecuta la sugerencia seleccionada y limpia la búsqueda.</summary>
    /// <param name="entry">Destino encontrado.</param>
    private void ExecuteSearch(SearchEntry entry)
    {
        _searchMenu.Close();
        _globalSearch.Clear();
        if (entry.Target is null)
            ShowModuleOverview(entry.Module);
        else
            OpenModuleDetail(entry.Module, entry.Target);
    }

    /// <summary>Permite abrir la primera coincidencia con Enter y enfocar con Ctrl+K.</summary>
    /// <param name="sender">Campo de búsqueda.</param>
    /// <param name="e">Tecla presionada.</param>
    private void GlobalSearch_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter)
            return;
        var match = _searchEntries.FirstOrDefault(item =>
            CultureInfo.CurrentCulture.CompareInfo.IndexOf(
                item.Label,
                _globalSearch.Text.Trim(),
                CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace
            ) >= 0
        );
        if (match is not null)
            ExecuteSearch(match);
        e.SuppressKeyPress = true;
    }

    /// <summary>Atiende Ctrl+K como acceso rápido a la búsqueda.</summary>
    /// <param name="msg">Mensaje nativo de teclado.</param>
    /// <param name="keyData">Combinación recibida.</param>
    /// <returns><see langword="true"/> cuando se consumió Ctrl+K.</returns>
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.K))
        {
            _globalSearch.Focus();
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    /// <summary>Determina si la sesión posee un permiso concreto.</summary>
    /// <param name="permission">Código de permiso.</param>
    /// <returns><see langword="true"/> cuando el permiso está presente.</returns>
    private bool Can(string permission) =>
        _session.Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);

    /// <summary>Determina si la sesión pertenece a un administrador.</summary>
    /// <returns><see langword="true"/> para administradores.</returns>
    private bool IsAdministrator() =>
        _session.Roles.Contains("ADMINISTRADOR", StringComparer.OrdinalIgnoreCase);

    /// <summary>Crea una página con desplazamiento vertical nativo.</summary>
    /// <returns>Panel de contenido sin barras artificiales.</returns>
    private static Panel CreateScrollablePage() =>
        new()
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = UiTheme.Background,
        };

    /// <summary>Crea la estructura del Home con alturas estables.</summary>
    /// <returns>Tabla vertical adaptable.</returns>
    private static TableLayoutPanel CreatePageLayout()
    {
        var page = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 710,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = UiTheme.Background,
            Padding = new Padding(28, 22, 28, 28),
            Margin = Padding.Empty,
        };
        page.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 166));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        return page;
    }

    /// <summary>Mantiene el ancho de una página aun cuando aparece el desplazamiento vertical.</summary>
    /// <param name="scroll">Contenedor desplazable.</param>
    /// <param name="page">Página interna.</param>
    private static void ResizePageWith(Panel scroll, Control page)
    {
        var minimumHeight = page.Height;
        void Resize()
        {
            page.Width = Math.Max(
                900,
                scroll.ClientSize.Width - SystemInformation.VerticalScrollBarWidth
            );
            page.Height = Math.Max(minimumHeight, scroll.ClientSize.Height);
        }
        scroll.Resize += (_, _) => Resize();
        Resize();
    }

    /// <summary>Crea una tarjeta blanca con borde suave compartida por el dashboard.</summary>
    /// <returns>Panel visual reutilizable.</returns>
    private static Panel CreateCard() =>
        new RoundedPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            BorderColor = Border,
            CornerRadius = 10,
            Padding = new Padding(18),
        };

    /// <summary>Crea un título de sección uniforme.</summary>
    /// <param name="text">Título visible.</param>
    /// <returns>Etiqueta resaltada.</returns>
    private static Label SectionTitle(string text) =>
        new()
        {
            Text = text,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = BrandGreen,
            TextAlign = ContentAlignment.MiddleLeft,
        };

    /// <summary>Crea un título con una explicación breve debajo.</summary>
    /// <param name="title">Título principal.</param>
    /// <param name="subtitle">Ayuda contextual.</param>
    /// <returns>Panel de encabezado.</returns>
    private static Control CreateSectionHeader(string title, string subtitle)
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        panel.Controls.Add(
            new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 28,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = BrandGreen,
            }
        );
        panel.Controls.Add(
            new Label
            {
                Text = subtitle,
                Dock = DockStyle.Bottom,
                Height = 22,
                Font = new Font("Segoe UI", 9F),
                ForeColor = MutedText,
            }
        );
        return panel;
    }

    /// <summary>Crea un mensaje neutral para una colección vacía.</summary>
    /// <param name="text">Explicación visible.</param>
    /// <returns>Etiqueta centrada.</returns>
    private static Control CreateEmptyState(string text) =>
        new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            ForeColor = MutedText,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 10F),
        };

    /// <summary>Convierte una fecha de servidor a la hora local conservando fechas ya locales.</summary>
    /// <param name="value">Fecha recibida desde la API.</param>
    /// <returns>Fecha local para mostrar al usuario.</returns>
    private static DateTime ToLocal(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Local => value,
            DateTimeKind.Utc => value.ToLocalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc).ToLocalTime(),
        };

    /// <summary>Obtiene hasta dos iniciales legibles del nombre del usuario.</summary>
    /// <param name="displayName">Nombre completo.</param>
    /// <returns>Iniciales en mayúsculas.</returns>
    private static string GetInitials(string displayName) =>
        string.Concat(
            displayName
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Take(2)
                .Select(item => char.ToUpperInvariant(item[0]))
        );

    /// <summary>Devuelve el texto correspondiente al idioma activo.</summary>
    /// <param name="spanish">Texto español.</param>
    /// <param name="english">Texto inglés.</param>
    /// <returns>Texto localizado.</returns>
    private static string L(string spanish, string english) =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en" ? english : spanish;

    private sealed record LoadResult<T>(T? Value, bool Failed)
        where T : class;

    private sealed record DashboardData(
        SalesSnapshotResponse? Sales,
        InventorySnapshotResponse? Inventory,
        PurchasingSnapshotResponse? Purchasing,
        TraceabilitySnapshotResponse? Traceability,
        LogisticsSnapshotResponse? Logistics,
        FinanceSnapshotResponse? Finance,
        IReadOnlyList<UserSummaryResponse>? Users,
        bool HasFailures
    )
    {
        internal static DashboardData Empty { get; } =
            new(null, null, null, null, null, null, null, false);
    }

    private sealed record MetricInfo(
        string Title,
        string Value,
        string Detail,
        DashboardModule Module
    );

    private sealed record DecisionInfo(
        string Title,
        string Detail,
        string Action,
        DashboardModule Module,
        DecisionTone Tone
    );

    private sealed record ActivityInfo(DateTime When, string Module, string Description);

    private sealed record FlowStageInfo(string Title, string Value, string Detail, string Target);

    private sealed record SearchEntry(string Label, DashboardModule Module, string? Target);

    private sealed record ModuleAction(
        string Text,
        string Target,
        int Width,
        ButtonTone Tone = ButtonTone.Primary
    );

    private sealed record ModulePresentation(
        string Title,
        string Description,
        string Guidance,
        IReadOnlyList<ModuleAction> Actions
    );
}

/// <summary>Reglas pequeñas y verificables que clasifican estados para el panel principal.</summary>
internal static class DashboardSummaryLogic
{
    /// <summary>Determina si un pedido comercial todavía requiere trabajo.</summary>
    /// <param name="statusCode">Estado persistido.</param>
    /// <returns><see langword="true"/> para borradores y pedidos confirmados.</returns>
    internal static bool IsOpenOrder(string? statusCode) =>
        statusCode is "BORRADOR" or "CONFIRMADO";

    /// <summary>Determina si una orden de compra todavía conserva trabajo pendiente.</summary>
    /// <param name="statusCode">Estado persistido.</param>
    /// <returns><see langword="true"/> mientras no esté cancelada ni cerrada.</returns>
    internal static bool IsOpenPurchaseOrder(string? statusCode) =>
        statusCode is "BORRADOR" or "PENDIENTE_APROBACION" or "APROBADA" or "RECIBIDA_PARCIAL";

    /// <summary>Determina si una orden puede recibir mercadería.</summary>
    /// <param name="statusCode">Estado persistido de la orden.</param>
    /// <returns><see langword="true"/> para órdenes aprobadas o recibidas parcialmente.</returns>
    internal static bool IsReceivablePurchaseOrder(string? statusCode) =>
        statusCode is "APROBADA" or "RECIBIDA_PARCIAL";

    /// <summary>Detecta una entrega prevista vencida que todavía conserva cantidades pendientes.</summary>
    /// <param name="statusCode">Estado persistido de la orden.</param>
    /// <param name="expectedDeliveryDate">Fecha comprometida por el proveedor.</param>
    /// <param name="hasPendingQuantity">Indica si al menos un renglón permanece pendiente.</param>
    /// <param name="today">Fecha local usada para comparar.</param>
    /// <returns><see langword="true"/> si la orden puede recibirse, tiene saldo y la fecha ya pasó.</returns>
    internal static bool IsOverduePurchaseOrder(
        string? statusCode,
        DateOnly? expectedDeliveryDate,
        bool hasPendingQuantity,
        DateOnly today
    ) =>
        IsReceivablePurchaseOrder(statusCode) && hasPendingQuantity && expectedDeliveryDate < today;

    /// <summary>Determina si una hoja participa del recorrido activo.</summary>
    /// <param name="statusCode">Estado persistido.</param>
    /// <returns><see langword="true"/> para hojas planificadas, despachadas o pausadas.</returns>
    internal static bool IsActiveRoute(string? statusCode) =>
        statusCode is "PLANIFICADA" or "DESPACHADA" or "PAUSADA";

    /// <summary>Determina si un documento o una parada alcanzaron un estado final positivo.</summary>
    /// <param name="statusCode">Estado persistido.</param>
    /// <returns><see langword="true"/> para estados completos o entregados.</returns>
    internal static bool IsCompleted(string? statusCode) =>
        statusCode
            is "COMPLETADA"
                or "COMPLETADO"
                or "ENTREGADA"
                or "ENTREGADO"
                or "DEVUELTO"
                or "CERRADA";

    /// <summary>Determina si una configuración comercial está activa para una fecha.</summary>
    /// <param name="statusCode">Estado persistido de la lista o promoción.</param>
    /// <param name="validFrom">Primer día de vigencia, si fue definido.</param>
    /// <param name="validUntil">Último día de vigencia, si fue definido.</param>
    /// <param name="date">Fecha local que se desea evaluar.</param>
    /// <returns><see langword="true"/> cuando está activa y la fecha pertenece al período inclusivo.</returns>
    internal static bool IsEffective(
        string? statusCode,
        DateOnly? validFrom,
        DateOnly? validUntil,
        DateOnly date
    ) =>
        string.Equals(statusCode, "ACTIVO", StringComparison.OrdinalIgnoreCase)
        && (validFrom is null || validFrom <= date)
        && (validUntil is null || validUntil >= date);

    /// <summary>Detecta cantidades incompatibles en una posición de inventario.</summary>
    /// <param name="physical">Cantidad física registrada.</param>
    /// <param name="reserved">Cantidad reservada por pedidos.</param>
    /// <param name="available">Cantidad disponible calculada.</param>
    /// <returns><see langword="true"/> si hay negativos o la reserva supera al stock físico.</returns>
    internal static bool IsInvalidStockBalance(
        decimal physical,
        decimal reserved,
        decimal available
    ) => physical < 0 || reserved < 0 || available < 0 || reserved > physical;

    /// <summary>Determina si el estado actual de un activo requiere intervención.</summary>
    /// <param name="statusCode">Estado de disponibilidad persistido.</param>
    /// <param name="conditionCode">Condición física persistida.</param>
    /// <returns><see langword="true"/> si está bloqueado, dañado, en revisión o mantenimiento.</returns>
    internal static bool NeedsAssetAttention(string? statusCode, string? conditionCode) =>
        statusCode == "BLOQUEADO"
        || conditionCode is "DANADO" or "EN_REVISION" or "EN_MANTENIMIENTO";

    /// <summary>Detecta contenido positivo sin los datos mínimos para reconstruir su origen.</summary>
    /// <param name="quantity">Cantidad actual almacenada.</param>
    /// <param name="productId">Producto contenido, cuando existe.</param>
    /// <param name="lotId">Lote de origen, cuando existe.</param>
    /// <returns><see langword="true"/> para cantidades negativas o contenido positivo sin producto o lote.</returns>
    internal static bool IsTraceabilityContentInconsistent(
        decimal? quantity,
        long? productId,
        long? lotId
    ) => quantity < 0 || quantity > 0 && (productId is null || lotId is null);

    /// <summary>Determina si corresponde realizar la revisión programada de un mantenimiento terminado.</summary>
    /// <param name="statusCode">Estado persistido del mantenimiento.</param>
    /// <param name="nextReviewDate">Fecha de la próxima revisión.</param>
    /// <param name="today">Fecha local de comparación.</param>
    /// <returns><see langword="true"/> cuando la revisión está prevista para hoy o una fecha anterior.</returns>
    internal static bool IsMaintenanceReviewDue(
        string? statusCode,
        DateOnly? nextReviewDate,
        DateOnly today
    ) => statusCode == "COMPLETADO" && nextReviewDate <= today;

    /// <summary>Detecta un préstamo cuya devolución prevista ya pasó y todavía no fue registrada.</summary>
    /// <param name="statusCode">Estado actual del préstamo.</param>
    /// <param name="expectedReturnUtc">Fecha y hora prevista de devolución.</param>
    /// <param name="actualReturnUtc">Fecha y hora real de devolución.</param>
    /// <param name="today">Fecha local de comparación.</param>
    /// <returns><see langword="true"/> cuando la devolución está vencida.</returns>
    internal static bool IsOverdueLoan(
        string? statusCode,
        DateTime? expectedReturnUtc,
        DateTime? actualReturnUtc,
        DateTime today
    ) =>
        actualReturnUtc is null && !IsCompleted(statusCode) && expectedReturnUtc?.Date < today.Date;

    /// <summary>Presenta una fecha reciente sin ocultar su hora real.</summary>
    /// <param name="eventTime">Fecha local del evento.</param>
    /// <param name="now">Fecha local actual.</param>
    /// <returns>Texto relativo para hoy o fecha y hora para días anteriores.</returns>
    internal static string FormatRelativeTime(DateTime eventTime, DateTime now)
    {
        if (eventTime.Date == now.Date)
            return eventTime.ToString("HH:mm", CultureInfo.CurrentCulture);
        if (eventTime.Date == now.Date.AddDays(-1))
            return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en"
                ? $"Yesterday {eventTime:HH:mm}"
                : $"Ayer {eventTime:HH:mm}";
        return eventTime.ToString("dd/MM HH:mm", CultureInfo.CurrentCulture);
    }
}

/// <summary>Identifica las áreas principales visibles en la barra lateral.</summary>
internal enum DashboardModule
{
    Home,
    Commercial,
    Inventory,
    Purchasing,
    Traceability,
    Logistics,
    Finance,
    Security,
    Configuration,
}

/// <summary>Clasifica visualmente asuntos que requieren una decisión.</summary>
internal enum DecisionTone
{
    Neutral,
    Warning,
    Danger,
}

/// <summary>Botón lateral dibujado de forma estable sin fuentes de iconos externas.</summary>
internal sealed class SidebarButton : Button
{
    private bool _selected;
    private bool _hovered;

    /// <summary>Inicializa el botón para un módulo concreto.</summary>
    /// <param name="module">Módulo que determina su icono nativo.</param>
    internal SidebarButton(DashboardModule module)
    {
        Module = module;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = Color.Transparent;
        ForeColor = Color.White;
        Cursor = Cursors.Hand;
        TabStop = true;
        SetStyle(
            ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer,
            true
        );
    }

    /// <summary>Módulo representado por el botón.</summary>
    internal DashboardModule Module { get; }

    /// <summary>Indica si el módulo es el destino activo.</summary>
    [System.ComponentModel.Browsable(false)]
    [System.ComponentModel.DesignerSerializationVisibility(
        System.ComponentModel.DesignerSerializationVisibility.Hidden
    )]
    internal bool Selected
    {
        get => _selected;
        set
        {
            if (_selected == value)
                return;
            _selected = value;
            Invalidate();
        }
    }

    /// <inheritdoc />
    protected override void OnMouseEnter(EventArgs e)
    {
        _hovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    /// <inheritdoc />
    protected override void OnMouseLeave(EventArgs e)
    {
        _hovered = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        var background =
            !Enabled ? Color.FromArgb(5, 71, 51)
            : Selected ? Color.FromArgb(25, 111, 79)
            : _hovered ? Color.FromArgb(13, 91, 65)
            : Color.FromArgb(5, 78, 55);
        using var brush = new SolidBrush(background);
        e.Graphics.FillRectangle(brush, ClientRectangle);
        if (Selected)
        {
            using var accent = new SolidBrush(Color.FromArgb(239, 174, 25));
            e.Graphics.FillRectangle(accent, 0, 5, 4, Height - 10);
        }

        var iconColor = Enabled ? Color.White : Color.FromArgb(125, 158, 145);
        DrawIcon(e.Graphics, new Rectangle(18, 14, 20, 20), iconColor, Module);
        TextRenderer.DrawText(
            e.Graphics,
            Text,
            Font,
            new Rectangle(50, 0, Width - 58, Height),
            Enabled ? Color.White : Color.FromArgb(145, 174, 162),
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis
        );
        if (Focused && ShowFocusCues)
            ControlPaint.DrawFocusRectangle(
                e.Graphics,
                new Rectangle(8, 5, Width - 16, Height - 10)
            );
    }

    /// <summary>Dibuja símbolos geométricos simples para evitar glifos incompatibles.</summary>
    /// <param name="graphics">Superficie de dibujo.</param>
    /// <param name="bounds">Área disponible.</param>
    /// <param name="color">Color del icono.</param>
    /// <param name="module">Módulo representado.</param>
    private static void DrawIcon(
        Graphics graphics,
        Rectangle bounds,
        Color color,
        DashboardModule module
    )
    {
        using var pen = new Pen(color, 1.9F)
        {
            StartCap = System.Drawing.Drawing2D.LineCap.Round,
            EndCap = System.Drawing.Drawing2D.LineCap.Round,
            LineJoin = System.Drawing.Drawing2D.LineJoin.Round,
        };
        var x = bounds.X;
        var y = bounds.Y;
        switch (module)
        {
            case DashboardModule.Home:
                graphics.DrawLine(pen, x + 2, y + 9, x + 10, y + 2);
                graphics.DrawLine(pen, x + 10, y + 2, x + 18, y + 9);
                graphics.DrawRectangle(pen, x + 4, y + 9, 12, 9);
                break;
            case DashboardModule.Commercial:
                graphics.DrawEllipse(pen, x + 3, y + 2, 6, 6);
                graphics.DrawEllipse(pen, x + 11, y + 4, 6, 6);
                graphics.DrawArc(pen, x + 1, y + 9, 10, 9, 180, 180);
                graphics.DrawArc(pen, x + 9, y + 10, 10, 8, 180, 180);
                break;
            case DashboardModule.Inventory:
                graphics.DrawRectangle(pen, x + 2, y + 5, 16, 12);
                graphics.DrawLine(pen, x + 2, y + 9, x + 18, y + 9);
                graphics.DrawLine(pen, x + 10, y + 9, x + 10, y + 17);
                break;
            case DashboardModule.Purchasing:
                graphics.DrawLine(pen, x + 1, y + 3, x + 4, y + 3);
                graphics.DrawLine(pen, x + 4, y + 3, x + 7, y + 13);
                graphics.DrawLines(
                    pen,
                    [
                        new Point(x + 6, y + 6),
                        new Point(x + 18, y + 6),
                        new Point(x + 16, y + 13),
                        new Point(x + 7, y + 13),
                    ]
                );
                graphics.DrawEllipse(pen, x + 7, y + 15, 3, 3);
                graphics.DrawEllipse(pen, x + 14, y + 15, 3, 3);
                break;
            case DashboardModule.Traceability:
                graphics.DrawLine(pen, x + 4, y + 4, x + 4, y + 16);
                graphics.DrawEllipse(pen, x + 2, y + 2, 4, 4);
                graphics.DrawEllipse(pen, x + 2, y + 8, 4, 4);
                graphics.DrawEllipse(pen, x + 2, y + 14, 4, 4);
                graphics.DrawLine(pen, x + 8, y + 4, x + 18, y + 4);
                graphics.DrawLine(pen, x + 8, y + 10, x + 15, y + 10);
                graphics.DrawLine(pen, x + 8, y + 16, x + 18, y + 16);
                graphics.DrawLine(pen, x + 16, y + 2, x + 18, y + 4);
                graphics.DrawLine(pen, x + 16, y + 6, x + 18, y + 4);
                break;
            case DashboardModule.Logistics:
                graphics.DrawRectangle(pen, x + 1, y + 6, 11, 8);
                graphics.DrawRectangle(pen, x + 12, y + 9, 6, 5);
                graphics.DrawEllipse(pen, x + 3, y + 14, 4, 4);
                graphics.DrawEllipse(pen, x + 13, y + 14, 4, 4);
                break;
            case DashboardModule.Finance:
                graphics.DrawRectangle(pen, x + 2, y + 4, 16, 12);
                graphics.DrawLine(pen, x + 2, y + 8, x + 18, y + 8);
                graphics.DrawEllipse(pen, x + 13, y + 11, 2, 2);
                break;
            case DashboardModule.Security:
                graphics.DrawArc(pen, x + 5, y + 1, 10, 13, 180, 180);
                graphics.DrawRectangle(pen, x + 3, y + 8, 14, 11);
                graphics.DrawEllipse(pen, x + 9, y + 12, 2, 2);
                graphics.DrawLine(pen, x + 10, y + 14, x + 10, y + 17);
                break;
            case DashboardModule.Configuration:
                graphics.DrawLine(pen, x + 1, y + 4, x + 19, y + 4);
                graphics.DrawLine(pen, x + 1, y + 10, x + 19, y + 10);
                graphics.DrawLine(pen, x + 1, y + 16, x + 19, y + 16);
                graphics.DrawEllipse(pen, x + 5, y + 1, 6, 6);
                graphics.DrawEllipse(pen, x + 12, y + 7, 6, 6);
                graphics.DrawEllipse(pen, x + 3, y + 13, 6, 6);
                break;
        }
    }
}

/// <summary>Panel con borde y esquinas redondeadas dibujadas con GDI+ nativo.</summary>
internal sealed class RoundedPanel : Panel
{
    /// <summary>Inicializa doble búfer para evitar parpadeos.</summary>
    internal RoundedPanel()
    {
        SetStyle(
            ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw,
            true
        );
    }

    /// <summary>Color del borde exterior.</summary>
    [System.ComponentModel.Browsable(false)]
    [System.ComponentModel.DesignerSerializationVisibility(
        System.ComponentModel.DesignerSerializationVisibility.Hidden
    )]
    internal Color BorderColor { get; set; } = Color.LightGray;

    /// <summary>Radio de las esquinas en píxeles lógicos.</summary>
    [System.ComponentModel.Browsable(false)]
    [System.ComponentModel.DesignerSerializationVisibility(
        System.ComponentModel.DesignerSerializationVisibility.Hidden
    )]
    internal int CornerRadius { get; set; } = 10;

    /// <inheritdoc />
    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        UpdateRegion();
    }

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var path = CreatePath(new Rectangle(0, 0, Width - 1, Height - 1), CornerRadius);
        using var pen = new Pen(BorderColor);
        e.Graphics.DrawPath(pen, path);
    }

    /// <summary>Ajusta la región clickeable al rectángulo redondeado.</summary>
    private void UpdateRegion()
    {
        if (Width <= 1 || Height <= 1)
            return;
        using var path = CreatePath(new Rectangle(0, 0, Width, Height), CornerRadius);
        Region?.Dispose();
        Region = new Region(path);
    }

    /// <summary>Crea un rectángulo redondeado sin dependencias externas.</summary>
    /// <param name="bounds">Límites del panel.</param>
    /// <param name="radius">Radio solicitado.</param>
    /// <returns>Ruta cerrada lista para dibujar o recortar.</returns>
    private static System.Drawing.Drawing2D.GraphicsPath CreatePath(Rectangle bounds, int radius)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        var diameter = Math.Max(2, radius * 2);
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}

/// <summary>Dibuja el isotipo de OxiTigre sin depender de un archivo externo.</summary>
internal sealed class BrandMark : Control
{
    /// <summary>Activa doble búfer y oculta interacción.</summary>
    internal BrandMark()
    {
        SetStyle(
            ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw,
            true
        );
        TabStop = false;
        AccessibleName = "OxiTigre";
    }

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        var scale = Math.Min(Width, Height) / 64F;
        var offsetX = (Width - (64F * scale)) / 2F;
        var offsetY = (Height - (64F * scale)) / 2F;
        var state = e.Graphics.Save();
        e.Graphics.TranslateTransform(offsetX, offsetY);
        e.Graphics.ScaleTransform(scale, scale);

        using var pen = new Pen(Color.FromArgb(242, 183, 44), 3.5F)
        {
            StartCap = System.Drawing.Drawing2D.LineCap.Round,
            EndCap = System.Drawing.Drawing2D.LineCap.Round,
            LineJoin = System.Drawing.Drawing2D.LineJoin.Round,
        };

        e.Graphics.DrawLines(pen, [new PointF(14, 21), new PointF(8, 12), new PointF(21, 16)]);
        e.Graphics.DrawLines(pen, [new PointF(50, 21), new PointF(56, 12), new PointF(43, 16)]);

        using var head = new System.Drawing.Drawing2D.GraphicsPath();
        head.StartFigure();
        head.AddBezier(13, 24, 15, 13, 23, 8, 32, 8);
        head.AddBezier(32, 8, 41, 8, 49, 13, 51, 24);
        head.AddLine(51, 24, 51, 36);
        head.AddBezier(51, 36, 51, 48, 42, 56, 32, 56);
        head.AddBezier(32, 56, 22, 56, 13, 48, 13, 36);
        head.CloseFigure();
        e.Graphics.DrawPath(pen, head);

        e.Graphics.DrawLines(pen, [new PointF(23, 24), new PointF(29, 28), new PointF(23, 31)]);
        e.Graphics.DrawLines(pen, [new PointF(41, 24), new PointF(35, 28), new PointF(41, 31)]);
        e.Graphics.DrawLines(pen, [new PointF(27, 40), new PointF(32, 44), new PointF(37, 40)]);
        e.Graphics.DrawLine(pen, 32, 44, 32, 49);
        e.Graphics.DrawLine(pen, 20, 37, 12, 39);
        e.Graphics.DrawLine(pen, 44, 37, 52, 39);
        e.Graphics.Restore(state);
    }
}

/// <summary>Dibuja las iniciales del usuario en el encabezado.</summary>
internal sealed class InitialsBadge(string initials) : Control
{
    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var background = new SolidBrush(Color.FromArgb(232, 245, 237));
        using var text = new SolidBrush(Color.FromArgb(5, 78, 55));
        e.Graphics.FillEllipse(background, ClientRectangle);
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
        };
        using var font = new Font("Segoe UI", 9F, FontStyle.Bold);
        e.Graphics.DrawString(initials, font, text, ClientRectangle, format);
    }
}

/// <summary>Muestra una etapa del flujo con jerarquía tipográfica y navegación accesible.</summary>
internal sealed class FlowMetricButton : Button
{
    private readonly string _title;
    private readonly string _value;
    private readonly string _detail;
    private bool _hovered;

    /// <summary>Inicializa una tarjeta clickeable del flujo operativo.</summary>
    /// <param name="title">Nombre de la etapa.</param>
    /// <param name="value">Valor principal.</param>
    /// <param name="detail">Descripción breve del valor.</param>
    internal FlowMetricButton(string title, string value, string detail)
    {
        _title = title;
        _value = value;
        _detail = detail;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = Color.White;
        Cursor = Cursors.Hand;
        TabStop = true;
        SetStyle(
            ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw,
            true
        );
    }

    /// <inheritdoc />
    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _hovered = true;
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hovered = false;
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(_hovered ? Color.FromArgb(232, 245, 237) : Color.White);
        using var border = new Pen(Color.FromArgb(218, 226, 221));
        using var titleFont = new Font("Segoe UI", 9F, FontStyle.Bold);
        using var valueFont = new Font("Segoe UI", 17F, FontStyle.Bold);
        using var detailFont = new Font("Segoe UI", 8.5F);
        e.Graphics.DrawRectangle(border, 0, 0, Math.Max(0, Width - 1), Math.Max(0, Height - 1));

        TextRenderer.DrawText(
            e.Graphics,
            _title,
            titleFont,
            new Rectangle(10, 7, Math.Max(0, Width - 20), 20),
            Color.FromArgb(71, 85, 105),
            TextFormatFlags.HorizontalCenter
                | TextFormatFlags.EndEllipsis
                | TextFormatFlags.SingleLine
        );
        TextRenderer.DrawText(
            e.Graphics,
            _value,
            valueFont,
            new Rectangle(10, 25, Math.Max(0, Width - 20), 30),
            Color.FromArgb(5, 78, 55),
            TextFormatFlags.HorizontalCenter
                | TextFormatFlags.VerticalCenter
                | TextFormatFlags.SingleLine
        );
        TextRenderer.DrawText(
            e.Graphics,
            _detail,
            detailFont,
            new Rectangle(10, 56, Math.Max(0, Width - 20), Math.Max(0, Height - 62)),
            Color.FromArgb(100, 116, 139),
            TextFormatFlags.HorizontalCenter
                | TextFormatFlags.EndEllipsis
                | TextFormatFlags.SingleLine
        );

        if (Focused && ShowFocusCues)
            ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(ClientRectangle, -4, -4));
    }
}

/// <summary>Dibuja un aviso con contador sin depender de glifos de fuentes externas.</summary>
internal sealed class NotificationButton : Button
{
    private int _count;

    /// <summary>Inicializa el botón accesible de notificaciones.</summary>
    internal NotificationButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Cursor = Cursors.Hand;
        BackColor = Color.White;
        SetStyle(
            ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer,
            true
        );
    }

    /// <summary>Cantidad de asuntos detectados.</summary>
    [System.ComponentModel.Browsable(false)]
    [System.ComponentModel.DesignerSerializationVisibility(
        System.ComponentModel.DesignerSerializationVisibility.Hidden
    )]
    internal int Count
    {
        get => _count;
        set
        {
            _count = Math.Max(0, value);
            Invalidate();
        }
    }

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        var center = Width / 2;
        var bellLeft = center - 10;
        using var bell = new Pen(Color.FromArgb(71, 85, 105), 1.8F);
        e.Graphics.DrawArc(bell, bellLeft, 11, 20, 22, 200, 140);
        e.Graphics.DrawLine(bell, bellLeft + 1, 23, bellLeft - 2, 28);
        e.Graphics.DrawLine(bell, bellLeft - 2, 28, bellLeft + 22, 28);
        e.Graphics.DrawLine(bell, bellLeft + 22, 28, bellLeft + 19, 23);
        e.Graphics.DrawArc(bell, center - 4, 27, 8, 6, 0, 180);
        if (Count == 0)
            return;

        var badgeLeft = Math.Max(1, Width - 22);
        using var badge = new SolidBrush(Color.FromArgb(190, 35, 35));
        using var badgeFont = new Font("Segoe UI", 7F, FontStyle.Bold);
        e.Graphics.FillEllipse(badge, badgeLeft, 3, 20, 20);
        TextRenderer.DrawText(
            e.Graphics,
            Math.Min(Count, 99).ToString(CultureInfo.CurrentCulture),
            badgeFont,
            new Rectangle(badgeLeft, 3, 20, 20),
            Color.White,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
        );
    }
}

/// <summary>Dibuja el punto de una línea temporal.</summary>
internal sealed class TimelineDot : Control
{
    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var outer = new SolidBrush(Color.FromArgb(232, 245, 237));
        using var inner = new SolidBrush(Color.FromArgb(17, 104, 74));
        e.Graphics.FillEllipse(outer, 0, 0, Width - 1, Height - 1);
        e.Graphics.FillEllipse(inner, 6, 6, Math.Max(2, Width - 13), Math.Max(2, Height - 13));
    }
}
