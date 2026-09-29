# Despliegue

## Development

- Ejecución local.
- Base local independiente.
- User Secrets o variables de entorno.
- Datos de prueba.

## Testing

- API, base y configuración propias.
- Datos ficticios o anonimizados.
- Despliegue posterior a un checkpoint aprobado.
- Smoke tests posteriores al despliegue.
- Datos ficticios `QAADMIN`, cliente, producto, depósito y existencia.

## Sandbox

- Base operativa y Platform separadas.
- Datos ficticios para pruebas libres del equipo.
- Ningún acceso o referencia a Producción.

## Producción

- Servidor o servicio dedicado.
- API publicada mediante HTTPS.
- Base independiente con backup previo.
- Credenciales de runtime y deployment separadas.
- Confirmación manual en cada release.

## Rollback

- API y WinForms utilizan artefactos inmutables por versión.
- La base utiliza cambios compatibles, backup y estrategia expand/migrate/contract.
- Se prioriza forward-fix.
- Una restauración productiva requiere autorización explícita.

## Artefacto

```powershell
./scripts/ci/Build-Release.ps1 -Version '2026.09.22'
```

El ZIP resultante contiene los tres ejecutables, scripts SQL, semillas ficticias exclusivas de Testing/Sandbox,
documentación operativa y un `manifest.json` con commit, tamaño y SHA-256 de cada archivo. No contiene datos de
Development, información personal real ni secretos.

El alcance del artefacto es `DesktopApiDatabase`: API, WinForms, Admin Web y base de datos. No incluye la aplicación
móvil. Antes de publicarlo se debe ejecutar `./scripts/ci/Test-DesktopDelivery.ps1`; la entrega oficial se genera desde
un árbol Git limpio y sin `-AllowDirty`.
