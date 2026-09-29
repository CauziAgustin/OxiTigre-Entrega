# Logística y hojas de ruta

Este módulo organiza domicilios de entrega, solicitudes, recorridos y custodia de tubos o activos. No elimina la
historia: cada hoja conserva los datos que tenía la parada al momento de planificarla.

## Antes de cargar una solicitud

Confirmá con el cliente:

- domicilio exacto, localidad y referencia de acceso;
- nombre y teléfono de quien recibe;
- franja horaria real en la que se lo puede visitar;
- fecha necesaria, prioridad e instrucciones para el transportista;
- si se entrega, retira, recarga, intercambia o presta un tubo;
- número de serie, propietario, condición y contenido de cada tubo involucrado.

## Domicilios y horarios

1. Presioná **Nuevo domicilio** y elegí el cliente.
2. Completá contacto, teléfono e instrucciones obligatorias.
3. Si existe una restricción horaria, informá **Desde** y **Hasta** en formato `HH:mm`; si no existe, dejá ambos
   vacíos.
4. Guardá y comprobá el domicilio en la grilla.

## Solicitudes y custodia

Si el trabajo ya se cargó en Pedidos, usá primero **Pedidos para planificar**. Seleccioná el pedido, elegí domicilio,
fecha y depósito receptor, y generá el retiro o la entrega ofrecidos. No vuelvas a cargar la serie: el sistema usa los
activos autorizados en el pedido y muestra si la venta está pendiente, parcialmente pagada o pagada.

Para un trabajo independiente:

1. Presioná **Nueva solicitud** y elegí el domicilio.
2. Indicá tipo de servicio, prioridad, fecha, horario, instrucciones y el depósito que recibirá los retiros.
3. Para retiros, devoluciones o intercambios, identificá los activos. Usá:
   - **Propietario CLIENTE** para un tubo ajeno que OxiTigre recibe temporalmente.
   - **Propietario OXITIGRE** para un tubo propio que se entrega o presta.
   - **RETIRO_CLIENTE**, **PRESTAMO_TEMPORAL**, **ENTREGA** o **DEVOLUCION** según el movimiento acordado.
4. Guardá y revisá el detalle de custodia debajo de la solicitud.

No registres como stock vendible un tubo perteneciente al cliente.

## Armar y ejecutar una hoja de ruta

1. En **Transportistas y vehículos**, verificá que exista un transportista y un vehículo activos. Si no existe la
   persona, primero asignale el rol **Transportista** desde **Seguridad · Usuarios**.
2. Al cargar un vehículo, elegí si es de la empresa o del transportista particular. La patente admite `ABC-123` y
   `AB-123-CD`; la aplicación agrega los guiones.
3. En **Solicitudes**, seleccioná con `Ctrl` una o varias solicitudes `PENDIENTE`.
4. Presioná **Crear hoja**, indicá fecha, tipo y observación, y elegí una modalidad:
   - **Asignación directa:** seleccioná transportista y vehículo desde la oficina.
   - **Publicar como oferta:** dejá la hoja disponible para que un transportista la tome desde el móvil.
5. Usá una hoja **URGENTE** solamente para una solicitud inmediata; una hoja **NORMAL** admite varias paradas.
6. Para una hoja ofrecida, usá **Asignar oferta** si la oficina decide el chofer o **Editar oferta** si debe cambiar
   fecha, tipo u observación antes de que alguien la tome.
7. En **Hojas de ruta**, elegí la hoja asignada y revisá sus paradas antes de **Despachar**.
8. Al terminar una visita, elegí la parada, presioná **Confirmar parada** e informá siempre el resultado y una
   observación concreta. Si fue parcial, marcá únicamente los activos realmente retirados o entregados.

Resultados disponibles:

- **ENTREGADA**: el servicio se cumplió por completo.
- **PARCIAL**: se cumplió una parte; detallá qué quedó pendiente.
- **FALLIDA**: no pudo realizarse; explicá el motivo.
- **REPROGRAMADA**: vuelve a la agenda pendiente sin borrar la visita anterior.

Una confirmación de retiro mueve el activo del cliente al depósito elegido; una devolución lo devuelve a su estado
**En cliente**. Un préstamo temporal registra además el compromiso de devolución. Revisá después el **Historial
inmutable** de Trazabilidad si necesitás comprobar quién realizó el cambio.

Una hoja ofrecida o planificada puede cancelarse únicamente con un motivo. Una hoja despachada puede pausarse y
reanudarse con motivo desde el móvil, sin liberar su carga. Se resuelve confirmando sus paradas para no perder
trazabilidad.

## Historial y avisos

La pestaña **Historial y avisos** muestra quién realizó cada cambio, cuándo ocurrió y qué resultado tuvo. Los avisos
de esta etapa son simulados: permiten revisar destinatario y mensaje, pero todavía no envían correo ni WhatsApp.

Ante un error de carga, no borres ni reemplaces registros históricos. Cancelá, reprogramá o registrá el resultado con
una observación que permita reconstruir lo sucedido.
