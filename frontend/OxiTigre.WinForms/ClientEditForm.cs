/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.WinForms.ClientEditForm
Archivo: ClientEditForm.cs | Versión: 2.3.0 | Fecha: 2026-08-26 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Carga clientes con catálogos, validación, teléfonos y borrador recuperable.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 2.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Experiencia física/jurídica, catálogos y precarga.
Historial: 2.1.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Consumo del error controlado de cliente inexistente.
Historial: 2.2.0 | 2026-08-26 | FABRICA | Agustin Omar Cauzi | Máscaras documentales, vista telefónica y ayuda contextual.
Historial: 2.3.0 | 2026-08-26 | FABRICA | Agustin Omar Cauzi | Placeholders de carga y acciones con color semántico.
===============================================================================
*/
using System.ComponentModel;
using OxiTigre.ApiClient;
using OxiTigre.Contracts.Commercial;

namespace OxiTigre.WinForms;

/// <summary>Presenta y valida la edición completa del cliente y sus contactos telefónicos.</summary>
internal sealed class ClientEditForm : Form
{
    private readonly OxiTigreApiClient _apiClient;
    private readonly string _token;
    private readonly long? _clientId;
    private readonly ComboBox _cmbTipoPersona = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
    };
    private readonly ComboBox _cmbTipoDocumento = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
    };
    private readonly ComboBox _cmbTipoTelefono = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
    };
    private readonly ComboBox _cmbPais = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _txtNombre = new() { MaxLength = 200 };
    private readonly TextBox _txtApellido = new() { MaxLength = 150 };
    private readonly MaskedTextBox _txtNumeroDocumento = new()
    {
        MaxLength = 30,
        TextMaskFormat = MaskFormat.IncludeLiterals,
        CutCopyMaskFormat = MaskFormat.IncludeLiterals,
    };
    private readonly TextBox _txtEmail = new() { MaxLength = 254 };
    private readonly TextBox _txtObservacion = new() { Multiline = true, MaxLength = 1000 };
    private readonly TextBox _txtCodigoPais = new() { MaxLength = 5 };
    private readonly TextBox _txtCodigoArea = new() { MaxLength = 10 };
    private readonly TextBox _txtNumeroTelefono = new() { MaxLength = 20 };
    private readonly TextBox _txtInterno = new() { MaxLength = 10 };
    private readonly Label _lblNombre = new() { AutoSize = true };
    private readonly Label _lblApellido = new()
    {
        Text = Localization.Text("ClientEdit_Surname"),
        AutoSize = true,
    };
    private readonly Label _lblVistaTelefono = new() { AutoSize = true, ForeColor = Color.DimGray };
    private readonly CheckBox _chkPrincipal = new()
    {
        Text = Localization.Text("ClientEdit_Primary"),
        AutoSize = true,
    };
    private readonly CheckBox _chkWhatsApp = new()
    {
        Text = Localization.Text("ClientEdit_WhatsApp"),
        AutoSize = true,
    };
    private readonly DataGridView _dgvTelefonos = new()
    {
        ReadOnly = true,
        AutoGenerateColumns = true,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false,
    };
    private readonly BindingList<PhoneRow> _phones = [];
    private readonly Button _btnGuardar;
    private ClientCatalogResponse? _catalogs;

    /// <summary>Inicializa el componente de edición de clientes con sus dependencias y datos de trabajo.</summary>
    /// <param name="apiClient">Cliente usado para comunicarse con la API.</param>
    /// <param name="token">Token de la sesión autenticada.</param>
    /// <param name="clientId">Identificador opcional del cliente que se edita.</param>
    internal ClientEditForm(OxiTigreApiClient apiClient, string token, long? clientId = null)
    {
        _apiClient = apiClient;
        _token = token;
        _clientId = clientId;
        Text = Localization.Text(clientId is null ? "ClientEdit_NewTitle" : "ClientEdit_EditTitle");
        Width = 1010;
        Height = 760;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        UiTheme.Apply(this);

        _txtNombre.PlaceholderText = Localization.Text("ClientEdit_Name");
        _txtApellido.PlaceholderText = Localization.Text("ClientEdit_Surname");
        _txtEmail.PlaceholderText = "nombre@empresa.com";
        _txtObservacion.PlaceholderText = Localization.Text("ClientEdit_Observation");
        _txtCodigoPais.PlaceholderText = "54";
        _txtCodigoArea.PlaceholderText = Localization.Text("ClientEdit_AreaCode");
        _txtNumeroTelefono.PlaceholderText = Localization.Text("ClientEdit_Number");
        _txtInterno.PlaceholderText = Localization.Text("ClientEdit_Extension");

        Controls.Add(
            new Label
            {
                Text = Localization.Text("ClientEdit_Data"),
                Left = 28,
                Top = 22,
                AutoSize = true,
                Font = new Font(Font, FontStyle.Bold),
            }
        );
        var help = UiTheme.HelpButton(Localization.Text("ClientEdit_Help"));
        help.SetBounds(926, 18, 24, 24);
        Controls.Add(help);
        AddField(Localization.Text("ClientEdit_PersonType"), _cmbTipoPersona, 28, 50, 180);
        AddField(_lblNombre, _txtNombre, 230, 50, 300);
        AddField(_lblApellido, _txtApellido, 550, 50, 400);
        AddField(Localization.Text("ClientEdit_DocumentType"), _cmbTipoDocumento, 28, 110, 300);
        AddField(
            Localization.Text("ClientEdit_DocumentNumber"),
            _txtNumeroDocumento,
            350,
            110,
            260
        );
        AddField(Localization.Text("ClientEdit_Email"), _txtEmail, 630, 110, 320);
        AddField(Localization.Text("ClientEdit_Observation"), _txtObservacion, 28, 170, 922, 58);

        Controls.Add(
            new Label
            {
                Text = Localization.Text("ClientEdit_Phones"),
                Left = 28,
                Top = 255,
                AutoSize = true,
                Font = new Font(Font, FontStyle.Bold),
            }
        );
        AddField(Localization.Text("ClientEdit_Type"), _cmbTipoTelefono, 28, 283, 150);
        AddField(Localization.Text("ClientEdit_Country"), _cmbPais, 195, 283, 205);
        AddField(Localization.Text("ClientEdit_CountryCode"), _txtCodigoPais, 417, 283, 80);
        AddField(Localization.Text("ClientEdit_Area"), _txtCodigoArea, 514, 283, 80);
        AddField(Localization.Text("ClientEdit_Number"), _txtNumeroTelefono, 611, 283, 180);
        AddField(Localization.Text("ClientEdit_Extension"), _txtInterno, 808, 283, 80);
        _chkPrincipal.SetBounds(28, 345, 100, 25);
        _chkWhatsApp.SetBounds(140, 345, 150, 25);
        _lblVistaTelefono.SetBounds(660, 350, 290, 22);
        Controls.AddRange([_chkPrincipal, _chkWhatsApp, _lblVistaTelefono]);

        var btnAgregarTelefono = UiTheme.Button(
            Localization.Text("ClientEdit_AddPhone"),
            310,
            340,
            150
        );
        btnAgregarTelefono.Click += btnAgregarTelefono_Click;
        var btnQuitarTelefono = UiTheme.Button(
            Localization.Text("ClientEdit_RemovePhone"),
            472,
            340,
            165
        );
        btnQuitarTelefono.Click += btnQuitarTelefono_Click;
        Controls.AddRange([btnAgregarTelefono, btnQuitarTelefono]);

        _dgvTelefonos.SetBounds(28, 390, 922, 220);
        _dgvTelefonos.DataSource = _phones;
        _dgvTelefonos.CellDoubleClick += dgvTelefonos_CellDoubleClick;
        UiTheme.Grid(_dgvTelefonos);
        Controls.Add(_dgvTelefonos);

        var btnBorrador = UiTheme.Button(Localization.Text("ClientEdit_SaveDraft"), 28, 635, 160);
        btnBorrador.Visible = clientId is null;
        btnBorrador.Click += btnBorrador_Click;
        var btnDescartar = UiTheme.Button(
            Localization.Text("ClientEdit_DiscardDraft"),
            200,
            635,
            170
        );
        btnDescartar.Visible = clientId is null;
        btnDescartar.Click += btnDescartar_Click;
        var btnCancelar = UiTheme.Button(Localization.Text("Common_Cancel"), 598, 635, 160);
        btnCancelar.Click += (_, _) => Close();
        _btnGuardar = UiTheme.Button(Localization.Text("ClientEdit_SaveClient"), 780, 635, 170);
        _btnGuardar.Click += btnGuardar_Click;
        Controls.AddRange([btnBorrador, btnDescartar, btnCancelar, _btnGuardar]);

        _cmbTipoPersona.DataSource = new[]
        {
            new PersonTypeOption("F", Localization.Text("Clients_PersonNatural")),
            new PersonTypeOption("J", Localization.Text("Clients_PersonLegal")),
        };
        _cmbTipoPersona.DisplayMember = nameof(PersonTypeOption.Name);
        _cmbTipoPersona.ValueMember = nameof(PersonTypeOption.Code);
        _cmbTipoPersona.SelectedValueChanged += (_, _) => UpdatePersonType();
        _cmbTipoDocumento.SelectedValueChanged += (_, _) => UpdateDocumentMask();
        _cmbPais.SelectedValueChanged += (_, _) => UpdateCountry();
        UiTheme.DigitsOnly(_txtCodigoArea, _txtNumeroTelefono, _txtInterno);
        _txtCodigoPais.TextChanged += (_, _) => UpdatePhonePreview();
        _txtCodigoArea.TextChanged += (_, _) => UpdatePhonePreview();
        _txtNumeroTelefono.TextChanged += (_, _) => UpdatePhonePreview();
        _txtInterno.TextChanged += (_, _) => UpdatePhonePreview();
        Shown += ClientEditForm_Shown;
    }

    private string PersonType => _cmbTipoPersona.SelectedValue?.ToString() ?? "F";

    /// <summary>Carga la información inicial cuando se muestra la pantalla de edición de clientes.</summary>
    /// <param name="sender">Control que originó el evento.</param>
    /// <param name="e">Datos asociados al evento de la interfaz.</param>
    private async void ClientEditForm_Shown(object? sender, EventArgs e)
    {
        try
        {
            _catalogs = await _apiClient.GetClientCatalogsAsync(_token);
            _cmbTipoTelefono.DataSource = _catalogs.PhoneTypes.ToList();
            _cmbTipoTelefono.DisplayMember = nameof(PhoneTypeOptionResponse.Name);
            _cmbTipoTelefono.ValueMember = nameof(PhoneTypeOptionResponse.Code);
            _cmbPais.DataSource = _catalogs.Countries.ToList();
            _cmbPais.DisplayMember = nameof(CountryOptionResponse.Name);
            _cmbPais.ValueMember = nameof(CountryOptionResponse.Code);
            _cmbPais.SelectedItem = _catalogs.Countries.FirstOrDefault(country =>
                country.IsDefault
            );
            UpdatePersonType();

            if (_clientId is { } clientId)
            {
                var client = await _apiClient.GetClientAsync(_token, clientId);
                ApplyRequest(
                    new SaveClientRequest(
                        client.PersonType,
                        client.NameOrBusinessName,
                        client.Surname,
                        client.DocumentType,
                        client.DocumentNumber,
                        client.Email,
                        client.Observation,
                        client
                            .Phones.Select(phone => new ClientPhoneRequest(
                                phone.TypeCode,
                                phone.CountryCode,
                                phone.AreaCode,
                                phone.Number,
                                phone.Extension,
                                phone.IsPrimary,
                                phone.AllowsWhatsApp,
                                phone.Observation
                            ))
                            .ToList()
                    )
                );
                return;
            }

            var saved = await _apiClient.GetClientDraftAsync(_token);
            if (
                saved is not null
                && MessageBox.Show(
                    this,
                    Localization.Format(
                        "ClientEdit_DraftQuestion",
                        saved.SavedAtUtc.ToLocalTime().ToString("g")
                    ),
                    "OxiTigre",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                ) == DialogResult.Yes
            )
                ApplyRequest(saved.Draft);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidDataException)
        {
            ShowError(exception.Message);
            Close();
        }
    }

    /// <summary>Aplica al formulario los datos recuperados de un cliente o de su borrador.</summary>
    /// <param name="request">Datos validados que se aplican o envían para completar la operación.</param>
    private void ApplyRequest(SaveClientRequest request)
    {
        _cmbTipoPersona.SelectedValue = request.PersonType;
        UpdatePersonType();
        _txtNombre.Text = request.NameOrBusinessName;
        _txtApellido.Text = request.Surname;
        if (request.DocumentType is null)
            _cmbTipoDocumento.SelectedIndex = -1;
        else
            _cmbTipoDocumento.SelectedValue = request.DocumentType;
        _txtNumeroDocumento.Text = request.DocumentNumber;
        _txtEmail.Text = request.Email;
        _txtObservacion.Text = request.Observation;
        _phones.Clear();
        foreach (var phone in request.Phones ?? [])
            _phones.Add(PhoneRow.From(phone, _catalogs!));
        ConfigurePhoneColumns();
    }

    /// <summary>Procesa la acción de agregar un teléfono solicitada desde la interfaz.</summary>
    /// <param name="sender">Control que originó el evento.</param>
    /// <param name="e">Datos asociados al evento de la interfaz.</param>
    private void btnAgregarTelefono_Click(object? sender, EventArgs e)
    {
        var countryCode = _txtCodigoPais.Text.Trim().TrimStart('+');
        if (
            _cmbTipoTelefono.SelectedItem is not PhoneTypeOptionResponse phoneType
            || _cmbPais.SelectedItem is not CountryOptionResponse country
            || !IsDigits(countryCode, 1, 5)
            || !IsDigits(_txtNumeroTelefono.Text, 6, 20)
            || (
                !string.IsNullOrWhiteSpace(_txtCodigoArea.Text)
                && !IsDigits(_txtCodigoArea.Text, 1, 10)
            )
            || (!string.IsNullOrWhiteSpace(_txtInterno.Text) && !IsDigits(_txtInterno.Text, 1, 10))
        )
        {
            ShowWarning(Localization.Text("ClientEdit_InvalidPhone"));
            return;
        }
        if (country.Code == "AR" && string.IsNullOrWhiteSpace(_txtCodigoArea.Text))
        {
            ShowWarning(Localization.Text("ClientEdit_AreaRequired"));
            return;
        }

        if (_chkPrincipal.Checked)
            foreach (var phone in _phones)
                phone.IsPrimary = false;
        _phones.Add(
            new PhoneRow(
                phoneType.Code,
                phoneType.Name,
                country.Name,
                countryCode,
                NullIfWhiteSpace(_txtCodigoArea.Text),
                _txtNumeroTelefono.Text.Trim(),
                NullIfWhiteSpace(_txtInterno.Text),
                _chkPrincipal.Checked,
                _chkWhatsApp.Checked,
                null
            )
        );
        _dgvTelefonos.Refresh();
        ConfigurePhoneColumns();
        _txtNumeroTelefono.Clear();
        _txtInterno.Clear();
        _chkPrincipal.Checked = false;
    }

    /// <summary>Procesa la acción de quitar un teléfono solicitada desde la interfaz.</summary>
    /// <param name="sender">Control que originó el evento.</param>
    /// <param name="e">Datos asociados al evento de la interfaz.</param>
    private void btnQuitarTelefono_Click(object? sender, EventArgs e)
    {
        if (_dgvTelefonos.CurrentRow?.DataBoundItem is PhoneRow phone)
            _phones.Remove(phone);
    }

    /// <summary>Abre la edición del registro seleccionado al hacer doble clic en la grilla.</summary>
    /// <param name="sender">Control que originó el evento.</param>
    /// <param name="e">Datos asociados al evento de la interfaz.</param>
    private void dgvTelefonos_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || _dgvTelefonos.Rows[e.RowIndex].DataBoundItem is not PhoneRow phone)
            return;
        _cmbTipoTelefono.SelectedValue = phone.TypeCode;
        _cmbPais.SelectedItem =
            _catalogs!.Countries.FirstOrDefault(country => country.PhoneCode == phone.CountryCode)
            ?? _catalogs.Countries.FirstOrDefault(country => country.Code == "OTRO");
        _txtCodigoPais.Text = $"+{phone.CountryCode}";
        _txtCodigoArea.Text = phone.AreaCode;
        _txtNumeroTelefono.Text = phone.Number;
        _txtInterno.Text = phone.Extension;
        _chkPrincipal.Checked = phone.IsPrimary;
        _chkWhatsApp.Checked = phone.AllowsWhatsApp;
        _phones.Remove(phone);
    }

    /// <summary>Procesa la acción de guardar solicitada desde la interfaz.</summary>
    /// <param name="sender">Control que originó el evento.</param>
    /// <param name="e">Datos asociados al evento de la interfaz.</param>
    private async void btnGuardar_Click(object? sender, EventArgs e)
    {
        var request = BuildRequest(requireComplete: true);
        if (request is null)
            return;
        _btnGuardar.Enabled = false;
        try
        {
            if (_clientId is null)
                await _apiClient.CreateClientAsync(_token, request);
            else
                await _apiClient.UpdateClientAsync(_token, _clientId.Value, request);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (HttpRequestException exception)
        {
            ShowError(exception.Message);
        }
        finally
        {
            _btnGuardar.Enabled = true;
        }
    }

    /// <summary>Procesa la acción de guardar el borrador solicitada desde la interfaz.</summary>
    /// <param name="sender">Control que originó el evento.</param>
    /// <param name="e">Datos asociados al evento de la interfaz.</param>
    private async void btnBorrador_Click(object? sender, EventArgs e)
    {
        var request = BuildRequest(requireComplete: false);
        if (request is null)
            return;
        try
        {
            await _apiClient.SaveClientDraftAsync(_token, request);
            MessageBox.Show(
                this,
                Localization.Text("ClientEdit_DraftSaved"),
                "OxiTigre",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }
        catch (HttpRequestException exception)
        {
            ShowError(exception.Message);
        }
    }

    /// <summary>Procesa la acción de descartar los cambios solicitada desde la interfaz.</summary>
    /// <param name="sender">Control que originó el evento.</param>
    /// <param name="e">Datos asociados al evento de la interfaz.</param>
    private async void btnDescartar_Click(object? sender, EventArgs e)
    {
        if (
            MessageBox.Show(
                this,
                Localization.Text("ClientEdit_DiscardQuestion"),
                "OxiTigre",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            ) != DialogResult.Yes
        )
            return;
        try
        {
            await _apiClient.DeleteClientDraftAsync(_token);
            ClearForm();
            MessageBox.Show(
                this,
                Localization.Text("ClientEdit_DraftDiscarded"),
                "OxiTigre",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }
        catch (HttpRequestException exception)
        {
            ShowError(exception.Message);
        }
    }

    /// <summary>Construye la solicitud de guardado y valida los datos obligatorios del cliente.</summary>
    /// <param name="requireComplete">Indica si deben exigirse todos los datos obligatorios del cliente.</param>
    /// <returns>Objeto construido u obtenido por la operación.</returns>
    private SaveClientRequest? BuildRequest(bool requireComplete)
    {
        if (requireComplete)
        {
            if (
                string.IsNullOrWhiteSpace(_txtNombre.Text)
                || (PersonType == "F" && string.IsNullOrWhiteSpace(_txtApellido.Text))
            )
            {
                ShowWarning(Localization.Text("ClientEdit_NameRequired"));
                return null;
            }
            if (
                _cmbTipoDocumento.SelectedItem is null
                || !_txtNumeroDocumento.Text.Any(char.IsLetterOrDigit)
                || _cmbTipoDocumento.SelectedValue?.ToString() is "CUIT" or "CUIL"
                    && !_txtNumeroDocumento.MaskCompleted
            )
            {
                ShowWarning(Localization.Text("ClientEdit_DocumentRequired"));
                return null;
            }
            if (
                !string.IsNullOrWhiteSpace(_txtEmail.Text)
                && !System.Net.Mail.MailAddress.TryCreate(_txtEmail.Text, out _)
            )
            {
                ShowWarning(Localization.Text("ClientEdit_InvalidEmail"));
                return null;
            }
            if (_phones.Count == 0 || _phones.Count(phone => phone.IsPrimary) != 1)
            {
                ShowWarning(Localization.Text("ClientEdit_PrimaryPhoneRequired"));
                return null;
            }
        }

        return new SaveClientRequest(
            PersonType,
            _txtNombre.Text,
            PersonType == "F" ? _txtApellido.Text : null,
            _cmbTipoDocumento.SelectedValue?.ToString(),
            _txtNumeroDocumento.Text,
            _txtEmail.Text,
            _txtObservacion.Text,
            _phones.Select(phone => phone.ToRequest()).ToList()
        );
    }

    /// <summary>Actualiza el tipo de persona según los datos ingresados.</summary>
    private void UpdatePersonType()
    {
        var isNaturalPerson = PersonType == "F";
        _lblNombre.Text = Localization.Text(
            isNaturalPerson ? "ClientEdit_Name" : "ClientEdit_BusinessName"
        );
        _lblApellido.Visible = isNaturalPerson;
        _txtApellido.Visible = isNaturalPerson;
        if (!isNaturalPerson)
            _txtApellido.Clear();
        if (_catalogs is null)
            return;
        var previous = _cmbTipoDocumento.SelectedValue?.ToString();
        var options = _catalogs
            .DocumentTypes.Where(option =>
                isNaturalPerson ? option.AppliesToNaturalPerson : option.AppliesToLegalPerson
            )
            .ToList();
        _cmbTipoDocumento.DataSource = options;
        _cmbTipoDocumento.DisplayMember = nameof(DocumentTypeOptionResponse.Name);
        _cmbTipoDocumento.ValueMember = nameof(DocumentTypeOptionResponse.Code);
        if (previous is not null && options.Any(option => option.Code == previous))
            _cmbTipoDocumento.SelectedValue = previous;
        UpdateDocumentMask();
    }

    /// <summary>Actualiza la máscara del documento según los datos ingresados.</summary>
    private void UpdateDocumentMask()
    {
        var value = new string(_txtNumeroDocumento.Text.Where(char.IsLetterOrDigit).ToArray());
        _txtNumeroDocumento.Mask = _cmbTipoDocumento.SelectedValue?.ToString() switch
        {
            "CUIT" or "CUIL" => "00-00000000-0",
            "DNI" => "99999999",
            _ => string.Empty,
        };
        _txtNumeroDocumento.Text = value;
    }

    /// <summary>Actualiza el país seleccionado según los datos ingresados.</summary>
    private void UpdateCountry()
    {
        if (_cmbPais.SelectedItem is not CountryOptionResponse country)
            return;
        _txtCodigoPais.Text = country.PhoneCode is null ? string.Empty : $"+{country.PhoneCode}";
        _txtCodigoPais.ReadOnly = country.PhoneCode is not null;
        _txtCodigoPais.BackColor = country.PhoneCode is null
            ? Color.White
            : Color.FromArgb(235, 238, 235);
    }

    /// <summary>Actualiza la vista previa del teléfono según los datos ingresados.</summary>
    private void UpdatePhonePreview()
    {
        var country = _txtCodigoPais.Text.Trim();
        var area = _txtCodigoArea.Text.Trim();
        var number = _txtNumeroTelefono.Text.Trim();
        if (number.Length > 4)
            number = $"{number[..^4]}-{number[^4..]}";
        _lblVistaTelefono.Text = string.IsNullOrWhiteSpace(number)
            ? string.Empty
            : $"{country}{(area.Length == 0 ? string.Empty : $" ({area})")} {number}{(string.IsNullOrWhiteSpace(_txtInterno.Text) ? string.Empty : $" int. {_txtInterno.Text}")}";
    }

    /// <summary>Configura las columnas de teléfonos para su presentación y uso.</summary>
    private void ConfigurePhoneColumns()
    {
        Hide(nameof(PhoneRow.TypeCode));
        Hide(nameof(PhoneRow.CountryCode));
        Hide(nameof(PhoneRow.AreaCode));
        Hide(nameof(PhoneRow.Number));
        Hide(nameof(PhoneRow.Extension));
        Rename(nameof(PhoneRow.TypeName), Localization.Text("ClientEdit_Type"));
        Rename(nameof(PhoneRow.CountryName), Localization.Text("ClientEdit_Country"));
        Rename(nameof(PhoneRow.FormattedNumber), Localization.Text("ClientEdit_Number"));
        Rename(nameof(PhoneRow.IsPrimary), Localization.Text("ClientEdit_Primary"));
        Rename(nameof(PhoneRow.AllowsWhatsApp), "WhatsApp");
        Rename(nameof(PhoneRow.Observation), Localization.Text("ClientEdit_Observation"));
    }

    /// <summary>Limpia el formulario y restablece sus valores iniciales.</summary>
    private void ClearForm()
    {
        _cmbTipoPersona.SelectedValue = "F";
        _txtNombre.Clear();
        _txtApellido.Clear();
        _txtNumeroDocumento.Clear();
        _txtEmail.Clear();
        _txtObservacion.Clear();
        _phones.Clear();
        _cmbPais.SelectedItem = _catalogs?.Countries.FirstOrDefault(country => country.IsDefault);
        _txtCodigoArea.Clear();
        _txtNumeroTelefono.Clear();
        _txtInterno.Clear();
        _chkPrincipal.Checked = false;
        _chkWhatsApp.Checked = false;
    }

    /// <summary>Agrega un campo etiquetado a la distribución visual indicada.</summary>
    /// <param name="caption">Texto visible que identifica el campo.</param>
    /// <param name="control">Control visual asociado al campo o sección.</param>
    /// <param name="left">Posición horizontal del control.</param>
    /// <param name="top">Posición vertical del control.</param>
    /// <param name="width">Ancho asignado al control.</param>
    /// <param name="height">Alto asignado al control.</param>
    private void AddField(
        string caption,
        Control control,
        int left,
        int top,
        int width,
        int height = 27
    )
    {
        var label = new Label
        {
            Text = caption,
            Left = left,
            Top = top,
            AutoSize = true,
        };
        AddField(label, control, left, top, width, height);
    }

    /// <summary>Agrega un campo etiquetado a la distribución visual indicada.</summary>
    /// <param name="label">Etiqueta visible del campo.</param>
    /// <param name="control">Control visual asociado al campo o sección.</param>
    /// <param name="left">Posición horizontal del control.</param>
    /// <param name="top">Posición vertical del control.</param>
    /// <param name="width">Ancho asignado al control.</param>
    /// <param name="height">Alto asignado al control.</param>
    private void AddField(
        Label label,
        Control control,
        int left,
        int top,
        int width,
        int height = 27
    )
    {
        label.SetBounds(left, top, width, 20);
        control.SetBounds(left, top + 20, width, height);
        Controls.AddRange([label, control]);
    }

    /// <summary>Asigna el título visible correspondiente a la operación de edición de clientes.</summary>
    /// <param name="property">Nombre de la propiedad o columna que se modifica.</param>
    /// <param name="title">Título visible de la ventana, pestaña o sección.</param>
    private void Rename(string property, string title)
    {
        if (_dgvTelefonos.Columns[property] is { } column)
            column.HeaderText = title;
    }

    /// <summary>Oculta la operación de edición de clientes para no exponer información técnica.</summary>
    /// <param name="property">Nombre de la propiedad o columna que se modifica.</param>
    private void Hide(string property)
    {
        if (_dgvTelefonos.Columns[property] is { } column)
            column.Visible = false;
    }

    /// <summary>Comprueba que un texto contenga solo dígitos y respete la longitud admitida.</summary>
    /// <param name="value">Texto que se normaliza, valida o asigna.</param>
    /// <param name="minimum">Longitud mínima admitida.</param>
    /// <param name="maximum">Longitud máxima admitida.</param>
    /// <returns>Verdadero cuando se cumple la condición evaluada; en caso contrario, falso.</returns>
    private static bool IsDigits(string? value, int minimum, int maximum) =>
        value is { Length: var length }
        && length >= minimum
        && length <= maximum
        && value.All(char.IsDigit);

    /// <summary>Convierte texto vacío o compuesto por espacios en nulo.</summary>
    /// <param name="value">Texto que se normaliza, valida o asigna.</param>
    /// <returns>Texto normalizado, localizado o formateado por la operación.</returns>
    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Muestra una advertencia de validación al usuario.</summary>
    /// <param name="message">Mensaje funcional que se muestra al usuario.</param>
    private void ShowWarning(string message) =>
        MessageBox.Show(this, message, "OxiTigre", MessageBoxButtons.OK, MessageBoxIcon.Warning);

    /// <summary>Muestra un error funcional sin exponer detalles técnicos.</summary>
    /// <param name="message">Mensaje funcional que se muestra al usuario.</param>
    private void ShowError(string message) =>
        MessageBox.Show(this, message, "OxiTigre", MessageBoxButtons.OK, MessageBoxIcon.Error);

    private sealed record PersonTypeOption(string Code, string Name);

    private sealed class PhoneRow(
        string typeCode,
        string typeName,
        string countryName,
        string countryCode,
        string? areaCode,
        string number,
        string? extension,
        bool isPrimary,
        bool allowsWhatsApp,
        string? observation
    )
    {
        public string TypeCode { get; } = typeCode;
        public string TypeName { get; } = typeName;
        public string CountryName { get; } = countryName;
        public string CountryCode { get; } = countryCode;
        public string? AreaCode { get; } = areaCode;
        public string Number { get; } = number;
        public string? Extension { get; } = extension;
        public string FormattedNumber =>
            $"+{CountryCode}{(string.IsNullOrWhiteSpace(AreaCode) ? string.Empty : $" ({AreaCode})")} {FormatNumber(Number)}{(string.IsNullOrWhiteSpace(Extension) ? string.Empty : $" int. {Extension}")}";
        public bool IsPrimary { get; set; } = isPrimary;
        public bool AllowsWhatsApp { get; } = allowsWhatsApp;
        public string? Observation { get; } = observation;

        /// <summary>Convierte el teléfono visible en un contrato apto para guardarse.</summary>
        /// <returns>Objeto construido u obtenido por la operación.</returns>
        internal ClientPhoneRequest ToRequest() =>
            new(
                TypeCode,
                CountryCode,
                AreaCode,
                Number,
                Extension,
                IsPrimary,
                AllowsWhatsApp,
                Observation
            );

        /// <summary>Crea un renglón visible a partir de un teléfono y sus catálogos.</summary>
        /// <param name="phone">Teléfono que se convierte al contrato de guardado.</param>
        /// <param name="catalogs">Catálogos usados para resolver nombres visibles del teléfono.</param>
        /// <returns>Objeto construido u obtenido por la operación.</returns>
        internal static PhoneRow From(ClientPhoneRequest phone, ClientCatalogResponse catalogs)
        {
            var phoneType = catalogs.PhoneTypes.FirstOrDefault(option =>
                option.Code == phone.TypeCode
            );
            var country =
                catalogs.Countries.FirstOrDefault(option => option.PhoneCode == phone.CountryCode)
                ?? catalogs.Countries.First(option => option.Code == "OTRO");
            return new PhoneRow(
                phone.TypeCode,
                phoneType?.Name ?? phone.TypeCode,
                country.Name,
                phone.CountryCode ?? string.Empty,
                phone.AreaCode,
                phone.Number,
                phone.Extension,
                phone.IsPrimary,
                phone.AllowsWhatsApp,
                phone.Observation
            );
        }

        /// <summary>Formatea number para su presentación al usuario.</summary>
        /// <param name="value">Texto que se normaliza, valida o asigna.</param>
        /// <returns>Texto normalizado, localizado o formateado por la operación.</returns>
        private static string FormatNumber(string value) =>
            value.Length > 4 ? $"{value[..^4]}-{value[^4..]}" : value;
    }
}
