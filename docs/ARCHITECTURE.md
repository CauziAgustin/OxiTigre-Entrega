# Arquitectura del sistema

## Objetivo

OxiTigre se implementa como un monolito modular con una API central. Esta decisión permite mantener una solución
operativa simple y conservar límites claros entre módulos.

```text
WinForms ───────┐
Admin Web ──────┼── HTTPS ── OxiTigre.Api ── BLL ── DAL
Mobile* ────────┘                                  │
                                                   ▼
                                  StoredProcedureExecutor
                                                   │
                                                   ▼
                                            SQL Server
```

`*` Mobile posee un desarrollo independiente, pero se excluye del corte de entrega del 22/09/2026. La arquitectura
permite incorporarlo sin acceso directo a la base cuando complete su validación específica.

## Principios

- Los clientes conocen únicamente la URL de la API.
- La API centraliza autenticación, autorización, reglas de negocio, transacciones y auditoría.
- La DAL ejecuta Stored Procedures mediante `Microsoft.Data.SqlClient`.
- Cada empresa tendrá una base operativa independiente.
- Una base de plataforma mantendrá el catálogo de empresas y permitirá una vista global autorizada.
- Las aplicaciones web y móviles tienen build, configuración y despliegue independientes.

## Tecnología objetivo

- .NET 10 LTS.
- ASP.NET Core Web API.
- Windows Forms sobre .NET 10 para Windows.
- SQL Server con instalador ordenado de scripts versionados.
- ScriptDom, registro SHA-256 de migraciones y verificación posterior al despliegue.
- xUnit para pruebas automatizadas.

## Base técnica

El código de prueba heredado fue retirado por decisión del responsable. La solución nueva utiliza .NET 10, separa API, negocio, datos, dominio, contratos y clientes, y no contiene credenciales ni URLs de ambientes dentro del código fuente.

## Aislamiento por empresa

Convención prevista:

```text
OxiTigre_Platform
OxiTigre_<EMPRESA>_Development
OxiTigre_<EMPRESA>_Testing
OxiTigre_<EMPRESA>_Production
OxiTigre_<EMPRESA>_Sandbox
```

`OxiTigre_Platform` contiene el catálogo, ubicación y versión de cada base. No almacena ventas, stock, operaciones,
contraseñas ni secretos. Los usuarios, roles y sesiones permanecen dentro de cada empresa; el login consulta las bases
registradas y el token resultante identifica la empresa seleccionada. Consulte [`MULTI_COMPANY.md`](MULTI_COMPANY.md).
