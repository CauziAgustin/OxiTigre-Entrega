# Estrategia multilenguaje

## Objetivo

La aplicación funciona como mínimo en español e inglés y permite agregar idiomas sin modificar el modelo funcional.
Español es el idioma predeterminado y cada usuario puede elegir una cultura instalada.

## Principios

- Los códigos internos son estables y no se traducen: `F`, `J`, `DNI`, `ACTIVO`, permisos y códigos de error.
- Los textos visibles no se utilizan para reglas, relaciones ni búsquedas técnicas.
- Fechas, números y monedas se formatean con la cultura elegida.
- La API intercambia códigos; el cliente presenta el texto traducido.
- Ningún idioma se implementará duplicando columnas como `NOMBRE_INGLES`.

## Implementación actual

1. `frontend/OxiTigre.WinForms/Properties/Resources.resx` contiene el español neutral.
2. `frontend/OxiTigre.WinForms/Properties/Resources.en.resx` contiene el inglés.
3. `CONFIGURACION.USUARIOS_PREFERENCIAS` conserva la cultura por usuario. El cliente recuerda localmente la última cultura para traducir el login antes de conocer al usuario y la sincroniza después de autenticarlo.
4. `CONFIGURACION.TRADUCCIONES_CATALOGO` traduce catálogos sin duplicar columnas.
5. La API intercambia códigos estables y selecciona las traducciones del catálogo para la cultura del usuario.
6. `LocalizationResourceTests` comprueba que los recursos `.resx` mantengan las claves requeridas.
7. Configuración permite importar un paquete JSON de hasta 1 MB. La aplicación valida esquema, cultura específica,
   propiedades repetidas, claves conocidas, textos y parámetros de formato antes de guardarlo en el perfil local.

Al guardar otro idioma, WinForms cierra las ventanas del dashboard y las reconstruye con la cultura elegida. La sesión,
el token, la empresa y los permisos se conservan; el login no vuelve a mostrarse. La cultura local también se guarda
para que el próximo inicio, incluido el login, utilice el último idioma elegido.

## Agregar un idioma en Escritorio

El administrador abre **Configuración > Idioma > Importar idioma** y selecciona un JSON como este:

```json
{
    "schemaVersion": 1,
    "culture": "pt-BR",
    "displayName": "Português (Brasil)",
    "translations": {
        "Common_Save": "Salvar",
        "Common_Cancel": "Cancelar"
    }
}
```

Las traducciones pueden incorporarse gradualmente: una clave ausente conserva el texto español de respaldo. Una clave
desconocida, duplicada, con llaves de formato mal cerradas o con parámetros `{0}` incompatibles rechaza todo el archivo.
El límite de 1 MB se vuelve a verificar al cargar paquetes instalados. Los paquetes importados se guardan
en `%LOCALAPPDATA%\OxiTigre\languages`; no se ejecuta contenido del archivo.

Las traducciones de catálogos se administran por separado en Configuración y aceptan la misma cultura. Las respuestas
técnicas continúan incluyendo códigos estables.
