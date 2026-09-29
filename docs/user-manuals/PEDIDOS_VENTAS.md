# Pedidos y ventas internas

## Antes de cargar

Confirmar con el cliente qué productos o servicios necesita, cantidades, depósito cuando haya mercadería, moneda,
precio acordado, promoción o descuento manual e impuesto aplicable. Si hay tubos, confirmar de quién son, número de
serie, trabajo pedido, si el cliente retira o si OxiTigre entrega y dejar una observación concreta.

## Lista de precios

1. Abrir **Listas de precios** y crear la lista con código, nombre, moneda y vigencia.
2. Seleccionarla y agregar cada producto con su precio unitario.
3. Inactivar una lista solo cuando ya no deba usarse para pedidos nuevos.

## Promociones

1. Abrir **Promociones** y elegir producto, código, nombre, estado y vigencia; la empresa es la seleccionada al iniciar sesión.
2. Elegir una única regla:
   - **Llevá N, pagá M**: `N=2, M=1` para 2x1; `N=4, M=3` para 4x3.
   - **Porcentaje en unidades bonificadas**: `N=2`, una unidad bonificada y `50 %` para la segunda al 50 %; `N=1`, una unidad bonificada y `50 %` para cada unidad al 50 %.
   - **Precio fijo por paquete**: informar cuántas unidades forman el paquete y su precio total.
3. Guardar y comprobar que permanezca `ACTIVO` durante la fecha en que deberá aplicarse.

Una promoción se aplica solamente al producto configurado. No se combina con descuento manual: al seleccionarla, el
descuento manual debe quedar en cero y SQL Server calcula el importe definitivo.

## Pedido

1. Presionar **Nuevo pedido** y elegir cliente, lista, fecha y moneda.
2. Agregar uno o más renglones. Un producto físico exige depósito; un servicio como recarga, válvula, mano de obra o
   envío muestra **No aplica** y no usa depósito.
3. Si interviene un tubo, usar **Vincular tubo o activo** y seleccionar:
   - el tubo exacto y el renglón que se cobrará;
   - servicio sobre activo del cliente, venta, préstamo o intercambio;
   - retiro del cliente, entrega OxiTigre o no aplica;
   - una observación que explique el trabajo y la entrega prevista.
4. Revisar el total y guardar: queda `BORRADOR`, todavía sin reservar stock.
5. Mientras esté en borrador puede corregirse o cancelarse.
6. Presionar **Confirmar y reservar** únicamente cuando el cliente haya confirmado. El estado cambia a `CONFIRMADO`.

Si se agrega varias veces el mismo producto con igual depósito, precio, descuento manual, impuesto y promoción, el
sistema reúne esas entradas y vuelve a calcular sobre la cantidad total. Por ejemplo, dos cargas sucesivas de una unidad activan un 2x1.
Si cambia cualquiera de esos datos, se conserva otro renglón y no se mezclan las cantidades.

En Development existen ejemplos en los estados **Borrador**, **Confirmado** y **Vendido**. La pestaña **Ventas** solo
muestra información después de generar una venta desde un pedido confirmado; una venta no se carga directamente.

## Cancelación y venta

- **Cancelar pedido** libera las reservas activas y es definitivo.
- **Generar venta** descuenta únicamente el stock físico reservado, registra la cuenta a cobrar completa y deja el
  pedido `VENDIDO`. Los servicios se cobran, pero no crean movimientos de stock.
- El estado de pago se consulta en **Finanzas · Cuenta corriente** y **Cobros**; no se escribe “pagado” solamente en
  la observación.
- La venta conserva la promoción y los importes aplicados aunque después la promoción se edite o inactive.
- No repetir la acción si la pantalla demora; actualizar el listado y revisar el estado.
- Un mensaje con código `40005` a `40009` debe informarse completo, incluida su correlación.

La venta de esta fase es un registro interno. No entregar como factura fiscal hasta que el módulo de comprobantes esté aprobado.
