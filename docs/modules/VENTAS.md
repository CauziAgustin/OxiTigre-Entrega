# Pedidos y ventas internas

## Alcance completado

- Listas de precios con moneda, vigencia, estado y precio por producto.
- Promociones por empresa y producto con vigencia, estado y tres tipos de beneficio.
- Pedidos valorizados con promoción o descuento manual e impuesto por renglón.
- Productos físicos y servicios parametrizados en el mismo catálogo y listas de precios.
- Tubos concretos vinculados al trabajo cobrado, con propietario, finalidad, retiro o entrega y observación.
- Estados `BORRADOR`, `CONFIRMADO`, `CANCELADO` y `VENDIDO`.
- Reservas separadas del stock físico.
- Venta interna inmutable; genera salida de Inventario únicamente por los productos físicos.
- Permisos `COMERCIAL.CONSULTAR` y `COMERCIAL.VENTAS_GESTIONAR`.
- Concurrencia optimista y errores controlados `40004` a `40010`.

## Regla transaccional

Confirmar reserva solamente los renglones `PRODUCTO`; los renglones `SERVICIO` no necesitan depósito ni existencia.
Cancelar libera; vender aplica la reserva, genera un único movimiento de salida cuando hay productos físicos, copia
la valorización del pedido y marca el pedido vendido. Si falla cualquier paso, SQL Server revierte todos.

## Servicios y activos

Recarga, cambio de válvula, mano de obra y envío son servicios del catálogo. Tienen precio por lista e impuestos,
pero nunca aumentan, reservan ni descuentan stock. Un pedido puede mezclar esos cargos con productos físicos.

`COMERCIAL.PEDIDOS_ACTIVOS` identifica qué tubo corresponde a qué renglón y conserva la finalidad
`CLIENTE_SERVICIO`, `VENTA_ACTIVO`, `PRESTAMO` o `INTERCAMBIO`; también registra si retira el cliente, entrega
OxiTigre o no aplica. La observación es obligatoria. Vincular un activo no cambia su custodia por sí solo: el ingreso,
la entrega, el préstamo y la ruta se confirman en sus operaciones trazables correspondientes.

## Límite aprobado

La venta actual es interna y no equivale a comprobante fiscal. Factura, nota de crédito, pagos, cuenta corriente e
integración AFIP se implementarán cuando estén aprobadas sus reglas fiscales y contables.

## Promociones

Cada promoción pertenece a una empresa y un producto, y solo se aplica si está `ACTIVO` y la fecha del pedido está
dentro de su vigencia. Se admite una promoción por renglón y no puede combinarse con descuento manual.

| Tipo | Configuración | Beneficio por cada grupo completo |
|---|---|---|
| `CANTIDAD_PAGADA` | cantidad requerida `N` y cantidad pagada `M` | cobra `M` unidades; cubre 2x1 y 4x3 |
| `PORCENTAJE_UNIDADES` | cantidad requerida, unidades bonificadas y porcentaje | descuenta el porcentaje a las unidades bonificadas |
| `PRECIO_PAQUETE` | cantidad requerida y precio del paquete | cobra el precio fijo por cada paquete completo |

SQL Server valida la regla y calcula el descuento; la pantalla no constituye la fuente contable. Antes del cálculo,
los renglones compatibles se agrupan por producto, depósito, precio, descuento manual, impuesto y promoción. Así, agregar una unidad y
luego otra acumula cantidad para un 2x1. Los sobrantes fuera de un grupo completo mantienen su precio normal.

El pedido guarda código, nombre e importes calculados de la promoción. Al vender, esos datos se copian como una
fotografía inmutable, por lo que editar o inactivar la promoción no modifica ventas anteriores.

## Verificación

```powershell
dotnet test OxiTigre.sln --configuration Release
sqlcmd -S ".\SQLEXPRESS" -d "OxiTigre_Development" -E -b -C -i "tests\Database\SMOKE_VENTAS.sql"
sqlcmd -S ".\SQLEXPRESS" -d "OxiTigre_Development" -E -b -C -i "tests\Database\SMOKE_PEDIDO_SERVICIOS_ACTIVOS.sql"
```
