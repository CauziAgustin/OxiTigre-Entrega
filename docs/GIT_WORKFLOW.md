# Flujo Git de la entrega

`entrega-profesor` es la única rama de este repositorio privado. Contiene un snapshot autocontenido del sistema de
escritorio y no expone las ramas de trabajo del repositorio de desarrollo. La fecha del commit de entrega es la fecha
real de publicación; las fechas históricas de desarrollo se conservan en Git de origen y en las cabeceras de cada
componente, sin inventarlas.

La integración continua compila y valida este corte en cada push. Las mejoras futuras se realizan en el repositorio
de desarrollo y solo se incorporan aquí después de verificar el alcance, documentación, pruebas y ausencia de secretos.
