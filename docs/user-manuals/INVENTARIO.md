# Manual de usuario: Inventario

## Antes de comenzar

Desde el panel principal, presionar **Inventario**. El administrador puede mantener datos y confirmar movimientos;
un usuario de consulta solo puede revisar la información. Antes de cargar stock deben existir al menos una categoría,
una unidad, un producto y un depósito.

## Productos y catálogos

1. En **Categorías y unidades**, crear las clasificaciones y unidades que utiliza la empresa.
2. En **Productos**, presionar **Nuevo producto**, elegir categoría y unidad, e informar nombre.
3. Código de barras y descripción son opcionales. El código `PRD-...` se genera automáticamente.
4. Para retirar un maestro de nuevas operaciones, editarlo y elegir **Inactivo**; no se elimina su historial.

El empleado debe confirmar con el responsable de stock el nombre comercial, la unidad real de control y, si existe,
el código de barras. No debe convertir metros, kilogramos o unidades de forma manual sin una regla aprobada.

## Depósitos y ubicaciones

Crear primero el depósito y luego sus ubicaciones. Una ubicación identifica una posición interna, por ejemplo
**Pasillo A · Estante 2**. El domicilio y la descripción ayudan a distinguir depósitos similares.

## Existencias y alertas

La cantidad no se escribe manualmente: cambia únicamente al confirmar movimientos. La grilla separa **Stock físico**,
**Stock reservado** por pedidos y **Stock disponible** para nuevas operaciones. En **Existencias**, seleccionar una fila
y presionar **Modificar mínimo**. Cuando el disponible sea menor o igual al mínimo, la fila se resaltará.

## Movimientos

1. Presionar **Nuevo movimiento** y elegir tipo y fecha.
2. Agregar uno o más renglones indicando producto, cantidad y el origen o destino solicitado.
3. Revisar todos los renglones y presionar **Confirmar movimiento**.

- **Entrada:** mercadería que ingresa a un depósito.
- **Salida:** mercadería que egresa de un depósito.
- **Transferencia:** traslado entre dos depósitos o ubicaciones distintas.
- **Ajuste de entrada/salida:** corrección documentada de una diferencia física.

Antes de confirmar, el empleado debe verificar comprobante u origen del pedido, producto, unidad, cantidad, depósito,
ubicación y motivo. Un movimiento confirmado no se modifica: si hubo un error, se registra un ajuste contrario con
una observación clara. Si el saldo no alcanza, el sistema muestra el código `30004` y no cambia ninguna existencia.
