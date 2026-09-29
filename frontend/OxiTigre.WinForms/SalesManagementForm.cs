/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.WinForms.SalesManagementForm
Archivo: SalesManagementForm.cs | Versión: 1.5.0 | Fecha: 2026-08-28 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Administra listas, pedidos, reservas y ventas internas desde una interfaz homogénea.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-21 | FABRICA | Agustin Omar Cauzi | Ayuda funcional, validación inicial y precio sugerido confiable.
Historial: 1.2.0 | 2026-08-21 | FABRICA | Agustin Omar Cauzi | Promociones configurables y acumulación de cantidades compatibles.
Historial: 1.2.1 | 2026-08-21 | FABRICA | Agustin Omar Cauzi | Redondeo idéntico a SQL y conservación del descuento manual informado.
Historial: 1.2.2 | 2026-08-26 | FABRICA | Agustin Omar Cauzi | Pestañas y búsquedas homogéneas.
Historial: 1.3.0 | 2026-08-26 | FABRICA | Agustin Omar Cauzi | Filtros guiados y detalles visibles con mejor distribución vertical.
Historial: 1.4.0 | 2026-08-28 | FABRICA | Agustin Omar Cauzi | Servicios sin stock y activos vinculados al pedido.
Historial: 1.5.0 | 2026-08-28 | FABRICA | Agustin Omar Cauzi | Ingreso, regreso y devolución prevista configurables por activo.
===============================================================================
*/
using System.Globalization;
using OxiTigre.ApiClient;
using OxiTigre.Contracts.Commercial;
using OxiTigre.Contracts.Security;

namespace OxiTigre.WinForms;

/// <summary>Presenta el ciclo pedido, reserva y venta junto con sus listas de precios.</summary>
internal sealed class SalesManagementForm : Form
{
    private const decimal SqlDecimal19_4Max = 999999999999999.9999m;
    private readonly OxiTigreApiClient _api;
    private readonly LoginResponse _session;
    private readonly bool _canManage;
    private readonly DataGridView _orders = Grid(),
        _orderLines = Grid(),
        _sales = Grid(),
        _saleLines = Grid(),
        _lists = Grid(),
        _prices = Grid(),
        _promotions = Grid();
    private SalesSnapshotResponse? _data;

    /// <summary>Inicializa el componente de pedidos y ventas con sus dependencias y datos de trabajo.</summary>
    /// <param name="api">Cliente usado para comunicarse con la API.</param>
    /// <param name="session">Sesión autenticada que determina permisos y contexto operativo.</param>
    internal SalesManagementForm(OxiTigreApiClient api, LoginResponse session)
    {
        _api = api;
        _session = session;
        _canManage = session.Permissions.Contains(
            "COMERCIAL.VENTAS_GESTIONAR",
            StringComparer.OrdinalIgnoreCase
        );
        Text = Localization.Text("Sales_Title");
        Width = 1260;
        Height = 790;
        MinimumSize = new Size(1000, 650);
        StartPosition = FormStartPosition.CenterParent;
        UiTheme.Apply(this);
        var tabs = new TabControl { Dock = DockStyle.Fill };
        UiTheme.Tabs(tabs);
        tabs.TabPages.Add(Tab("Sales_Orders", OrdersPage()));
        tabs.TabPages.Add(Tab("Sales_Sales", SalesPage()));
        tabs.TabPages.Add(Tab("Sales_PriceLists", PricesPage()));
        tabs.TabPages.Add(Tab("Sales_Promotions", PromotionsPage()));
        Controls.Add(tabs);
        Controls.Add(InformationBar("Sales_Overview"));
        _orders.SelectionChanged += (_, _) => BindOrderLines();
        _sales.SelectionChanged += (_, _) => BindSaleLines();
        _lists.SelectionChanged += (_, _) => BindPrices();
        Shown += async (_, _) => await LoadAsync();
    }

    /// <summary>Construye la vista de pedidos y sus renglones.</summary>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private Control OrdersPage()
    {
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 295,
        };
        split.Panel1.Controls.Add(UiTheme.Searchable(_orders));
        split.Panel1.Controls.Add(InformationBar("Sales_OrdersHelp"));
        split.Panel1.Controls.Add(
            Toolbar(
                "Sales_Orders",
                ("Sales_NewOrder", _canManage, NewOrder),
                ("Sales_EditOrder", _canManage, EditOrder),
                ("Sales_ConfirmOrder", _canManage, ConfirmOrder),
                ("Sales_CancelOrder", _canManage, CancelOrder),
                ("Sales_CreateSale", _canManage, CreateSale)
            )
        );
        split.Panel2.Controls.Add(
            UiTheme.Searchable(_orderLines, title: Localization.Text("Sales_OrderDetails"))
        );
        return split;
    }

    /// <summary>Construye la vista de ventas internas y sus renglones.</summary>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private Control SalesPage()
    {
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 295,
        };
        split.Panel1.Controls.Add(UiTheme.Searchable(_sales));
        split.Panel1.Controls.Add(InformationBar("Sales_SalesHelp"));
        split.Panel1.Controls.Add(TitleBar("Sales_Sales"));
        split.Panel2.Controls.Add(
            UiTheme.Searchable(_saleLines, title: Localization.Text("Sales_SaleDetails"))
        );
        return split;
    }

    /// <summary>Construye la vista de listas de precios y productos.</summary>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private Control PricesPage()
    {
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 265,
        };
        split.Panel1.Controls.Add(UiTheme.Searchable(_lists));
        split.Panel1.Controls.Add(InformationBar("Sales_PricesHelp"));
        split.Panel1.Controls.Add(
            Toolbar(
                "Sales_PriceLists",
                ("Sales_NewList", _canManage, NewList),
                ("Sales_EditList", _canManage, EditList)
            )
        );
        split.Panel2.Controls.Add(UiTheme.Searchable(_prices));
        split.Panel2.Controls.Add(
            Toolbar(
                "Sales_Prices",
                ("Sales_NewPrice", _canManage, NewPrice),
                ("Sales_EditPrice", _canManage, EditPrice)
            )
        );
        return split;
    }

    /// <summary>Construye la vista de promociones comerciales.</summary>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private Control PromotionsPage()
    {
        var panel = new Panel { Dock = DockStyle.Fill };
        panel.Controls.Add(UiTheme.Searchable(_promotions));
        panel.Controls.Add(InformationBar("Sales_PromotionsHelp"));
        panel.Controls.Add(
            Toolbar(
                "Sales_Promotions",
                ("Sales_NewPromotion", _canManage, NewPromotion),
                ("Sales_EditPromotion", _canManage, EditPromotion)
            )
        );
        return panel;
    }

    /// <summary>Carga  y actualiza la interfaz con los datos obtenidos.</summary>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private async Task LoadAsync()
    {
        try
        {
            _data = await _api.GetSalesAsync(_session.Token);
            Bind();
        }
        catch (HttpRequestException exception)
        {
            Error(exception.Message);
        }
    }

    /// <summary>Actualiza las grillas y detalles de pedidos y ventas con la información cargada.</summary>
    private void Bind()
    {
        if (_data is null)
            return;
        var orders = _data
            .Orders.GroupBy(item => item.OrderId)
            .Select(group =>
            {
                var item = group.First();
                return new OrderSummary(
                    item.OrderId,
                    item.ClientId,
                    item.PriceListId,
                    item.OrderCode,
                    item.OrderDateUtc.ToLocalTime(),
                    item.Client,
                    item.Currency,
                    item.Total,
                    item.StatusCode,
                    item.Observation,
                    item.RowVersion
                );
            })
            .ToList();
        Bind(_orders, orders, "OrderId", "ClientId", "PriceListId", "RowVersion", "Observation");
        var sales = _data
            .Sales.GroupBy(item => item.SaleId)
            .Select(group =>
            {
                var item = group.First();
                return new SaleSummary(
                    item.SaleId,
                    item.SaleCode,
                    item.OrderId,
                    item.SaleDateUtc.ToLocalTime(),
                    item.Client,
                    item.Currency,
                    item.Total,
                    item.StatusCode,
                    item.MovementId
                );
            })
            .ToList();
        Bind(_sales, sales, "SaleId", "MovementId");
        Bind(_lists, _data.PriceLists, "PriceListId", "RowVersion");
        Bind(
            _promotions,
            _data.Promotions,
            "PromotionId",
            "ProductId",
            "Description",
            "RowVersion"
        );
        if (_promotions.Columns["ProductCode"] is { } productCode)
            productCode.HeaderText = Localization.Text("Sales_ProductCode");
        if (_promotions.Columns["Code"] is { } promotionCode)
            promotionCode.HeaderText = Localization.Text("Sales_PromotionCode");
        BindOrderLines();
        BindSaleLines();
        BindPrices();
    }

    /// <summary>Vincula los renglones del pedido con sus controles visuales.</summary>
    private void BindOrderLines()
    {
        var selected = Selected<OrderSummary>(_orders);
        var rows =
            selected is null || _data is null
                ? []
                : _data
                    .Orders.Where(item => item.OrderId == selected.OrderId)
                    .Select(item => new OrderLineView(
                        item.ProductCode,
                        item.Product,
                        item.Warehouse,
                        item.Quantity,
                        item.UnitSymbol,
                        item.UnitPrice,
                        item.PromotionName,
                        item.DiscountAmount,
                        item.DiscountRate * 100,
                        item.TaxRate * 100,
                        item.LineTotal
                    ))
                    .ToList();
        Bind(_orderLines, rows);
    }

    /// <summary>Vincula los precios con sus controles visuales.</summary>
    private void BindPrices()
    {
        var selected = Selected<PriceListResponse>(_lists);
        var rows =
            selected is null || _data is null
                ? []
                : _data.Prices.Where(item => item.PriceListId == selected.PriceListId).ToList();
        Bind(_prices, rows, "PriceListProductId", "PriceListId", "ProductId", "RowVersion");
    }

    /// <summary>Vincula los renglones de la venta con sus controles visuales.</summary>
    private void BindSaleLines()
    {
        var selected = Selected<SaleSummary>(_sales);
        var rows =
            selected is null || _data is null
                ? []
                : _data
                    .Sales.Where(item => item.SaleId == selected.SaleId)
                    .Select(item => new SaleLineView(
                        item.ProductCode,
                        item.Product,
                        item.Warehouse,
                        item.Quantity,
                        item.UnitSymbol,
                        item.UnitPrice,
                        item.PromotionName,
                        item.DiscountAmount,
                        item.DiscountRate * 100,
                        item.TaxRate * 100,
                        item.LineTotal
                    ))
                    .ToList();
        Bind(_saleLines, rows);
    }

    /// <summary>Inicia la creación de una lista de precios.</summary>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private Task NewList() => SaveList(null);

    /// <summary>Abre el editor correspondiente a una lista de precios y conserva los cambios confirmados.</summary>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private Task EditList() => EditSelected<PriceListResponse>(_lists, SaveList);

    /// <summary>Valida y guarda una lista de precios mediante la API.</summary>
    /// <param name="item">Registro existente que se edita, o nulo para crear uno nuevo.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private async Task SaveList(PriceListResponse? item)
    {
        using var dialog = new SalesInputForm(
            item is null ? "Sales_NewList" : "Sales_EditList",
            Field.Text("Code", "Grid_Code", item?.Code, true, item is not null),
            Field.Text("Name", "Config_Name", item?.Name, true),
            Field.Text("Currency", "Sales_Currency", item?.Currency ?? "ARS", true),
            Field.Text(
                "From",
                "Sales_ValidFrom",
                item?.ValidFrom?.ToString("d", CultureInfo.CurrentCulture)
            ),
            Field.Text(
                "Until",
                "Sales_ValidUntil",
                item?.ValidUntil?.ToString("d", CultureInfo.CurrentCulture)
            ),
            Field.Choice("Status", "Common_Status", item?.StatusCode ?? "ACTIVO", Statuses())
        );
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;
        if (!TryDate(dialog["From"], out var from) || !TryDate(dialog["Until"], out var until))
        {
            Error(Localization.Text("Sales_InvalidNumber"));
            return;
        }
        await Run(async () =>
            await _api.SavePriceListAsync(
                _session.Token,
                new(
                    item?.PriceListId,
                    dialog["Code"],
                    dialog["Name"],
                    dialog["Currency"],
                    from,
                    until,
                    dialog["Status"],
                    item?.RowVersion
                )
            )
        );
    }

    /// <summary>Inicia la creación de un precio.</summary>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private Task NewPrice() => SavePrice(null);

    /// <summary>Abre el editor correspondiente a un precio y conserva los cambios confirmados.</summary>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private Task EditPrice() => EditSelected<PriceListProductResponse>(_prices, SavePrice);

    /// <summary>Valida y guarda un precio mediante la API.</summary>
    /// <param name="item">Registro existente que se edita, o nulo para crear uno nuevo.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private async Task SavePrice(PriceListProductResponse? item)
    {
        if (_data is null || _data.PriceLists.Count == 0 || _data.Products.Count == 0)
        {
            Error(Localization.Text("Sales_NoSelection"));
            return;
        }
        using var dialog = new SalesInputForm(
            item is null ? "Sales_NewPrice" : "Sales_EditPrice",
            Field.Choice(
                "List",
                "Sales_PriceList",
                (
                    item?.PriceListId
                    ?? Selected<PriceListResponse>(_lists)?.PriceListId
                    ?? _data.PriceLists[0].PriceListId
                ).ToString(CultureInfo.InvariantCulture),
                _data
                    .PriceLists.Select(value => new Choice(
                        value.PriceListId.ToString(CultureInfo.InvariantCulture),
                        $"{value.Code} · {value.Name}"
                    ))
                    .ToList(),
                item is not null
            ),
            Field.Choice(
                "Product",
                "Inventory_Product",
                (item?.ProductId ?? _data.Products[0].ProductId).ToString(
                    CultureInfo.InvariantCulture
                ),
                _data
                    .Products.Select(value => new Choice(
                        value.ProductId.ToString(CultureInfo.InvariantCulture),
                        $"{value.Code} · {value.Name}"
                    ))
                    .ToList(),
                item is not null
            ),
            Field.Text(
                "Price",
                "Sales_UnitPrice",
                item?.UnitPrice.ToString("0.####", CultureInfo.CurrentCulture),
                true
            ),
            Field.Choice("Status", "Common_Status", item?.StatusCode ?? "ACTIVO", Statuses())
        );
        if (
            dialog.ShowDialog(this) != DialogResult.OK
            || !decimal.TryParse(
                dialog["Price"],
                NumberStyles.Number,
                CultureInfo.CurrentCulture,
                out var price
            )
            || !FitsSqlDecimal(price, 4)
        )
        {
            Error(Localization.Text("Sales_InvalidNumber"));
            return;
        }
        await Run(async () =>
            await _api.SavePriceAsync(
                _session.Token,
                new(
                    item?.PriceListProductId,
                    long.Parse(dialog["List"], CultureInfo.InvariantCulture),
                    long.Parse(dialog["Product"], CultureInfo.InvariantCulture),
                    price,
                    dialog["Status"],
                    item?.RowVersion
                )
            )
        );
    }

    /// <summary>Inicia la creación de una promoción.</summary>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private Task NewPromotion() => SavePromotion(null);

    /// <summary>Abre el editor correspondiente a una promoción y conserva los cambios confirmados.</summary>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private Task EditPromotion() => EditSelected<PromotionResponse>(_promotions, SavePromotion);

    /// <summary>Valida y guarda una promoción mediante la API.</summary>
    /// <param name="item">Registro existente que se edita, o nulo para crear uno nuevo.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private async Task SavePromotion(PromotionResponse? item)
    {
        if (_data is null || (_data.Products.Count == 0 && item is null))
        {
            Error(Localization.Text("Sales_PromotionProductRequired"));
            return;
        }
        var productOptions = _data
            .Products.Select(value => new Choice(
                value.ProductId.ToString(CultureInfo.InvariantCulture),
                $"{value.Code} · {value.Name}"
            ))
            .ToList();
        if (
            item is not null
            && productOptions.All(value =>
                value.Value != item.ProductId.ToString(CultureInfo.InvariantCulture)
            )
        )
            productOptions.Add(
                new(
                    item.ProductId.ToString(CultureInfo.InvariantCulture),
                    $"{item.ProductCode} · {item.Product}"
                )
            );
        using var dialog = new SalesInputForm(
            item is null ? "Sales_NewPromotion" : "Sales_EditPromotion",
            Field.Choice(
                "Product",
                "Inventory_Product",
                (item?.ProductId ?? _data.Products[0].ProductId).ToString(
                    CultureInfo.InvariantCulture
                ),
                productOptions,
                item is not null
            ),
            Field.Text("Code", "Grid_Code", item?.Code, true, item is not null),
            Field.Text("Name", "Config_Name", item?.Name, true),
            Field.Text("Description", "Config_Description", item?.Description),
            Field.Choice(
                "Type",
                "Sales_PromotionType",
                item?.PromotionType ?? "CANTIDAD_PAGADA",
                PromotionTypes()
            ),
            Field.Text(
                "Required",
                "Sales_RequiredQuantity",
                item?.RequiredQuantity.ToString("0.####", CultureInfo.CurrentCulture),
                true
            ),
            Field.Text(
                "Paid",
                "Sales_PaidQuantity",
                item?.PaidQuantity?.ToString("0.####", CultureInfo.CurrentCulture)
            ),
            Field.Text(
                "Discounted",
                "Sales_BonusQuantity",
                item?.DiscountedQuantity?.ToString("0.####", CultureInfo.CurrentCulture)
            ),
            Field.Text(
                "Rate",
                "Sales_Discount",
                item?.DiscountRate is null
                    ? null
                    : (item.DiscountRate.Value * 100).ToString("0.####", CultureInfo.CurrentCulture)
            ),
            Field.Text(
                "Package",
                "Sales_PackagePrice",
                item?.PackagePrice?.ToString("0.####", CultureInfo.CurrentCulture)
            ),
            Field.Text(
                "From",
                "Sales_ValidFrom",
                item?.ValidFrom?.ToString("d", CultureInfo.CurrentCulture)
            ),
            Field.Text(
                "Until",
                "Sales_ValidUntil",
                item?.ValidUntil?.ToString("d", CultureInfo.CurrentCulture)
            ),
            Field.Choice("Status", "Common_Status", item?.StatusCode ?? "ACTIVO", Statuses())
        );
        void ToggleFields()
        {
            var type = dialog["Type"];
            dialog.Control("Paid").Enabled = type == "CANTIDAD_PAGADA";
            dialog.Control("Discounted").Enabled = type == "PORCENTAJE_UNIDADES";
            dialog.Control("Rate").Enabled = type == "PORCENTAJE_UNIDADES";
            dialog.Control("Package").Enabled = type == "PRECIO_PAQUETE";
        }
        dialog.Choice("Type").SelectedValueChanged += (_, _) => ToggleFields();
        ToggleFields();
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;
        if (
            !decimal.TryParse(
                dialog["Required"],
                NumberStyles.Number,
                CultureInfo.CurrentCulture,
                out var required
            )
            || !TryOptionalDecimal(dialog["Paid"], out var paid)
            || !TryOptionalDecimal(dialog["Discounted"], out var discounted)
            || !TryOptionalDecimal(dialog["Rate"], out var rate)
            || !TryOptionalDecimal(dialog["Package"], out var package)
            || !TryDate(dialog["From"], out var from)
            || !TryDate(dialog["Until"], out var until)
            || until < from
            || !FitsSqlDecimal(required, 4)
            || paid is not null && !FitsSqlDecimal(paid.Value, 4)
            || discounted is not null && !FitsSqlDecimal(discounted.Value, 4)
            || package is not null && !FitsSqlDecimal(package.Value, 4)
            || rate is not null && !FitsSqlDecimal(rate.Value / 100, 6)
        )
        {
            Error(Localization.Text("Sales_PromotionInvalid"));
            return;
        }
        var type = dialog["Type"];
        var valid =
            required > 0
            && (
                type switch
                {
                    "CANTIDAD_PAGADA" => paid is >= 0 && paid < required,
                    "PORCENTAJE_UNIDADES" => discounted is > 0
                        && discounted <= required
                        && rate is > 0 and <= 100,
                    "PRECIO_PAQUETE" => package is >= 0,
                    _ => false,
                }
            );
        if (!valid)
        {
            Error(Localization.Text("Sales_PromotionInvalid"));
            return;
        }
        await Run(async () =>
            await _api.SavePromotionAsync(
                _session.Token,
                new(
                    item?.PromotionId,
                    long.Parse(dialog["Product"], CultureInfo.InvariantCulture),
                    dialog["Code"],
                    dialog["Name"],
                    dialog["Description"],
                    type,
                    required,
                    type == "CANTIDAD_PAGADA" ? paid : null,
                    type == "PORCENTAJE_UNIDADES" ? discounted : null,
                    type == "PORCENTAJE_UNIDADES" ? rate / 100 : null,
                    type == "PRECIO_PAQUETE" ? package : null,
                    from,
                    until,
                    dialog["Status"],
                    item?.RowVersion
                )
            )
        );
    }

    /// <summary>Inicia la creación de un pedido.</summary>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private Task NewOrder() => SaveOrder(null);

    /// <summary>Abre el editor correspondiente a un pedido y conserva los cambios confirmados.</summary>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private Task EditOrder() => EditSelected<OrderSummary>(_orders, SaveOrder);

    /// <summary>Valida y guarda un pedido mediante la API.</summary>
    /// <param name="item">Registro existente que se edita, o nulo para crear uno nuevo.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private async Task SaveOrder(OrderSummary? item)
    {
        if (_data is null || (item is not null && item.StatusCode != "BORRADOR"))
        {
            Error(Localization.Text("Sales_NoSelection"));
            return;
        }
        if (
            _data.Clients.Count == 0
            || _data.PriceLists.All(value => value.StatusCode != "ACTIVO")
            || _data.Products.Count == 0
        )
        {
            Error(Localization.Text("Sales_SetupRequired"));
            return;
        }
        using var dialog = new OrderEditForm(
            _data,
            item,
            item is null ? [] : _data.Orders.Where(line => line.OrderId == item.OrderId).ToList()
        );
        if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Request is null)
            return;
        await Run(async () => await _api.SaveOrderAsync(_session.Token, dialog.Request));
    }

    /// <summary>Confirma un pedido después de validar su estado.</summary>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private Task ConfirmOrder() =>
        Transition(
            "BORRADOR",
            "Sales_ConfirmQuestion",
            (item, request) => _api.ConfirmOrderAsync(_session.Token, item.OrderId, request)
        );

    /// <summary>Cancela un pedido conservando su trazabilidad.</summary>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private Task CancelOrder() =>
        Transition(
            null,
            "Sales_CancelQuestion",
            (item, request) => _api.CancelOrderAsync(_session.Token, item.OrderId, request)
        );

    /// <summary>Crea una venta con la información actual.</summary>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private async Task CreateSale()
    {
        var item = Selected<OrderSummary>(_orders);
        if (item is null || item.StatusCode != "CONFIRMADO")
        {
            Error(Localization.Text("Sales_NoSelection"));
            return;
        }
        if (
            MessageBox.Show(
                this,
                Localization.Text("Sales_SaleQuestion"),
                "OxiTigre",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            ) != DialogResult.Yes
        )
            return;
        await Run(async () =>
            await _api.CreateSaleAsync(
                _session.Token,
                item.OrderId,
                new(DateTime.UtcNow, item.RowVersion)
            )
        );
    }

    /// <summary>Ejecuta una transición válida sobre el pedido seleccionado.</summary>
    /// <param name="required">Estado requerido para habilitar la transición, o nulo cuando no se restringe.</param>
    /// <param name="question">Pregunta de confirmación presentada al usuario.</param>
    /// <param name="action">Operación que se ejecuta cuando el usuario confirma la acción.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private async Task Transition(
        string? required,
        string question,
        Func<OrderSummary, OrderTransitionRequest, Task> action
    )
    {
        var item = Selected<OrderSummary>(_orders);
        if (
            item is null
            || (required is not null && item.StatusCode != required)
            || (required is null && item.StatusCode is not ("BORRADOR" or "CONFIRMADO"))
        )
        {
            Error(Localization.Text("Sales_NoSelection"));
            return;
        }
        if (
            MessageBox.Show(
                this,
                Localization.Text(question),
                "OxiTigre",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            ) != DialogResult.Yes
        )
            return;
        await Run(async () =>
        {
            await action(item, new(item.RowVersion));
            return new SavedCommercialResponse(item.OrderId);
        });
    }

    /// <summary>Ejecuta una operación, informa el resultado y traduce errores funcionales.</summary>
    /// <param name="action">Operación que se ejecuta cuando el usuario confirma la acción.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private async Task Run(Func<Task<SavedCommercialResponse>> action)
    {
        try
        {
            await action();
            MessageBox.Show(
                this,
                Localization.Text("Sales_Saved"),
                "OxiTigre",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
            await LoadAsync();
        }
        catch (HttpRequestException exception)
        {
            Error(exception.Message);
        }
    }

    /// <summary>Crea una pestaña localizada y agrega el contenido indicado.</summary>
    /// <param name="key">Clave de recurso o campo que identifica el texto o control.</param>
    /// <param name="content">Control visual que se incorpora a la pestaña o sección.</param>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private static TabPage Tab(string key, Control content)
    {
        var tab = new TabPage(Localization.Text(key))
        {
            Padding = new Padding(8),
            BackColor = UiTheme.Background,
        };
        tab.Controls.Add(content);
        return tab;
    }

    /// <summary>Crea una barra de acciones coherente con el módulo de pedidos y ventas.</summary>
    /// <param name="key">Clave de recurso o campo que identifica el texto o control.</param>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private static FlowLayoutPanel TitleBar(string key)
    {
        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 48,
            Padding = new Padding(8),
        };
        bar.Controls.Add(
            new Label
            {
                Text = Localization.Text(key),
                AutoSize = true,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = UiTheme.Primary,
                Margin = new Padding(0, 6, 12, 0),
            }
        );
        return bar;
    }

    /// <summary>Crea una ayuda contextual localizada para la sección.</summary>
    /// <param name="key">Clave de recurso o campo que identifica el texto o control.</param>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private static Label InformationBar(string key) =>
        new()
        {
            Text = Localization.Text(key),
            Dock = DockStyle.Top,
            Height = 46,
            Padding = new Padding(10, 10, 10, 6),
            BackColor = Color.FromArgb(230, 244, 234),
            ForeColor = UiTheme.Primary,
            AutoEllipsis = true,
        };

    /// <summary>Crea una barra de acciones coherente con el módulo de pedidos y ventas.</summary>
    /// <param name="title">Título visible de la ventana, pestaña o sección.</param>
    /// <param name="actions">Acciones disponibles en la barra de herramientas.</param>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private static FlowLayoutPanel Toolbar(
        string title,
        params (string Key, bool Enabled, Func<Task> Action)[] actions
    )
    {
        var bar = TitleBar(title);
        foreach (var action in actions)
        {
            var button = UiTheme.Button(Localization.Text(action.Key), 0, 0, 155);
            button.Enabled = action.Enabled;
            button.Click += async (_, _) => await action.Action();
            bar.Controls.Add(button);
        }
        return bar;
    }

    /// <summary>Crea una grilla con el estilo y comportamiento común del módulo de pedidos y ventas.</summary>
    /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
    private static DataGridView Grid()
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AutoGenerateColumns = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        };
        grid.CellFormatting += (_, args) =>
        {
            if (
                args.ColumnIndex >= 0
                && grid.Columns[args.ColumnIndex].Name == "DiscountRate"
                && args.Value is decimal rate
            )
            {
                args.Value = rate * 100;
                return;
            }
            args.Value = args.Value switch
            {
                "ACTIVO" => Localization.Text("Common_Active"),
                "INACTIVO" => Localization.Text("Common_Inactive"),
                "BORRADOR" => Localization.Text("Sales_Draft"),
                "CONFIRMADO" => Localization.Text("Sales_Reserved"),
                "CANCELADO" => Localization.Text("Sales_Cancelled"),
                "VENDIDO" => Localization.Text("Sales_Sold"),
                "CONFIRMADA" => Localization.Text("Common_Confirmed"),
                "CANTIDAD_PAGADA" => Localization.Text("Sales_PromotionQuantityPaid"),
                "PORCENTAJE_UNIDADES" => Localization.Text("Sales_PromotionPercentageUnits"),
                "PRECIO_PAQUETE" => Localization.Text("Sales_PromotionPackagePrice"),
                "CLIENTE_SERVICIO" => Localization.Text("Sales_ClientService"),
                "VENTA_ACTIVO" => Localization.Text("Sales_AssetSale"),
                "PRESTAMO" => Localization.Text("Sales_AssetLoan"),
                "INTERCAMBIO" => Localization.Text("Sales_AssetExchange"),
                "RETIRO_CLIENTE" => Localization.Text("Sales_ClientPickup"),
                "ENTREGA_OXITIGRE" => Localization.Text("Sales_OxiTigreDelivery"),
                "NO_APLICA" => Localization.Text("Sales_NotApplicable"),
                _ => args.Value,
            };
        };
        UiTheme.Grid(grid);
        return grid;
    }

    /// <summary>Actualiza las grillas y detalles de pedidos y ventas con la información cargada.</summary>
    /// <param name="grid">Grilla que se configura, consulta o actualiza.</param>
    /// <param name="values">Valores que se vinculan con el control.</param>
    /// <param name="hidden">Nombres de propiedades técnicas que no deben mostrarse.</param>
    private static void Bind<T>(DataGridView grid, IReadOnlyList<T> values, params string[] hidden)
    {
        grid.DataSource = null;
        grid.DataSource = values.ToList();
        foreach (var name in hidden)
            if (grid.Columns[name] is { } column)
                column.Visible = false;
        foreach (DataGridViewColumn column in grid.Columns)
        {
            column.HeaderText = Header(column.Name);
            if (
                column.Name
                is "Total"
                    or "LineTotal"
                    or "UnitPrice"
                    or "DiscountAmount"
                    or "PackagePrice"
            )
                column.DefaultCellStyle.Format = "N2";
            if (column.Name is "OrderDate" or "SaleDate")
                column.DefaultCellStyle.Format = "g";
        }
    }

    /// <summary>Resuelve el encabezado localizado de una columna o propiedad.</summary>
    /// <param name="name">Nombre técnico del elemento solicitado.</param>
    /// <returns>Texto normalizado, localizado o formateado por la operación.</returns>
    private static string Header(string name) =>
        name switch
        {
            "Code" or "OrderCode" or "SaleCode" or "ProductCode" or "AssetCode" =>
                Localization.Text("Grid_Code"),
            "Name" => Localization.Text("Config_Name"),
            "Description" or "Observation" => Localization.Text("Config_Description"),
            "Client" => Localization.Text("Sales_Client"),
            "Currency" => Localization.Text("Sales_Currency"),
            "Total" or "LineTotal" => Localization.Text("Sales_Total"),
            "StatusCode" => Localization.Text("Common_Status"),
            "OrderDate" or "SaleDate" => Localization.Text("Inventory_Date"),
            "OrderId" => Localization.Text("Sales_Order"),
            "Product" => Localization.Text("Inventory_Product"),
            "ProductLine" => Localization.Text("Sales_ChargedLine"),
            "Warehouse" => Localization.Text("Inventory_Warehouse"),
            "Quantity" or "RequiredQuantity" => Localization.Text(
                name == "Quantity" ? "Inventory_Quantity" : "Sales_RequiredQuantity"
            ),
            "PaidQuantity" => Localization.Text("Sales_PaidQuantity"),
            "DiscountedQuantity" => Localization.Text("Sales_BonusQuantity"),
            "UnitSymbol" => Localization.Text("Inventory_Unit"),
            "UnitPrice" => Localization.Text("Sales_UnitPrice"),
            "PromotionName" => Localization.Text("Sales_Promotion"),
            "PromotionType" => Localization.Text("Sales_PromotionType"),
            "PackagePrice" => Localization.Text("Sales_PackagePrice"),
            "DiscountAmount" => Localization.Text("Sales_DiscountAmount"),
            "DiscountRate" or "DiscountPercent" => Localization.Text("Sales_Discount"),
            "TaxPercent" => Localization.Text("Sales_Tax"),
            "ValidFrom" => Localization.Text("Sales_ValidFrom"),
            "ValidUntil" => Localization.Text("Sales_ValidUntil"),
            "SerialNumber" => Localization.Text("Column_Serial"),
            "LinkType" => Localization.Text("Sales_LinkType"),
            "InboundMode" => Localization.Text("Sales_InboundMode"),
            "ReturnMode" => Localization.Text("Sales_ReturnMode"),
            "ExpectedReturnDate" => Localization.Text("Sales_ExpectedReturn"),
            _ => name,
        };

    /// <summary>Obtiene el registro actualmente seleccionado en la grilla.</summary>
    /// <param name="grid">Grilla que se configura, consulta o actualiza.</param>
    /// <returns>Objeto construido u obtenido por la operación.</returns>
    private static T? Selected<T>(DataGridView grid)
        where T : class => grid.CurrentRow?.DataBoundItem as T;

    /// <summary>Devuelve los estados disponibles para listas, precios y promociones.</summary>
    /// <returns>Colección de registros u opciones obtenida por la operación.</returns>
    private static IReadOnlyList<Choice> Statuses() =>
        [
            new("ACTIVO", Localization.Text("Common_Active")),
            new("INACTIVO", Localization.Text("Common_Inactive")),
        ];

    /// <summary>Devuelve los tipos de promoción admitidos por el cálculo comercial.</summary>
    /// <returns>Colección de registros u opciones obtenida por la operación.</returns>
    private static IReadOnlyList<Choice> PromotionTypes() =>
        [
            new("CANTIDAD_PAGADA", Localization.Text("Sales_PromotionQuantityPaid")),
            new("PORCENTAJE_UNIDADES", Localization.Text("Sales_PromotionPercentageUnits")),
            new("PRECIO_PAQUETE", Localization.Text("Sales_PromotionPackagePrice")),
        ];

    /// <summary>Intenta interpretar una fecha opcional con la cultura activa.</summary>
    /// <param name="value">Texto que se normaliza, valida o asigna.</param>
    /// <param name="result">Variable de salida que recibe el valor interpretado.</param>
    /// <returns>Verdadero cuando se cumple la condición evaluada; en caso contrario, falso.</returns>
    private static bool TryDate(string value, out DateOnly? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(value))
            return true;
        if (
            !DateOnly.TryParse(
                value,
                CultureInfo.CurrentCulture,
                DateTimeStyles.None,
                out var parsed
            )
        )
            return false;
        result = parsed;
        return true;
    }

    /// <summary>Intenta interpretar un importe decimal opcional con la cultura activa.</summary>
    /// <param name="value">Texto que se normaliza, valida o asigna.</param>
    /// <param name="result">Variable de salida que recibe el valor interpretado.</param>
    /// <returns>Verdadero cuando se cumple la condición evaluada; en caso contrario, falso.</returns>
    private static bool TryOptionalDecimal(string value, out decimal? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(value))
            return true;
        if (
            !decimal.TryParse(
                value,
                NumberStyles.Number,
                CultureInfo.CurrentCulture,
                out var parsed
            )
        )
            return false;
        result = parsed;
        return true;
    }

    /// <summary>Indica si un decimal cabe en la precisión y escala utilizadas por SQL Server.</summary>
    /// <param name="value">Valor que se convierte o asigna al destino correspondiente.</param>
    /// <param name="scale">Cantidad de decimales reservada por SQL Server.</param>
    /// <returns>Verdadero cuando un decimal cabe en la precisión y escala utilizadas por sql server; en caso contrario, falso.</returns>
    private static bool FitsSqlDecimal(decimal value, int scale) =>
        value >= 0 && value <= SqlDecimal19_4Max && decimal.Round(value, scale) == value;

    /// <summary>Abre el editor correspondiente a el registro seleccionado y conserva los cambios confirmados.</summary>
    /// <param name="grid">Grilla que se configura, consulta o actualiza.</param>
    /// <param name="edit">Operación usada para editar el registro seleccionado.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private Task EditSelected<T>(DataGridView grid, Func<T, Task> edit)
        where T : class
    {
        var item = Selected<T>(grid);
        if (item is not null)
            return edit(item);
        Error(Localization.Text("Common_SelectRecord"));
        return Task.CompletedTask;
    }

    /// <summary>Muestra un error funcional sin exponer detalles técnicos.</summary>
    /// <param name="message">Mensaje funcional que se muestra al usuario.</param>
    private void Error(string message) =>
        MessageBox.Show(this, message, "OxiTigre", MessageBoxButtons.OK, MessageBoxIcon.Error);

    private sealed record OrderSummary(
        long OrderId,
        long ClientId,
        long? PriceListId,
        string OrderCode,
        DateTime OrderDate,
        string Client,
        string Currency,
        decimal Total,
        string StatusCode,
        string? Observation,
        string RowVersion
    );

    private sealed record SaleSummary(
        long SaleId,
        string SaleCode,
        long OrderId,
        DateTime SaleDate,
        string Client,
        string Currency,
        decimal Total,
        string StatusCode,
        long? MovementId
    );

    private sealed record OrderLineView(
        string ProductCode,
        string Product,
        string? Warehouse,
        decimal Quantity,
        string UnitSymbol,
        decimal UnitPrice,
        string? PromotionName,
        decimal DiscountAmount,
        decimal DiscountPercent,
        decimal TaxPercent,
        decimal LineTotal
    );

    private sealed record SaleLineView(
        string ProductCode,
        string Product,
        string? Warehouse,
        decimal Quantity,
        string UnitSymbol,
        decimal UnitPrice,
        string? PromotionName,
        decimal DiscountAmount,
        decimal DiscountPercent,
        decimal TaxPercent,
        decimal LineTotal
    );

    internal sealed record Choice(string Value, string Label);

    private sealed record Field(
        string Key,
        string LabelKey,
        string Value,
        bool Required,
        bool ReadOnly,
        IReadOnlyList<Choice>? Choices
    )
    {
        /// <summary>Crea la definición de un campo de texto para el formulario.</summary>
        /// <param name="key">Clave de recurso o campo que identifica el texto o control.</param>
        /// <param name="label">Etiqueta visible del campo.</param>
        /// <param name="value">Texto que se normaliza, valida o asigna.</param>
        /// <param name="required">Indica si el campo debe completarse para guardar.</param>
        /// <param name="readOnly">Indica si el usuario puede modificar el campo.</param>
        /// <returns>Objeto construido u obtenido por la operación.</returns>
        internal static Field Text(
            string key,
            string label,
            string? value,
            bool required = false,
            bool readOnly = false
        ) => new(key, label, value ?? string.Empty, required, readOnly, null);

        /// <summary>Crea o recupera un campo de selección configurado con sus opciones.</summary>
        /// <param name="key">Clave de recurso o campo que identifica el texto o control.</param>
        /// <param name="label">Etiqueta visible del campo.</param>
        /// <param name="value">Texto que se normaliza, valida o asigna.</param>
        /// <param name="choices">Opciones visibles admitidas por el campo.</param>
        /// <param name="readOnly">Indica si el usuario puede modificar el campo.</param>
        /// <param name="required">Indica si el campo debe completarse para guardar.</param>
        /// <returns>Objeto construido u obtenido por la operación.</returns>
        internal static Field Choice(
            string key,
            string label,
            string value,
            IReadOnlyList<SalesManagementForm.Choice> choices,
            bool readOnly = false,
            bool required = true
        ) => new(key, label, value, required, readOnly, choices);
    }

    private sealed class SalesInputForm : Form
    {
        private readonly IReadOnlyList<Field> _fields;
        private readonly Dictionary<string, Control> _controls = [];
        private readonly ToolTip _tips = new();
        internal string this[string key] =>
            _controls[key] is ComboBox combo
                ? Convert.ToString(combo.SelectedValue, CultureInfo.InvariantCulture)
                    ?? string.Empty
                : ((TextBox)_controls[key]).Text.Trim();

        /// <summary>Crea o recupera un campo de selección configurado con sus opciones.</summary>
        /// <param name="key">Clave de recurso o campo que identifica el texto o control.</param>
        /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
        internal ComboBox Choice(string key) => (ComboBox)_controls[key];

        /// <summary>Obtiene el control asociado a la clave de campo indicada.</summary>
        /// <param name="key">Clave de recurso o campo que identifica el texto o control.</param>
        /// <returns>Control visual configurado y listo para incorporarse a la pantalla.</returns>
        internal Control Control(string key) => _controls[key];

        /// <summary>Asigna tool tip en el control correspondiente.</summary>
        /// <param name="key">Clave de recurso o campo que identifica el texto o control.</param>
        /// <param name="value">Texto que se normaliza, valida o asigna.</param>
        internal void SetToolTip(string key, string value) =>
            _tips.SetToolTip(_controls[key], value);

        /// <summary>Asigna text en el control correspondiente.</summary>
        /// <param name="key">Clave de recurso o campo que identifica el texto o control.</param>
        /// <param name="value">Texto que se normaliza, valida o asigna.</param>
        internal void SetText(string key, string value) => ((TextBox)_controls[key]).Text = value;

        /// <summary>Inicializa el componente de pedidos y ventas con sus dependencias y datos de trabajo.</summary>
        /// <param name="titleKey">Clave de recurso usada como título visible.</param>
        /// <param name="fields">Campos que componen el editor.</param>
        internal SalesInputForm(string titleKey, params Field[] fields)
        {
            _fields = fields;
            Text = Localization.Text(titleKey);
            Width = 650;
            Height = Math.Min(700, 150 + fields.Length * 48);
            MinimumSize = new Size(560, 300);
            StartPosition = FormStartPosition.CenterParent;
            UiTheme.Apply(this);
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(18),
                ColumnCount = 2,
                RowCount = fields.Length + 1,
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (var index = 0; index < fields.Length; index++)
            {
                var field = fields[index];
                Control control;
                if (field.Choices is not null)
                {
                    var combo = new ComboBox
                    {
                        Dock = DockStyle.Fill,
                        DropDownStyle = ComboBoxStyle.DropDownList,
                        DataSource = field.Choices.ToList(),
                        DisplayMember = "Label",
                        ValueMember = "Value",
                        Enabled = !field.ReadOnly,
                    };
                    combo.SelectedValue = field.Value;
                    control = combo;
                }
                else
                    control = new TextBox
                    {
                        Dock = DockStyle.Fill,
                        Text = field.Value,
                        ReadOnly = field.ReadOnly,
                        PlaceholderText = Localization.Text(field.LabelKey),
                    };
                _controls[field.Key] = control;
                layout.Controls.Add(
                    new Label
                    {
                        Text = Localization.Text(field.LabelKey),
                        AutoSize = true,
                        Anchor = AnchorStyles.Left,
                    },
                    0,
                    index
                );
                layout.Controls.Add(control, 1, index);
            }
            var save = UiTheme.Button(Localization.Text("Common_Save"), 0, 0, 130);
            save.Click += (_, _) =>
            {
                if (
                    _fields.Any(field =>
                        field.Required && string.IsNullOrWhiteSpace(this[field.Key])
                    )
                )
                {
                    MessageBox.Show(
                        this,
                        Localization.Text("Common_Required"),
                        "OxiTigre",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    return;
                }
                DialogResult = DialogResult.OK;
                Close();
            };
            layout.Controls.Add(save, 1, fields.Length);
            var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            scroll.Controls.Add(layout);
            Controls.Add(scroll);
            AcceptButton = save;
        }
    }

    private sealed class OrderEditForm : Form
    {
        private readonly SalesSnapshotResponse _data;
        private readonly OrderSummary? _item;
        private readonly ComboBox _client =
                new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 260 },
            _list = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 240 };
        private readonly DateTimePicker _date = new()
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd/MM/yyyy HH:mm",
            Width = 170,
        };
        private readonly TextBox _currency =
                new()
                {
                    Width = 60,
                    Text = "ARS",
                    ReadOnly = true,
                },
            _observation = new() { Width = 300 };
        private readonly DataGridView _grid = Grid(),
            _assetGrid = Grid();
        private readonly List<OrderDraftLine> _lines;
        private readonly List<OrderAssetDraft> _assets;
        internal SaveOrderRequest? Request { get; private set; }

        /// <summary>Inicializa el componente de pedidos y ventas con sus dependencias y datos de trabajo.</summary>
        /// <param name="data">Datos cargados que alimentan la pantalla.</param>
        /// <param name="item">Registro existente que se edita, o nulo para crear uno nuevo.</param>
        /// <param name="lines">Renglones actuales del documento.</param>
        internal OrderEditForm(
            SalesSnapshotResponse data,
            OrderSummary? item,
            IReadOnlyList<OrderLineResponse> lines
        )
        {
            _data = data;
            _item = item;
            _lines = lines
                .Select(line => new OrderDraftLine(
                    line.ProductId,
                    line.ProductCode,
                    line.Product,
                    line.WarehouseId,
                    line.Warehouse,
                    line.Quantity,
                    line.UnitSymbol,
                    line.UnitPrice,
                    line.PromotionId,
                    line.PromotionName,
                    line.DiscountAmount,
                    line.DiscountRate * 100,
                    line.TaxRate * 100,
                    line.LineTotal
                ))
                .ToList();
            _assets = item is null
                ? []
                : data
                    .OrderAssets.Where(value => value.OrderId == item.OrderId)
                    .Select(value => new OrderAssetDraft(
                        value.AssetId,
                        value.AssetCode,
                        value.SerialNumber,
                        value.ProductLineId,
                        value.ProductLine,
                        value.LinkType,
                        value.InboundMode,
                        value.ReturnMode,
                        value.ExpectedReturnDate,
                        value.Observation
                    ))
                    .ToList();
            Text = Localization.Text(item is null ? "Sales_NewOrder" : "Sales_EditOrder");
            Width = 1120;
            Height = 700;
            MinimumSize = new Size(900, 580);
            StartPosition = FormStartPosition.CenterParent;
            UiTheme.Apply(this);
            _client.DataSource = data
                .Clients.Select(value => new Choice(
                    value.ClientId.ToString(CultureInfo.InvariantCulture),
                    $"{value.Code} · {value.Name}"
                ))
                .ToList();
            _client.DisplayMember = "Label";
            _client.ValueMember = "Value";
            if (item is not null)
            {
                _client.SelectedValue = item.ClientId.ToString(CultureInfo.InvariantCulture);
                _date.Value = item.OrderDate;
                _currency.Text = item.Currency;
                _observation.Text = item.Observation ?? string.Empty;
            }
            void RefreshLists()
            {
                var previous = Convert.ToString(_list.SelectedValue, CultureInfo.InvariantCulture);
                var orderDate = DateOnly.FromDateTime(_date.Value.ToUniversalTime());
                var options = data
                    .PriceLists.Where(value =>
                        value.PriceListId == item?.PriceListId
                        || value.StatusCode == "ACTIVO"
                            && (value.ValidFrom is null || value.ValidFrom <= orderDate)
                            && (value.ValidUntil is null || value.ValidUntil >= orderDate)
                    )
                    .Select(value => new Choice(
                        value.PriceListId.ToString(CultureInfo.InvariantCulture),
                        $"{value.Code} · {value.Name}"
                    ))
                    .ToList();
                _list.DataSource = options;
                _list.DisplayMember = nameof(Choice.Label);
                _list.ValueMember = nameof(Choice.Value);
                var target = previous ?? item?.PriceListId?.ToString(CultureInfo.InvariantCulture);
                if (target is not null && options.Any(value => value.Value == target))
                    _list.SelectedValue = target;
                UpdateCurrency();
            }
            RefreshLists();
            _list.SelectedValueChanged += (_, _) => UpdateCurrency();
            _date.ValueChanged += (_, _) => RefreshLists();
            var header = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 70,
                Padding = new Padding(12),
            };
            Add(header, "Sales_Client", _client);
            Add(header, "Sales_PriceList", _list);
            Add(header, "Inventory_Date", _date);
            Add(header, "Sales_Currency", _currency);
            Add(header, "Inventory_Observation", _observation);
            var tools = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 52,
                Padding = new Padding(12, 6, 0, 0),
            };
            var add = UiTheme.Button(Localization.Text("Sales_AddLine"), 0, 0, 150);
            add.Click += (_, _) => AddLine();
            var remove = UiTheme.Button(Localization.Text("Sales_RemoveLine"), 0, 0, 150);
            remove.Click += (_, _) =>
            {
                if (_grid.CurrentRow?.DataBoundItem is OrderDraftLine line)
                {
                    _lines.Remove(line);
                    if (_lines.All(value => value.ProductId != line.ProductId))
                        _assets.RemoveAll(value => value.ProductLineId == line.ProductId);
                    BindLines();
                }
            };
            var save = UiTheme.Button(Localization.Text("Common_Save"), 0, 0, 140);
            save.Click += (_, _) => Save();
            tools.Controls.AddRange([add, remove, save]);
            var assetTools = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 46,
                Padding = new Padding(0, 4, 0, 0),
            };
            var addAsset = UiTheme.Button(Localization.Text("Sales_LinkAsset"), 0, 0, 180);
            addAsset.Click += (_, _) => AddAsset();
            var removeAsset = UiTheme.Button(Localization.Text("Sales_UnlinkAsset"), 0, 0, 180);
            removeAsset.Click += (_, _) =>
            {
                if (_assetGrid.CurrentRow?.DataBoundItem is OrderAssetDraft asset)
                {
                    _assets.Remove(asset);
                    BindLines();
                }
            };
            assetTools.Controls.AddRange([addAsset, removeAsset]);
            var assetPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 6, 0, 0) };
            assetPanel.Controls.Add(_assetGrid);
            assetPanel.Controls.Add(assetTools);
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 250,
            };
            split.Panel1.Controls.Add(_grid);
            split.Panel2.Controls.Add(assetPanel);
            var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
            body.Controls.Add(split);
            Controls.Add(body);
            Controls.Add(tools);
            Controls.Add(header);
            BindLines();
        }

        /// <summary>Agrega line al documento o control actual.</summary>
        private void AddLine()
        {
            if (_data.Products.Count == 0 || _list.SelectedValue is null)
            {
                ShowError(Localization.Text("Sales_NoSelection"));
                return;
            }
            var warehouseChoices = new List<Choice>
            {
                new(string.Empty, Localization.Text("Sales_NotApplicable")),
            };
            warehouseChoices.AddRange(
                _data.Warehouses.Select(value => new Choice(
                    value.WarehouseId.ToString(CultureInfo.InvariantCulture),
                    value.Name
                ))
            );
            using var dialog = new SalesInputForm(
                "Sales_AddLine",
                Field.Choice(
                    "Product",
                    "Inventory_Product",
                    _data.Products[0].ProductId.ToString(CultureInfo.InvariantCulture),
                    _data
                        .Products.Select(value => new Choice(
                            value.ProductId.ToString(CultureInfo.InvariantCulture),
                            $"{value.Code} · {value.Name}"
                        ))
                        .ToList()
                ),
                Field.Choice(
                    "Warehouse",
                    "Inventory_Warehouse",
                    string.Empty,
                    warehouseChoices,
                    required: false
                ),
                Field.Text("Quantity", "Inventory_Quantity", "1", true),
                Field.Text("Price", "Sales_UnitPrice", "", true),
                Field.Choice(
                    "Promotion",
                    "Sales_Promotion",
                    string.Empty,
                    [new(string.Empty, Localization.Text("Sales_NoPromotion"))],
                    required: false
                ),
                Field.Text("Discount", "Sales_Discount", "0", true),
                Field.Text("Tax", "Sales_Tax", "21", true)
            );
            void RefreshProductOptions()
            {
                if (
                    !long.TryParse(dialog["Product"], CultureInfo.InvariantCulture, out var product)
                    || !long.TryParse(
                        Convert.ToString(_list.SelectedValue, CultureInfo.InvariantCulture),
                        CultureInfo.InvariantCulture,
                        out var list
                    )
                )
                    return;
                var price = _data
                    .Prices.FirstOrDefault(value =>
                        value.PriceListId == list
                        && value.ProductId == product
                        && value.StatusCode == "ACTIVO"
                    )
                    ?.UnitPrice;
                dialog.SetText(
                    "Price",
                    price?.ToString("0.####", CultureInfo.CurrentCulture) ?? string.Empty
                );
                var productItem = _data.Products.First(value => value.ProductId == product);
                var warehouse = dialog.Choice("Warehouse");
                warehouse.Enabled = productItem.ItemType == "PRODUCTO";
                if (productItem.ItemType == "SERVICIO")
                    warehouse.SelectedValue = string.Empty;
                else if (
                    string.IsNullOrWhiteSpace(dialog["Warehouse"])
                    && _data.Warehouses.Count > 0
                )
                    warehouse.SelectedValue = _data
                        .Warehouses[0]
                        .WarehouseId.ToString(CultureInfo.InvariantCulture);
                var orderDate = DateOnly.FromDateTime(_date.Value.ToUniversalTime());
                var choices = new List<Choice>
                {
                    new(string.Empty, Localization.Text("Sales_NoPromotion")),
                };
                choices.AddRange(
                    _data
                        .Promotions.Where(value =>
                            value.ProductId == product
                            && value.StatusCode == "ACTIVO"
                            && (value.ValidFrom is null || value.ValidFrom <= orderDate)
                            && (value.ValidUntil is null || value.ValidUntil >= orderDate)
                        )
                        .Select(value => new Choice(
                            value.PromotionId.ToString(CultureInfo.InvariantCulture),
                            $"{value.Code} · {value.Name}"
                        ))
                );
                var combo = dialog.Choice("Promotion");
                combo.DataSource = choices;
                combo.DisplayMember = nameof(Choice.Label);
                combo.ValueMember = nameof(Choice.Value);
            }
            void ToggleManualDiscount()
            {
                var hasPromotion = !string.IsNullOrWhiteSpace(dialog["Promotion"]);
                dialog.Control("Discount").Enabled = !hasPromotion;
                if (hasPromotion)
                    dialog.SetText("Discount", "0");
            }
            dialog.Choice("Product").SelectedValueChanged += (_, _) => RefreshProductOptions();
            dialog.Choice("Promotion").SelectedValueChanged += (_, _) => ToggleManualDiscount();
            dialog.SetToolTip("Promotion", Localization.Text("Sales_PromotionReplacesManual"));
            RefreshProductOptions();
            ToggleManualDiscount();
            if (
                dialog.ShowDialog(this) != DialogResult.OK
                || !decimal.TryParse(
                    dialog["Quantity"],
                    NumberStyles.Number,
                    CultureInfo.CurrentCulture,
                    out var quantity
                )
                || !decimal.TryParse(
                    dialog["Price"],
                    NumberStyles.Number,
                    CultureInfo.CurrentCulture,
                    out var priceValue
                )
                || !decimal.TryParse(
                    dialog["Discount"],
                    NumberStyles.Number,
                    CultureInfo.CurrentCulture,
                    out var discount
                )
                || !decimal.TryParse(
                    dialog["Tax"],
                    NumberStyles.Number,
                    CultureInfo.CurrentCulture,
                    out var tax
                )
                || quantity <= 0
                || discount is < 0 or > 100
                || tax is < 0 or > 100
                || !FitsSqlDecimal(quantity, 4)
                || !FitsSqlDecimal(priceValue, 4)
                || !FitsSqlDecimal(discount / 100, 6)
                || !FitsSqlDecimal(tax / 100, 6)
                || priceValue > SqlDecimal19_4Max / quantity
            )
            {
                ShowError(Localization.Text("Sales_InvalidNumber"));
                return;
            }
            var productItem = _data.Products.First(value =>
                value.ProductId == long.Parse(dialog["Product"], CultureInfo.InvariantCulture)
            );
            long? warehouseId = long.TryParse(
                dialog["Warehouse"],
                CultureInfo.InvariantCulture,
                out var parsedWarehouse
            )
                ? parsedWarehouse
                : null;
            if (productItem.ItemType == "PRODUCTO" && warehouseId is null)
            {
                ShowError(Localization.Text("Sales_ProductWarehouseRequired"));
                return;
            }
            var warehouseName = warehouseId is null
                ? null
                : _data.Warehouses.First(value => value.WarehouseId == warehouseId).Name;
            long? promotionId = long.TryParse(
                dialog["Promotion"],
                CultureInfo.InvariantCulture,
                out var parsedPromotion
            )
                ? parsedPromotion
                : null;
            var promotion = promotionId is null
                ? null
                : _data.Promotions.First(value => value.PromotionId == promotionId);
            if (promotion is not null)
                discount = 0;
            var existingIndex = _lines.FindIndex(line =>
                line.ProductId == productItem.ProductId
                && line.WarehouseId == warehouseId
                && line.UnitPrice == priceValue
                && line.TaxPercent == tax
                && line.PromotionId == promotionId
                && (promotionId is not null || line.DiscountPercent == discount)
            );
            if (existingIndex >= 0)
                quantity += _lines[existingIndex].Quantity;
            var subtotal = RoundMoney(quantity * priceValue);
            var discountAmount = PromotionDiscount(promotion, quantity, priceValue, discount);
            var effectiveDiscount = promotion is null
                ? discount
                : (subtotal == 0 ? 0 : discountAmount / subtotal * 100);
            var total = RoundMoney((subtotal - discountAmount) * (1 + tax / 100));
            var line = new OrderDraftLine(
                productItem.ProductId,
                productItem.Code,
                productItem.Name,
                warehouseId,
                warehouseName,
                quantity,
                productItem.UnitSymbol,
                priceValue,
                promotionId,
                promotion?.Name,
                discountAmount,
                effectiveDiscount,
                tax,
                total
            );
            if (existingIndex >= 0)
                _lines[existingIndex] = line;
            else
                _lines.Add(line);
            BindLines();
        }

        /// <summary>Valida y guarda la operación de pedidos y ventas mediante la API.</summary>
        private void Save()
        {
            if (_lines.Count == 0)
            {
                ShowError(Localization.Text("Sales_EmptyOrder"));
                return;
            }
            if (
                !long.TryParse(
                    Convert.ToString(_client.SelectedValue, CultureInfo.InvariantCulture),
                    CultureInfo.InvariantCulture,
                    out var clientId
                )
                || !long.TryParse(
                    Convert.ToString(_list.SelectedValue, CultureInfo.InvariantCulture),
                    CultureInfo.InvariantCulture,
                    out var listId
                )
            )
            {
                ShowError(Localization.Text("Sales_SetupRequired"));
                return;
            }
            if (_assets.Any(asset => !_lines.Any(line => line.ProductId == asset.ProductLineId)))
            {
                ShowError(Localization.Text("Sales_AssetLineRequired"));
                return;
            }
            Request = new(
                _item?.OrderId,
                clientId,
                listId,
                _date.Value.ToUniversalTime(),
                _currency.Text.Trim(),
                _observation.Text.Trim(),
                _lines
                    .Select(line => new SaveOrderDetailRequest(
                        line.ProductId,
                        line.WarehouseId,
                        line.Quantity,
                        line.UnitPrice,
                        line.PromotionId is null ? line.DiscountPercent / 100 : 0,
                        line.TaxPercent / 100,
                        line.PromotionId
                    ))
                    .ToList(),
                _item?.RowVersion,
                _assets
                    .Select(asset => new SaveOrderAssetRequest(
                        asset.AssetId,
                        asset.ProductLineId,
                        asset.LinkType,
                        asset.ReturnMode,
                        asset.Observation,
                        asset.InboundMode,
                        asset.ExpectedReturnDate
                    ))
                    .ToList()
            );
            DialogResult = DialogResult.OK;
            Close();
        }

        /// <summary>Actualiza la moneda del pedido según los datos ingresados.</summary>
        private void UpdateCurrency()
        {
            if (
                long.TryParse(
                    Convert.ToString(_list.SelectedValue, CultureInfo.InvariantCulture),
                    CultureInfo.InvariantCulture,
                    out var listId
                )
            )
                _currency.Text = _data
                    .PriceLists.First(value => value.PriceListId == listId)
                    .Currency;
        }

        /// <summary>Calcula el descuento promocional según tipo, cantidad y precio vigentes.</summary>
        /// <param name="promotion">Promoción vigente que puede aplicarse al renglón.</param>
        /// <param name="quantity">Cantidad de unidades alcanzadas por el cálculo.</param>
        /// <param name="price">Precio unitario anterior a descuentos e impuestos.</param>
        /// <param name="manualPercent">Porcentaje de descuento manual informado para el renglón.</param>
        /// <returns>Valor obtenido después de aplicar la conversión o búsqueda.</returns>
        private static decimal PromotionDiscount(
            PromotionResponse? promotion,
            decimal quantity,
            decimal price,
            decimal manualPercent
        )
        {
            var subtotal = RoundMoney(quantity * price);
            if (promotion is null)
                return RoundMoney(subtotal * manualPercent / 100);
            var groups = decimal.Floor(quantity / promotion.RequiredQuantity);
            if (groups <= 0)
                return 0;
            var discount = promotion.PromotionType switch
            {
                "CANTIDAD_PAGADA" => groups
                    * (promotion.RequiredQuantity - promotion.PaidQuantity!.Value)
                    * price,
                "PORCENTAJE_UNIDADES" => groups
                    * promotion.DiscountedQuantity!.Value
                    * price
                    * promotion.DiscountRate!.Value,
                "PRECIO_PAQUETE" => groups
                    * (promotion.RequiredQuantity * price - promotion.PackagePrice!.Value),
                _ => 0,
            };
            return RoundMoney(Math.Clamp(discount, 0, RoundMoney(subtotal)));
        }

        /// <summary>Redondea un importe a la precisión monetaria usada por ventas.</summary>
        /// <param name="value">Valor que se convierte o asigna al destino correspondiente.</param>
        /// <returns>Valor obtenido después de aplicar la conversión o búsqueda.</returns>
        private static decimal RoundMoney(decimal value) =>
            Math.Round(value, 4, MidpointRounding.AwayFromZero);

        /// <summary>Agrega al borrador un vínculo trazable entre el activo concreto y el renglón cobrado.</summary>
        private void AddAsset()
        {
            if (
                _lines.Count == 0
                || !long.TryParse(
                    Convert.ToString(_client.SelectedValue, CultureInfo.InvariantCulture),
                    CultureInfo.InvariantCulture,
                    out var clientId
                )
            )
            {
                ShowError(Localization.Text("Sales_AssetLineRequired"));
                return;
            }
            var available = _data
                .Assets.Where(value =>
                    value.ClientOwnerId is null || value.ClientOwnerId == clientId
                )
                .ToList();
            if (available.Count == 0)
            {
                ShowError(Localization.Text("Sales_NoAssets"));
                return;
            }
            var products = _lines
                .GroupBy(value => value.ProductId)
                .Select(value => value.First())
                .ToList();
            using var dialog = new SalesInputForm(
                "Sales_LinkAsset",
                Field.Choice(
                    "Asset",
                    "Sales_Asset",
                    available[0].AssetId.ToString(CultureInfo.InvariantCulture),
                    available
                        .Select(value => new Choice(
                            value.AssetId.ToString(CultureInfo.InvariantCulture),
                            $"{value.Code} · {value.SerialNumber} · {value.Owner}"
                        ))
                        .ToList()
                ),
                Field.Choice(
                    "Product",
                    "Sales_ChargedLine",
                    products[0].ProductId.ToString(CultureInfo.InvariantCulture),
                    products
                        .Select(value => new Choice(
                            value.ProductId.ToString(CultureInfo.InvariantCulture),
                            $"{value.ProductCode} · {value.Product}"
                        ))
                        .ToList()
                ),
                Field.Choice(
                    "Link",
                    "Sales_LinkType",
                    "CLIENTE_SERVICIO",
                    [
                        new("CLIENTE_SERVICIO", Localization.Text("Sales_ClientService")),
                        new("VENTA_ACTIVO", Localization.Text("Sales_AssetSale")),
                        new("PRESTAMO", Localization.Text("Sales_AssetLoan")),
                        new("INTERCAMBIO", Localization.Text("Sales_AssetExchange")),
                    ]
                ),
                Field.Choice(
                    "Inbound",
                    "Sales_InboundMode",
                    "ENTREGA_CLIENTE",
                    [
                        new("ENTREGA_CLIENTE", Localization.Text("Sales_ClientDelivers")),
                        new("RETIRO_OXITIGRE", Localization.Text("Sales_OxiTigrePickup")),
                        new("NO_APLICA", Localization.Text("Sales_NotApplicable")),
                    ]
                ),
                Field.Choice(
                    "Return",
                    "Sales_ReturnMode",
                    "RETIRO_CLIENTE",
                    [
                        new("RETIRO_CLIENTE", Localization.Text("Sales_ClientPickup")),
                        new("ENTREGA_OXITIGRE", Localization.Text("Sales_OxiTigreDelivery")),
                        new("NO_APLICA", Localization.Text("Sales_NotApplicable")),
                    ]
                ),
                Field.Text("ExpectedReturn", "Sales_ExpectedReturn", string.Empty),
                Field.Text("Observation", "Inventory_Observation", string.Empty, true)
            );
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;
            var asset = available.First(value =>
                value.AssetId == long.Parse(dialog["Asset"], CultureInfo.InvariantCulture)
            );
            var product = products.First(value =>
                value.ProductId == long.Parse(dialog["Product"], CultureInfo.InvariantCulture)
            );
            if (
                _assets.Any(value =>
                    value.AssetId == asset.AssetId
                    && value.ProductLineId == product.ProductId
                    && value.LinkType == dialog["Link"]
                )
            )
            {
                ShowError(Localization.Text("Sales_DuplicateAsset"));
                return;
            }
            DateOnly? expectedReturn =
                string.IsNullOrWhiteSpace(dialog["ExpectedReturn"]) ? null
                : DateOnly.TryParse(
                    dialog["ExpectedReturn"],
                    CultureInfo.CurrentCulture,
                    out var parsedDate
                )
                    ? parsedDate
                : null;
            if (
                !string.IsNullOrWhiteSpace(dialog["ExpectedReturn"]) && expectedReturn is null
                || dialog["Link"] == "PRESTAMO" && expectedReturn is null
            )
            {
                ShowError(Localization.Text("Sales_ExpectedReturnRequired"));
                return;
            }
            _assets.Add(
                new(
                    asset.AssetId,
                    asset.Code,
                    asset.SerialNumber,
                    product.ProductId,
                    product.Product,
                    dialog["Link"],
                    dialog["Inbound"],
                    dialog["Return"],
                    expectedReturn,
                    dialog["Observation"]
                )
            );
            BindLines();
        }

        /// <summary>Actualiza las grillas de renglones y activos sin seleccionar registros automáticamente.</summary>
        private void BindLines()
        {
            Bind(_grid, _lines, "ProductId", "WarehouseId", "PromotionId");
            Bind(_assetGrid, _assets, "AssetId", "ProductLineId");
        }

        /// <summary>Agrega un campo etiquetado a la distribución visual indicada.</summary>
        /// <param name="panel">Panel visual al que se agrega el control.</param>
        /// <param name="key">Clave de recurso o campo que identifica el texto o control.</param>
        /// <param name="control">Control visual asociado al campo o sección.</param>
        private static void Add(FlowLayoutPanel panel, string key, Control control)
        {
            panel.Controls.Add(
                new Label
                {
                    Text = Localization.Text(key),
                    AutoSize = true,
                    Margin = new Padding(0, 8, 5, 0),
                }
            );
            panel.Controls.Add(control);
        }

        /// <summary>Muestra un error funcional sin exponer detalles técnicos.</summary>
        /// <param name="message">Mensaje funcional que se muestra al usuario.</param>
        private void ShowError(string message) =>
            MessageBox.Show(this, message, "OxiTigre", MessageBoxButtons.OK, MessageBoxIcon.Error);

        private sealed record OrderDraftLine(
            long ProductId,
            string ProductCode,
            string Product,
            long? WarehouseId,
            string? Warehouse,
            decimal Quantity,
            string UnitSymbol,
            decimal UnitPrice,
            long? PromotionId,
            string? PromotionName,
            decimal DiscountAmount,
            decimal DiscountPercent,
            decimal TaxPercent,
            decimal LineTotal
        );

        private sealed record OrderAssetDraft(
            long AssetId,
            string AssetCode,
            string SerialNumber,
            long ProductLineId,
            string ProductLine,
            string LinkType,
            string InboundMode,
            string ReturnMode,
            DateOnly? ExpectedReturnDate,
            string Observation
        );
    }
}
