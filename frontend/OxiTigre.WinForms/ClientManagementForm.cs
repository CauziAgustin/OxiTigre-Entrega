/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.WinForms.ClientManagementForm
Archivo: ClientManagementForm.cs | Versión: 1.2.0 | Fecha: 2026-08-26 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Presenta listado, alta, edición y estado de clientes desde el dashboard.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Extracción de la gestión comercial a su pantalla modular.
Historial: 1.1.0 | 2026-08-26 | FABRICA | Agustin Omar Cauzi | Búsqueda homogénea en la grilla.
Historial: 1.2.0 | 2026-08-26 | FABRICA | Agustin Omar Cauzi | Explicación operativa y corrección del área útil de la grilla.
===============================================================================
*/
using OxiTigre.ApiClient;
using OxiTigre.Contracts.Commercial;
using OxiTigre.Contracts.Security;

namespace OxiTigre.WinForms;

/// <summary>Administra clientes y teléfonos según los permisos de la sesión.</summary>
internal sealed class ClientManagementForm : Form
{
    private readonly OxiTigreApiClient _apiClient;
    private readonly LoginResponse _session;
    private readonly bool _canManageClients;
    private readonly ComboBox _cmbEstado = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList
    };
    private readonly DataGridView _dgvClientes = new()
    {
        ReadOnly = true,
        AutoGenerateColumns = true,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
    };

    /// <summary>Inicializa el componente de gestión de clientes con sus dependencias y datos de trabajo.</summary>
    /// <param name="apiClient">Cliente usado para comunicarse con la API.</param>
    /// <param name="session">Sesión autenticada que determina permisos y contexto operativo.</param>
    internal ClientManagementForm(OxiTigreApiClient apiClient, LoginResponse session)
    {
        _apiClient = apiClient;
        _session = session;
        _canManageClients = session.Permissions.Contains("COMERCIAL.GESTIONAR", StringComparer.OrdinalIgnoreCase);
        Text = Localization.Text("Clients_Title");
        Width = 1180;
        Height = 720;
        MinimumSize = new Size(960, 620);
        StartPosition = FormStartPosition.CenterParent;
        UiTheme.Apply(this);
        _cmbEstado.DataSource = new[]
        {
            new StatusOption("ACTIVO", Localization.Text("Common_Active")),
            new StatusOption("INACTIVO", Localization.Text("Common_Inactive"))
        };
        _cmbEstado.DisplayMember = nameof(StatusOption.Name);
        _cmbEstado.ValueMember = nameof(StatusOption.Code);

        Controls.Add(new Label
        {
            Text = Localization.Text("Clients_Heading"),
            Left = 28,
            Top = 22,
            AutoSize = true,
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = UiTheme.Primary
        });
        var overview = new Label
        {
            Text = Localization.Text("Clients_Overview"),
            Left = 28,
            Top = 55,
            Width = 1105,
            Height = 36,
            Padding = new Padding(8),
            BackColor = Color.FromArgb(230, 244, 234),
            ForeColor = UiTheme.Primary,
            AutoEllipsis = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        Controls.Add(overview);
        Controls.Add(new Label { Text = Localization.Text("Common_Status"), Left = 28, Top = 112, AutoSize = true });
        _cmbEstado.SetBounds(82, 107, 120, 29);
        _cmbEstado.SelectedValueChanged += async (_, _) => await LoadClientsAsync();
        Controls.Add(_cmbEstado);

        var btnNuevo = UiTheme.Button(Localization.Text("Clients_New"), 225, 104, 140);
        btnNuevo.Enabled = _canManageClients;
        btnNuevo.Click += btnNuevo_Click;
        var btnEditar = UiTheme.Button(Localization.Text("Common_Edit"), 377, 104, 110);
        btnEditar.Enabled = _canManageClients;
        btnEditar.Click += (_, _) => EditSelected();
        var btnEstado = UiTheme.Button(Localization.Text("Clients_ChangeStatus"), 499, 104, 140);
        btnEstado.Enabled = _canManageClients;
        btnEstado.Click += btnEstado_Click;
        var btnActualizar = UiTheme.Button(Localization.Text("Common_Refresh"), 651, 104, 120);
        btnActualizar.Click += async (_, _) => await LoadClientsAsync();
        var btnCerrar = UiTheme.Button(Localization.Text("Common_Back"), 783, 104, 120);
        btnCerrar.Click += (_, _) => Close();
        Controls.AddRange([btnNuevo, btnEditar, btnEstado, btnActualizar, btnCerrar]);

        var clients = UiTheme.Searchable(_dgvClientes);
        clients.SetBounds(28, 150, 1105, 500);
        clients.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _dgvClientes.CellDoubleClick += (_, eventArgs) => { if (_canManageClients && eventArgs.RowIndex >= 0) EditSelected(); };
        UiTheme.Grid(_dgvClientes);
        Controls.Add(clients);
        Shown += async (_, _) => await LoadClientsAsync();
    }

    /// <summary>Carga clients y actualiza la interfaz con los datos obtenidos.</summary>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private async Task LoadClientsAsync()
    {
        try
        {
            _dgvClientes.DataSource = (await _apiClient.GetClientsAsync(_session.Token, _cmbEstado.SelectedValue?.ToString() ?? "ACTIVO"))
                .Select(client => new ClientGridRow(client, client.Code,
                    client.PersonType == "F" ? Localization.Text("Clients_PersonNatural") : Localization.Text("Clients_PersonLegal"),
                    client.NameOrBusinessName, client.Surname, FormatDocument(client.DocumentNumber), client.Email,
                    client.PrimaryPhone, client.PhoneCount, Localization.Text(client.StatusCode == "ACTIVO" ? "Common_Active" : "Common_Inactive"))).ToList();
            if (_dgvClientes.Columns[nameof(ClientGridRow.Client)] is { } client) client.Visible = false;
            Header(nameof(ClientGridRow.Código), Localization.Text("Grid_Code"));
            Header(nameof(ClientGridRow.Tipo), Localization.Text("Grid_Type"));
            Header(nameof(ClientGridRow.NombreRazónSocial), Localization.Text("Clients_NameBusiness"));
            Header(nameof(ClientGridRow.Apellido), Localization.Text("Grid_Surname"));
            Header(nameof(ClientGridRow.Documento), Localization.Text("Grid_Document"));
            Header(nameof(ClientGridRow.Correo), Localization.Text("Grid_Email"));
            Header(nameof(ClientGridRow.TeléfonoPrincipal), Localization.Text("Clients_PrimaryPhone"));
            Header(nameof(ClientGridRow.CantidadTeléfonos), Localization.Text("Clients_PhoneCount"));
            Header(nameof(ClientGridRow.Estado), Localization.Text("Grid_Status"));
        }
        catch (HttpRequestException exception) { ShowError(Localization.Format("Clients_LoadFailed", exception.Message)); }
    }

    /// <summary>Procesa la acción de crear un registro solicitada desde la interfaz.</summary>
    /// <param name="sender">Control que originó el evento.</param>
    /// <param name="e">Datos asociados al evento de la interfaz.</param>
    private void btnNuevo_Click(object? sender, EventArgs e)
    {
        using var form = new ClientEditForm(_apiClient, _session.Token);
        if (form.ShowDialog(this) == DialogResult.OK) _ = LoadClientsAsync();
    }

    /// <summary>Abre el editor correspondiente a el registro seleccionado y conserva los cambios confirmados.</summary>
    private void EditSelected()
    {
        if (SelectedClient is not { } client) return;
        using var form = new ClientEditForm(_apiClient, _session.Token, client.ClientId);
        if (form.ShowDialog(this) == DialogResult.OK) _ = LoadClientsAsync();
    }

    /// <summary>Procesa la acción de cambiar el estado solicitada desde la interfaz.</summary>
    /// <param name="sender">Control que originó el evento.</param>
    /// <param name="e">Datos asociados al evento de la interfaz.</param>
    private async void btnEstado_Click(object? sender, EventArgs e)
    {
        if (SelectedClient is not { } client) return;
        var target = client.StatusCode == "ACTIVO" ? "INACTIVO" : "ACTIVO";
        if (MessageBox.Show(this, Localization.Format("Clients_ChangeQuestion", client.Code, target), "OxiTigre",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        try
        {
            await _apiClient.ChangeClientStatusAsync(_session.Token, client.ClientId, target);
            await LoadClientsAsync();
        }
        catch (HttpRequestException exception) { ShowError(exception.Message); }
    }

    private ClientSummaryResponse? SelectedClient => (_dgvClientes.CurrentRow?.DataBoundItem as ClientGridRow)?.Client;
    /// <summary>Resuelve el encabezado localizado de una columna o propiedad.</summary>
    /// <param name="property">Nombre de la propiedad o columna que se modifica.</param>
    /// <param name="text">Texto que se muestra, interpreta o transforma.</param>
    private void Header(string property, string text) { if (_dgvClientes.Columns[property] is { } column) column.HeaderText = text; }
    /// <summary>Muestra un error funcional sin exponer detalles técnicos.</summary>
    /// <param name="message">Mensaje funcional que se muestra al usuario.</param>
    private void ShowError(string message) => MessageBox.Show(this, message, "OxiTigre", MessageBoxButtons.OK, MessageBoxIcon.Error);
    /// <summary>Presenta CUIT o CUIL con separadores y conserva otros documentos sin cambios.</summary>
    /// <param name="value">Texto que se normaliza, valida o asigna.</param>
    /// <returns>Texto normalizado, localizado o formateado por la operación.</returns>
    private static string? FormatDocument(string? value) => value is { Length: 11 } && value.All(char.IsDigit)
        ? $"{value[..2]}-{value.Substring(2, 8)}-{value[^1]}" : value;

    private sealed record ClientGridRow(ClientSummaryResponse Client, string Código, string Tipo,
        string NombreRazónSocial, string? Apellido, string? Documento, string? Correo,
        string? TeléfonoPrincipal, long CantidadTeléfonos, string Estado);
    private sealed record StatusOption(string Code, string Name);
}
