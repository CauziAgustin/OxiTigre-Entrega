# OxiTigre — entrega de escritorio

Esta rama contiene el corte académico del **22 de septiembre de 2026**: aplicación WinForms, API ASP.NET Core,
panel administrativo Blazor, reglas de negocio, acceso a SQL Server, procedimientos almacenados, scripts de
instalación, documentación y pruebas. La aplicación móvil no forma parte de esta entrega.

## Arquitectura y prácticas

El sistema es un monolito modular en C#/.NET 10: WinForms y Blazor consumen la API; la BLL concentra reglas y
autorizaciones; la DAL utiliza parámetros y Stored Procedures; SQL Server conserva integridad, transacciones e
historial. Se aplican aislamiento por empresa, sesiones y roles, `rowversion`, bajas lógicas, migraciones versionadas,
auditoría, documentación XML, validaciones en cada frontera y compilación con warnings como errores.

El [informe de entrega](docs/DELIVERY_2026-09-22.md) explica qué está implementado, qué requiere revisión manual y
qué permanece pendiente. Las fechas de las cabeceras indican la versión actual; el historial registra la creación y
los cambios según la evidencia conservada. No se presentan integraciones externas ni facturación fiscal como completas.


## Documentos principales

- [Arquitectura](docs/ARCHITECTURE.md)
- [Alcance y buenas prácticas de la entrega](docs/DELIVERY_2026-09-22.md)
- [Estándares y documentación de código](docs/DEVELOPMENT_STANDARDS.md)
- [Modelo y despliegue de datos](docs/DATABASE.md)
- [Seguridad](docs/SECURITY.md)
- [Internacionalización e importación de idiomas](docs/INTERNATIONALIZATION.md)
- [Manuales de usuario](docs/user-manuals/README.md)

