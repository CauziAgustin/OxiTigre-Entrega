# Estándares de desarrollo

## Objetivo

Todo componente debe poder entenderse sin depender de quien lo creó. La documentación debe explicar su responsabilidad, contrato, decisiones relevantes y cambios. Git conserva el historial oficial; la cabecera permite reconocer rápidamente el contexto funcional dentro del archivo.

## Metadatos obligatorios

Cada archivo SQL y C# creado por el proyecto debe indicar:

- proyecto;
- componente;
- archivo;
- versión;
- fecha de la versión actual en formato ISO `AAAA-MM-DD`;
- ID de pedido;
- nombre y apellido del desarrollador;
- correo;
- descripción funcional;
- historial de modificaciones.

Hasta disponer de un sistema de pedidos, el valor oficial es:

```text
ID pedido: FABRICA
```

El autor inicial es `Agustin Omar Cauzi <agustincauzi10@hotmail.com>`. La primera fila del historial registra la fecha
real de creación; las siguientes registran las modificaciones. `Fecha` coincide con la versión declarada en la cabecera,
no reemplaza la fecha de creación. Las fechas se recuperan de Git o del archivo original y nunca se inventan. Cada
modificación agrega una fila con el autor real; no se reemplazan entradas anteriores.

## Versionado del componente

Se utiliza `MAYOR.MENOR.PARCHE`:

- `MAYOR`: cambio incompatible del contrato;
- `MENOR`: nueva capacidad compatible;
- `PARCHE`: corrección compatible o documentación.

La primera versión es `1.0.0`. El número no reemplaza tags, commits ni releases de Git.

## SQL Server

### Objetos

- Schemas, tablas, columnas y procedimientos: `MAYUSCULAS_CON_GUION_BAJO`.
- Procedimientos: `<SCHEMA>.SP_<ENTIDAD_SINGULAR>_<ACCION>`.
- Un procedimiento por archivo.
- El archivo, el procedimiento y el campo `Componente` deben compartir el mismo nombre de SP.
- Siempre utilizar `CREATE OR ALTER PROCEDURE` y nombres de objetos calificados por schema.
- Las columnas compartidas se definen en [`DATA_DICTIONARY.md`](DATA_DICTIONARY.md) y deben conservar nombre, tipo, nulabilidad y significado.
- Las tablas nuevas deben partir de [`templates/sql/Table.sql.template`](../templates/sql/Table.sql.template).

Las consultas relacionadas que devuelven una pantalla completa pueden agruparse en un único Query, por ejemplo `SP_COMPRAS_GET`, para evitar viajes innecesarios. Las operaciones de un mismo agregado pueden consolidarse en un Command con `@I_ACCION` solamente cuando comparten límite transaccional, auditoría y módulo, como `LOGISTICA.SP_LOGISTICA_COMMAND`. En ese caso, la cabecera enumera cada acción admitida, su finalidad, los parámetros aplicables y el permiso que controla la API. Operaciones con permisos, transacciones o impacto claramente independientes se mantienen en SP separados.

### Homogeneidad de tablas

- Una FK conserva exactamente el nombre y tipo de la PK referenciada.
- `CODIGO_ESTADO NVARCHAR(30) NOT NULL` representa el estado actual con códigos legibles y homogéneos.
- Los SP validan `ENTIDAD + CODIGO_ESTADO` contra `CONFIGURACION.ESTADOS`; no se crea FK por decisión funcional.
- Las columnas de creación, modificación y concurrencia son siempre las definidas en el diccionario de datos.
- No se permite crear sinónimos para el mismo concepto.
- Los cambios del diccionario deben realizarse antes de usar una columna nueva.

Ejemplo:

```text
Archivo:       SP_ERROR_GET_BY_CODE.sql
Componente:    SP_ERROR_GET_BY_CODE
Procedimiento: AUDITORIA.SP_ERROR_GET_BY_CODE
```

### Parámetros y variables

| Prefijo | Uso | Ejemplo |
|---|---|---|
| `@I_` | parámetro de entrada | `@I_CODIGO_ERROR` |
| `@O_` | parámetro de salida | `@O_MENSAJE` |
| `@IO_` | entrada y salida | `@IO_NUMERO_PAGINA` |
| `@V_` | variable local escalar | `@V_FECHA_ACTUAL` |
| `@T_` | variable de tabla | `@T_RESULTADOS` |
| `@S_` | contexto seguro de sesión/servidor | `@S_ID_SESION` |
| `@C_` | valor constante dentro del SP | `@C_ESTADO_ACTIVO` |

Nombres homogéneos reservados:

```text
@S_ID_SESION
@S_ID_USUARIO
@S_ID_DISPOSITIVO
@S_IP_ORIGEN
@S_APLICACION_ORIGEN
@S_ID_CORRELACION
@O_CODIGO_ERROR
@O_MENSAJE
@O_FILAS_AFECTADAS
```

`SSN` puede confundirse con Social Security Number. Para el número de sesión se adopta `ID_SESION`, por ejemplo `@S_ID_SESION`.

### Reglas T-SQL

- `SET NOCOUNT ON` en todos los SP.
- `SET XACT_ABORT ON` en Commands con transacción.
- Prohibido `SELECT *`.
- Alias cortos pero semánticos: `USR`, `ROL`, `ERR`.
- Fechas técnicas en UTC mediante `SYSUTCDATETIME()`.
- `TRY/CATCH`, transacción y `THROW` para Commands que modifiquen varias entidades.
- No usar SQL dinámico genérico ni concatenar entradas del usuario.
- Los parámetros deben declarar longitud y precisión explícitas.
- Un Query no modifica estado funcional.
- Un Command informa filas afectadas y participa de la auditoría centralizada.
- Cada `SELECT`, `INSERT`, `UPDATE`, `DELETE`, `IF`, `BEGIN` y `END` se separa visualmente del bloque siguiente cuando cambia la intención de negocio.
- Las listas de columnas, condiciones y valores extensos se distribuyen en varias líneas con sangría consistente; no se comprimen varias sentencias en una línea.

### Resultados y errores controlados

- No utilizar `@@ERROR`: es un mecanismo legado cuyo valor cambia después de cada sentencia. Las fallas técnicas se capturan con `TRY/CATCH`, se revierten si corresponde y se propagan con `THROW`.
- Capturar `@@ROWCOUNT` inmediatamente después del `SELECT`, `INSERT`, `UPDATE` o `DELETE` que se desea comprobar.
- Cero filas en un listado, búsqueda opcional o borrador inexistente es un resultado válido.
- Cero filas al buscar o modificar un identificador concreto es un resultado funcional controlado.
- Todo resultado funcional controlado asigna `@O_CODIGO_ERROR` con un código existente en `AUDITORIA.CATALOGO_ERRORES`.
- Antes de reutilizar o solicitar un código nuevo, el desarrollador debe comprobar su existencia y significado con `AUDITORIA.SP_ERROR_GET_BY_CODE` o consultando el catálogo por módulo.
- `@O_MENSAJE` puede personalizarse para explicar el contexto puntual sin alterar la descripción general del código. Si se omite, `AUDITORIA.SP_ERROR_MESSAGE_RESOLVE` obtiene la descripción del catálogo.
- `AUDITORIA.SP_ERROR_MESSAGE_RESOLVE` rechaza códigos inexistentes o inactivos incluso cuando se informa un mensaje personalizado.
- Las fallas imprevistas de aplicación o infraestructura usan `70002` y una correlación. Si SQL Server no está disponible, el error se controla desde la API porque la misma base no puede registrar su propia indisponibilidad.

Ejemplo para una consulta por identificador:

```sql
SELECT ... FROM [COMERCIAL].[CLIENTES] WHERE [ID_CLIENTE] = @I_ID_CLIENTE;
IF @@ROWCOUNT = 0
BEGIN
    SET @O_CODIGO_ERROR = 40002;
    EXEC [AUDITORIA].[SP_ERROR_MESSAGE_RESOLVE]
        @I_CODIGO_ERROR = @O_CODIGO_ERROR,
        @I_MENSAJE_PERSONALIZADO = NULL,
        @O_MENSAJE = @O_MENSAJE OUTPUT;
END;
```

### Comentarios de bloques

Los bloques lógicos relevantes deben marcar el inicio y el final con la misma descripción:

```sql
-- INICIO: Validación del código de error.
IF @I_CODIGO_ERROR IS NULL
BEGIN
    THROW 50000, 'El código de error es obligatorio.', 1;
END; -- FIN: Validación del código de error.
```

Los `SELECT`, `INSERT`, `UPDATE` y `DELETE` deben tener un comentario cuando la intención de negocio no sea evidente. El comentario explica por qué se realiza la operación; no repite literalmente la sentencia.

### Documentación adicional en SQL Server

La cabecera del archivo es obligatoria. Las propiedades extendidas `MS_Description` se utilizarán para describir tablas y columnas y podrán utilizarse para SP públicos. No reemplazan el archivo versionado.

## API, BLL, DAL y contratos

### Nomenclatura C#

- namespaces, tipos, métodos y propiedades públicas: `PascalCase`;
- parámetros y variables locales: `camelCase`;
- campos privados: `_camelCase`;
- interfaces: prefijo `I`;
- métodos asíncronos: sufijo `Async`;
- booleanos: nombres afirmativos, por ejemplo `isActive` o `CanEdit`;
- evitar abreviaturas salvo términos oficiales del dominio.

No se aplican prefijos `v_`, `i_` u `o_` en C#: el tipo, la firma y el IDE ya expresan ese contexto. Esos prefijos quedan reservados para T-SQL.

### Comentarios XML

Todo tipo y método declarado, incluidos los internos y privados, debe usar documentación XML:

- `<summary>`: qué responsabilidad cumple;
- `<param>`: significado, unidad y restricciones del parámetro;
- `<returns>`: resultado y casos sin datos;
- `<exception>`: errores que el consumidor debe contemplar;
- `<remarks>`: reglas de negocio o decisiones no evidentes.

Una implementación puede usar `<inheritdoc />` cuando cumple sin cambios el contrato ya documentado por su interfaz o
clase base. No se agregan comentarios que solo repitan el nombre del método.

Esto permite que Visual Studio muestre la documentación al posicionarse sobre el símbolo. Los comentarios internos deben explicar decisiones y motivos, no traducir cada línea de código.

Los métodos se separan con una línea en blanco. Las validaciones, preparación de datos, acceso externo y retorno se organizan en bloques visuales distintos. No se comprimen múltiples sentencias, condiciones extensas ni cuerpos de métodos en una sola línea.

Se agregan solamente las etiquetas que correspondan: un método `void` no necesita `<returns>` y `<exception>` enumera únicamente excepciones que realmente puedan salir de la operación. Ejemplo:

```csharp
/// <summary>Revierte una recepción intacta mediante un movimiento compensatorio auditado.</summary>
/// <param name="goodsReceiptId">Identificador de la recepción confirmada.</param>
/// <param name="reason">Motivo obligatorio, con un máximo de 500 caracteres.</param>
/// <param name="cancellationToken">Token que permite cancelar la operación.</param>
/// <returns>Tarea que finaliza cuando la reversión queda persistida.</returns>
/// <exception cref="ArgumentException">El identificador o el motivo no son válidos.</exception>
/// <exception cref="UnauthorizedAccessException">La sesión no posee el permiso requerido.</exception>
public Task ReverseReceiptAsync(long goodsReceiptId, string reason, CancellationToken cancellationToken);
```

## Windows Forms

Además de las reglas C#:

| Control | Prefijo | Ejemplo |
|---|---|---|
| Formulario | `frm` | `frmUsuarios` |
| Botón | `btn` | `btnGuardar` |
| TextBox | `txt` | `txtNombre` |
| Label | `lbl` | `lblNombre` |
| ComboBox | `cmb` | `cmbEstado` |
| DataGridView | `dgv` | `dgvUsuarios` |
| CheckBox | `chk` | `chkActivo` |
| DateTimePicker | `dtp` | `dtpFechaDesde` |

Los eventos se nombran `<Control>_<Accion>`, por ejemplo `btnGuardar_Click`. La lógica de negocio no se implementa dentro del evento: el evento valida la interacción, llama al servicio y presenta el resultado.

Los formularios reutilizan `UiTheme` para evitar diferencias visuales entre módulos. Toda grilla de consulta debe ocupar su contenedor, mostrar un título cuando comparte pantalla con otra grilla y usar el filtro común por columna. Los botones emplean el color semántico compartido: primario, neutral, advertencia o peligro; deben conservar borde, foco de teclado, estado al pasar el puntero y estado presionado. Los campos editables muestran un placeholder breve cuando el tipo de control lo admite, sin reemplazar la etiqueta ni la validación.

## Política de modificación

En cada cambio funcional:

1. actualizar la versión del componente;
2. agregar fecha, ID de pedido, desarrollador y correo al historial;
3. resumir qué se modificó, sin reemplazar la descripción funcional;
4. actualizar `<summary>`, parámetros y documentación si cambió el contrato;
5. ejecutar build, tests y validación del repositorio;
6. incluir documentación y rollback en el Pull Request.

Una corrección exclusivamente documental no modifica la versión ni agrega una entrada de historial del componente.

## Propuestas adicionales adoptadas

- `.editorconfig` controla estilo básico y nombres de C#.
- `TreatWarningsAsErrors` mantiene los warnings como errores.
- La generación de documentación XML detecta métodos públicos, internos y privados sin documentar.
- CI valida cabeceras, coincidencia de nombre de SP y prefijos T-SQL.
- Los comentarios de historial no sustituyen una explicación completa en el commit y PR.
