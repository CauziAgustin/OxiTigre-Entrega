# Seguridad

## Usuarios de aplicación

La convención de nombre de usuario es:

- Dos o más nombres: inicial de cada nombre más apellido.
- Un nombre: dos primeras letras del nombre más apellido.
- Mayúsculas, sin espacios ni tildes.
- La disponibilidad se valida antes de guardar.
- Ante una colisión se amplía progresivamente el primer nombre.

Ejemplos:

```text
Agustin Omar Cauzi -> AOCAUZI
Agustin Cauzi      -> AGCAUZI
```

La base impondrá unicidad sobre el usuario normalizado.

## Acceso a SQL Server

Cada desarrollador tendrá una cuenta individual. No se compartirán credenciales.

Roles técnicos implementados:

- `OXI_DEVELOPER`: control de la base asignado únicamente en Development o Sandbox;
- `OXI_READONLY`: lectura autorizada, especialmente para Producción;
- `OXI_APP_EXECUTOR`: ejecución de SP por parte de la API;
- `OXI_DEPLOYER`: instalación y actualización controlada.

Development y Sandbox permiten trabajo libre dentro de su propia base. Producción restringe a los desarrolladores a lectura. La API y el deployment usan identidades técnicas separadas.

## Credenciales

- No se guardan contraseñas, tokens o claves en Git.
- Una contraseña temporal debe cambiarse en el primer acceso.
- Los tokens de sesión y recuperación se almacenan como hash.
- Las credenciales de cada ambiente son independientes.

## Autenticación de aplicación implementada

- La API recibe empresa, usuario y contraseña por HTTPS.
- La contraseña se verifica con PBKDF2-SHA512 y comparación en tiempo constante.
- La sesión entrega un token opaco aleatorio de 256 bits; la base almacena solamente SHA-512 del token.
- La vigencia inicial es de ocho horas.
- `Admin123` existe únicamente como clave temporal del seed de Development y debe cambiarse en el primer ingreso.
- Cambiar la contraseña revoca las sesiones vigentes.
- La API recupera roles y permisos efectivos desde la base.
- El quinto intento fallido bloquea temporalmente la cuenta durante quince minutos.
- Un acceso correcto posterior al bloqueo temporal limpia el contador.
- El cierre normal marca la sesión como `CERRADA`; un administrador puede marcar sesiones ajenas como `REVOCADA`.
- Un administrador puede editar datos, estado y múltiples roles, y asignar una contraseña temporal nueva.
- Desactivar un usuario o reiniciar su contraseña revoca sus sesiones vigentes.
- Un administrador no puede desactivarse ni quitarse su propio rol `ADMINISTRADOR`.

Endpoints iniciales:

```text
POST /api/security/login
POST /api/security/change-password
POST /api/security/logout
GET  /api/security/session
GET  /api/security/users
POST /api/security/users
PUT  /api/security/users/{id}
POST /api/security/users/{id}/reset-password
POST /api/security/users/{id}/revoke-sessions
GET  /api/security/roles
```

La recuperación local se realiza mediante reinicio administrativo de contraseña temporal. No se devuelve ni se
muestra un token de recuperación. El autoservicio se habilitará únicamente cuando el empleado tenga correo o teléfono
verificado y exista un proveedor de notificaciones configurado.

La solicitud responderá siempre con el mismo mensaje exista o no la cuenta. El código o enlace será aleatorio, de un
solo uso, tendrá vencimiento breve, se almacenará únicamente como hash y limitará intentos y reenvíos. Nunca se enviará
una contraseña por correo o WhatsApp. Al completar el cambio se revocarán las sesiones vigentes y se notificará al
titular por sus canales verificados.

Los teléfonos y correos de clientes y empleados son datos personales. Se normalizarán, validarán y expondrán solamente
a permisos funcionales que los necesiten. El consentimiento, canal preferido, verificación, fecha y origen quedarán
auditados. Los secretos de correo y WhatsApp se configurarán por ambiente mediante referencias seguras y nunca se
guardarán en la base operativa, Git, logs ni mensajes de error.

## Permisos de Configuración

- `CONFIGURACION.CONSULTAR`: visualiza la configuración de la empresa autenticada.
- `CONFIGURACION.GESTIONAR`: modifica empresa, estructura, catálogos, parámetros, módulos y traducciones.
- `AUDITORIA.GESTIONAR_ERRORES`: crea o modifica diagnósticos del catálogo de errores.

Estos permisos se resuelven desde la sesión; la API nunca acepta empresa, usuario o sesión desde el cuerpo de una petición.
El seed los asigna al rol `ADMINISTRADOR`, no al rol `CONSULTA` para operaciones de gestión.
