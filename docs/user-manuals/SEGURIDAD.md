# Manual de usuario: Seguridad

## Pantalla de ingreso

La aplicación recuerda en esta computadora únicamente el nombre del último usuario autenticado. Nunca guarda
contraseña, token ni empresa. Los intentos, bloqueos, sesiones y cierres continúan auditados en el servidor.

1. Ingresar el usuario corporativo y la contraseña personal.
2. Presionar **Continuar** para validar las credenciales.
3. La aplicación ocultará el formulario anterior y mostrará solamente la selección de empresa.
4. Seleccionar una empresa y presionar **Ingresar**. El botón **Volver** permite corregir las credenciales sin cerrar la
   aplicación.

Si la contraseña es temporal, la aplicación solicitará una nueva. Debe tener como mínimo 10 caracteres, una mayúscula,
una minúscula y un número. Los campos vacíos o las confirmaciones distintas se informan como validaciones controladas.

Luego de cinco intentos fallidos, la cuenta queda bloqueada durante 15 minutos. Un administrador puede recuperarla
asignando una contraseña temporal nueva.

## Pantalla Usuarios y accesos

Solo está disponible para `ADMINISTRADOR`.

### Crear un usuario

1. Presionar **Nuevo**.
2. Completar nombres, apellido y correo corporativo.
3. Seleccionar uno o más roles.
4. Asignar una contraseña temporal.
5. Presionar **Guardar**.

El nombre de usuario se genera automáticamente. No se deben compartir cuentas entre empleados.

### Editar datos, estado y roles

Seleccionar el usuario y presionar **Editar**. El administrador no puede desactivar su propia cuenta ni quitarse
el rol `ADMINISTRADOR`. Al inactivar un usuario se revocan sus sesiones.

### Reiniciar clave

Seleccionar el usuario, presionar **Reiniciar clave** y escribir dos veces la contraseña temporal. Las sesiones
anteriores se revocan y el usuario deberá cambiarla al ingresar.

### Revocar sesiones

Usar esta acción cuando un dispositivo se perdió, un empleado cambió de función o existe una sospecha de acceso.
No cambia la contraseña; solamente invalida las sesiones abiertas. El administrador también puede seleccionar su
propio usuario: la aplicación mostrará el aviso de cierre, permitirá cerrar inmediatamente y se cerrará como máximo
quince segundos después.
