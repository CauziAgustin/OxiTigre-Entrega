# Integración continua de la entrega

El workflow de GitHub Actions se ejecuta sobre `entrega-profesor` en Windows. Restaura y compila la solución de
escritorio en Release, ejecuta pruebas .NET, comprueba sintaxis T-SQL con ScriptDom y valida documentación, filtros,
tableros y paquetes de idioma.

La integración API–SQL se ejecuta localmente con `./scripts/ci/Test-ApiSqlIntegration.ps1` porque requiere SQL Server
Testing privado. No se expone SQL Server ni se cargan credenciales en GitHub para automatizar esa prueba.

Los scripts de despliegue remoto no se activan en este corte. Para producción faltan hosting, HTTPS, secretos,
backups y aprobación manual, según [la guía de despliegue](DEPLOYMENT.md).
