# Finanzas: caja, cobros y cuenta corriente

## Alcance

La Fase 11 registra operaciones internas. No emite facturas, notas fiscales ni reemplaza la documentación tributaria.

## Flujo operativo

1. El administrador configura una caja por sucursal y los medios de pago.
2. El cajero abre su caja informando el efectivo inicial.
3. Registra un cobro, distribuye el total entre uno o más medios y puede aplicarlo a varias ventas pendientes.
4. El saldo no aplicado permanece como crédito del cliente.
5. Al cerrar, informa el efectivo contado; SQL Server calcula el esperado y la diferencia.
6. Una reversión autorizada conserva el cobro, libera sus aplicaciones y agrega un débito compensatorio.

Un cobro con efectivo exige una apertura propia. Transferencias y tarjetas requieren referencia. Los cobros de una
caja cerrada no se reversan: se debe registrar una corrección trazable en una nueva apertura.

## Qué muestra cada pestaña

- **Caja:** arriba aparecen los puntos físicos de cobro; abajo, cada apertura y cierre realizado por un usuario. Una
  caja no contiene dinero por sí sola: el efectivo pertenece a una apertura concreta.
- **Cobros:** arriba aparecen los recibos internos. Al seleccionar uno, abajo se muestran cómo se pagó y a qué
  ventas se aplicó. Transferencia, tarjeta y cheque conservan su referencia.
- **Cuenta corriente:** el saldo es débitos menos créditos. Un saldo positivo significa que el cliente todavía debe;
  uno negativo es crédito a favor. Al seleccionar el cliente se muestran sus ventas pendientes y el historial que
  explica el saldo sin ocultar ni borrar movimientos.
- **Medios de pago:** define efectivo, transferencia, tarjeta, cheque u otro. “Afecta efectivo” suma o resta en el
  arqueo de caja; “Requiere referencia” obliga a informar número de operación, cupón o cheque.

Los identificadores internos y la versión técnica de cada fila existen para seguridad y concurrencia, pero no se
presentan al empleado porque no forman parte de la operación.

## Seguridad

- `FINANZAS.CONSULTAR`: lectura.
- `FINANZAS.COBRAR`: alta de cobros.
- `FINANZAS.CAJA_GESTIONAR`: apertura y cierre propios.
- `FINANZAS.REVERSAR`: reversión compensatoria.
- `FINANZAS.CONFIGURAR`: cajas y medios.

## Próximas fases

La activación remota de URLs, dominio, HTTPS, backups y proveedores de mensajería sigue en la Fase 10 y no bloquea
el uso local. La Fase 12 definirá la facturación fiscal argentina únicamente cuando estén aprobadas las reglas y se
disponga de certificados y credenciales reales.
