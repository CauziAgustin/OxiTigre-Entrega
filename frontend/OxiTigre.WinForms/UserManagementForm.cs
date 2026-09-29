/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.WinForms.UserManagementForm
Archivo: UserManagementForm.cs | Versión: 2.2.0 | Fecha: 2026-08-26 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Administra usuarios, roles, estados, contraseñas temporales y sesiones.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Nomenclatura, validación y estética homogéneas.
Historial: 2.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Ciclo funcional completo de Seguridad.
Historial: 2.1.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Autorrevocación con cierre informado de la aplicación.
Historial: 2.1.1 | 2026-08-26 | FABRICA | Agustin Omar Cauzi | Búsqueda homogénea en la grilla.
Historial: 2.2.0 | 2026-08-26 | FABRICA | Agustin Omar Cauzi | Explicación operativa y corrección del área útil de la grilla.
===============================================================================
*/
using OxiTigre.ApiClient;
using OxiTigre.Contracts.Security;

namespace OxiTigre.WinForms;

/// <summary>Presenta la administración funcional de usuarios para una sesión administradora.</summary>
internal sealed class UserManagementForm : Form
{
    private readonly OxiTigreApiClient _apiClient;
    private readonly string _token;
    private readonly long _currentUserId;
    private readonly DataGridView _dgvUsuarios = new()
    {
        ReadOnly = true,
        AutoGenerateColumns = true,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false
    };

    /// <summary>Inicializa el componente de gestión de usuarios con sus dependencias y datos de trabajo.</summary>
    /// <param name="apiClient">Cliente usado para comunicarse con la API.</param>
    /// <param name="token">Token de la sesión autenticada.</param>
    /// <param name="currentUserId">Identificador del usuario actualmente autenticado.</param>
    internal UserManagementForm(OxiTigreApiClient apiClient, string token, long currentUserId)
    {
        _apiClient = apiClient;
        _token = token;
        _currentUserId = currentUserId;
        Text = Localization.Text("Users_Title");
        Width = 1020;
        Height = 650;
        MinimumSize = new Size(900, 560);
        StartPosition = FormStartPosition.CenterParent;
        UiTheme.Apply(this);

        Controls.Add(new Label
        {
            Text = Localization.Text("Users_Heading"),
            Left = 28,
            Top = 22,
            AutoSize = true,
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = UiTheme.Primary
        });
        Controls.Add(new Label
        {
            Text = Localization.Text("Users_Overview"),
            Left = 28,
            Top = 55,
            Width = 948,
            Height = 36,
            Padding = new Padding(8),
            BackColor = Color.FromArgb(230, 244, 234),
            ForeColor = UiTheme.Primary,
            AutoEllipsis = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        });
        var btnNuevo = UiTheme.Button(Localization.Text("Common_New"), 28, 101, 120);
        btnNuevo.Click += btnNuevo_Click;
        var btnEditar = UiTheme.Button(Localization.Text("Common_Edit"), 160, 101, 120);
        btnEditar.Click += btnEditar_Click;
        var btnClave = UiTheme.Button(Localization.Text("Users_ResetPassword"), 292, 101, 145);
        btnClave.Click += btnClave_Click;
        var btnRevocar = UiTheme.Button(Localization.Text("Users_RevokeSessions"), 449, 101, 155);
        btnRevocar.Click += btnRevocar_Click;
        var btnActualizar = UiTheme.Button(Localization.Text("Common_Refresh"), 616, 101, 120);
        btnActualizar.Click += async (_, _) => await LoadUsersAsync();
        Controls.AddRange([btnNuevo, btnEditar, btnClave, btnRevocar, btnActualizar]);

        var users = UiTheme.Searchable(_dgvUsuarios);
        users.SetBounds(28, 147, 948, 438);
        users.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _dgvUsuarios.CellDoubleClick += (_, eventArgs) => { if (eventArgs.RowIndex >= 0) EditSelected(); };
        UiTheme.Grid(_dgvUsuarios);
        Controls.Add(users);
        Shown += async (_, _) => await LoadUsersAsync();
    }

    private UserSummaryResponse? SelectedUser => (_dgvUsuarios.CurrentRow?.DataBoundItem as UserGridRow)?.User;

    /// <summary>Carga users y actualiza la interfaz con los datos obtenidos.</summary>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private async Task LoadUsersAsync()
    {
        try
        {
            _dgvUsuarios.DataSource = (await _apiClient.GetUsersAsync(_token)).Select(user => new UserGridRow(user,
                user.Username, user.GivenNames, user.Surname, user.Email,
                Localization.Text(user.StatusCode == "ACTIVO" ? "Common_Active" : "Common_Inactive"), string.Join(", ", user.Roles))).ToList();
            if (_dgvUsuarios.Columns[nameof(UserGridRow.User)] is { } user) user.Visible = false;
            Header(nameof(UserGridRow.Usuario), "Grid_User");
            Header(nameof(UserGridRow.Nombres), "Grid_GivenNames");
            Header(nameof(UserGridRow.Apellido), "Grid_Surname");
            Header(nameof(UserGridRow.Correo), "Grid_Email");
            Header(nameof(UserGridRow.Estado), "Grid_Status");
            Header(nameof(UserGridRow.Roles), "Users_Roles");
        }
        catch (HttpRequestException exception) { ShowError(exception.Message); }
    }

    /// <summary>Procesa la acción de crear un registro solicitada desde la interfaz.</summary>
    /// <param name="sender">Control que originó el evento.</param>
    /// <param name="e">Datos asociados al evento de la interfaz.</param>
    private void btnNuevo_Click(object? sender, EventArgs e)
    {
        using var form = new UserEditForm(_apiClient, _token);
        if (form.ShowDialog(this) == DialogResult.OK) _ = LoadUsersAsync();
    }

    /// <summary>Procesa la acción de editar el registro solicitada desde la interfaz.</summary>
    /// <param name="sender">Control que originó el evento.</param>
    /// <param name="e">Datos asociados al evento de la interfaz.</param>
    private void btnEditar_Click(object? sender, EventArgs e) => EditSelected();

    /// <summary>Abre el editor correspondiente a el registro seleccionado y conserva los cambios confirmados.</summary>
    private void EditSelected()
    {
        if (SelectedUser is not { } user) return;
        using var form = new UserEditForm(_apiClient, _token, user);
        if (form.ShowDialog(this) == DialogResult.OK) _ = LoadUsersAsync();
    }

    /// <summary>Procesa la acción de reiniciar la contraseña solicitada desde la interfaz.</summary>
    /// <param name="sender">Control que originó el evento.</param>
    /// <param name="e">Datos asociados al evento de la interfaz.</param>
    private async void btnClave_Click(object? sender, EventArgs e)
    {
        if (SelectedUser is not { } user) return;
        using var dialog = new ChangePasswordForm();
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            await _apiClient.ResetUserPasswordAsync(_token, user.UserId, dialog.NewPassword);
            MessageBox.Show(this, Localization.Text("Users_TemporaryAssigned"),
                "OxiTigre", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (HttpRequestException exception) { ShowError(exception.Message); }
    }

    /// <summary>Procesa la acción de revocar las sesiones solicitada desde la interfaz.</summary>
    /// <param name="sender">Control que originó el evento.</param>
    /// <param name="e">Datos asociados al evento de la interfaz.</param>
    private async void btnRevocar_Click(object? sender, EventArgs e)
    {
        if (SelectedUser is not { } user) return;
        var ownSession = user.UserId == _currentUserId;
        var question = ownSession
            ? Localization.Format("Users_RevokeOwnQuestion", user.Username)
            : Localization.Format("Users_RevokeQuestion", user.Username);
        if (MessageBox.Show(this, question, "OxiTigre",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        try
        {
            await _apiClient.RevokeUserSessionsAsync(_token, user.UserId);
            if (ownSession)
            {
                DialogResult = DialogResult.Abort;
                Close();
                return;
            }
            MessageBox.Show(this, Localization.Text("Users_Revoked"), "OxiTigre",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (HttpRequestException exception) { ShowError(exception.Message); }
    }

    /// <summary>Muestra un error funcional sin exponer detalles técnicos.</summary>
    /// <param name="message">Mensaje funcional que se muestra al usuario.</param>
    private void ShowError(string message) => MessageBox.Show(this, message, "OxiTigre", MessageBoxButtons.OK, MessageBoxIcon.Error);
    /// <summary>Resuelve el encabezado localizado de una columna o propiedad.</summary>
    /// <param name="property">Nombre de la propiedad o columna que se modifica.</param>
    /// <param name="resource">Clave del recurso localizado usado como encabezado.</param>
    private void Header(string property, string resource) { if (_dgvUsuarios.Columns[property] is { } column) column.HeaderText = Localization.Text(resource); }

    private sealed record UserGridRow(UserSummaryResponse User, string Usuario, string Nombres, string Apellido,
        string Correo, string Estado, string Roles);
}
