/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.WinForms.PurchasingManagementForm
Archivo: PurchasingManagementForm.cs | Versión: 1.4.0 | Fecha: 2026-08-27 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Opera proveedores, órdenes, aprobaciones y recepciones parciales de Compras.
Historial: 1.0.0 | 2026-08-24 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-26 | FABRICA | Agustin Omar Cauzi | Máscaras, filtros, baja lógica y reversión auditable.
Historial: 1.2.0 | 2026-08-26 | FABRICA | Agustin Omar Cauzi | Detalle integral de la orden y snapshot del proveedor.
Historial: 1.3.0 | 2026-08-26 | FABRICA | Agustin Omar Cauzi | Títulos operativos, filtros guiados y mayor espacio para detalles.
Historial: 1.4.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Editor de órdenes guiado, filtros por etapa y mensajes de edición controlados.
===============================================================================
*/
using System.Globalization;
using OxiTigre.ApiClient;
using OxiTigre.Contracts.Purchasing;
using OxiTigre.Contracts.Security;

namespace OxiTigre.WinForms;

/// <summary>Presenta el circuito operativo de Compras respetando permisos y concurrencia.</summary>
internal sealed class PurchasingManagementForm : Form
{
    private readonly OxiTigreApiClient _api;
    private readonly LoginResponse _session;
    private readonly bool _canManage,
        _canApprove,
        _canReceive;
    private readonly DataGridView _suppliers = Grid(),
        _orders = Grid(),
        _lines = Grid(),
        _receipts = Grid();
    private readonly CheckBox _showDrafts = new() { AutoSize = true, Checked = true };
    private readonly CheckBox _showInProgress = new() { AutoSize = true, Checked = true };
    private readonly CheckBox _showFinalized = new() { AutoSize = true, Checked = true };
    private PurchasingSnapshotResponse? _data;

    /// <summary>Inicializa el componente de compras con sus dependencias y datos de trabajo.</summary>
    /// <param name="api">Cliente usado para comunicarse con la API.</param>
    /// <param name="session">Sesión autenticada que determina permisos y contexto operativo.</param>
    internal PurchasingManagementForm(OxiTigreApiClient api, LoginResponse session)
    {
        _api = api;
        _session = session;
        _canManage = Has("COMPRAS.GESTIONAR");
        _canApprove = Has("COMPRAS.APROBAR");
        _canReceive = Has("COMPRAS.RECIBIR");
        Text = Localization.Text("Purchases_Title");
        Width = 1260;
        Height = 790;
        MinimumSize = new Size(1000, 650);
        StartPosition = FormStartPosition.CenterParent;
        UiTheme.Apply(this);
        var tabs = new TabControl { Dock = DockStyle.Fill };
        UiTheme.Tabs(tabs);
        tabs.TabPages.Add(Page("Purchases_Suppliers", SuppliersPage()));
        tabs.TabPages.Add(Page("Purchases_Orders", OrdersPage()));
        tabs.TabPages.Add(Page("Purchases_Receipts", ReceiptsPage()));
        Controls.Add(tabs);
        Controls.Add(Info("Purchases_Overview"));
        _orders.SelectionChanged += (_, _) => BindLines();
        Shown += async (_, _) => await LoadAsync();
    }

    /// <summary>Construye la vista de proveedores y sus acciones permitidas.</summary>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private Control SuppliersPage()
    {
        var panel = new Panel { Dock = DockStyle.Fill };
        panel.Controls.Add(
            UiTheme.Searchable(
                _suppliers,
                Localization.Text("Purchases_SupplierSearchHelp"),
                Localization.Text("Purchases_Suppliers")
            )
        );
        panel.Controls.Add(
            Toolbar(
                ("Purchases_NewSupplier", _canManage, () => SaveSupplier(null)),
                ("Purchases_EditSupplier", _canManage, EditSupplier),
                ("Purchases_ChangeSupplierStatus", _canManage, ChangeSupplierStatus)
            )
        );
        return panel;
    }

    /// <summary>Construye la vista de órdenes de compra y sus renglones.</summary>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private Control OrdersPage()
    {
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 330,
        };
        var filters = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 38,
            Padding = new Padding(10, 7, 8, 3),
            WrapContents = false,
        };
        _showDrafts.Text = Localization.Text("Purchases_FilterDrafts");
        _showInProgress.Text = Localization.Text("Purchases_FilterInProgress");
        _showFinalized.Text = Localization.Text("Purchases_FilterFinalized");
        _showDrafts.CheckedChanged += (_, _) => BindOrders();
        _showInProgress.CheckedChanged += (_, _) => BindOrders();
        _showFinalized.CheckedChanged += (_, _) => BindOrders();
        filters.Controls.AddRange([_showDrafts, _showInProgress, _showFinalized]);

        split.Panel1.Controls.Add(
            UiTheme.Searchable(
                _orders,
                Localization.Text("Purchases_OrderSearchHelp"),
                Localization.Text("Purchases_Orders")
            )
        );
        split.Panel1.Controls.Add(filters);
        split.Panel1.Controls.Add(
            Toolbar(
                ("Purchases_NewOrder", _canManage, () => SaveOrder(null)),
                ("Purchases_EditOrder", _canManage, EditOrder),
                ("Purchases_ViewOrder", true, ViewOrder),
                ("Purchases_Submit", _canManage, Submit),
                ("Purchases_Approve", _canApprove, Approve),
                ("Purchases_Cancel", _canManage, Cancel),
                ("Purchases_Receive", _canReceive, Receive),
                ("Purchases_Close", _canReceive, CloseBalance)
            )
        );
        split.Panel2.Controls.Add(
            UiTheme.Searchable(
                _lines,
                Localization.Text("Purchases_OrderDetailHelp"),
                Localization.Text("Purchases_OrderLines")
            )
        );
        return split;
    }

    /// <summary>Construye la vista de recepciones y diferencias registradas.</summary>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private Control ReceiptsPage()
    {
        var panel = new Panel { Dock = DockStyle.Fill };
        panel.Controls.Add(
            UiTheme.Searchable(
                _receipts,
                Localization.Text("Purchases_ReceiptSearchHelp"),
                Localization.Text("Purchases_Receipts")
            )
        );
        panel.Controls.Add(Toolbar(("Purchases_ReverseReceipt", _canReceive, ReverseReceipt)));
        return panel;
    }

    /// <summary>Recupera el estado de Compras desde la API y refresca sus grillas; informa fallos de red al operador.</summary>
    /// <returns>Tarea que termina después de actualizar la vista o mostrar el error de conexión.</returns>
    private async Task LoadAsync()
    {
        try
        {
            _data = await _api.GetPurchasingAsync(_session.Token);
            Bind();
        }
        catch (HttpRequestException ex)
        {
            Error(ex.Message);
        }
    }

    /// <summary>Distribuye el catálogo recibido entre proveedores, órdenes, recepciones y renglones seleccionados.</summary>
    private void Bind()
    {
        if (_data is null)
            return;
        Bind(_suppliers, _data.Suppliers, "SupplierId", "RowVersion", "Observation");
        BindOrders();
        Bind(
            _receipts,
            _data.Receipts,
            "GoodsReceiptId",
            "PurchaseOrderId",
            "MovementId",
            "ReversalMovementId",
            "ReversedByUserId",
            "RowVersion",
            "GoodsReceiptLineId",
            "PurchaseOrderLineId",
            "ProductId",
            "Observation"
        );
        BindLines();
    }

    /// <summary>Aplica los filtros operativos de estado a la lista de órdenes.</summary>
    private void BindOrders()
    {
        if (_data is null)
            return;
        var headers = _data
            .Orders.GroupBy(line => line.PurchaseOrderId)
            .Select(group =>
            {
                var order = group.First();
                return new OrderView(
                    order.PurchaseOrderId,
                    order.OrderCode,
                    order.OrderDateUtc.ToLocalTime(),
                    order.ExpectedDeliveryDate,
                    order.Supplier,
                    order.Warehouse,
                    order.Total,
                    order.StatusCode,
                    order.RowVersion
                );
            })
            .Where(order => IsVisible(order.Status))
            .ToList();
        Bind(_orders, headers, "Id", "RowVersion");
    }

    /// <summary>Determina si una orden aparece según los filtros de etapa seleccionados.</summary>
    /// <param name="status">Estado vigente de la orden.</param>
    /// <returns><see langword="true"/> cuando el filtro de su etapa está activo.</returns>
    private bool IsVisible(string status) =>
        status switch
        {
            "BORRADOR" => _showDrafts.Checked,
            "PENDIENTE_APROBACION" or "APROBADA" or "RECIBIDA_PARCIAL" => _showInProgress.Checked,
            _ => _showFinalized.Checked,
        };

    /// <summary>Muestra únicamente los renglones de la orden seleccionada y sus cantidades pendientes.</summary>
    private void BindLines()
    {
        var selected = Selected<OrderView>(_orders);
        Bind(
            _lines,
            selected is null || _data is null
                ? []
                : _data
                    .Orders.Where(x => x.PurchaseOrderId == selected.Id)
                    .Select(x => new LineView(
                        x.PurchaseOrderLineId,
                        x.ProductCode,
                        x.Product,
                        x.OrderedQuantity,
                        x.ReceivedQuantity,
                        x.PendingQuantity,
                        x.UnitCost,
                        x.LineTotal,
                        x.UnitSymbol
                    ))
                    .ToList(),
            "Id"
        );
    }

    /// <summary>Abre la edición del proveedor seleccionado, o avisa si no hay selección.</summary>
    /// <returns>Tarea que termina cuando el diálogo y, si corresponde, la actualización de la API finalizan.</returns>
    private async Task EditSupplier()
    {
        var item = Selected<SupplierResponse>(_suppliers);
        if (item is null)
        {
            Error(Localization.Text("Purchases_NoSelection"));
            return;
        }
        await SaveSupplier(item);
    }

    /// <summary>Solicita los datos del proveedor y conserva los contactos secundarios activos al guardar.</summary>
    /// <param name="item">Proveedor existente; <see langword="null"/> para dar de alta uno nuevo.</param>
    /// <returns>Tarea que termina tras cancelar el diálogo o persistir y recargar la vista.</returns>
    private async Task SaveSupplier(SupplierResponse? item)
    {
        if (_data is null)
            return;
        var contact = item is null
            ? null
            : _data.SupplierContacts.FirstOrDefault(x =>
                x.SupplierId == item.SupplierId && x.IsPrimary
            );
        using var dialog = new SimpleEntryForm(
            item is null
                ? Localization.Text("Purchases_NewSupplier")
                : Localization.Text("Purchases_EditSupplier"),
            new("Legal", Localization.Text("Purchases_LegalName"), item?.LegalName, Required: true),
            new("Trade", Localization.Text("Purchases_TradeName"), item?.TradeName),
            new(
                "Tax",
                Localization.Text("Purchases_TaxId"),
                item?.TaxId,
                Required: true,
                Mask: "00-00000000-0",
                Help: Localization.Text("Purchases_TaxIdHelp")
            ),
            new("Email", Localization.Text("Column_Email"), item?.Email),
            new(
                "Phone",
                Localization.Text("Column_Phone"),
                item?.Phone,
                Help: Localization.Text("Purchases_PhoneHelp")
            ),
            new(
                "Payment",
                Localization.Text("Purchases_PaymentTerms"),
                item?.PaymentTerms,
                Help: Localization.Text("Purchases_PaymentTermsHelp")
            ),
            new("Contact", Localization.Text("Purchases_Contact"), contact?.Name),
            new(
                "Observation",
                Localization.Text("Config_Description"),
                item?.Observation,
                Multiline: true
            )
        );
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;
        var contacts = (
            item is null
                ? []
                : _data
                    .SupplierContacts.Where(x =>
                        x.SupplierId == item.SupplierId && x.StatusCode == "ACTIVO" && !x.IsPrimary
                    )
                    .Select(x => new SupplierContactRequest(
                        x.Name,
                        x.Position,
                        x.Email,
                        x.Phone,
                        false
                    ))
                    .ToList()
        );
        if (!string.IsNullOrWhiteSpace(dialog["Contact"]))
            contacts.Add(
                new(dialog["Contact"], contact?.Position, contact?.Email, contact?.Phone, true)
            );
        await Run(async () =>
            await _api.SaveSupplierAsync(
                _session.Token,
                new(
                    item?.SupplierId,
                    dialog["Legal"],
                    dialog["Trade"],
                    dialog["Tax"],
                    dialog["Email"],
                    dialog["Phone"],
                    dialog["Payment"],
                    dialog["Observation"],
                    item?.StatusCode ?? "ACTIVO",
                    contacts,
                    item?.RowVersion
                )
            )
        );
    }

    /// <summary>Confirma la baja o reactivación lógica del proveedor sin eliminar su historial ni contactos.</summary>
    /// <returns>Tarea que termina al cancelar la confirmación o guardar el nuevo estado.</returns>
    private async Task ChangeSupplierStatus()
    {
        var item = Selected<SupplierResponse>(_suppliers);
        if (item is null)
        {
            Error(Localization.Text("Purchases_SelectSupplier"));
            return;
        }
        var target = item.StatusCode == "ACTIVO" ? "INACTIVO" : "ACTIVO";
        if (
            MessageBox.Show(
                this,
                Localization.Format(
                    "Purchases_ChangeSupplierStatusQuestion",
                    item.Code,
                    Localization.Text(target == "ACTIVO" ? "Common_Active" : "Common_Inactive")
                ),
                "OxiTigre",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            ) != DialogResult.Yes
        )
            return;
        var contacts = _data!
            .SupplierContacts.Where(x =>
                x.SupplierId == item.SupplierId && x.StatusCode == "ACTIVO"
            )
            .Select(x => new SupplierContactRequest(
                x.Name,
                x.Position,
                x.Email,
                x.Phone,
                x.IsPrimary
            ))
            .ToList();
        await Run(async () =>
            await _api.SaveSupplierAsync(
                _session.Token,
                new(
                    item.SupplierId,
                    item.LegalName,
                    item.TradeName,
                    item.TaxId,
                    item.Email,
                    item.Phone,
                    item.PaymentTerms,
                    item.Observation,
                    target,
                    contacts,
                    item.RowVersion
                )
            )
        );
    }

    /// <summary>Permite editar exclusivamente órdenes en borrador e informa por qué otros estados no son editables.</summary>
    /// <returns>Tarea que termina al cerrar el editor o al actualizar la orden.</returns>
    private async Task EditOrder()
    {
        var item = Selected<OrderView>(_orders);
        if (item is null)
        {
            Error(Localization.Text("Purchases_SelectOrder"));
            return;
        }
        if (item.Status != "BORRADOR")
        {
            Error(
                Localization.Format(
                    "Purchases_EditDraftOnly",
                    item.Code,
                    item.Status.Replace('_', ' ')
                )
            );
            return;
        }
        await SaveOrder(item);
    }

    /// <summary>Muestra la cabecera histórica, importes y renglones de la orden seleccionada.</summary>
    /// <returns>Tarea completada después de cerrar el detalle, incluso si no había orden seleccionada.</returns>
    private Task ViewOrder()
    {
        var item = Selected<OrderView>(_orders);
        if (item is null || _data is null)
        {
            Error(Localization.Text("Purchases_NoSelection"));
            return Task.CompletedTask;
        }
        using var form = new PurchaseOrderDetailForm(
            _data.Orders.Where(line => line.PurchaseOrderId == item.Id).ToList()
        );
        form.ShowDialog(this);
        return Task.CompletedTask;
    }

    /// <summary>Abre el editor guiado y envía a la API la nueva orden o la revisión de su borrador.</summary>
    /// <param name="item">Borrador a modificar; <see langword="null"/> para crear una orden.</param>
    /// <returns>Tarea que termina al cancelar el editor o persistir la orden y actualizar la vista.</returns>
    private async Task SaveOrder(OrderView? item)
    {
        if (
            _data is null
            || _data.Suppliers.Count == 0
            || _data.Products.Count == 0
            || _data.Warehouses.Count == 0
        )
        {
            Error(Localization.Text("Purchases_NoSelection"));
            return;
        }
        using var dialog = new PurchaseOrderEditForm(_data, item);
        if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Request is null)
            return;
        await Run(async () => await _api.SavePurchaseOrderAsync(_session.Token, dialog.Request));
    }

    /// <summary>Envía un borrador a aprobación usando su versión actual.</summary>
    /// <returns>Tarea de transición y recarga de la orden.</returns>
    private Task Submit() =>
        Transition("BORRADOR", (x, r) => _api.SubmitPurchaseOrderAsync(_session.Token, x.Id, r));

    /// <summary>Aprueba una orden pendiente con control de concurrencia.</summary>
    /// <returns>Tarea de transición y recarga de la orden.</returns>
    private Task Approve() =>
        Transition(
            "PENDIENTE_APROBACION",
            (x, r) => _api.ApprovePurchaseOrderAsync(_session.Token, x.Id, r)
        );

    /// <summary>Solicita la cancelación de la orden seleccionada; la API valida el estado permitido.</summary>
    /// <returns>Tarea de transición y recarga de la orden.</returns>
    private Task Cancel() =>
        Transition(null, (x, r) => _api.CancelPurchaseOrderAsync(_session.Token, x.Id, r));

    /// <summary>Aplica una acción a la orden seleccionada con su <c>RowVersion</c> y refresca el resultado.</summary>
    /// <param name="state">Estado exigido por la acción; <see langword="null"/> si lo valida la API.</param>
    /// <param name="action">Operación HTTP que ejecuta el cambio de estado.</param>
    /// <returns>Tarea que termina tras mostrar una selección inválida o ejecutar y recargar la transición.</returns>
    private async Task Transition(
        string? state,
        Func<OrderView, PurchaseOrderTransitionRequest, Task> action
    )
    {
        var item = Selected<OrderView>(_orders);
        if (item is null || (state is not null && item.Status != state))
        {
            Error(Localization.Text("Purchases_NoSelection"));
            return;
        }
        await Run(async () =>
        {
            await action(item, new(item.RowVersion));
            return new SavedPurchasingResponse(item.Id);
        });
    }

    /// <summary>Cierra el saldo pendiente de una orden aprobada o recibida parcialmente con motivo obligatorio.</summary>
    /// <returns>Tarea que termina al cancelar el diálogo o registrar el cierre y recargar la orden.</returns>
    private async Task CloseBalance()
    {
        var item = Selected<OrderView>(_orders);
        if (item is null || item.Status is not ("APROBADA" or "RECIBIDA_PARCIAL"))
        {
            Error(Localization.Text("Purchases_NoSelection"));
            return;
        }
        using var dialog = new SimpleEntryForm(
            Localization.Text("Purchases_Close"),
            new EntryField(
                "Reason",
                Localization.Text("Purchases_Reason"),
                Required: true,
                Multiline: true
            )
        );
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;
        await Run(async () =>
        {
            await _api.ClosePurchaseOrderAsync(
                _session.Token,
                item.Id,
                new(dialog["Reason"], item.RowVersion)
            );
            return new SavedPurchasingResponse(item.Id);
        });
    }

    /// <summary>Registra una recepción sobre una orden aprobada, incluida una entrega parcial.</summary>
    /// <returns>Tarea que termina al cancelar el diálogo o guardar la recepción y actualizar saldos.</returns>
    private async Task Receive()
    {
        var item = Selected<OrderView>(_orders);
        if (item is null || _data is null || item.Status is not ("APROBADA" or "RECIBIDA_PARCIAL"))
        {
            Error(Localization.Text("Purchases_NoSelection"));
            return;
        }
        using var dialog = new GoodsReceiptEditForm(_data, item);
        if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Request is null)
            return;
        await Run(async () =>
            await _api.CreateGoodsReceiptAsync(_session.Token, item.Id, dialog.Request)
        );
    }

    /// <summary>Revierte una recepción confirmada tras solicitar motivo y segunda confirmación explícita.</summary>
    /// <returns>Tarea que termina al cancelar o registrar la reversión compensatoria.</returns>
    private async Task ReverseReceipt()
    {
        var item = Selected<GoodsReceiptLineResponse>(_receipts);
        if (item is null || item.StatusCode != "CONFIRMADA")
        {
            Error(Localization.Text("Purchases_SelectConfirmedReceipt"));
            return;
        }
        using var dialog = new SimpleEntryForm(
            Localization.Text("Purchases_ReverseReceipt"),
            new EntryField(
                "Reason",
                Localization.Text("Purchases_Reason"),
                Required: true,
                Multiline: true,
                Help: Localization.Text("Purchases_ReverseHelp")
            )
        );
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;
        if (
            MessageBox.Show(
                this,
                Localization.Format("Purchases_ReverseQuestion", item.ReceiptCode),
                "OxiTigre",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            ) != DialogResult.Yes
        )
            return;
        await Run(async () =>
        {
            await _api.ReverseGoodsReceiptAsync(
                _session.Token,
                item.GoodsReceiptId,
                new(dialog["Reason"], item.RowVersion)
            );
            return new SavedPurchasingResponse(item.GoodsReceiptId);
        });
    }

    /// <summary>Ejecuta una escritura de Compras, recarga su estado y presenta errores HTTP sin perder la vista.</summary>
    /// <param name="action">Solicitud a la API que devuelve el identificador afectado.</param>
    /// <returns>Tarea que termina tras la recarga o la notificación del error de red.</returns>
    private async Task Run(Func<Task<SavedPurchasingResponse>> action)
    {
        try
        {
            await action();
            await LoadAsync();
        }
        catch (HttpRequestException ex)
        {
            Error(ex.Message);
        }
    }

    /// <summary>Indica si la sesión posee el permiso solicitado.</summary>
    /// <param name="permission">Código del permiso que se comprueba.</param>
    /// <returns>Verdadero cuando la sesión posee el permiso solicitado; en caso contrario, falso.</returns>
    private bool Has(string permission) =>
        _session.Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);

    /// <summary>Muestra un error funcional sin exponer detalles técnicos.</summary>
    /// <param name="message">Mensaje funcional que se muestra al usuario.</param>
    private static void Error(string message) =>
        MessageBox.Show(message, "OxiTigre", MessageBoxButtons.OK, MessageBoxIcon.Warning);

    /// <summary>Crea una grilla con el estilo y comportamiento común del módulo de compras.</summary>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private static DataGridView Grid()
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AutoGenerateColumns = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
        };
        UiTheme.Grid(grid);
        return grid;
    }

    /// <summary>Actualiza las grillas y detalles de compras con la información cargada.</summary>
    /// <param name="grid">Grilla que se configura, consulta o actualiza.</param>
    /// <param name="items">Registros u opciones que se presentan en el control.</param>
    /// <param name="hidden">Nombres de propiedades técnicas que no deben mostrarse.</param>
    private static void Bind<T>(DataGridView grid, IReadOnlyList<T> items, params string[] hidden)
    {
        grid.DataSource = null;
        grid.DataSource = items.ToList();
        foreach (var name in hidden)
            if (grid.Columns[name] is { } c)
                c.Visible = false;
        Phase7Headers.Apply(grid);
    }

    /// <summary>Obtiene el registro actualmente seleccionado en la grilla.</summary>
    /// <param name="grid">Grilla que se configura, consulta o actualiza.</param>
    /// <returns>Objeto construido u obtenido por la operación.</returns>
    private static T? Selected<T>(DataGridView grid)
        where T : class => grid.CurrentRow?.DataBoundItem as T;

    /// <summary>Crea una pestaña localizada y agrega el contenido indicado.</summary>
    /// <param name="key">Clave de recurso o campo que identifica el texto o control.</param>
    /// <param name="control">Control visual asociado al campo o sección.</param>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private static TabPage Page(string key, Control control)
    {
        var page = new TabPage(Localization.Text(key));
        control.Dock = DockStyle.Fill;
        page.Controls.Add(control);
        return page;
    }

    /// <summary>Crea una ayuda contextual localizada para la sección.</summary>
    /// <param name="key">Clave de recurso o campo que identifica el texto o control.</param>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private static Label Info(string key) =>
        new()
        {
            Text = Localization.Text(key),
            Dock = DockStyle.Top,
            Height = 48,
            Padding = new Padding(10),
            BackColor = Color.FromArgb(230, 244, 234),
            ForeColor = UiTheme.Primary,
        };

    /// <summary>Crea una barra de acciones coherente con el módulo de compras.</summary>
    /// <param name="actions">Acciones disponibles en la barra de herramientas.</param>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private static FlowLayoutPanel Toolbar(
        params (string Key, bool Enabled, Func<Task> Action)[] actions
    )
    {
        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 58,
            Padding = new Padding(8),
            WrapContents = false,
        };
        foreach (var a in actions)
        {
            var b = UiTheme.Button(Localization.Text(a.Key), 0, 0, 140);
            b.Enabled = a.Enabled;
            b.Click += async (_, _) => await a.Action();
            bar.Controls.Add(b);
        }
        return bar;
    }

    internal sealed record OrderView(
        long Id,
        string Code,
        DateTime Date,
        DateOnly? Expected,
        string Supplier,
        string Warehouse,
        decimal Total,
        string Status,
        string RowVersion
    );

    internal sealed record LineView(
        long Id,
        string ProductCode,
        string Product,
        decimal Ordered,
        decimal Received,
        decimal Pending,
        decimal UnitCost,
        decimal Total,
        string Unit
    );
}

/// <summary>Muestra la cabecera histórica y todos los renglones valorizados de una orden de compra.</summary>
internal sealed class PurchaseOrderDetailForm : Form
{
    /// <summary>Construye una vista de solo lectura a partir de los renglones de una misma orden.</summary>
    /// <param name="lines">Renglones de la orden, incluyendo sus datos históricos de proveedor e importes.</param>
    /// <exception cref="ArgumentException">La colección no contiene renglones.</exception>
    internal PurchaseOrderDetailForm(IReadOnlyList<PurchaseOrderLineResponse> lines)
    {
        if (lines.Count == 0)
            throw new ArgumentException("La orden no contiene renglones.", nameof(lines));
        var order = lines[0];
        Text = $"{Localization.Text("Purchases_OrderDetail")} · {order.OrderCode}";
        Width = 1080;
        Height = 680;
        MinimumSize = new Size(900, 560);
        StartPosition = FormStartPosition.CenterParent;
        UiTheme.Apply(this);

        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 188,
            Padding = new Padding(14),
            ColumnCount = 4,
            RowCount = 5,
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        AddPair(header, 0, 0, "Purchases_OrderNumber", order.OrderCode);
        AddPair(header, 0, 2, "Common_Status", order.StatusCode);
        AddPair(
            header,
            1,
            0,
            "Purchases_OrderDate",
            order.OrderDateUtc.ToLocalTime().ToString("g", CultureInfo.CurrentCulture)
        );
        AddPair(
            header,
            1,
            2,
            "Purchases_Expected",
            order.ExpectedDeliveryDate?.ToString("d", CultureInfo.CurrentCulture) ?? "—"
        );
        AddPair(header, 2, 0, "Purchases_Supplier", order.Supplier);
        AddPair(header, 2, 2, "Purchases_TaxId", order.SupplierTaxId);
        AddPair(header, 3, 0, "Purchases_PaymentTerms", order.SupplierPaymentTerms ?? "—");
        AddPair(header, 3, 2, "Inventory_Warehouse", order.Warehouse);
        AddPair(header, 4, 0, "Purchases_Currency", order.Currency);
        AddPair(header, 4, 2, "Config_Description", order.Observation ?? "—");

        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AutoGenerateColumns = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        };
        grid.DataSource = lines
            .Select(line => new OrderDetailLine(
                line.ProductCode,
                line.Product,
                line.OrderedQuantity,
                line.ReceivedQuantity,
                line.PendingQuantity,
                line.UnitCost,
                line.DiscountAmount,
                line.TaxAmount,
                line.LineTotal,
                line.UnitSymbol
            ))
            .ToList();
        UiTheme.Grid(grid);
        Phase7Headers.Apply(grid);

        var totals = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 62,
            Padding = new Padding(12),
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
        };
        totals.Controls.Add(Value(Localization.Text("Column_Total"), order.Total, order.Currency));
        totals.Controls.Add(
            Value(Localization.Text("Purchases_TotalTax"), order.TotalTax, order.Currency)
        );
        totals.Controls.Add(
            Value(Localization.Text("Purchases_TotalDiscount"), order.TotalDiscount, order.Currency)
        );
        totals.Controls.Add(
            Value(Localization.Text("Purchases_Subtotal"), order.Subtotal, order.Currency)
        );
        totals.Controls.Add(UiTheme.HelpButton(Localization.Text("Purchases_OrderDetailHelp")));
        Controls.Add(grid);
        Controls.Add(header);
        Controls.Add(totals);
    }

    /// <summary>Agrega una etiqueta y su valor a la cabecera de la orden.</summary>
    /// <param name="panel">Cuadrícula de cabecera.</param>
    /// <param name="row">Fila de destino.</param>
    /// <param name="column">Columna donde comienza el par.</param>
    /// <param name="key">Clave localizada de la etiqueta.</param>
    /// <param name="value">Valor histórico que se mostrará.</param>
    private static void AddPair(
        TableLayoutPanel panel,
        int row,
        int column,
        string key,
        string value
    )
    {
        panel.Controls.Add(
            new Label
            {
                Text = Localization.Text(key),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Margin = new Padding(3, 7, 3, 3),
            },
            column,
            row
        );
        panel.Controls.Add(
            new Label
            {
                Text = value,
                AutoSize = true,
                Margin = new Padding(3, 7, 3, 3),
            },
            column + 1,
            row
        );
    }

    /// <summary>Crea el total monetario localizado que se muestra al pie.</summary>
    /// <param name="caption">Nombre del total.</param>
    /// <param name="amount">Importe monetario.</param>
    /// <param name="currency">Código ISO de la moneda.</param>
    /// <returns>Etiqueta lista para agregar al panel de totales.</returns>
    private static Label Value(string caption, decimal amount, string currency) =>
        new()
        {
            AutoSize = true,
            Text = $"{caption}: {amount:N2} {currency}",
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Margin = new Padding(16, 8, 0, 0),
        };

    private sealed record OrderDetailLine(
        string ProductCode,
        string Product,
        decimal Ordered,
        decimal Received,
        decimal Pending,
        decimal UnitCost,
        decimal DiscountAmount,
        decimal TaxAmount,
        decimal LineTotal,
        string Unit
    );
}

/// <summary>Permite crear o editar una orden de compra en estado borrador.</summary>
internal sealed class PurchaseOrderEditForm : Form
{
    private readonly PurchasingSnapshotResponse _data;
    private readonly PurchasingManagementForm.OrderView? _order;
    private readonly DateTime _orderDateUtc;
    private readonly ComboBox _supplier = new();
    private readonly ComboBox _warehouse = new();
    private readonly ComboBox _product = new();
    private readonly DateTimePicker _expected = new() { ShowCheckBox = true };
    private readonly TextBox _observation = new()
    {
        Multiline = true,
        ScrollBars = ScrollBars.Vertical,
    };
    private readonly NumericUpDown _quantity = Number();
    private readonly NumericUpDown _unitCost = Number();
    private readonly NumericUpDown _discount = Percent();
    private readonly NumericUpDown _tax = Percent();
    private readonly DataGridView _lines = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AutoGenerateColumns = true,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
    };
    private readonly Label _totals = new()
    {
        AutoSize = true,
        Font = new Font("Segoe UI", 10F, FontStyle.Bold),
    };
    private readonly List<PurchaseOrderDetailRequest> _items = [];

    /// <summary>Construye el editor y precarga los datos históricos cuando se modifica un borrador.</summary>
    /// <param name="data">Catálogos y órdenes disponibles para la empresa.</param>
    /// <param name="item">Orden borrador a editar o <see langword="null"/> para una nueva.</param>
    internal PurchaseOrderEditForm(
        PurchasingSnapshotResponse data,
        PurchasingManagementForm.OrderView? item
    )
    {
        _data = data;
        _order = item;
        var header = item is null
            ? null
            : data.Orders.First(line => line.PurchaseOrderId == item.Id);
        _orderDateUtc = header?.OrderDateUtc ?? DateTime.UtcNow;

        Text = Localization.Text(item is null ? "Purchases_NewOrder" : "Purchases_EditOrder");
        Width = 1200;
        Height = 780;
        MinimumSize = new Size(1000, 680);
        StartPosition = FormStartPosition.CenterParent;
        WindowState = FormWindowState.Maximized;
        UiTheme.Apply(this);

        Setup(
            _supplier,
            data.Suppliers.Where(supplier =>
                    supplier.StatusCode == "ACTIVO" || header?.SupplierId == supplier.SupplierId
                )
                .Select(supplier => new KeyValuePair<long, string>(
                    supplier.SupplierId,
                    supplier.LegalName
                ))
        );
        Setup(
            _warehouse,
            data.Warehouses.Select(warehouse => new KeyValuePair<long, string>(
                warehouse.WarehouseId,
                warehouse.Name
            ))
        );
        Setup(
            _product,
            data.Products.Select(product => new KeyValuePair<long, string>(
                product.ProductId,
                $"{product.Code} · {product.Name}"
            )),
            280
        );

        if (header is not null)
        {
            _supplier.SelectedValue = header.SupplierId;
            _warehouse.SelectedValue = header.WarehouseId;
            _observation.Text = header.Observation;
            if (header.ExpectedDeliveryDate is { } expected)
            {
                _expected.Checked = true;
                _expected.Value = expected.ToDateTime(TimeOnly.MinValue);
            }
            _items.AddRange(
                data.Orders.Where(line => line.PurchaseOrderId == item!.Id)
                    .Select(line => new PurchaseOrderDetailRequest(
                        line.ProductId,
                        line.OrderedQuantity,
                        line.UnitCost,
                        line.DiscountRate,
                        line.TaxRate
                    ))
            );
        }

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 1,
            RowCount = 5,
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 142));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 122));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));

        var information = new Label
        {
            Text = Localization.Text("Purchases_OrderEditorHelp"),
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            BackColor = Color.FromArgb(230, 244, 234),
            ForeColor = UiTheme.Primary,
        };
        layout.Controls.Add(information, 0, 0);
        layout.Controls.Add(GeneralInformation(), 0, 1);
        layout.Controls.Add(LineEditor(), 0, 2);
        layout.Controls.Add(LinesSection(), 0, 3);
        layout.Controls.Add(Actions(), 0, 4);
        Controls.Add(layout);

        UiTheme.Grid(_lines);
        _lines.SelectionChanged += (_, _) => LoadSelectedLine();
        RefreshLines();
    }

    /// <summary>Obtiene la solicitud validada cuando el usuario confirma el formulario.</summary>
    internal SavePurchaseOrderRequest? Request { get; private set; }

    /// <summary>Construye la sección de proveedor, depósito, entrega y observación de la orden.</summary>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private Control GeneralInformation()
    {
        var fields = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10, 8, 10, 4),
            WrapContents = false,
            AutoScroll = true,
        };
        fields.Controls.Add(Field("Purchases_Supplier", _supplier, 300));
        fields.Controls.Add(Field("Inventory_Warehouse", _warehouse, 260));
        fields.Controls.Add(Field("Purchases_Expected", _expected, 230));
        fields.Controls.Add(Field("Purchases_Currency", ReadOnlyValue("ARS"), 90));
        fields.Controls.Add(Field("Config_Description", _observation, 360, 76));
        return Section("Purchases_OrderInformation", fields);
    }

    /// <summary>Construye el editor de producto, cantidad, costo, descuento e impuesto.</summary>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private Control LineEditor()
    {
        var fields = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10, 8, 10, 4),
            WrapContents = false,
            AutoScroll = true,
        };
        fields.Controls.Add(Field("Inventory_Product", _product, 300));
        fields.Controls.Add(Field("Inventory_Quantity", _quantity, 120));
        fields.Controls.Add(Field("Purchases_UnitCost", _unitCost, 130));
        fields.Controls.Add(Field("Sales_Discount", _discount, 110));
        fields.Controls.Add(Field("Sales_Tax", _tax, 110));

        var add = UiTheme.Button(Localization.Text("Purchases_AddOrUpdateLine"), 0, 0, 170);
        add.Margin = new Padding(12, 28, 4, 0);
        add.Click += (_, _) => AddOrUpdateLine();
        var remove = UiTheme.Button(
            Localization.Text("Purchases_RemoveLine"),
            0,
            0,
            145,
            ButtonTone.Danger
        );
        remove.Margin = new Padding(4, 28, 4, 0);
        remove.Click += (_, _) => RemoveSelectedLine();
        fields.Controls.Add(add);
        fields.Controls.Add(remove);
        return Section("Purchases_LineEditor", fields);
    }

    /// <summary>Construye la grilla de renglones y sus acciones de edición.</summary>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private Control LinesSection()
    {
        var panel = new Panel { Dock = DockStyle.Fill };
        var bottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 42,
            Padding = new Padding(10, 10, 12, 0),
        };
        _totals.Dock = DockStyle.Right;
        bottom.Controls.Add(_totals);
        panel.Controls.Add(_lines);
        panel.Controls.Add(bottom);
        return Section("Purchases_LoadedLines", panel);
    }

    /// <summary>Construye las acciones de guardar o cancelar la edición de la orden.</summary>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private Control Actions()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(8),
            WrapContents = false,
        };
        var save = UiTheme.Button(Localization.Text("Common_Save"), 0, 0, 150);
        var cancel = UiTheme.Button(
            Localization.Text("Common_Cancel"),
            0,
            0,
            150,
            ButtonTone.Neutral
        );
        save.Click += (_, _) => Save();
        cancel.DialogResult = DialogResult.Cancel;
        panel.Controls.Add(save);
        panel.Controls.Add(cancel);
        AcceptButton = save;
        CancelButton = cancel;
        return panel;
    }

    /// <summary>Incorpora el producto elegido o reemplaza su renglón, evitando duplicados en el borrador.</summary>
    private void AddOrUpdateLine()
    {
        if (_product.SelectedValue is null || _quantity.Value <= 0 || _unitCost.Value < 0)
        {
            Warn("Purchases_InvalidLine");
            return;
        }

        var productId = Id(_product);
        _items.RemoveAll(line => line.ProductId == productId);
        _items.Add(
            new PurchaseOrderDetailRequest(
                productId,
                _quantity.Value,
                _unitCost.Value,
                _discount.Value / 100,
                _tax.Value / 100
            )
        );
        RefreshLines();
        ClearLineEditor();
    }

    /// <summary>Quita el renglón seleccionado del borrador antes de guardar la orden.</summary>
    private void RemoveSelectedLine()
    {
        if (_lines.CurrentRow?.DataBoundItem is not PurchaseLineView selected)
        {
            Warn("Purchases_SelectLine");
            return;
        }
        _items.RemoveAll(line => line.ProductId == selected.ProductId);
        RefreshLines();
        ClearLineEditor();
    }

    /// <summary>Copia al editor cantidad, costo, descuento e impuesto del renglón seleccionado.</summary>
    private void LoadSelectedLine()
    {
        if (_lines.CurrentRow?.DataBoundItem is not PurchaseLineView selected)
            return;
        var line = _items.First(item => item.ProductId == selected.ProductId);
        _product.SelectedValue = line.ProductId;
        _quantity.Value = line.Quantity;
        _unitCost.Value = line.UnitCost;
        _discount.Value = line.DiscountRate * 100;
        _tax.Value = line.TaxRate * 100;
    }

    /// <summary>Limpia los importes y la selección tras agregar o quitar un renglón.</summary>
    private void ClearLineEditor()
    {
        _quantity.Value = 0;
        _unitCost.Value = 0;
        _discount.Value = 0;
        _tax.Value = 0;
        _lines.ClearSelection();
        _lines.CurrentCell = null;
    }

    /// <summary>Recalcula el total visible de cada renglón y de la orden con descuento e impuesto.</summary>
    private void RefreshLines()
    {
        var rows = _items
            .Select(line =>
            {
                var product = _data.Products.First(item => item.ProductId == line.ProductId);
                var subtotal = line.Quantity * line.UnitCost;
                var discountAmount = subtotal * line.DiscountRate;
                var taxAmount = (subtotal - discountAmount) * line.TaxRate;
                return new PurchaseLineView(
                    line.ProductId,
                    product.Code,
                    product.Name,
                    line.Quantity,
                    product.UnitSymbol,
                    line.UnitCost,
                    line.DiscountRate * 100,
                    line.TaxRate * 100,
                    subtotal - discountAmount + taxAmount
                );
            })
            .ToList();
        _lines.DataSource = null;
        _lines.DataSource = rows;
        if (_lines.Columns[nameof(PurchaseLineView.ProductId)] is { } id)
            id.Visible = false;
        Phase7Headers.Apply(_lines);
        _totals.Text = Localization.Format("Purchases_EditorTotal", rows.Sum(row => row.Total));
    }

    /// <summary>Construye la solicitud solo si hay proveedor, depósito y al menos un renglón.</summary>
    private void Save()
    {
        if (_supplier.SelectedValue is null || _warehouse.SelectedValue is null)
        {
            Warn("Common_RequiredFields");
            return;
        }
        if (_items.Count == 0)
        {
            Warn("Purchases_OrderNeedsLines");
            return;
        }

        Request = new SavePurchaseOrderRequest(
            _order?.Id,
            Id(_supplier),
            Id(_warehouse),
            _orderDateUtc,
            _expected.Checked ? DateOnly.FromDateTime(_expected.Value) : null,
            "ARS",
            string.IsNullOrWhiteSpace(_observation.Text) ? null : _observation.Text.Trim(),
            _items.ToList(),
            _order?.RowVersion
        );
        DialogResult = DialogResult.OK;
    }

    /// <summary>Muestra una advertencia funcional al usuario.</summary>
    /// <param name="key">Clave de recurso o campo que identifica el texto o control.</param>
    private void Warn(string key) =>
        MessageBox.Show(
            this,
            Localization.Text(key),
            "OxiTigre",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning
        );

    /// <summary>Crea una sección titulada y organiza su contenido y acciones.</summary>
    /// <param name="titleKey">Clave de recurso usada como título visible.</param>
    /// <param name="content">Control visual que se incorpora a la pestaña o sección.</param>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private static Control Section(string titleKey, Control content)
    {
        var section = new GroupBox
        {
            Text = Localization.Text(titleKey),
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Padding = new Padding(8),
        };
        content.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        section.Controls.Add(content);
        return section;
    }

    /// <summary>Crea un campo de edición con etiqueta, tamaño y comportamiento definidos.</summary>
    /// <param name="key">Clave de recurso o campo que identifica el texto o control.</param>
    /// <param name="input">Texto ingresado que se interpreta.</param>
    /// <param name="width">Ancho asignado al control.</param>
    /// <param name="height">Alto asignado al control.</param>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private static Control Field(string key, Control input, int width, int height = 62)
    {
        input.Width = width - 8;
        input.Height = Math.Max(input.Height, height - 26);
        var panel = new Panel
        {
            Width = width,
            Height = height,
            Margin = new Padding(4),
        };
        var label = new Label
        {
            Text = Localization.Text(key),
            AutoSize = true,
            Left = 0,
            Top = 0,
        };
        input.Left = 0;
        input.Top = 22;
        panel.Controls.Add(label);
        panel.Controls.Add(input);
        return panel;
    }

    /// <summary>Lee only value desde el resultado disponible.</summary>
    /// <param name="value">Texto que se normaliza, valida o asigna.</param>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private static TextBox ReadOnlyValue(string value) =>
        new()
        {
            Text = value,
            ReadOnly = true,
            BackColor = Color.White,
        };

    /// <summary>Configura una lista desplegable con identificadores y textos visibles.</summary>
    /// <param name="combo">Lista desplegable cuya selección se consulta o configura.</param>
    /// <param name="items">Registros u opciones que se presentan en el control.</param>
    /// <param name="width">Ancho asignado al control.</param>
    private static void Setup(
        ComboBox combo,
        IEnumerable<KeyValuePair<long, string>> items,
        int width = 240
    )
    {
        combo.Width = width;
        combo.DropDownStyle = ComboBoxStyle.DropDownList;
        combo.DisplayMember = "Value";
        combo.ValueMember = "Key";
        combo.DataSource = items.ToList();
    }

    /// <summary>Obtiene el identificador seleccionado en una lista desplegable.</summary>
    /// <param name="combo">Lista desplegable cuya selección se consulta o configura.</param>
    /// <returns>Valor obtenido después de aplicar la conversión o búsqueda.</returns>
    private static long Id(ComboBox combo) =>
        Convert.ToInt64(combo.SelectedValue, CultureInfo.InvariantCulture);

    /// <summary>Crea un control numérico para cantidades y costos.</summary>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private static NumericUpDown Number() => UiTheme.Number(4, 110);

    /// <summary>Crea un control numérico limitado a porcentajes.</summary>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private static NumericUpDown Percent()
    {
        var value = UiTheme.Number(2, 95);
        value.Maximum = 100;
        return value;
    }

    private sealed record PurchaseLineView(
        long ProductId,
        string ProductCode,
        string Product,
        decimal Quantity,
        string Unit,
        decimal UnitCost,
        decimal DiscountRate,
        decimal TaxRate,
        decimal Total
    );
}

internal sealed class GoodsReceiptEditForm : Form
{
    private readonly PurchasingSnapshotResponse _data;
    private readonly List<ReceiptRow> _rows;
    private readonly DataGridView _grid = new()
    {
        Dock = DockStyle.Fill,
        AutoGenerateColumns = true,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
    };
    private readonly TextBox _document = new() { Width = 180 };
    internal CreateGoodsReceiptRequest? Request { get; private set; }

    /// <summary>Inicializa el componente de compras con sus dependencias y datos de trabajo.</summary>
    /// <param name="data">Datos cargados que alimentan la pantalla.</param>
    /// <param name="order">Orden seleccionada sobre la que se opera.</param>
    internal GoodsReceiptEditForm(
        PurchasingSnapshotResponse data,
        PurchasingManagementForm.OrderView order
    )
    {
        _data = data;
        Text = Localization.Text("Purchases_Receive");
        Width = 1120;
        Height = 650;
        StartPosition = FormStartPosition.CenterParent;
        UiTheme.Apply(this);
        _rows = data
            .Orders.Where(x => x.PurchaseOrderId == order.Id && x.PendingQuantity > 0)
            .Select(x => new ReceiptRow(
                x.PurchaseOrderLineId,
                x.ProductCode,
                x.Product,
                x.TrackingType(data),
                x.PendingQuantity
            ))
            .ToList();
        _grid.DataSource = _rows;
        UiTheme.Grid(_grid);
        foreach (var name in new[] { "Id", "Tracking" })
            if (_grid.Columns[name] is { } c)
                c.Visible = false;
        Phase7Headers.Apply(_grid);
        var top = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 52,
            Padding = new Padding(8),
        };
        top.Controls.Add(
            new Label
            {
                Text = Localization.Text("Purchases_Document"),
                AutoSize = true,
                Margin = new Padding(5, 8, 5, 0),
            }
        );
        top.Controls.Add(_document);
        top.Controls.Add(UiTheme.HelpButton(Localization.Text("Purchases_ReceiptHelp")));
        var save = UiTheme.Button(Localization.Text("Common_Save"), 0, 0, 150);
        save.Dock = DockStyle.Bottom;
        save.Click += (_, _) => Save(order);
        Controls.Add(_grid);
        Controls.Add(top);
        Controls.Add(save);
    }

    /// <summary>Convierte cantidades aceptadas, rechazadas y dañadas en una recepción parcial auditable.</summary>
    /// <param name="order">Orden origen y versión vigente para impedir recepciones sobre datos obsoletos.</param>
    private void Save(PurchasingManagementForm.OrderView order)
    {
        try
        {
            var details = _rows
                .Where(x => x.Accepted + x.Rejected + x.Damaged > 0)
                .Select(x => new GoodsReceiptDetailRequest(
                    x.Id,
                    x.Accepted,
                    x.Rejected,
                    x.Damaged,
                    x.Reason,
                    ParseLots(x.Lots),
                    ParseSerials(x.Serials),
                    ParseContents(x.ContainerContents)
                ))
                .ToList();
            if (details.Count == 0)
                return;
            Request = new(DateTime.UtcNow, _document.Text.Trim(), null, details, order.RowVersion);
            DialogResult = DialogResult.OK;
        }
        catch (FormatException ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "OxiTigre",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
        }
    }

    /// <summary>Interpreta lotes escritos como <c>código:cantidad</c>, separados por punto y coma.</summary>
    /// <param name="text">Lotes opcionales cargados por el operador.</param>
    /// <returns>Lotes y cantidades declaradas para la recepción.</returns>
    /// <exception cref="FormatException">Algún lote no tiene dos componentes o su cantidad no es numérica.</exception>
    private static IReadOnlyList<GoodsReceiptLotRequest> ParseLots(string? text) =>
        string.IsNullOrWhiteSpace(text)
            ? []
            : text.Split(
                    ';',
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                )
                .Select(x =>
                {
                    var p = x.Split(':', StringSplitOptions.TrimEntries);
                    return
                        p.Length == 2
                        && decimal.TryParse(
                            p[1],
                            NumberStyles.Number,
                            CultureInfo.CurrentCulture,
                            out var q
                        )
                        ? new GoodsReceiptLotRequest(p[0], q, null, null)
                        : throw new FormatException(Localization.Text("Purchases_InvalidLots"));
                })
                .ToList();

    /// <summary>Separa los números de serie de envases que ingresan como activos de OxiTigre.</summary>
    /// <param name="text">Series opcionales separadas por punto y coma.</param>
    /// <returns>Series con su tipo, propietario y condición inicial.</returns>
    private static IReadOnlyList<GoodsReceiptSerialRequest> ParseSerials(string? text) =>
        string.IsNullOrWhiteSpace(text)
            ? []
            : text.Split(
                    ';',
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                )
                .Select(x => new GoodsReceiptSerialRequest(
                    x,
                    "ENVASE",
                    null,
                    "OXITIGRE",
                    "OxiTigre",
                    "OPERATIVO"
                ))
                .ToList();

    /// <summary>Vincula el contenido recibido a serie, producto, lote, cantidad y método de medición.</summary>
    /// <param name="text">Contenido opcional en el formato guiado de cinco componentes.</param>
    /// <returns>Contenido de cada envase reconocido en el catálogo de productos.</returns>
    /// <exception cref="FormatException">Faltan componentes, el producto no existe o la cantidad es inválida.</exception>
    private IReadOnlyList<GoodsReceiptContainerContentRequest> ParseContents(string? text) =>
        string.IsNullOrWhiteSpace(text)
            ? []
            : text.Split(
                    ';',
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                )
                .Select(value =>
                {
                    var parts = value.Split(':', StringSplitOptions.TrimEntries);
                    var product =
                        parts.Length == 5
                            ? _data.Products.FirstOrDefault(x =>
                                x.Code.Equals(parts[1], StringComparison.OrdinalIgnoreCase)
                            )
                            : null;
                    return
                        product is not null
                        && decimal.TryParse(
                            parts[3],
                            NumberStyles.Number,
                            CultureInfo.CurrentCulture,
                            out var quantity
                        )
                        && quantity >= 0
                        ? new GoodsReceiptContainerContentRequest(
                            parts[0],
                            product.ProductId,
                            string.IsNullOrWhiteSpace(parts[2]) ? null : parts[2],
                            quantity,
                            parts[4]
                        )
                        : throw new FormatException(Localization.Text("Purchases_InvalidContents"));
                })
                .ToList();

    /// <summary>Renglón editable de recepción con cantidades y datos de trazabilidad industrial.</summary>
    internal sealed class ReceiptRow(
        long id,
        string productCode,
        string product,
        string tracking,
        decimal pending
    )
    {
        public long Id { get; } = id;
        public string ProductCode { get; } = productCode;
        public string Product { get; } = product;
        public string Tracking { get; } = tracking;
        public decimal Pending { get; } = pending;
        public decimal Accepted { get; set; }
        public decimal Rejected { get; set; }
        public decimal Damaged { get; set; }
        public string? Reason { get; set; }
        public string? Lots { get; set; }
        public string? Serials { get; set; }
        public string? ContainerContents { get; set; }
    }
}

internal static class PurchasingUiExtensions
{
    /// <summary>Obtiene el tipo de trazabilidad configurado para el producto del renglón.</summary>
    /// <param name="line">Renglón de la orden o movimiento que se evalúa.</param>
    /// <param name="data">Datos cargados que alimentan la pantalla.</param>
    /// <returns>Texto normalizado, localizado o formateado por la operación.</returns>
    internal static string TrackingType(
        this PurchaseOrderLineResponse line,
        PurchasingSnapshotResponse data
    ) => data.Products.First(x => x.ProductId == line.ProductId).TrackingType;
}

internal static class Phase7Headers
{
    private static readonly Dictionary<string, string> Keys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Code"] = "Column_Code",
        ["LegalName"] = "Purchases_LegalName",
        ["TradeName"] = "Purchases_TradeName",
        ["TaxId"] = "Purchases_TaxId",
        ["Email"] = "Column_Email",
        ["Phone"] = "Column_Phone",
        ["PaymentTerms"] = "Purchases_PaymentTerms",
        ["Status"] = "Common_Status",
        ["StatusCode"] = "Common_Status",
        ["Date"] = "Column_Date",
        ["Expected"] = "Purchases_Expected",
        ["Supplier"] = "Purchases_Supplier",
        ["Warehouse"] = "Inventory_Warehouse",
        ["Total"] = "Column_Total",
        ["ProductCode"] = "Column_ProductCode",
        ["Product"] = "Inventory_Product",
        ["Ordered"] = "Purchases_Ordered",
        ["Received"] = "Purchases_Received",
        ["Pending"] = "Purchases_Pending",
        ["UnitCost"] = "Purchases_UnitCost",
        ["DiscountRate"] = "Sales_Discount",
        ["TaxRate"] = "Sales_Tax",
        ["DiscountAmount"] = "Purchases_TotalDiscount",
        ["TaxAmount"] = "Purchases_TotalTax",
        ["LineTotal"] = "Column_Total",
        ["Unit"] = "Column_Unit",
        ["UnitSymbol"] = "Column_Unit",
        ["ReceiptCode"] = "Purchases_Receipt",
        ["ReceiptDateUtc"] = "Column_Date",
        ["SupplierDocumentNumber"] = "Purchases_Document",
        ["AcceptedQuantity"] = "Purchases_Accepted",
        ["RejectedQuantity"] = "Purchases_Rejected",
        ["DamagedQuantity"] = "Purchases_Damaged",
        ["DifferenceReason"] = "Purchases_Reason",
        ["Accepted"] = "Purchases_Accepted",
        ["Rejected"] = "Purchases_Rejected",
        ["Damaged"] = "Purchases_Damaged",
        ["Reason"] = "Purchases_Reason",
        ["Lots"] = "Traceability_Lots",
        ["Serials"] = "Column_Serials",
        ["ContainerContents"] = "Traceability_Content",
        ["OwnerCode"] = "Traceability_OwnerCode",
        ["OwnerName"] = "Traceability_Owner",
        ["Owner"] = "Traceability_Owner",
        ["Branch"] = "Config_Branches",
        ["Location"] = "Inventory_Locations",
        ["SerialNumber"] = "Column_Serial",
        ["AssetType"] = "Column_Type",
        ["Condition"] = "Traceability_Condition",
        ["Capacity"] = "Traceability_Capacity",
        ["CapacityUnit"] = "Column_Unit",
        ["ContentProduct"] = "Traceability_Content",
        ["ContentLot"] = "Traceability_Lots",
        ["ContentQuantity"] = "Inventory_Quantity",
        ["ContentUnit"] = "Column_Unit",
        ["ManufactureDate"] = "Traceability_ManufactureDate",
        ["ExpirationDate"] = "Traceability_ExpirationDate",
        ["InitialQuantity"] = "Traceability_InitialQuantity",
        ["SupplierLotCode"] = "Purchases_SupplierLot",
        ["Quantity"] = "Inventory_Quantity",
        ["MeasurementDateUtc"] = "Column_Date",
        ["MeasurementType"] = "Column_Type",
        ["Value"] = "Column_Value",
        ["Method"] = "Column_Method",
        ["Source"] = "Traceability_Source",
        ["IsEstimated"] = "Traceability_Estimated",
        ["DeviceReference"] = "Traceability_Device",
        ["Observation"] = "Config_Description",
        ["TransformationDateUtc"] = "Column_Date",
        ["SourceLot"] = "Traceability_SourceLot",
        ["SourceAsset"] = "Traceability_SourceAsset",
        ["SourceQuantity"] = "Traceability_SourceQuantity",
        ["LossQuantity"] = "Traceability_Loss",
        ["LossReason"] = "Purchases_Reason",
        ["DestinationAsset"] = "Traceability_DestinationAsset",
        ["LoadedQuantity"] = "Traceability_LoadedQuantity",
        ["IncidentDateUtc"] = "Column_Date",
        ["IncidentType"] = "Column_Type",
        ["Asset"] = "Traceability_Assets",
        ["Cause"] = "Traceability_Cause",
        ["ActionTaken"] = "Traceability_Action",
        ["MaintenanceType"] = "Column_Type",
        ["StartDateUtc"] = "Traceability_StartDate",
        ["EndDateUtc"] = "Traceability_EndDate",
        ["WorkDescription"] = "Traceability_Work",
        ["Result"] = "Column_Result",
        ["Cost"] = "Column_Cost",
        ["NextReviewDate"] = "Traceability_NextReview",
        ["Destination"] = "Traceability_Destination",
        ["DeliveryMode"] = "Traceability_DeliveryMode",
        ["DepartureDateUtc"] = "Traceability_DepartureDate",
        ["ExpectedReturnDateUtc"] = "Traceability_ExpectedReturn",
        ["ActualReturnDateUtc"] = "Traceability_ActualReturn",
        ["DepartureQuantity"] = "Traceability_DepartureQuantity",
        ["DepartureCondition"] = "Traceability_DepartureCondition",
        ["ReturnQuantity"] = "Traceability_ReturnQuantity",
        ["ReturnCondition"] = "Traceability_ReturnCondition",
        ["EventType"] = "Traceability_Event",
        ["EventDateUtc"] = "Column_Date",
        ["StatusBefore"] = "Traceability_PreviousStatus",
        ["StatusAfter"] = "Traceability_NewStatus",
        ["ConditionBefore"] = "Traceability_PreviousCondition",
        ["ConditionAfter"] = "Traceability_NewCondition",
        ["QuantityBefore"] = "Traceability_PreviousQuantity",
        ["QuantityAfter"] = "Traceability_NewQuantity",
        ["CorrelationId"] = "Traceability_Correlation",
        ["ReversalReason"] = "Purchases_ReversalReason",
        ["ReversedAtUtc"] = "Purchases_ReversedAt",
    };

    /// <summary>Aplica la operación de compras sobre el estado actual del formulario.</summary>
    /// <param name="grid">Grilla que se configura, consulta o actualiza.</param>
    internal static void Apply(DataGridView grid)
    {
        foreach (DataGridViewColumn column in grid.Columns)
            if (Keys.TryGetValue(column.Name, out var key))
                column.HeaderText = Localization.Text(key);
    }
}
