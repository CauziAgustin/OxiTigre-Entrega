# Revisión del DER original

## Fuente y alcance

Se revisaron las páginas 289 a 297 del TFI, incluyendo los esquemas `SEGURIDAD`, `CONFIGURACION`, `INVENTARIO`, `COMERCIAL`, `COMPRAS`, `LOGISTICA` y `AUDITORIA`.

El DER es una buena definición funcional inicial, pero no se implementará literalmente. Antes del modelo físico se aplicarán las correcciones de este documento.

## Correcciones obligatorias

1. **Auditoría centralizada.** Se eliminará la repetición de usuario, dispositivo, IP, origen y datos técnicos en cada tabla. Las entidades conservarán solo metadatos mínimos; `AUDITORIA.LOGUEO_FUNCIONALIDADES` registrará la ejecución de SP de forma centralizada.
2. **Sin relaciones circulares de auditoría.** Los dispositivos no dependerán de permisos ni de sí mismos. Usuario, sesión y dispositivo tendrán relaciones explícitas y simples.
3. **Empresa aislada por base.** Cada empresa tendrá su propia base operativa. `OxiTigre_Platform` mantendrá el catálogo global y la ubicación/versionado de cada base; no concentrará operaciones comerciales.
4. **PK física estable.** Las PK y FK serán `BIGINT`; los códigos funcionales legibles no se usarán como claves relacionales.
5. **Kardex derivado.** El Kardex será una vista/proyección de movimientos confirmados, evitando duplicar el mismo hecho en dos tablas editables.
6. **Caja separada de su apertura.** `CAJAS` representará el punto físico y `SESIONES_CAJA` cada apertura/cierre. Arqueos y movimientos dependerán de una sesión.
7. **Catálogos ubicados una sola vez.** Categorías, unidades de medida, estados y tipos no se repetirán entre esquemas.
8. **Pagos y aplicaciones separados.** Un pago podrá aplicarse a una o varias ventas mediante `APLICACIONES_PAGO_VENTA`, sin duplicar saldos.
9. **Errores normalizados.** Se incorporarán módulo, consecutivo por módulo, código calculado único, descripción, causa, solución y estado.
10. **Concurrencia.** Las tablas modificables incluirán `rowversion`; saldos, numeradores y cierres se actualizarán dentro de transacciones.

## Relaciones principales previstas

| Esquema | Núcleo | Relaciones principales |
|---|---|---|
| `SEGURIDAD` | Usuarios, roles, permisos, sesiones, dispositivos | Usuario N:M Rol; Rol N:M Permiso; Usuario 1:N Sesión; Dispositivo 1:N Sesión |
| `CONFIGURACION` | Empresa, sucursales, unidades operativas, módulos, parámetros | Empresa 1:N Sucursal; Sucursal 1:N Unidad; Módulo 1:N Parámetro |
| `INVENTARIO` | Productos, depósitos, ubicaciones, stock, tubos, movimientos, recargas | Producto 1:N Stock; Depósito 1:N Ubicación; Movimiento 1:N Detalle; Tubo 1:N Trazabilidad |
| `COMERCIAL` | Clientes, ventas, detalles, pagos, cajas, comodatos | Cliente 1:N Venta; Venta 1:N Detalle; Pago N:M Venta; Caja 1:N Sesión |
| `COMPRAS` | Proveedores, órdenes, recepciones, facturas, pagos | Proveedor 1:N Orden; Orden 1:N Detalle y Recepción; Factura 1:N Pago |
| `LOGISTICA` | Pedidos, hojas de ruta, transporte, entregas | Pedido 1:N Ítem; Hoja 1:N Detalle; Pedido 1:N Entrega; Transportista/Vehículo 1:N Entrega |
| `AUDITORIA` | Log funcional, errores, soluciones, bitácoras, monitoreo | Ejecución 0:1 Error; Error 1:N Solución; Usuario/Sesión 1:N Ejecución |

`COMERCIAL.CLIENTES` tendrá una relación 1:N con `COMERCIAL.CLIENTES_TELEFONOS`. El tipo de teléfono se resolverá mediante `CONFIGURACION.TIPOS_TELEFONO`; la posición principal se representará mediante `ORDEN` y `ES_PRINCIPAL`.

## Convención de códigos de error

El valor se calcula sin ceros a la izquierda:

```text
CODIGO_ERROR = MODULO * 10000 + CONSECUTIVO
```

- Módulo 1, error 1: `10001`.
- Módulo 12, error 1: `120001`.
- Módulo 123, error 1: `1230001`.

Los últimos cuatro dígitos representan el consecutivo entre 1 y 9999. Los dígitos anteriores representan el módulo entre 1 y 999.

## Identidad de usuarios

Las cuentas de aplicación son distintas de los logins técnicos de SQL Server. Para usuarios de empresa:

- varios nombres: inicial de cada nombre más apellido (`Agustin Omar Cauzi` → `AOCAUZI`);
- un nombre: primeras dos letras más apellido (`Agustin Cauzi` → `AGCAUZI`);
- ante duplicado, se amplía progresivamente el primer nombre y se vuelve a comprobar unicidad.

Las contraseñas de aplicación se almacenarán únicamente como hash. Una clave temporal deberá cambiarse en el primer ingreso y nunca se guardará en Git.

## Entregable de base completado

El núcleo físico ya cuenta con schemas, tablas de configuración, seguridad, clientes y auditoría, relaciones, restricciones, índices, seeds mínimos, datos de Development y despliegue por ambiente. También se implementaron la creación correlativa y consulta diagnóstica de errores. Los SP funcionales masivos siguen fuera de esta etapa: se crearán con cada caso de uso aprobado.
