# Registro de decisiones

## 2026-08-19 - Arquitectura

- Monolito modular con API obligatoria.
- .NET 10 LTS como plataforma objetivo.
- SQL Server con Stored Procedures y SQL Database Project.
- Una base operativa por empresa.
- Base de plataforma para catálogo y vista global.

## 2026-08-19 - Ambientes

- `develop` representa Development.
- `testing` representa Testing.
- `main` representa Producción.
- Producción requiere aprobación manual.

## 2026-08-19 - Seguridad

- Cuentas SQL individuales y roles por ambiente.
- Development y Sandbox permiten desarrollo libre en su base.
- Producción restringe desarrolladores a lectura.
- Sandbox es una base independiente, no un schema productivo.

## 2026-08-19 - Errores

- Código variable de cinco a siete dígitos.
- Los últimos cuatro dígitos son el error.
- Los dígitos anteriores identifican el módulo.
- Fórmula: `MODULO * 10000 + ERROR`.

## Decisión pendiente

- Formato de referencia funcional para PKEY y módulo. La PK física seguirá siendo numérica hasta completar el análisis.

## 2026-08-19 - Restricciones de GitHub

- Se crearon las ramas `develop`, `testing` y se conservó `main` sin promover cambios.
- Se crearon los Environments `testing` y `production` sin secretos.
- El plan actual del repositorio privado no admite branch protection ni required reviewers. La configuración recomendada queda documentada para activarla al cambiar de plan.

## 2026-08-19 - Estados homogéneos

- Todas las entidades utilizarán `CODIGO_ESTADO NVARCHAR(30)` con códigos estables en mayúsculas.
- `CONFIGURACION.ESTADOS` documentará los códigos por entidad sin FK.
- Todo SP de alta o modificación validará `ENTIDAD + CODIGO_ESTADO`.
- Los nombres visibles de los estados podrán cambiar sin modificar el código persistido.
- Para teléfonos, `ORDEN` representa 0, 1, 2... y `ID_TIPO_TELEFONO` representa móvil, fijo, laboral u otra clasificación.

## 2026-08-20 - Venta interna y reserva

- Confirmar un pedido reserva stock, pero no cambia la existencia física.
- La reserva reduce el disponible para movimientos manuales y otros pedidos.
- Vender aplica la reserva y crea un movimiento de salida dentro de la misma transacción.
- La venta interna no se presenta como factura fiscal.
- Comprobantes, pagos y cuenta corriente quedan pendientes hasta aprobar reglas fiscales, contables y AFIP.

## 2026-08-26 - Notificaciones y recuperación de acceso

- Correo y WhatsApp compartirán un motor de notificaciones basado en bandeja de salida, plantillas e intentos auditados.
- Los módulos publicarán eventos de negocio dentro de su misma transacción; un proceso independiente realizará el envío.
- Development utilizará un proveedor simulado y no transmitirá datos personales fuera del equipo.
- Cada empresa podrá habilitar canales y plantillas sin incorporar credenciales al código o la base operativa.
- Los clientes conservarán consentimiento, canal preferido y contactos habilitados para comunicaciones operativas.
- Los empleados deberán verificar correo o teléfono antes de utilizar recuperación autoservicio.
- La recuperación usará códigos o enlaces aleatorios, breves, de un solo uso y almacenados como hash; nunca enviará contraseñas.
- El proveedor concreto de correo, WhatsApp y futura optimización de rutas se decidirá al preparar el ambiente remoto.

## 2026-08-27 - Artefacto de base de datos

- El modelo ejecutable continúa siendo el instalador ordenado de scripts, validado con ScriptDom y despliegue limpio.
- No se mantiene un SQL Project paralelo porque los procedimientos reutilizables usan `CREATE OR ALTER` y varios
  archivos agrupan objetos; duplicarlos en formato declarativo crearía dos fuentes de verdad con riesgo de deriva.
- Un DACPAC se evaluará nuevamente cuando se normalice el repositorio a un objeto declarativo por archivo sin perder
  la capacidad de aplicar procedimientos incrementalmente.
