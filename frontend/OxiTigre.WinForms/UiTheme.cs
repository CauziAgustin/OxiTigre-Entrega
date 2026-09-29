/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.WinForms.UiTheme
Archivo: UiTheme.cs | Versión: 1.3.0 | Fecha: 2026-08-26 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Centraliza la estética nativa compartida por los formularios WinForms.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-21 | FABRICA | Agustin Omar Cauzi | Grillas sin selección implícita al enlazar o recargar datos.
Historial: 1.2.0 | 2026-08-26 | FABRICA | Agustin Omar Cauzi | Pestañas, importes, búsqueda y ayuda contextual homogéneos.
Historial: 1.3.0 | 2026-08-26 | FABRICA | Agustin Omar Cauzi | Filtro por columna y fecha parcial, ayudas circulares y botones semánticos.
===============================================================================
*/
namespace OxiTigre.WinForms;

/// <summary>Aplica colores, tipografía y estilos homogéneos sin dependencias visuales externas.</summary>
internal static class UiTheme
{
    private static readonly ToolTip HelpToolTip = new()
    {
        AutoPopDelay = 12000,
        InitialDelay = 300,
        ReshowDelay = 100,
    };
    internal static readonly Color Primary = Color.FromArgb(22, 101, 52);
    internal static readonly Color Surface = Color.White;
    internal static readonly Color Background = Color.FromArgb(244, 247, 245);
    internal static readonly Color Text = Color.FromArgb(31, 41, 35);

    /// <summary>Aplica colores, tipografía y escalado DPI al formulario.</summary>
    /// <param name="form">Formulario que recibirá el estilo compartido.</param>
    internal static void Apply(Form form)
    {
        form.BackColor = Background;
        form.ForeColor = Text;
        form.Font = new Font("Segoe UI", 9F);
        form.AutoScaleMode = AutoScaleMode.Dpi;
    }

    /// <summary>Crea un botón con color semántico, relieve y estados de puntero compartidos.</summary>
    /// <param name="text">Texto visible localizado.</param>
    /// <param name="left">Posición horizontal cuando el contenedor usa coordenadas.</param>
    /// <param name="top">Posición vertical cuando el contenedor usa coordenadas.</param>
    /// <param name="width">Ancho del botón en píxeles lógicos.</param>
    /// <returns>Botón configurado, todavía sin acción asociada.</returns>
    /// <param name="tone">Color semántico opcional; si se omite se deduce de la acción visible.</param>
    internal static Button Button(
        string text,
        int left,
        int top,
        int width = 130,
        ButtonTone? tone = null
    )
    {
        var colors = Colors(tone ?? InferTone(text));
        var button = new Button
        {
            Text = text,
            Left = left,
            Top = top,
            Width = width,
            Height = 34,
            BackColor = colors.Normal,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
            Padding = new Padding(2),
        };
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = colors.Border;
        button.FlatAppearance.MouseOverBackColor = colors.Hover;
        button.FlatAppearance.MouseDownBackColor = colors.Pressed;
        return button;
    }

    /// <summary>Configura una grilla homogénea y evita seleccionar implícitamente el primer registro.</summary>
    /// <param name="grid">Grilla que se configurará.</param>
    internal static void Grid(DataGridView grid)
    {
        grid.BackgroundColor = Surface;
        grid.BorderStyle = BorderStyle.None;
        grid.RowHeadersVisible = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Primary;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        grid.EnableHeadersVisualStyles = false;
        grid.DataBindingComplete += (_, _) =>
        {
            FormatNumericColumns(grid);
            ClearSelection(grid);
        };
        ClearSelection(grid);
    }

    /// <summary>Conserva el mismo tamaño legible de pestañas en todos los módulos.</summary>
    /// <param name="tabs">Control de pestañas que recibirá el espaciado compartido.</param>
    internal static void Tabs(TabControl tabs) => tabs.Padding = new Point(18, 8);

    /// <summary>Crea un editor numérico localizado con separador de miles visible.</summary>
    /// <param name="decimalPlaces">Cantidad de decimales permitidos.</param>
    /// <param name="width">Ancho del editor en píxeles lógicos.</param>
    /// <returns>Editor numérico con límites compatibles con <c>DECIMAL(19,4)</c>.</returns>
    internal static NumericUpDown Number(int decimalPlaces = 2, int width = 100) =>
        new()
        {
            Width = width,
            DecimalPlaces = decimalPlaces,
            Maximum = 999999999999999m,
            Minimum = 0,
            ThousandsSeparator = true,
        };

    /// <summary>Agrega búsqueda por columna y fecha parcial sin alterar la fuente de datos.</summary>
    /// <param name="grid">Grilla que se filtrará por sus celdas visibles.</param>
    /// <param name="help">Explicación opcional específica de la pantalla.</param>
    /// <param name="title">Título opcional que identifica el contenido de la grilla.</param>
    /// <returns>Panel que contiene el filtro, la ayuda y la grilla.</returns>
    internal static Control Searchable(DataGridView grid, string? help = null, string? title = null)
    {
        var panel = new Panel { Dock = DockStyle.Fill };
        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 44,
            Padding = new Padding(6, 5, 6, 4),
            WrapContents = false,
        };
        var columns = new ComboBox
        {
            Width = 170,
            DropDownStyle = ComboBoxStyle.DropDownList,
            AccessibleName = Localization.Text("Common_FilterColumn"),
        };
        var search = new TextBox
        {
            Width = 280,
            PlaceholderText = Localization.Text("Common_Search"),
            AccessibleName = Localization.Text("Common_Search"),
        };
        var clear = new Button
        {
            Text = "×",
            Width = 26,
            Height = 26,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            AccessibleName = Localization.Text("Common_ClearFilter"),
            BackColor = Color.White,
            ForeColor = Color.FromArgb(71, 85, 105),
            Margin = new Padding(3, 1, 3, 0),
        };
        clear.FlatAppearance.BorderColor = Color.FromArgb(148, 163, 184);
        grid.Dock = DockStyle.Fill;

        void RefreshColumns()
        {
            var selected = (columns.SelectedItem as GridColumnOption)?.Name;
            columns.BeginUpdate();
            columns.Items.Clear();
            columns.Items.Add(new GridColumnOption(null, Localization.Text("Common_AllColumns")));
            foreach (
                DataGridViewColumn column in grid
                    .Columns.Cast<DataGridViewColumn>()
                    .Where(item => item.Visible)
            )
                columns.Items.Add(new GridColumnOption(column.Name, column.HeaderText));
            columns.SelectedItem =
                columns.Items.Cast<GridColumnOption>().FirstOrDefault(item => item.Name == selected)
                ?? columns.Items[0];
            columns.EndUpdate();
        }

        void ApplyFilter()
        {
            var tokens = search.Text.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            );
            var selectedColumn = (columns.SelectedItem as GridColumnOption)?.Name;
            ClearSelection(grid);
            foreach (DataGridViewRow row in grid.Rows)
            {
                var cells = row
                    .Cells.Cast<DataGridViewCell>()
                    .Where(cell =>
                        cell.OwningColumn?.Visible == true
                        && (selectedColumn is null || cell.OwningColumn.Name == selectedColumn)
                    )
                    .ToArray();
                row.Visible =
                    tokens.Length == 0
                    || tokens.All(token => cells.Any(cell => CellMatches(cell, token)));
            }
        }

        search.TextChanged += (_, _) => ApplyFilter();
        columns.SelectedIndexChanged += (_, _) => ApplyFilter();
        columns.DropDown += (_, _) => RefreshColumns();
        clear.Click += (_, _) =>
        {
            search.Clear();
            search.Focus();
        };
        HelpToolTip.SetToolTip(clear, Localization.Text("Common_ClearFilter"));
        grid.DataBindingComplete += (_, _) =>
        {
            if (grid.IsHandleCreated)
                grid.BeginInvoke(() =>
                {
                    RefreshColumns();
                    ApplyFilter();
                });
        };
        if (!string.IsNullOrWhiteSpace(title))
            bar.Controls.Add(
                new Label
                {
                    Text = title,
                    AutoSize = true,
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    ForeColor = Primary,
                    Margin = new Padding(0, 5, 12, 0),
                }
            );
        bar.Controls.Add(columns);
        bar.Controls.Add(search);
        bar.Controls.Add(clear);
        bar.Controls.Add(HelpButton(help ?? Localization.Text("Common_SearchHelp")));
        panel.Controls.Add(grid);
        panel.Controls.Add(bar);
        RefreshColumns();
        return panel;
    }

    /// <summary>Muestra ayuda tanto con teclado/clic como al mantener el puntero.</summary>
    /// <param name="help">Texto localizado que se mostrará.</param>
    /// <returns>Botón accesible con ayuda emergente y diálogo informativo.</returns>
    internal static Button HelpButton(string help)
    {
        var button = new Button
        {
            Text = "?",
            Width = 24,
            Height = 24,
            FlatStyle = FlatStyle.Flat,
            TabStop = true,
            AccessibleName = Localization.Text("Common_Help"),
            Cursor = Cursors.Help,
            BackColor = Color.White,
            ForeColor = Primary,
            Margin = new Padding(3, 2, 3, 0),
        };
        button.FlatAppearance.BorderColor = Primary;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(220, 252, 231);
        button.Resize += (_, _) => MakeCircular(button);
        MakeCircular(button);
        HelpToolTip.SetToolTip(button, help);
        button.Click += (_, _) =>
            MessageBox.Show(
                help,
                Localization.Text("Common_Help"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        return button;
    }

    /// <summary>Limita campos estructurados a dígitos, incluso al pegar texto.</summary>
    /// <param name="textBoxes">Campos que solamente deben conservar dígitos.</param>
    internal static void DigitsOnly(params TextBox[] textBoxes)
    {
        foreach (var textBox in textBoxes)
            textBox.TextChanged += (_, _) =>
            {
                var digits = new string(textBox.Text.Where(char.IsDigit).ToArray());
                if (digits == textBox.Text)
                    return;
                textBox.Text = digits;
                textBox.SelectionStart = digits.Length;
            };
    }

    /// <summary>Presenta importes con dos decimales y cantidades con cuatro.</summary>
    /// <param name="grid">Grilla cuyas columnas numéricas se formatearán.</param>
    private static void FormatNumericColumns(DataGridView grid)
    {
        foreach (DataGridViewColumn column in grid.Columns)
        {
            var type =
                Nullable.GetUnderlyingType(column.ValueType ?? typeof(object)) ?? column.ValueType;
            if (type != typeof(decimal) && type != typeof(double) && type != typeof(float))
                continue;
            column.DefaultCellStyle.Format =
                column.Name.Contains("Total", StringComparison.OrdinalIgnoreCase)
                || column.Name.Contains("Cost", StringComparison.OrdinalIgnoreCase)
                || column.Name.Contains("Price", StringComparison.OrdinalIgnoreCase)
                || column.Name.Contains("Amount", StringComparison.OrdinalIgnoreCase)
                    ? "N2"
                    : "N4";
        }
    }

    /// <summary>Determina si una celda contiene el texto o la fecha parcial indicada.</summary>
    /// <param name="cell">Celda visible que se evaluará.</param>
    /// <param name="token">Palabra o fecha parcial escrita por el usuario.</param>
    /// <returns><see langword="true"/> cuando la celda coincide.</returns>
    private static bool CellMatches(DataGridViewCell cell, string token)
    {
        if (
            Convert
                .ToString(cell.FormattedValue)
                ?.Contains(token, StringComparison.CurrentCultureIgnoreCase) == true
        )
            return true;
        var date = cell.Value switch
        {
            DateOnly value => value,
            DateTime value => DateOnly.FromDateTime(value),
            DateTimeOffset value => DateOnly.FromDateTime(value.LocalDateTime),
            _ => (DateOnly?)null,
        };
        return date is not null && MatchesPartialDate(date.Value, token);
    }

    /// <summary>Acepta día, día/mes o día/mes/año con uno o más dígitos por componente.</summary>
    /// <param name="date">Fecha real de la celda.</param>
    /// <param name="value">Fecha parcial separada por barra, guion o punto.</param>
    /// <returns><see langword="true"/> si todos los componentes escritos coinciden.</returns>
    internal static bool MatchesPartialDate(DateOnly date, string value)
    {
        var parts = value.Split(
            ['/', '-', '.'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );
        if (parts.Length is < 1 or > 3 || parts.Any(part => !int.TryParse(part, out _)))
            return false;
        if (!int.TryParse(parts[0], out var day) || day != date.Day)
            return false;
        if (parts.Length > 1 && (!int.TryParse(parts[1], out var month) || month != date.Month))
            return false;
        return parts.Length < 3
            || date.Year.ToString().EndsWith(parts[2], StringComparison.Ordinal);
    }

    /// <summary>Convierte el botón de ayuda en un círculo conservando navegación por teclado.</summary>
    /// <param name="button">Botón cuya región visible se ajustará.</param>
    private static void MakeCircular(Button button)
    {
        using var path = new System.Drawing.Drawing2D.GraphicsPath();
        path.AddEllipse(0, 0, button.Width - 1, button.Height - 1);
        button.Region?.Dispose();
        button.Region = new Region(path);
    }

    /// <summary>Determina el tono visual de un botón a partir de la acción que representa.</summary>
    /// <param name="text">Texto que se muestra, interpreta o transforma.</param>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private static ButtonTone InferTone(string text)
    {
        var value = text.ToLowerInvariant();
        if (
            new[]
            {
                "cancel",
                "revers",
                "revocar",
                "eliminar",
                "quitar",
                "descartar",
                "discard",
                "cerrar sesión",
                "log out",
            }.Any(value.Contains)
        )
            return ButtonTone.Danger;
        if (
            new[]
            {
                "cambiar estado",
                "change status",
                "reiniciar",
                "reset",
                "cerrar saldo",
                "close balance",
            }.Any(value.Contains)
        )
            return ButtonTone.Warning;
        if (
            new[]
            {
                "editar",
                "edit",
                "actualizar",
                "refresh",
                "volver",
                "back",
                "ver detalle",
                "view details",
                "consult",
            }.Any(value.Contains)
        )
            return ButtonTone.Neutral;
        return ButtonTone.Primary;
    }

    /// <summary>Devuelve los colores normal, resaltado, presionado y borde del tono solicitado.</summary>
    /// <param name="tone">Tono semántico de la acción representada por el botón.</param>
    /// <returns>Objeto construido u obtenido por la operación.</returns>
    private static (Color Normal, Color Hover, Color Pressed, Color Border) Colors(
        ButtonTone tone
    ) =>
        tone switch
        {
            ButtonTone.Neutral => (
                Color.FromArgb(71, 85, 105),
                Color.FromArgb(100, 116, 139),
                Color.FromArgb(51, 65, 85),
                Color.FromArgb(51, 65, 85)
            ),
            ButtonTone.Warning => (
                Color.FromArgb(180, 83, 9),
                Color.FromArgb(217, 119, 6),
                Color.FromArgb(146, 64, 14),
                Color.FromArgb(146, 64, 14)
            ),
            ButtonTone.Danger => (
                Color.FromArgb(180, 35, 24),
                Color.FromArgb(217, 45, 32),
                Color.FromArgb(145, 28, 18),
                Color.FromArgb(145, 28, 18)
            ),
            _ => (
                Primary,
                Color.FromArgb(21, 128, 61),
                Color.FromArgb(20, 83, 45),
                Color.FromArgb(20, 83, 45)
            ),
        };

    /// <summary>Evita ejecutar acciones sobre una fila que el usuario todavía no eligió.</summary>
    /// <param name="grid">Grilla cuya selección actual se limpiará.</param>
    private static void ClearSelection(DataGridView grid)
    {
        grid.ClearSelection();
        grid.CurrentCell = null;
    }

    private sealed record GridColumnOption(string? Name, string Label)
    {
        /// <inheritdoc />
        public override string ToString() => Label;
    }
}

/// <summary>Identifica el significado visual de una acción.</summary>
internal enum ButtonTone
{
    Primary,
    Neutral,
    Warning,
    Danger,
}
