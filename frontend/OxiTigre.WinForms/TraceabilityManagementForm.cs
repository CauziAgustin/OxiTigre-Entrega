/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.WinForms.TraceabilityManagementForm
Archivo: TraceabilityManagementForm.cs | Versión: 1.3.0 | Fecha: 2026-08-28 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Consulta y confirma mediciones, fraccionamientos, incidentes, mantenimiento y préstamos trazables.
Historial: 1.0.0 | 2026-08-24 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-26 | FABRICA | Agustin Omar Cauzi | Pestañas, filtros, ayuda y cantidades localizadas.
Historial: 1.2.0 | 2026-08-26 | FABRICA | Agustin Omar Cauzi | Títulos visibles y filtros guiados en todas las grillas trazables.
Historial: 1.3.0 | 2026-08-28 | FABRICA | Alta y movimientos de custodia de activos propiedad de clientes.
===============================================================================
*/
using System.Globalization;
using OxiTigre.ApiClient;
using OxiTigre.Contracts.Inventory;
using OxiTigre.Contracts.Security;

namespace OxiTigre.WinForms;

/// <summary>Opera la trazabilidad transversal sin permitir editar su historial.</summary>
internal sealed class TraceabilityManagementForm : Form
{
    private readonly OxiTigreApiClient _api;
    private readonly LoginResponse _session;
    private readonly bool _canManage;
    private readonly DataGridView _assets = Grid(),
        _lots = Grid(),
        _measurements = Grid(),
        _transformations = Grid(),
        _incidents = Grid(),
        _maintenances = Grid(),
        _loans = Grid(),
        _events = Grid();
    private TraceabilitySnapshotResponse? _data;
    private InventorySnapshotResponse? _inventory;

    /// <summary>Inicializa el componente de trazabilidad con sus dependencias y datos de trabajo.</summary>
    /// <param name="api">Cliente usado para comunicarse con la API.</param>
    /// <param name="session">Sesión autenticada que determina permisos y contexto operativo.</param>
    internal TraceabilityManagementForm(OxiTigreApiClient api, LoginResponse session)
    {
        _api = api;
        _session = session;
        _canManage = session.Permissions.Contains(
            "INVENTARIO.GESTIONAR",
            StringComparer.OrdinalIgnoreCase
        );
        Text = Localization.Text("Traceability_Title");
        Width = 1280;
        Height = 800;
        MinimumSize = new Size(1000, 650);
        StartPosition = FormStartPosition.CenterParent;
        UiTheme.Apply(this);
        var tabs = new TabControl { Dock = DockStyle.Fill };
        UiTheme.Tabs(tabs);
        tabs.TabPages.Add(Page("Traceability_Current", Current()));
        tabs.TabPages.Add(
            Page(
                "Traceability_Measurements",
                Panel(
                    "Traceability_Measurements",
                    _measurements,
                    ("Traceability_NewMeasurement", Measure)
                )
            )
        );
        tabs.TabPages.Add(
            Page(
                "Traceability_Transformations",
                Panel(
                    "Traceability_Transformations",
                    _transformations,
                    ("Traceability_NewTransformation", Transform)
                )
            )
        );
        tabs.TabPages.Add(
            Page(
                "Traceability_Incidents",
                Panel("Traceability_Incidents", _incidents, ("Traceability_NewIncident", Incident))
            )
        );
        tabs.TabPages.Add(
            Page(
                "Traceability_Maintenances",
                Panel(
                    "Traceability_Maintenances",
                    _maintenances,
                    ("Traceability_NewMaintenance", Maintenance),
                    ("Traceability_CompleteMaintenance", CompleteMaintenance)
                )
            )
        );
        tabs.TabPages.Add(
            Page(
                "Traceability_Loans",
                Panel(
                    "Traceability_Loans",
                    _loans,
                    ("Traceability_NewLoan", Loan),
                    ("Traceability_ReturnLoan", ReturnLoan)
                )
            )
        );
        tabs.TabPages.Add(
            Page(
                "Traceability_Events",
                UiTheme.Searchable(
                    _events,
                    Localization.Text("Traceability_Help"),
                    Localization.Text("Traceability_Events")
                )
            )
        );
        Controls.Add(tabs);
        Controls.Add(
            new Label
            {
                Text = Localization.Text("Traceability_Overview"),
                Dock = DockStyle.Top,
                Height = 48,
                Padding = new Padding(10),
                BackColor = Color.FromArgb(230, 244, 234),
                ForeColor = UiTheme.Primary,
            }
        );
        Shown += async (_, _) => await LoadAsync();
    }

    /// <summary>Construye la vista combinada del estado actual y los lotes disponibles.</summary>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private Control Current()
    {
        var panel = new Panel { Dock = DockStyle.Fill };
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 300,
        };
        split.Panel1.Controls.Add(
            UiTheme.Searchable(
                _assets,
                Localization.Text("Traceability_ClientAssetHelp"),
                Localization.Text("Traceability_Assets")
            )
        );
        split.Panel2.Controls.Add(
            UiTheme.Searchable(
                _lots,
                Localization.Text("Traceability_Help"),
                Localization.Text("Traceability_Lots")
            )
        );
        panel.Controls.Add(split);

        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 58,
            Padding = new Padding(8),
        };
        var register = UiTheme.Button(
            Localization.Text("Traceability_RegisterClientAsset"),
            0,
            0,
            220
        );
        register.Enabled = _canManage;
        register.Click += async (_, _) => await RegisterClientAsset();
        var custody = UiTheme.Button(
            Localization.Text("Traceability_ClientAssetCustody"),
            0,
            0,
            220
        );
        custody.Enabled = _canManage;
        custody.Click += async (_, _) => await ChangeClientAssetCustody();
        bar.Controls.Add(register);
        bar.Controls.Add(custody);
        panel.Controls.Add(bar);

        return panel;
    }

    /// <summary>Crea un panel titulado con su grilla y acciones operativas.</summary>
    /// <param name="titleKey">Clave de recurso usada como título visible.</param>
    /// <param name="grid">Grilla que se configura, consulta o actualiza.</param>
    /// <param name="actions">Acciones disponibles en la barra de herramientas.</param>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private Control Panel(
        string titleKey,
        DataGridView grid,
        params (string Key, Func<Task> Action)[] actions
    )
    {
        var panel = new Panel { Dock = DockStyle.Fill };
        panel.Controls.Add(
            UiTheme.Searchable(
                grid,
                Localization.Text("Traceability_Help"),
                Localization.Text(titleKey)
            )
        );
        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 58,
            Padding = new Padding(8),
        };
        foreach (var a in actions)
        {
            var button = UiTheme.Button(Localization.Text(a.Key), 0, 0, 190);
            button.Enabled = _canManage;
            button.Click += async (_, _) => await a.Action();
            bar.Controls.Add(button);
        }
        panel.Controls.Add(bar);
        return panel;
    }

    /// <summary>Carga  y actualiza la interfaz con los datos obtenidos.</summary>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private async Task LoadAsync()
    {
        try
        {
            var traceabilityTask = _api.GetTraceabilityAsync(_session.Token);
            var inventoryTask = _api.GetInventoryAsync(_session.Token);
            await Task.WhenAll(traceabilityTask, inventoryTask);
            _data = await traceabilityTask;
            _inventory = await inventoryTask;

            Bind(
                _assets,
                _data.Assets,
                "AssetId",
                "ProductId",
                "OwnerClientId",
                "WarehouseId",
                "LocationId",
                "ContentProductId",
                "ContentLotId",
                "RowVersion"
            );
            Bind(_lots, _data.Lots, "LotId", "ProductId", "WarehouseId");
            Bind(_measurements, _data.Measurements, "MeasurementId", "AssetId");
            Bind(
                _transformations,
                _data.Transformations,
                "TransformationId",
                "SourceLotId",
                "SourceAssetId",
                "DestinationAssetId"
            );
            Bind(_incidents, _data.Incidents, "IncidentId", "AssetId");
            Bind(_maintenances, _data.Maintenances, "MaintenanceId", "RowVersion");
            Bind(_loans, _data.Loans, "LoanId", "LoanLineId", "AssetId", "RowVersion");
            Bind(_events, _data.Events, "EventId", "AssetId", "UserId");
        }
        catch (HttpRequestException ex)
        {
            Error(ex.Message);
        }
    }

    /// <summary>Solicita los datos mínimos para identificar un activo que pertenece a un cliente.</summary>
    /// <returns>Tarea que finaliza cuando el alta fue enviada o cancelada.</returns>
    /// <exception cref="HttpRequestException">La API no pudo completar el alta.</exception>
    private async Task RegisterClientAsset()
    {
        if (_data is null || _inventory is null)
        {
            return;
        }

        var products = _inventory
            .Products.Where(item =>
                item.StatusCode == "ACTIVO" && (item.TrackingType == "SERIE" || item.IsReusable)
            )
            .ToList();
        var warehouses = _inventory.Warehouses.Where(item => item.StatusCode == "ACTIVO").ToList();
        if (_data.Clients.Count == 0 || products.Count == 0)
        {
            Error(Localization.Text("Purchases_NoSelection"));
            return;
        }

        var locations = new[]
        {
            new KeyValuePair<string, string>("CLIENTE", Localization.Text("Traceability_AtClient")),
            new KeyValuePair<string, string>(
                "OXITIGRE",
                Localization.Text("Traceability_AtOxiTigre")
            ),
        };
        var assetTypes = new[]
        {
            new KeyValuePair<string, string>("CILINDRO", "Cilindro"),
            new KeyValuePair<string, string>("TUBO", "Tubo"),
            new KeyValuePair<string, string>("ENVASE", "Envase"),
            new KeyValuePair<string, string>("HERRAMIENTA", "Herramienta"),
            new KeyValuePair<string, string>("OTRO", "Otro"),
        };
        var conditions = new[]
        {
            new KeyValuePair<string, string>("OPERATIVO", "Operativo"),
            new KeyValuePair<string, string>("EN_REVISION", "En revisión"),
            new KeyValuePair<string, string>("DANADO", "Dañado"),
        };
        var warehouseOptions = new[]
        {
            new KeyValuePair<string, string>(string.Empty, string.Empty),
        }
            .Concat(
                warehouses.Select(item => new KeyValuePair<string, string>(
                    item.WarehouseId.ToString(CultureInfo.InvariantCulture),
                    item.Name
                ))
            )
            .ToList();

        using var form = new SimpleEntryForm(
            Localization.Text("Traceability_RegisterClientAsset"),
            Choice(
                "Client",
                "Purchases_Client",
                _data.Clients,
                item => item.Id,
                item => $"{item.Code} · {item.Name}"
            ),
            Choice(
                "Product",
                "Inventory_Product",
                products,
                item => item.ProductId,
                item => $"{item.Code} · {item.Name}"
            ),
            new(
                "InitialLocation",
                Localization.Text("Traceability_InitialLocation"),
                "CLIENTE",
                locations,
                Required: true
            ),
            new("Warehouse", Localization.Text("Inventory_Warehouse"), Options: warehouseOptions),
            new(
                "Serial",
                Localization.Text("Column_Serial"),
                Help: Localization.Text("Traceability_SerialHelp")
            ),
            new(
                "AssetType",
                Localization.Text("Column_Type"),
                "CILINDRO",
                assetTypes,
                Required: true
            ),
            new("Capacity", Localization.Text("Traceability_Capacity"), "0", Numeric: true),
            new("Unit", Localization.Text("Column_Unit"), "kg", Required: true),
            new(
                "Condition",
                Localization.Text("Traceability_Condition"),
                "OPERATIVO",
                conditions,
                Required: true
            ),
            new(
                "Observation",
                Localization.Text("Config_Description"),
                Required: true,
                Multiline: true,
                Help: Localization.Text("Traceability_ClientAssetObservationHelp")
            )
        );

        if (form.ShowDialog(this) != DialogResult.OK || !Number(form["Capacity"], out var capacity))
        {
            return;
        }

        var inCustody = form["InitialLocation"] == "OXITIGRE";
        long? warehouseId = long.TryParse(
            form["Warehouse"],
            CultureInfo.InvariantCulture,
            out var parsedWarehouse
        )
            ? parsedWarehouse
            : null;
        if (inCustody != warehouseId.HasValue)
        {
            Error(Localization.Text("Traceability_InvalidInitialLocation"));
            return;
        }

        await Run(() =>
            _api.SaveClientAssetAsync(
                _session.Token,
                new(
                    long.Parse(form["Client"], CultureInfo.InvariantCulture),
                    long.Parse(form["Product"], CultureInfo.InvariantCulture),
                    warehouseId,
                    form["Serial"],
                    form["AssetType"],
                    capacity == 0 ? null : capacity,
                    form["Unit"],
                    form["Condition"],
                    inCustody,
                    form["Observation"]
                )
            )
        );
    }

    /// <summary>Confirma el próximo movimiento válido de custodia del activo seleccionado.</summary>
    /// <returns>Tarea que finaliza cuando el movimiento fue enviado o cancelado.</returns>
    /// <exception cref="HttpRequestException">La API no pudo completar el movimiento.</exception>
    private async Task ChangeClientAssetCustody()
    {
        var asset = Selected<TraceAssetResponse>(_assets);
        if (asset?.OwnerClientId is null || _inventory is null)
        {
            Error(Localization.Text("Traceability_SelectClientAsset"));
            return;
        }

        var action = asset.StatusCode switch
        {
            "EN_CLIENTE" => "INGRESAR",
            "DISPONIBLE" => "ENTREGAR",
            _ => null,
        };
        if (action is null)
        {
            Error(Localization.Text("Traceability_InvalidClientAssetState"));
            return;
        }

        var fields = new List<EntryField>();
        if (action == "INGRESAR")
        {
            var warehouses = _inventory
                .Warehouses.Where(item => item.StatusCode == "ACTIVO")
                .ToList();
            if (warehouses.Count == 0)
            {
                Error(Localization.Text("Purchases_NoSelection"));
                return;
            }

            fields.Add(
                Choice(
                    "Warehouse",
                    "Inventory_Warehouse",
                    warehouses,
                    item => item.WarehouseId,
                    item => item.Name
                )
            );
        }
        fields.Add(
            new(
                "Observation",
                Localization.Text("Config_Description"),
                Required: true,
                Multiline: true,
                Help: Localization.Text("Traceability_CustodyObservationHelp")
            )
        );

        using var form = new SimpleEntryForm(
            Localization.Text(
                action == "INGRESAR"
                    ? "Traceability_ReceiveClientAsset"
                    : "Traceability_DeliverClientAsset"
            ),
            fields.ToArray()
        );
        if (form.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        long? warehouseId =
            action == "INGRESAR"
                ? long.Parse(form["Warehouse"], CultureInfo.InvariantCulture)
                : null;
        await Run(() =>
            _api.ChangeClientAssetCustodyAsync(
                _session.Token,
                asset.AssetId,
                new(asset.AssetId, action, warehouseId, form["Observation"], asset.RowVersion)
            )
        );
    }

    /// <summary>Solicita y registra una medición manual sobre el activo seleccionado.</summary>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private async Task Measure()
    {
        if (_data?.Assets.Count is not > 0)
            return;
        using var f = new SimpleEntryForm(
            Localization.Text("Traceability_NewMeasurement"),
            Choice(
                "Asset",
                "Traceability_Assets",
                _data.Assets,
                x => x.AssetId,
                x => $"{x.Code} · {x.Product}"
            ),
            new("Type", Localization.Text("Column_Type"), "CONTENIDO", Required: true),
            new("Value", Localization.Text("Column_Value"), Required: true, Numeric: true),
            new("Unit", Localization.Text("Column_Unit"), "kg", Required: true),
            new("Method", Localization.Text("Column_Method"), "PESAJE", Required: true),
            new("Observation", Localization.Text("Config_Description"), Multiline: true)
        );
        if (f.ShowDialog(this) != DialogResult.OK || !Number(f["Value"], out var value))
            return;
        await Run(async () =>
            await _api.CreateAssetMeasurementAsync(
                _session.Token,
                new(
                    long.Parse(f["Asset"]),
                    DateTime.UtcNow,
                    f["Type"],
                    value,
                    f["Unit"],
                    f["Method"],
                    "MANUAL",
                    false,
                    null,
                    f["Observation"]
                )
            )
        );
    }

    /// <summary>Registra un fraccionamiento y conserva origen, destinos y merma.</summary>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private async Task Transform()
    {
        if (_data is null || _data.Lots.Count == 0 || _data.Assets.Count == 0)
            return;
        using var f = new SimpleEntryForm(
            Localization.Text("Traceability_NewTransformation"),
            Choice(
                "Lot",
                "Traceability_Lots",
                _data.Lots,
                x => x.LotId,
                x => $"{x.Code} · {x.Product} ({x.Quantity:N4})"
            ),
            new(
                "Destinations",
                Localization.Text("Traceability_DestinationsFormat"),
                Required: true,
                Multiline: true
            ),
            new("Loss", Localization.Text("Traceability_Loss"), "0", Required: true),
            new("Method", Localization.Text("Column_Method"), "PESAJE", Required: true),
            new("Reason", Localization.Text("Purchases_Reason")),
            new("Observation", Localization.Text("Config_Description"), Multiline: true)
        );
        if (f.ShowDialog(this) != DialogResult.OK || !Number(f["Loss"], out var loss))
            return;
        var lot = _data.Lots.First(x => x.LotId == long.Parse(f["Lot"]));
        if (ParseDestinations(f["Destinations"], _data.Assets, lot.LotId) is not { } destinations)
            return;
        await Run(async () =>
            await _api.CreateTransformationAsync(
                _session.Token,
                new(
                    lot.LotId,
                    null,
                    lot.ProductId,
                    DateTime.UtcNow,
                    destinations.Sum(x => x.Quantity) + loss,
                    loss,
                    f["Method"],
                    f["Reason"],
                    f["Observation"],
                    destinations
                )
            )
        );
    }

    /// <summary>Registra un incidente y su efecto sobre activo, contenido y condición.</summary>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private async Task Incident()
    {
        if (_data?.Assets.Any(x => x.WarehouseId is not null) != true)
            return;
        using var f = new SimpleEntryForm(
            Localization.Text("Traceability_NewIncident"),
            Choice(
                "Asset",
                "Traceability_Assets",
                _data.Assets.Where(x => x.WarehouseId is not null).ToList(),
                x => x.AssetId,
                x => $"{x.Code} · {x.Product}"
            ),
            new("Type", Localization.Text("Column_Type"), "FUGA", Required: true),
            new("Loss", Localization.Text("Traceability_Loss"), "0", Required: true),
            new("Cause", Localization.Text("Traceability_Cause"), Required: true, Multiline: true),
            new("Action", Localization.Text("Traceability_Action"), Multiline: true)
        );
        if (f.ShowDialog(this) != DialogResult.OK || !Number(f["Loss"], out var loss))
            return;
        var asset = _data.Assets.First(x => x.AssetId == long.Parse(f["Asset"]));
        await Run(async () =>
            await _api.CreateIncidentAsync(
                _session.Token,
                new(
                    asset.ProductId,
                    asset.WarehouseId!.Value,
                    asset.AssetId,
                    asset.ContentLotId,
                    f["Type"],
                    DateTime.UtcNow,
                    asset.ContentQuantity,
                    loss,
                    asset.ContentQuantity - loss,
                    true,
                    "ESTIMADO",
                    f["Cause"],
                    f["Action"],
                    null
                )
            )
        );
    }

    /// <summary>Registra el inicio de un mantenimiento sobre el activo seleccionado.</summary>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private async Task Maintenance()
    {
        if (_data?.Assets.Count is not > 0)
            return;
        using var f = new SimpleEntryForm(
            Localization.Text("Traceability_NewMaintenance"),
            Choice(
                "Asset",
                "Traceability_Assets",
                _data.Assets,
                x => x.AssetId,
                x => $"{x.Code} · {x.Product}"
            ),
            new("Type", Localization.Text("Column_Type"), "CORRECTIVO", Required: true),
            new("Work", Localization.Text("Traceability_Work"), Required: true, Multiline: true),
            new("Cost", Localization.Text("Column_Cost"), "0")
        );
        if (f.ShowDialog(this) != DialogResult.OK || !Number(f["Cost"], out var cost))
            return;
        await Run(async () =>
            await _api.CreateMaintenanceAsync(
                _session.Token,
                new(long.Parse(f["Asset"]), null, null, f["Type"], DateTime.UtcNow, f["Work"], cost)
            )
        );
    }

    /// <summary>Completa un mantenimiento con resultado, repuesto y próxima revisión.</summary>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private async Task CompleteMaintenance()
    {
        var item = Selected<AssetMaintenanceResponse>(_maintenances);
        if (item is null || item.StatusCode != "EN_CURSO")
        {
            Error(Localization.Text("Purchases_NoSelection"));
            return;
        }
        using var f = new SimpleEntryForm(
            Localization.Text("Traceability_CompleteMaintenance"),
            new("Result", Localization.Text("Column_Result"), Required: true, Multiline: true),
            new("Old", Localization.Text("Traceability_OldComponent")),
            new("New", Localization.Text("Traceability_NewComponent")),
            new(
                "Cost",
                Localization.Text("Column_Cost"),
                item.Cost?.ToString(CultureInfo.CurrentCulture) ?? "0"
            ),
            new("Certificate", Localization.Text("Traceability_Certificate")),
            new("Next", Localization.Text("Traceability_NextReview"))
        );
        if (f.ShowDialog(this) != DialogResult.OK || !Number(f["Cost"], out var cost))
            return;
        DateOnly? next = DateOnly.TryParse(f["Next"], out var date) ? date : null;
        await Run(async () =>
        {
            await _api.CompleteMaintenanceAsync(
                _session.Token,
                item.MaintenanceId,
                new(
                    item.MaintenanceId,
                    DateTime.UtcNow,
                    f["Result"],
                    f["Old"],
                    f["New"],
                    cost,
                    f["Certificate"],
                    next,
                    item.RowVersion
                )
            );
            return new SavedTraceabilityResponse(item.MaintenanceId);
        });
    }

    /// <summary>Registra un préstamo con activos, condición, entrega y devolución prevista.</summary>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private async Task Loan()
    {
        if (_data?.Assets.Count is not > 0)
            return;
        var destinationTypes = new[]
        {
            new KeyValuePair<string, string>(
                "SUCURSAL",
                Localization.Text("Traceability_DestinationBranch")
            ),
            new("CLIENTE", Localization.Text("Traceability_DestinationClient")),
            new("TERCERO", Localization.Text("Traceability_DestinationThirdParty")),
            new("INTEREMPRESA", Localization.Text("Traceability_DestinationCompany")),
        };
        using var f = new SimpleEntryForm(
            Localization.Text("Traceability_NewLoan"),
            new(
                "Assets",
                Localization.Text("Traceability_LoanAssetsFormat"),
                Required: true,
                Multiline: true
            ),
            new(
                "DestinationType",
                Localization.Text("Traceability_DestinationType"),
                "TERCERO",
                destinationTypes,
                Required: true
            ),
            ChoiceOptional("Branch", "Config_Branch", _data.Branches),
            ChoiceOptional("Client", "Purchases_Client", _data.Clients),
            new("External", Localization.Text("Traceability_ExternalDestination")),
            new("Company", Localization.Text("Traceability_CompanyCode")),
            new(
                "Mode",
                Localization.Text("Traceability_DeliveryMode"),
                "ENTREGA_PROPIA",
                Required: true
            ),
            new("Return", Localization.Text("Traceability_ExpectedReturn")),
            new(
                "Condition",
                Localization.Text("Traceability_Condition"),
                "OPERATIVO",
                Required: true
            ),
            new("Observation", Localization.Text("Config_Description"), Multiline: true)
        );
        if (
            f.ShowDialog(this) != DialogResult.OK
            || ParseLoanAssets(f["Assets"], _data.Assets, f["Condition"]) is not { } assets
        )
            return;
        var type = f["DestinationType"];
        long? branch =
            type == "SUCURSAL" && long.TryParse(f["Branch"], out var branchId) ? branchId : null;
        long? client =
            type == "CLIENTE" && long.TryParse(f["Client"], out var clientId) ? clientId : null;
        var external = type is "TERCERO" or "INTEREMPRESA" ? f["External"] : null;
        if (
            (type == "SUCURSAL" && branch is null)
            || (type == "CLIENTE" && client is null)
            || (type is "TERCERO" or "INTEREMPRESA" && string.IsNullOrWhiteSpace(external))
            || (type == "INTEREMPRESA" && string.IsNullOrWhiteSpace(f["Company"]))
        )
        {
            Error(Localization.Text("Common_RequiredFields"));
            return;
        }
        DateTime? expected = DateTime.TryParse(f["Return"], out var parsed) ? parsed : null;
        await Run(async () =>
            await _api.CreateLoanAsync(
                _session.Token,
                new(
                    type,
                    branch,
                    client,
                    external,
                    f["Mode"],
                    DateTime.UtcNow,
                    expected,
                    f["Observation"],
                    type == "INTEREMPRESA" ? f["Company"] : null,
                    type == "INTEREMPRESA" ? Guid.NewGuid() : null,
                    assets
                )
            )
        );
    }

    /// <summary>Registra la devolución de un préstamo y cualquier diferencia observada.</summary>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private async Task ReturnLoan()
    {
        var selected = Selected<AssetLoanLineResponse>(_loans);
        if (selected is null || selected.StatusCode != "PRESTADO" || _data is null)
        {
            Error(Localization.Text("Purchases_NoSelection"));
            return;
        }
        using var f = new SimpleEntryForm(
            Localization.Text("Traceability_ReturnLoan"),
            new(
                "Condition",
                Localization.Text("Traceability_Condition"),
                "OPERATIVO",
                Required: true
            ),
            new("Observation", Localization.Text("Config_Description"), Multiline: true)
        );
        if (f.ShowDialog(this) != DialogResult.OK)
            return;
        var lines = _data
            .Loans.Where(x => x.LoanId == selected.LoanId)
            .Select(x => new LoanAssetReturnRequest(
                x.LoanLineId,
                x.DepartureQuantity,
                f["Condition"],
                f["Observation"]
            ))
            .ToList();
        await Run(async () =>
        {
            await _api.ReturnLoanAsync(
                _session.Token,
                selected.LoanId,
                new(selected.LoanId, DateTime.UtcNow, f["Observation"], lines, selected.RowVersion)
            );
            return new SavedTraceabilityResponse(selected.LoanId);
        });
    }

    /// <summary>Ejecuta una operación, informa el resultado y traduce errores funcionales.</summary>
    /// <param name="action">Operación que se ejecuta cuando el usuario confirma la acción.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private async Task Run(Func<Task<SavedTraceabilityResponse>> action)
    {
        try
        {
            await action();
            await LoadAsync();
        }
        catch (HttpRequestException ex)
        {
            Error(ex.Message);
        }
    }

    /// <summary>Crea o recupera un campo de selección configurado con sus opciones.</summary>
    /// <param name="key">Clave de recurso o campo que identifica el texto o control.</param>
    /// <param name="label">Etiqueta visible del campo.</param>
    /// <param name="items">Registros u opciones que se presentan en el control.</param>
    /// <param name="id">Identificador del registro buscado.</param>
    /// <param name="text">Texto que se muestra, interpreta o transforma.</param>
    /// <returns>Objeto construido u obtenido por la operación.</returns>
    private static EntryField Choice<T>(
        string key,
        string label,
        IReadOnlyList<T> items,
        Func<T, long> id,
        Func<T, string> text
    ) =>
        new(
            key,
            Localization.Text(label),
            Options: items
                .Select(x => new KeyValuePair<string, string>(
                    id(x).ToString(CultureInfo.InvariantCulture),
                    text(x)
                ))
                .ToList(),
            Required: true
        );

    /// <summary>Crea un campo de selección que también admite ausencia de valor.</summary>
    /// <param name="key">Clave de recurso o campo que identifica el texto o control.</param>
    /// <param name="label">Etiqueta visible del campo.</param>
    /// <param name="items">Registros u opciones que se presentan en el control.</param>
    /// <returns>Objeto construido u obtenido por la operación.</returns>
    private static EntryField ChoiceOptional(
        string key,
        string label,
        IReadOnlyList<TraceDestinationResponse> items
    ) =>
        new(
            key,
            Localization.Text(label),
            Options: new[] { new KeyValuePair<string, string>(string.Empty, string.Empty) }
                .Concat(
                    items.Select(x => new KeyValuePair<string, string>(
                        x.Id.ToString(CultureInfo.InvariantCulture),
                        $"{x.Code} · {x.Name}"
                    ))
                )
                .ToList()
        );

    /// <summary>Interpreta y valida los destinos de fraccionamiento a partir del texto ingresado.</summary>
    /// <param name="text">Texto que se muestra, interpreta o transforma.</param>
    /// <param name="assets">Activos disponibles usados para validar series y destinos.</param>
    /// <param name="lotId">Identificador del lote de origen de la transformación.</param>
    /// <returns>Colección de registros u opciones obtenida por la operación.</returns>
    /// <exception cref="FormatException">Se produce cuando una respuesta o estado inválido impide completar la operación.</exception>
    private static IReadOnlyList<TransformationDestinationRequest>? ParseDestinations(
        string text,
        IReadOnlyList<TraceAssetResponse> assets,
        long lotId
    )
    {
        try
        {
            return
                text.Split(
                        ';',
                        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                    )
                    .Select(value =>
                    {
                        var parts = value.Split(':', StringSplitOptions.TrimEntries);
                        var asset =
                            parts.Length == 2
                                ? assets.FirstOrDefault(x =>
                                    x.Code.Equals(parts[0], StringComparison.OrdinalIgnoreCase)
                                    && x.StatusCode == "DISPONIBLE"
                                )
                                : null;
                        return
                            asset is not null
                            && decimal.TryParse(
                                parts[1],
                                NumberStyles.Number,
                                CultureInfo.CurrentCulture,
                                out var quantity
                            )
                            && quantity > 0
                            ? new TransformationDestinationRequest(asset.AssetId, lotId, quantity)
                            : throw new FormatException();
                    })
                    .ToList()
                    is { Count: > 0 } result
                ? result
                : throw new FormatException();
        }
        catch (FormatException)
        {
            Error(Localization.Text("Traceability_InvalidDestinations"));
            return null;
        }
    }

    /// <summary>Interpreta y valida los activos del préstamo a partir del texto ingresado.</summary>
    /// <param name="text">Texto que se muestra, interpreta o transforma.</param>
    /// <param name="assets">Activos disponibles usados para validar series y destinos.</param>
    /// <param name="condition">Condición declarada para los activos del préstamo.</param>
    /// <returns>Colección de registros u opciones obtenida por la operación.</returns>
    /// <exception cref="FormatException">Se produce cuando una respuesta o estado inválido impide completar la operación.</exception>
    private static IReadOnlyList<LoanAssetRequest>? ParseLoanAssets(
        string text,
        IReadOnlyList<TraceAssetResponse> assets,
        string condition
    )
    {
        try
        {
            return
                text.Split(
                        ';',
                        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                    )
                    .Select(value =>
                    {
                        var parts = value.Split(':', StringSplitOptions.TrimEntries);
                        var asset =
                            parts.Length == 2
                                ? assets.FirstOrDefault(x =>
                                    x.Code.Equals(parts[0], StringComparison.OrdinalIgnoreCase)
                                    && x.StatusCode == "DISPONIBLE"
                                )
                                : null;
                        return
                            asset is not null
                            && decimal.TryParse(
                                parts[1],
                                NumberStyles.Number,
                                CultureInfo.CurrentCulture,
                                out var quantity
                            )
                            && quantity >= 0
                            ? new LoanAssetRequest(
                                asset.AssetId,
                                asset.ContentLotId,
                                quantity,
                                condition
                            )
                            : throw new FormatException();
                    })
                    .ToList()
                    is { Count: > 0 } result
                ? result
                : throw new FormatException();
        }
        catch (FormatException)
        {
            Error(Localization.Text("Traceability_InvalidLoanAssets"));
            return null;
        }
    }

    /// <summary>Intenta interpretar una cantidad decimal con la cultura activa.</summary>
    /// <param name="value">Texto que se normaliza, valida o asigna.</param>
    /// <param name="result">Variable de salida que recibe el valor interpretado.</param>
    /// <returns>Verdadero cuando se cumple la condición evaluada; en caso contrario, falso.</returns>
    private static bool Number(string value, out decimal result)
    {
        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out result))
            return true;
        Error(Localization.Text("Sales_InvalidNumber"));
        return false;
    }

    /// <summary>Crea una grilla con el estilo y comportamiento común del módulo de trazabilidad.</summary>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private static DataGridView Grid()
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AutoGenerateColumns = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
        };
        UiTheme.Grid(grid);
        return grid;
    }

    /// <summary>Actualiza las grillas y detalles de trazabilidad con la información cargada.</summary>
    /// <param name="grid">Grilla que se configura, consulta o actualiza.</param>
    /// <param name="items">Registros u opciones que se presentan en el control.</param>
    /// <param name="hidden">Nombres de propiedades técnicas que no deben mostrarse.</param>
    private static void Bind<T>(DataGridView grid, IReadOnlyList<T> items, params string[] hidden)
    {
        grid.DataSource = null;
        grid.DataSource = items.ToList();
        foreach (var name in hidden)
            if (grid.Columns[name] is { } c)
                c.Visible = false;
        Phase7Headers.Apply(grid);
    }

    /// <summary>Obtiene el registro actualmente seleccionado en la grilla.</summary>
    /// <param name="grid">Grilla que se configura, consulta o actualiza.</param>
    /// <returns>Objeto construido u obtenido por la operación.</returns>
    private static T? Selected<T>(DataGridView grid)
        where T : class => grid.CurrentRow?.DataBoundItem as T;

    /// <summary>Crea una pestaña localizada y agrega el contenido indicado.</summary>
    /// <param name="key">Clave de recurso o campo que identifica el texto o control.</param>
    /// <param name="control">Control visual asociado al campo o sección.</param>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private static TabPage Page(string key, Control control)
    {
        var page = new TabPage(Localization.Text(key));
        control.Dock = DockStyle.Fill;
        page.Controls.Add(control);
        return page;
    }

    /// <summary>Muestra un error funcional sin exponer detalles técnicos.</summary>
    /// <param name="message">Mensaje funcional que se muestra al usuario.</param>
    private static void Error(string message) =>
        MessageBox.Show(message, "OxiTigre", MessageBoxButtons.OK, MessageBoxIcon.Warning);
}
