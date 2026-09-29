# Diseño y ciclo de vida de la base de datos

## Estrategia

La base se define mediante scripts versionados, validación ScriptDom y despliegue ordenado con
`scripts/database/Deploy-Database.ps1`. Las migraciones incrementales se registran por archivo y hash SHA-256; un
SQL Project se evaluará cuando todos los objetos puedan expresarse declarativamente sin duplicar esta fuente.

No se aplicarán cambios manuales sin su correspondiente definición versionada.

Cada empresa y cada ambiente se despliegan en bases separadas. La guía operativa se encuentra en [`DATABASE_DEPLOYMENT.md`](DATABASE_DEPLOYMENT.md).

## Esquemas operativos

- `SEGURIDAD`
- `CONFIGURACION`
- `INVENTARIO`
- `COMERCIAL`
- `COMPRAS`
- `LOGISTICA`
- `AUDITORIA`

## Identificadores

Las claves primarias físicas utilizarán `BIGINT IDENTITY`, salvo justificación documentada. El módulo se conservará como dato explícito o se deducirá del schema y la entidad.

Una referencia funcional podrá presentar el identificador y el módulo sin convertirse en PK ni FK. La PK física permanece estable y no codifica reglas de negocio modificables.

## Homogeneidad

El diccionario oficial de columnas, tipos y nulabilidad se encuentra en [`DATA_DICTIONARY.md`](DATA_DICTIONARY.md). Antes de agregar una columna debe comprobarse si el concepto ya existe. Las FK reutilizan exactamente el nombre y tipo de la PK referenciada.

## Errores

El código funcional de error se forma mediante:

```text
CODIGO_ERROR = CODIGO_MODULO * 10000 + NUMERO_ERROR
```

Ejemplos: `10001`, `120001`, `1230001`. Los últimos cuatro dígitos son el consecutivo del error; los dígitos anteriores identifican el módulo. Se admitirán módulos 1 a 999 y errores 1 a 9999 por módulo.

`AUDITORIA.SP_ERROR_CREATE` asigna el correlativo dentro de una transacción serializable. `AUDITORIA.SP_ERROR_GET_BY_CODE` devuelve módulo, diagnóstico, causa, acción recomendada y solución preferida.

## Núcleo físico implementado

| Esquema | Tablas actuales | Relaciones principales |
|---|---|---|
| `CONFIGURACION` | `EMPRESAS`, `SUCURSALES`, `UNIDADES_OPERATIVAS`, `MODULOS`, `PARAMETROS_SISTEMA`, `ESTADOS`, `TIPOS_TELEFONO`, `TIPOS_DOCUMENTO`, `PAISES`, `UNIDADES_MEDIDA` | Empresa 1:N Sucursal; Sucursal 1:N Unidad; Módulo 1:N Parámetro; catálogos compartidos independientes |
| `SEGURIDAD` | `USUARIOS`, `ROLES`, `PERMISOS`, tablas N:M, `DISPOSITIVOS_ACCESO`, `SESIONES`, `RECUPERACIONES_CLAVE` | Usuario N:M Rol; Rol N:M Permiso; Usuario 1:N Sesión y Recuperación |
| `COMERCIAL` | `CLIENTES`, teléfonos/borradores, `LISTAS_PRECIOS`, precios, `PROMOCIONES`, `PEDIDOS`, detalles, `VENTAS`, detalles | Empresa 1:N Cliente/Lista/Promoción/Pedido/Venta; Producto 1:N Promoción; Lista N:M Producto; Pedido 1:N Detalle; Pedido 1:0..1 Venta |
| `INVENTARIO` | categorías, productos, depósitos, ubicaciones, existencias, movimientos, reservas, lotes, activos/contenidos, mediciones, transformaciones, incidentes, mantenimientos, préstamos y eventos | Producto/Depósito 1:1 Existencia; Lote 1:N saldos; Activo 1:N medición/evento; Transformación une lote origen con activos destino |
| `COMPRAS` | proveedores/contactos, órdenes/detalles y recepciones/detalles | Proveedor 1:N Orden; Orden 1:N recepción; recepción aceptada 1:1 movimiento de entrada |
| `AUDITORIA` | `CATALOGO_ERRORES`, `ERRORES_APLICACION`, `SOLUCIONES_ERROR`, `LOGUEO_FUNCIONALIDADES`, `BITACORA_CAMBIOS_ESTADO` | Módulo 1:N Error; Error 1:N Solución; Sesión/Usuario 1:N trazas |

La base central utiliza únicamente `PLATAFORMA.EMPRESAS_BASES`; las bases operativas incorporan además `LOGISTICA`
para solicitudes, rutas, paradas, transportistas, vehículos, eventos y notificaciones simuladas.

## Stock

- `INVENTARIO.MOVIMIENTOS` y `MOVIMIENTOS_DETALLES` forman el registro histórico inmutable.
- `INVENTARIO.EXISTENCIAS` representa el saldo actual por producto y depósito.
- El Kardex se expondrá como vista o proyección verificable.
- Las reservas se mantienen separadas del saldo físico y reducen el disponible de nuevas salidas.

## Promociones comerciales

`COMERCIAL.PROMOCIONES` define reglas por `ID_EMPRESA + ID_PRODUCTO`, con estado y vigencia. Admite
`CANTIDAD_PAGADA`, `PORCENTAJE_UNIDADES` y `PRECIO_PAQUETE`; sus restricciones garantizan que cada tipo conserve solo
los parámetros que necesita. Un detalle de pedido admite una promoción o un descuento manual, nunca ambos.

`COMERCIAL.SP_PEDIDO_SAVE` es la autoridad de cálculo. Agrupa entradas compatibles por producto, depósito, precio,
descuento manual, impuesto y promoción, aplica el beneficio sobre la cantidad acumulada y guarda el resultado monetario. La venta copia
el identificador, código y nombre promocional junto con los importes del pedido; una modificación posterior de la regla
no altera la operación histórica.

## Caja

`CAJAS` representa la caja física. `SESIONES_CAJA` representa cada apertura y cierre. Solo podrá existir una sesión abierta por caja.

## Sandbox

Las pruebas libres se ejecutarán en `OxiTigre_<EMPRESA>_Sandbox`, nunca en un schema abierto dentro de Producción. La base Sandbox utilizará datos ficticios o anonimizados.
