# Manual de usuario: Inicio y navegación

## Objetivo

El Inicio reúne el estado operativo de la empresa y sucursal elegidas. La barra lateral, la búsqueda, los avisos y el
usuario conectado permanecen visibles al ingresar en cualquier módulo.

## Información del Inicio

- **Flujo operativo:** resume pedidos abiertos, posiciones con stock disponible, hojas activas del día, paradas
  completadas y ventas con saldo pendiente.
- **Lo que necesita una decisión:** prioriza faltantes de stock, compras abiertas, solicitudes logísticas, préstamos
  vencidos, cobros pendientes y avisos fallidos cuando existen.
- **Actividad reciente:** combina los últimos eventos confirmados de Logística, Trazabilidad, Finanzas, Inventario y
  Comercial, ordenados por fecha. No los presenta como actividad de hoy cuando corresponden a días anteriores ni
  reemplaza los historiales completos de cada módulo.
- **Actualizar datos:** vuelve a consultar la API sin cerrar la pantalla.

Si un módulo no está autorizado o no responde, el resto del Inicio continúa disponible y la barra inferior informa que
los datos son parciales.

## Navegación

Cada opción lateral abre primero un paneo del módulo con indicadores, una explicación y sus acciones disponibles:

- **Comercial:** clientes, listas de precios, promociones, pedidos y ventas.
- **Inventario:** productos, depósitos, existencias y movimientos.
- **Compras:** proveedores, órdenes y recepciones.
- **Trazabilidad:** lotes, activos, mediciones, fraccionamientos, incidentes, mantenimientos y préstamos.
- **Logística:** domicilios, solicitudes, transportistas, vehículos, hojas de ruta, historial y avisos.
- **Finanzas:** cajas, cobros, cuenta corriente, medios de pago y preparación fiscal autorizada.
- **Seguridad:** usuarios y sesiones; solamente está habilitada para administradores.
- **Configuración:** empresa, sucursales y parámetros operativos.

Las pantallas operativas se abren dentro del panel principal. Para volver al paneo de un módulo, se presiona nuevamente
su opción lateral; para volver al resumen general, se presiona **Inicio**.

Al entrar en una pantalla operativa aparece además **Volver al resumen de…** sobre su contenido. Esta acción regresa
al tablero del módulo sin cerrar la sesión ni cambiar la empresa o sucursal.

## Tablero Comercial

- Resume clientes disponibles, listas y promociones vigentes, y ventas del día.
- El circuito permite entrar a Clientes, Precios, Borradores, Pedidos por vender y Ventas.
- Las prioridades advierten sobre precios faltantes, borradores, pedidos confirmados y promociones próximas a vencer.
- La actividad combina los últimos pedidos y ventas sin repetir sus renglones.

## Tablero Inventario

- Resume productos y depósitos activos, posiciones disponibles y existencias bajo mínimo.
- El flujo distingue posiciones con stock físico, reservado y disponible, además de movimientos confirmados hoy.
- Las prioridades detectan mínimos incumplidos, cantidades incompatibles y productos activos todavía sin existencias.
- La actividad agrupa cada movimiento una sola vez e informa cuántos renglones contiene.

El tablero no suma cantidades de unidades distintas: por ejemplo, kilogramos y unidades se mantienen separados en la
gestión detallada de Inventario.

## Tablero Compras

- Resume proveedores activos, órdenes en curso, órdenes que admiten recepción y sus renglones pendientes.
- El circuito distingue proveedores, borradores editables, órdenes por aprobar, órdenes por recibir y recepciones
  confirmadas durante el día.
- Las prioridades señalan falta de proveedores activos, aprobaciones pendientes, entregas vencidas, recepciones
  parciales y mercadería rechazada o dañada.
- La actividad agrupa cada orden y cada recepción una sola vez, aunque el documento contenga varios renglones.

Una entrega esperada para el día actual todavía está dentro de plazo. Se considera vencida desde el día siguiente,
siempre que la orden conserve cantidad pendiente y admita nuevas recepciones.

## Tablero Trazabilidad

- Resume activos trazados, lotes con saldo, activos que requieren atención y préstamos abiertos.
- El circuito conecta lotes, activos disponibles, fraccionamientos del día, custodia externa e historial inmutable.
- Las prioridades señalan activos bloqueados o dañados, préstamos vencidos o sin fecha de devolución,
  mantenimientos/revisiones y contenido cuyo producto o lote de origen estén incompletos.
- La actividad muestra una vez cada evento confirmado e informa los cambios de estado y cantidad registrados.

El mapa pertenece al tablero de Logística, donde representa domicilios, paradas y recorridos. Trazabilidad utiliza el
historial de eventos del activo para reconstruir su recorrido físico y documental sin mezclar ambos conceptos.

## Búsqueda y contexto

- La búsqueda superior encuentra módulos y acciones, por ejemplo `clientes`, `recepciones` o `hojas de ruta`.
- `Ctrl+K` lleva el foco directamente a la búsqueda.
- El selector superior muestra la empresa y sucursal actuales. Cambiar ese contexto exige volver al acceso para no
  reutilizar una sesión de otra sucursal.
- La campana lleva a los asuntos que necesitan una decisión y muestra su cantidad.
- **Cerrar sesión** invalida la sesión actual y vuelve al acceso.
- La **X** cierra la sesión y finaliza la aplicación.

## Sesión revocada

La aplicación comprueba la sesión cada cinco segundos. Si un administrador la revoca, aparece un aviso con una cuenta
regresiva de quince segundos. Una falla temporal de red no cierra la aplicación: solamente una respuesta confirmada de
sesión no vigente inicia el cierre.
