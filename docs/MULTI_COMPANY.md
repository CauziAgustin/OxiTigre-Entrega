# Plataforma multiempresa

## Regla funcional

Una empresa posee una base operativa independiente. Una sucursal pertenece a una empresa y comparte su base,
usuarios y catálogos, conservando su propio identificador para depósitos, unidades operativas y operaciones.

Por eso `OxiTigre San Fernando` es una sucursal de `OXITIGRE`; no es otra empresa ni otra base.

## Resolución de bases

`OxiTigre_Platform` almacena exclusivamente:

- código y nombre de la empresa;
- servidor y base operativa;
- identificador local de la empresa;
- versión del esquema y estado;
- referencia externa de secreto cuando el ambiente no utiliza autenticación integrada.

No guarda ventas, clientes, stock, contraseñas ni cadenas con claves. La API carga este registro al iniciar y solo
acepta códigos presentes en él. El código elegido queda incluido en el token opaco y su hash se guarda únicamente
en la base operativa correspondiente. Reiniciar la API aplica altas o cambios del registro central.

Los usuarios, roles, permisos y sesiones permanecen aislados dentro de cada empresa. Un integrante que deba entrar
a dos empresas se habilita en ambas; el login valida sus credenciales contra las bases registradas y después muestra
el desplegable de empresas autorizadas.

## Vista global

El endpoint `GET /api/platform/overview` exige una sesión con rol `ADMINISTRADOR`. Consulta cada base por separado y
devuelve indicadores agregados. Si una empresa no responde, aparece como `NO_DISPONIBLE` sin impedir la consulta de
las demás. El panel Blazor presenta esta información sin realizar joins ni movimientos cruzados.

## Instalación local

```powershell
./scripts/database/Deploy-PlatformDatabase.ps1 `
    -ServerInstance '.\SQLEXPRESS' `
    -CompanyCode 'OXITIGRE' `
    -CompanyName 'OxiTigre' `
    -OperationalDatabaseName 'OxiTigre_Development' `
    -LocalCompanyId 1 `
    -SchemaVersion '9.0.0'
```

El script es idempotente para el registro: crea Platform cuando falta y actualiza el destino de `OXITIGRE` cuando
ya existe. Un servidor con autenticación SQL requerirá primero un proveedor de secretos; nunca se guarda la clave en
la tabla central ni en Git.

## San Fernando

Development contiene:

- sucursal `SAN_FERNANDO` / `OxiTigre San Fernando`;
- unidad `OPERACION_SF`;
- depósito `SAN_FERNANDO`;
- ubicación inicial `RECEPCION`.

El domicilio y las áreas definitivas deben completarse cuando existan datos reales; no se inventaron direcciones.
