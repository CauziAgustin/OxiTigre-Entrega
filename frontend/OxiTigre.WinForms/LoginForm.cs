/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.WinForms.LoginForm
Archivo: LoginForm.cs | Versión: 2.2.0 | Fecha: 2026-09-29 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Autentica al usuario y permite elegir empresa y sucursal.
Historial: 1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 2.0.0 | 2026-09-02 | FABRICA | Agustin Omar Cauzi | Acceso visual adaptable en dos etapas con sucursal real.
Historial: 2.1.0 | 2026-09-28 | FABRICA | Agustin Omar Cauzi | Estado de API alineado al cambiar su contenido.
Historial: 2.2.0 | 2026-09-29 | FABRICA | Agustin Omar Cauzi | Nombre de sucursal sin prefijos duplicados.
===============================================================================
*/
using OxiTigre.ApiClient;
using OxiTigre.Contracts.Security;

namespace OxiTigre.WinForms;

/// <summary>Presenta el acceso inicial contra la API, sin conexión directa a SQL Server.</summary>
internal sealed class LoginForm : Form
{
    private static readonly Color BrandGreen = Color.FromArgb(6, 78, 55);
    private static readonly Color MutedText = Color.FromArgb(100, 116, 139);
    private static readonly Color SoftGreen = Color.FromArgb(232, 245, 237);
    private static readonly Color Border = Color.FromArgb(210, 220, 215);

    private readonly OxiTigreApiClient _apiClient;
    private readonly TextBox _txtUsuario = new();
    private readonly TextBox _txtClave = new() { UseSystemPasswordChar = true };
    private readonly Panel _credentialsPanel = new() { Dock = DockStyle.Fill };
    private readonly Panel _companyPanel = new() { Dock = DockStyle.Fill, Visible = false };
    private readonly FlowLayoutPanel _accessList = new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = true,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        Padding = new Padding(0, 2, 6, 2),
    };
    private readonly Label _contentTitle = new();
    private readonly Label _contentHelp = new();
    private readonly Label _validatedUser = new();
    private readonly Label _status = new();
    private readonly Label _apiStatus = new();
    private readonly Label _brandSubtitle = new();
    private readonly Label _brandBenefits = new();
    private readonly Label _userCaption = new();
    private readonly Label _passwordCaption = new();
    private readonly Label _security = new();
    private readonly Label _securityHelp = new();
    private readonly Panel _stepIndicator = new();
    private readonly CheckBox _rememberUser = new() { Checked = true, AutoSize = true };
    private readonly CheckBox _rememberAccess = new() { AutoSize = true };
    private readonly Button _showPassword = new();
    private readonly Button _continueButton;
    private readonly Button _enterButton;
    private readonly Button _backButton;
    private readonly Button _spanishButton;
    private readonly Button _englishButton;
    private IReadOnlyList<CompanyOptionResponse> _companies = [];
    private AccessOption? _selectedAccess;
    private int _activeStep = 1;

    /// <summary>Inicializa los controles del formulario de acceso.</summary>
    /// <param name="apiClient">Cliente HTTP utilizado para autenticar sin acceder directamente a la base.</param>
    public LoginForm(OxiTigreApiClient apiClient)
    {
        _apiClient = apiClient;
        _continueButton = CreateActionButton();
        _enterButton = CreateActionButton();
        _backButton = CreateLinkButton();
        _spanishButton = CreateLanguageButton("ES");
        _englishButton = CreateLanguageButton("EN");

        ClientSize = new Size(840, 820);
        MinimumSize = new Size(840, 820);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        UiTheme.Apply(this);

        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
        };
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        shell.Controls.Add(BuildBrandPanel(), 0, 0);
        shell.Controls.Add(BuildAccessPanel(), 1, 0);
        var stepIndicator = BuildStepIndicator();
        shell.Controls.Add(stepIndicator, 0, 1);
        shell.SetColumnSpan(stepIndicator, 2);
        Controls.Add(shell);

        _txtUsuario.Text = LoadLastUser();
        _txtUsuario.TextChanged += CredentialsChanged;
        _txtClave.TextChanged += CredentialsChanged;
        _continueButton.Click += async (_, _) => await ValidateCredentialsAsync();
        _enterButton.Click += async (_, _) => await LoginAsync();
        _backButton.Click += (_, _) => ShowCredentialsStep();
        _showPassword.Click += (_, _) =>
        {
            _txtClave.UseSystemPasswordChar = !_txtClave.UseSystemPasswordChar;
            UpdatePasswordVisibilityButton();
        };
        _spanishButton.Click += (_, _) => ChangeLanguage("es-AR");
        _englishButton.Click += (_, _) => ChangeLanguage("en-US");
        _accessList.SizeChanged += (_, _) => ResizeAccessCards();
        Shown += async (_, _) =>
        {
            ShowCredentialsStep();
            await CheckApiAsync();
        };

        ApplyLanguage();
    }

    /// <summary>Obtiene la sesión creada luego de un acceso correcto.</summary>
    public LoginResponse? Session { get; private set; }

    /// <summary>Obtiene el nombre visible de la empresa seleccionada.</summary>
    public string? SelectedCompanyName { get; private set; }

    /// <summary>Obtiene el nombre visible de la sucursal seleccionada.</summary>
    public string? SelectedBranchName { get; private set; }

    /// <summary>Construye el área institucional izquierda del acceso.</summary>
    /// <returns>Panel adaptable con identidad y beneficios del sistema.</returns>
    private Control BuildBrandPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = BrandGreen,
            Padding = new Padding(30, 38, 28, 28),
        };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = BrandGreen,
            Margin = Padding.Empty,
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 170));
        var brandHeader = new Panel { Dock = DockStyle.Fill };
        var brandMark = CreateBrandMark();
        brandMark.Left = 0;
        brandMark.Top = 5;
        brandHeader.Controls.Add(brandMark);
        brandHeader.Controls.Add(
            new Label
            {
                Text = "OxiTigre",
                Left = 70,
                Top = 14,
                Width = 190,
                Height = 54,
                Font = new Font("Segoe UI", 25F, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft,
            }
        );
        layout.Controls.Add(brandHeader, 0, 0);
        _brandSubtitle.Dock = DockStyle.Fill;
        _brandSubtitle.Font = new Font("Segoe UI", 12F);
        _brandSubtitle.ForeColor = Color.FromArgb(220, 235, 226);
        layout.Controls.Add(_brandSubtitle, 0, 1);
        layout.Controls.Add(CreateIndustrialIllustration(), 0, 2);
        _brandBenefits.Dock = DockStyle.Fill;
        _brandBenefits.Font = new Font("Segoe UI", 11F);
        _brandBenefits.ForeColor = Color.White;
        _brandBenefits.TextAlign = ContentAlignment.BottomLeft;
        layout.Controls.Add(_brandBenefits, 0, 3);
        panel.Controls.Add(layout);
        return panel;
    }

    /// <summary>Construye el área derecha con idioma, estado de API y pasos de acceso.</summary>
    /// <returns>Panel adaptable que contiene ambas etapas.</returns>
    private Control BuildAccessPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        var topBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 106,
            BackColor = Color.White,
        };
        _englishButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _englishButton.Location = new Point(370, 18);
        _spanishButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _spanishButton.Location = new Point(290, 18);
        _apiStatus.AutoSize = true;
        _apiStatus.Padding = new Padding(10, 7, 10, 6);
        _apiStatus.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _apiStatus.Location = new Point(322, 57);
        topBar.Controls.Add(_englishButton);
        topBar.Controls.Add(_spanishButton);
        topBar.Controls.Add(_apiStatus);
        _apiStatus.SizeChanged += (_, _) =>
            _apiStatus.Left = topBar.ClientSize.Width - _apiStatus.Width - 20;
        topBar.Resize += (_, _) =>
        {
            _englishButton.Left = topBar.ClientSize.Width - _englishButton.Width - 20;
            _spanishButton.Left = _englishButton.Left - _spanishButton.Width;
            _apiStatus.Left = topBar.ClientSize.Width - _apiStatus.Width - 20;
        };

        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(42, 8, 42, 24),
        };
        var cardLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
        };
        cardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        cardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        cardLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        cardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        cardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 10));
        cardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        _contentTitle.Dock = DockStyle.Fill;
        _contentTitle.Font = new Font("Segoe UI", 24F, FontStyle.Bold);
        _contentTitle.ForeColor = UiTheme.Text;
        _contentHelp.Dock = DockStyle.Fill;
        _contentHelp.Font = new Font("Segoe UI", 11F);
        _contentHelp.ForeColor = MutedText;
        _contentHelp.AutoEllipsis = true;
        _status.Dock = DockStyle.Fill;
        _status.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _status.TextAlign = ContentAlignment.MiddleLeft;
        _security.Dock = DockStyle.Fill;
        _security.BackColor = Color.FromArgb(247, 250, 248);
        _security.BorderStyle = BorderStyle.FixedSingle;
        _security.ForeColor = MutedText;
        _security.Padding = new Padding(14, 8, 14, 8);
        _security.TextAlign = ContentAlignment.MiddleLeft;
        cardLayout.Controls.Add(_contentTitle, 0, 0);
        cardLayout.Controls.Add(_contentHelp, 0, 1);
        cardLayout.Controls.Add(BuildStepHost(), 0, 2);
        cardLayout.Controls.Add(_status, 0, 3);
        cardLayout.Controls.Add(_security, 0, 5);
        card.Controls.Add(cardLayout);
        panel.Controls.Add(card);
        panel.Controls.Add(topBar);
        return panel;
    }

    /// <summary>Superpone las dos etapas para mostrar una sola por vez.</summary>
    /// <returns>Contenedor de credenciales y selección operativa.</returns>
    private Control BuildStepHost()
    {
        var host = new Panel { Dock = DockStyle.Fill };
        BuildCredentialsStep();
        BuildCompanyStep();
        host.Controls.Add(_credentialsPanel);
        host.Controls.Add(_companyPanel);
        return host;
    }

    /// <summary>Compone usuario, contraseña, preferencia local y acción de validación.</summary>
    private void BuildCredentialsStep()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 7,
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _userCaption.Dock = DockStyle.Fill;
        _passwordCaption.Dock = DockStyle.Fill;
        _userCaption.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
        _passwordCaption.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
        ConfigureTextBox(_txtUsuario);
        ConfigureTextBox(_txtClave);
        var passwordRow = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
        };
        _txtClave.Dock = DockStyle.None;
        _txtClave.BorderStyle = BorderStyle.None;
        _txtClave.BackColor = Color.White;
        _txtClave.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
        _showPassword.Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;
        _showPassword.ImageAlign = ContentAlignment.MiddleCenter;
        _showPassword.FlatStyle = FlatStyle.Flat;
        _showPassword.BackColor = Color.White;
        _showPassword.ForeColor = BrandGreen;
        _showPassword.Cursor = Cursors.Hand;
        _showPassword.FlatAppearance.BorderSize = 0;
        UpdatePasswordVisibilityButton();
        passwordRow.Controls.Add(_txtClave);
        passwordRow.Controls.Add(_showPassword);
        passwordRow.Resize += (_, _) =>
        {
            _showPassword.SetBounds(
                passwordRow.ClientSize.Width - 44,
                1,
                42,
                passwordRow.ClientSize.Height - 2
            );
            _txtClave.SetBounds(
                10,
                10,
                Math.Max(80, passwordRow.ClientSize.Width - 58),
                _txtClave.PreferredHeight
            );
        };
        _rememberUser.Font = new Font("Segoe UI", 10F);
        _rememberUser.Margin = new Padding(0, 12, 0, 0);
        _continueButton.Dock = DockStyle.Fill;
        _continueButton.Margin = new Padding(0, 8, 0, 0);
        var helpRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 12, 0, 0),
        };
        var helpButton = new Button
        {
            Text = "?",
            Width = 28,
            Height = 28,
            FlatStyle = FlatStyle.Flat,
            BackColor = SoftGreen,
            ForeColor = BrandGreen,
            Cursor = Cursors.Hand,
            Margin = Padding.Empty,
        };
        helpButton.FlatAppearance.BorderSize = 0;
        helpButton.Resize += (_, _) =>
        {
            using var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddEllipse(
                1,
                1,
                Math.Max(1, helpButton.Width - 2),
                Math.Max(1, helpButton.Height - 2)
            );
            helpButton.Region = new Region(path);
        };
        helpButton.Click += (_, _) =>
            MessageBox.Show(
                this,
                Localization.Text("Login_Help"),
                "OxiTigre",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        _securityHelp.AutoSize = true;
        _securityHelp.Font = new Font("Segoe UI", 9.5F);
        _securityHelp.ForeColor = MutedText;
        _securityHelp.Margin = new Padding(8, 5, 0, 0);
        helpRow.Controls.Add(helpButton);
        helpRow.Controls.Add(_securityHelp);
        layout.Controls.Add(_userCaption, 0, 0);
        layout.Controls.Add(_txtUsuario, 0, 1);
        layout.Controls.Add(_passwordCaption, 0, 2);
        layout.Controls.Add(passwordRow, 0, 3);
        layout.Controls.Add(_rememberUser, 0, 4);
        layout.Controls.Add(_continueButton, 0, 5);
        layout.Controls.Add(helpRow, 0, 6);
        _credentialsPanel.Controls.Add(layout);
    }

    /// <summary>Compone las tarjetas de empresa y sucursal posteriores a la validación.</summary>
    private void BuildCompanyStep()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        _validatedUser.Dock = DockStyle.Fill;
        _validatedUser.BackColor = SoftGreen;
        _validatedUser.ForeColor = BrandGreen;
        _validatedUser.Padding = new Padding(12, 10, 12, 8);
        _validatedUser.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _rememberAccess.Margin = new Padding(0, 10, 0, 0);
        _enterButton.Dock = DockStyle.Fill;
        _enterButton.Margin = new Padding(0, 8, 0, 0);
        _backButton.Dock = DockStyle.Fill;
        layout.Controls.Add(_validatedUser, 0, 0);
        layout.Controls.Add(_accessList, 0, 1);
        layout.Controls.Add(_rememberAccess, 0, 2);
        layout.Controls.Add(_enterButton, 0, 3);
        layout.Controls.Add(_backButton, 0, 4);
        _companyPanel.Controls.Add(layout);
    }

    /// <summary>Dibuja la marca compacta de OxiTigre usando únicamente GDI+ nativo.</summary>
    /// <returns>Control que representa la marca sin archivos externos.</returns>
    private static Control CreateBrandMark()
    {
        var mark = new Panel
        {
            Width = 62,
            Height = 62,
            Margin = Padding.Empty,
        };
        mark.Paint += (_, eventArgs) =>
        {
            eventArgs.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var background = new SolidBrush(Color.FromArgb(241, 168, 17));
            using var border = new Pen(Color.FromArgb(255, 195, 54), 2F);
            using var textBrush = new SolidBrush(BrandGreen);
            using var font = new Font("Segoe UI", 16F, FontStyle.Bold);
            var badge = new Rectangle(5, 5, 52, 52);
            eventArgs.Graphics.FillEllipse(background, badge);
            eventArgs.Graphics.DrawEllipse(border, badge);
            using var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
            };
            eventArgs.Graphics.DrawString("OT", font, textBrush, badge, format);
        };
        return mark;
    }

    /// <summary>Dibuja una referencia industrial tenue como la propuesta aprobada.</summary>
    /// <returns>Panel decorativo sin interacción ni dependencia externa.</returns>
    private static Control CreateIndustrialIllustration()
    {
        var illustration = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
        illustration.Paint += (_, eventArgs) =>
        {
            eventArgs.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var line = new Pen(Color.FromArgb(105, 180, 145), 1.5F);
            var width = illustration.ClientSize.Width;
            var height = illustration.ClientSize.Height;
            var floor = height - 38;
            eventArgs.Graphics.DrawLine(line, 5, floor, width - 8, floor - 16);
            eventArgs.Graphics.DrawRectangle(line, 28, floor - 62, 78, 62);
            eventArgs.Graphics.DrawLine(line, 28, floor - 62, 67, floor - 88);
            eventArgs.Graphics.DrawLine(line, 67, floor - 88, 106, floor - 62);
            eventArgs.Graphics.DrawRectangle(line, 122, floor - 148, 42, 142);
            eventArgs.Graphics.DrawArc(line, 122, floor - 160, 42, 24, 180, 180);
            eventArgs.Graphics.DrawRectangle(line, 177, floor - 174, 48, 168);
            eventArgs.Graphics.DrawArc(line, 177, floor - 188, 48, 28, 180, 180);
            eventArgs.Graphics.DrawArc(line, 12, floor - 20, width - 30, 95, 180, 135);
            eventArgs.Graphics.DrawEllipse(line, width - 72, floor - 40, 27, 17);
            eventArgs.Graphics.DrawEllipse(line, width - 68, floor - 24, 8, 8);
            eventArgs.Graphics.DrawEllipse(line, width - 51, floor - 24, 8, 8);
        };
        return illustration;
    }

    /// <summary>Crea el indicador visual de avance entre los dos pasos.</summary>
    /// <returns>Indicador adaptable de la etapa actual.</returns>
    private Control BuildStepIndicator()
    {
        _stepIndicator.Dock = DockStyle.Fill;
        _stepIndicator.BackColor = Color.White;
        _stepIndicator.Paint += (_, eventArgs) =>
        {
            eventArgs.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var centerY = _stepIndicator.ClientSize.Height / 2;
            var firstX = Math.Max(28, _stepIndicator.ClientSize.Width / 9);
            var secondX = _stepIndicator.ClientSize.Width / 2 + 36;
            DrawStep(
                eventArgs.Graphics,
                firstX,
                centerY,
                "1",
                Localization.Text("Login_StepCredentials"),
                _activeStep == 1
            );
            DrawStep(
                eventArgs.Graphics,
                secondX,
                centerY,
                "2",
                Localization.Text("Login_StepAccess"),
                _activeStep == 2
            );
            using var separator = new Pen(Color.FromArgb(203, 213, 225), 1.5F);
            eventArgs.Graphics.DrawLine(separator, firstX + 182, centerY, secondX - 24, centerY);
        };
        return _stepIndicator;
    }

    /// <summary>Dibuja un paso centrado y estable dentro del indicador inferior.</summary>
    /// <param name="graphics">Superficie sobre la que se dibuja.</param>
    /// <param name="x">Posición horizontal del círculo.</param>
    /// <param name="centerY">Centro vertical del indicador.</param>
    /// <param name="number">Número del paso.</param>
    /// <param name="text">Descripción localizada del paso.</param>
    /// <param name="active">Indica si debe destacarse.</param>
    private static void DrawStep(
        Graphics graphics,
        int x,
        int centerY,
        string number,
        string text,
        bool active
    )
    {
        var circle = new Rectangle(x, centerY - 17, 34, 34);
        using var fill = new SolidBrush(active ? BrandGreen : Color.FromArgb(238, 242, 240));
        using var numberBrush = new SolidBrush(active ? Color.White : MutedText);
        using var textBrush = new SolidBrush(active ? BrandGreen : MutedText);
        using var numberFont = new Font("Segoe UI", 10F, FontStyle.Bold);
        using var textFont = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        using var centered = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
        };
        graphics.FillEllipse(fill, circle);
        graphics.DrawString(number, numberFont, numberBrush, circle, centered);
        graphics.DrawString(text, textFont, textBrush, x + 46, centerY - 9);
    }

    /// <summary>Aplica el estilo compartido a un campo del acceso.</summary>
    /// <param name="textBox">Campo que se configurará.</param>
    private static void ConfigureTextBox(TextBox textBox)
    {
        textBox.Dock = DockStyle.Fill;
        textBox.BorderStyle = BorderStyle.FixedSingle;
        textBox.Font = new Font("Segoe UI", 12F);
        textBox.Margin = Padding.Empty;
    }

    /// <summary>Actualiza el ícono del botón según la visibilidad de la contraseña.</summary>
    private void UpdatePasswordVisibilityButton()
    {
        var previous = _showPassword.Image;
        _showPassword.Image = CreateEyeIcon(!_txtClave.UseSystemPasswordChar);
        previous?.Dispose();
    }

    /// <summary>Dibuja un ícono de ojo legible sin depender de archivos externos.</summary>
    /// <param name="passwordVisible">Indica si la contraseña está visible y debe mostrarse el ojo tachado.</param>
    /// <returns>Imagen lista para el botón de visibilidad.</returns>
    private static Bitmap CreateEyeIcon(bool passwordVisible)
    {
        var image = new Bitmap(24, 24);
        using var graphics = Graphics.FromImage(image);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var pen = new Pen(BrandGreen, 2F)
        {
            StartCap = System.Drawing.Drawing2D.LineCap.Round,
            EndCap = System.Drawing.Drawing2D.LineCap.Round,
        };
        using var eye = new System.Drawing.Drawing2D.GraphicsPath();
        eye.AddBezier(2, 12, 7, 5, 17, 5, 22, 12);
        eye.AddBezier(22, 12, 17, 19, 7, 19, 2, 12);
        graphics.DrawPath(pen, eye);
        graphics.DrawEllipse(pen, 9, 9, 6, 6);
        if (passwordVisible)
        {
            using var background = new Pen(Color.White, 4F);
            graphics.DrawLine(background, 4, 3, 21, 20);
            graphics.DrawLine(pen, 4, 3, 21, 20);
        }

        return image;
    }

    /// <summary>Crea una acción principal con el tema existente.</summary>
    /// <returns>Botón primario todavía sin texto ni evento.</returns>
    private static Button CreateActionButton()
    {
        var button = UiTheme.Button(string.Empty, 0, 0, 180);
        button.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        return button;
    }

    /// <summary>Crea una acción secundaria visualmente liviana.</summary>
    /// <returns>Botón secundario todavía sin texto ni evento.</returns>
    private static Button CreateLinkButton()
    {
        var button = new Button
        {
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = BrandGreen,
            Cursor = Cursors.Hand,
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = SoftGreen;
        return button;
    }

    /// <summary>Crea un selector compacto de idioma.</summary>
    /// <param name="text">Nombre del idioma que se mostrará.</param>
    /// <returns>Botón de idioma listo para asociar a una cultura.</returns>
    private static Button CreateLanguageButton(string text)
    {
        var button = new Button
        {
            Text = text,
            Width = 76,
            Height = 30,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = BrandGreen,
            Cursor = Cursors.Hand,
            Margin = new Padding(4, 0, 0, 0),
        };
        button.FlatAppearance.BorderColor = Border;
        return button;
    }

    /// <summary>Actualiza todos los textos visibles sin reiniciar ni cerrar la sesión.</summary>
    private void ApplyLanguage()
    {
        Text = Localization.Text("Login_Title");
        _brandSubtitle.Text = Localization.Text("Login_BrandSubtitle");
        _brandBenefits.Text = Localization.Text("Login_BrandBenefits");
        _userCaption.Text = Localization.Text("Login_User");
        _passwordCaption.Text = Localization.Text("Login_Password");
        _txtUsuario.PlaceholderText = Localization.Text("Login_User");
        _txtClave.PlaceholderText = Localization.Text("Login_Password");
        _rememberUser.Text = Localization.Text("Login_RememberUser");
        _rememberAccess.Text = Localization.Text("Login_RememberAccess");
        _continueButton.Text = Localization.Text("Login_Continue");
        _enterButton.Text = Localization.Text("Login_EnterSystem");
        _backButton.Text = Localization.Text("Login_ChangeUser");
        _showPassword.AccessibleName = Localization.Text("Login_ShowPassword");
        _securityHelp.Text = Localization.Text("Login_SecurityHelp");
        _stepIndicator.Invalidate();
        _spanishButton.Text = Localization.Text("Login_LanguageSpanish");
        _englishButton.Text = Localization.Text("Login_LanguageEnglish");
        _validatedUser.Text = Localization.Format("Login_ValidatedAccess", _txtUsuario.Text);
        UpdateLanguageButtons();

        if (_companyPanel.Visible)
        {
            ShowCompanyStep();
            BuildAccessCards();
        }
        else
        {
            ShowCredentialsStep(false);
        }
    }

    /// <summary>Cambia y recuerda la cultura elegida en este equipo.</summary>
    /// <param name="cultureCode">Código de una cultura soportada.</param>
    private void ChangeLanguage(string cultureCode)
    {
        Localization.SetCulture(cultureCode);
        Localization.SaveCulture(cultureCode);
        ApplyLanguage();
        _ = CheckApiAsync();
    }

    /// <summary>Destaca el idioma actualmente activo.</summary>
    private void UpdateLanguageButtons()
    {
        var english = System.Globalization.CultureInfo.CurrentUICulture.Name == "en-US";
        StyleSelectedLanguage(_englishButton, english);
        StyleSelectedLanguage(_spanishButton, !english);
    }

    /// <summary>Aplica el estado seleccionado a un botón de idioma.</summary>
    /// <param name="button">Botón que se actualizará.</param>
    /// <param name="selected">Indica si representa la cultura activa.</param>
    private static void StyleSelectedLanguage(Button button, bool selected)
    {
        button.BackColor = selected ? BrandGreen : Color.White;
        button.ForeColor = selected ? Color.White : BrandGreen;
    }

    /// <summary>Consulta el endpoint público de salud y presenta su disponibilidad.</summary>
    /// <returns>Tarea que finaliza luego de actualizar el indicador.</returns>
    private async Task CheckApiAsync()
    {
        _apiStatus.Text = Localization.Text("Login_ApiChecking");
        _apiStatus.BackColor = Color.FromArgb(241, 245, 249);
        _apiStatus.ForeColor = MutedText;

        try
        {
            await _apiClient.GetHealthAsync();
            _apiStatus.Text = "● " + Localization.Text("Login_ApiAvailable");
            _apiStatus.BackColor = SoftGreen;
            _apiStatus.ForeColor = BrandGreen;
        }
        catch (HttpRequestException)
        {
            _apiStatus.Text = "● " + Localization.Text("Login_ApiUnavailableShort");
            _apiStatus.BackColor = Color.FromArgb(254, 226, 226);
            _apiStatus.ForeColor = Color.FromArgb(185, 28, 28);
        }
    }

    /// <summary>Invalida las opciones calculadas cuando cambian las credenciales.</summary>
    /// <param name="sender">Control que originó el cambio.</param>
    /// <param name="eventArgs">Datos del evento.</param>
    private void CredentialsChanged(object? sender, EventArgs eventArgs)
    {
        _companies = [];
        _selectedAccess = null;
    }

    /// <summary>Valida usuario y clave antes de exponer empresas y sucursales autorizadas.</summary>
    /// <returns>Tarea que finaliza al mostrar el segundo paso o un error controlado.</returns>
    private async Task ValidateCredentialsAsync()
    {
        var username = _txtUsuario.Text.Trim().ToUpperInvariant();
        if (username.Length == 0 || _txtClave.TextLength == 0)
        {
            SetStatus(Localization.Text("Login_CredentialsRequired"), true);
            return;
        }

        SetBusy(true);
        SetStatus(Localization.Text("Login_Validating"));

        try
        {
            _txtUsuario.Text = username;
            _companies =
                await _apiClient.GetCompaniesAsync(new CredentialsRequest(username, _txtClave.Text))
                ?? [];
            if (_companies.Count == 0)
            {
                SetStatus(Localization.Text("Login_Invalid"), true);
                return;
            }

            BuildAccessCards();
            _validatedUser.Text = Localization.Format("Login_ValidatedAccess", username);
            ShowCompanyStep();
        }
        catch (HttpRequestException exception)
        {
            ShowHttpError(exception);
        }
        finally
        {
            SetBusy(false);
        }
    }

    /// <summary>Construye una tarjeta seleccionable por cada sucursal activa devuelta por la API.</summary>
    private void BuildAccessCards()
    {
        var remembered = LoadLastAccess();
        _selectedAccess = null;
        _accessList.SuspendLayout();
        foreach (var control in _accessList.Controls.Cast<Control>().ToArray())
            control.Dispose();
        _accessList.Controls.Clear();

        foreach (var company in _companies)
        {
            foreach (var branch in company.Branches ?? [])
            {
                var access = new AccessOption(company, branch);
                var card = new RadioButton
                {
                    Appearance = Appearance.Button,
                    AutoCheck = true,
                    Height = 86,
                    Text = string.Empty,
                    FlatStyle = FlatStyle.Flat,
                    UseVisualStyleBackColor = false,
                    Cursor = Cursors.Hand,
                    Tag = access,
                    Margin = new Padding(0, 5, 0, 5),
                    AccessibleName = $"{company.Name}, {branch.Name}",
                };
                card.FlatAppearance.BorderColor = Border;
                card.Paint += (_, eventArgs) => PaintAccessCard(eventArgs.Graphics, card, access);
                card.CheckedChanged += (_, _) =>
                {
                    StyleAccessCard(card);
                    if (card.Checked)
                        _selectedAccess = (AccessOption)card.Tag!;
                };
                _accessList.Controls.Add(card);

                if (
                    remembered is { } saved
                    && string.Equals(
                        saved.CompanyCode,
                        company.Code,
                        StringComparison.OrdinalIgnoreCase
                    )
                    && saved.BranchId == branch.BranchId
                )
                {
                    card.Checked = true;
                    _rememberAccess.Checked = true;
                }
            }
        }

        if (_accessList.Controls.Count == 0)
        {
            _accessList.Controls.Add(
                new Label
                {
                    Text = Localization.Text("Login_NoBranches"),
                    AutoSize = true,
                    ForeColor = Color.FromArgb(185, 28, 28),
                    Margin = new Padding(0, 16, 0, 0),
                }
            );
        }
        else if (_selectedAccess is null)
        {
            ((RadioButton)_accessList.Controls[0]).Checked = true;
        }

        ResizeAccessCards();
        _accessList.ResumeLayout();
    }

    /// <summary>Ajusta las tarjetas al ancho disponible sin ocultar la barra de desplazamiento.</summary>
    private void ResizeAccessCards()
    {
        var width = Math.Max(
            180,
            _accessList.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 8
        );
        foreach (var card in _accessList.Controls.OfType<RadioButton>())
            card.Width = width;
    }

    /// <summary>Representa visualmente la selección de una sucursal.</summary>
    /// <param name="card">Tarjeta cuyo estado se actualizará.</param>
    private static void StyleAccessCard(RadioButton card)
    {
        card.BackColor = card.Checked ? SoftGreen : Color.White;
        card.ForeColor = card.Checked ? BrandGreen : UiTheme.Text;
        card.FlatAppearance.BorderColor = card.Checked ? BrandGreen : Border;
        card.FlatAppearance.BorderSize = card.Checked ? 2 : 1;
    }

    /// <summary>Crea una sesión asociada a la empresa y sucursal seleccionadas.</summary>
    /// <returns>Tarea que finaliza al abrir el sistema o informar un error.</returns>
    private async Task LoginAsync()
    {
        if (_selectedAccess is null)
        {
            SetStatus(Localization.Text("Login_SelectCompany"), true);
            return;
        }

        SetBusy(true);
        SetStatus(Localization.Text("Login_Validating"));

        try
        {
            var access = _selectedAccess;
            Session = await _apiClient.LoginAsync(
                new LoginRequest(
                    access.Company.Code,
                    _txtUsuario.Text,
                    _txtClave.Text,
                    access.Branch.BranchId
                )
            );
            if (Session is null)
            {
                SetStatus(Localization.Text("Login_CompanyFailed"), true);
                return;
            }

            SelectedCompanyName = access.Company.Name;
            SelectedBranchName = access.Branch.Name;
            SaveLastUser(_rememberUser.Checked ? _txtUsuario.Text : null);
            SaveLastAccess(_rememberAccess.Checked ? access : null);

            if (Session.MustChangePassword && !await ChangeTemporaryPasswordAsync(access.Company))
            {
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (HttpRequestException exception)
        {
            ShowHttpError(exception);
        }
        finally
        {
            SetBusy(false);
        }
    }

    /// <summary>Obliga a reemplazar una contraseña temporal antes de permitir el acceso.</summary>
    /// <param name="company">Empresa sobre la cual se cambiará la credencial.</param>
    /// <returns><see langword="true"/> si puede continuar; actualmente exige volver a ingresar.</returns>
    private async Task<bool> ChangeTemporaryPasswordAsync(CompanyOptionResponse company)
    {
        using var dialog = new ChangePasswordForm();
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return false;

        var changed = await _apiClient.ChangePasswordAsync(
            new ChangePasswordRequest(
                company.Code,
                _txtUsuario.Text,
                _txtClave.Text,
                dialog.NewPassword
            )
        );
        if (!changed)
        {
            SetStatus(Localization.Text("Password_ChangeFailed"), true);
            return false;
        }

        MessageBox.Show(
            this,
            Localization.Text("Password_Changed"),
            "OxiTigre",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information
        );
        _txtClave.Clear();
        Session = null;
        ShowCredentialsStep();
        return false;
    }

    /// <summary>Muestra la etapa de credenciales y restaura el foco correspondiente.</summary>
    /// <param name="clearStatus">Indica si debe limpiar el mensaje vigente.</param>
    private void ShowCredentialsStep(bool clearStatus = true)
    {
        _companyPanel.Visible = false;
        _credentialsPanel.Visible = true;
        _credentialsPanel.BringToFront();
        _contentTitle.Text = Localization.Text("Login_CredentialsTitle");
        _contentHelp.Text = Localization.Text("Login_CredentialsHelp");
        _security.Text = "🔒  " + Localization.Text("Login_Secure");
        _activeStep = 1;
        _stepIndicator.Invalidate();
        AcceptButton = _continueButton;
        if (clearStatus)
            SetStatus(string.Empty);
        (_txtUsuario.TextLength == 0 ? _txtUsuario : _txtClave).Focus();
    }

    /// <summary>Muestra la etapa de selección operativa luego de validar la identidad.</summary>
    private void ShowCompanyStep()
    {
        _credentialsPanel.Visible = false;
        _companyPanel.Visible = true;
        _companyPanel.BringToFront();
        _contentTitle.Text = "←  " + Localization.Text("Login_CompanyTitle");
        _contentHelp.Text = Localization.Text("Login_CompanyStepHelp");
        _security.Text = "🔒  " + Localization.Text("Login_AccessScope");
        _activeStep = 2;
        _stepIndicator.Invalidate();
        AcceptButton = _enterButton;
        SetStatus(Localization.Text("Login_SelectCompanyContinue"));
        _accessList.Controls.OfType<RadioButton>().FirstOrDefault(value => value.Checked)?.Focus();
    }

    /// <summary>Dibuja una tarjeta de sucursal con selección, edificio y estado operativo.</summary>
    /// <param name="graphics">Superficie de dibujo de la tarjeta.</param>
    /// <param name="card">Tarjeta que contiene el estado seleccionado.</param>
    /// <param name="access">Empresa y sucursal representadas.</param>
    private static void PaintAccessCard(Graphics graphics, RadioButton card, AccessOption access)
    {
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        var branchName = access.Branch.Name.StartsWith(
            access.Company.Name,
            StringComparison.OrdinalIgnoreCase
        )
            ? access.Branch.Name[access.Company.Name.Length..].Trim()
            : access.Branch.Name.Trim();
        branchName = System.Text.RegularExpressions.Regex.Replace(
            branchName,
            @"^(Sucursal|Branch)\s+",
            string.Empty,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
        );
        var branchLabel = $"{Localization.Text("Config_Branch")} {branchName}";
        var warehouseLabel = $"{Localization.Text("Inventory_Warehouse")} {branchName}";
        using var outline = new Pen(
            card.Checked ? BrandGreen : Color.FromArgb(148, 163, 184),
            1.5F
        );
        using var selected = new SolidBrush(BrandGreen);
        graphics.DrawEllipse(outline, 14, 34, 18, 18);
        if (card.Checked)
            graphics.FillEllipse(selected, 19, 39, 8, 8);

        using var iconBackground = new SolidBrush(SoftGreen);
        using var iconLine = new Pen(BrandGreen, 1.8F);
        graphics.FillEllipse(iconBackground, 44, 19, 48, 48);
        graphics.DrawRectangle(iconLine, 57, 36, 22, 18);
        graphics.DrawLine(iconLine, 57, 36, 68, 29);
        graphics.DrawLine(iconLine, 68, 29, 79, 36);
        graphics.DrawRectangle(iconLine, 65, 45, 6, 9);
        graphics.DrawLine(iconLine, 60, 41, 64, 41);
        graphics.DrawLine(iconLine, 72, 41, 76, 41);

        using var titleFont = new Font("Segoe UI", 10.5F, FontStyle.Bold);
        using var detailFont = new Font("Segoe UI", 9.5F);
        using var statusFont = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        var statusText = Localization.Text("Login_Operational");
        var statusSize = TextRenderer.MeasureText(statusText, statusFont);
        var statusRectangle = new Rectangle(
            card.ClientSize.Width - statusSize.Width - 24,
            18,
            statusSize.Width + 12,
            25
        );
        using var statusBackground = new SolidBrush(Color.FromArgb(224, 242, 231));
        graphics.FillEllipse(
            statusBackground,
            statusRectangle.X,
            statusRectangle.Y,
            statusRectangle.Height,
            statusRectangle.Height
        );
        graphics.FillEllipse(
            statusBackground,
            statusRectangle.Right - statusRectangle.Height,
            statusRectangle.Y,
            statusRectangle.Height,
            statusRectangle.Height
        );
        graphics.FillRectangle(
            statusBackground,
            statusRectangle.X + statusRectangle.Height / 2,
            statusRectangle.Y,
            statusRectangle.Width - statusRectangle.Height,
            statusRectangle.Height
        );
        TextRenderer.DrawText(
            graphics,
            statusText,
            statusFont,
            statusRectangle,
            BrandGreen,
            TextFormatFlags.HorizontalCenter
                | TextFormatFlags.VerticalCenter
                | TextFormatFlags.NoPadding
        );

        var titleWidth = Math.Max(80, statusRectangle.Left - 108);
        TextRenderer.DrawText(
            graphics,
            $"{access.Company.Name} · {branchLabel}",
            titleFont,
            new Rectangle(104, 17, titleWidth, 27),
            UiTheme.Text,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis
        );
        TextRenderer.DrawText(
            graphics,
            warehouseLabel,
            detailFont,
            new Rectangle(104, 45, card.ClientSize.Width - 120, 25),
            MutedText,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis
        );
    }

    /// <summary>Impide acciones duplicadas durante una operación HTTP.</summary>
    /// <param name="value">Indica si hay una operación en curso.</param>
    private void SetBusy(bool value)
    {
        _continueButton.Enabled = !value;
        _enterButton.Enabled = !value;
        _backButton.Enabled = !value;
        _spanishButton.Enabled = !value;
        _englishButton.Enabled = !value;
        UseWaitCursor = value;
    }

    /// <summary>Presenta un mensaje contextual sin abrir diálogos innecesarios.</summary>
    /// <param name="message">Texto visible.</param>
    /// <param name="isError">Indica si debe usar el color de error.</param>
    private void SetStatus(string message, bool isError = false)
    {
        _status.Text = message;
        _status.ForeColor = isError ? Color.FromArgb(180, 35, 24) : BrandGreen;
    }

    /// <summary>Convierte una falla HTTP en un mensaje comprensible y trazable.</summary>
    /// <param name="exception">Error informado por el cliente de API.</param>
    private void ShowHttpError(HttpRequestException exception)
    {
        SetStatus(
            Localization.Text(
                exception.StatusCode is null ? "Login_ApiUnavailable" : "Login_OperationFailed"
            ),
            true
        );
        if (exception.StatusCode is not null)
        {
            MessageBox.Show(
                this,
                exception.Message,
                "OxiTigre",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
        }
    }

    /// <summary>Lee el último usuario autenticado en esta instalación de Windows.</summary>
    /// <returns>Nombre de usuario guardado o una cadena vacía si no existe.</returns>
    private static string LoadLastUser()
    {
        try
        {
            return File.Exists(LastUserPath) ? File.ReadAllText(LastUserPath).Trim() : string.Empty;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return string.Empty;
        }
    }

    /// <summary>Guarda únicamente el nombre del último usuario autenticado.</summary>
    /// <param name="userName">Usuario validado o <see langword="null"/> para borrar la preferencia.</param>
    private static void SaveLastUser(string? userName)
    {
        SavePreference(LastUserPath, userName?.Trim());
    }

    /// <summary>Recupera la última sucursal elegida sin almacenar credenciales.</summary>
    /// <returns>Empresa y sucursal recordadas o <see langword="null"/>.</returns>
    private static RememberedAccess? LoadLastAccess()
    {
        try
        {
            if (!File.Exists(LastAccessPath))
                return null;
            var values = File.ReadAllText(LastAccessPath).Split('|', 2);
            return values.Length == 2 && long.TryParse(values[1], out var branchId)
                ? new RememberedAccess(values[0], branchId)
                : null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>Guarda o elimina la última selección operativa local.</summary>
    /// <param name="access">Selección confirmada o <see langword="null"/> para borrarla.</param>
    private static void SaveLastAccess(AccessOption? access)
    {
        SavePreference(
            LastAccessPath,
            access is null ? null : $"{access.Company.Code}|{access.Branch.BranchId}"
        );
    }

    /// <summary>Guarda o elimina una preferencia local que no contiene secretos.</summary>
    /// <param name="path">Archivo local de destino.</param>
    /// <param name="value">Valor a conservar o <see langword="null"/> para eliminarlo.</param>
    private static void SavePreference(string path, string? value)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            if (string.IsNullOrWhiteSpace(value))
            {
                if (File.Exists(path))
                    File.Delete(path);
                return;
            }

            File.WriteAllText(path, value);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Una preferencia local nunca debe impedir el acceso al sistema.
        }
    }

    private static string LastUserPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OxiTigre",
            "last-user.txt"
        );

    private static string LastAccessPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OxiTigre",
            "last-access.txt"
        );

    private sealed record AccessOption(CompanyOptionResponse Company, BranchOptionResponse Branch);

    private sealed record RememberedAccess(string CompanyCode, long BranchId);
}
