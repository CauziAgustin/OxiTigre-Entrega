# Pruebas de base de datos

Pruebas transaccionales disponibles:

- `SMOKE_SEGURIDAD_FASE2.sql`: usuarios, múltiples roles, edición, claves, bloqueo, cierre y revocación de sesiones.
- `SMOKE_COMERCIAL.sql`: clientes, teléfonos, códigos y estados.
- `SMOKE_ERROR_MESSAGES.sql`: código obligatorio, descripción central y mensaje personalizado.
- `SMOKE_CONFIGURACION.sql`: empresa, concurrencia, sucursales, unidades, parámetros, idioma y correlativo de errores.
- `SMOKE_INVENTARIO.sql`: saldos, mínimos, concurrencia y stock insuficiente.
- `SMOKE_VENTAS.sql`: promociones 2x1/4x3, porcentajes, paquetes, acumulación, errores controlados, pedido, reserva y venta inmutable.
- `SMOKE_PLATFORM.sql`: registro central, aislamiento operativo y sucursal San Fernando; es una comprobación de solo lectura.

Las pruebas no conservan datos: las que realizan escrituras finalizan con `ROLLBACK`.
