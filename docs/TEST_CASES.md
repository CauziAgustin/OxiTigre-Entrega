# Casos de revisión funcional

## Experiencia común

1. Abrir Clientes, Usuarios y cada pestaña de Pedidos, Ventas, Inventario, Compras y Trazabilidad: las grillas deben ocupar todo el ancho disponible y no seleccionar la primera fila.
2. Elegir una columna del filtro y buscar texto con una o varias palabras; **×** debe restaurar todos los registros.
3. En una columna de fecha probar `1`, `01`, `1/8`, `01/08`, `1/8/26` y `01/08/2026`; deben encontrarse solamente las fechas cuyos componentes coincidan.
4. Verificar que **?** sea circular, abra la ayuda al hacer clic y no se estire junto a un motivo multilínea.
5. Pasar el puntero y presionar botones verdes, grises, ámbar y rojos: el borde y el color deben responder sin perder el texto ni el foco de teclado.

## Login y empresa

1. Ingresar `AOCAUZI` y la contraseña vigente: debe aparecer el desplegable de empresa.
2. Elegir `OxiTigre` e ingresar: debe abrir la ventana principal.

## Usuarios

1. Ingresar como administrador y abrir **Usuarios**: debe verse el listado actual.
2. Crear un usuario con nombres, apellido, correo, uno o más roles y contraseña temporal de al menos 10 caracteres, con mayúscula, minúscula y número.
3. Confirmar que el usuario generado respete la convención corporativa y que su primer acceso exija cambiar la contraseña.
4. Editar nombres, apellido, correo y roles; cerrar y volver a abrir la pantalla para confirmar la persistencia.
5. Cambiar el estado a `INACTIVO`: el usuario no debe poder iniciar sesión y sus sesiones anteriores deben quedar revocadas.
6. Volver a activarlo y asignarle una contraseña temporal desde **Reiniciar clave**: el siguiente acceso debe exigir el cambio.
7. Presionar **Revocar sesiones** sobre otro usuario: un token anterior debe dejar de ser válido.
8. Intentar desactivar al propio administrador o quitarle `ADMINISTRADOR`: la aplicación debe rechazarlo.

## Sesiones y bloqueo

1. Cerrar sesión desde la ventana principal: el token anterior debe devolver HTTP `401`.
2. Ingresar cinco veces una contraseña incorrecta: la cuenta debe quedar bloqueada durante quince minutos.
3. Usar el reinicio administrativo para recuperar una cuenta bloqueada: debe limpiar el bloqueo y exigir cambio de clave.
4. Revocar las sesiones de otro usuario conectado: su aplicación debe detectar el cierre en hasta cinco segundos, mostrar el aviso y cerrar en quince segundos o al presionar **Cerrar ahora**.
5. Seleccionar el propio usuario y revocar sesiones: la operación debe aceptarse y cerrar esta aplicación con el mismo aviso.

## Panel principal

1. Confirmar que el ingreso abre el panel principal y no directamente el listado de clientes.
2. Verificar que empresa, sucursal, avisos, usuario y conexión permanezcan visibles en el encabezado y pie globales.
3. Confirmar que **Flujo operativo** muestre Pedidos, Stock, Despacho, Entrega y Cobro con datos de la API.
4. Confirmar que **Lo que necesita una decisión** y **Actividad reciente** expliquen el estado vacío cuando no hay datos.
5. Abrir cada opción lateral: debe aparecer primero su paneo con métricas, explicación y acciones operativas.
6. Abrir una pantalla operativa desde el paneo: la barra lateral y el encabezado deben permanecer visibles.
7. Maximizar y luego reducir la ventana hasta su tamaño mínimo: título, búsqueda, sucursal, campana y usuario deben
   permanecer dentro del encabezado, sin invadir el contenido.
7. Buscar `clientes`, `recepciones` y `hojas de ruta`; cada sugerencia debe navegar al destino correcto.
8. Presionar `Ctrl+K`: el foco debe pasar a la búsqueda global.
9. Abrir **Seguridad** como administrador; con rol de consulta debe permanecer deshabilitado.
10. Presionar la empresa y sucursal: debe advertir que el cambio requiere volver al acceso.
11. Presionar **Cerrar sesión**: debe invalidar la sesión y volver al login sin finalizar la aplicación.
12. Ingresar nuevamente y cerrar el panel con la **X**: debe finalizar la aplicación completa.
13. Reducir la ventana o usar escala 150 %: navegación, contenido y **Cerrar sesión** deben permanecer utilizables.

## Clientes y teléfonos

1. Ingresar como administrador: deben estar habilitados **Nuevo cliente**, **Editar** y **Cambiar estado**.
2. Crear una persona física con nombre, apellido y dos teléfonos: la aplicación debe exigir exactamente uno principal.
3. Confirmar que el código visible se genere como `CLI-000001` y que el teléfono principal aparezca en el listado.
4. Editar el cliente, cambiar datos generales y reemplazar sus teléfonos: el listado debe reflejar la nueva información.
5. Cambiar el cliente a `INACTIVO`: debe desaparecer de la vista `ACTIVO` y aparecer en la vista `INACTIVO`.
6. Volver a activarlo: debe conservar sus datos y teléfonos.
7. Ingresar con rol `CONSULTA`: debe poder listar y consultar, pero no crear, editar ni cambiar estados.
8. Confirmar que `F` y `J` se presenten como **Persona física** y **Persona jurídica**.
9. Elegir persona jurídica: debe decir **Razón social** y ocultar **Apellido**.
10. Cambiar el tipo de persona: los tipos de documento deben filtrarse desde el catálogo.
11. Seleccionar Argentina: el código de país debe completarse con `54` y quedar bloqueado.
12. Seleccionar otro país: debe permitirse escribir manualmente un código numérico.
13. Guardar una carga incompleta como borrador, cerrar y volver a **Nuevo cliente**: debe ofrecer recuperarla.
14. Confirmar el cliente recuperado: el borrador debe descartarse automáticamente.
15. Verificar que todas las columnas de clientes y teléfonos estén tituladas en español.

## Errores controlados

1. Presionar **Cambiar** sin escribir una contraseña: debe mostrar una advertencia funcional, no una excepción.
2. Enviar datos inválidos de cliente: debe mostrar el mensaje, código `40001` y correlación.
3. Provocar un duplicado de documento: debe rechazarse sin cerrar la aplicación ni mostrar stack trace.
4. Consultar un cliente inexistente: la API debe devolver HTTP `404`, código `40002`, descripción y correlación.

## Configuración e idioma

1. Abrir **Configuración** como administrador y confirmar las seis pestañas; con rol `CONSULTA`, los botones de edición deben estar deshabilitados.
2. Editar la empresa y volver a abrir la pantalla para confirmar persistencia.
3. Crear una sucursal y una unidad operativa; intentar inactivar la sucursal activa debe rechazarse mientras la unidad siga activa.
4. Crear un tipo de teléfono y verificar que aparezca al cargar un cliente nuevo.
5. Editar el nombre o la vigencia de un estado sin cambiar su código técnico.
6. Crear un parámetro entero válido y luego intentar guardar texto como entero: debe aparecer un error controlado.
7. Crear un parámetro secreto usando solo una referencia; comprobar que la grilla no muestre el valor ni la referencia.
8. Crear un módulo de prueba y un error: el código debe ser el número de módulo seguido por cuatro dígitos correlativos.
9. Editar causa probable y acción recomendada del error; el código generado no debe cambiar.
10. Crear o editar una traducción inglesa y seleccionar **English**: debe avisar y reconstruir el dashboard, menús, botones, validaciones y grillas en inglés sin cerrar la sesión ni mostrar el login.
11. Volver a español y confirmar que los códigos internos (`ACTIVO`, permisos y códigos de error) no hayan cambiado.
12. Abrir la interfaz al 100 %, 125 % y 150 % de escala de Windows; confirmar que no se recorten textos en español ni inglés.
13. Abrir el mismo registro en dos ventanas, guardar cambios en ambas: la segunda debe recibir conflicto controlado y conservar el primer cambio.

## Inventario

1. Abrir **Inventario** como administrador; con rol `CONSULTA`, verificar que todos los botones de edición estén deshabilitados.
2. Crear una categoría y una unidad de medida; editar sus nombres y comprobar que los códigos técnicos no cambien.
3. Crear un producto: debe recibir un código `PRD-000001` o el siguiente correlativo disponible.
4. Crear un depósito y dos ubicaciones; comprobar que cada ubicación pertenezca al depósito elegido.
5. Confirmar una **Entrada** de producto: la existencia por depósito debe aumentar y aparecer el movimiento `MOV-...`.
6. Confirmar una **Salida** menor al saldo: la existencia debe disminuir sin editar manualmente la cantidad.
7. Intentar una salida mayor al saldo: debe rechazarse con código `30004` y no registrar ningún movimiento.
8. Transferir entre dos depósitos: el origen debe disminuir, el destino aumentar y ambos deben quedar trazados.
9. Crear un ajuste de entrada y uno de salida; verificar que ambos exijan cantidades positivas.
10. Modificar el stock mínimo por encima del saldo: la fila debe resaltarse como **Bajo mínimo**.
11. Editar simultáneamente un maestro o mínimo desde dos instancias: la segunda debe recibir `30003` sin pisar el primer cambio.
12. Cambiar a inglés y volver a abrir Inventario: pestañas, botones, estados, tipos y columnas deben verse traducidos.
13. Verificar que la grilla muestre físico, reservado y disponible; el pedido confirmado de Development debe reservar 2 válvulas.
14. Abrir **Categorías y unidades** y **Existencias**: las columnas booleanas deben mostrarse sin excepción ni cuadro técnico de `DataGridView`.
15. Confirmar que ninguna grilla de Inventario marque automáticamente su primera fila al abrir o actualizar.

## Pedidos y ventas internas

1. Abrir **Comercial · Pedidos y ventas** como administrador; con rol `CONSULTA`, comprobar que todas las acciones de edición estén deshabilitadas.
2. Crear una lista `MAYORISTA` en `ARS`, definir vigencia y agregar un precio para `PRD-000001`; editar el importe y comprobar su persistencia.
3. Crear un pedido con cliente, lista y dos renglones; verificar cálculo de subtotal, descuento, impuesto y total.
4. Cerrar y volver a abrir: el pedido debe permanecer `BORRADOR` y permitir edición.
5. Confirmar el pedido: debe pasar a `CONFIRMADO`, reservar la cantidad y no modificar todavía la existencia física.
6. Intentar una salida manual que consuma la cantidad reservada: debe rechazarse con `30004`.
7. Cancelar un pedido confirmado: debe pasar a `CANCELADO`, liberar su reserva y volver a permitir el uso de ese disponible.
8. Crear otro pedido, confirmarlo y presionar **Generar venta**: debe crear `VEN-000001` o el siguiente, marcar el pedido `VENDIDO` y descontar el stock una sola vez.
9. Abrir **Inventario · Movimientos** y comprobar la salida `MOV-...` vinculada en su observación con el pedido vendido.
10. Intentar editar, confirmar, cancelar o vender nuevamente un pedido finalizado: debe aparecer `40006` sin duplicar movimiento ni venta.
11. Abrir el mismo borrador en dos instancias, guardar ambas: la segunda debe recibir `40007` y no pisar el primer cambio.
12. Cambiar a English y volver a abrir: pestañas, botones, estados y columnas deben mostrarse traducidos sin perder la sesión.
13. Revisar la pantalla a 100 %, 125 % y 150 % de escala, en ambos idiomas, sin texto recortado.
14. Confirmar que Development muestre al menos dos listas, ocho precios y una venta interna de demostración.
15. Crear una promoción 2x1 (`N=2`, `M=1`), cargar dos unidades y comprobar que descuente exactamente una unidad antes del impuesto.
16. Crear una promoción 4x3 (`N=4`, `M=3`), cargar cuatro unidades y comprobar que descuente exactamente una unidad; con cinco, la quinta debe quedar a precio normal.
17. Configurar segunda unidad al 50 % (`N=2`, una bonificada, `50 %`): dos unidades deben descontar medio precio unitario.
18. Configurar una unidad al 50 % (`N=1`, una bonificada, `50 %`): cada unidad cargada debe recibir ese porcentaje.
19. Configurar un paquete de tres unidades a precio fijo: tres unidades deben costar el paquete; cuatro deben sumar una unidad a precio normal.
20. Con un 2x1, agregar una unidad y luego otra con iguales producto, depósito, precio, impuesto y promoción: debe quedar cantidad dos y activarse el descuento acumulado.
21. Repetir el caso anterior cambiando depósito, precio, impuesto o promoción: las entradas no deben agruparse.
22. Intentar guardar un renglón con promoción y descuento manual simultáneos: debe rechazarse de forma controlada. Una promoción inactiva, vencida o de otro producto tampoco debe aplicarse.
23. Vender un pedido promocional y luego editar o inactivar la promoción: la venta debe conservar código, nombre e importes originales.
24. Abrir o actualizar Pedidos, Ventas, Listas, Precios y Promociones: ninguna grilla debe iniciar con la primera fila seleccionada y sus detalles deben quedar vacíos hasta elegir una fila.
25. Crear un artículo de tipo **Servicio sin stock**, asignarle precio y comprobar que al agregarlo al pedido el
    depósito quede en **No aplica**.
26. Crear un pedido con un producto físico y una recarga; vincular un tubo del cliente con observación obligatoria,
    confirmar y vender. Debe existir una sola reserva y un solo renglón de movimiento físico, pero los dos cargos en
    la venta y en la cuenta corriente.
27. Intentar vincular como “servicio del cliente” un tubo propio de OxiTigre: debe rechazarse con error controlado
    `40010`.

## Panel administrativo web

1. Iniciar API y `OxiTigre.AdminWeb`; abrir `http://localhost:5100`.
2. Intentar ingresar con un usuario sin rol `ADMINISTRADOR`: debe rechazarlo y cerrar el token creado.
3. Ingresar como administrador con empresa `OXITIGRE`: debe mostrar indicadores, stock, pedidos, ventas y usuarios.
4. Verificar que la válvula muestre 8 físicas, 2 reservadas y 6 disponibles en los datos iniciales de Development.
5. Presionar **Cerrar sesión** y volver a `/`: debe solicitar autenticación nuevamente.
6. Verificar **Vista global de empresas**: OxiTigre debe aparecer disponible con versión `9.0.0`.

## Plataforma multiempresa y San Fernando

1. Reiniciar la API e iniciar sesión: los tokens anteriores a la fase 9 deben rechazarse y el nuevo debe conservar la empresa seleccionada.
2. Abrir **Configuración · Sucursales** y comprobar `OxiTigre San Fernando`, localidad `San Fernando`, provincia `Buenos Aires` y estado activo.
3. Verificar la unidad operativa `OPERACION_SF`, el depósito `SAN_FERNANDO` y su ubicación `RECEPCION`.
4. Abrir el panel web con un administrador y comprobar la vista global; un usuario sin ese rol debe recibir acceso denegado.
5. Ejecutar `sqlcmd -S ".\SQLEXPRESS" -d "master" -E -b -C -i "tests\Database\SMOKE_PLATFORM.sql"` y obtener `PLATAFORMA_MULTIEMPRESA_OK`.

## Fase 10: ambientes y publicación

1. Ejecutar `Test-DatabaseDeployment.ps1` para Testing y Sandbox y confirmar que ambos poseen `QAADMIN`, `CLI-900001` y `PRD-900001`.
2. Ejecutar `Test-ApiSqlIntegration.ps1` y obtener la confirmación de login, empresa, vista global y logout.
3. Crear un artefacto con `Build-Release.ps1 -Version 10.0.0-local` y abrir `manifest.json`; cada archivo debe tener SHA-256.
4. Confirmar que el ZIP no contenga datos de Development, cadenas productivas, secretos ni datos personales reales.
5. Mantener `TESTING_DEPLOY_ENABLED` y `PRODUCTION_DEPLOY_ENABLED` desactivados hasta disponer de infraestructura y secretos.

## Comprobaciones automatizadas ejecutadas

- `dotnet test OxiTigre.sln --configuration Release`.
- `tests/Database/SMOKE_COMERCIAL.sql`, con rollback final para no conservar datos de prueba.
- `tests/Database/SMOKE_SEGURIDAD_FASE2.sql`, con rollback final para no conservar usuarios ni sesiones de prueba.
- `tests/Database/SMOKE_CONFIGURACION.sql`, con rollback final para no conservar configuración de prueba.
- `tests/Database/SMOKE_INVENTARIO.sql`, con rollback final para no conservar movimientos ni mínimos de prueba.
- `tests/Database/SMOKE_VENTAS.sql`, con rollback final para no conservar pedidos, reservas, ventas ni movimientos.
- `tests/Database/SMOKE_PLATFORM.sql`, de solo lectura para validar el registro central y San Fernando.
- `GET /health` devuelve `Healthy`.
- Comercial sin token devuelve HTTP `401`.
- El flujo API de Seguridad validó empresa, login, roles, listado, edición, logout y token revocado (`401`).

# Revisión visual del Dashboard, Comercial, Inventario, Compras y Trazabilidad

1. Abrir **Comercial**, entrar en **Pedidos y ventas** y presionar **Volver al resumen de Comercial**: debe regresar al
   tablero Comercial conservando la sesión y la selección lateral.
2. Repetir desde Clientes y desde la gestión de Inventario; el retorno debe señalar siempre el módulo correcto.
3. Abrir **Inventario** y verificar los cuatro indicadores, las cinco etapas del flujo, prioridades y actividad.
4. Confirmar que stock físico, reservado y disponible se expresen como posiciones y no sumen unidades incompatibles.
5. Crear un movimiento con varios renglones: la actividad debe mostrar una cabecera y la cantidad de renglones.
6. Probar una posición coherente y otra con reserva superior al físico; solamente la segunda debe solicitar auditoría.
7. Reducir la ventana a 1180 × 720: encabezados y botones no deben superponerse y debe aparecer desplazamiento.
8. Abrir **Compras** y verificar sus cuatro indicadores, las cinco etapas del circuito, prioridades y actividad.
9. Confirmar que el icono del carrito se vea completo y centrado con escalas de Windows de 100 %, 125 % y 150 %.
10. Crear una orden aprobada con entrega esperada para hoy y cantidad pendiente: no debe figurar vencida. Cambiar la
    fecha a ayer: debe aparecer en prioridades.
11. Crear una recepción con varios renglones: la actividad debe mostrar una sola recepción y su cantidad de renglones.
12. Presionar **Abrir gestión de compras** y luego **Volver al resumen de Compras**: debe regresar al tablero sin
    cerrar la sesión ni cambiar empresa o sucursal.
13. Abrir **Trazabilidad** y verificar sus cuatro indicadores, las cinco etapas del circuito, prioridades e historial.
14. Confirmar que el icono de Trazabilidad se vea completo a 100 %, 125 % y 150 % de escala de Windows.
15. Registrar un incidente sobre un activo: debe aumentar la alerta de activos y aparecer un evento inmutable.
16. Crear un préstamo sin fecha prevista y otro vencido: ambos deben solicitar seguimiento, diferenciando el vencido.
17. Completar un mantenimiento con revisión para hoy: debe figurar como revisión pendiente; una fecha futura no.
18. Presionar **Abrir trazabilidad industrial** y después **Volver al resumen de Trazabilidad**: debe conservar sesión,
    empresa y sucursal.
19. Confirmar que Trazabilidad no muestre un mapa; la cartografía debe permanecer en Logística para rutas y paradas.
# Fase 7 — Compras y trazabilidad

1. Crear proveedor con contacto y verificar CUIT duplicado controlado.
2. Crear orden con varios renglones, enviar, aprobar y comprobar que solo borrador es editable.
3. Recibir menos cantidad: aceptado suma stock, rechazado/dañado no; orden queda `RECIBIDA_PARCIAL`.
4. Completar una segunda recepción y comprobar `RECIBIDA`; probar sobreentrega y remito repetido.
5. Cerrar saldo pendiente con motivo y verificar la bitácora.
6. Recibir producto por lote y por serie; las cantidades deben explicar exactamente lo aceptado.
7. Fraccionar CO₂ desde lote grande a cilindros: origen = destinos + merma y todos conservan lote origen.
8. Registrar apertura accidental, fuga o válvula rota; comprobar merma y activo `BLOQUEADO/EN_REVISION`.
9. Completar mantenimiento con componente anterior/nuevo, costo, certificado y próxima revisión.
10. Prestar cilindros vacíos, llenos y parciales a sucursal, cliente y tercero usando entrega propia y retiro; devolver con diferencia e incidente.
11. Verificar que ninguna grilla seleccione la primera fila automáticamente y que ES/EN no recorte textos.
12. Abrir **Ver detalle** y comprobar proveedor, CUIT, condición de pago, importes y cantidades; después modificar el proveedor y verificar que la orden conserve su snapshot original.
13. Cambiar un proveedor a `INACTIVO`: debe seguir visible en documentos históricos y no debe poder elegirse en una orden nueva.
14. Reversar una recepción intacta: debe quedar `REVERSADA`, crearse una salida compensatoria y volver la cantidad a pendiente. Repetir luego de mover o fraccionar el material y comprobar el error controlado `50008`.
15. Probar buscadores, ayuda `?`, máscara de CUIT y separadores numéricos en Compras y Trazabilidad.
16. Ejecutar `sqlcmd -S ".\SQLEXPRESS" -d "OxiTigre_Development" -E -b -C -i "tests\Database\SMOKE_FASE7.sql"` y obtener `SMOKE_FASE7_OK`.
17. Crear una orden: debe abrir maximizada, mostrar títulos y ayuda, permitir agregar/actualizar/quitar renglones y conservar observación al editar.
18. Seleccionar una orden `RECIBIDA_PARCIAL` y presionar **Editar**: debe explicar que solo los borradores se modifican, sin alterar el documento.
19. Probar los filtros **Borradores**, **En curso** y **Finalizadas** y verificar que puedan combinarse.
20. Cerrar sesión y volver a abrir: el último usuario debe aparecer escrito, pero contraseña y empresa deben permanecer vacías.

# Revisión funcional de Logística Fase 8

1. Entrar con `AOCAUZI`, abrir **Logística** y comprobar las cinco pestañas sin selección inicial.
2. Crear un domicilio con contacto, teléfono, horario `12:00` a `14:00` e instrucciones obligatorias.
3. Crear una entrega normal sin activo y comprobar que aparece `PENDIENTE`.
4. Intentar crear `INTERCAMBIO_TEMPORAL` sin serie: debe mostrar un error controlado `60001`.
5. Crear el intercambio indicando un tubo `CLIENTE / RETIRO_CLIENTE` y otro `OXITIGRE / PRESTAMO_TEMPORAL`.
6. Desde Seguridad, verificar el rol `TRANSPORTISTA`; crear o editar su perfil con licencia y vencimiento.
7. Crear un vehículo de empresa con patente anterior `ABC123` y otro particular Mercosur `AB123CD`; deben verse `ABC-123` y `AB-123-CD`.
8. Intentar guardar un vehículo particular sin propietario: debe rechazarse de forma controlada.
9. Seleccionar dos solicitudes con `Ctrl`, crear una hoja `NORMAL`, elegir transportista y vehículo y revisar sus paradas.
10. Intentar crear una hoja `URGENTE` con más de una solicitud: debe rechazarse de forma controlada.
11. Despachar una hoja planificada y verificar solicitud `EN_RUTA` y aviso `TRANSPORTISTA_EN_CAMINO` simulado.
12. Confirmar una parada con resultado y observación; comprobar evento, aviso y estado de custodia.
13. Confirmar una parada `REPROGRAMADA`: debe volver a solicitudes pendientes sin borrar la parada anterior.
14. Cancelar una hoja planificada con motivo y comprobar que queda histórica como `CANCELADA`.
15. Cambiar nombre, perfil o patente luego de crear una hoja y verificar que la hoja histórica conserva el snapshot anterior.
16. Cambiar el domicilio actual y verificar que una parada histórica conserva el domicilio anterior.

# Revisión funcional de Finanzas Fase 11

1. Abrir **Finanzas** y comprobar que todas las columnas estén en español, sin identificadores internos ni `RowVersion`.
2. En **Caja**, crear una caja, abrirla con importe inicial y verificar la apertura bajo la caja física.
3. Intentar abrir otra caja con el mismo usuario: debe recibirse el error controlado `80002`.
4. Registrar un cobro dividido entre efectivo y transferencia; ambos medios deben sumar exactamente el total.
5. Intentar guardar transferencia sin referencia o efectivo sin caja propia abierta: debe rechazarse sin error técnico.
6. Aplicar parcialmente el cobro a una venta y comprobar el saldo pendiente en **Cuenta corriente**.
7. Dejar parte del cobro sin aplicar y comprobar que el saldo restante queda a favor del cliente.
8. Seleccionar un recibo y verificar debajo sus medios y ventas aplicadas.
9. Reversar un cobro confirmado con motivo obligatorio; el original debe conservarse y el saldo compensarse.
10. Cerrar la caja e informar el efectivo contado; comprobar esperado, contado y diferencia.
11. Seleccionar un cliente y comprobar que ventas e historial explican el saldo mostrado.
12. Ejecutar `sqlcmd -S ".\SQLEXPRESS" -d "OxiTigre_Development" -E -b -C -i "tests\Database\SMOKE_FINANZAS.sql"` y obtener `SMOKE_FINANZAS_OK`.
13. Abrir **Nuevo cobro** al 100 %, 125 % y 150 % de escala: ambas mitades deben conservar el mismo ancho.
14. Elegir un cliente sin ventas pendientes: debe explicarse que el cobro queda como saldo a favor y deshabilitarse **Agregar** en la aplicación.
15. Escribir importes en los medios: **Total ARS** debe calcularse automáticamente sin pedir el mismo dato dos veces.

# Revisión funcional de Facturación fiscal Fase 12A

1. Volver a iniciar sesión para actualizar permisos y abrir **Fiscal · Preparación**.
2. Verificar que la pantalla abra maximizada, con columnas en español y sin selección inicial.
3. Comprobar que CUIT, condición frente al IVA, servicio, ambiente, punto de venta, certificado y datos fiscales de clientes indiquen su situación real.
4. Confirmar que no exista acción para emitir, numerar, generar CAE ni marcar una venta como factura.
5. Abrir **Configuración**, completar un dato no sensible, volver y comprobar que **Actualizar diagnóstico** lo refleje.
6. Verificar que las ventas aparezcan una sola vez aunque posean varios renglones.
7. Cambiar a inglés conservando la sesión y volver a abrir: títulos, ayudas, estados y columnas deben traducirse.
8. Ejecutar `sqlcmd -S ".\SQLEXPRESS" -d "OxiTigre_Development" -E -b -C -i "tests\Database\SMOKE_FISCAL_PREPARACION.sql"` y obtener `SMOKE_FISCAL_PREPARACION_OK`.

## Pedidos, Logística y custodia automática

1. Abrir **Comercial · Pedidos y ventas** y comprobar el pedido demostrativo de recarga con un tubo del cliente.
2. Abrir **Logística · Pedidos para planificar** y verificar que figure como **Venta pendiente**.
3. Crear el retiro seleccionando domicilio, fecha y depósito receptor; confirmar que no se solicita reescribir la serie.
4. Crear una hoja con esa solicitud, despacharla y confirmar la parada como entregada.
5. En Trazabilidad, comprobar que el mismo tubo quedó en el depósito y que existe el evento `CLIENTE_INGRESO`.
6. Generar luego la entrega del mismo pedido, completar su parada y comprobar estado `EN_CLIENTE` y evento
   `CLIENTE_ENTREGA`.
7. En una solicitud con más de un activo, confirmar resultado **Parcial** y marcar uno solo; comprobar que el otro no
   cambió de custodia.
8. Registrar un préstamo temporal con devolución prevista y comprobar la cabecera/detalle de préstamo y el estado
   `PRESTADO`.
