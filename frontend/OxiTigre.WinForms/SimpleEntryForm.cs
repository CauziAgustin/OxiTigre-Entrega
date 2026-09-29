/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.WinForms.SimpleEntryForm
Archivo: SimpleEntryForm.cs | Versión: 1.3.0 | Fecha: 2026-08-27 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Captura campos breves en los módulos Compras y Trazabilidad con diseño nativo homogéneo.
Historial: 1.0.0 | 2026-08-24 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-26 | FABRICA | Agustin Omar Cauzi | Máscaras, números localizados y ayuda accesible.
Historial: 1.2.0 | 2026-08-26 | FABRICA | Agustin Omar Cauzi | Placeholders y ayuda compacta sin deformar campos multilínea.
Historial: 1.3.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Formateo visual opcional durante la escritura.
===============================================================================
*/
namespace OxiTigre.WinForms;

/// <summary>Describe un campo de texto o selección.</summary>
internal sealed record EntryField(
    string Key,
    string Label,
    string? Value = null,
    IReadOnlyList<KeyValuePair<string, string>>? Options = null,
    bool Required = false,
    bool Multiline = false,
    string? Mask = null,
    bool Numeric = false,
    string? Help = null,
    Func<string, string>? Formatter = null,
    bool ReadOnly = false
);

/// <summary>Formulario nativo reutilizado por operaciones simples y controladas.</summary>
internal sealed class SimpleEntryForm : Form
{
    private readonly Dictionary<string, Control> _controls = [];

    /// <summary>Construye campos nativos, ayudas y validación de obligatoriedad para un diálogo breve.</summary>
    /// <param name="title">Título de la acción visible al operador.</param>
    /// <param name="fields">Definiciones de campos en el orden en que deben mostrarse.</param>
    internal SimpleEntryForm(string title, params EntryField[] fields)
    {
        Text = title;
        Width = 580;
        Height = Math.Min(760, 150 + fields.Sum(field => field.Multiline ? 110 : 48));
        MinimumSize = new Size(500, 260);
        StartPosition = FormStartPosition.CenterParent;
        UiTheme.Apply(this);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 2,
            RowCount = fields.Length + 1,
            AutoScroll = true,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));

        for (var index = 0; index < fields.Length; index++)
        {
            var field = fields[index];
            var height = field.Multiline ? 96 : 38;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            layout.Controls.Add(new Label
            {
                Text = field.Label + (field.Required ? " *" : string.Empty),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
            }, 0, index);

            Control input;
            if (field.Options is { } options)
            {
                var combo = new ComboBox
                {
                    Dock = DockStyle.Top,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    DisplayMember = "Value",
                    ValueMember = "Key",
                    DataSource = options.ToList(),
                };
                combo.SelectedValue = field.Value ?? options.FirstOrDefault().Key;
                input = combo;
            }
            else if (field.Numeric)
            {
                var number = UiTheme.Number(4, 180);
                if (
                    decimal.TryParse(field.Value, out var value)
                    && value >= number.Minimum
                    && value <= number.Maximum
                )
                {
                    number.Value = value;
                }

                input = number;
            }
            else if (field.Mask is not null)
            {
                input = new MaskedTextBox(field.Mask)
                {
                    Dock = DockStyle.Top,
                    Text = field.Value ?? string.Empty,
                    TextMaskFormat = MaskFormat.IncludeLiterals,
                    CutCopyMaskFormat = MaskFormat.IncludeLiterals,
                };
            }
            else
            {
                input = new TextBox
                {
                    Dock = field.Multiline ? DockStyle.Fill : DockStyle.Top,
                    Multiline = field.Multiline,
                    ScrollBars = field.Multiline ? ScrollBars.Vertical : ScrollBars.None,
                    Text = field.Value ?? string.Empty,
                    PlaceholderText = field.Label,
                    ReadOnly = field.ReadOnly,
                };
            }

            if (input is TextBox textBox && field.Formatter is not null)
            {
                var formatting = false;
                textBox.TextChanged += (_, _) =>
                {
                    if (formatting)
                    {
                        return;
                    }

                    var formatted = field.Formatter(textBox.Text);
                    if (formatted == textBox.Text)
                    {
                        return;
                    }

                    formatting = true;
                    textBox.Text = formatted;
                    textBox.SelectionStart = formatted.Length;
                    formatting = false;
                };
            }

            _controls[field.Key] = input;
            if (field.Help is null)
            {
                layout.Controls.Add(input, 1, index);
            }
            else
            {
                var host = new TableLayoutPanel
                {
                    Dock = field.Multiline ? DockStyle.Fill : DockStyle.Top,
                    Height = field.Multiline ? height - 8 : 30,
                    ColumnCount = 2,
                    RowCount = 1,
                };
                host.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                host.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 32));
                input.Dock = DockStyle.Fill;
                var help = UiTheme.HelpButton(field.Help);
                help.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                host.Controls.Add(input, 0, 0);
                host.Controls.Add(help, 1, 0);
                layout.Controls.Add(host, 1, index);
            }
        }

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        var save = UiTheme.Button(Localization.Text("Common_Save"), 0, 0, 130);
        save.DialogResult = DialogResult.OK;
        var cancel = UiTheme.Button(Localization.Text("Common_Cancel"), 0, 0, 130);
        cancel.DialogResult = DialogResult.Cancel;
        buttons.Controls.Add(save);
        buttons.Controls.Add(cancel);
        layout.Controls.Add(buttons, 0, fields.Length);
        layout.SetColumnSpan(buttons, 2);
        AcceptButton = save;
        CancelButton = cancel;
        Controls.Add(layout);

        save.Click += (_, _) =>
        {
            if (fields.Any(field => field.Required && string.IsNullOrWhiteSpace(this[field.Key])))
            {
                DialogResult = DialogResult.None;
                MessageBox.Show(
                    this,
                    Localization.Text("Common_RequiredFields"),
                    "OxiTigre",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
            }
        };
    }

    /// <summary>Devuelve el valor del campo según su control, sin aceptar máscaras incompletas.</summary>
    /// <param name="key">Clave de la definición del campo.</param>
    /// <returns>Selección, número localizado o texto normalizado; vacío si la máscara está incompleta.</returns>
    /// <exception cref="KeyNotFoundException">La clave no pertenece a este diálogo.</exception>
    internal string this[string key] => _controls[key] switch
    {
        ComboBox combo => Convert.ToString(combo.SelectedValue) ?? string.Empty,
        NumericUpDown number => number.Value.ToString(System.Globalization.CultureInfo.CurrentCulture),
        MaskedTextBox mask when !mask.MaskCompleted => string.Empty,
        _ => _controls[key].Text.Trim(),
    };
}
