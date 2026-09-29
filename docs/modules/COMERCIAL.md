# Módulo Comercial: clientes

## Alcance implementado

El módulo administra clientes aislados por empresa. Incluye listado por estado, detalle, alta, edición, activación e inactivación lógica. Cada cliente conserva un código funcional automático `CLI-000001` independiente de su clave técnica `ID_CLIENTE`.

Los teléfonos se guardan como colección ordenada. Toda versión activa debe contener al menos un teléfono y exactamente uno principal. La edición inactiva la colección anterior e inserta la nueva para conservar trazabilidad sin eliminar registros.

## Autorización

| Permiso | Operaciones |
|---|---|
| `COMERCIAL.CONSULTAR` | Listado y detalle |
| `COMERCIAL.GESTIONAR` | Alta, edición y cambio de estado |

`ADMINISTRADOR` recibe ambos permisos. `CONSULTA` recibe únicamente permisos terminados en `.CONSULTAR`.

## Flujo técnico

```text
WinForms → API → ClientService → SqlClientStore → Stored Procedures → SQL Server
```

Todas las escrituras utilizan procedimientos almacenados, transacción controlada y auditoría funcional. Los datos personales completos no se copian al detalle JSON de auditoría.

## Objetos principales

- `COMERCIAL.CLIENTES` y `COMERCIAL.CLIENTES_TELEFONOS`.
- `COMERCIAL.SEQ_CLIENTE_CODIGO`.
- `SP_CLIENTE_LIST`, `SP_CLIENTE_GET`, `SP_CLIENTE_CREATE`, `SP_CLIENTE_UPDATE` y `SP_CLIENTE_STATUS_CHANGE`.
- `ClientService`, `SqlClientStore`, endpoints `/api/commercial/clients` y formularios `ClientManagementForm` / `ClientEditForm`.

## Catálogos y precarga

La carga utiliza tipos de documento, países y tipos de teléfono parametrizados. Permite guardar un borrador incompleto
por usuario y empresa y recuperarlo al volver a abrir **Nuevo cliente**.

Manual funcional: `docs/user-manuals/COMERCIAL_CLIENTES.md`.
