/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.WinForms.ConfigurationForm
Archivo: ConfigurationForm.cs | Versión: 1.4.0 | Fecha: 2026-09-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Administra estructura, catálogos, parámetros, módulos, errores, traducciones e idioma.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Reinicio inmediato después de cambiar el idioma.
Historial: 1.2.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Recarga visual del idioma conservando la sesión.
Historial: 1.3.0 | 2026-08-26 | FABRICA | Agustin Omar Cauzi | CUIT asistido, pestañas y búsquedas homogéneas.
Historial: 1.4.0 | 2026-09-20 | FABRICA | Agustin Omar Cauzi | Importación validada de paquetes de idioma.
===============================================================================
*/
using System.Globalization;
using System.Text.Json;
using OxiTigre.ApiClient;
using OxiTigre.Contracts.Configuration;

namespace OxiTigre.WinForms;

/// <summary>Presenta la Fase 4 en una única ventana nativa con pestañas y permisos funcionales.</summary>
internal sealed class ConfigurationForm : Form
{
    private readonly OxiTigreApiClient _apiClient;
    private readonly string _token;
    private readonly bool _canManage;
    private readonly bool _canManageErrors;
    private ConfigurationSnapshotResponse? _data;
    private readonly TextBox _txtLegalName = new();
    private readonly TextBox _txtTradeName = new();
    private readonly MaskedTextBox _txtTaxId = new("00-00000000-0")
    {
        TextMaskFormat = MaskFormat.IncludeLiterals,
        CutCopyMaskFormat = MaskFormat.IncludeLiterals,
    };
    private readonly TextBox _txtEmail = new();
    private readonly DataGridView _dgvBranches = Grid();
    private readonly DataGridView _dgvUnits = Grid();
    private readonly DataGridView _dgvPhoneTypes = Grid();
    private readonly DataGridView _dgvStates = Grid();
    private readonly DataGridView _dgvTranslations = Grid();
    private readonly DataGridView _dgvParameters = Grid();
    private readonly DataGridView _dgvModules = Grid();
    private readonly DataGridView _dgvErrors = Grid();
    private readonly ComboBox _cmbLanguage = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = 280,
    };

    /// <summary>Inicializa la administración según permisos de consulta y modificación.</summary>
    /// <param name="apiClient">Cliente usado para consultar y modificar la configuración.</param>
    /// <param name="token">Token de la sesión autenticada.</param>
    /// <param name="permissions">Permisos que determinan las operaciones habilitadas.</param>
    internal ConfigurationForm(
        OxiTigreApiClient apiClient,
        string token,
        IReadOnlyList<string> permissions
    )
    {
        _apiClient = apiClient;
        _token = token;
        _canManage = permissions.Contains(
            "CONFIGURACION.GESTIONAR",
            StringComparer.OrdinalIgnoreCase
        );
        _canManageErrors = permissions.Contains(
            "AUDITORIA.GESTIONAR_ERRORES",
            StringComparer.OrdinalIgnoreCase
        );
        Text = Localization.Text("Config_Title");
        Width = 1240;
        Height = 780;
        MinimumSize = new Size(1000, 650);
        StartPosition = FormStartPosition.CenterParent;
        UiTheme.Apply(this);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        UiTheme.Tabs(tabs);
        tabs.TabPages.Add(BuildCompanyTab());
        tabs.TabPages.Add(BuildStructureTab());
        tabs.TabPages.Add(BuildCatalogsTab());
        tabs.TabPages.Add(BuildParametersTab());
        tabs.TabPages.Add(BuildModulesErrorsTab());
        tabs.TabPages.Add(BuildLanguageTab());
        Controls.Add(tabs);
        Shown += async (_, _) => await LoadAsync();
    }

    /// <summary>Construye la pestaña correspondiente a la empresa y configura sus controles.</summary>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private TabPage BuildCompanyTab()
    {
        var tab = Tab(Localization.Text("Config_Company"));
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 250,
            Padding = new Padding(28),
            ColumnCount = 2,
            RowCount = 5,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddRow(layout, 0, Localization.Text("Config_LegalName"), _txtLegalName);
        AddRow(layout, 1, Localization.Text("Config_TradeName"), _txtTradeName);
        AddRow(layout, 2, Localization.Text("Config_TaxId"), _txtTaxId);
        AddRow(layout, 3, Localization.Text("Config_Email"), _txtEmail);
        var save = UiTheme.Button(Localization.Text("Common_Save"), 0, 0, 170);
        save.Enabled = _canManage;
        save.Click += async (_, _) => await SaveCompanyAsync();
        layout.Controls.Add(save, 1, 4);
        tab.Controls.Add(layout);
        if (!_canManage)
            tab.Controls.Add(
                new Label
                {
                    Dock = DockStyle.Bottom,
                    Height = 38,
                    Text = Localization.Text("Config_ReadOnly"),
                    ForeColor = Color.DimGray,
                }
            );
        return tab;
    }

    /// <summary>Construye la pestaña correspondiente a structure y configura sus controles.</summary>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private TabPage BuildStructureTab()
    {
        var tab = Tab(Localization.Text("Config_Structure"));
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 315,
        };
        split.Panel1.Controls.Add(
            Section(
                Localization.Text("Config_Branches"),
                _dgvBranches,
                (
                    Localization.Text("Common_New"),
                    _canManage,
                    async () => await EditBranchAsync(null)
                ),
                (
                    Localization.Text("Common_Edit"),
                    _canManage,
                    async () =>
                        await EditSelectedAsync<BranchConfigurationResponse>(
                            _dgvBranches,
                            EditBranchAsync
                        )
                )
            )
        );
        split.Panel2.Controls.Add(
            Section(
                Localization.Text("Config_Units"),
                _dgvUnits,
                (
                    Localization.Text("Common_New"),
                    _canManage,
                    async () => await EditUnitAsync(null)
                ),
                (
                    Localization.Text("Common_Edit"),
                    _canManage,
                    async () =>
                        await EditSelectedAsync<OperatingUnitConfigurationResponse>(
                            _dgvUnits,
                            EditUnitAsync
                        )
                )
            )
        );
        tab.Controls.Add(split);
        return tab;
    }

    /// <summary>Construye la pestaña correspondiente a catalogs y configura sus controles.</summary>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private TabPage BuildCatalogsTab()
    {
        var tab = Tab(Localization.Text("Config_Catalogs"));
        var inner = new TabControl { Dock = DockStyle.Fill };
        UiTheme.Tabs(inner);
        inner.TabPages.Add(
            ChildTab(
                Localization.Text("Config_PhoneTypes"),
                Section(
                    Localization.Text("Config_PhoneTypes"),
                    _dgvPhoneTypes,
                    (
                        Localization.Text("Common_New"),
                        _canManage,
                        async () => await EditPhoneTypeAsync(null)
                    ),
                    (
                        Localization.Text("Common_Edit"),
                        _canManage,
                        async () =>
                            await EditSelectedAsync<PhoneTypeConfigurationResponse>(
                                _dgvPhoneTypes,
                                EditPhoneTypeAsync
                            )
                    )
                )
            )
        );
        inner.TabPages.Add(
            ChildTab(
                Localization.Text("Config_States"),
                Section(
                    Localization.Text("Config_States"),
                    _dgvStates,
                    (
                        Localization.Text("Common_Edit"),
                        _canManage,
                        async () =>
                            await EditSelectedAsync<StateConfigurationResponse>(
                                _dgvStates,
                                EditStateAsync
                            )
                    )
                )
            )
        );
        inner.TabPages.Add(
            ChildTab(
                Localization.Text("Config_Translations"),
                Section(
                    Localization.Text("Config_Translations"),
                    _dgvTranslations,
                    (
                        Localization.Text("Common_New"),
                        _canManage,
                        async () => await EditTranslationAsync(null)
                    ),
                    (
                        Localization.Text("Common_Edit"),
                        _canManage,
                        async () =>
                            await EditSelectedAsync<CatalogTranslationConfigurationResponse>(
                                _dgvTranslations,
                                EditTranslationAsync
                            )
                    )
                )
            )
        );
        tab.Controls.Add(inner);
        return tab;
    }

    /// <summary>Construye la pestaña correspondiente a parameters y configura sus controles.</summary>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private TabPage BuildParametersTab()
    {
        var tab = Tab(Localization.Text("Config_Parameters"));
        tab.Controls.Add(
            Section(
                Localization.Text("Config_Parameters"),
                _dgvParameters,
                (
                    Localization.Text("Common_New"),
                    _canManage,
                    async () => await EditParameterAsync(null)
                ),
                (
                    Localization.Text("Common_Edit"),
                    _canManage,
                    async () =>
                        await EditSelectedAsync<SystemParameterConfigurationResponse>(
                            _dgvParameters,
                            EditParameterAsync
                        )
                )
            )
        );
        return tab;
    }

    /// <summary>Construye la pestaña correspondiente a modules errors y configura sus controles.</summary>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private TabPage BuildModulesErrorsTab()
    {
        var tab = Tab(Localization.Text("Config_ModulesErrors"));
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 285,
        };
        split.Panel1.Controls.Add(
            Section(
                Localization.Text("Config_Modules"),
                _dgvModules,
                (
                    Localization.Text("Common_New"),
                    _canManage,
                    async () => await EditModuleAsync(null)
                ),
                (
                    Localization.Text("Common_Edit"),
                    _canManage,
                    async () =>
                        await EditSelectedAsync<ModuleConfigurationResponse>(
                            _dgvModules,
                            EditModuleAsync
                        )
                )
            )
        );
        split.Panel2.Controls.Add(
            Section(
                Localization.Text("Config_Errors"),
                _dgvErrors,
                (
                    Localization.Text("Common_New"),
                    _canManageErrors,
                    async () => await EditErrorAsync(null)
                ),
                (
                    Localization.Text("Common_Edit"),
                    _canManageErrors,
                    async () =>
                        await EditSelectedAsync<ErrorCatalogConfigurationResponse>(
                            _dgvErrors,
                            EditErrorAsync
                        )
                )
            )
        );
        tab.Controls.Add(split);
        return tab;
    }

    /// <summary>Construye la pestaña correspondiente a un paquete de idioma y configura sus controles.</summary>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private TabPage BuildLanguageTab()
    {
        var tab = Tab(Localization.Text("Config_Language"));
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 120,
            Padding = new Padding(28),
            FlowDirection = FlowDirection.LeftToRight,
        };
        panel.Controls.Add(
            new Label
            {
                Text = Localization.Text("Config_Language"),
                AutoSize = true,
                Margin = new Padding(0, 10, 15, 0),
            }
        );
        _cmbLanguage.DisplayMember = nameof(Option.Label);
        _cmbLanguage.ValueMember = nameof(Option.Value);
        BindLanguages();
        panel.Controls.Add(_cmbLanguage);
        var save = UiTheme.Button(Localization.Text("Common_Save"), 0, 0, 150);
        save.Click += async (_, _) => await SaveLanguageAsync();
        panel.Controls.Add(save);
        var import = UiTheme.Button(Localization.Text("Config_ImportLanguage"), 0, 0, 190);
        import.Enabled = _canManage;
        import.Click += (_, _) => ImportLanguage();
        panel.Controls.Add(import);
        tab.Controls.Add(
            new Label
            {
                Dock = DockStyle.Top,
                Height = 45,
                Padding = new Padding(28, 12, 0, 0),
                Text = Localization.Text("Config_LanguageBehavior"),
                ForeColor = Color.DimGray,
            }
        );
        tab.Controls.Add(panel);
        return tab;
    }

    /// <summary>Carga  y actualiza la interfaz con los datos obtenidos.</summary>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private async Task LoadAsync()
    {
        try
        {
            _data = await _apiClient.GetConfigurationAsync(_token);
            Bind();
        }
        catch (HttpRequestException exception)
        {
            ShowError(exception.Message);
        }
    }

    /// <summary>Actualiza las grillas y detalles de configuración con la información cargada.</summary>
    private void Bind()
    {
        if (_data is null)
            return;
        _txtLegalName.Text = _data.Company.LegalName;
        _txtTradeName.Text = _data.Company.TradeName;
        _txtTaxId.Text = _data.Company.TaxId;
        _txtEmail.Text = _data.Company.Email;
        Bind(
            _dgvBranches,
            _data.Branches,
            nameof(BranchConfigurationResponse.RowVersion),
            nameof(BranchConfigurationResponse.BranchId)
        );
        Bind(
            _dgvUnits,
            _data.OperatingUnits,
            nameof(OperatingUnitConfigurationResponse.RowVersion),
            nameof(OperatingUnitConfigurationResponse.OperatingUnitId),
            nameof(OperatingUnitConfigurationResponse.BranchId)
        );
        Bind(
            _dgvPhoneTypes,
            _data.PhoneTypes,
            nameof(PhoneTypeConfigurationResponse.RowVersion),
            nameof(PhoneTypeConfigurationResponse.PhoneTypeId)
        );
        Bind(
            _dgvStates,
            _data.States,
            nameof(StateConfigurationResponse.RowVersion),
            nameof(StateConfigurationResponse.StateId)
        );
        Bind(
            _dgvTranslations,
            _data.Translations,
            nameof(CatalogTranslationConfigurationResponse.RowVersion),
            nameof(CatalogTranslationConfigurationResponse.TranslationId)
        );
        Bind(
            _dgvParameters,
            _data.Parameters,
            nameof(SystemParameterConfigurationResponse.RowVersion),
            nameof(SystemParameterConfigurationResponse.ParameterId)
        );
        Bind(
            _dgvModules,
            _data.Modules,
            nameof(ModuleConfigurationResponse.RowVersion),
            nameof(ModuleConfigurationResponse.ModuleId)
        );
        Bind(
            _dgvErrors,
            _data.Errors,
            nameof(ErrorCatalogConfigurationResponse.RowVersion),
            nameof(ErrorCatalogConfigurationResponse.ErrorId),
            nameof(ErrorCatalogConfigurationResponse.ModuleId)
        );
        _cmbLanguage.SelectedValue = _data.CultureCode;
    }

    /// <summary>Valida y guarda company mediante la API.</summary>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private async Task SaveCompanyAsync()
    {
        if (_data is null)
            return;
        await RunAsync(async () =>
            await _apiClient.UpdateCompanyConfigurationAsync(
                _token,
                new UpdateCompanyConfigurationRequest(
                    _txtLegalName.Text,
                    _txtTradeName.Text,
                    _txtTaxId.Text,
                    _txtEmail.Text,
                    _data.Company.RowVersion
                )
            )
        );
    }

    /// <summary>Abre el editor correspondiente a branch y conserva los cambios confirmados.</summary>
    /// <param name="item">Registro existente que se edita, o nulo para crear uno nuevo.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private async Task EditBranchAsync(BranchConfigurationResponse? item)
    {
        using var dialog = Editor(
            Localization.Text("Config_Branches"),
            Field("Code", "Config_Code", item?.Code, item is not null),
            Field("Name", "Config_Name", item?.Name),
            Field("Address", "Config_Address", item?.Address),
            Field("City", "Config_City", item?.City),
            Field("Province", "Config_Province", item?.Province),
            Field("Postal", "Config_PostalCode", item?.PostalCode),
            OptionsField("Status", "Common_Status", item?.StatusCode ?? "ACTIVO", StatusOptions())
        );
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;
        await RunAsync(async () =>
            await _apiClient.SaveBranchConfigurationAsync(
                _token,
                new SaveBranchConfigurationRequest(
                    item?.BranchId,
                    dialog["Code"],
                    dialog["Name"],
                    dialog["Address"],
                    dialog["City"],
                    dialog["Province"],
                    dialog["Postal"],
                    dialog["Status"],
                    item?.RowVersion
                )
            )
        );
    }

    /// <summary>Abre el editor correspondiente a unit y conserva los cambios confirmados.</summary>
    /// <param name="item">Registro existente que se edita, o nulo para crear uno nuevo.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private async Task EditUnitAsync(OperatingUnitConfigurationResponse? item)
    {
        if (_data is null || _data.Branches.Count == 0)
        {
            ShowError(Localization.Text("Config_BranchRequired"));
            return;
        }
        var branchOptions = _data
            .Branches.Select(branch => new Option(
                branch.BranchId.ToString(CultureInfo.InvariantCulture),
                $"{branch.Code} · {branch.Name}"
            ))
            .ToList();
        using var dialog = Editor(
            Localization.Text("Config_Units"),
            OptionsField(
                "Branch",
                "Config_Branch",
                item?.BranchId.ToString(CultureInfo.InvariantCulture) ?? branchOptions[0].Value,
                branchOptions
            ),
            Field("Code", "Config_Code", item?.Code, item is not null),
            Field("Name", "Config_Name", item?.Name),
            Field("Description", "Config_Description", item?.Description, multiline: true),
            OptionsField("Status", "Common_Status", item?.StatusCode ?? "ACTIVO", StatusOptions())
        );
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;
        await RunAsync(async () =>
            await _apiClient.SaveOperatingUnitConfigurationAsync(
                _token,
                new SaveOperatingUnitConfigurationRequest(
                    item?.OperatingUnitId,
                    long.Parse(dialog["Branch"], CultureInfo.InvariantCulture),
                    dialog["Code"],
                    dialog["Name"],
                    dialog["Description"],
                    dialog["Status"],
                    item?.RowVersion
                )
            )
        );
    }

    /// <summary>Abre el editor correspondiente a phone type y conserva los cambios confirmados.</summary>
    /// <param name="item">Registro existente que se edita, o nulo para crear uno nuevo.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private async Task EditPhoneTypeAsync(PhoneTypeConfigurationResponse? item)
    {
        using var dialog = Editor(
            Localization.Text("Config_PhoneTypes"),
            Field("Code", "Config_Code", item?.Code, item is not null),
            Field("Name", "Config_Name", item?.Name),
            Field("Description", "Config_Description", item?.Description, multiline: true),
            OptionsField("Status", "Common_Status", item?.StatusCode ?? "ACTIVO", StatusOptions())
        );
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;
        await RunAsync(async () =>
            await _apiClient.SavePhoneTypeConfigurationAsync(
                _token,
                new SavePhoneTypeConfigurationRequest(
                    item?.PhoneTypeId,
                    dialog["Code"],
                    dialog["Name"],
                    dialog["Description"],
                    dialog["Status"],
                    item?.RowVersion
                )
            )
        );
    }

    /// <summary>Abre el editor correspondiente a state y conserva los cambios confirmados.</summary>
    /// <param name="item">Registro existente que se edita, o nulo para crear uno nuevo.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private async Task EditStateAsync(StateConfigurationResponse? item)
    {
        if (item is null)
            return;
        using var dialog = Editor(
            Localization.Text("Config_States"),
            Field("Name", "Config_Name", item.Name),
            Field("Description", "Config_Description", item.Description, multiline: true),
            Field("Order", "Config_Order", item.Order.ToString(CultureInfo.InvariantCulture)),
            Field(
                "ValidUntil",
                "Config_ValidUntil",
                item.ValidUntilUtc?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            )
        );
        if (
            dialog.ShowDialog(this) != DialogResult.OK
            || !short.TryParse(dialog["Order"], out var order)
        )
            return;
        DateTime? validUntil = null;
        if (!string.IsNullOrWhiteSpace(dialog["ValidUntil"]))
        {
            if (
                !DateTime.TryParseExact(
                    dialog["ValidUntil"],
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsed
                )
            )
            {
                ShowError(Localization.Text("Config_ValidUntil"));
                return;
            }
            validUntil = parsed;
        }
        await RunAsync(async () =>
            await _apiClient.UpdateStateConfigurationAsync(
                _token,
                new UpdateStateConfigurationRequest(
                    item.StateId,
                    dialog["Name"],
                    dialog["Description"],
                    order,
                    validUntil,
                    item.RowVersion
                )
            )
        );
    }

    /// <summary>Abre el editor correspondiente a parameter y conserva los cambios confirmados.</summary>
    /// <param name="item">Registro existente que se edita, o nulo para crear uno nuevo.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private async Task EditParameterAsync(SystemParameterConfigurationResponse? item)
    {
        if (_data is null)
            return;
        var modules = new List<Option> { new(string.Empty, Localization.Text("Config_Global")) };
        modules.AddRange(
            _data.Modules.Select(module => new Option(
                module.ModuleId.ToString(CultureInfo.InvariantCulture),
                module.Name
            ))
        );
        using var dialog = Editor(
            Localization.Text("Config_Parameters"),
            OptionsField(
                "Module",
                "Config_Module",
                item?.ModuleId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                modules
            ),
            Field("Key", "Config_Key", item?.Key, item is not null),
            Field("Value", "Config_Value", item?.Value),
            OptionsField(
                "Type",
                "Config_DataType",
                item?.DataType ?? "TEXTO",
                [
                    new("TEXTO", "TEXTO"),
                    new("ENTERO", "ENTERO"),
                    new("DECIMAL", "DECIMAL"),
                    new("BOOLEANO", "BOOLEANO"),
                    new("FECHA", "FECHA"),
                ]
            ),
            OptionsField(
                "Secret",
                "Config_IsSecret",
                item?.IsSecret == true ? "true" : "false",
                BoolOptions()
            ),
            Field("Reference", "Config_SecretReference", null),
            Field("Description", "Config_Description", item?.Description, multiline: true),
            OptionsField("Status", "Common_Status", item?.StatusCode ?? "ACTIVO", StatusOptions())
        );
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;
        long? moduleId = string.IsNullOrWhiteSpace(dialog["Module"])
            ? null
            : long.Parse(dialog["Module"], CultureInfo.InvariantCulture);
        await RunAsync(async () =>
            await _apiClient.SaveSystemParameterConfigurationAsync(
                _token,
                new SaveSystemParameterConfigurationRequest(
                    item?.ParameterId,
                    moduleId,
                    dialog["Key"],
                    dialog["Value"],
                    dialog["Type"],
                    dialog["Secret"] == "true",
                    dialog["Reference"],
                    dialog["Description"],
                    dialog["Status"],
                    item?.RowVersion
                )
            )
        );
    }

    /// <summary>Abre el editor correspondiente a module y conserva los cambios confirmados.</summary>
    /// <param name="item">Registro existente que se edita, o nulo para crear uno nuevo.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private async Task EditModuleAsync(ModuleConfigurationResponse? item)
    {
        using var dialog = Editor(
            Localization.Text("Config_Modules"),
            Field(
                "Number",
                "Config_ModuleNumber",
                item?.ModuleNumber.ToString(CultureInfo.InvariantCulture),
                item is not null
            ),
            Field("Code", "Config_Code", item?.Code, item is not null),
            Field("Name", "Config_Name", item?.Name),
            Field("Description", "Config_Description", item?.Description, multiline: true),
            Field(
                "Order",
                "Config_Order",
                item?.Order.ToString(CultureInfo.InvariantCulture) ?? "0"
            ),
            OptionsField("Status", "Common_Status", item?.StatusCode ?? "ACTIVO", StatusOptions())
        );
        if (
            dialog.ShowDialog(this) != DialogResult.OK
            || !short.TryParse(dialog["Number"], out var number)
            || !short.TryParse(dialog["Order"], out var order)
        )
            return;
        await RunAsync(async () =>
            await _apiClient.SaveModuleConfigurationAsync(
                _token,
                new SaveModuleConfigurationRequest(
                    item?.ModuleId,
                    number,
                    dialog["Code"],
                    dialog["Name"],
                    dialog["Description"],
                    order,
                    dialog["Status"],
                    item?.RowVersion
                )
            )
        );
    }

    /// <summary>Abre el editor correspondiente a error y conserva los cambios confirmados.</summary>
    /// <param name="item">Registro existente que se edita, o nulo para crear uno nuevo.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private async Task EditErrorAsync(ErrorCatalogConfigurationResponse? item)
    {
        if (_data is null)
            return;
        var fields = new List<EditField>();
        if (item is null)
            fields.Add(
                OptionsField(
                    "Module",
                    "Config_Module",
                    _data.Modules[0].ModuleId.ToString(CultureInfo.InvariantCulture),
                    _data
                        .Modules.Select(module => new Option(
                            module.ModuleId.ToString(CultureInfo.InvariantCulture),
                            module.Name
                        ))
                        .ToList()
                )
            );
        fields.AddRange([
            Field("Name", "Config_Name", item?.Name),
            Field("Description", "Config_Description", item?.Description, multiline: true),
            Field("Cause", "Config_ProbableCause", item?.ProbableCause, multiline: true),
            Field("Action", "Config_RecommendedAction", item?.RecommendedAction, multiline: true),
            OptionsField(
                "Severity",
                "Config_Severity",
                item?.Severity ?? "ERROR",
                [
                    new("INFORMATIVO", "INFORMATIVO"),
                    new("ADVERTENCIA", "ADVERTENCIA"),
                    new("ERROR", "ERROR"),
                    new("CRITICO", "CRITICO"),
                ]
            ),
        ]);
        if (item is not null)
            fields.Add(OptionsField("Status", "Common_Status", item.StatusCode, StatusOptions()));
        using var dialog = Editor(Localization.Text("Config_Errors"), [.. fields]);
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;
        if (item is null)
        {
            CreatedErrorCodeResponse? created = null;
            await RunAsync(
                async () =>
                    created = await _apiClient.CreateErrorCatalogAsync(
                        _token,
                        new CreateErrorCatalogRequest(
                            long.Parse(dialog["Module"], CultureInfo.InvariantCulture),
                            dialog["Name"],
                            dialog["Description"],
                            dialog["Cause"],
                            dialog["Action"],
                            dialog["Severity"]
                        )
                    ),
                false
            );
            if (created is not null)
                MessageBox.Show(
                    this,
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Localization.Text("Config_ErrorCreated"),
                        created.ErrorCode
                    ),
                    "OxiTigre"
                );
        }
        else
            await RunAsync(async () =>
                await _apiClient.UpdateErrorCatalogAsync(
                    _token,
                    new UpdateErrorCatalogRequest(
                        item.ErrorId,
                        dialog["Name"],
                        dialog["Description"],
                        dialog["Cause"],
                        dialog["Action"],
                        dialog["Severity"],
                        dialog["Status"],
                        item.RowVersion
                    )
                )
            );
    }

    /// <summary>Abre el editor correspondiente a translation y conserva los cambios confirmados.</summary>
    /// <param name="item">Registro existente que se edita, o nulo para crear uno nuevo.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private async Task EditTranslationAsync(CatalogTranslationConfigurationResponse? item)
    {
        using var dialog = Editor(
            Localization.Text("Config_Translations"),
            Field("Entity", "Config_Entity", item?.Entity, item is not null),
            Field("Code", "Config_Code", item?.Code, item is not null),
            OptionsField(
                "Culture",
                "Config_Culture",
                item?.CultureCode ?? "en-US",
                Localization
                    .AvailableLanguages()
                    .Select(language => new Option(
                        language.CultureCode,
                        $"{language.DisplayName} · {language.CultureCode}"
                    ))
                    .ToList(),
                item is not null
            ),
            Field("Name", "Config_Name", item?.Name),
            Field("Description", "Config_Description", item?.Description, multiline: true),
            OptionsField("Status", "Common_Status", item?.StatusCode ?? "ACTIVO", StatusOptions())
        );
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;
        await RunAsync(async () =>
            await _apiClient.SaveCatalogTranslationAsync(
                _token,
                new SaveCatalogTranslationRequest(
                    item?.TranslationId,
                    dialog["Entity"],
                    dialog["Code"],
                    dialog["Culture"],
                    dialog["Name"],
                    dialog["Description"],
                    dialog["Status"],
                    item?.RowVersion
                )
            )
        );
    }

    /// <summary>Valida y guarda language mediante la API.</summary>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private async Task SaveLanguageAsync()
    {
        if (_cmbLanguage.SelectedValue is not string culture)
            return;
        try
        {
            await _apiClient.SaveLanguagePreferenceAsync(
                _token,
                new SaveLanguagePreferenceRequest(culture)
            );
            Localization.SaveCulture(culture);
            MessageBox.Show(
                this,
                Localization.Text("Config_LanguageRestart"),
                "OxiTigre",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
            Localization.SetCulture(culture);
            DialogResult = DialogResult.Retry;
            Close();
        }
        catch (Exception exception)
            when (exception is HttpRequestException or IOException or UnauthorizedAccessException)
        {
            ShowError(exception.Message);
        }
    }

    /// <summary>Solicita un archivo JSON, lo valida e incorpora el idioma al selector local.</summary>
    private void ImportLanguage()
    {
        using var dialog = new OpenFileDialog
        {
            Title = Localization.Text("Config_ImportLanguage"),
            Filter = Localization.Text("Config_LanguageFileFilter"),
            CheckFileExists = true,
            Multiselect = false,
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            var result = Localization.ImportLanguage(dialog.FileName);
            BindLanguages(result.CultureCode);
            MessageBox.Show(
                this,
                Localization.Format(
                    "Config_LanguageImported",
                    result.DisplayName,
                    result.TranslationCount
                ),
                "OxiTigre",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }
        catch (Exception exception)
            when (exception
                    is InvalidDataException
                        or JsonException
                        or IOException
                        or UnauthorizedAccessException
                        or CultureNotFoundException
            )
        {
            MessageBox.Show(
                this,
                Localization.Format("Config_LanguageInvalid", exception.Message),
                "OxiTigre",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }

    /// <summary>Actualiza el selector con idiomas incluidos y paquetes instalados.</summary>
    /// <param name="selectedCulture">Cultura que debe quedar seleccionada, si está disponible.</param>
    private void BindLanguages(string? selectedCulture = null)
    {
        var selected =
            selectedCulture
            ?? Convert.ToString(_cmbLanguage.SelectedValue, CultureInfo.InvariantCulture);
        _cmbLanguage.DataSource = Localization
            .AvailableLanguages()
            .Select(item => new Option(
                item.CultureCode,
                item.Imported ? $"{item.DisplayName} · {item.CultureCode}" : item.DisplayName
            ))
            .ToList();
        if (!string.IsNullOrWhiteSpace(selected))
            _cmbLanguage.SelectedValue = selected;
    }

    /// <summary>Ejecuta una operación, informa el resultado y traduce errores funcionales.</summary>
    /// <param name="action">Operación que se ejecuta cuando el usuario confirma la acción.</param>
    /// <param name="showSaved">Indica si debe mostrarse la confirmación de guardado.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private async Task RunAsync(Func<Task> action, bool showSaved = true)
    {
        try
        {
            await action();
            if (showSaved)
                MessageBox.Show(
                    this,
                    Localization.Text("Config_Saved"),
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

    /// <summary>Muestra un error funcional sin exponer detalles técnicos.</summary>
    /// <param name="message">Mensaje funcional que se muestra al usuario.</param>
    private void ShowError(string message) =>
        MessageBox.Show(this, message, "OxiTigre", MessageBoxButtons.OK, MessageBoxIcon.Error);

    /// <summary>Crea el editor modal usado por el módulo de configuración.</summary>
    /// <param name="title">Título visible de la ventana, pestaña o sección.</param>
    /// <param name="fields">Campos que componen el editor.</param>
    /// <returns>Objeto construido u obtenido por la operación.</returns>
    private static ConfigurationEditorForm Editor(string title, params EditField[] fields) =>
        new(title, fields);

    /// <summary>Crea un campo de edición con etiqueta, tamaño y comportamiento definidos.</summary>
    /// <param name="key">Clave de recurso o campo que identifica el texto o control.</param>
    /// <param name="labelKey">Clave de recurso usada como etiqueta visible.</param>
    /// <param name="value">Texto que se normaliza, valida o asigna.</param>
    /// <param name="readOnly">Indica si el usuario puede modificar el campo.</param>
    /// <param name="multiline">Indica si el editor admite varias líneas.</param>
    /// <returns>Objeto construido u obtenido por la operación.</returns>
    private static EditField Field(
        string key,
        string labelKey,
        string? value,
        bool readOnly = false,
        bool multiline = false
    ) => new(key, Localization.Text(labelKey), value ?? string.Empty, readOnly, null, multiline);

    /// <summary>Crea la definición de un campo de selección con sus opciones permitidas.</summary>
    /// <param name="key">Clave de recurso o campo que identifica el texto o control.</param>
    /// <param name="labelKey">Clave de recurso usada como etiqueta visible.</param>
    /// <param name="value">Texto que se normaliza, valida o asigna.</param>
    /// <param name="options">Opciones admitidas por el campo.</param>
    /// <param name="readOnly">Indica si el usuario puede modificar el campo.</param>
    /// <returns>Objeto construido u obtenido por la operación.</returns>
    private static EditField OptionsField(
        string key,
        string labelKey,
        string value,
        IReadOnlyList<Option> options,
        bool readOnly = false
    ) => new(key, Localization.Text(labelKey), value, readOnly, options, false);

    /// <summary>Devuelve los estados que pueden seleccionarse en configuración.</summary>
    /// <returns>Colección de registros u opciones obtenida por la operación.</returns>
    private static IReadOnlyList<Option> StatusOptions() =>
        [
            new("ACTIVO", Localization.Text("Common_Active")),
            new("INACTIVO", Localization.Text("Common_Inactive")),
        ];

    /// <summary>Devuelve las opciones localizadas para valores booleanos.</summary>
    /// <returns>Colección de registros u opciones obtenida por la operación.</returns>
    private static IReadOnlyList<Option> BoolOptions() =>
        [
            new("false", Localization.Text("Common_No")),
            new("true", Localization.Text("Common_Yes")),
        ];

    /// <summary>Crea una pestaña localizada y agrega el contenido indicado.</summary>
    /// <param name="text">Texto que se muestra, interpreta o transforma.</param>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private static TabPage Tab(string text) =>
        new(text) { Padding = new Padding(8), BackColor = UiTheme.Background };

    /// <summary>Crea una pestaña localizada y agrega el contenido indicado.</summary>
    /// <param name="text">Texto que se muestra, interpreta o transforma.</param>
    /// <param name="content">Control visual que se incorpora a la pestaña o sección.</param>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private static TabPage ChildTab(string text, Control content)
    {
        var tab = Tab(text);
        tab.Controls.Add(content);
        return tab;
    }

    /// <summary>Crea una grilla con el estilo y comportamiento común del módulo de configuración.</summary>
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
        {
            if (args.Value is "ACTIVO")
                args.Value = Localization.Text("Common_Active");
            else if (args.Value is "INACTIVO")
                args.Value = Localization.Text("Common_Inactive");
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
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 48,
            FlowDirection = FlowDirection.LeftToRight,
        };
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
            var button = UiTheme.Button(action.Text, 0, 0, 125);
            button.Enabled = action.Enabled;
            button.Click += async (_, _) => await action.Action();
            toolbar.Controls.Add(button);
        }
        panel.Controls.Add(UiTheme.Searchable(grid));
        panel.Controls.Add(toolbar);
        return panel;
    }

    /// <summary>Agrega un campo etiquetado a la distribución visual indicada.</summary>
    /// <param name="layout">Distribución visual que recibe el nuevo renglón.</param>
    /// <param name="row">Índice del renglón dentro de la distribución.</param>
    /// <param name="label">Etiqueta visible del campo.</param>
    /// <param name="control">Control visual asociado al campo o sección.</param>
    private static void AddRow(TableLayoutPanel layout, int row, string label, Control control)
    {
        control.Dock = DockStyle.Fill;
        control.Margin = new Padding(3, 6, 3, 6);
        layout.Controls.Add(
            new Label
            {
                Text = label,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
            },
            0,
            row
        );
        layout.Controls.Add(control, 1, row);
    }

    /// <summary>Actualiza las grillas y detalles de configuración con la información cargada.</summary>
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
            if (HeaderResource(column.Name) is { } resource)
                column.HeaderText = Localization.Text(resource);
    }

    /// <summary>Resuelve el encabezado localizado de una columna o propiedad.</summary>
    /// <param name="property">Nombre de la propiedad o columna que se modifica.</param>
    /// <returns>Texto normalizado, localizado o formateado por la operación.</returns>
    private static string? HeaderResource(string property) =>
        property switch
        {
            "Code" => "Config_Code",
            "Name" => "Config_Name",
            "Description" => "Config_Description",
            "Address" => "Config_Address",
            "City" => "Config_City",
            "Province" => "Config_Province",
            "PostalCode" => "Config_PostalCode",
            "StatusCode" => "Common_Status",
            "Entity" => "Config_Entity",
            "IsInitial" => "Config_Initial",
            "IsFinal" => "Config_Final",
            "Order" => "Config_Order",
            "ValidUntilUtc" => "Config_ValidUntilColumn",
            "ModuleCode" => "Grid_ModuleCode",
            "Key" => "Config_Key",
            "Value" => "Config_Value",
            "DataType" => "Config_DataType",
            "IsSecret" => "Config_IsSecret",
            "HasSecretReference" => "Config_HasSecret",
            "ModuleNumber" => "Config_ModuleNumber",
            "ErrorNumber" => "Config_ErrorNumber",
            "ErrorCode" => "Config_ErrorCode",
            "ProbableCause" => "Config_ProbableCause",
            "RecommendedAction" => "Config_RecommendedAction",
            "Severity" => "Config_Severity",
            "CultureCode" => "Config_Culture",
            _ => null,
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
    private async Task EditSelectedAsync<T>(DataGridView grid, Func<T?, Task> edit)
        where T : class
    {
        var item = Selected<T>(grid);
        if (item is null)
        {
            ShowError(Localization.Text("Common_SelectRecord"));
            return;
        }
        await edit(item);
    }

    private sealed record Option(string Value, string Label);

    private sealed record EditField(
        string Key,
        string Label,
        string Value,
        bool ReadOnly,
        IReadOnlyList<Option>? Options,
        bool Multiline
    );

    /// <summary>Editor reutilizable para formularios simples de Configuración.</summary>
    private sealed class ConfigurationEditorForm : Form
    {
        private readonly IReadOnlyList<EditField> _fields;
        private readonly Dictionary<string, Control> _controls = new(StringComparer.Ordinal);
        internal string this[string key] =>
            _controls[key] switch
            {
                ComboBox combo => Convert.ToString(
                    combo.SelectedValue,
                    CultureInfo.InvariantCulture
                ) ?? string.Empty,
                TextBox text => text.Text.Trim(),
                _ => string.Empty,
            };

        /// <summary>Inicializa el componente de configuración con sus dependencias y datos de trabajo.</summary>
        /// <param name="title">Título visible de la ventana, pestaña o sección.</param>
        /// <param name="fields">Campos que componen el editor.</param>
        internal ConfigurationEditorForm(string title, IReadOnlyList<EditField> fields)
        {
            _fields = fields;
            Text = title;
            Width = 620;
            Height = Math.Min(760, 150 + fields.Sum(field => field.Multiline ? 105 : 55));
            MinimumSize = new Size(520, 300);
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
                if (field.Options is not null)
                {
                    var combo = new ComboBox
                    {
                        DropDownStyle = ComboBoxStyle.DropDownList,
                        DataSource = field.Options.ToList(),
                        DisplayMember = nameof(Option.Label),
                        ValueMember = nameof(Option.Value),
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
            if (
                _fields
                    .Where(field => !field.ReadOnly)
                    .Any(field =>
                        string.IsNullOrWhiteSpace(this[field.Key])
                        && field.Options is null
                        && field.Key is "Code" or "Name" or "Number" or "Order"
                    )
            )
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
