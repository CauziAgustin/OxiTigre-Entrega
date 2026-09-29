# Compras y trazabilidad industrial

## Alcance implementado

Compras administra proveedores y contactos, órdenes valorizadas, envío a aprobación, aprobación separada, cancelación, recepción parcial y cierre justificado del saldo. La orden conserva una copia de la razón social, CUIT y condición de pago del proveedor para que una modificación posterior no reescriba el documento histórico. **Ver detalle** presenta cabecera, renglones, cantidades pedidas/recibidas/pendientes e importes.

Una recepción conserva lo entregado, aceptado, rechazado y dañado; únicamente lo aceptado genera entrada de stock. El remito es único por empresa y una sobreentrega se rechaza. Una carga errónea no se elimina: **Reversar recepción** genera un movimiento de salida compensatorio, restituye el saldo de la orden y guarda usuario, fecha y motivo. Solo se permite mientras el stock, lotes y series creados por esa recepción permanezcan intactos.

La trazabilidad separa producto, lote, activo físico reutilizable y contenido actual. Un cilindro mantiene número de serie, propietario, condición, depósito/ubicación, capacidad y contenido. La pantalla dice “información almacenada” porque el dato cambia solamente con una operación confirmada, no por sensores.

Los activos de clientes poseen una relación explícita con `COMERCIAL.CLIENTES`. `EN_CLIENTE` significa que el activo permanece fuera de OxiTigre; `DISPONIBLE` junto con un depósito significa que OxiTigre lo recibió y lo mantiene bajo custodia. El alta, ingreso y entrega generan eventos `CLIENTE_ALTA`, `CLIENTE_INGRESO` y `CLIENTE_ENTREGA` sin alterar stock vendible ni transferir la propiedad.

## Flujos y reglas

- Orden: `BORRADOR → PENDIENTE_APROBACION → APROBADA → RECIBIDA_PARCIAL → RECIBIDA`; también puede cancelarse o cerrar el saldo con motivo.
- Proveedor: la baja es lógica (`ACTIVO/INACTIVO`); los documentos existentes permanecen consultables y un proveedor inactivo no puede utilizarse en una orden nueva.
- Reversión: `CONFIRMADA → REVERSADA`; el documento original y la salida compensatoria quedan vinculados. Si el material ya fue movido, vendido, fraccionado, medido, prestado o intervenido, se rechaza con el error `50008` y debe registrarse una corrección específica.
- Recepción trazada: `NINGUNA` no admite identificadores; `LOTE` exige que los lotes sumen lo aceptado; `SERIE` exige una serie por unidad; `SERIE_LOTE` exige ambos.
- Fraccionamiento: cantidad origen = suma cargada en destinos + merma. El lote origen, cada activo destino, usuario y correlación quedan unidos.
- Incidente: apertura accidental, fuga, rotura, robo, pérdida o componente defectuoso puede generar ajuste y bloquear el activo en revisión.
- Mantenimiento: conserva proveedor, costo, componentes anterior/nuevo, certificado y próxima revisión. Al completarlo el activo vuelve a servicio.
- Préstamo: admite destino sucursal, cliente, tercero o empresa; conserva modalidad `ENTREGA_PROPIA`/`RETIRO_DESTINATARIO`, devolución prevista y snapshots de cantidad/condición de salida y regreso. “Vencido” se deriva de la fecha.
- Activo del cliente: `EN_CLIENTE → DISPONIBLE` al ingresar a un depósito y `DISPONIBLE → EN_CLIENTE` al entregarse. La observación es obligatoria y la versión evita confirmar sobre información desactualizada.

## Seguridad e integración futura

La empresa, sesión y usuario se obtienen de la identidad autenticada. Compras usa `COMPRAS.CONSULTAR`, `COMPRAS.GESTIONAR`, `COMPRAS.APROBAR` y `COMPRAS.RECIBIR`; trazabilidad reutiliza `INVENTARIO.CONSULTAR/GESTIONAR`.

Se guardan referencia de dispositivo, método y origen de medición para sensores futuros, pero no hay telemetría. Se guardan fechas fuente para futuras alertas, pero no hay worker ni mensajes. Una operación interempresa conserva empresa destino y correlación; no modifica otra base. La compensación contable entre empresas queda para una fase financiera posterior.
