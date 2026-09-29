# Diccionario de datos y columnas homogéneas

## Regla principal

Una misma información debe conservar exactamente el mismo nombre, tipo, nulabilidad y significado en todas las tablas. Una FK utiliza el mismo nombre y tipo que la PK referenciada.

No se crearán variantes como `IDCLIENTE`, `CLIENTE_ID`, `COD_CLIENTE` o `ID_CLI` para representar `ID_CLIENTE`.

`ID_USUARIO_ALTA` e `ID_USUARIO_MODIFICACION` son referencias lógicas homogéneas de auditoría y no poseen FK física. Esta excepción evita un ciclo de arranque al crear el primer usuario y permite conservar la trazabilidad histórica aunque una identidad sea archivada. Las relaciones funcionales, como `ID_USUARIO` en sesiones, sí utilizan FK.

## Columnas estructurales obligatorias

Salvo una excepción aprobada y documentada expresamente, todas las tablas utilizarán:

| Columna | Tipo SQL Server | Nulabilidad | Significado |
|---|---|---:|---|
| `ID_<ENTIDAD>` | `BIGINT IDENTITY(1,1)` | No | PK física estable |
| `CODIGO_ESTADO` | `NVARCHAR(30)` | No | Código legible del estado actual |
| `FECHA_ALTA_UTC` | `DATETIME2(3)` | No | Momento de creación en UTC |
| `ID_USUARIO_ALTA` | `BIGINT` | No | Usuario responsable de la creación |
| `FECHA_MODIFICACION_UTC` | `DATETIME2(3)` | Sí | Última modificación en UTC |
| `ID_USUARIO_MODIFICACION` | `BIGINT` | Sí | Usuario responsable de la modificación |
| `ROW_VERSION` | `ROWVERSION` | No | Control de concurrencia optimista |

`CODIGO_ESTADO` siempre será `NVARCHAR(30) NOT NULL`. Sus valores se escriben en mayúsculas con guion bajo y deben existir en `CONFIGURACION.ESTADOS` para la entidad correspondiente.

El estado representa el ciclo de vida real de cada registro. Por ejemplo, un maestro utiliza `ACTIVO` o `INACTIVO`, mientras que un pedido puede utilizar `PENDIENTE`, `APROBADO`, `ENTREGADO` o `ANULADO`.

## Convenciones de nombres

- PK: `ID_<ENTIDAD_SINGULAR>`.
- FK: exactamente el nombre de la PK referenciada.
- Códigos funcionales: `CODIGO` o `CODIGO_<CONCEPTO>`.
- Catálogos de estado: `ID_ESTADO_<ENTIDAD>`.
- Fechas funcionales: `FECHA_<EVENTO>_UTC` cuando incluyen hora; `FECHA_<EVENTO>` cuando son solo una fecha civil.
- Booleanos: `ES_`, `TIENE_` o `PERMITE_`. La vigencia o etapa funcional se expresa mediante `CODIGO_ESTADO`.
- Orden visible o prioridad: `ORDEN SMALLINT` comenzando en `0`.
- Texto libre corto: `OBSERVACION NVARCHAR(500)`.
- Descripciones: `DESCRIPCION NVARCHAR(500)`.
- Correos: `EMAIL NVARCHAR(254)`.
- Teléfonos y documentos: texto, nunca tipos numéricos, porque pueden contener ceros iniciales. La aplicación elimina separadores visuales antes de guardar.
- Importes: `DECIMAL(19,4)`.
- Cantidades generales: `DECIMAL(19,4)`; mediciones técnicas podrán usar `DECIMAL(19,6)` si queda documentado.
- Porcentajes: `DECIMAL(9,6)` almacenando el valor decimal, por ejemplo `0.210000` para 21 %.

## Catálogo informativo de estados

`CONFIGURACION.ESTADOS` documentará los códigos permitidos por entidad sin convertirse en FK, según la decisión funcional. Contendrá `ENTIDAD`, `CODIGO_ESTADO`, `NOMBRE`, `DESCRIPCION`, indicadores de estado inicial/final y orden.

Al no existir FK, todo SP que inserte o modifique información debe validar `ENTIDAD + CODIGO_ESTADO`. Las operaciones directas quedan fuera del contrato soportado.

## Tabla de teléfonos de clientes

Se incorpora `COMERCIAL.CLIENTES_TELEFONOS` para permitir cero, uno o muchos números por cliente.

| Columna | Tipo | Nulo | Regla |
|---|---|---:|---|
| `ID_CLIENTE_TELEFONO` | `BIGINT IDENTITY(1,1)` | No | PK |
| `ID_CLIENTE` | `BIGINT` | No | FK a `COMERCIAL.CLIENTES` |
| `ID_TIPO_TELEFONO` | `BIGINT` | No | FK a `CONFIGURACION.TIPOS_TELEFONO` |
| `CODIGO_PAIS` | `NVARCHAR(5)` | Sí | Ejemplo `54` |
| `CODIGO_AREA` | `NVARCHAR(10)` | Sí | Conserva ceros iniciales |
| `NUMERO` | `NVARCHAR(20)` | No | Número local sin convertir a valor numérico |
| `INTERNO` | `NVARCHAR(10)` | Sí | Interno o extensión |
| `ORDEN` | `SMALLINT` | No | `0` primero, `1` segundo, etc. |
| `ES_PRINCIPAL` | `BIT` | No | Solo un teléfono activo principal por cliente |
| `PERMITE_WHATSAPP` | `BIT` | No | Indica si admite contacto por WhatsApp |
| `OBSERVACION` | `NVARCHAR(500)` | Sí | Contexto adicional |
| `CODIGO_ESTADO` | `NVARCHAR(30)` | No | Estado actual, inicialmente `ACTIVO` |
| `FECHA_ALTA_UTC` | `DATETIME2(3)` | No | Auditoría de creación |
| `ID_USUARIO_ALTA` | `BIGINT` | No | Auditoría de creación |
| `FECHA_MODIFICACION_UTC` | `DATETIME2(3)` | Sí | Auditoría de modificación |
| `ID_USUARIO_MODIFICACION` | `BIGINT` | Sí | Auditoría de modificación |
| `ROW_VERSION` | `ROWVERSION` | No | Concurrencia |

### Tipo y orden no son lo mismo

`ID_TIPO_TELEFONO` identifica la naturaleza del número, por ejemplo:

- `1`: Móvil;
- `2`: Fijo;
- `3`: Laboral;
- `4`: WhatsApp;
- `5`: Emergencia.

`ORDEN` identifica la posición solicitada: `0` es el primero, `1` el siguiente y así sucesivamente. Separarlos evita mezclar clasificación con prioridad.

### Restricciones previstas

- `ORDEN >= 0`.
- Un mismo cliente no puede repetir el mismo `ORDEN` entre teléfonos con `CODIGO_ESTADO = 'ACTIVO'`.
- Un cliente solo puede tener un teléfono con `CODIGO_ESTADO = 'ACTIVO'` y `ES_PRINCIPAL = 1`.
- No se permite un teléfono sin `NUMERO`.
- La baja se realiza asignando `CODIGO_ESTADO = 'INACTIVO'`; no se elimina físicamente salvo procedimiento técnico autorizado.

## Catálogos de carga de clientes

`CONFIGURACION.TIPOS_DOCUMENTO` define los documentos seleccionables. `CODIGO` y `NOMBRE` son homogéneos; `APLICA_PERSONA_FISICA` y `APLICA_PERSONA_JURIDICA` permiten filtrar la lista según el tipo de persona sin mostrar códigos internos al usuario.

`CONFIGURACION.PAISES` define `CODIGO`, `NOMBRE`, `CODIGO_TELEFONICO` y `ES_PREDETERMINADO`. Argentina se entrega con código telefónico `54`; la opción `OTRO` habilita el ingreso manual. Solo puede existir un país activo predeterminado.

## Borradores de clientes

`COMERCIAL.CLIENTES_BORRADORES` conserva una precarga incompleta por usuario y empresa. El contenido se almacena como JSON porque todavía no representa una entidad comercial válida. Un borrador activo usa `BORRADOR`; al crear el cliente o descartarlo pasa a `DESCARTADO`. Los borradores no reemplazan las validaciones finales del alta.

## Proceso para agregar columnas

Antes de crear una columna nueva se debe buscar el concepto en este diccionario. Si ya existe, se reutiliza su nombre y tipo. Si no existe, primero se incorpora aquí y después se utiliza en tablas, SP, contratos y código.

## Inventario

- `CANTIDAD DECIMAL(19,4) NOT NULL` representa saldo o cantidad física positiva en la unidad del producto.
- `STOCK_MINIMO DECIMAL(19,4) NOT NULL` representa el umbral de alerta por producto y depósito.
- `TIPO_MOVIMIENTO NVARCHAR(30) NOT NULL` admite `ENTRADA`, `SALIDA`, `TRANSFERENCIA`, `AJUSTE_ENTRADA` y `AJUSTE_SALIDA`.
- `FECHA_MOVIMIENTO_UTC DATETIME2(3) NOT NULL` identifica el momento funcional del movimiento en UTC.
- `CODIGO_BARRAS NVARCHAR(80) NULL` conserva el identificador leído como texto, incluidos ceros iniciales.

`INVENTARIO.EXISTENCIAS` mantiene una fila única por `ID_PRODUCTO + ID_DEPOSITO`. El saldo se modifica únicamente
mediante `INVENTARIO.SP_MOVIMIENTO_CREATE`; `SP_EXISTENCIA_MINIMO_UPDATE` cambia el umbral sin alterar la cantidad.
Los movimientos confirmados y sus detalles forman la trazabilidad y no se editan físicamente.

## Pedidos, precios y ventas

- `MONEDA CHAR(3) NOT NULL` conserva el código ISO en mayúsculas, por ejemplo `ARS`.
- `PRECIO_UNITARIO`, `SUBTOTAL`, `IMPORTE_DESCUENTO`, `IMPORTE_IMPUESTO` y `TOTAL` usan `DECIMAL(19,4)`.
- `PORCENTAJE_DESCUENTO` y `PORCENTAJE_IMPUESTO` usan `DECIMAL(9,6)` entre `0` y `1`; la pantalla los presenta como porcentaje entre `0` y `100`.
- `FECHA_PEDIDO_UTC` y `FECHA_VENTA_UTC` usan `DATETIME2(3)` en UTC.
- `TIPO_PROMOCION NVARCHAR(30) NOT NULL` admite `CANTIDAD_PAGADA`, `PORCENTAJE_UNIDADES` o `PRECIO_PAQUETE`.
- `CANTIDAD_REQUERIDA`, `CANTIDAD_PAGADA` y `CANTIDAD_BONIFICADA` usan `DECIMAL(19,4)`; solo las columnas requeridas por el tipo contienen valor.
- `PRECIO_PAQUETE DECIMAL(19,4)` y `PORCENTAJE_DESCUENTO DECIMAL(9,6)` conservan el beneficio configurado cuando corresponde.
- `INVENTARIO.PRODUCTOS.TIPO_ITEM` admite `PRODUCTO` o `SERVICIO`. Un servicio no usa depósito, existencia,
  trazabilidad física, préstamo ni mantenimiento de stock.
- `COMERCIAL.PEDIDOS_ACTIVOS` relaciona pedido, activo y producto del renglón. `TIPO_VINCULO` admite
  `CLIENTE_SERVICIO`, `VENTA_ACTIVO`, `PRESTAMO` e `INTERCAMBIO`; `MODALIDAD_RETORNO` admite
  `RETIRO_CLIENTE`, `ENTREGA_OXITIGRE` y `NO_APLICA`.

`COMERCIAL.PEDIDOS` tiene detalles activos y transita `BORRADOR → CONFIRMADO → VENDIDO`, o a `CANCELADO` desde
borrador/confirmado. `INVENTARIO.RESERVAS_STOCK` conserva el compromiso separado de `EXISTENCIAS`: `RESERVADA`,
`LIBERADA` o `APLICADA`. `COMERCIAL.VENTAS` copia de forma inmutable los importes del pedido y referencia el
movimiento `SALIDA` que descontó el stock cuando posee productos físicos. Una venta compuesta únicamente por
servicios conserva `ID_MOVIMIENTO = NULL` porque no debe inventar un movimiento de existencias.

`COMERCIAL.PROMOCIONES` pertenece a `ID_EMPRESA + ID_PRODUCTO` y utiliza `FECHA_VIGENCIA_DESDE` y
`FECHA_VIGENCIA_HASTA` para límites civiles opcionales. `ID_PROMOCION` vincula el beneficio elegido; el código, nombre
e importes aplicados se conservan en pedido y venta como fotografía histórica. Una promoción es incompatible con un
descuento manual en el mismo renglón.

Al guardar un pedido, SQL Server reúne únicamente renglones con iguales `ID_PRODUCTO`, `ID_DEPOSITO`,
`PRECIO_UNITARIO`, `PORCENTAJE_DESCUENTO`, `PORCENTAJE_IMPUESTO` e `ID_PROMOCION`. La suma de `CANTIDAD` determina cuántos grupos completos
reciben el beneficio; la cantidad restante conserva el precio normal.

## Preferencias y traducciones

## Compras y trazabilidad

- `CANTIDAD_PEDIDA`, `CANTIDAD_RECIBIDA`, `CANTIDAD_CERRADA`, `CANTIDAD_ACEPTADA`, `CANTIDAD_RECHAZADA` y `CANTIDAD_DANADA` usan `DECIMAL(19,4)`.
- `TIPO_TRAZABILIDAD` admite `NINGUNA`, `LOTE`, `SERIE` y `SERIE_LOTE`.
- `NUMERO_SERIE NVARCHAR(100)` identifica el activo físico; `CODIGO_LOTE_PROVEEDOR NVARCHAR(100)` conserva la referencia externa.
- `ACTIVOS_CONTENIDOS` guarda producto, lote y cantidad dentro del envase sin confundir contenido con el activo.
- `METODO_MEDICION` y `ORIGEN_MEDICION` distinguen `MANUAL`, `PESAJE`, `CALCULADO` y la futura fuente `SENSOR`.
- `FECHA_ENTREGA_ESPERADA`, `FECHA_DEVOLUCION_PREVISTA` y `FECHA_PROXIMA_REVISION` son datos fuente de alertas; vencido se deriva y no se guarda como estado.
- `ID_CORRELACION` une eventos inmutables; `EMPRESA_DESTINO_CODIGO` e `ID_CORRELACION_INTEREMPRESA` preparan integración sin escribir otra base.

`CONFIGURACION.USUARIOS_PREFERENCIAS` conserva una fila por usuario con `CULTURA` (`es-AR` o `en-US`). Mantiene estado,
auditoría y `ROW_VERSION` homogéneos.

`CONFIGURACION.TRADUCCIONES_CATALOGO` identifica cada texto por `ENTIDAD + CODIGO + CULTURA`. El código técnico no se
traduce; `NOMBRE` y `DESCRIPCION` son la presentación. La unicidad impide dos traducciones del mismo elemento e idioma.
Si falta una traducción, los SP usan el nombre español original como alternativa.

Los parámetros secretos de `CONFIGURACION.PARAMETROS_SISTEMA` conservan `REFERENCIA_SECRETO`, nunca el secreto en
`VALOR`. La API devuelve únicamente si la referencia existe, sin exponer su contenido.
