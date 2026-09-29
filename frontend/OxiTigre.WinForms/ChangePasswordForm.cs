/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.WinForms.ChangePasswordForm
Archivo: ChangePasswordForm.cs | Versión: 1.2.0 | Fecha: 2026-08-26 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Solicita y confirma una contraseña nueva sin conservarla.
Historial: 1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.0.1 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Aplicación de estética visual compartida.
Historial: 1.1.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Validación local y mensaje controlado para valores vacíos.
Historial: 1.2.0 | 2026-08-26 | FABRICA | Agustin Omar Cauzi | Placeholders y acción visual homogénea.
===============================================================================
*/
namespace OxiTigre.WinForms;

/// <summary>Recopila una contraseña nueva y valida su confirmación local.</summary>
internal sealed class ChangePasswordForm : Form
{
    private readonly TextBox _txtNueva = new() { UseSystemPasswordChar = true };
    private readonly TextBox _txtConfirmacion = new() { UseSystemPasswordChar = true };

    /// <summary>Inicializa el diálogo de reemplazo de contraseña temporal.</summary>
    public ChangePasswordForm()
    {
        Text = Localization.Text("Password_Title");
        Width = 430;
        Height = 245;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        UiTheme.Apply(this);
        _txtNueva.PlaceholderText = Localization.Text("Password_New");
        _txtConfirmacion.PlaceholderText = Localization.Text("Password_Confirm");
        AddField(Localization.Text("Password_New"), _txtNueva, 25);
        AddField(Localization.Text("Password_Confirm"), _txtConfirmacion, 85);
        var btnAceptar = UiTheme.Button(Localization.Text("Password_Change"), 255, 155, 120);
        btnAceptar.Click += btnAceptar_Click;
        Controls.Add(btnAceptar);
        AcceptButton = btnAceptar;
    }

    /// <summary>Obtiene la contraseña nueva únicamente después de validar el diálogo.</summary>
    public string NewPassword => _txtNueva.Text;

    /// <summary>Agrega un campo etiquetado a la distribución visual indicada.</summary>
    /// <param name="caption">Texto visible que identifica el campo.</param>
    /// <param name="textBox">Cuadro de texto asociado al campo.</param>
    /// <param name="top">Posición vertical del control.</param>
    private void AddField(string caption, TextBox textBox, int top)
    {
        Controls.Add(new Label { Text = caption, AutoSize = true, Left = 35, Top = top });
        textBox.SetBounds(35, top + 22, 340, 27);
        Controls.Add(textBox);
    }

    /// <summary>Procesa la acción de aceptar solicitada desde la interfaz.</summary>
    /// <param name="sender">Control que originó el evento.</param>
    /// <param name="e">Datos asociados al evento de la interfaz.</param>
    private void btnAceptar_Click(object? sender, EventArgs e)
    {
        var validationMessage = ValidatePassword(_txtNueva.Text);
        if (validationMessage is not null)
        {
            MessageBox.Show(this, validationMessage, "OxiTigre", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (_txtNueva.Text != _txtConfirmacion.Text)
        {
            MessageBox.Show(this, Localization.Text("Password_NotMatch"), "OxiTigre",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    /// <summary>Valida la política visible compartida por claves nuevas y temporales.</summary>
    /// <param name="password">Contraseña que se valida contra la política vigente.</param>
    /// <returns>Mensaje localizado de validación, o nulo cuando la contraseña es válida.</returns>
    internal static string? ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password)) return Localization.Text("Password_Required");
        return password.Length < 10 || !password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit)
            ? Localization.Text("Password_Policy")
            : null;
    }
}
