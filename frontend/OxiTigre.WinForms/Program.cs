/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.WinForms.Program
Archivo: Program.cs | Versión: 13.0.0 | Fecha: 2026-09-03 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Inicia WinForms, autentica al usuario y presenta el shell operativo.
Historial: 1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 12.0.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Acceso a preparación fiscal.
Historial: 13.0.0 | 2026-09-03 | FABRICA | Agustin Omar Cauzi | Shell persistente del Diseño 1.
===============================================================================
*/
using OxiTigre.ApiClient;

namespace OxiTigre.WinForms;

/// <summary>Configura el proceso de escritorio y administra los ciclos de acceso e idioma.</summary>
internal static class Program
{
    /// <summary>Inicia la aplicación y conserva la sesión al reconstruir únicamente el idioma.</summary>
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        var apiUrl =
            Environment.GetEnvironmentVariable("OXITIGRE_API_URL") ?? "http://localhost:5000/";
        var httpClient = new HttpClient { BaseAddress = new Uri(apiUrl, UriKind.Absolute) };
        var apiClient = new OxiTigreApiClient(httpClient);

        while (true)
        {
            Localization.SetCulture(Localization.LoadSavedCulture());
            using var loginForm = new LoginForm(apiClient);
            if (loginForm.ShowDialog() != DialogResult.OK || loginForm.Session is null)
                return;

            try
            {
                var culture = apiClient
                    .GetLanguagePreferenceAsync(loginForm.Session.Token)
                    .GetAwaiter()
                    .GetResult()
                    .CultureCode;
                Localization.SetCulture(culture);
                Localization.SaveCulture(culture);
            }
            catch (HttpRequestException)
            {
                // Conserva el idioma local si la preferencia remota no está disponible.
            }

            var companyName =
                loginForm.SelectedCompanyName ?? Localization.Text("Dashboard_ActiveCompany");
            var workplaceName = string.IsNullOrWhiteSpace(loginForm.SelectedBranchName)
                ? companyName
                : $"{companyName} · {loginForm.SelectedBranchName}";

            while (true)
            {
                using var dashboard = new DashboardForm(
                    apiClient,
                    loginForm.Session,
                    workplaceName
                );
                Application.Run(dashboard);
                if (dashboard.ReloadInterface)
                    continue;
                if (!dashboard.LoginAgain)
                    return;
                break;
            }
        }
    }
}

/// <summary>Informa el cierre remoto y permite finalizar inmediatamente o esperar quince segundos.</summary>
internal sealed class SessionEndedForm : Form
{
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 1_000 };
    private readonly Label _countdown = new() { AutoSize = true };
    private int _secondsRemaining = 15;

    /// <summary>Inicializa el aviso seguro de finalización de sesión.</summary>
    internal SessionEndedForm()
    {
        Text = Localization.Text("Session_Title");
        Width = 500;
        Height = 245;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        UiTheme.Apply(this);

        Controls.Add(
            new Label
            {
                Text = Localization.Text("Session_AdminClosed"),
                Left = 32,
                Top = 28,
                AutoSize = true,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = UiTheme.Primary,
            }
        );
        Controls.Add(
            new Label
            {
                Text = Localization.Text("Session_SaveAndClose"),
                Left = 32,
                Top = 72,
                AutoSize = true,
            }
        );
        _countdown.SetBounds(32, 124, 360, 24);
        Controls.Add(_countdown);

        var close = UiTheme.Button(Localization.Text("Session_CloseNow"), 315, 158, 140);
        close.Click += (_, _) => Close();
        Controls.Add(close);
        AcceptButton = close;

        UpdateCountdown();
        _timer.Tick += (_, _) =>
        {
            _secondsRemaining--;
            UpdateCountdown();
            if (_secondsRemaining <= 0)
                Close();
        };
        Shown += (_, _) => _timer.Start();
        FormClosed += (_, _) => _timer.Stop();
    }

    /// <summary>Actualiza el texto localizado con los segundos restantes.</summary>
    private void UpdateCountdown()
    {
        _countdown.Text = string.Format(
            System.Globalization.CultureInfo.CurrentCulture,
            Localization.Text("Session_Countdown"),
            _secondsRemaining
        );
    }
}
