# Logística y hojas de ruta

## Objetivo operativo

La Fase 8 organiza solicitudes, recorridos y custodia sin perder la historia. Una hoja de ruta conserva una copia del
cliente, domicilio, contacto, teléfono, ventana horaria, prioridad e instrucciones que estaban vigentes cuando se
armó. Cambiar luego el domicilio del cliente no modifica una hoja histórica.

## Flujo actual

1. Crear y confirmar el pedido cobrable, indicando en cada activo quién coordina su ingreso y su regreso.
2. Registrar uno o más domicilios operativos del cliente, con contacto, teléfono, horario e instrucciones.
3. En **Pedidos para planificar**, generar el retiro o la entrega sin volver a escribir los tubos del pedido.
4. Para una solicitud independiente, indicar fecha, ventana, prioridad, depósito receptor y activos identificados.
5. Crear un usuario con rol `TRANSPORTISTA`, completar su perfil y registrar el vehículo propio o particular.
6. Seleccionar solicitudes pendientes y crear una hoja normal; una hoja urgente admite una sola parada.
7. Elegir **Asignación directa** o **Publicar como oferta**. Una oferta queda sin chofer ni vehículo hasta que la oficina
   la asigne o un transportista habilitado la tome desde el móvil.
8. Al asignar, validar transportista, licencia y vehículo activos. La hoja conserva sus identificadores y también el
   nombre/patente históricos.
9. Despachar la hoja. Las solicitudes pasan a `EN_RUTA` y se generan avisos simulados.
10. Si es necesario, pausar el recorrido con motivo y reanudarlo sin liberar solicitudes, paradas ni carga.
11. Confirmar cada parada como `ENTREGADA`, `PARCIAL`, `FALLIDA` o `REPROGRAMADA`, siempre con observación.
12. Al confirmar todas las paradas, la hoja se completa automáticamente. Una reprogramada vuelve a la agenda.

No se eliminan hojas, paradas ni eventos. Una cancelación conserva la hoja y devuelve sus solicitudes pendientes.

## Integración con Pedidos y Finanzas

Un pedido confirmado o vendido aparece como candidato únicamente cuando contiene un activo con una operación aún no
planificada. `RETIRO_OXITIGRE` crea el retiro del activo del cliente; `ENTREGA_OXITIGRE` crea su devolución, préstamo o
entrega. El pedido es la fuente autoritativa: cliente, activo, serie y modalidad no se duplican manualmente.

Los estados financieros visibles son **Venta pendiente**, **Pendiente**, **Pago parcial** y **Pagado**. Se calculan
desde cobros aplicados en Finanzas y nunca desde una observación escrita por el operador.

## Transportistas, vehículos y patentes

El perfil transportista referencia a un usuario activo que posee el rol `TRANSPORTISTA`; no duplica credenciales.
Allí se informa vínculo laboral, licencia, categoría, vencimiento y contacto. Un vehículo puede pertenecer a OxiTigre
o a un transportista particular. Seguro, revisión técnica y capacidad quedan disponibles para controles operativos.

La patente se persiste sin separadores y la aplicación muestra los formatos argentinos `ABC-123` y `AB-123-CD`.
Cambiar luego el perfil o el vehículo no altera el chofer ni la patente guardados en una hoja histórica.

## Custodia de tubos y activos

`PROPIETARIO` distingue `CLIENTE` de `OXITIGRE`. `ROL` explica el movimiento esperado:

| Rol | Uso |
|---|---|
| `RETIRO_CLIENTE` | OxiTigre recibe temporalmente un tubo ajeno para recarga, reparación o devolución. |
| `PRESTAMO_TEMPORAL` | OxiTigre deja un tubo propio mientras conserva el del cliente. |
| `ENTREGA` | Se entrega material o un activo propio. |
| `DEVOLUCION` | Se devuelve el mismo activo que estaba bajo custodia. |

Al completar una parada, la línea logística queda `EN_CUSTODIA`, `PRESTADO` o `ENTREGADO`. En paralelo, el activo
maestro cambia de forma transaccional: el tubo retirado queda `DISPONIBLE` dentro del depósito receptor, la devolución
queda `EN_CLIENTE`, el préstamo propio queda `PRESTADO` y una venta de activo queda `VENDIDO`. Cada cambio crea un
evento inmutable. Un préstamo temporal también crea su cabecera y detalle formal con devolución prevista.

Si una parada fue parcial, el transportista selecciona exactamente qué activos operó; los restantes conservan su
estado anterior. El número de serie y el propietario impiden tratar el tubo del cliente como stock vendible propio.

## Notificaciones

La tabla `LOGISTICA.NOTIFICACIONES` es una bandeja transaccional. En Development usa canal `SIMULADO`: permite revisar
destinatario y mensaje sin enviar información externa. Un worker y el proveedor real de correo/WhatsApp se agregarán
cuando existan credenciales, consentimiento, plantillas y reglas de reintento aprobadas.

## Archivo histórico de documentos en nube

Las hojas de ruta estructuradas ya quedan guardadas históricamente en SQL Server. Los archivos asociados —PDF de la
hoja, remito, foto, firma, comprobante o documento de pedido— se incorporarán en una etapa futura porque todavía no
se eligió proveedor de nube ni política legal de conservación.

La solución futura guardará el archivo en almacenamiento de objetos privado y solamente estos metadatos en SQL:

- empresa, entidad e identificador relacionados;
- tipo documental y versión;
- clave opaca del objeto, nunca una URL pública permanente;
- nombre original, tipo MIME y tamaño;
- hash SHA-256 para integridad;
- usuario, fecha, estado y plazo de conservación.

El acceso será mediante la API y enlaces temporales autorizados. Producción no guardará documentos dentro del
repositorio ni directamente como campos binarios de las tablas operativas.

## Aplicación del transportista

La aplicación nativa consulta `GET /api/driver`, que devuelve exclusivamente el perfil, sus hojas activas, las ofertas
sin asignar, su historial y sus vehículos elegibles. Un usuario que posee solamente el rol `TRANSPORTISTA` no puede
utilizar la consulta integral de Logística, cancelar hojas ni confirmar paradas de otro chofer. Toma de oferta,
despacho, pausa, reanudación y confirmación vuelven a validar la asignación en el servidor antes de ejecutar el
procedimiento almacenado.

La versión operativa se divide en **Mis rutas**, **Disponibles**, **Historial** y **Mi cuenta**. Permite tomar una oferta
con control atómico, llamar al contacto, abrir el domicilio en el mapa nativo, registrar llegada, pausar/reanudar,
informar incidencias y confirmar un resultado total o parcial por activos exactos con observación obligatoria. Llegada,
incidencia, pausa, reanudación y salida se guardan como eventos inmutables. La interfaz funciona en español e inglés
sin cerrar la sesión.
No se admite una confirmación sin conexión porque todavía no existe una política aprobada para resolver dos cambios
concurrentes. El cliente móvil no forma parte de este repositorio de entrega.

## Instalación incremental

```powershell
# Desde la raíz del repositorio:
powershell -ExecutionPolicy Bypass -File .\scripts\database\Apply-Phase8.ps1 `
  -ServerInstance ".\SQLEXPRESS" `
  -DatabaseName "OxiTigre_Development" `
  -IncludeDevelopmentData
```

Si la base ya tenía la primera versión de Fase 8, usar una sola vez:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\database\Apply-Phase8Transport.ps1 `
  -ServerInstance ".\SQLEXPRESS" `
  -DatabaseName "OxiTigre_Development" `
  -IncludeDevelopmentData
```
