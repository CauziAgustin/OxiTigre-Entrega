/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.WinForms.LogisticsManagementForm
Archivo: LogisticsManagementForm.cs | Versión: 1.2.0 | Fecha: 2026-08-28 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Opera domicilios, solicitudes, hojas, paradas, custodia, eventos y avisos logísticos.
Historial: 1.0.0 | 2026-08-26 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Transportistas, vehículos, patentes argentinas y asignación de rutas.
Historial: 1.1.1 | 2026-08-27 | FABRICA | Vista completa y guía operativa del ciclo de hojas de ruta.
Historial: 1.2.0 | 2026-08-28 | FABRICA | Agustin Omar Cauzi | Planificación desde pedidos y confirmación exacta de activos operados.
===============================================================================
*/
using System.Globalization;
using OxiTigre.ApiClient;
using OxiTigre.Contracts.Logistics;
using OxiTigre.Contracts.Security;
using OxiTigre.Domain.Logistics;

namespace OxiTigre.WinForms;

/// <summary>Presenta el circuito manual de Logística y conserva el historial de cada hoja.</summary>
internal sealed class LogisticsManagementForm : Form
{
    private readonly OxiTigreApiClient _apiClient;
    private readonly LoginResponse _session;
    private readonly DataGridView _addresses = Grid();
    private readonly DataGridView _requests = Grid(true);
    private readonly DataGridView _assets = Grid();
    private readonly DataGridView _routes = Grid();
    private readonly DataGridView _stops = Grid();
    private readonly DataGridView _events = Grid();
    private readonly DataGridView _notifications = Grid();
    private readonly DataGridView _transporters = Grid();
    private readonly DataGridView _vehicles = Grid();
    private readonly DataGridView _orderCandidates = Grid();
    private LogisticsSnapshotResponse? _snapshot;

    /// <summary>Inicializa la pantalla con acciones habilitadas según la sesión.</summary>
    /// <param name="apiClient">Cliente HTTP compartido.</param>
    /// <param name="session">Sesión autenticada y sus permisos.</param>
    internal LogisticsManagementForm(OxiTigreApiClient apiClient, LoginResponse session)
    {
        _apiClient = apiClient;
        _session = session;
        Text = Localization.Text("Logistics_Title");
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(1000, 650);
        StartPosition = FormStartPosition.CenterParent;
        UiTheme.Apply(this);

        var help = new Label
        {
            Dock = DockStyle.Top,
            Height = 48,
            Padding = new Padding(14, 14, 14, 8),
            BackColor = Color.FromArgb(228, 245, 233),
            ForeColor = UiTheme.Primary,
            Text = Localization.Text("Logistics_Help"),
        };
        var tabs = new TabControl { Dock = DockStyle.Fill };
        UiTheme.Tabs(tabs);
        tabs.TabPages.Add(AddressTab());
        tabs.TabPages.Add(OrderCandidatesTab());
        tabs.TabPages.Add(RequestTab());
        tabs.TabPages.Add(TransportTab());
        tabs.TabPages.Add(RouteTab());
        tabs.TabPages.Add(HistoryTab());
        Controls.Add(tabs);
        Controls.Add(help);

        _requests.SelectionChanged += (_, _) => ShowAssets();
        _routes.SelectionChanged += (_, _) => ShowStops();
        Shown += async (_, _) => await LoadAsync();
    }

    /// <summary>Construye la vista de pedidos que todavía requieren planificación.</summary>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private TabPage OrderCandidatesTab()
    {
        var page = new TabPage(Localization.Text("Logistics_OrderCandidates"));
        var buttons = Bar();
        var create = UiTheme.Button(Localization.Text("Logistics_CreateFromOrder"), 0, 0, 220);
        create.Enabled = Can("LOGISTICA.GESTIONAR");
        create.Click += async (_, _) => await CreateRequestFromOrderAsync();
        var refresh = UiTheme.Button(
            Localization.Text("Common_Refresh"),
            0,
            0,
            130,
            ButtonTone.Neutral
        );
        refresh.Click += async (_, _) => await LoadAsync();
        buttons.Controls.AddRange([create, refresh]);
        page.Controls.Add(
            UiTheme.Searchable(
                _orderCandidates,
                Localization.Text("Logistics_OrderCandidatesHelp"),
                Localization.Text("Logistics_OrderCandidates")
            )
        );
        page.Controls.Add(buttons);
        return page;
    }

    /// <summary>Construye la vista de domicilios, contactos y ventanas horarias.</summary>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private TabPage AddressTab()
    {
        var page = new TabPage(Localization.Text("Logistics_Addresses"));
        var buttons = Bar();
        var create = UiTheme.Button(Localization.Text("Logistics_NewAddress"), 0, 0, 160);
        create.Enabled = Can("LOGISTICA.GESTIONAR");
        create.Click += async (_, _) => await NewAddressAsync();
        var refresh = UiTheme.Button(Localization.Text("Common_Refresh"), 0, 0, 130);
        refresh.Click += async (_, _) => await LoadAsync();
        buttons.Controls.AddRange([create, refresh]);
        page.Controls.Add(
            UiTheme.Searchable(
                _addresses,
                Localization.Text("Logistics_AddressHelp"),
                Localization.Text("Logistics_Addresses")
            )
        );
        page.Controls.Add(buttons);
        return page;
    }

    /// <summary>Construye la vista de solicitudes logísticas pendientes.</summary>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private TabPage RequestTab()
    {
        var page = new TabPage(Localization.Text("Logistics_Requests"));
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 360,
        };
        var buttons = Bar();
        var create = UiTheme.Button(Localization.Text("Logistics_NewRequest"), 0, 0, 160);
        create.Enabled = Can("LOGISTICA.GESTIONAR");
        create.Click += async (_, _) => await NewRequestAsync();
        var route = UiTheme.Button(Localization.Text("Logistics_CreateRoute"), 0, 0, 170);
        route.Enabled = Can("LOGISTICA.GESTIONAR");
        route.Click += async (_, _) => await NewRouteAsync();
        buttons.Controls.AddRange([create, route]);
        split.Panel1.Controls.Add(
            UiTheme.Searchable(
                _requests,
                Localization.Text("Logistics_RequestHelp"),
                Localization.Text("Logistics_Requests")
            )
        );
        split.Panel1.Controls.Add(buttons);
        split.Panel2.Controls.Add(
            UiTheme.Searchable(
                _assets,
                Localization.Text("Logistics_CustodyHelp"),
                Localization.Text("Logistics_Custody")
            )
        );
        page.Controls.Add(split);
        return page;
    }

    /// <summary>Construye la vista de transportistas y vehículos.</summary>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private TabPage TransportTab()
    {
        var page = new TabPage(Localization.Text("Logistics_Transport"));
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 350,
        };
        var transporterButtons = Bar();
        var newTransporter = UiTheme.Button(
            Localization.Text("Logistics_NewTransporter"),
            0,
            0,
            170
        );
        newTransporter.Enabled = Can("LOGISTICA.GESTIONAR");
        newTransporter.Click += async (_, _) => await SaveTransporterAsync(null);
        var editTransporter = UiTheme.Button(
            Localization.Text("Logistics_EditTransporter"),
            0,
            0,
            170,
            ButtonTone.Neutral
        );
        editTransporter.Enabled = Can("LOGISTICA.GESTIONAR");
        editTransporter.Click += async (_, _) => await EditTransporterAsync();
        transporterButtons.Controls.AddRange([newTransporter, editTransporter]);
        split.Panel1.Controls.Add(
            UiTheme.Searchable(
                _transporters,
                Localization.Text("Logistics_TransporterHelp"),
                Localization.Text("Logistics_Transporters")
            )
        );
        split.Panel1.Controls.Add(transporterButtons);

        var vehicleButtons = Bar();
        var newVehicle = UiTheme.Button(Localization.Text("Logistics_NewVehicle"), 0, 0, 170);
        newVehicle.Enabled = Can("LOGISTICA.GESTIONAR");
        newVehicle.Click += async (_, _) => await SaveVehicleAsync(null);
        var editVehicle = UiTheme.Button(
            Localization.Text("Logistics_EditVehicle"),
            0,
            0,
            170,
            ButtonTone.Neutral
        );
        editVehicle.Enabled = Can("LOGISTICA.GESTIONAR");
        editVehicle.Click += async (_, _) => await EditVehicleAsync();
        vehicleButtons.Controls.AddRange([newVehicle, editVehicle]);
        split.Panel2.Controls.Add(
            UiTheme.Searchable(
                _vehicles,
                Localization.Text("Logistics_VehicleHelp"),
                Localization.Text("Logistics_Vehicles")
            )
        );
        split.Panel2.Controls.Add(vehicleButtons);
        page.Controls.Add(split);
        return page;
    }

    /// <summary>Construye la vista de hojas de ruta y sus paradas.</summary>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private TabPage RouteTab()
    {
        var page = new TabPage(Localization.Text("Logistics_Routes"));
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 380,
        };
        var buttons = Bar();
        var assign = UiTheme.Button(Localization.Text("Logistics_AssignOffer"), 0, 0, 155);
        assign.Enabled = Can("LOGISTICA.GESTIONAR");
        assign.Click += async (_, _) => await AssignRouteAsync();
        var editOffer = UiTheme.Button(
            Localization.Text("Logistics_EditOffer"),
            0,
            0,
            145,
            ButtonTone.Neutral
        );
        editOffer.Enabled = Can("LOGISTICA.GESTIONAR");
        editOffer.Click += async (_, _) => await EditRouteOfferAsync();
        var dispatch = UiTheme.Button(Localization.Text("Logistics_Dispatch"), 0, 0, 150);
        dispatch.Enabled = Can("LOGISTICA.DESPACHAR");
        dispatch.Click += async (_, _) => await ChangeRouteAsync(false);
        var cancel = UiTheme.Button(
            Localization.Text("Common_Cancel"),
            0,
            0,
            140,
            ButtonTone.Danger
        );
        cancel.Enabled = Can("LOGISTICA.GESTIONAR");
        cancel.Click += async (_, _) => await ChangeRouteAsync(true);
        var complete = UiTheme.Button(Localization.Text("Logistics_CompleteStop"), 0, 0, 180);
        complete.Enabled = Can("LOGISTICA.DESPACHAR");
        complete.Click += async (_, _) => await CompleteStopAsync();
        buttons.Controls.AddRange([assign, editOffer, dispatch, cancel, complete]);

        var routeHeader = new Panel { Dock = DockStyle.Top, Height = 88 };
        var stateGuide = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 36,
            Padding = new Padding(12, 8, 8, 4),
            BackColor = Color.FromArgb(230, 245, 235),
            ForeColor = UiTheme.Primary,
            Text = Localization.Text("Logistics_RouteStatusGuide"),
        };
        routeHeader.Controls.Add(stateGuide);
        routeHeader.Controls.Add(buttons);
        split.Panel1.Controls.Add(
            UiTheme.Searchable(
                _routes,
                Localization.Text("Logistics_RouteHelp"),
                Localization.Text("Logistics_Routes")
            )
        );
        split.Panel1.Controls.Add(routeHeader);
        split.Panel2.Controls.Add(
            UiTheme.Searchable(
                _stops,
                Localization.Text("Logistics_StopHelp"),
                Localization.Text("Logistics_Stops")
            )
        );
        page.Controls.Add(split);
        return page;
    }

    /// <summary>Construye la vista histórica de recorridos, eventos y avisos.</summary>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private TabPage HistoryTab()
    {
        var page = new TabPage(Localization.Text("Logistics_History"));
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 360,
        };
        split.Panel1.Controls.Add(
            UiTheme.Searchable(
                _events,
                Localization.Text("Logistics_EventHelp"),
                Localization.Text("Logistics_Events")
            )
        );
        split.Panel2.Controls.Add(
            UiTheme.Searchable(
                _notifications,
                Localization.Text("Logistics_NotificationHelp"),
                Localization.Text("Logistics_Notifications")
            )
        );
        page.Controls.Add(split);
        return page;
    }

    /// <summary>Recupera solicitudes, rutas, custodia y avisos; oculta identificadores técnicos y localiza las grillas.</summary>
    /// <returns>Tarea que termina tras enlazar el estado o notificar un error de la API.</returns>
    private async Task LoadAsync()
    {
        await RunAsync(async () =>
        {
            _snapshot = await _apiClient.GetLogisticsAsync(_session.Token);
            _addresses.DataSource = _snapshot.Addresses.ToList();
            _requests.DataSource = _snapshot.Requests.ToList();
            _routes.DataSource = _snapshot.Routes.ToList();
            _events.DataSource = _snapshot.Events.ToList();
            _notifications.DataSource = _snapshot.Notifications.ToList();
            _transporters.DataSource = _snapshot.Transporters.ToList();
            _vehicles.DataSource = _snapshot.Vehicles.ToList();
            _orderCandidates.DataSource = _snapshot.OrderCandidates.ToList();
            ShowAssets();
            ShowStops();
            HideTechnicalColumns(
                _addresses,
                "AddressId",
                "ClientId",
                "RowVersion",
                "Latitude",
                "Longitude"
            );
            HideTechnicalColumns(
                _requests,
                "RequestId",
                "ClientId",
                "AddressId",
                "OrderId",
                "RowVersion",
                "DestinationWarehouseId"
            );
            HideTechnicalColumns(_routes, "RouteId", "TransporterId", "VehicleId", "RowVersion");
            HideTechnicalColumns(_stops, "StopId", "RouteId", "RequestId", "RowVersion");
            HideTechnicalColumns(
                _events,
                "EventId",
                "RequestId",
                "RouteId",
                "StopId",
                "UserId",
                "SessionId"
            );
            HideTechnicalColumns(_notifications, "NotificationId", "RequestId", "RouteId");
            HideTechnicalColumns(
                _transporters,
                "TransporterId",
                "UserId",
                "RowVersion",
                "Observation"
            );
            HideTechnicalColumns(
                _vehicles,
                "VehicleId",
                "OwnerTransporterId",
                "RowVersion",
                "Observation"
            );
            HideTechnicalColumns(_orderCandidates, "OrderId", "ClientId", "ServiceType");

            LocalizeColumns(_addresses);
            LocalizeColumns(_requests);
            LocalizeColumns(_routes);
            LocalizeColumns(_events);
            LocalizeColumns(_notifications);
            LocalizeColumns(_transporters);
            LocalizeColumns(_vehicles);
            LocalizeColumns(_orderCandidates);
        });
    }

    /// <summary>Crea una solicitud desde el pedido seleccionado y reutiliza sus activos vinculados.</summary>
    /// <returns>Tarea que finaliza cuando la API persiste y recarga la pantalla.</returns>
    private async Task CreateRequestFromOrderAsync()
    {
        if (
            _orderCandidates.CurrentRow?.DataBoundItem
            is not LogisticsOrderCandidateResponse candidate
        )
        {
            Warn(Localization.Text("Logistics_SelectOrderCandidate"));
            return;
        }

        var addresses =
            _snapshot
                ?.Addresses.Where(item =>
                    item.ClientId == candidate.ClientId && item.StatusCode == "ACTIVO"
                )
                .ToList()
            ?? [];
        if (addresses.Count == 0)
        {
            Warn(Localization.Text("Logistics_OrderNeedsAddress"));
            return;
        }

        var warehouseOptions = new List<KeyValuePair<string, string>>
        {
            new("0", Localization.Text("Sales_NotApplicable")),
        };
        warehouseOptions.AddRange(
            (_snapshot?.Warehouses ?? []).Select(item => new KeyValuePair<string, string>(
                item.WarehouseId.ToString(CultureInfo.InvariantCulture),
                $"{item.Code} · {item.Name}"
            ))
        );
        if (candidate.Operation == "RETIRO" && warehouseOptions.Count == 1)
        {
            Warn(Localization.Text("Logistics_PickupNeedsWarehouse"));
            return;
        }

        using var form = new SimpleEntryForm(
            Localization.Text("Logistics_CreateFromOrder"),
            new("order", Localization.Text("Sales_Order"), candidate.OrderCode, ReadOnly: true),
            new(
                "operation",
                Localization.Text("Logistics_Operation"),
                candidate.Operation,
                ReadOnly: true
            ),
            new(
                "payment",
                Localization.Text("Logistics_PaymentStatus"),
                candidate.PaymentStatus,
                ReadOnly: true
            ),
            new(
                "address",
                Localization.Text("Logistics_Address"),
                Options: addresses
                    .Select(item => new KeyValuePair<string, string>(
                        item.AddressId.ToString(CultureInfo.InvariantCulture),
                        $"{item.Name} · {item.Address}"
                    ))
                    .ToList(),
                Required: true
            ),
            new(
                "warehouse",
                Localization.Text("Logistics_DestinationWarehouse"),
                candidate.Operation == "RETIRO" ? warehouseOptions[1].Key : "0",
                warehouseOptions,
                Required: true
            ),
            new(
                "priority",
                Localization.Text("Common_Priority"),
                "NORMAL",
                Options: Options("NORMAL", "ALTA", "URGENTE", "EMERGENCIA"),
                Required: true
            ),
            new(
                "date",
                Localization.Text("Common_Date"),
                DateOnly
                    .FromDateTime(DateTime.Today)
                    .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Required: true
            ),
            new("from", Localization.Text("Logistics_FromTime")),
            new("to", Localization.Text("Logistics_ToTime")),
            new(
                "instructions",
                Localization.Text("Common_Instructions"),
                candidate.Summary,
                Required: true,
                Multiline: true,
                Help: Localization.Text("Logistics_InstructionsHelp")
            ),
            new("observation", Localization.Text("Common_Observation"), Multiline: true)
        );
        if (form.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var address = addresses.Single(item =>
            item.AddressId == long.Parse(form["address"], CultureInfo.InvariantCulture)
        );
        var warehouseId = long.Parse(form["warehouse"], CultureInfo.InvariantCulture);
        await RunAndReloadAsync(() =>
            _apiClient.CreateLogisticsRequestAsync(
                _session.Token,
                new(
                    candidate.ClientId,
                    address.AddressId,
                    candidate.OrderId,
                    candidate.ServiceType,
                    form["priority"],
                    DateOnly.ParseExact(form["date"], "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    Time(form["from"]),
                    Time(form["to"]),
                    form["instructions"],
                    Empty(form["observation"]),
                    [],
                    warehouseId == 0 ? null : warehouseId
                )
            )
        );
    }

    /// <summary>Inicia la creación de address.</summary>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private async Task NewAddressAsync()
    {
        if (_snapshot?.Clients.Count is not > 0)
        {
            Warn(Localization.Text("Logistics_NoClients"));
            return;
        }

        using var form = new SimpleEntryForm(
            Localization.Text("Logistics_NewAddress"),
            new(
                "client",
                Localization.Text("Common_Client"),
                Options: _snapshot
                    .Clients.Select(item => new KeyValuePair<string, string>(
                        item.ClientId.ToString(CultureInfo.InvariantCulture),
                        $"{item.Code} · {item.Name}"
                    ))
                    .ToList(),
                Required: true
            ),
            new("name", Localization.Text("Common_Name"), Required: true),
            new("address", Localization.Text("Common_Address"), Required: true),
            new("city", Localization.Text("Common_City")),
            new("province", Localization.Text("Common_Province")),
            new("postal", Localization.Text("Common_PostalCode")),
            new("contact", Localization.Text("Common_Contact"), Required: true),
            new("phone", Localization.Text("Common_Phone"), Required: true),
            new("email", Localization.Text("Common_Email")),
            new(
                "from",
                Localization.Text("Logistics_FromTime"),
                Help: "Formato HH:mm. Dejá ambos horarios vacíos si no hay restricción."
            ),
            new("to", Localization.Text("Logistics_ToTime")),
            new(
                "instructions",
                Localization.Text("Common_Instructions"),
                Required: true,
                Multiline: true,
                Help: Localization.Text("Logistics_InstructionsHelp")
            )
        );
        if (form.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        await RunAndReloadAsync(() =>
            _apiClient.SaveLogisticsAddressAsync(
                _session.Token,
                new(
                    long.Parse(form["client"], CultureInfo.InvariantCulture),
                    form["name"],
                    form["address"],
                    Empty(form["city"]),
                    Empty(form["province"]),
                    Empty(form["postal"]),
                    form["contact"],
                    form["phone"],
                    Empty(form["email"]),
                    Time(form["from"]),
                    Time(form["to"]),
                    form["instructions"]
                )
            )
        );
    }

    /// <summary>Inicia la creación de request.</summary>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private async Task NewRequestAsync()
    {
        if (
            _snapshot?.Addresses.Where(x => x.StatusCode == "ACTIVO").ToList()
            is not { Count: > 0 } addresses
        )
        {
            Warn(Localization.Text("Logistics_NoAddresses"));
            return;
        }

        using var form = new SimpleEntryForm(
            Localization.Text("Logistics_NewRequest"),
            new(
                "address",
                Localization.Text("Logistics_Address"),
                Options: addresses
                    .Select(item => new KeyValuePair<string, string>(
                        item.AddressId.ToString(CultureInfo.InvariantCulture),
                        $"{item.Client} · {item.Address}"
                    ))
                    .ToList(),
                Required: true
            ),
            new(
                "type",
                Localization.Text("Logistics_ServiceType"),
                "ENTREGA_PEDIDO",
                Options: Options(
                    "ENTREGA_PEDIDO",
                    "RETIRO_RECARGA",
                    "DEVOLUCION_CLIENTE",
                    "INTERCAMBIO_TEMPORAL",
                    "TRASLADO_SUCURSAL",
                    "URGENCIA"
                ),
                Required: true
            ),
            new(
                "warehouse",
                Localization.Text("Logistics_DestinationWarehouse"),
                "0",
                [
                    new("0", Localization.Text("Sales_NotApplicable")),
                    .. (_snapshot?.Warehouses ?? []).Select(item => new KeyValuePair<
                        string,
                        string
                    >(
                        item.WarehouseId.ToString(CultureInfo.InvariantCulture),
                        $"{item.Code} · {item.Name}"
                    )),
                ],
                Required: true
            ),
            new(
                "priority",
                Localization.Text("Common_Priority"),
                "NORMAL",
                Options: Options("NORMAL", "ALTA", "URGENTE", "EMERGENCIA"),
                Required: true
            ),
            new(
                "date",
                Localization.Text("Common_Date"),
                DateOnly
                    .FromDateTime(DateTime.Today)
                    .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Required: true
            ),
            new("from", Localization.Text("Logistics_FromTime")),
            new("to", Localization.Text("Logistics_ToTime")),
            new(
                "instructions",
                Localization.Text("Common_Instructions"),
                Required: true,
                Multiline: true,
                Help: Localization.Text("Logistics_InstructionsHelp")
            ),
            new("observation", Localization.Text("Common_Observation"), Multiline: true),
            new(
                "serial",
                Localization.Text("Logistics_AssetSerial"),
                Help: Localization.Text("Logistics_AssetHelp")
            ),
            new(
                "owner",
                Localization.Text("Logistics_Owner"),
                "CLIENTE",
                Options: Options("CLIENTE", "OXITIGRE")
            ),
            new(
                "role",
                Localization.Text("Logistics_Role"),
                "RETIRO_CLIENTE",
                Options: Options("RETIRO_CLIENTE", "PRESTAMO_TEMPORAL", "ENTREGA", "DEVOLUCION")
            ),
            new("product", Localization.Text("Common_Product")),
            new("quantity", Localization.Text("Logistics_Content"), Numeric: true),
            new("unit", Localization.Text("Common_Unit"), "kg"),
            new("condition", Localization.Text("Common_Condition"), "OPERATIVO"),
            new(
                "expected",
                Localization.Text("Sales_ExpectedReturn"),
                DateOnly
                    .FromDateTime(DateTime.Today.AddDays(7))
                    .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Help: Localization.Text("Logistics_DateHelp")
            )
        );
        if (form.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var address = addresses.Single(x =>
            x.AddressId == long.Parse(form["address"], CultureInfo.InvariantCulture)
        );
        var assets = string.IsNullOrWhiteSpace(form["serial"])
            ? []
            : new List<LogisticsRequestAssetRequest>
            {
                new(
                    null,
                    form["owner"],
                    form["role"],
                    form["serial"],
                    Empty(form["product"]),
                    decimal.TryParse(
                        form["quantity"],
                        NumberStyles.Number,
                        CultureInfo.CurrentCulture,
                        out var quantity
                    )
                        ? quantity
                        : null,
                    Empty(form["unit"]),
                    form["condition"],
                    null,
                    form["role"] == "PRESTAMO_TEMPORAL" ? Date(form["expected"]) : null
                ),
            };
        var warehouseId = long.Parse(form["warehouse"], CultureInfo.InvariantCulture);
        await RunAndReloadAsync(() =>
            _apiClient.CreateLogisticsRequestAsync(
                _session.Token,
                new(
                    address.ClientId,
                    address.AddressId,
                    null,
                    form["type"],
                    form["priority"],
                    DateOnly.ParseExact(form["date"], "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    Time(form["from"]),
                    Time(form["to"]),
                    form["instructions"],
                    Empty(form["observation"]),
                    assets,
                    warehouseId == 0 ? null : warehouseId
                )
            )
        );
    }

    /// <summary>Abre el editor correspondiente a transporter y conserva los cambios confirmados.</summary>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private async Task EditTransporterAsync()
    {
        if (_transporters.CurrentRow?.DataBoundItem is not TransporterResponse transporter)
        {
            Warn(Localization.Text("Logistics_SelectTransporter"));
            return;
        }
        await SaveTransporterAsync(transporter);
    }

    /// <summary>Valida y guarda transporter mediante la API.</summary>
    /// <param name="transporter">Transportista existente que se edita, o nulo para un alta.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private async Task SaveTransporterAsync(TransporterResponse? transporter)
    {
        if (_snapshot is null)
            return;
        var users = _snapshot
            .TransporterUsers.Where(user =>
                transporter is not null && user.UserId == transporter.UserId
                || _snapshot.Transporters.All(existing => existing.UserId != user.UserId)
            )
            .Select(user => new KeyValuePair<string, string>(
                user.UserId.ToString(CultureInfo.InvariantCulture),
                $"{user.UserName} · {user.FullName}"
            ))
            .ToList();
        if (users.Count == 0)
        {
            Warn(Localization.Text("Logistics_NoTransporterUsers"));
            return;
        }

        using var form = new SimpleEntryForm(
            Localization.Text(
                transporter is null ? "Logistics_NewTransporter" : "Logistics_EditTransporter"
            ),
            new(
                "user",
                Localization.Text("Logistics_User"),
                transporter?.UserId.ToString(CultureInfo.InvariantCulture),
                users,
                Required: true
            ),
            new(
                "relationship",
                Localization.Text("Logistics_Relationship"),
                transporter?.RelationshipType ?? "EMPRESA",
                Options("EMPRESA", "PARTICULAR"),
                Required: true
            ),
            new("document", Localization.Text("Logistics_Document"), transporter?.Document),
            new("phone", Localization.Text("Common_Phone"), transporter?.Phone),
            new(
                "license",
                Localization.Text("Logistics_License"),
                transporter?.License,
                Required: true
            ),
            new(
                "category",
                Localization.Text("Logistics_LicenseCategory"),
                transporter?.LicenseCategory
            ),
            new(
                "expiration",
                Localization.Text("Logistics_LicenseExpiration"),
                transporter?.LicenseExpiration?.ToString(
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture
                ),
                Help: Localization.Text("Logistics_DateHelp")
            ),
            new(
                "status",
                Localization.Text("Common_Status"),
                transporter?.StatusCode ?? "ACTIVO",
                Options: Options("ACTIVO", "INACTIVO"),
                Required: true
            ),
            new(
                "observation",
                Localization.Text("Common_Observation"),
                transporter?.Observation,
                Multiline: true
            )
        );
        if (form.ShowDialog(this) != DialogResult.OK)
            return;

        await RunAndReloadAsync(() =>
            _apiClient.SaveTransporterAsync(
                _session.Token,
                new SaveTransporterRequest(
                    transporter?.TransporterId,
                    long.Parse(form["user"], CultureInfo.InvariantCulture),
                    form["relationship"],
                    Empty(form["document"]),
                    Empty(form["phone"]),
                    form["license"],
                    Empty(form["category"]),
                    Date(form["expiration"]),
                    Empty(form["observation"]),
                    form["status"],
                    transporter?.RowVersion
                )
            )
        );
    }

    /// <summary>Abre el editor correspondiente a vehicle y conserva los cambios confirmados.</summary>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private async Task EditVehicleAsync()
    {
        if (_vehicles.CurrentRow?.DataBoundItem is not LogisticsVehicleResponse vehicle)
        {
            Warn(Localization.Text("Logistics_SelectVehicle"));
            return;
        }
        await SaveVehicleAsync(vehicle);
    }

    /// <summary>Valida y guarda vehicle mediante la API.</summary>
    /// <param name="vehicle">Vehículo existente que se edita, o nulo para un alta.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private async Task SaveVehicleAsync(LogisticsVehicleResponse? vehicle)
    {
        if (_snapshot is null)
            return;
        var owners = new List<KeyValuePair<string, string>>
        {
            new("0", Localization.Text("Logistics_CompanyVehicle")),
        };
        owners.AddRange(
            _snapshot
                .Transporters.Where(item =>
                    item.StatusCode == "ACTIVO" || item.TransporterId == vehicle?.OwnerTransporterId
                )
                .Select(item => new KeyValuePair<string, string>(
                    item.TransporterId.ToString(CultureInfo.InvariantCulture),
                    $"{item.Code} · {item.FullName}"
                ))
        );

        using var form = new SimpleEntryForm(
            Localization.Text(vehicle is null ? "Logistics_NewVehicle" : "Logistics_EditVehicle"),
            new(
                "plate",
                Localization.Text("Logistics_Plate"),
                vehicle?.Plate,
                Required: true,
                Help: Localization.Text("Logistics_PlateHelp"),
                Formatter: ArgentineLicensePlate.FormatInput
            ),
            new(
                "type",
                Localization.Text("Logistics_VehicleType"),
                vehicle?.VehicleType ?? "CAMIONETA",
                Options("CAMION", "CAMIONETA", "UTILITARIO", "AUTO", "OTRO"),
                Required: true
            ),
            new(
                "ownership",
                Localization.Text("Logistics_Ownership"),
                vehicle?.OwnershipType ?? "EMPRESA",
                Options("EMPRESA", "TRANSPORTISTA"),
                Required: true
            ),
            new(
                "owner",
                Localization.Text("Logistics_OwnerTransporter"),
                (vehicle?.OwnerTransporterId ?? 0).ToString(CultureInfo.InvariantCulture),
                owners,
                Required: true,
                Help: Localization.Text("Logistics_OwnerHelp")
            ),
            new("brand", Localization.Text("Logistics_Brand"), vehicle?.Brand),
            new("model", Localization.Text("Logistics_Model"), vehicle?.Model),
            new(
                "year",
                Localization.Text("Logistics_Year"),
                vehicle?.Year?.ToString(CultureInfo.InvariantCulture)
            ),
            new(
                "capacity",
                Localization.Text("Logistics_LoadCapacity"),
                vehicle?.LoadCapacityKg?.ToString(CultureInfo.CurrentCulture),
                Numeric: true
            ),
            new("policy", Localization.Text("Logistics_InsurancePolicy"), vehicle?.InsurancePolicy),
            new(
                "insurance",
                Localization.Text("Logistics_InsuranceExpiration"),
                vehicle?.InsuranceExpiration?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Help: Localization.Text("Logistics_DateHelp")
            ),
            new(
                "inspection",
                Localization.Text("Logistics_InspectionExpiration"),
                vehicle?.InspectionExpiration?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Help: Localization.Text("Logistics_DateHelp")
            ),
            new(
                "status",
                Localization.Text("Common_Status"),
                vehicle?.StatusCode ?? "ACTIVO",
                Options: Options("ACTIVO", "INACTIVO"),
                Required: true
            ),
            new(
                "observation",
                Localization.Text("Common_Observation"),
                vehicle?.Observation,
                Multiline: true
            )
        );
        if (form.ShowDialog(this) != DialogResult.OK)
            return;

        var ownerId = long.Parse(form["owner"], CultureInfo.InvariantCulture);
        await RunAndReloadAsync(() =>
            _apiClient.SaveLogisticsVehicleAsync(
                _session.Token,
                new SaveLogisticsVehicleRequest(
                    vehicle?.VehicleId,
                    ownerId == 0 ? null : ownerId,
                    form["plate"],
                    form["type"],
                    form["ownership"],
                    Empty(form["brand"]),
                    Empty(form["model"]),
                    short.TryParse(
                        form["year"],
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out var year
                    )
                        ? year
                        : null,
                    decimal.TryParse(
                        form["capacity"],
                        NumberStyles.Number,
                        CultureInfo.CurrentCulture,
                        out var capacity
                    )
                        ? capacity
                        : null,
                    Empty(form["policy"]),
                    Date(form["insurance"]),
                    Date(form["inspection"]),
                    Empty(form["observation"]),
                    form["status"],
                    vehicle?.RowVersion
                )
            )
        );
    }

    /// <summary>Forma una hoja con solicitudes pendientes, fecha, prioridad y asignación directa u oferta.</summary>
    /// <returns>Tarea que termina al cancelar el editor o registrar y recargar la hoja.</returns>
    private async Task NewRouteAsync()
    {
        var selected = _requests
            .SelectedRows.Cast<DataGridViewRow>()
            .Select(row => row.DataBoundItem as LogisticsRequestResponse)
            .Where(item => item?.StatusCode == "PENDIENTE")
            .Cast<LogisticsRequestResponse>()
            .DistinctBy(item => item.RequestId)
            .ToList();
        if (selected.Count == 0)
        {
            Warn(Localization.Text("Logistics_SelectRequests"));
            return;
        }

        var transporters =
            _snapshot?.Transporters.Where(item => item.StatusCode == "ACTIVO").ToList() ?? [];
        var vehicles =
            _snapshot?.Vehicles.Where(item => item.StatusCode == "ACTIVO").ToList() ?? [];
        var transporterOptions = transporters
            .Select(item => new KeyValuePair<string, string>(
                item.TransporterId.ToString(CultureInfo.InvariantCulture),
                $"{item.Code} · {item.FullName}"
            ))
            .ToList();
        var vehicleOptions = vehicles
            .Select(item => new KeyValuePair<string, string>(
                item.VehicleId.ToString(CultureInfo.InvariantCulture),
                $"{item.Plate} · {item.VehicleType}"
            ))
            .ToList();
        transporterOptions.Add(new("0", "—"));
        vehicleOptions.Add(new("0", "—"));

        using var form = new SimpleEntryForm(
            Localization.Text("Logistics_CreateRoute"),
            new(
                "date",
                Localization.Text("Common_Date"),
                DateOnly
                    .FromDateTime(DateTime.Today)
                    .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Required: true
            ),
            new(
                "type",
                Localization.Text("Logistics_RouteType"),
                selected.Count == 1 && selected[0].Priority is "URGENTE" or "EMERGENCIA"
                    ? "URGENTE"
                    : "NORMAL",
                Options: Options("NORMAL", "URGENTE"),
                Required: true
            ),
            new(
                "assignment",
                Localization.Text("Logistics_AssignmentType"),
                "DIRECTA",
                Options:
                [
                    new("DIRECTA", Localization.Text("Logistics_DirectAssignment")),
                    new("OFERTA", Localization.Text("Logistics_RouteOffer")),
                ],
                Required: true,
                Help: Localization.Text("Logistics_AssignmentHelp")
            ),
            new("transporter", Localization.Text("Logistics_Driver"), Options: transporterOptions),
            new("vehicle", Localization.Text("Logistics_Vehicle"), Options: vehicleOptions),
            new(
                "observation",
                Localization.Text("Common_Observation"),
                Required: true,
                Multiline: true
            )
        );
        if (form.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var transporterId = long.Parse(form["transporter"], CultureInfo.InvariantCulture);
        var vehicleId = long.Parse(form["vehicle"], CultureInfo.InvariantCulture);
        if (form["assignment"] == "DIRECTA" && (transporterId == 0 || vehicleId == 0))
        {
            Warn(Localization.Text("Logistics_RouteNeedsTransport"));
            return;
        }

        await RunAndReloadAsync(() =>
            _apiClient.CreateRouteAsync(
                _session.Token,
                new(
                    DateOnly.ParseExact(form["date"], "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    form["type"],
                    form["assignment"],
                    form["assignment"] == "DIRECTA" ? transporterId : null,
                    form["assignment"] == "DIRECTA" ? vehicleId : null,
                    form["observation"],
                    selected
                        .Select(
                            (item, index) =>
                                new RouteRequestItem((short)(index + 1), item.RequestId)
                        )
                        .ToList()
                )
            )
        );
    }

    /// <summary>Asigna desde la oficina una hoja publicada.</summary>
    /// <returns>Tarea que finaliza al actualizar la pantalla.</returns>
    private async Task AssignRouteAsync()
    {
        if (
            _routes.CurrentRow?.DataBoundItem
            is not RouteSheetResponse { StatusCode: "OFRECIDA" } route
        )
        {
            Warn(Localization.Text("Logistics_SelectOffer"));
            return;
        }

        var transporters =
            _snapshot?.Transporters.Where(item => item.StatusCode == "ACTIVO").ToList() ?? [];
        var vehicles =
            _snapshot?.Vehicles.Where(item => item.StatusCode == "ACTIVO").ToList() ?? [];
        if (transporters.Count == 0 || vehicles.Count == 0)
        {
            Warn(Localization.Text("Logistics_RouteNeedsTransport"));
            return;
        }

        using var form = new SimpleEntryForm(
            Localization.Text("Logistics_AssignOffer"),
            new(
                "transporter",
                Localization.Text("Logistics_Driver"),
                Options: transporters
                    .Select(item => new KeyValuePair<string, string>(
                        item.TransporterId.ToString(CultureInfo.InvariantCulture),
                        $"{item.Code} · {item.FullName}"
                    ))
                    .ToList(),
                Required: true
            ),
            new(
                "vehicle",
                Localization.Text("Logistics_Vehicle"),
                Options: vehicles
                    .Select(item => new KeyValuePair<string, string>(
                        item.VehicleId.ToString(CultureInfo.InvariantCulture),
                        $"{item.Plate} · {item.VehicleType}"
                    ))
                    .ToList(),
                Required: true
            )
        );
        if (form.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        await RunAndReloadAsync(() =>
            _apiClient.AssignRouteAsync(
                _session.Token,
                route.RouteId,
                new(
                    long.Parse(form["transporter"], CultureInfo.InvariantCulture),
                    long.Parse(form["vehicle"], CultureInfo.InvariantCulture),
                    route.RowVersion
                )
            )
        );
    }

    /// <summary>Modifica la agenda descriptiva de una oferta todavía libre.</summary>
    /// <returns>Tarea que finaliza al actualizar la pantalla.</returns>
    private async Task EditRouteOfferAsync()
    {
        if (
            _routes.CurrentRow?.DataBoundItem
            is not RouteSheetResponse { StatusCode: "OFRECIDA" } route
        )
        {
            Warn(Localization.Text("Logistics_SelectOffer"));
            return;
        }

        using var form = new SimpleEntryForm(
            Localization.Text("Logistics_EditOffer"),
            new(
                "date",
                Localization.Text("Common_Date"),
                route.RouteDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Required: true
            ),
            new(
                "type",
                Localization.Text("Logistics_RouteType"),
                route.RouteType,
                Options: Options("NORMAL", "URGENTE"),
                Required: true
            ),
            new(
                "observation",
                Localization.Text("Common_Observation"),
                route.Observation,
                Required: true,
                Multiline: true
            )
        );
        if (form.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        await RunAndReloadAsync(() =>
            _apiClient.UpdateRouteOfferAsync(
                _session.Token,
                route.RouteId,
                new(
                    DateOnly.ParseExact(form["date"], "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    form["type"],
                    form["observation"],
                    route.RowVersion
                )
            )
        );
    }

    /// <summary>Despacha una hoja planificada o solicita su cancelación con motivo auditable.</summary>
    /// <param name="cancel">Indica cancelación; <see langword="false"/> inicia el recorrido.</param>
    /// <returns>Tarea que termina al cancelar el diálogo o aplicar y recargar la transición.</returns>
    private async Task ChangeRouteAsync(bool cancel)
    {
        if (_routes.CurrentRow?.DataBoundItem is not RouteSheetResponse route)
        {
            Warn(Localization.Text("Logistics_SelectRoute"));
            return;
        }

        string? reason = null;
        if (!cancel && route.StatusCode != "PLANIFICADA")
        {
            Warn(Localization.Text("Logistics_SelectRoute"));
            return;
        }
        if (cancel)
        {
            using var form = new SimpleEntryForm(
                Localization.Text("Common_Cancel"),
                new EntryField(
                    "reason",
                    Localization.Text("Common_Reason"),
                    Required: true,
                    Multiline: true
                )
            );
            if (form.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }
            reason = form["reason"];
        }

        await RunAndReloadAsync(() =>
            cancel
                ? _apiClient.CancelRouteAsync(
                    _session.Token,
                    route.RouteId,
                    new LogisticsTransitionRequest(route.RowVersion, reason)
                )
                : _apiClient.DispatchRouteAsync(
                    _session.Token,
                    route.RouteId,
                    new LogisticsTransitionRequest(route.RowVersion, null)
                )
        );
    }

    /// <summary>Confirma el resultado de una parada y, si es parcial, los activos efectivamente operados.</summary>
    /// <returns>Tarea que termina al cancelar el formulario o guardar y recargar la parada.</returns>
    private async Task CompleteStopAsync()
    {
        if (_stops.CurrentRow?.DataBoundItem is not RouteStopResponse stop)
        {
            Warn(Localization.Text("Logistics_SelectStop"));
            return;
        }

        using var form = new SimpleEntryForm(
            Localization.Text("Logistics_CompleteStop"),
            new(
                "result",
                Localization.Text("Common_Result"),
                "ENTREGADA",
                Options: Options("ENTREGADA", "PARCIAL", "FALLIDA", "REPROGRAMADA"),
                Required: true
            ),
            new(
                "observation",
                Localization.Text("Common_Observation"),
                Required: true,
                Multiline: true
            )
        );
        if (form.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        IReadOnlyList<long> completedAssets = [];
        if (form["result"] == "PARCIAL")
        {
            var assets =
                _snapshot?.Assets.Where(item => item.RequestId == stop.RequestId).ToList() ?? [];
            var selected = SelectPartialAssets(assets);
            if (selected is null)
            {
                return;
            }
            completedAssets = selected;
        }

        await RunAndReloadAsync(() =>
            _apiClient.CompleteRouteStopAsync(
                _session.Token,
                stop.StopId,
                new(form["result"], form["observation"], stop.RowVersion, completedAssets)
            )
        );
    }

    /// <summary>Permite identificar los activos realmente operados cuando la visita fue parcial.</summary>
    /// <param name="assets">Activos asociados a la solicitud de la parada.</param>
    /// <returns>Identificadores marcados o <see langword="null"/> si el usuario cancela.</returns>
    private IReadOnlyList<long>? SelectPartialAssets(IReadOnlyList<LogisticsAssetResponse> assets)
    {
        if (assets.Count == 0)
        {
            return [];
        }

        using var dialog = new Form
        {
            Text = Localization.Text("Logistics_SelectCompletedAssets"),
            Width = 620,
            Height = 460,
            MinimumSize = new Size(520, 360),
            StartPosition = FormStartPosition.CenterParent,
        };
        UiTheme.Apply(dialog);
        var list = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true };
        foreach (var asset in assets)
        {
            list.Items.Add(
                new AssetChoice(
                    asset.RequestAssetId,
                    $"{asset.SerialNumber} · {asset.Role.Replace('_', ' ')} · {asset.Product}"
                )
            );
        }

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 58,
            Padding = new Padding(8),
            FlowDirection = FlowDirection.RightToLeft,
        };
        var accept = UiTheme.Button(Localization.Text("Common_Save"), 0, 0, 130);
        accept.DialogResult = DialogResult.OK;
        var cancel = UiTheme.Button(
            Localization.Text("Common_Cancel"),
            0,
            0,
            130,
            ButtonTone.Danger
        );
        cancel.DialogResult = DialogResult.Cancel;
        buttons.Controls.AddRange([accept, cancel]);
        dialog.Controls.Add(list);
        dialog.Controls.Add(buttons);
        dialog.AcceptButton = accept;
        dialog.CancelButton = cancel;

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return null;
        }

        return list.CheckedItems.Cast<AssetChoice>().Select(item => item.Id).ToList();
    }

    /// <summary>Representa un activo seleccionable en la confirmación parcial.</summary>
    private sealed record AssetChoice(long Id, string Label)
    {
        /// <inheritdoc />
        public override string ToString() => Label;
    }

    /// <summary>Filtra los activos de la solicitud seleccionada y presenta su información funcional.</summary>
    private void ShowAssets()
    {
        var request = _requests.CurrentRow?.DataBoundItem as LogisticsRequestResponse;
        _assets.DataSource = (
            _snapshot?.Assets.Where(x => request is null || x.RequestId == request.RequestId) ?? []
        ).ToList();
        HideTechnicalColumns(_assets, "RequestAssetId", "RequestId", "AssetId", "LoanId");
        LocalizeColumns(_assets);
    }

    /// <summary>Filtra las paradas de la hoja seleccionada sin perder su historial de resultados.</summary>
    private void ShowStops()
    {
        var route = _routes.CurrentRow?.DataBoundItem as RouteSheetResponse;
        _stops.DataSource = (
            _snapshot?.Stops.Where(x => route is null || x.RouteId == route.RouteId) ?? []
        ).ToList();
        HideTechnicalColumns(_stops, "StopId", "RouteId", "RequestId", "RowVersion");
        LocalizeColumns(_stops);
    }

    /// <summary>Ejecuta una operación logística y actualiza la vista aun cuando deba informar un error recuperable.</summary>
    /// <param name="action">Solicitud de escritura enviada a la API.</param>
    /// <returns>Tarea que termina tras ejecutar y volver a consultar el estado logístico.</returns>
    private async Task RunAndReloadAsync(Func<Task> action)
    {
        await RunAsync(action);
        await LoadAsync();
    }

    /// <summary>Muestra el cursor de espera y presenta errores HTTP o de entrada sin cerrar la pantalla.</summary>
    /// <param name="action">Operación asíncrona a ejecutar.</param>
    /// <returns>Tarea que termina cuando finaliza la operación o se informa un error recuperable.</returns>
    private async Task RunAsync(Func<Task> action)
    {
        try
        {
            UseWaitCursor = true;
            await action();
        }
        catch (Exception exception)
            when (exception is HttpRequestException or FormatException or ArgumentException)
        {
            MessageBox.Show(
                this,
                exception.Message,
                "OxiTigre",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    /// <summary>Consulta los permisos logísticos de la sesión para habilitar acciones.</summary>
    /// <param name="permission">Código de permiso requerido.</param>
    /// <returns><see langword="true"/> cuando la sesión lo incluye.</returns>
    private bool Can(string permission) =>
        _session.Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);

    /// <summary>Crea una grilla con el estilo y comportamiento común del módulo de logística.</summary>
    /// <param name="multiSelect">Indica si la grilla permite seleccionar varias filas.</param>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private static DataGridView Grid(bool multiSelect = false)
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AutoGenerateColumns = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells,
        };
        UiTheme.Grid(grid);
        grid.MultiSelect = multiSelect;
        return grid;
    }

    /// <summary>Crea una barra de acciones coherente con el módulo de logística.</summary>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private static FlowLayoutPanel Bar() =>
        new()
        {
            Dock = DockStyle.Top,
            Height = 52,
            Padding = new Padding(10, 8, 10, 4),
            WrapContents = false,
        };

    /// <summary>Convierte valores técnicos en opciones localizadas para una lista.</summary>
    /// <param name="values">Valores que se vinculan con el control.</param>
    /// <returns>Colección de registros u opciones obtenida por la operación.</returns>
    private static IReadOnlyList<KeyValuePair<string, string>> Options(params string[] values) =>
        values
            .Select(value => new KeyValuePair<string, string>(value, value.Replace('_', ' ')))
            .ToList();

    /// <summary>Convierte un texto vacío en nulo antes de enviarlo a la API.</summary>
    /// <param name="value">Texto que se normaliza, valida o asigna.</param>
    /// <returns>Texto normalizado, localizado o formateado por la operación.</returns>
    private static string? Empty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

    /// <summary>Interpreta una hora opcional en formato operativo.</summary>
    /// <param name="value">Texto que se normaliza, valida o asigna.</param>
    /// <returns>Valor obtenido después de aplicar la conversión o búsqueda.</returns>
    private static TimeOnly? Time(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : TimeOnly.ParseExact(value, "HH:mm", CultureInfo.InvariantCulture);

    /// <summary>Interpreta una fecha opcional en formato operativo.</summary>
    /// <param name="value">Texto que se normaliza, valida o asigna.</param>
    /// <returns>Valor obtenido después de aplicar la conversión o búsqueda.</returns>
    private static DateOnly? Date(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>Muestra una advertencia funcional al usuario.</summary>
    /// <param name="message">Mensaje funcional que se muestra al usuario.</param>
    private void Warn(string message) =>
        MessageBox.Show(
            this,
            message,
            "OxiTigre",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information
        );

    /// <summary>Oculta las columnas técnicas para no exponer información técnica.</summary>
    /// <param name="grid">Grilla que se configura, consulta o actualiza.</param>
    /// <param name="names">Nombres de columnas técnicas que se ocultan.</param>
    private static void HideTechnicalColumns(DataGridView grid, params string[] names)
    {
        foreach (var name in names)
        {
            if (grid.Columns[name] is { } column)
            {
                column.Visible = false;
            }
        }
    }

    /// <summary>Asigna títulos en español o inglés a las columnas de la grilla.</summary>
    /// <param name="grid">Grilla que se configura, consulta o actualiza.</param>
    private static void LocalizeColumns(DataGridView grid)
    {
        var resources = new Dictionary<string, string>
        {
            ["Code"] = "Grid_Code",
            ["Name"] = "Common_Name",
            ["Client"] = "Common_Client",
            ["Address"] = "Common_Address",
            ["City"] = "Common_City",
            ["Province"] = "Common_Province",
            ["PostalCode"] = "Common_PostalCode",
            ["Contact"] = "Common_Contact",
            ["Phone"] = "Column_Phone",
            ["Email"] = "Column_Email",
            ["FromTime"] = "Logistics_FromTime",
            ["ToTime"] = "Logistics_ToTime",
            ["ServiceMinutes"] = "Logistics_ServiceMinutes",
            ["Restrictions"] = "Logistics_Restrictions",
            ["Instructions"] = "Common_Instructions",
            ["StatusCode"] = "Grid_Status",
            ["ServiceType"] = "Logistics_ServiceType",
            ["Priority"] = "Common_Priority",
            ["RequestedDate"] = "Common_Date",
            ["Observation"] = "Common_Observation",
            ["Owner"] = "Logistics_Owner",
            ["Role"] = "Logistics_Role",
            ["SerialNumber"] = "Column_Serial",
            ["Product"] = "Common_Product",
            ["ContentQuantity"] = "Logistics_Content",
            ["Unit"] = "Column_Unit",
            ["Condition"] = "Common_Condition",
            ["RouteDate"] = "Common_Date",
            ["RouteType"] = "Logistics_RouteType",
            ["AssignmentType"] = "Logistics_AssignmentType",
            ["Driver"] = "Logistics_Driver",
            ["Plate"] = "Logistics_Plate",
            ["AssignedUtc"] = "Logistics_Assigned",
            ["DepartureUtc"] = "Logistics_Departure",
            ["ClosedUtc"] = "Logistics_Closed",
            ["Order"] = "Logistics_Order",
            ["ArrivalUtc"] = "Logistics_Arrival",
            ["Result"] = "Column_Result",
            ["ResultObservation"] = "Common_Observation",
            ["EventType"] = "Traceability_Event",
            ["PreviousStatus"] = "Traceability_PreviousStatus",
            ["NewStatus"] = "Traceability_NewStatus",
            ["Correlation"] = "Traceability_Correlation",
            ["DateUtc"] = "Common_Date",
            ["Channel"] = "Logistics_Channel",
            ["Recipient"] = "Logistics_Recipient",
            ["Message"] = "Logistics_Message",
            ["Attempts"] = "Logistics_Attempts",
            ["CreatedUtc"] = "Logistics_Created",
            ["SentUtc"] = "Logistics_Sent",
            ["UserName"] = "Logistics_User",
            ["FullName"] = "Logistics_Transporter",
            ["RelationshipType"] = "Logistics_Relationship",
            ["Document"] = "Logistics_Document",
            ["License"] = "Logistics_License",
            ["LicenseCategory"] = "Logistics_LicenseCategory",
            ["LicenseExpiration"] = "Logistics_LicenseExpiration",
            ["VehicleType"] = "Logistics_VehicleType",
            ["OwnershipType"] = "Logistics_Ownership",
            ["Brand"] = "Logistics_Brand",
            ["Model"] = "Logistics_Model",
            ["Year"] = "Logistics_Year",
            ["LoadCapacityKg"] = "Logistics_LoadCapacity",
            ["InsurancePolicy"] = "Logistics_InsurancePolicy",
            ["InsuranceExpiration"] = "Logistics_InsuranceExpiration",
            ["InspectionExpiration"] = "Logistics_InspectionExpiration",
            ["OrderCode"] = "Sales_Order",
            ["Operation"] = "Logistics_Operation",
            ["OrderDate"] = "Common_Date",
            ["Total"] = "Sales_Total",
            ["Outstanding"] = "Finance_Outstanding",
            ["PaymentStatus"] = "Logistics_PaymentStatus",
            ["AssetCount"] = "Logistics_AssetCount",
            ["ExpectedReturnDate"] = "Sales_ExpectedReturn",
            ["Summary"] = "Common_Description",
            ["DestinationWarehouse"] = "Logistics_DestinationWarehouse",
        };

        foreach (DataGridViewColumn column in grid.Columns)
        {
            if (resources.TryGetValue(column.Name, out var resource))
            {
                column.HeaderText = Localization.Text(resource);
            }

            if (column.Name.EndsWith("Utc", StringComparison.Ordinal))
            {
                column.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
            }
        }

        grid.AutoSizeColumnsMode =
            grid.Columns.Cast<DataGridViewColumn>().Count(column => column.Visible) <= 9
                ? DataGridViewAutoSizeColumnsMode.Fill
                : DataGridViewAutoSizeColumnsMode.DisplayedCells;
    }
}
