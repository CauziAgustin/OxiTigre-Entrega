# Manual de usuario: Configuración

## Acceso

Desde el panel principal, presionar **Configuración**. Un usuario de consulta puede ver la información; solamente un administrador autorizado puede modificarla.

## Empresa

Actualizar razón social, nombre de fantasía, CUIT o correo y presionar **Guardar**. La pantalla corresponde a la empresa elegida al iniciar sesión; no crea empresas nuevas.

## Estructura

1. Crear una sucursal con código y nombre.
2. Seleccionar **Unidades operativas** y crear las áreas pertenecientes a una sucursal.
3. Para retirar un registro, editarlo y elegir **Inactivo**.

Antes de inactivar una sucursal deben inactivarse sus unidades. Los códigos no se cambian después del alta.

## Catálogos

- **Tipos de teléfono:** definir las opciones visibles al cargar contactos.
- **Estados visibles:** ajustar nombre, descripción, orden o fecha de vigencia; los códigos protegidos no se renombran.
- **Traducciones:** elegir entidad, código y cultura, y cargar el nombre y la descripción que verá el usuario.

Los códigos se usan internamente. El empleado siempre debe trabajar con los nombres visibles.

## Parámetros

Elegir si el parámetro es global o pertenece a un módulo, indicar clave, tipo, valor y descripción. Para un secreto, marcar **Es secreto** e ingresar únicamente la referencia provista por infraestructura; nunca copiar contraseñas o tokens en el valor.

## Módulos y errores

Al crear un módulo se asigna un número entre `1` y `999`. Ese número y su código quedan fijos. Al crear un error, seleccionar primero el módulo y documentar nombre, descripción, causa probable, acción recomendada y severidad.

El sistema genera el código automáticamente: número de módulo seguido por cuatro dígitos. Ejemplos: módulo `1` → `10001`; módulo `12` → `120001`.

Antes de crear un error, buscar si ya existe uno aplicable. Un desarrollador puede enviar un mensaje más específico en una ejecución sin alterar el diagnóstico central.

## Idioma

Seleccionar **Español (Argentina)** o **English (United States)** y guardar. El dashboard se reconstruirá de inmediato
en el idioma elegido sin cerrar la sesión ni volver al login. El equipo recuerda la última selección para que el
próximo acceso también utilice ese idioma.

Si otra persona modificó el mismo registro mientras la pantalla estaba abierta, el sistema rechazará el guardado. Presionar **Actualizar**, revisar el cambio y volver a editar.
