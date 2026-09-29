# Manual de usuario: Clientes y teléfonos

## Qué debe preguntar el empleado

Antes de cargar un cliente, confirmar:

1. ¿Es una persona física o una persona jurídica?
2. Persona física: nombre y apellido completos.
3. Persona jurídica: razón social exacta; no se solicita apellido.
4. ¿Qué documento presenta y cuál es el número?
5. ¿Cuál es su correo electrónico de contacto?
6. ¿Qué teléfonos desea informar?
7. Para cada teléfono: tipo, país, código de área, número e interno, si corresponde.
8. ¿Cuál es el teléfono principal?
9. ¿Autoriza el contacto por WhatsApp en ese número?
10. ¿Existe alguna observación útil para la atención?

No registrar contraseñas, datos de tarjetas ni información sensible en **Observación**.

## Pantalla principal de clientes

- **Estado** filtra clientes activos o inactivos.
- **Nuevo cliente** abre una carga vacía o permite recuperar el borrador propio.
- **Editar** modifica el cliente seleccionado.
- **Cambiar estado** activa o inactiva sin eliminar información.
- **Actualizar** vuelve a consultar la base.

La grilla presenta todos los títulos en español y muestra “Persona física” o “Persona jurídica”, nunca los códigos
internos `F` y `J`.

## Nuevo cliente

### Persona física

Seleccionar **Persona física**. La pantalla muestra **Nombre** y **Apellido**. Los documentos disponibles se filtran
por el catálogo, por ejemplo DNI, CUIL, CUIT o pasaporte.

### Persona jurídica

Seleccionar **Persona jurídica**. La pantalla cambia el título a **Razón social** y oculta el apellido. Los documentos
se limitan a los admitidos para empresas, como CUIT o CDI.

### Teléfonos

1. Seleccionar el tipo de teléfono parametrizado.
2. Seleccionar el país. Argentina completa `54` automáticamente.
3. Para **Otro país**, escribir manualmente el código telefónico.
4. Ingresar código de área, número e interno con dígitos solamente.
5. Marcar **Principal** en exactamente un teléfono.
6. Presionar **Agregar teléfono**.

Un doble clic sobre un teléfono lo devuelve a los campos para corregirlo. Después de modificarlo debe agregarse
nuevamente.

## Guardar una carga incompleta

Presionar **Guardar borrador** aunque todavía falten documento o teléfonos. La precarga queda asociada al usuario y
a la empresa. Al volver a **Nuevo cliente**, la aplicación ofrece recuperarla.

**Descartar borrador** elimina lógicamente esa precarga. Al guardar el cliente definitivo, el borrador se descarta
automáticamente.

## Validaciones principales

- Nombre o razón social obligatorios.
- Apellido obligatorio únicamente para persona física.
- Tipo y número de documento obligatorios y compatibles con la persona.
- DNI de 7 u 8 dígitos; CUIT/CUIL con dígito verificador válido.
- Correo opcional, pero con formato válido cuando se informa.
- Teléfonos numéricos, sin duplicados y con exactamente uno principal.
- Documento único dentro de la empresa.

Si aparece un código de error y una correlación, copiarlos completos para el equipo de desarrollo.
