# Auditoría funcional y técnica

## Ejecución de Stored Procedures

Todas las funcionalidades pasan por un ejecutor central:

```text
Repository
    ↓
StoredProcedureExecutor
    ├─ LOG BEGIN
    ├─ Stored Procedure
    └─ LOG END / ERROR
```

`AUDITORIA.LOGUEO_FUNCIONALIDADES` registra:

- funcionalidad y PKEY;
- empresa, módulo, pantalla, acción y caso de uso;
- usuario, sesión y dispositivo;
- schema y Stored Procedure;
- parámetros y outputs redactados;
- timestamps UTC, duración y filas afectadas;
- resultado, código y detalle de error;
- equipo, IP, origen y `correlation_id`.

Los SP de auditoría se excluyen para evitar recursión.

## Catálogo de errores

- `CATALOGO_ERRORES` define cada error.
- `ERRORES_APLICACION` registra cada ocurrencia.
- `SOLUCIONES_ERROR` conserva diagnósticos y procedimientos de solución.

Stored Procedures iniciales implementados:

```text
AUDITORIA.SP_ERROR_CREATE
AUDITORIA.SP_ERROR_GET_BY_CODE
AUDITORIA.SP_ERROR_OCURRENCIA_CREATE
AUDITORIA.SP_LOGUEO_FUNCIONALIDAD_CREATE
```

`SqlStoredProcedureAuditWriter` es el único escritor desde .NET. Recibe parámetros previamente redactados y el SP de auditoría se excluye a sí mismo para evitar recursión.

La API devuelve errores esperables con `CODIGO_ERROR`, mensaje legible y `CORRELATION_ID`. Los errores técnicos imprevistos se transforman en el código `70002` y registran su ocurrencia sin exponer detalles internos. Las validaciones, credenciales y permisos usan los códigos `10001` a `10004`; la carga comercial utiliza `40001`.
