# Inventario

## Alcance completado

- Productos con código automático `PRD-000001`, categoría, unidad, código de barras y estado lógico.
- Categorías y unidades de medida administrables, incluida la indicación de cantidades decimales.
- Depósitos de la empresa y ubicaciones internas.
- Existencias físicas, reservas, disponible y stock mínimo por producto y depósito.
- Movimientos confirmados de entrada, salida, transferencia, ajuste de entrada y ajuste de salida.
- Alerta visible cuando el disponible es menor o igual al mínimo.
- Concurrencia optimista y errores controlados `30001` a `30004`.

## Reglas funcionales

- Consultar requiere `INVENTARIO.CONSULTAR`; modificar o mover stock requiere `INVENTARIO.GESTIONAR`.
- La empresa, el usuario y la sesión siempre provienen del token autenticado.
- Un movimiento contiene entre 1 y 100 renglones y cada cantidad debe ser positiva.
- Una salida, transferencia o ajuste de salida nunca puede dejar saldo negativo.
- El encabezado, los renglones y todos los saldos se confirman o revierten en una única transacción.
- Los movimientos confirmados no se editan ni eliminan. Una corrección se realiza mediante otro ajuste trazable.
- No hay bajas físicas de maestros; se utiliza `CODIGO_ESTADO`.
- Una categoría o unidad con productos activos no puede inactivarse. Un producto o depósito con saldo tampoco puede inactivarse.
- Las reservas de pedidos reducen el disponible sin modificar el saldo físico; la venta aplica su reserva y genera la salida.
- Lotes, series y vencimientos se incorporarán solo cuando exista una necesidad funcional aprobada.

## Componentes

- WinForms: `InventoryManagementForm` e `InventoryMovementForm`.
- API: `/api/inventory`, maestros, mínimos y `/api/inventory/movements`.
- BLL/DAL: `InventoryService` y `SqlInventoryStore`.
- SQL Server: tablas de `INVENTARIO`, `CONFIGURACION.UNIDADES_MEDIDA` y siete SP públicos.

## Verificación

```powershell
dotnet test OxiTigre.sln --configuration Release
sqlcmd -S ".\SQLEXPRESS" -d "OxiTigre_Development" -E -b -C -i "tests\Database\SMOKE_INVENTARIO.sql"
```

La prueba SQL usa una transacción y termina con `ROLLBACK`.
