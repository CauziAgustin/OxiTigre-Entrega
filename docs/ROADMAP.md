# Roadmap de OxiTigre

## Estado general

| Fase | Alcance | Estado |
|---|---|---|
| 0 | Arquitectura, estándares y herramientas locales | Completada |
| 1 | Base de datos núcleo, seguridad técnica, auditoría y CI/CD inicial | Completada |
| 2 | Login, selección de empresa, sesiones y administración de usuarios | Completada |
| 3 | Comercial: clientes y teléfonos | Lista para prueba funcional |
| 4 | Configuración empresarial y catálogos administrables | Lista para prueba funcional |
| 5 | Inventario | Lista para prueba funcional |
| 6 | Comercial: pedidos y ventas internas | Lista para prueba funcional |
| 7 | Compras y trazabilidad industrial | Lista para prueba funcional |
| 8 | Logística | Lista para prueba funcional |
| 9 | Plataforma multiempresa y vista global | Completada para alcance local |
| 10 | Testing, publicación y operación remota | Alcance local completado; activación remota bloqueada |
| 11 | Caja, cobros y cuenta corriente no fiscal | Lista para prueba funcional |
| 12 | Facturación fiscal argentina | Definición iniciada; pendiente de datos fiscales y homologación |
| 13 | Notificaciones reales y recuperación autoservicio | Pendiente de proveedores externos |
| 14 | Documentos en nube y aplicación móvil | App operativa ampliada lista para prueba; nube pendiente |

## Fase 2: Seguridad completada

Incluye login, selección posterior de empresa, cambio obligatorio de clave, sesiones opacas, cierre y revocación,
bloqueo temporal al quinto intento fallido, listado, alta, edición, estado, múltiples roles y reinicio administrativo
de contraseña temporal. El administrador no puede desactivar su propia cuenta ni quitarse su propio rol.

La administración de usuarios utiliza los roles existentes `ADMINISTRADOR` y `CONSULTA`. La creación de roles y
permisos personalizados se incorporará cuando exista una necesidad funcional concreta. La recuperación autoservicio
por correo o WhatsApp se construirá sobre el motor transversal de notificaciones y se activará únicamente para
contactos verificados; hasta entonces se utiliza el reinicio administrativo, sin exponer tokens de recuperación.

## Fase 3: validar Comercial

El desarrollo está terminado para el alcance actual: cliente, teléfonos, código automático, listado, detalle, alta, edición y estado lógico. Falta la prueba manual registrada en `TEST_CASES.md` y cualquier ajuste visual o funcional que surja de esa revisión.

El dashboard modular y el cierre remoto de sesiones ya están disponibles como base de navegación para los próximos módulos.

Direcciones, condición fiscal, listas de precios, cuentas corrientes, pedidos y ventas no pertenecen a este bloque y se incorporarán con su modelo funcional correspondiente.

## Fase 4: Configuración

1. empresas, sucursales y unidades operativas;
2. catálogos de tipos de teléfono y estados visibles;
3. parámetros del sistema por módulo;
4. administración de módulos y numeración de errores con acceso restringido.
5. preferencia de idioma, recursos español/inglés e importación validada de paquetes JSON según
   `docs/INTERNATIONALIZATION.md`.

El desarrollo está terminado para este alcance. Incluye permisos separados de consulta y gestión, concurrencia
optimista, ausencia de bajas físicas, referencias seguras para secretos, traducciones de catálogos y prueba SQL con
rollback. Falta únicamente la revisión manual de Agustin registrada en `TEST_CASES.md` y los ajustes que surjan de ella.

## Fase 5: Inventario

1. productos, categorías y unidades de medida;
2. depósitos y ubicaciones;
3. existencias por depósito;
4. movimientos, ajustes y trazabilidad;
5. alertas de stock mínimo.

El desarrollo está terminado para el alcance actual. Incluye productos numerados, categorías, unidades de medida,
depósitos, ubicaciones, saldo por depósito, mínimos, alertas y movimientos confirmados de entrada, salida,
transferencia y ajuste. Cada movimiento actualiza saldos y trazabilidad en una sola transacción. La Fase 7 amplió
este núcleo con lotes, números de serie, activos reutilizables, contenido y vencimientos.

## Fase 6: Pedidos y ventas

1. pedidos y renglones;
2. listas de precios, promociones, descuentos e impuestos;
3. estados y transiciones del pedido;
4. reserva y descuento de stock;
5. comprobantes y cuenta corriente cuando se definan las reglas fiscales.

El alcance no fiscal está terminado: listas y precios por producto; promociones **Llevá N, pagá M**, porcentaje sobre
unidades bonificadas y precio fijo por paquete; pedidos borrador; descuentos e impuestos por renglón; confirmación
con reserva; cancelación con liberación; y venta interna con movimiento automático de salida. Las promociones se
definen por empresa y producto, respetan estado y vigencia y son calculadas de forma autoritativa por SQL Server.
Una promoción reemplaza al descuento manual y las entradas compatibles se agrupan para aplicar el beneficio sobre
la cantidad total, incluso cuando el producto se agrega en pasos sucesivos.
El stock físico cambia únicamente al vender; mientras el pedido está confirmado, la reserva reduce el disponible para
otras salidas. El catálogo distingue productos y servicios: recarga, válvula, mano de obra y envío pueden cobrarse en
el mismo pedido sin crear reservas ni movimientos falsos. Los tubos concretos quedan vinculados al renglón, finalidad
y modalidades de ingreso y regreso mediante `COMERCIAL.PEDIDOS_ACTIVOS`. Un pedido confirmado alimenta la agenda
logística sin reescribir los activos ni perder su relación con el servicio cobrado.

La cuenta corriente, cajas, cobros y aplicaciones ya pertenecen a la Fase 11. Los comprobantes fiscales permanecen
separados: la preparación 12A está completa, pero la emisión real requiere definición tributaria y homologación.

Como complemento administrativo existe un panel Blazor protegido por rol `ADMINISTRADOR`, con indicadores y vistas
de supervisión de usuarios, clientes, existencias, pedidos y ventas. Las modificaciones continúan centralizadas en
WinForms hasta aprobar qué operaciones deben exponerse también por navegador.

## Fase 7: Compras

1. proveedores y contactos;
2. solicitudes y órdenes de compra;
3. recepción de mercadería;
4. actualización de stock y costos;
5. estados, autorizaciones y auditoría.

Fase terminada para el alcance local. Incluye proveedores y contactos; órdenes con autorización separada; recepciones
parciales con aceptado/rechazado/dañado; lotes y series; activos reutilizables con contenido; mediciones; genealogía de
fraccionamientos; incidentes y pérdidas; mantenimiento y componentes; préstamos con ambas modalidades y devolución;
historial inmutable, permisos, errores `50001+`/`30005+`, datos de prueba y smoke con rollback.
La revisión funcional agrega snapshot del proveedor en la orden, detalle integral consultable, baja lógica de proveedor,
formato y ayuda de carga, filtros y reversión compensatoria controlada de recepciones intactas.

El motor de alertas, sensores y compensación contable interempresa no se implementaron todavía: se conservaron las
fechas, referencias y correlaciones necesarias y se incorporarán cuando exista el canal/proveedor y una segunda empresa real.

## Fase 8: Logística

1. preparación de pedidos;
2. bultos, rutas y entregas;
3. transportistas;
4. seguimiento de estados e incidencias;
5. confirmación de entrega.
6. ventanas horarias, fechas solicitadas, prioridades y salidas urgentes;
7. retiro, custodia, recarga, intercambio temporal y devolución de tubos de clientes;
8. hoja de ruta manual preparada para optimización automática futura;
9. notificaciones de pedido, programación, salida del transportista, entrega e incidencias.

La primera versión permitirá ordenar rutas manualmente y generar una ruta urgente de una sola parada sin atravesar
la planificación completa. Cada parada conservará fecha solicitada, ventana horaria, prioridad, contacto e
instrucción operativa obligatoria. Se almacenarán coordenadas, duración estimada, capacidades y restricciones para
integrar más adelante un optimizador gratuito o pago sin cambiar el modelo logístico.

Las comunicaciones se generarán como eventos transaccionales en una bandeja de salida. En Development se simularán
sin enviar datos externos. Correo y WhatsApp se habilitarán por empresa cuando existan proveedor, remitente, plantillas,
consentimiento y secretos configurados. El mismo mecanismo permitirá informar creación del pedido, entrega programada,
transportista en camino, resultado de entrega, devolución prevista y material listo para retirar.

El alcance local está implementado: domicilios y ventanas, solicitudes, prioridades, custodia por serie y propietario,
transportistas vinculados a usuarios, vehículos propios o particulares, patente argentina normalizada, hojas normales
o urgentes con asignación directa u oferta abierta, autoselección atómica, pausa/reanudación, snapshot histórico,
despacho, cancelación, confirmación, reprogramación, eventos inmutables, permisos, errores `60001+`, datos
demostrativos y avisos simulados. El detalle funcional y la prueba incremental están en `LOGISTICS.md`.

La integración con Pedidos y Finanzas también está implementada: los pedidos pendientes de retiro o entrega aparecen
como candidatos, muestran su estado de pago calculado desde aplicaciones reales y generan solicitudes autoritativas.
La confirmación total o parcial actualiza el activo maestro, registra eventos inmutables y crea préstamos formales
cuando OxiTigre deja un tubo propio.

El archivo de documentos en nube queda diseñado pero no implementado hasta elegir proveedor, conservación y formatos.
Las hojas estructuradas permanecen históricas desde esta fase; más adelante, PDF, remitos, fotos y firmas usarán
almacenamiento de objetos privado con hash y metadatos versionados en SQL.

## Fase 9: multiempresa

`OxiTigre_Platform` registra empresas, ubicación, identificador local y versión de cada base sin guardar secretos ni
operaciones. La API valida credenciales contra las bases registradas, muestra la empresa después de las credenciales,
asocia el token a la elegida y enruta todos los módulos exclusivamente a esa conexión. Usuarios, roles, permisos y
sesiones quedan aislados por empresa.

El panel Blazor ofrece a administradores una vista global de indicadores y conserva disponibles las demás empresas
si una base falla. La implementación local registra OxiTigre y fue verificada extremo a extremo. San Fernando se creó
como sucursal de OxiTigre —no como empresa— con unidad operativa, depósito y ubicación inicial. Una segunda empresa
real se agrega al registro cuando exista su base operativa y sus usuarios. Detalle en `MULTI_COMPANY.md`.

## Fase 10: ambientes y despliegue

La preparación local admite instalaciones independientes para Development, Testing, Sandbox y Production. Testing
y Sandbox poseen Platform propia y datos exclusivamente ficticios; Producción rechaza los datos de demostración.
Una prueba ejecutable valida API, SQL, autenticación, aislamiento y cierre de sesión. Los workflows construyen un
artefacto inmutable con binarios, instaladores y manifiesto SHA-256.

1. instalador SQL declarativo, validación ScriptDom y cambios incrementales versionados;
2. Testing y Sandbox con datos ficticios;
3. pruebas de integración API/SQL y lista de pruebas de interfaz prioritarias;
4. elegir servidor y proveedor;
5. publicar API por HTTPS y mantener SQL Server privado;
6. configurar backups, restauración probada, monitoreo, secretos y alertas;
7. habilitar los workflows de despliegue actualmente bloqueados;
8. desplegar Production con aprobación manual y rollback documentado.
9. configurar dominio remitente de correo y validar entregabilidad;
10. conectar WhatsApp Business y aprobar las plantillas operativas;
11. habilitar recuperación autoservicio por contacto verificado, con límites y auditoría.

Los puntos 1 a 3 y la generación de artefactos están completos localmente. Los puntos 4 a 11 requieren decisiones,
cuentas, dominio, servidores y secretos externos que Agustin todavía no proporcionó; por seguridad los workflows
continúan bloqueados aunque ya generan exactamente el paquete que se desplegará.

## Orden inmediato recomendado

1. Agustin ejecuta, cuando pueda, los casos manuales pendientes documentados en `TEST_CASES.md`.
2. Registrar los ajustes visuales o funcionales detectados en esa revisión.
3. Elegir infraestructura remota para activar el despliegue de Testing sin exponer SQL Server.

## Fase 11: caja, cobros y cuenta corriente

El alcance local está implementado. Una venta interna genera un débito inmutable; un cobro puede dividirse entre
varios medios y aplicarse a una o varias ventas. El saldo no aplicado queda a favor del cliente. Las cajas físicas
se separan de sus aperturas, el cierre calcula efectivo esperado, contado y diferencia, y una reversión compensa sin
borrar el recibo original. El rol `CAJERO` puede consultar, cobrar y manejar únicamente su propia apertura.

Los recibos son comprobantes internos y no tienen validez fiscal. Numeración fiscal, condición impositiva,
CAE/CAEA, certificados e integración tributaria pertenecen a la Fase 12 y no se activarán con datos supuestos.

## Fase 12: facturación fiscal argentina

La frontera funcional y los requisitos oficiales están documentados en `FISCAL_PHASE12.md`. El incremento 12A ya
incorpora el módulo **Fiscal · Preparación**, parámetros no sensibles, permiso propio y diagnóstico de ventas
candidatas. La emisión sigue bloqueada antes de generar modelos o numeración supuestos: requiere condición frente
al IVA, CUIT emisor, categorías fiscales de clientes, tipos de comprobante, servicio aplicable, punto de venta y
certificado de homologación confirmados.

## Fase 14: aplicación móvil y documentos

La aplicación móvil queda fuera de este repositorio de entrega. El backend conserva las operaciones logísticas
necesarias para un futuro cliente transportista; su interfaz, instalación y prueba se gestionan en el repositorio de
desarrollo. El archivo de documentos, fotografías, firma, ubicación en vivo, modo sin conexión y avisos push siguen
pendientes de proveedor, consentimiento, retención y reglas de sincronización.

## Cierre final del producto

Esta tarea se ejecutará última, después de terminar y aprobar todos los módulos: retirar avisos temporales, textos
técnicos y referencias visibles a herramientas de desarrollo o Git —incluidas las ayudas de preparación fiscal—,
sin eliminar advertencias funcionales, legales o de seguridad que el usuario necesite para operar correctamente.
