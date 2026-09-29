# Base de datos OxiTigre

Esta carpeta es la fuente declarativa de las bases OxiTigre. El diseño físico comienza con los catálogos de estados y tipos de teléfono, y con los teléfonos de clientes. Cada objeto debe respetar el diccionario homogéneo y los estándares de desarrollo.

## Orden lógico

1. Base y opciones.
2. Schemas.
3. Tablas.
4. Types y funciones.
5. Vistas.
6. Stored Procedures.
7. Índices y constraints.
8. Seeds.
9. Datos exclusivos de Development.
10. Perfiles y scripts de despliegue.

Cada Stored Procedure tendrá un archivo propio y se clasificará en `Queries`, `Commands` o `Audit`.

El despliegue repetible se realiza con `scripts/database/Deploy-Database.ps1`. Consulte `docs/DATABASE_DEPLOYMENT.md` para ambientes, credenciales y aislamiento por empresa.


## Normas obligatorias

- [docs/DATA_DICTIONARY.md](../docs/DATA_DICTIONARY.md): nombres, tipos y estados homogéneos.
- [docs/DEVELOPMENT_STANDARDS.md](../docs/DEVELOPMENT_STANDARDS.md): cabeceras, versionado y reglas T-SQL.
- [templates/sql](../templates/sql): plantillas oficiales de tablas y Stored Procedures.
