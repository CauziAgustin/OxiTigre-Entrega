# Configuración empresarial

## Alcance completado

- Edición de la empresa autenticada.
- Alta y edición lógica de sucursales y unidades operativas.
- Alta y edición de tipos de teléfono; edición controlada de estados informativos.
- Parámetros globales o por módulo, con tipos validados y referencias externas para secretos.
- Módulos numerados del `1` al `999` y catálogo de errores con correlativo de cuatro dígitos.
- Traducciones de catálogos y preferencia personal `es-AR` o `en-US`.
- Concurrencia mediante `ROW_VERSION`; una edición obsoleta devuelve el código `20003` y HTTP `409`.

## Reglas funcionales

- La base pertenece a una sola empresa. Esta pantalla modifica la empresa actual; las nuevas empresas se registran mediante el instalador de Platform y reinicio de la API.
- Consultar requiere `CONFIGURACION.CONSULTAR`; modificar requiere `CONFIGURACION.GESTIONAR`.
- Crear o modificar diagnósticos de error requiere `AUDITORIA.GESTIONAR_ERRORES`.
- No existen bajas físicas: se utiliza estado o vigencia.
- Los códigos de sucursal, unidad, catálogo y módulo son técnicos e inmutables después del alta.
- Un módulo no puede inactivarse mientras tenga permisos, parámetros o errores activos.
- Una sucursal no puede inactivarse mientras tenga unidades operativas activas.
- Los secretos no se guardan como texto: solo se conserva una referencia externa y nunca se devuelve por API.
- El mensaje personalizado de una ejecución no modifica la descripción central del código de error.

## Componentes

- WinForms: `ConfigurationForm`, con pestañas Empresa, Estructura, Catálogos, Parámetros, Módulos y errores e Idioma.
- API: rutas `/api/configuration` y `/api/audit/errors`.
- BLL/DAL: `ConfigurationService` y `SqlConfigurationStore`.
- SQL Server: maestros de `CONFIGURACION`, `USUARIOS_PREFERENCIAS`, `TRADUCCIONES_CATALOGO` y `AUDITORIA.CATALOGO_ERRORES`.

## Verificación

```powershell
dotnet test OxiTigre.sln --configuration Release
sqlcmd -S ".\SQLEXPRESS" -d "OxiTigre_Development" -E -b -C -i "tests\Database\SMOKE_CONFIGURACION.sql"
```

La prueba SQL usa una transacción y termina con `ROLLBACK`.
