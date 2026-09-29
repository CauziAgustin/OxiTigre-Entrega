# Ambientes de OxiTigre

## Separación

Cada ambiente utiliza una base operativa y una base Platform propias. Ninguna aplicación de Testing o Sandbox
apunta a Development o Producción.

| Ambiente | Base Platform | Base operativa | Datos automáticos |
|---|---|---|---|
| Development | `OxiTigre_Platform` | `OxiTigre_Development` | Demostración funcional local |
| Testing | `OxiTigre_Platform_Testing` | `OxiTigre_Testing` | Usuario y maestros ficticios de QA |
| Sandbox | `OxiTigre_Platform_Sandbox` | `OxiTigre_Sandbox` | Usuario y maestros ficticios de QA |
| Production | Configuración secreta del servidor | Base productiva independiente | Ninguno |

Testing y Sandbox incluyen `QAADMIN` con la credencial ficticia `OxiTest123!`. No debe copiarse a Producción ni
usarse para una persona real. El verificador de Producción rechaza usuarios y productos demostrativos.

## Instalación nueva

```powershell
./scripts/database/Deploy-Environment.ps1 `
    -ServerInstance '.\SQLEXPRESS' `
    -Environment Testing
```

El comando instala ambas bases, registra `OXITIGRE` en Platform y comprueba estructura y datos del ambiente. El
instalador declarativo es para una base nueva; las existentes se actualizan mediante cambios incrementales
versionados, registrados por hash, y luego se verifican.

## Integración local

```powershell
./scripts/ci/Test-ApiSqlIntegration.ps1
```

La prueba inicia temporalmente la API con configuración Testing, valida login, aislamiento de empresa, consulta
administrativa y logout, y finalmente detiene ese proceso. No utiliza ni modifica Development.

## Configuración remota

Las cadenas productivas se inyectan mediante secretos o variables protegidas. El repositorio no contiene servidor,
usuario ni clave reales. La API se publica por HTTPS y SQL Server permanece en red privada.
