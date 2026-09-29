# Notificaciones transversales

## Objetivo

OxiTigre informará a clientes y empleados sin acoplar Pedidos, Logística o Seguridad a un proveedor determinado. Los
eventos se guardarán primero en SQL Server y un proceso de envío los procesará posteriormente. Una caída del proveedor
no debe impedir confirmar un pedido, una entrega o un cambio de contraseña.

## Eventos previstos

| Evento | Destinatario | Canales posibles |
|---|---|---|
| Pedido recibido o confirmado | Cliente | Correo, WhatsApp |
| Entrega programada o reprogramada | Cliente | Correo, WhatsApp |
| Transportista en camino | Cliente | WhatsApp, correo |
| Entrega completa, parcial o fallida | Cliente y responsable interno | Correo, WhatsApp |
| Tubo retirado, listo o devuelto | Cliente | Correo, WhatsApp |
| Préstamo o devolución próxima/vencida | Cliente y responsable interno | Correo, WhatsApp |
| Recuperación de contraseña | Empleado | Correo o WhatsApp verificado |
| Contraseña modificada | Empleado | Todos los canales verificados |

Cada plantilla tendrá versión, idioma, asunto cuando corresponda, cuerpo y variables permitidas. Los mensajes
conservarán evento origen, destinatario, canal, plantilla, estado, intentos, fechas, identificador del proveedor y
diagnóstico técnico sin incluir secretos.

## Preferencias y consentimiento

- El cliente elegirá si acepta comunicaciones por correo y/o WhatsApp y cuál es su canal preferido.
- La autorización conservará fecha, origen y empleado que la registró.
- Los datos de contacto se normalizarán; los teléfonos se almacenarán en formato internacional.
- Una entrega podrá usar un contacto operativo distinto sin modificar el contacto principal del cliente.
- El empleado elegirá un canal de recuperación únicamente entre contactos previamente verificados.
- Las bajas, revocaciones y cambios de contacto tendrán efecto antes de generar nuevos mensajes.

## Bandeja de salida

La operación de negocio y la notificación pendiente se guardarán en la misma transacción. El proceso de envío tomará
registros pendientes, aplicará la plantilla, llamará al proveedor y registrará el resultado. Los reintentos usarán
espera progresiva y un límite configurable; un fallo permanente quedará visible para intervención manual.

En Development el proveedor simulado mostrará la previsualización dentro del sistema o en logs protegidos, sin enviar
correo, WhatsApp ni datos personales reales. Testing utilizará cuentas y destinatarios ficticios. Production exigirá
secretos por ambiente, HTTPS, remitentes autorizados y monitoreo.

## Recuperación de contraseña

1. El empleado escribe su usuario y elige un canal verificado parcialmente oculto.
2. La API devuelve un mensaje genérico, independientemente de que la cuenta exista.
3. Se genera un código o enlace criptográficamente aleatorio, de un solo uso y vencimiento breve.
4. La base guarda solamente su hash, vencimiento, intentos y auditoría.
5. El empleado valida el código y registra una contraseña nueva con la política vigente.
6. Se consumen los códigos pendientes, se revocan las sesiones y se informa el cambio.

Se limitarán solicitudes, reenvíos e intentos por cuenta y origen. No se utilizarán preguntas de seguridad ni se
enviarán contraseñas temporales por estos canales.

## Información necesaria para activar proveedores

### Correo

- dominio y dirección remitente de la empresa;
- servicio SMTP o API elegido;
- acceso para configurar SPF, DKIM y DMARC;
- credencial separada para cada ambiente;
- textos y datos legales del pie de mensaje.

### WhatsApp

- cuenta empresarial y número dedicado;
- acceso administrativo al servicio elegido;
- plantillas aprobadas para cada evento e idioma;
- registro del consentimiento del destinatario;
- credenciales y webhook separados por ambiente.

La selección comercial del proveedor se realizará antes del despliegue remoto. Hasta entonces se desarrollará y
probará el flujo completo con el proveedor simulado.
