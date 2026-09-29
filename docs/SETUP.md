# Configuración del entorno local

## Requisitos

- Windows 10 o posterior.
- Git.
- GitHub CLI.
- .NET SDK 10.0.400 o parche compatible.
- Visual Studio con soporte para .NET, WinForms y herramientas SQL.
- SQL Server para Development.
- `sqlcmd` y, cuando se incorpore el SQL Project, SqlPackage.

## Verificación

```powershell
git --version
gh --version
gh auth status
dotnet --version
sqlcmd -?
```

## Configuración sensible

Las cadenas de conexión locales deben configurarse mediante variables de entorno o User Secrets. No se deben agregar contraseñas a `appsettings.json`, archivos SQL o documentación.

Variables utilizadas:

```text
ConnectionStrings__OxiTigrePlatform
OXITIGRE_API_URL
```

## Compilar y probar

Para el corte Desktop, que excluye Mobile:

```powershell
./scripts/ci/Test-DesktopDelivery.ps1
```

Para ejecutar manualmente:

```powershell
./scripts/development/Start-Api.ps1
# En otra consola:
./scripts/development/Start-Desktop.ps1
# Panel web administrativo opcional:
dotnet run --project frontend/OxiTigre.AdminWeb
```

`OxiTigre.sln` y `OxiTigre.Desktop.slnf` contienen únicamente los proyectos de este corte. No se requieren Android
ni workloads móviles.

La API expone `GET /health` como comprobación inicial. WinForms y Blazor utilizan `OXITIGRE_API_URL` cuando deben conectarse a otra URL.

## Ambiente local sugerido

La demostración utiliza SQL Server local con una base Platform y una base operativa Development separadas. Los
scripts de `scripts/database/` crean y verifican esas bases; no se incluyen claves reales en el repositorio.
