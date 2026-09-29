/*
===============================================================================
Proyecto:              Sistema Modular de Gestión OxiTigre
Componente:            Seed de errores iniciales
Archivo:               06_ERRORES_INICIALES.sql
Versión:               1.7.0
Fecha:                 2026-08-26
ID pedido:             FABRICA
Desarrollador:         Agustin Omar Cauzi
Correo:                agustincauzi10@hotmail.com
Descripción funcional: Registra el primer error del módulo Auditoría y su solución.
Historial de modificaciones:
1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
1.1.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Errores controlados iniciales de API y validación.
1.2.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Sesión no vigente, cliente no encontrado y catálogos incompletos.
1.3.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Errores controlados de Configuración y administración del catálogo.
1.4.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Validación, inexistencia, concurrencia y saldo de Inventario.
1.5.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Validación, stock, transición y concurrencia de pedidos y ventas.
1.6.0 | 2026-08-21 | FABRICA | Agustin Omar Cauzi | Definición y elegibilidad controladas de promociones.
1.7.0 | 2026-08-26 | FABRICA | Agustin Omar Cauzi | Validaciones y transiciones controladas de Logística.
===============================================================================
*/
DECLARE @V_ID_MODULO BIGINT = (SELECT [ID_MODULO] FROM [CONFIGURACION].[MODULOS] WHERE [CODIGO] = N'AUDITORIA');

IF NOT EXISTS (SELECT 1 FROM [AUDITORIA].[CATALOGO_ERRORES] WHERE [CODIGO_ERROR] = 70001)
BEGIN
    INSERT INTO [AUDITORIA].[CATALOGO_ERRORES]
    (
        [ID_MODULO], [NUMERO_ERROR], [CODIGO_ERROR], [NOMBRE], [DESCRIPCION],
        [CAUSA_PROBABLE], [ACCION_RECOMENDADA], [SEVERIDAD], [CODIGO_ESTADO], [ID_USUARIO_ALTA]
    )
    VALUES
    (
        @V_ID_MODULO, 1, 70001, N'CÓDIGO DE ERROR NO ENCONTRADO',
        N'El código solicitado no existe o no está activo en el catálogo.',
        N'Código incorrecto, dato aún no registrado o registro inactivo.',
        N'Verificar el código y registrar el diagnóstico mediante AUDITORIA.SP_ERROR_CREATE si corresponde.',
        N'ADVERTENCIA', N'ACTIVO', 1
    );
END;

-- INICIO: Errores funcionales controlados de Logística Fase 8.
INSERT INTO [AUDITORIA].[CATALOGO_ERRORES]
    ([ID_MODULO],[NUMERO_ERROR],[CODIGO_ERROR],[NOMBRE],[DESCRIPCION],[CAUSA_PROBABLE],[ACCION_RECOMENDADA],[SEVERIDAD],[CODIGO_ESTADO],[ID_USUARIO_ALTA])
SELECT [M].[ID_MODULO],[E].[NUMERO_ERROR],[E].[CODIGO_ERROR],[E].[NOMBRE],[E].[DESCRIPCION],[E].[CAUSA],[E].[ACCION],N'ADVERTENCIA',N'ACTIVO',1
FROM (VALUES
    (CONVERT(SMALLINT,1),CONVERT(BIGINT,60001),N'VALIDACIÓN DE LOGÍSTICA',N'Los datos o el estado no permiten la operación logística.',N'Campos incompletos, relación ajena, horario o transición inválida.',N'Actualizar la pantalla, corregir los datos y volver a intentar.'),
    (CONVERT(SMALLINT,2),CONVERT(BIGINT,60002),N'SOLICITUD NO ENCONTRADA',N'La solicitud no existe o pertenece a otra empresa.',N'Identificador desactualizado o aislamiento de empresa.',N'Actualizar la agenda y seleccionar una solicitud vigente.'),
    (CONVERT(SMALLINT,3),CONVERT(BIGINT,60003),N'RUTA MODIFICADA',N'La hoja o parada cambió desde que fue consultada.',N'Otro usuario confirmó o modificó el registro.',N'Actualizar la hoja antes de continuar.'),
    (CONVERT(SMALLINT,4),CONVERT(BIGINT,60004),N'ERROR CONTROLADO DE LOGÍSTICA',N'No se pudo completar la operación de forma segura.',N'Error SQL, restricción o dato inconsistente.',N'Informar código y correlación al equipo de desarrollo.')
) AS [E] ([NUMERO_ERROR],[CODIGO_ERROR],[NOMBRE],[DESCRIPCION],[CAUSA],[ACCION])
CROSS JOIN (SELECT [ID_MODULO] FROM [CONFIGURACION].[MODULOS] WHERE [CODIGO]=N'LOGISTICA') AS [M]
WHERE NOT EXISTS (SELECT 1 FROM [AUDITORIA].[CATALOGO_ERRORES] WHERE [CODIGO_ERROR]=[E].[CODIGO_ERROR]);
-- FIN: Errores funcionales controlados de Logística Fase 8.

-- INICIO: Catálogo mínimo utilizado por las respuestas controladas de la API.
INSERT INTO [AUDITORIA].[CATALOGO_ERRORES]
    ([ID_MODULO], [NUMERO_ERROR], [CODIGO_ERROR], [NOMBRE], [DESCRIPCION], [CAUSA_PROBABLE],
     [ACCION_RECOMENDADA], [SEVERIDAD], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
SELECT [MOD].[ID_MODULO], [E].[NUMERO_ERROR], [E].[CODIGO_ERROR], [E].[NOMBRE], [E].[DESCRIPCION],
       [E].[CAUSA], [E].[ACCION], [E].[SEVERIDAD], N'ACTIVO', 1
FROM (VALUES
    (N'SEGURIDAD', CONVERT(SMALLINT, 1), CONVERT(BIGINT, 10001), N'CREDENCIALES INVÁLIDAS',
     N'Las credenciales o la empresa seleccionada no permiten iniciar sesión.', N'Usuario, clave, empresa, estado o bloqueo temporal.',
     N'Verificar los datos sin revelar cuál de ellos es incorrecto.', N'ADVERTENCIA'),
    (N'SEGURIDAD', CONVERT(SMALLINT, 2), CONVERT(BIGINT, 10002), N'POLÍTICA DE CONTRASEÑA',
     N'La contraseña nueva no cumple la política vigente.', N'Campo vacío, confirmación distinta o complejidad insuficiente.',
     N'Ingresar al menos 10 caracteres con mayúscula, minúscula y número.', N'ADVERTENCIA'),
    (N'SEGURIDAD', CONVERT(SMALLINT, 3), CONVERT(BIGINT, 10003), N'VALIDACIÓN DE USUARIO',
     N'Los datos, roles, estado o acción administrativa no son válidos.', N'Datos incompletos, duplicados o protección del administrador.',
     N'Revisar los datos informados y conservar al menos un administrador activo.', N'ADVERTENCIA'),
    (N'SEGURIDAD', CONVERT(SMALLINT, 4), CONVERT(BIGINT, 10004), N'PERMISO INSUFICIENTE',
     N'El usuario autenticado no posee el permiso requerido para la operación.', N'Rol sin permiso o acceso a una función no habilitada.',
     N'Solicitar a un administrador la asignación del rol correspondiente.', N'ADVERTENCIA'),
    (N'SEGURIDAD', CONVERT(SMALLINT, 5), CONVERT(BIGINT, 10005), N'SESIÓN NO VIGENTE',
     N'La sesión fue cerrada, revocada o alcanzó su vencimiento.', N'Cierre voluntario, revocación administrativa o expiración.',
     N'Cerrar la aplicación e iniciar sesión nuevamente si corresponde.', N'ADVERTENCIA'),
    (N'SEGURIDAD', CONVERT(SMALLINT, 6), CONVERT(BIGINT, 10006), N'ROLES NO DISPONIBLES',
     N'No existen roles activos configurados para la empresa.', N'Configuración inicial incompleta o roles inactivos.',
     N'Revisar el catálogo de roles antes de administrar usuarios.', N'ERROR'),
    (N'COMERCIAL', CONVERT(SMALLINT, 1), CONVERT(BIGINT, 40001), N'VALIDACIÓN DE CLIENTE',
     N'La información del cliente, documento, teléfono o borrador no es válida.', N'Dato obligatorio, formato, catálogo o duplicado inválido.',
     N'Corregir los campos indicados por la pantalla y volver a guardar.', N'ADVERTENCIA'),
    (N'COMERCIAL', CONVERT(SMALLINT, 2), CONVERT(BIGINT, 40002), N'CLIENTE NO ENCONTRADO',
     N'El cliente solicitado no existe o no pertenece a la empresa de la sesión.', N'Identificador inexistente, empresa distinta o registro eliminado.',
     N'Actualizar el listado y volver a seleccionar un cliente válido.', N'ADVERTENCIA'),
    (N'COMERCIAL', CONVERT(SMALLINT, 3), CONVERT(BIGINT, 40003), N'CATÁLOGOS DE CLIENTE INCOMPLETOS',
     N'Falta al menos un catálogo obligatorio para cargar clientes.', N'Tipos de documento, países o tipos de teléfono sin registros activos.',
     N'Revisar la configuración de catálogos antes de continuar.', N'ERROR'),
    (N'COMERCIAL', CONVERT(SMALLINT, 4), CONVERT(BIGINT, 40004), N'VALIDACIÓN DE PEDIDO O VENTA',
     N'Los datos de la lista, precio, pedido o venta no son válidos.', N'Campos incompletos, moneda, porcentaje o relación inválida.',
     N'Revisar los datos indicados y volver a guardar.', N'ADVERTENCIA'),
    (N'COMERCIAL', CONVERT(SMALLINT, 5), CONVERT(BIGINT, 40005), N'STOCK NO DISPONIBLE PARA EL PEDIDO',
     N'El stock físico disponible no alcanza para reservar o vender el pedido.', N'Existencia insuficiente o cantidad ya comprometida por otros pedidos.',
     N'Revisar depósito, cantidad, movimientos y reservas vigentes.', N'ADVERTENCIA'),
    (N'COMERCIAL', CONVERT(SMALLINT, 6), CONVERT(BIGINT, 40006), N'TRANSICIÓN COMERCIAL INVÁLIDA',
     N'El estado actual no permite confirmar, cancelar o vender el pedido.', N'Operación repetida, estado final o modificación concurrente.',
     N'Actualizar la pantalla y revisar el estado vigente.', N'ADVERTENCIA'),
    (N'COMERCIAL', CONVERT(SMALLINT, 7), CONVERT(BIGINT, 40007), N'PEDIDO O PRECIO MODIFICADO',
     N'Otro usuario modificó el pedido o el precio mientras estaba abierto.', N'La versión enviada no coincide con la almacenada.',
     N'Actualizar los datos, revisar los cambios y volver a intentar.', N'ADVERTENCIA'),
    (N'COMERCIAL', CONVERT(SMALLINT, 8), CONVERT(BIGINT, 40008), N'REGLA PROMOCIONAL INVÁLIDA',
     N'La definición o combinación de descuentos de la promoción no es válida.', N'Parámetros incompatibles, código duplicado o descuento manual combinado con promoción.',
     N'Revisar el tipo de promoción y completar únicamente los valores que le corresponden.', N'ADVERTENCIA'),
    (N'COMERCIAL', CONVERT(SMALLINT, 9), CONVERT(BIGINT, 40009), N'PROMOCIÓN NO APLICABLE',
     N'La promoción seleccionada no puede aplicarse al renglón y fecha del pedido.', N'Promoción inactiva, fuera de vigencia, de otro producto o de otra empresa.',
     N'Actualizar promociones y seleccionar una vigente para el producto del pedido.', N'ADVERTENCIA'),
    (N'COMERCIAL', CONVERT(SMALLINT, 10), CONVERT(BIGINT, 40010), N'ACTIVO NO APLICABLE AL PEDIDO',
     N'El tubo o activo seleccionado no puede vincularse al cliente, renglón o finalidad indicada.', N'Propietario incorrecto, activo inexistente o renglón eliminado.',
     N'Actualizar el pedido y revisar propietario, finalidad, retiro o entrega y observación.', N'ADVERTENCIA'),
    (N'CONFIGURACION', CONVERT(SMALLINT, 1), CONVERT(BIGINT, 20001), N'VALIDACIÓN DE CONFIGURACIÓN',
     N'Los datos informados para la configuración no son válidos.', N'Campo obligatorio, formato, catálogo o valor fuera de rango.',
     N'Corregir los datos indicados y volver a guardar.', N'ADVERTENCIA'),
    (N'CONFIGURACION', CONVERT(SMALLINT, 2), CONVERT(BIGINT, 20002), N'CONFIGURACIÓN NO ENCONTRADA',
     N'El registro solicitado no existe o no pertenece a la empresa de la sesión.', N'Identificador inexistente, empresa distinta o registro eliminado.',
     N'Actualizar el listado y volver a seleccionar un registro válido.', N'ADVERTENCIA'),
    (N'CONFIGURACION', CONVERT(SMALLINT, 3), CONVERT(BIGINT, 20003), N'CONFIGURACIÓN MODIFICADA',
     N'Otro usuario modificó el registro mientras estaba abierto.', N'La versión enviada ya no coincide con la versión guardada.',
     N'Actualizar los datos, revisar los cambios y volver a guardar.', N'ADVERTENCIA'),
    (N'CONFIGURACION', CONVERT(SMALLINT, 4), CONVERT(BIGINT, 20004), N'CONFIGURACIÓN PROTEGIDA',
     N'La operación no puede realizarse porque el registro es base o posee dependencias activas.', N'Código técnico en uso o estructura relacionada activa.',
     N'Revisar las dependencias y conservar los códigos estables.', N'ADVERTENCIA'),
    (N'AUDITORIA', CONVERT(SMALLINT, 2), CONVERT(BIGINT, 70002), N'ERROR INTERNO CONTROLADO',
     N'La API interceptó una excepción técnica no prevista.', N'Fallo de infraestructura, programación o datos inconsistentes.',
     N'Informar código y correlación al equipo; consultar logs y ocurrencia registrada.', N'ERROR'),
    (N'INVENTARIO', CONVERT(SMALLINT, 1), CONVERT(BIGINT, 30001), N'VALIDACIÓN DE INVENTARIO',
     N'Los datos del producto, depósito, ubicación o movimiento no son válidos.', N'Campos incompletos, catálogos inactivos o relación incompatible.',
     N'Revisar los datos indicados y volver a guardar.', N'ADVERTENCIA'),
    (N'INVENTARIO', CONVERT(SMALLINT, 2), CONVERT(BIGINT, 30002), N'REGISTRO DE INVENTARIO NO ENCONTRADO',
     N'El registro solicitado no existe o no pertenece a la empresa autenticada.', N'Identificador inexistente o aislamiento de empresa.',
     N'Actualizar el listado y seleccionar un registro válido.', N'ADVERTENCIA'),
    (N'INVENTARIO', CONVERT(SMALLINT, 3), CONVERT(BIGINT, 30003), N'INVENTARIO MODIFICADO',
     N'Otro usuario modificó el registro mientras estaba abierto.', N'La versión enviada no coincide con la almacenada.',
     N'Actualizar los datos, revisar los cambios y volver a guardar.', N'ADVERTENCIA'),
    (N'INVENTARIO', CONVERT(SMALLINT, 4), CONVERT(BIGINT, 30004), N'STOCK INSUFICIENTE',
     N'El depósito de origen no posee cantidad suficiente para confirmar el movimiento.', N'Salida, transferencia o ajuste mayor al saldo disponible.',
     N'Revisar el depósito, la cantidad y los movimientos anteriores.', N'ADVERTENCIA'),
    (N'INVENTARIO', CONVERT(SMALLINT, 5), CONVERT(BIGINT, 30005), N'MEDICIÓN DE ACTIVO INVÁLIDA',
     N'La medición no puede asociarse al activo indicado.', N'Activo inexistente, empresa distinta o datos inválidos.',
     N'Actualizar activos y revisar valor, unidad, método y fecha.', N'ADVERTENCIA'),
    (N'INVENTARIO', CONVERT(SMALLINT, 6), CONVERT(BIGINT, 30006), N'FRACCIONAMIENTO INVÁLIDO',
     N'No se pudo conservar la igualdad entre origen, destinos y merma.', N'Saldo insuficiente o cantidades inconsistentes.',
     N'Revisar que el origen sea igual a destinos más merma.', N'ADVERTENCIA'),
    (N'INVENTARIO', CONVERT(SMALLINT, 7), CONVERT(BIGINT, 30007), N'INCIDENTE INVÁLIDO',
     N'No se pudo registrar el incidente y su ajuste.', N'Activo, lote, depósito o cantidades incompatibles.',
     N'Revisar la información anterior y posterior al incidente.', N'ADVERTENCIA'),
    (N'INVENTARIO', CONVERT(SMALLINT, 8), CONVERT(BIGINT, 30008), N'MANTENIMIENTO INVÁLIDO',
     N'El mantenimiento no admite la operación.', N'Orden abierta, versión concurrente o fecha incompatible.',
     N'Actualizar la pantalla y revisar el estado del activo.', N'ADVERTENCIA'),
    (N'INVENTARIO', CONVERT(SMALLINT, 9), CONVERT(BIGINT, 30009), N'PRÉSTAMO INVÁLIDO',
     N'El préstamo o la devolución no puede confirmarse.', N'Activo no disponible, devolución incompleta o versión concurrente.',
     N'Revisar activos y snapshots de salida y devolución.', N'ADVERTENCIA'),
    (N'INVENTARIO', CONVERT(SMALLINT, 10), CONVERT(BIGINT, 30010), N'ACTIVO DE CLIENTE INVÁLIDO',
     N'El activo del cliente o su movimiento de custodia no puede confirmarse.', N'Cliente, activo, depósito, estado o versión incompatible.',
     N'Actualizar la pantalla y revisar propietario, ubicación y observación.', N'ADVERTENCIA'),
    (N'COMPRAS', CONVERT(SMALLINT, 1), CONVERT(BIGINT, 50001), N'VALIDACIÓN DE COMPRAS',
     N'Los datos del proveedor, orden o recepción no son válidos.', N'Campos, cantidades o relaciones incompatibles.',
     N'Corregir los datos y volver a guardar.', N'ADVERTENCIA'),
    (N'COMPRAS', CONVERT(SMALLINT, 2), CONVERT(BIGINT, 50002), N'REGISTRO DE COMPRAS NO ENCONTRADO',
     N'El registro no existe o pertenece a otra empresa.', N'Identificador inexistente o empresa distinta.',
     N'Actualizar el listado y volver a seleccionar.', N'ADVERTENCIA'),
    (N'COMPRAS', CONVERT(SMALLINT, 3), CONVERT(BIGINT, 50003), N'TRANSICIÓN DE COMPRA INVÁLIDA',
     N'El estado actual no permite la operación.', N'Operación repetida, estado final o modificación concurrente.',
     N'Actualizar la pantalla y revisar el estado vigente.', N'ADVERTENCIA'),
    (N'COMPRAS', CONVERT(SMALLINT, 4), CONVERT(BIGINT, 50004), N'PERMISO O VALIDACIÓN DE COMPRA',
     N'La sesión o los datos no permiten la operación.', N'Permiso insuficiente o entrada inválida.',
     N'Revisar permisos y datos informados.', N'ADVERTENCIA'),
    (N'COMPRAS', CONVERT(SMALLINT, 5), CONVERT(BIGINT, 50005), N'SOBREENTREGA',
     N'La recepción supera el saldo pendiente.', N'Cantidad mayor que la orden pendiente.',
     N'Corregir cantidades o gestionar una nueva orden.', N'ADVERTENCIA'),
    (N'COMPRAS', CONVERT(SMALLINT, 6), CONVERT(BIGINT, 50006), N'REMITO DUPLICADO',
     N'El número de remito ya fue registrado.', N'Documento repetido para la empresa.',
     N'Verificar el documento antes de confirmar.', N'ADVERTENCIA'),
    (N'COMPRAS', CONVERT(SMALLINT, 7), CONVERT(BIGINT, 50007), N'TRAZABILIDAD DE RECEPCIÓN INVÁLIDA',
     N'Lotes, series o contenido no explican la cantidad aceptada.', N'Identificadores faltantes, repetidos o inconsistentes.',
     N'Revisar lotes, series y contenido antes de confirmar.', N'ADVERTENCIA'),
    (N'COMPRAS', CONVERT(SMALLINT, 8), CONVERT(BIGINT, 50008), N'REVERSIÓN DE RECEPCIÓN NO DISPONIBLE',
     N'La recepción no puede reversarse sin afectar la trazabilidad.', N'El stock, los lotes o las series ya tuvieron operaciones posteriores.',
     N'Revisar el historial y registrar una corrección específica.', N'ADVERTENCIA'),
    (N'AUDITORIA', CONVERT(SMALLINT, 3), CONVERT(BIGINT, 70003), N'VALIDACIÓN DEL CATÁLOGO DE ERRORES',
     N'Los datos del código de error no son válidos o el registro está protegido.', N'Datos incompletos, severidad inválida o código técnico inmutable.',
     N'Revisar el módulo y la definición funcional antes de guardar.', N'ADVERTENCIA')
) AS [E] ([MODULO], [NUMERO_ERROR], [CODIGO_ERROR], [NOMBRE], [DESCRIPCION], [CAUSA], [ACCION], [SEVERIDAD])
INNER JOIN [CONFIGURACION].[MODULOS] AS [MOD] ON [MOD].[CODIGO] = [E].[MODULO]
WHERE NOT EXISTS (SELECT 1 FROM [AUDITORIA].[CATALOGO_ERRORES] WHERE [CODIGO_ERROR] = [E].[CODIGO_ERROR]);
-- FIN: Catálogo mínimo utilizado por las respuestas controladas de la API.

DECLARE @V_ID_CATALOGO_ERROR BIGINT =
(
    SELECT [ID_CATALOGO_ERROR] FROM [AUDITORIA].[CATALOGO_ERRORES] WHERE [CODIGO_ERROR] = 70001
);

IF NOT EXISTS
(
    SELECT 1 FROM [AUDITORIA].[SOLUCIONES_ERROR]
    WHERE [ID_CATALOGO_ERROR] = @V_ID_CATALOGO_ERROR AND [ES_PREFERIDA] = 1 AND [CODIGO_ESTADO] = N'ACTIVO'
)
BEGIN
    INSERT INTO [AUDITORIA].[SOLUCIONES_ERROR]
    (
        [ID_CATALOGO_ERROR], [TITULO], [DESCRIPCION], [PASOS_SOLUCION],
        [ES_PREFERIDA], [CODIGO_ESTADO], [ID_USUARIO_ALTA]
    )
    VALUES
    (
        @V_ID_CATALOGO_ERROR, N'Verificar y documentar el código',
        N'Confirma que módulo y correlativo sean correctos antes de registrar un nuevo error.',
        N'1. Confirmar el módulo funcional. 2. Consultar el código. 3. Si no existe, solicitar su alta controlada.',
        1, N'ACTIVO', 1
    );
END;
