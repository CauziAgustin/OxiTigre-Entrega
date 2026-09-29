# Manual de usuario — Compras y trazabilidad

## Antes de empezar

Confirmá qué se pidió, qué llegó físicamente, número de remito, depósito y condición. Para gases o líquidos preguntá lote, cantidad medida, método de medición y qué envase contiene el producto. Para equipos y envases preguntá número de serie, propietario, capacidad y condición.

## Crear o editar una orden

La pantalla abre maximizada y guía la carga en cuatro pasos: proveedor y depósito; entrega y observación; productos
con cantidad, costo, descuento e impuesto; revisión del total. Seleccioná un renglón para actualizarlo o quitarlo.
Las grillas muestran nombres funcionales y permiten filtrar borradores, órdenes en curso y finalizadas.

Solo una orden `BORRADOR` puede editarse. Una orden aprobada o parcialmente recibida conserva su documento original:
usá **Ver detalle** y continuá con recepción, cierre de saldo o cancelación según corresponda.

## Proveedores

Entrá en **Compras → Proveedores**. Usá **Nuevo proveedor** o seleccioná una fila y elegí **Editar proveedor**. Registrá razón social, CUIT, correo, teléfono, condición de pago y contacto principal. En CUIT escribí los once números: la pantalla agrega los guiones y valida el dígito verificador. No crees un duplicado si el CUIT ya existe.

**Cambiar estado** da de baja o reactiva al proveedor sin borrar su historia. Un proveedor inactivo no aparece para órdenes nuevas. Las grillas identifican su contenido con un título y permiten elegir la columna por la que se busca. Para una fecha escribí `1`, `1/8` o `1/8/26`; **×** limpia el filtro y **?** explica la pantalla.

## Órdenes y recepción

1. Creá la orden en borrador y agregá los productos, cantidades, costos, descuentos e impuestos.
   Los importes muestran separadores de miles y decimales sin cambiar el valor guardado.
   Seleccioná la orden y usá **Ver detalle** para revisar proveedor, CUIT, condición de pago, depósito, fechas, estado, renglones y totales.
2. Enviá a aprobación. Desde ese momento ya no se edita.
3. Un usuario autorizado aprueba o cancela.
4. En **Recepcionar**, informá por renglón cantidades aceptadas, rechazadas y dañadas. Si existe diferencia, escribí el motivo.
5. Para lotes usá `CODIGO:CANTIDAD` separados por `;`. Para activos, informá las series separadas por `;`. Si el producto recibido queda dentro de un envase, completá **Contenido** como `SERIE:CODIGO_PRODUCTO:LOTE:CANTIDAD:METODO`.
6. Si llegó menos, la orden queda **Recibida parcial**. Esperá otra entrega o usá **Cerrar saldo** con motivo. Si llegó más, no confirmes: el sistema lo rechaza.

Una recepción confirmada no se edita ni se borra. Si fue cargada por error y el material todavía no tuvo ninguna operación posterior, seleccionala en **Recepciones**, elegí **Reversar recepción**, escribí el motivo y confirmá. El sistema genera una salida compensatoria y vuelve a dejar pendiente la cantidad en la orden. Si el material ya fue movido, vendido, fraccionado, medido, prestado o intervenido, la reversión se bloquea para no romper la trazabilidad; revisá el historial y registrá el ajuste o incidente correspondiente.

## Estado actual y mediciones

En **Trazabilidad industrial** se muestran activos, lotes y contenido almacenado. Seleccioná **Registrar medición**, elegí activo, tipo, valor, unidad y método. Usá `MANUAL`, `PESAJE` o `CALCULADO`; `SENSOR` queda reservado para una integración futura identificada.

### Tubos propiedad del cliente

Usá **Registrar tubo del cliente** cuando el envase no pertenece a OxiTigre. Elegí cliente y producto, informá la serie si existe, tipo, capacidad, unidad, condición y una observación que permita reconocerlo. Si no posee serie, el sistema asigna una identificación interna; no reutilices la identificación de otro tubo.

- Elegí **Permanece con el cliente** para registrar propiedad sin indicar depósito.
- Elegí **Recibido en OxiTigre** solamente cuando el tubo ingresó físicamente y seleccioná el depósito que lo custodia.
- Para un tubo ya registrado, seleccioná la fila y usá **Registrar ingreso o entrega**. Un tubo que está con el cliente puede ingresar; uno disponible bajo custodia puede entregarse. Un tubo bloqueado, prestado o en mantenimiento debe resolver primero esa situación.

Cada alta, ingreso y entrega conserva usuario, fecha, condición, observación y correlación en **Historial inmutable**. El estado de pago no se escribe en la observación: se consulta en Finanzas. Los trabajos y cargos asociados se incorporan desde el pedido en el siguiente bloque funcional.

## Fraccionamiento y pérdidas

Elegí el lote grande e indicá uno o más destinos como `CODIGO_ACTIVO:CANTIDAD`, separados por `;`. La cantidad de origen se calcula como destinos más merma y se confirma en una transacción. Si se retiró contenido sin registrarlo, primero registrá consumo/incidente/ajuste y recién después prestá o enviá el activo.

## Incidentes y mantenimiento

Registrá fecha, activo, tipo, cantidad antes/después, pérdida exacta o estimada, causa y acción tomada. El activo queda bloqueado para evitar uso accidental. Iniciá el mantenimiento y, al terminar, documentá resultado, válvula/componente reemplazado, costo, certificado y próxima revisión.

## Préstamos y devolución

Indicá uno o más activos como `CODIGO_ACTIVO:CANTIDAD`, elegí si el destino es sucursal, cliente, tercero u otra empresa y completá solamente el dato correspondiente. Informá si OxiTigre entrega o el destinatario retira, devolución prevista y condición. La cantidad puede ser `0` para un tubo vacío. Al regresar, registrá cantidad y condición reales. Si hay diferencia o rotura, confirmá la devolución y vinculá un incidente. Las fechas próximas o vencidas se visualizan; los avisos automáticos se configurarán cuando se implemente el motor de alertas.
