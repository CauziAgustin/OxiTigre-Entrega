/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.WinForms.UserEditForm
Archivo: UserEditForm.cs | Versión: 1.1.0 | Fecha: 2026-08-26 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Permite crear o editar datos, estado y múltiples roles de un usuario.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-26 | FABRICA | Agustin Omar Cauzi | Placeholders y ayuda visible para carga de usuarios y roles.
===============================================================================
*/
using OxiTigre.ApiClient;
using OxiTigre.Contracts.Security;

namespace OxiTigre.WinForms;

/// <summary>Presenta un formulario único para alta y edición de usuarios.</summary>
internal sealed class UserEditForm : Form
{
    private readonly OxiTigreApiClient _apiClient;
    private readonly string _token;
    private readonly UserSummaryResponse? _user;
    private readonly TextBox _txtNombres = new();
    private readonly TextBox _txtApellido = new();
    private readonly TextBox _txtEmail = new();
    private readonly TextBox _txtClaveTemporal = new() { UseSystemPasswordChar = true };
    private readonly ComboBox _cmbEstado = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly CheckedListBox _clbRoles = new() { CheckOnClick = true };
    private readonly Button _btnGuardar;

    /// <summary>Inicializa el componente de edición de usuarios con sus dependencias y datos de trabajo.</summary>
    /// <param name="apiClient">Cliente usado para comunicarse con la API.</param>
    /// <param name="token">Token de la sesión autenticada.</param>
    /// <param name="user">Usuario existente que se edita, o nulo para crear uno nuevo.</param>
    internal UserEditForm(OxiTigreApiClient apiClient, string token, UserSummaryResponse? user = null)
    {
        _apiClient = apiClient;
        _token = token;
        _user = user;
        Text = user is null ? Localization.Text("UserEdit_NewTitle") : Localization.Format("UserEdit_EditTitle", user.Username);
        Width = 620;
        Height = 500;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        UiTheme.Apply(this);

        _txtNombres.PlaceholderText = Localization.Text("UserEdit_GivenNames");
        _txtApellido.PlaceholderText = Localization.Text("UserEdit_Surname");
        _txtEmail.PlaceholderText = "nombre@empresa.com";
        _txtClaveTemporal.PlaceholderText = Localization.Text("UserEdit_TemporaryPassword");

        AddField(Localization.Text("UserEdit_GivenNames"), _txtNombres, 28, 30, 260);
        AddField(Localization.Text("UserEdit_Surname"), _txtApellido, 306, 30, 260);
        AddField(Localization.Text("UserEdit_Email"), _txtEmail, 28, 92, 538);
        AddField(Localization.Text("Common_Status"), _cmbEstado, 28, 154, 260);
        AddField(Localization.Text("UserEdit_TemporaryPassword"), _txtClaveTemporal, 306, 154, 260);
        Controls.Add(new Label { Text = Localization.Text("UserEdit_Roles"), Left = 28, Top = 220, AutoSize = true });
        var help = UiTheme.HelpButton(Localization.Text("UserEdit_Help")); help.SetBounds(542, 214, 24, 24); Controls.Add(help);
        _clbRoles.SetBounds(28, 242, 538, 125);
        Controls.Add(_clbRoles);
        _btnGuardar = UiTheme.Button(Localization.Text("Common_Save"), 446, 390, 120);
        _btnGuardar.Click += btnGuardar_Click;
        Controls.Add(_btnGuardar);
        AcceptButton = _btnGuardar;

        _cmbEstado.DataSource = new[]
        {
            new StatusOption("ACTIVO", Localization.Text("Common_Active")),
            new StatusOption("INACTIVO", Localization.Text("Common_Inactive"))
        };
        _cmbEstado.DisplayMember = nameof(StatusOption.Name);
        _cmbEstado.ValueMember = nameof(StatusOption.Code);
        _txtClaveTemporal.Enabled = user is null;
        if (user is not null)
        {
            _txtNombres.Text = user.GivenNames;
            _txtApellido.Text = user.Surname;
            _txtEmail.Text = user.Email;
            _cmbEstado.SelectedValue = user.StatusCode;
        }
        Shown += async (_, _) => await LoadRolesAsync();
    }

    /// <summary>Carga roles y actualiza la interfaz con los datos obtenidos.</summary>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private async Task LoadRolesAsync()
    {
        try
        {
            var roles = (await _apiClient.GetRolesAsync(_token)).ToList();
            _clbRoles.DataSource = roles;
            _clbRoles.DisplayMember = nameof(RoleSummaryResponse.Name);
            for (var index = 0; index < roles.Count; index++)
                _clbRoles.SetItemChecked(index, _user?.Roles.Contains(roles[index].Code, StringComparer.OrdinalIgnoreCase) == true);
        }
        catch (HttpRequestException exception) { ShowError(exception.Message); }
    }

    /// <summary>Procesa la acción de guardar solicitada desde la interfaz.</summary>
    /// <param name="sender">Control que originó el evento.</param>
    /// <param name="e">Datos asociados al evento de la interfaz.</param>
    private async void btnGuardar_Click(object? sender, EventArgs e)
    {
        var roles = _clbRoles.CheckedItems.Cast<RoleSummaryResponse>().Select(role => role.Code).ToList();
        if (string.IsNullOrWhiteSpace(_txtNombres.Text) || string.IsNullOrWhiteSpace(_txtApellido.Text)
            || string.IsNullOrWhiteSpace(_txtEmail.Text) || roles.Count == 0
            || (_user is null && string.IsNullOrWhiteSpace(_txtClaveTemporal.Text)))
        {
            MessageBox.Show(this, Localization.Text("UserEdit_Required"), "OxiTigre",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (_user is null && ChangePasswordForm.ValidatePassword(_txtClaveTemporal.Text) is { } passwordError)
        {
            MessageBox.Show(this, passwordError, "OxiTigre", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _btnGuardar.Enabled = false;
        try
        {
            if (_user is null)
                await _apiClient.CreateUserAsync(_token, new CreateUserRequest(_txtNombres.Text, _txtApellido.Text,
                    _txtEmail.Text, roles, _txtClaveTemporal.Text));
            else
                await _apiClient.UpdateUserAsync(_token, _user.UserId, new UpdateUserRequest(_txtNombres.Text,
                    _txtApellido.Text, _txtEmail.Text, _cmbEstado.SelectedValue?.ToString() ?? "ACTIVO", roles));
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (HttpRequestException exception) { ShowError(exception.Message); }
        finally { _btnGuardar.Enabled = true; }
    }

    /// <summary>Agrega un campo etiquetado a la distribución visual indicada.</summary>
    /// <param name="caption">Texto visible que identifica el campo.</param>
    /// <param name="control">Control visual asociado al campo o sección.</param>
    /// <param name="left">Posición horizontal del control.</param>
    /// <param name="top">Posición vertical del control.</param>
    /// <param name="width">Ancho asignado al control.</param>
    private void AddField(string caption, Control control, int left, int top, int width)
    {
        Controls.Add(new Label { Text = caption, Left = left, Top = top, AutoSize = true });
        control.SetBounds(left, top + 20, width, 27);
        Controls.Add(control);
    }

    /// <summary>Muestra un error funcional sin exponer detalles técnicos.</summary>
    /// <param name="message">Mensaje funcional que se muestra al usuario.</param>
    private void ShowError(string message) => MessageBox.Show(this, message, "OxiTigre", MessageBoxButtons.OK, MessageBoxIcon.Error);
    private sealed record StatusOption(string Code, string Name);
}
