# Despliegue de base de datos

## Modelo adoptado

Cada empresa tendrá su propia base operativa. Desarrollo y Producción también son bases separadas; nunca son dos schemas dentro de la misma base. Esto entrega aislamiento de datos, backup, restauración y permisos por empresa.

La visión conjunta se resuelve mediante Platform, que registra destinos autorizados y permite a la API consultar
cada base por separado. No se utilizan consultas cruzadas improvisadas ni credenciales compartidas entre empresas.

Ejemplos de nombres:

```text
OxiTigre_Development
OxiTigre_Production
ClienteEjemplo_Development
ClienteEjemplo_Production
```

## Ejecución local

El script crea la base si no existe y aplica schemas, tablas, Stored Procedures, índices, constraints, seeds y perfiles
técnicos en orden. Development recibe demostraciones funcionales; Testing y Sandbox reciben un conjunto mínimo
ficticio; Production no recibe datos funcionales.

Antes de iniciar la API se instala el registro central siguiendo [`MULTI_COMPANY.md`](MULTI_COMPANY.md). Cada alta de
una empresa registra una base ya desplegada; Platform no crea ni comparte tablas operativas entre empresas.

> Este es un instalador de base nueva. No debe ejecutarse sobre una base ya creada: los scripts de definición inicial
> no son migraciones idempotentes. Una actualización se aplica ejecutando únicamente los cambios incrementales
> versionados y luego las pruebas `SMOKE_*`; nunca se reinstala una base con información.

Las migraciones se ejecutan una sola vez y quedan registradas con su hash. Si un archivo ya aplicado cambia, el
proceso se detiene:

```powershell
./scripts/database/Apply-Migrations.ps1 `
    -ServerInstance '.\SQLEXPRESS' `
    -DatabaseName 'OxiTigre_Development'
```

Después del despliegue se ejecuta la comprobación funcional:

```powershell
./scripts/database/Test-DatabaseDeployment.ps1 `
    -ServerInstance '.\SQLEXPRESS' `
    -DatabaseName 'OxiTigre_Development'
```

Para Testing, Sandbox o Producción se informa el ambiente para comprobar solamente estructura y datos técnicos;
el verificador exige los datos correspondientes al ambiente y confirma que Producción no contenga demostraciones:

```powershell
./scripts/database/Test-DatabaseDeployment.ps1 `
    -ServerInstance '.\SQLEXPRESS' `
    -DatabaseName 'OxiTigre_Sandbox' `
    -Environment Sandbox
```

Con autenticación integrada de Windows:

```powershell
./scripts/database/Deploy-Database.ps1 `
    -ServerInstance '.\SQLEXPRESS' `
    -DatabaseName 'OxiTigre_Development' `
    -Environment Development
```

Con un login de SQL Server:

```powershell
$credential = Get-Credential
./scripts/database/Deploy-Database.ps1 `
    -ServerInstance '.\SQLEXPRESS' `
    -DatabaseName 'OxiTigre_Development' `
    -Environment Development `
    -SqlCredential $credential
```

La contraseña no se agrega a los argumentos del proceso: se entrega temporalmente a `sqlcmd` mediante `SQLCMDPASSWORD` y se elimina al terminar.

## Usuario inicial exclusivo de Development

El conjunto de prueba crea al usuario funcional `AOCAUZI`, correspondiente a Agustin Omar Cauzi. La contraseña temporal acordada es `Admin123`; se almacena como PBKDF2-SHA512 con salt aleatorio y 210.000 iteraciones, y `DEBE_CAMBIAR_CLAVE = 1`.

Este usuario no se crea en Producción. En Producción el primer administrador se provisionará con una contraseña secreta suministrada durante el despliegue, nunca almacenada en Git.

## Producción y sandbox

`OxiTigre_Sandbox` será una base separada donde el equipo podrá ejecutar pruebas controladas. No será un schema libre dentro de Producción. Así una prueba no puede bloquear, alterar ni borrar datos productivos accidentalmente.

El instalador admite `Development`, `Testing`, `Sandbox` y `Production`. Testing y Sandbox reciben automáticamente
datos ficticios sin información personal; Producción recibe solamente estructura, catálogos y seguridad técnica.

En Producción:

- administradores: acceso administrativo nominal y auditado;
- usuarios de consulta: únicamente operaciones de lectura autorizadas;
- aplicación: permisos de ejecución sobre SP, sin acceso directo general a tablas;
- desarrolladores: pruebas y modificaciones en Sandbox o Development.

## Usuarios funcionales y accesos técnicos

Son identidades diferentes:

- `SEGURIDAD.USUARIOS` contiene las personas que inician sesión en la aplicación, como `AOCAUZI`, y sus roles funcionales;
- los logins de SQL Server se crean fuera del repositorio y se vinculan a roles técnicos de la base, sin reutilizar la contraseña de la aplicación.

Perfiles técnicos disponibles:

| Rol de base | Uso |
|---|---|
| `OXI_APP_EXECUTOR` | La API ejecuta SP, sin acceso general directo a tablas |
| `OXI_READONLY` | Consulta autorizada de Producción |
| `OXI_DEVELOPER` | Trabajo libre únicamente en Development o Sandbox |
| `OXI_DEPLOYER` | Instalación y actualización controlada |

La asignación de un login a estos roles se realizará con un administrador de SQL Server y una identidad nominal. No se versionan contraseñas ni `CREATE LOGIN` con claves conocidas.

## Estado local validado

La instancia `.\SQLEXPRESS` acepta autenticación integrada del usuario actual y contiene `OxiTigre_Development`.
El instalador completo se reserva para una base nueva; las correcciones sobre Development se aplican de forma incremental.

## Actualización de Fase 7

Para una base existente hasta Fase 6, ejecutar una sola vez:

```powershell
./scripts/database/Apply-Phase7.ps1 -ServerInstance '.\SQLEXPRESS' -DatabaseName 'OxiTigre_Development' -IncludeDevelopmentData
```

Si la Fase 7 ya estaba instalada antes del endurecimiento final, aplicar además `database/Migrations/20260824_FASE7_TRACEABILITY_HARDENING.sql` y los SP versionados de trazabilidad. Estas operaciones conservan los datos existentes.
