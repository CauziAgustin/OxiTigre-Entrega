/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.WinForms.InventoryManagementForm
Archivo: InventoryManagementForm.cs | Versión: 1.2.0 | Fecha: 2026-08-26 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Administra maestros, saldos, alertas y movimientos confirmados de Inventario.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-21 | FABRICA | Agustin Omar Cauzi | Ayuda funcional y stock reservado/disponible visible.
Historial: 1.1.1 | 2026-08-21 | FABRICA | Agustin Omar Cauzi | Corrección del formato nativo de columnas booleanas.
Historial: 1.1.2 | 2026-08-24 | FABRICA | Agustin Omar Cauzi | Conservación de la configuración de trazabilidad al editar.
Historial: 1.1.3 | 2026-08-26 | FABRICA | Agustin Omar Cauzi | Pestañas y búsquedas homogéneas.
Historial: 1.2.0 | 2026-08-26 | FABRICA | Agustin Omar Cauzi | Filtros guiados, placeholders y secciones relacionadas más próximas.
===============================================================================
*/
using System.Globalization;
using OxiTigre.ApiClient;
using OxiTigre.Contracts.Inventory;
using OxiTigre.Contracts.Security;

namespace OxiTigre.WinForms;

/// <summary>Presenta la Fase 5 en una única ventana nativa, adaptable y autorizada.</summary>
internal sealed class InventoryManagementForm : Form
{
    private readonly OxiTigreApiClient _apiClient;
    private readonly LoginResponse _session;
    private readonly bool _canManage;
    private InventorySnapshotResponse? _data;
    private readonly DataGridView _dgvProducts = Grid();
    private readonly DataGridView _dgvWarehouses = Grid();
    private readonly DataGridView _dgvLocations = Grid();
    private readonly DataGridView _dgvCategories = Grid();
    private readonly DataGridView _dgvUnits = Grid();
    private readonly DataGridView _dgvStock = Grid();
    private readonly DataGridView _dgvMovements = Grid();

    /// <summary>Inicializa Inventario con permisos derivados de la sesión autenticada.</summary>
    /// <param name="apiClient">Cliente usado para consultar y modificar el inventario.</param>
    /// <param name="session">Sesión autenticada que delimita empresa, sucursal y permisos.</param>
    internal InventoryManagementForm(OxiTigreApiClient apiClient, LoginResponse session)
    {
        _apiClient = apiClient;
        _session = session;
        _canManage = session.Permissions.Contains(
            "INVENTARIO.GESTIONAR",
            StringComparer.OrdinalIgnoreCase
        );
        Text = Localization.Text("Inventory_Title");
        Width = 1280;
        Height = 790;
        MinimumSize = new Size(1024, 650);
        StartPosition = FormStartPosition.CenterParent;
        UiTheme.Apply(this);
        var tabs = new TabControl { Dock = DockStyle.Fill };
        UiTheme.Tabs(tabs);
        tabs.TabPages.Add(
            Tab(
                Localization.Text("Inventory_Products"),
                Section(
                    Localization.Text("Inventory_Products"),
                    _dgvProducts,
                    (
                        Localization.Text("Inventory_NewProduct"),
                        _canManage,
                        () => EditProductAsync(null)
                    ),
                    (
                        Localization.Text("Common_Edit"),
                        _canManage,
                        () => EditSelectedAsync<ProductResponse>(_dgvProducts, EditProductAsync)
                    )
                )
            )
        );
        tabs.TabPages.Add(Tab(Localization.Text("Inventory_Structure"), StructureTab()));
        tabs.TabPages.Add(Tab(Localization.Text("Inventory_Catalogs"), CatalogsTab()));
        tabs.TabPages.Add(
            Tab(
                Localization.Text("Inventory_Stock"),
                Section(
                    Localization.Text("Inventory_Stock"),
                    _dgvStock,
                    (Localization.Text("Inventory_UpdateMinimum"), _canManage, EditMinimumAsync)
                )
            )
        );
        tabs.TabPages.Add(
            Tab(
                Localization.Text("Inventory_Movements"),
                Section(
                    Localization.Text("Inventory_Movements"),
                    _dgvMovements,
                    (Localization.Text("Inventory_NewMovement"), _canManage, NewMovementAsync)
                )
            )
        );
        Controls.Add(tabs);
        Controls.Add(InformationBar("Inventory_Overview"));
        Shown += async (_, _) => await LoadAsync();
    }

    /// <summary>Construye la pestaña de depósitos y ubicaciones.</summary>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private Control StructureTab()
    {
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 280,
        };
        split.Panel1.Controls.Add(
            Section(
                Localization.Text("Inventory_Warehouses"),
                _dgvWarehouses,
                (Localization.Text("Common_New"), _canManage, () => EditWarehouseAsync(null)),
                (
                    Localization.Text("Common_Edit"),
                    _canManage,
                    () => EditSelectedAsync<WarehouseResponse>(_dgvWarehouses, EditWarehouseAsync)
                )
            )
        );
        split.Panel2.Controls.Add(
            Section(
                Localization.Text("Inventory_Locations"),
                _dgvLocations,
                (Localization.Text("Common_New"), _canManage, () => EditLocationAsync(null)),
                (
                    Localization.Text("Common_Edit"),
                    _canManage,
                    () =>
                        EditSelectedAsync<WarehouseLocationResponse>(
                            _dgvLocations,
                            EditLocationAsync
                        )
                )
            )
        );
        return split;
    }

    /// <summary>Construye la pestaña de categorías y unidades de medida.</summary>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private Control CatalogsTab()
    {
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 280,
        };
        split.Panel1.Controls.Add(
            Section(
                Localization.Text("Inventory_Categories"),
                _dgvCategories,
                (Localization.Text("Common_New"), _canManage, () => EditCategoryAsync(null)),
                (
                    Localization.Text("Common_Edit"),
                    _canManage,
                    () =>
                        EditSelectedAsync<ProductCategoryResponse>(
                            _dgvCategories,
                            EditCategoryAsync
                        )
                )
            )
        );
        split.Panel2.Controls.Add(
            Section(
                Localization.Text("Inventory_Units"),
                _dgvUnits,
                (Localization.Text("Common_New"), _canManage, () => EditUnitAsync(null)),
                (
                    Localization.Text("Common_Edit"),
                    _canManage,
                    () => EditSelectedAsync<MeasurementUnitResponse>(_dgvUnits, EditUnitAsync)
                )
            )
        );
        return split;
    }

    /// <summary>Carga  y actualiza la interfaz con los datos obtenidos.</summary>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private async Task LoadAsync()
    {
        try
        {
            _data = await _apiClient.GetInventoryAsync(_session.Token);
            Bind(
                _dgvProducts,
                _data.Products,
                "ProductId",
                "ProductCategoryId",
                "MeasurementUnitId",
                "RowVersion",
                "Description"
            );
            Bind(
                _dgvWarehouses,
                _data.Warehouses,
                "WarehouseId",
                "BranchId",
                "RowVersion",
                "Description"
            );
            Bind(
                _dgvLocations,
                _data.Locations,
                "LocationId",
                "WarehouseId",
                "RowVersion",
                "Description"
            );
            Bind(_dgvCategories, _data.Categories, "ProductCategoryId", "RowVersion");
            Bind(_dgvUnits, _data.MeasurementUnits, "MeasurementUnitId", "RowVersion");
            Bind(
                _dgvStock,
                _data.Stock,
                "StockBalanceId",
                "ProductId",
                "WarehouseId",
                "RowVersion"
            );
            if (_dgvStock.Columns["Quantity"] is { } physicalStock)
                physicalStock.HeaderText = Localization.Text("Inventory_PhysicalStock");
            Bind(
                _dgvMovements,
                _data.Movements,
                "MovementId",
                "MovementLineId",
                "ProductId",
                "OriginWarehouseId",
                "OriginLocationId",
                "DestinationWarehouseId",
                "DestinationLocationId",
                "Observation"
            );
            foreach (DataGridViewRow row in _dgvStock.Rows)
                if (row.DataBoundItem is StockBalanceResponse { IsBelowMinimum: true })
                    row.DefaultCellStyle.BackColor = Color.MistyRose;
        }
        catch (HttpRequestException exception)
        {
            ShowError(Localization.Format("Inventory_LoadFailed", exception.Message));
        }
    }

    /// <summary>Abre el editor correspondiente a unit y conserva los cambios confirmados.</summary>
    /// <param name="item">Registro existente que se edita, o nulo para crear uno nuevo.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private async Task EditUnitAsync(MeasurementUnitResponse? item)
    {
        using var dialog = Editor(
            Localization.Text("Inventory_Units"),
            TextField("Code", "Config_Code", item?.Code, true, item is not null),
            TextField("Name", "Config_Name", item?.Name, true),
            TextField("Symbol", "Inventory_Symbol", item?.Symbol, true),
            ChoiceField(
                "Allows",
                "Inventory_AllowsDecimals",
                item?.AllowsDecimals == false ? "false" : "true",
                BoolChoices()
            ),
            ChoiceField("Status", "Common_Status", item?.StatusCode ?? "ACTIVO", StatusChoices())
        );
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;
        await RunAsync(() =>
            _apiClient.SaveMeasurementUnitAsync(
                _session.Token,
                new(
                    item?.MeasurementUnitId,
                    dialog["Code"],
                    dialog["Name"],
                    dialog["Symbol"],
                    dialog["Allows"] == "true",
                    dialog["Status"],
                    item?.RowVersion
                )
            )
        );
    }

    /// <summary>Abre el editor correspondiente a category y conserva los cambios confirmados.</summary>
    /// <param name="item">Registro existente que se edita, o nulo para crear uno nuevo.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private async Task EditCategoryAsync(ProductCategoryResponse? item)
    {
        using var dialog = Editor(
            Localization.Text("Inventory_Categories"),
            TextField("Code", "Config_Code", item?.Code, true, item is not null),
            TextField("Name", "Config_Name", item?.Name, true),
            TextField("Description", "Config_Description", item?.Description, false, false, true),
            ChoiceField("Status", "Common_Status", item?.StatusCode ?? "ACTIVO", StatusChoices())
        );
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;
        await RunAsync(() =>
            _apiClient.SaveProductCategoryAsync(
                _session.Token,
                new(
                    item?.ProductCategoryId,
                    dialog["Code"],
                    dialog["Name"],
                    dialog["Description"],
                    dialog["Status"],
                    item?.RowVersion
                )
            )
        );
    }

    /// <summary>Abre el editor correspondiente a product y conserva los cambios confirmados.</summary>
    /// <param name="item">Registro existente que se edita, o nulo para crear uno nuevo.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private async Task EditProductAsync(ProductResponse? item)
    {
        if (_data is null)
            return;
        var categories = _data
            .Categories.Where(value =>
                value.StatusCode == "ACTIVO" || value.ProductCategoryId == item?.ProductCategoryId
            )
            .Select(value => new Choice(
                value.ProductCategoryId.ToString(CultureInfo.InvariantCulture),
                value.Name
            ))
            .ToList();
        var units = _data
            .MeasurementUnits.Where(value =>
                value.StatusCode == "ACTIVO" || value.MeasurementUnitId == item?.MeasurementUnitId
            )
            .Select(value => new Choice(
                value.MeasurementUnitId.ToString(CultureInfo.InvariantCulture),
                $"{value.Name} ({value.Symbol})"
            ))
            .ToList();
        if (categories.Count == 0 || units.Count == 0)
        {
            ShowError(Localization.Text("Common_Required"));
            return;
        }
        using var dialog = Editor(
            Localization.Text(item is null ? "Inventory_NewProduct" : "Inventory_Products"),
            ChoiceField(
                "Category",
                "Inventory_Category",
                item?.ProductCategoryId.ToString(CultureInfo.InvariantCulture)
                    ?? categories[0].Value,
                categories
            ),
            ChoiceField(
                "Unit",
                "Inventory_Unit",
                item?.MeasurementUnitId.ToString(CultureInfo.InvariantCulture) ?? units[0].Value,
                units
            ),
            ChoiceField(
                "ItemType",
                "Inventory_ItemType",
                item?.ItemType ?? "PRODUCTO",
                [
                    new("PRODUCTO", Localization.Text("Inventory_ItemProduct")),
                    new("SERVICIO", Localization.Text("Inventory_ItemService")),
                ]
            ),
            TextField("Name", "Config_Name", item?.Name, true),
            TextField("Barcode", "Inventory_Barcode", item?.Barcode),
            TextField("Description", "Config_Description", item?.Description, false, false, true),
            ChoiceField("Status", "Common_Status", item?.StatusCode ?? "ACTIVO", StatusChoices())
        );
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;
        var isService = dialog["ItemType"] == "SERVICIO";
        await RunAsync(() =>
            _apiClient.SaveProductAsync(
                _session.Token,
                new(
                    item?.ProductId,
                    long.Parse(dialog["Category"], CultureInfo.InvariantCulture),
                    long.Parse(dialog["Unit"], CultureInfo.InvariantCulture),
                    dialog["Name"],
                    dialog["Description"],
                    dialog["Barcode"],
                    dialog["Status"],
                    item?.RowVersion,
                    dialog["ItemType"],
                    isService ? "NINGUNA" : item?.TrackingType ?? "NINGUNA",
                    !isService && item?.IsReusable == true,
                    !isService && item?.AllowsLoans == true,
                    !isService && item?.RequiresMaintenance == true,
                    !isService && item?.AllowsMeasurements == true
                )
            )
        );
    }

    /// <summary>Abre el editor correspondiente a warehouse y conserva los cambios confirmados.</summary>
    /// <param name="item">Registro existente que se edita, o nulo para crear uno nuevo.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private async Task EditWarehouseAsync(WarehouseResponse? item)
    {
        using var dialog = Editor(
            Localization.Text("Inventory_Warehouses"),
            TextField("Code", "Config_Code", item?.Code, true, item is not null),
            TextField("Name", "Config_Name", item?.Name, true),
            TextField("Address", "Inventory_Address", item?.Address),
            TextField("Description", "Config_Description", item?.Description, false, false, true),
            ChoiceField("Status", "Common_Status", item?.StatusCode ?? "ACTIVO", StatusChoices())
        );
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;
        await RunAsync(() =>
            _apiClient.SaveWarehouseAsync(
                _session.Token,
                new(
                    item?.WarehouseId,
                    item?.BranchId,
                    dialog["Code"],
                    dialog["Name"],
                    dialog["Address"],
                    dialog["Description"],
                    dialog["Status"],
                    item?.RowVersion
                )
            )
        );
    }

    /// <summary>Abre el editor correspondiente a location y conserva los cambios confirmados.</summary>
    /// <param name="item">Registro existente que se edita, o nulo para crear uno nuevo.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private async Task EditLocationAsync(WarehouseLocationResponse? item)
    {
        if (_data is null)
            return;
        var warehouses = _data
            .Warehouses.Where(value =>
                value.StatusCode == "ACTIVO" || value.WarehouseId == item?.WarehouseId
            )
            .Select(value => new Choice(
                value.WarehouseId.ToString(CultureInfo.InvariantCulture),
                value.Name
            ))
            .ToList();
        if (warehouses.Count == 0)
        {
            ShowError(Localization.Text("Common_Required"));
            return;
        }
        using var dialog = Editor(
            Localization.Text("Inventory_Locations"),
            ChoiceField(
                "Warehouse",
                "Inventory_Warehouse",
                item?.WarehouseId.ToString(CultureInfo.InvariantCulture) ?? warehouses[0].Value,
                warehouses
            ),
            TextField("Code", "Config_Code", item?.Code, true, item is not null),
            TextField("Name", "Config_Name", item?.Name, true),
            TextField("Description", "Config_Description", item?.Description, false, false, true),
            ChoiceField("Status", "Common_Status", item?.StatusCode ?? "ACTIVO", StatusChoices())
        );
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;
        await RunAsync(() =>
            _apiClient.SaveWarehouseLocationAsync(
                _session.Token,
                new(
                    item?.LocationId,
                    long.Parse(dialog["Warehouse"], CultureInfo.InvariantCulture),
                    dialog["Code"],
                    dialog["Name"],
                    dialog["Description"],
                    dialog["Status"],
                    item?.RowVersion
                )
            )
        );
    }

    /// <summary>Abre el editor correspondiente a minimum y conserva los cambios confirmados.</summary>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private async Task EditMinimumAsync()
    {
        var item = Selected<StockBalanceResponse>(_dgvStock);
        if (item is null)
        {
            ShowError(Localization.Text("Common_SelectRecord"));
            return;
        }
        using var dialog = Editor(
            Localization.Text("Inventory_UpdateMinimum"),
            TextField(
                "Minimum",
                "Inventory_Minimum",
                item.MinimumStock.ToString(CultureInfo.CurrentCulture),
                true
            )
        );
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;
        if (
            !decimal.TryParse(
                dialog["Minimum"],
                NumberStyles.Number,
                CultureInfo.CurrentCulture,
                out var minimum
            )
            || minimum < 0
        )
        {
            ShowError(Localization.Text("Inventory_InvalidNumber"));
            return;
        }
        await RunAsync(async () =>
        {
            await _apiClient.UpdateMinimumStockAsync(
                _session.Token,
                item.StockBalanceId,
                new(minimum, item.RowVersion)
            );
            return new SavedInventoryResponse(item.StockBalanceId);
        });
    }

    /// <summary>Inicia la creación de movement.</summary>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private async Task NewMovementAsync()
    {
        if (_data is null)
            return;
        using var dialog = new InventoryMovementForm(_apiClient, _session.Token, _data);
        if (dialog.ShowDialog(this) == DialogResult.OK)
            await LoadAsync();
    }

    /// <summary>Ejecuta una operación, informa el resultado y traduce errores funcionales.</summary>
    /// <param name="action">Operación que se ejecuta cuando el usuario confirma la acción.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private async Task RunAsync(Func<Task<SavedInventoryResponse>> action)
    {
        try
        {
            await action();
            MessageBox.Show(
                this,
                Localization.Text("Inventory_Saved"),
                "OxiTigre",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
            await LoadAsync();
        }
        catch (HttpRequestException exception)
        {
            ShowError(exception.Message);
        }
    }

    /// <summary>Crea el editor modal usado por el módulo de inventario.</summary>
    /// <param name="title">Título visible de la ventana, pestaña o sección.</param>
    /// <param name="fields">Campos que componen el editor.</param>
    /// <returns>Objeto construido u obtenido por la operación.</returns>
    private static InventoryEditorForm Editor(string title, params InventoryField[] fields) =>
        new(title, fields);

    /// <summary>Crea la definición de un campo de texto para el editor.</summary>
    /// <param name="key">Clave de recurso o campo que identifica el texto o control.</param>
    /// <param name="labelKey">Clave de recurso usada como etiqueta visible.</param>
    /// <param name="value">Texto que se normaliza, valida o asigna.</param>
    /// <param name="required">Indica si el campo debe completarse para guardar.</param>
    /// <param name="readOnly">Indica si el usuario puede modificar el campo.</param>
    /// <param name="multiline">Indica si el editor admite varias líneas.</param>
    /// <returns>Objeto construido u obtenido por la operación.</returns>
    private static InventoryField TextField(
        string key,
        string labelKey,
        string? value,
        bool required = false,
        bool readOnly = false,
        bool multiline = false
    ) =>
        new(
            key,
            Localization.Text(labelKey),
            value ?? string.Empty,
            required,
            readOnly,
            multiline,
            null
        );

    /// <summary>Crea la definición de un campo de selección con sus opciones permitidas.</summary>
    /// <param name="key">Clave de recurso o campo que identifica el texto o control.</param>
    /// <param name="labelKey">Clave de recurso usada como etiqueta visible.</param>
    /// <param name="value">Texto que se normaliza, valida o asigna.</param>
    /// <param name="choices">Opciones visibles admitidas por el campo.</param>
    /// <returns>Objeto construido u obtenido por la operación.</returns>
    private static InventoryField ChoiceField(
        string key,
        string labelKey,
        string value,
        IReadOnlyList<Choice> choices
    ) => new(key, Localization.Text(labelKey), value, true, false, false, choices);

    /// <summary>Devuelve los estados que pueden seleccionarse en inventario.</summary>
    /// <returns>Colección de registros u opciones obtenida por la operación.</returns>
    private static IReadOnlyList<Choice> StatusChoices() =>
        [
            new("ACTIVO", Localization.Text("Common_Active")),
            new("INACTIVO", Localization.Text("Common_Inactive")),
        ];

    /// <summary>Devuelve las opciones localizadas para valores booleanos.</summary>
    /// <returns>Colección de registros u opciones obtenida por la operación.</returns>
    private static IReadOnlyList<Choice> BoolChoices() =>
        [
            new("true", Localization.Text("Common_Yes")),
            new("false", Localization.Text("Common_No")),
        ];

    /// <summary>Crea una pestaña localizada y agrega el contenido indicado.</summary>
    /// <param name="title">Título visible de la ventana, pestaña o sección.</param>
    /// <param name="content">Control visual que se incorpora a la pestaña o sección.</param>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private static TabPage Tab(string title, Control content)
    {
        var tab = new TabPage(title) { Padding = new Padding(8), BackColor = UiTheme.Background };
        tab.Controls.Add(content);
        return tab;
    }

    /// <summary>Crea una ayuda contextual localizada para la sección.</summary>
    /// <param name="key">Clave de recurso o campo que identifica el texto o control.</param>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private static Label InformationBar(string key) =>
        new()
        {
            Text = Localization.Text(key),
            Dock = DockStyle.Top,
            Height = 48,
            Padding = new Padding(14, 12, 14, 8),
            BackColor = Color.FromArgb(230, 244, 234),
            ForeColor = UiTheme.Primary,
            AutoEllipsis = true,
        };

    /// <summary>Crea una grilla con el estilo y comportamiento común del módulo de inventario.</summary>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private static DataGridView Grid()
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AutoGenerateColumns = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        };
        grid.CellFormatting += (_, args) =>
            args.Value = args.Value switch
            {
                "ACTIVO" => Localization.Text("Common_Active"),
                "INACTIVO" => Localization.Text("Common_Inactive"),
                "CONFIRMADO" => Localization.Text("Common_Confirmed"),
                "ENTRADA" => Localization.Text("Inventory_Entry"),
                "SALIDA" => Localization.Text("Inventory_Exit"),
                "TRANSFERENCIA" => Localization.Text("Inventory_Transfer"),
                "AJUSTE_ENTRADA" => Localization.Text("Inventory_AdjustmentEntry"),
                "AJUSTE_SALIDA" => Localization.Text("Inventory_AdjustmentExit"),
                "PRODUCTO" => Localization.Text("Inventory_ItemProduct"),
                "SERVICIO" => Localization.Text("Inventory_ItemService"),
                _ => args.Value,
            };
        UiTheme.Grid(grid);
        return grid;
    }

    /// <summary>Crea una sección titulada y organiza su contenido y acciones.</summary>
    /// <param name="title">Título visible de la ventana, pestaña o sección.</param>
    /// <param name="grid">Grilla que se configura, consulta o actualiza.</param>
    /// <param name="actions">Acciones disponibles en la barra de herramientas.</param>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private static Control Section(
        string title,
        DataGridView grid,
        params (string Text, bool Enabled, Func<Task> Action)[] actions
    )
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48 };
        toolbar.Controls.Add(
            new Label
            {
                Text = title,
                AutoSize = true,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = UiTheme.Primary,
                Margin = new Padding(0, 9, 20, 0),
            }
        );
        foreach (var action in actions)
        {
            var button = UiTheme.Button(action.Text, 0, 0, 145);
            button.Enabled = action.Enabled;
            button.Click += async (_, _) => await action.Action();
            toolbar.Controls.Add(button);
        }
        panel.Controls.Add(UiTheme.Searchable(grid));
        panel.Controls.Add(toolbar);
        return panel;
    }

    /// <summary>Actualiza las grillas y detalles de inventario con la información cargada.</summary>
    /// <param name="grid">Grilla que se configura, consulta o actualiza.</param>
    /// <param name="values">Valores que se vinculan con el control.</param>
    /// <param name="hidden">Nombres de propiedades técnicas que no deben mostrarse.</param>
    private static void Bind<T>(DataGridView grid, IReadOnlyList<T> values, params string[] hidden)
    {
        grid.DataSource = values.ToList();
        foreach (var name in hidden)
            if (grid.Columns[name] is { } column)
                column.Visible = false;
        foreach (DataGridViewColumn column in grid.Columns)
            column.HeaderText = Header(column.Name);
    }

    /// <summary>Resuelve el encabezado localizado de una columna o propiedad.</summary>
    /// <param name="name">Nombre técnico del elemento solicitado.</param>
    /// <returns>Texto normalizado, localizado o formateado por la operación.</returns>
    private static string Header(string name) =>
        name switch
        {
            "Code" or "ProductCode" or "MovementCode" => Localization.Text("Grid_Code"),
            "Name" => Localization.Text("Config_Name"),
            "Category" => Localization.Text("Inventory_Category"),
            "UnitSymbol" => Localization.Text("Inventory_Unit"),
            "Symbol" => Localization.Text("Inventory_Symbol"),
            "Description" => Localization.Text("Config_Description"),
            "Barcode" => Localization.Text("Inventory_Barcode"),
            "ItemType" => Localization.Text("Inventory_ItemType"),
            "StatusCode" => Localization.Text("Common_Status"),
            "Warehouse" => Localization.Text("Inventory_Warehouse"),
            "OriginWarehouse" => Localization.Text("Inventory_Origin"),
            "DestinationWarehouse" => Localization.Text("Inventory_Destination"),
            "Address" => Localization.Text("Inventory_Address"),
            "Quantity" => Localization.Text("Inventory_Quantity"),
            "ReservedQuantity" => Localization.Text("Inventory_ReservedStock"),
            "AvailableQuantity" => Localization.Text("Inventory_AvailableStock"),
            "MinimumStock" => Localization.Text("Inventory_Minimum"),
            "IsBelowMinimum" => Localization.Text("Inventory_BelowMinimum"),
            "MovementType" => Localization.Text("Inventory_MovementType"),
            "MovementDateUtc" => Localization.Text("Inventory_Date"),
            "Product" => Localization.Text("Inventory_Product"),
            "OriginLocation" => Localization.Text("Inventory_Location"),
            "DestinationLocation" => Localization.Text("Inventory_Location"),
            "AllowsDecimals" => Localization.Text("Inventory_AllowsDecimals"),
            _ => name,
        };

    /// <summary>Obtiene el registro actualmente seleccionado en la grilla.</summary>
    /// <param name="grid">Grilla que se configura, consulta o actualiza.</param>
    /// <returns>Objeto construido u obtenido por la operación.</returns>
    private static T? Selected<T>(DataGridView grid)
        where T : class => grid.CurrentRow?.DataBoundItem as T;

    /// <summary>Abre el editor correspondiente a selected y conserva los cambios confirmados.</summary>
    /// <param name="grid">Grilla que se configura, consulta o actualiza.</param>
    /// <param name="edit">Operación usada para editar el registro seleccionado.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private Task EditSelectedAsync<T>(DataGridView grid, Func<T?, Task> edit)
        where T : class
    {
        var item = Selected<T>(grid);
        if (item is not null)
            return edit(item);
        ShowError(Localization.Text("Common_SelectRecord"));
        return Task.CompletedTask;
    }

    /// <summary>Muestra un error funcional sin exponer detalles técnicos.</summary>
    /// <param name="message">Mensaje funcional que se muestra al usuario.</param>
    private void ShowError(string message) =>
        MessageBox.Show(this, message, "OxiTigre", MessageBoxButtons.OK, MessageBoxIcon.Error);

    internal sealed record Choice(string Value, string Label);

    private sealed record InventoryField(
        string Key,
        string Label,
        string Value,
        bool Required,
        bool ReadOnly,
        bool Multiline,
        IReadOnlyList<Choice>? Choices
    );

    private sealed class InventoryEditorForm : Form
    {
        private readonly IReadOnlyList<InventoryField> _fields;
        private readonly Dictionary<string, Control> _controls = new(StringComparer.Ordinal);
        internal string this[string key] =>
            _controls[key] is ComboBox combo
                ? Convert.ToString(combo.SelectedValue, CultureInfo.InvariantCulture)
                    ?? string.Empty
                : ((TextBox)_controls[key]).Text.Trim();

        /// <summary>Inicializa el componente de inventario con sus dependencias y datos de trabajo.</summary>
        /// <param name="title">Título visible de la ventana, pestaña o sección.</param>
        /// <param name="fields">Campos que componen el editor.</param>
        internal InventoryEditorForm(string title, IReadOnlyList<InventoryField> fields)
        {
            _fields = fields;
            Text = title;
            Width = 640;
            Height = Math.Min(760, 145 + fields.Sum(field => field.Multiline ? 105 : 52));
            MinimumSize = new Size(540, 300);
            StartPosition = FormStartPosition.CenterParent;
            UiTheme.Apply(this);
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(18),
                AutoScroll = true,
                ColumnCount = 2,
                RowCount = fields.Count + 1,
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (var index = 0; index < fields.Count; index++)
            {
                var field = fields[index];
                Control control;
                if (field.Choices is not null)
                {
                    var combo = new ComboBox
                    {
                        DropDownStyle = ComboBoxStyle.DropDownList,
                        DataSource = field.Choices.ToList(),
                        DisplayMember = nameof(Choice.Label),
                        ValueMember = nameof(Choice.Value),
                        Enabled = !field.ReadOnly,
                    };
                    combo.SelectedValue = field.Value;
                    control = combo;
                }
                else
                    control = new TextBox
                    {
                        Text = field.Value,
                        ReadOnly = field.ReadOnly,
                        Multiline = field.Multiline,
                        Height = field.Multiline ? 75 : 27,
                        ScrollBars = field.Multiline ? ScrollBars.Vertical : ScrollBars.None,
                        PlaceholderText = field.Label,
                    };
                control.Dock = DockStyle.Fill;
                control.Margin = new Padding(3, 5, 3, 5);
                _controls[field.Key] = control;
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, field.Multiline ? 90 : 45));
                layout.Controls.Add(
                    new Label
                    {
                        Text = field.Label,
                        AutoSize = true,
                        Anchor = AnchorStyles.Left,
                    },
                    0,
                    index
                );
                layout.Controls.Add(control, 1, index);
            }
            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
            };
            var save = UiTheme.Button(Localization.Text("Common_Save"), 0, 0, 130);
            save.Click += Save_Click;
            var cancel = UiTheme.Button(Localization.Text("Common_Cancel"), 0, 0, 130);
            cancel.DialogResult = DialogResult.Cancel;
            buttons.Controls.Add(save);
            buttons.Controls.Add(cancel);
            layout.Controls.Add(buttons, 1, fields.Count);
            Controls.Add(layout);
            AcceptButton = save;
            CancelButton = cancel;
        }

        /// <summary>Procesa la acción de save solicitada desde la interfaz.</summary>
        /// <param name="sender">Control que originó el evento.</param>
        /// <param name="e">Datos asociados al evento de la interfaz.</param>
        private void Save_Click(object? sender, EventArgs e)
        {
            if (_fields.Any(field => field.Required && string.IsNullOrWhiteSpace(this[field.Key])))
            {
                MessageBox.Show(
                    this,
                    Localization.Text("Common_Required"),
                    "OxiTigre",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}

/// <summary>Permite componer y confirmar un movimiento con uno o más renglones.</summary>
internal sealed class InventoryMovementForm : Form
{
    private readonly OxiTigreApiClient _apiClient;
    private readonly string _token;
    private readonly InventorySnapshotResponse _data;
    private readonly ComboBox _cmbType = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = 210,
    };
    private readonly DateTimePicker _dtpDate = new()
    {
        Format = DateTimePickerFormat.Custom,
        CustomFormat = "dd/MM/yyyy HH:mm",
        Width = 170,
    };
    private readonly TextBox _txtObservation = new() { Width = 350 };
    private readonly DataGridView _dgvLines = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AutoGenerateColumns = true,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
    };
    private readonly List<MovementLineDraft> _lines = [];

    /// <summary>Inicializa el componente de inventario con sus dependencias y datos de trabajo.</summary>
    /// <param name="apiClient">Cliente usado para comunicarse con la API.</param>
    /// <param name="token">Token de la sesión autenticada.</param>
    /// <param name="data">Datos cargados que alimentan la pantalla.</param>
    internal InventoryMovementForm(
        OxiTigreApiClient apiClient,
        string token,
        InventorySnapshotResponse data
    )
    {
        _apiClient = apiClient;
        _token = token;
        _data = data;
        Text = Localization.Text("Inventory_NewMovement");
        Width = 1120;
        Height = 680;
        MinimumSize = new Size(900, 580);
        StartPosition = FormStartPosition.CenterParent;
        UiTheme.Apply(this);
        _cmbType.DataSource = new[]
        {
            new InventoryManagementForm.Choice("ENTRADA", Localization.Text("Inventory_Entry")),
            new("SALIDA", Localization.Text("Inventory_Exit")),
            new("TRANSFERENCIA", Localization.Text("Inventory_Transfer")),
            new("AJUSTE_ENTRADA", Localization.Text("Inventory_AdjustmentEntry")),
            new("AJUSTE_SALIDA", Localization.Text("Inventory_AdjustmentExit")),
        };
        _cmbType.DisplayMember = "Label";
        _cmbType.ValueMember = "Value";
        var header = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 65,
            Padding = new Padding(15, 14, 0, 0),
        };
        AddLabeled(header, "Inventory_MovementType", _cmbType);
        AddLabeled(header, "Inventory_Date", _dtpDate);
        AddLabeled(header, "Inventory_Observation", _txtObservation);
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 52,
            Padding = new Padding(15, 7, 0, 0),
        };
        var add = UiTheme.Button(Localization.Text("Inventory_AddLine"), 0, 0, 155);
        add.Click += (_, _) => AddLine();
        var remove = UiTheme.Button(Localization.Text("Inventory_RemoveLine"), 0, 0, 155);
        remove.Click += (_, _) => RemoveLine();
        var confirm = UiTheme.Button(Localization.Text("Inventory_ConfirmMovement"), 0, 0, 180);
        confirm.Click += async (_, _) => await ConfirmAsync();
        toolbar.Controls.AddRange([add, remove, confirm]);
        UiTheme.Grid(_dgvLines);
        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15) };
        body.Controls.Add(_dgvLines);
        Controls.Add(body);
        Controls.Add(toolbar);
        Controls.Add(header);
    }

    /// <summary>Agrega line al documento o control actual.</summary>
    private void AddLine()
    {
        var products = _data
            .Products.Where(item => item.StatusCode == "ACTIVO")
            .Select(item => new InventoryManagementForm.Choice(
                item.ProductId.ToString(CultureInfo.InvariantCulture),
                $"{item.Code} · {item.Name}"
            ))
            .ToList();
        var warehouses = _data
            .Warehouses.Where(item => item.StatusCode == "ACTIVO")
            .Select(item => new InventoryManagementForm.Choice(
                item.WarehouseId.ToString(CultureInfo.InvariantCulture),
                item.Name
            ))
            .ToList();
        if (products.Count == 0 || warehouses.Count == 0)
        {
            ShowError(Localization.Text("Common_Required"));
            return;
        }
        var type =
            Convert.ToString(_cmbType.SelectedValue, CultureInfo.InvariantCulture) ?? "ENTRADA";
        var outgoing = type is "SALIDA" or "TRANSFERENCIA" or "AJUSTE_SALIDA";
        var incoming = type is "ENTRADA" or "TRANSFERENCIA" or "AJUSTE_ENTRADA";
        var fields = new List<LineField>
        {
            new("Product", Localization.Text("Inventory_Product"), products),
            new("Quantity", Localization.Text("Inventory_Quantity"), null),
        };
        if (outgoing)
        {
            fields.Add(new("OriginWarehouse", Localization.Text("Inventory_Origin"), warehouses));
            fields.Add(
                new(
                    "OriginLocation",
                    Localization.Text("Inventory_Location"),
                    LocationChoices(null)
                )
            );
        }
        if (incoming)
        {
            fields.Add(
                new("DestinationWarehouse", Localization.Text("Inventory_Destination"), warehouses)
            );
            fields.Add(
                new(
                    "DestinationLocation",
                    Localization.Text("Inventory_Location"),
                    LocationChoices(null)
                )
            );
        }
        using var dialog = new MovementLineForm(fields);
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;
        if (
            !decimal.TryParse(
                dialog["Quantity"],
                NumberStyles.Number,
                CultureInfo.CurrentCulture,
                out var quantity
            )
            || quantity <= 0
        )
        {
            ShowError(Localization.Text("Inventory_InvalidNumber"));
            return;
        }
        long? originWarehouse = outgoing ? Parse(dialog["OriginWarehouse"]) : null,
            destinationWarehouse = incoming ? Parse(dialog["DestinationWarehouse"]) : null;
        long? originLocation = outgoing ? Parse(dialog["OriginLocation"]) : null,
            destinationLocation = incoming ? Parse(dialog["DestinationLocation"]) : null;
        if (
            (
                originLocation is not null
                && _data.Locations.First(item => item.LocationId == originLocation).WarehouseId
                    != originWarehouse
            )
            || (
                destinationLocation is not null
                && _data.Locations.First(item => item.LocationId == destinationLocation).WarehouseId
                    != destinationWarehouse
            )
        )
        {
            ShowError(Localization.Text("Common_Required"));
            return;
        }
        var product = _data.Products.First(item => item.ProductId == Parse(dialog["Product"]));
        _lines.Add(
            new(
                product.ProductId,
                product.Code,
                product.Name,
                originWarehouse,
                WarehouseName(_data.Warehouses, originWarehouse),
                originLocation,
                LocationName(originLocation),
                destinationWarehouse,
                WarehouseName(_data.Warehouses, destinationWarehouse),
                destinationLocation,
                LocationName(destinationLocation),
                quantity,
                product.UnitSymbol
            )
        );
        BindLines();
    }

    /// <summary>Confirma  después de validar su estado.</summary>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private async Task ConfirmAsync()
    {
        if (_lines.Count == 0)
        {
            ShowError(Localization.Text("Inventory_MovementEmpty"));
            return;
        }
        try
        {
            await _apiClient.CreateInventoryMovementAsync(
                _token,
                new(
                    Convert.ToString(_cmbType.SelectedValue, CultureInfo.InvariantCulture)
                        ?? "ENTRADA",
                    _dtpDate.Value.ToUniversalTime(),
                    _txtObservation.Text.Trim(),
                    _lines
                        .Select(line => new InventoryMovementDetailRequest(
                            line.ProductId,
                            line.OriginWarehouseId,
                            line.OriginLocationId,
                            line.DestinationWarehouseId,
                            line.DestinationLocationId,
                            line.Quantity
                        ))
                        .ToList()
                )
            );
            MessageBox.Show(
                this,
                Localization.Text("Inventory_MovementCreated"),
                "OxiTigre",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (HttpRequestException exception)
        {
            ShowError(exception.Message);
        }
    }

    /// <summary>Quita line del documento o control actual.</summary>
    private void RemoveLine()
    {
        if (_dgvLines.CurrentRow?.DataBoundItem is MovementLineDraft line)
        {
            _lines.Remove(line);
            BindLines();
        }
    }

    /// <summary>Actualiza la grilla con los renglones actuales del documento.</summary>
    private void BindLines()
    {
        _dgvLines.DataSource = null;
        _dgvLines.DataSource = _lines.ToList();
        foreach (
            var name in new[]
            {
                "ProductId",
                "OriginWarehouseId",
                "OriginLocationId",
                "DestinationWarehouseId",
                "DestinationLocationId",
            }
        )
            if (_dgvLines.Columns[name] is { } column)
                column.Visible = false;
        foreach (
            var header in new Dictionary<string, string>
            {
                ["ProductCode"] = "Grid_Code",
                ["Product"] = "Inventory_Product",
                ["OriginWarehouse"] = "Inventory_Origin",
                ["OriginLocation"] = "Inventory_Location",
                ["DestinationWarehouse"] = "Inventory_Destination",
                ["DestinationLocation"] = "Inventory_Location",
                ["Quantity"] = "Inventory_Quantity",
                ["Unit"] = "Inventory_Unit",
            }
        )
            if (_dgvLines.Columns[header.Key] is { } column)
                column.HeaderText = Localization.Text(header.Value);
    }

    /// <summary>Devuelve las ubicaciones pertenecientes al depósito seleccionado.</summary>
    /// <param name="warehouseId">Identificador opcional del depósito usado para filtrar ubicaciones.</param>
    /// <returns>Colección de registros u opciones obtenida por la operación.</returns>
    private IReadOnlyList<InventoryManagementForm.Choice> LocationChoices(long? warehouseId) =>
        [
            new("", Localization.Text("Inventory_NoSelection")),
            .. _data
                .Locations.Where(item =>
                    item.StatusCode == "ACTIVO"
                    && (warehouseId is null || item.WarehouseId == warehouseId)
                )
                .Select(item => new InventoryManagementForm.Choice(
                    item.LocationId.ToString(CultureInfo.InvariantCulture),
                    $"{WarehouseName(_data.Warehouses, item.WarehouseId)} · {item.Name}"
                )),
        ];

    /// <summary>Obtiene el nombre visible de un depósito por su identificador.</summary>
    /// <param name="items">Registros u opciones que se presentan en el control.</param>
    /// <param name="id">Identificador del registro buscado.</param>
    /// <returns>Texto normalizado, localizado o formateado por la operación.</returns>
    private static string? WarehouseName(IReadOnlyList<WarehouseResponse> items, long? id) =>
        id is null ? null : items.First(item => item.WarehouseId == id).Name;

    /// <summary>Obtiene el nombre visible de una ubicación por su identificador.</summary>
    /// <param name="id">Identificador del registro buscado.</param>
    /// <returns>Texto normalizado, localizado o formateado por la operación.</returns>
    private string? LocationName(long? id) =>
        id is null ? null : _data.Locations.First(item => item.LocationId == id).Name;

    /// <summary>Interpreta y valida la operación de inventario a partir del texto ingresado.</summary>
    /// <param name="value">Texto que se normaliza, valida o asigna.</param>
    /// <returns>Valor obtenido después de aplicar la conversión o búsqueda.</returns>
    private static long? Parse(string value) =>
        long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;

    /// <summary>Agrega un campo etiquetado a la distribución visual indicada.</summary>
    /// <param name="panel">Panel visual al que se agrega el control.</param>
    /// <param name="key">Clave de recurso o campo que identifica el texto o control.</param>
    /// <param name="control">Control visual asociado al campo o sección.</param>
    private static void AddLabeled(FlowLayoutPanel panel, string key, Control control)
    {
        panel.Controls.Add(
            new Label
            {
                Text = Localization.Text(key),
                AutoSize = true,
                Margin = new Padding(0, 8, 6, 0),
            }
        );
        panel.Controls.Add(control);
    }

    /// <summary>Muestra un error funcional sin exponer detalles técnicos.</summary>
    /// <param name="message">Mensaje funcional que se muestra al usuario.</param>
    private void ShowError(string message) =>
        MessageBox.Show(this, message, "OxiTigre", MessageBoxButtons.OK, MessageBoxIcon.Error);

    private sealed record MovementLineDraft(
        long ProductId,
        string ProductCode,
        string Product,
        long? OriginWarehouseId,
        string? OriginWarehouse,
        long? OriginLocationId,
        string? OriginLocation,
        long? DestinationWarehouseId,
        string? DestinationWarehouse,
        long? DestinationLocationId,
        string? DestinationLocation,
        decimal Quantity,
        string Unit
    );

    private sealed record LineField(
        string Key,
        string Label,
        IReadOnlyList<InventoryManagementForm.Choice>? Choices
    );

    private sealed class MovementLineForm : Form
    {
        private readonly Dictionary<string, Control> _controls = [];
        internal string this[string key] =>
            _controls[key] is ComboBox combo
                ? Convert.ToString(combo.SelectedValue, CultureInfo.InvariantCulture)
                    ?? string.Empty
                : ((TextBox)_controls[key]).Text.Trim();

        /// <summary>Inicializa el componente de inventario con sus dependencias y datos de trabajo.</summary>
        /// <param name="fields">Campos que componen el editor.</param>
        internal MovementLineForm(IReadOnlyList<LineField> fields)
        {
            Text = Localization.Text("Inventory_AddLine");
            Width = 620;
            Height = 150 + fields.Count * 48;
            StartPosition = FormStartPosition.CenterParent;
            UiTheme.Apply(this);
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(18),
                ColumnCount = 2,
                RowCount = fields.Count + 1,
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (var index = 0; index < fields.Count; index++)
            {
                var field = fields[index];
                Control control;
                if (field.Choices is not null)
                {
                    var combo = new ComboBox
                    {
                        Dock = DockStyle.Fill,
                        DropDownStyle = ComboBoxStyle.DropDownList,
                        DataSource = field.Choices.ToList(),
                        DisplayMember = "Label",
                        ValueMember = "Value",
                    };
                    control = combo;
                }
                else
                    control = new TextBox { Dock = DockStyle.Fill, PlaceholderText = field.Label };
                _controls[field.Key] = control;
                layout.Controls.Add(
                    new Label
                    {
                        Text = field.Label,
                        AutoSize = true,
                        Anchor = AnchorStyles.Left,
                    },
                    0,
                    index
                );
                layout.Controls.Add(control, 1, index);
            }
            var save = UiTheme.Button(Localization.Text("Common_Save"), 0, 0, 130);
            save.Click += (_, _) =>
            {
                DialogResult = DialogResult.OK;
                Close();
            };
            layout.Controls.Add(save, 1, fields.Count);
            Controls.Add(layout);
            AcceptButton = save;
        }
    }
}
