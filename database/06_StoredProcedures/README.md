# Stored Procedures

Convención: `<SCHEMA>.SP_<ENTIDAD_SINGULAR>_<ACCION>`.

La cabecera, nomenclatura de parámetros, comentarios de bloques, manejo de errores e historial obligatorio se definen en [`docs/DEVELOPMENT_STANDARDS.md`](../../docs/DEVELOPMENT_STANDARDS.md). Todo SP nuevo debe partir de [`templates/sql/StoredProcedure.sql.template`](../../templates/sql/StoredProcedure.sql.template).

Los procedimientos funcionales se incorporarán módulo por módulo después de aprobar tablas, constraints e índices. Los primeros SP permitidos serán los necesarios para auditoría, catálogo de errores y validación de la instalación.
