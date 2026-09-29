/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: SP_LOGISTICA_COMMAND
Archivo: SP_LOGISTICA_COMMAND.sql | Procedimiento: LOGISTICA.SP_LOGISTICA_COMMAND | Tipo: COMMAND
Versión: 1.3.2 | Fecha: 2026-09-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Ejecuta las acciones acotadas del circuito logístico y conserva su trazabilidad.
Parámetros de entrada: @I_ACCION, @I_ANIO, @I_CAPACIDAD, @I_CATEGORIA, @I_CHOFER, @I_CODIGO_ESTADO, @I_CODIGO_POSTAL, @I_CONTACTO, @I_CORREO, @I_DOCUMENTO, @I_DOMICILIO, @I_FECHA, @I_HORA_DESDE, @I_HORA_HASTA, @I_ID, @I_ID_CLIENTE, @I_ID_DEPOSITO, @I_ID_DIRECCION, @I_ID_EMPRESA, @I_ID_PEDIDO, @I_ID_TRANSPORTISTA, @I_ID_USUARIO, @I_ID_VEHICULO, @I_INSTRUCCIONES, @I_JSON, @I_LICENCIA, @I_LOCALIDAD, @I_MARCA, @I_MODELO, @I_NOMBRE, @I_OBSERVACION, @I_PATENTE, @I_POLIZA, @I_PRIORIDAD, @I_PROVINCIA, @I_RESULTADO, @I_ROW_VERSION, @I_TELEFONO, @I_TIPO, @I_TIPO_ASIGNACION, @I_TIPO_PROPIEDAD, @I_VENCIMIENTO, @I_VENCIMIENTO_REVISION, @I_VENCIMIENTO_SEGURO, @S_ID_SESION, @S_ID_USUARIO.
Parámetros de salida: @O_CODIGO_ERROR, @O_FILAS_AFECTADAS, @O_ID, @O_MENSAJE.
Retorno: Informa el resultado mediante los parámetros de salida declarados.
Tablas utilizadas: COMERCIAL.CLIENTES - SELECT; COMERCIAL.PEDIDOS - SELECT; COMERCIAL.PEDIDOS_ACTIVOS - SELECT; CONFIGURACION.UNIDADES_MEDIDA - SELECT; INVENTARIO.ACTIVOS - SELECT; INVENTARIO.ACTIVOS_CONTENIDOS - SELECT; INVENTARIO.ACTIVOS_EVENTOS - INSERT; INVENTARIO.DEPOSITOS - SELECT; INVENTARIO.PRESTAMOS - INSERT; INVENTARIO.PRESTAMOS_DETALLES - INSERT; INVENTARIO.PRODUCTOS - SELECT; LOGISTICA.CLIENTES_DIRECCIONES - INSERT/SELECT/UPDATE; LOGISTICA.EVENTOS - INSERT; LOGISTICA.HOJAS_RUTA - INSERT/SELECT/UPDATE; LOGISTICA.HOJAS_RUTA_PARADAS - INSERT/SELECT/UPDATE; LOGISTICA.NOTIFICACIONES - INSERT; LOGISTICA.SOLICITUDES - INSERT/SELECT/UPDATE; LOGISTICA.SOLICITUDES_ACTIVOS - INSERT/SELECT; LOGISTICA.TRANSPORTISTAS - INSERT/SELECT/UPDATE; LOGISTICA.VEHICULOS - INSERT/SELECT/UPDATE; SEGURIDAD.ROLES - SELECT; SEGURIDAD.USUARIOS - SELECT; SEGURIDAD.USUARIOS_ROLES - SELECT.
Transacción: Abre una transacción explícita para agrupar sus escrituras.
Auditoría: No registra auditoría explícita dentro del procedimiento.
Acciones admitidas mediante @I_ACCION:
- DIRECCION_GUARDAR: usar cuando se registra un punto de atención de un cliente.
  Requiere @I_ID_CLIENTE, @I_NOMBRE, @I_DOMICILIO, @I_CONTACTO,
  @I_TELEFONO, @I_INSTRUCCIONES y, si se informa una ventana, ambas horas.
  Permiso API: LOGISTICA.GESTIONAR.
- SOLICITUD_CREAR: usar cuando se agenda una entrega, retiro, intercambio,
  traslado o urgencia. Requiere @I_ID_CLIENTE, @I_ID_DIRECCION, @I_TIPO,
  @I_PRIORIDAD, @I_FECHA e @I_INSTRUCCIONES. Los servicios con custodia
  también requieren los activos en @I_JSON y el depósito receptor en
  @I_ID_DEPOSITO. Si nace de un pedido, @I_ID_PEDIDO es la fuente autoritativa.
  Permiso API: LOGISTICA.GESTIONAR.
- RUTA_CREAR: usar cuando el operador agrupa manualmente solicitudes pendientes.
  Requiere @I_FECHA, @I_TIPO, @I_TIPO_ASIGNACION, @I_OBSERVACION y las
  solicitudes ordenadas en @I_JSON. DIRECTA también requiere transportista y
  vehículo; OFERTA debe dejarlos vacíos. Una URGENTE admite una sola solicitud.
  Permiso API: LOGISTICA.GESTIONAR.
- RUTA_ASIGNAR: asigna desde la oficina una hoja OFRECIDA. Requiere @I_ID,
  @I_ID_TRANSPORTISTA, @I_ID_VEHICULO y @I_ROW_VERSION.
  Permiso API: LOGISTICA.GESTIONAR.
- RUTA_TOMAR_OFERTA: asigna atómicamente una oferta al transportista de la
  sesión. Requiere @I_ID, @I_ID_VEHICULO y @I_ROW_VERSION.
  Permiso API: LOGISTICA.DESPACHAR y rol TRANSPORTISTA.
- RUTA_OFERTA_ACTUALIZAR: cambia fecha, tipo y observación de una hoja que
  continúa OFRECIDA. Requiere @I_ID, @I_FECHA, @I_TIPO, @I_OBSERVACION y
  @I_ROW_VERSION. Permiso API: LOGISTICA.GESTIONAR.
- TRANSPORTISTA_GUARDAR: crea o modifica el perfil operativo de un usuario
  que ya posee el rol TRANSPORTISTA. Requiere @I_ID_USUARIO,
  @I_TIPO_PROPIEDAD, @I_LICENCIA y @I_CODIGO_ESTADO. Para modificar también
  requiere @I_ID y @I_ROW_VERSION. Permiso API: LOGISTICA.GESTIONAR.
- VEHICULO_GUARDAR: crea o modifica un vehículo propio o particular. Requiere
  @I_PATENTE, @I_TIPO, @I_TIPO_PROPIEDAD y @I_CODIGO_ESTADO. Si pertenece a
  un particular requiere @I_ID_TRANSPORTISTA. Para modificar también requiere
  @I_ID y @I_ROW_VERSION. Permiso API: LOGISTICA.GESTIONAR.
- RUTA_DESPACHAR: usar cuando el vehículo inicia una hoja planificada.
  Requiere @I_ID y @I_ROW_VERSION de la hoja.
  Permiso API: LOGISTICA.DESPACHAR.
- RUTA_PAUSAR / RUTA_REANUDAR: detiene temporalmente o continúa una hoja en
  ejecución sin liberar solicitudes ni carga. Requiere @I_ID, @I_ROW_VERSION
  y @I_OBSERVACION. Permiso API: LOGISTICA.DESPACHAR.
- PARADA_LLEGADA: usar cuando el transportista llega físicamente al domicilio.
  Requiere @I_ID y @I_ROW_VERSION de la parada.
  Permiso API: LOGISTICA.DESPACHAR.
- PARADA_INCIDENCIA: usar para informar una demora o inconveniente sin cerrar
  la visita. Requiere @I_ID, @I_ROW_VERSION, @I_RESULTADO como categoría y
  @I_OBSERVACION con el detalle.
  Permiso API: LOGISTICA.DESPACHAR.
- PARADA_CONFIRMAR: usar al finalizar una visita, incluso si fue parcial,
  fallida o debe reprogramarse. Requiere @I_ID, @I_ROW_VERSION,
  @I_RESULTADO y @I_OBSERVACION. Para un resultado PARCIAL, @I_JSON identifica
  solamente los activos efectivamente operados.
  Permiso API: LOGISTICA.DESPACHAR.
- RUTA_CANCELAR: usar antes del despacho para anular una hoja y liberar sus
  solicitudes. Requiere @I_ID, @I_ROW_VERSION y @I_OBSERVACION.
  Permiso API: LOGISTICA.GESTIONAR.
Uso: enviar solamente la acción necesaria y los parámetros indicados para ella;
los restantes deben conservar NULL.
Historial: 1.0.0 | 2026-08-26 | FABRICA | Agustin Omar Cauzi | Creación inicial consolidada por acción.
Historial: 1.1.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Maestros de transporte y asignación histórica a rutas.
Historial: 1.2.0 | 2026-08-28 | FABRICA | Agustin Omar Cauzi | Integración con pedidos y custodia automática con préstamos formales.
Historial: 1.3.0 | 2026-08-29 | FABRICA | Agustin Omar Cauzi | Llegada e incidencias móviles trazables.
Historial: 1.3.1 | 2026-08-29 | FABRICA | Agustin Omar Cauzi | Bloqueo optimista durante operaciones concurrentes de parada.
Historial: 1.3.2 | 2026-09-20 | FABRICA | Agustin Omar Cauzi | Documentación técnica completa del procedimiento.
===============================================================================
*/
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE [LOGISTICA].[SP_LOGISTICA_COMMAND]
    @I_ACCION NVARCHAR(30),
    @I_ID_EMPRESA BIGINT,
    @S_ID_SESION BIGINT,
    @S_ID_USUARIO BIGINT,
    @I_ID BIGINT = NULL,
    @I_ID_CLIENTE BIGINT = NULL,
    @I_ID_DIRECCION BIGINT = NULL,
    @I_ID_PEDIDO BIGINT = NULL,
    @I_ID_DEPOSITO BIGINT = NULL,
    @I_ID_USUARIO BIGINT = NULL,
    @I_ID_TRANSPORTISTA BIGINT = NULL,
    @I_ID_VEHICULO BIGINT = NULL,
    @I_NOMBRE NVARCHAR(200) = NULL,
    @I_DOMICILIO NVARCHAR(300) = NULL,
    @I_LOCALIDAD NVARCHAR(100) = NULL,
    @I_PROVINCIA NVARCHAR(100) = NULL,
    @I_CODIGO_POSTAL NVARCHAR(20) = NULL,
    @I_CONTACTO NVARCHAR(200) = NULL,
    @I_TELEFONO NVARCHAR(50) = NULL,
    @I_CORREO NVARCHAR(254) = NULL,
    @I_TIPO NVARCHAR(30) = NULL,
    @I_TIPO_ASIGNACION NVARCHAR(20) = NULL,
    @I_PRIORIDAD NVARCHAR(20) = NULL,
    @I_FECHA DATE = NULL,
    @I_HORA_DESDE TIME(0) = NULL,
    @I_HORA_HASTA TIME(0) = NULL,
    @I_CHOFER NVARCHAR(200) = NULL,
    @I_PATENTE NVARCHAR(20) = NULL,
    @I_DOCUMENTO NVARCHAR(30) = NULL,
    @I_TIPO_PROPIEDAD NVARCHAR(20) = NULL,
    @I_LICENCIA NVARCHAR(100) = NULL,
    @I_CATEGORIA NVARCHAR(30) = NULL,
    @I_VENCIMIENTO DATE = NULL,
    @I_MARCA NVARCHAR(80) = NULL,
    @I_MODELO NVARCHAR(80) = NULL,
    @I_ANIO SMALLINT = NULL,
    @I_CAPACIDAD DECIMAL(19,4) = NULL,
    @I_POLIZA NVARCHAR(100) = NULL,
    @I_VENCIMIENTO_SEGURO DATE = NULL,
    @I_VENCIMIENTO_REVISION DATE = NULL,
    @I_CODIGO_ESTADO NVARCHAR(30) = NULL,
    @I_INSTRUCCIONES NVARCHAR(1000) = NULL,
    @I_OBSERVACION NVARCHAR(1000) = NULL,
    @I_RESULTADO NVARCHAR(30) = NULL,
    @I_JSON NVARCHAR(MAX) = NULL,
    @I_ROW_VERSION VARBINARY(8) = NULL,
    @O_ID BIGINT OUTPUT,
    @O_FILAS_AFECTADAS INT OUTPUT,
    @O_CODIGO_ERROR BIGINT OUTPUT,
    @O_MENSAJE NVARCHAR(4000) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @O_ID = NULL;
    SET @O_FILAS_AFECTADAS = 0;
    SET @O_CODIGO_ERROR = NULL;
    SET @O_MENSAJE = NULL;

    BEGIN TRY
        IF @I_ACCION NOT IN
        (
            N'DIRECCION_GUARDAR',
            N'SOLICITUD_CREAR',
            N'TRANSPORTISTA_GUARDAR',
            N'VEHICULO_GUARDAR',
            N'RUTA_CREAR',
            N'RUTA_ASIGNAR',
            N'RUTA_TOMAR_OFERTA',
            N'RUTA_OFERTA_ACTUALIZAR',
            N'RUTA_DESPACHAR',
            N'RUTA_PAUSAR',
            N'RUTA_REANUDAR',
            N'PARADA_LLEGADA',
            N'PARADA_INCIDENCIA',
            N'PARADA_CONFIRMAR',
            N'RUTA_CANCELAR'
        )
        BEGIN
            THROW 51000, N'La acción logística no es válida.', 1;
        END;

        BEGIN TRANSACTION;

        -- INICIO: Alta de un domicilio operativo con su horario habitual.
        IF @I_ACCION = N'DIRECCION_GUARDAR'
        BEGIN
            IF NOT EXISTS
            (
                SELECT 1
                FROM [COMERCIAL].[CLIENTES]
                WHERE [ID_CLIENTE] = @I_ID_CLIENTE
                  AND [ID_EMPRESA] = @I_ID_EMPRESA
            )
            BEGIN
                THROW 51000, N'El cliente no existe en la empresa autenticada.', 1;
            END;

            IF NULLIF(TRIM(@I_NOMBRE), N'') IS NULL OR NULLIF(TRIM(@I_DOMICILIO), N'') IS NULL OR
               NULLIF(TRIM(@I_CONTACTO), N'') IS NULL OR NULLIF(TRIM(@I_TELEFONO), N'') IS NULL OR
               NULLIF(TRIM(@I_INSTRUCCIONES), N'') IS NULL OR
               (@I_HORA_DESDE IS NULL AND @I_HORA_HASTA IS NOT NULL) OR (@I_HORA_DESDE IS NOT NULL AND @I_HORA_HASTA IS NULL) OR
               (@I_HORA_DESDE IS NOT NULL AND @I_HORA_DESDE >= @I_HORA_HASTA)
            BEGIN
                THROW 51000, N'Completá nombre, domicilio, contacto, teléfono, instrucciones y un horario válido.', 1;
            END;

            INSERT INTO [LOGISTICA].[CLIENTES_DIRECCIONES]
            (
                [ID_EMPRESA],
                [ID_CLIENTE],
                [CODIGO],
                [NOMBRE],
                [DOMICILIO],
                [LOCALIDAD],
                [PROVINCIA],
                [CODIGO_POSTAL],
                [CONTACTO],
                [TELEFONO],
                [CORREO],
                [HORA_DESDE],
                [HORA_HASTA],
                [INSTRUCCIONES],
                [ID_USUARIO_ALTA]
            )
            VALUES
            (
                @I_ID_EMPRESA,
                @I_ID_CLIENTE,
                N'PENDIENTE',
                TRIM(@I_NOMBRE),
                TRIM(@I_DOMICILIO),
                NULLIF(TRIM(@I_LOCALIDAD), N''),
                NULLIF(TRIM(@I_PROVINCIA), N''),
                NULLIF(TRIM(@I_CODIGO_POSTAL), N''),
                TRIM(@I_CONTACTO),
                TRIM(@I_TELEFONO),
                NULLIF(TRIM(@I_CORREO), N''),
                @I_HORA_DESDE,
                @I_HORA_HASTA,
                TRIM(@I_INSTRUCCIONES),
                @S_ID_USUARIO
            );

            SET @O_ID = SCOPE_IDENTITY();

            UPDATE [LOGISTICA].[CLIENTES_DIRECCIONES]
            SET [CODIGO] = CONCAT(N'DIR-', FORMAT(@O_ID, N'000000'))
            WHERE [ID_DIRECCION] = @O_ID;

            SET @O_FILAS_AFECTADAS = 1;
        END;
        -- FIN: Alta de un domicilio operativo con su horario habitual.

        -- INICIO: Creación de solicitud y registro de los activos cuya custodia cambiará.
        IF @I_ACCION = N'SOLICITUD_CREAR'
        BEGIN
            IF NOT EXISTS
            (
                SELECT 1
                FROM [LOGISTICA].[CLIENTES_DIRECCIONES]
                WHERE [ID_DIRECCION] = @I_ID_DIRECCION
                  AND [ID_CLIENTE] = @I_ID_CLIENTE
                  AND [ID_EMPRESA] = @I_ID_EMPRESA
                  AND [CODIGO_ESTADO] = N'ACTIVO'
            )
            BEGIN
                THROW 51000, N'La dirección no corresponde al cliente o no está activa.', 1;
            END;

            IF @I_TIPO NOT IN
               (N'ENTREGA_PEDIDO', N'RETIRO_RECARGA', N'DEVOLUCION_CLIENTE',
                N'INTERCAMBIO_TEMPORAL', N'TRASLADO_SUCURSAL', N'URGENCIA')
               OR @I_PRIORIDAD NOT IN (N'NORMAL', N'ALTA', N'URGENTE', N'EMERGENCIA')
               OR @I_FECHA IS NULL
               OR NULLIF(TRIM(@I_INSTRUCCIONES), N'') IS NULL
            BEGIN
                THROW 51000, N'Informá tipo, prioridad, fecha e instrucciones válidas.', 1;
            END;

            IF @I_TIPO IN (N'RETIRO_RECARGA', N'INTERCAMBIO_TEMPORAL')
               AND NOT EXISTS
               (
                   SELECT 1
                   FROM [INVENTARIO].[DEPOSITOS]
                   WHERE [ID_DEPOSITO] = @I_ID_DEPOSITO
                     AND [ID_EMPRESA] = @I_ID_EMPRESA
                     AND [CODIGO_ESTADO] = N'ACTIVO'
               )
            BEGIN
                THROW 51000, N'Indicá el depósito activo que recibirá los activos retirados.', 1;
            END;

            IF @I_TIPO IN (N'RETIRO_RECARGA', N'INTERCAMBIO_TEMPORAL', N'DEVOLUCION_CLIENTE')
               AND @I_ID_PEDIDO IS NULL
               AND ISJSON(@I_JSON) <> 1
            BEGIN
                THROW 51000, N'El servicio requiere identificar los tubos o activos bajo custodia.', 1;
            END;

            -- INICIO: El pedido es la fuente autoritativa cuando origina el retiro o la entrega.
            IF @I_ID_PEDIDO IS NOT NULL
            BEGIN
                IF NOT EXISTS
                (
                    SELECT 1
                    FROM [COMERCIAL].[PEDIDOS]
                    WHERE [ID_PEDIDO] = @I_ID_PEDIDO
                      AND [ID_EMPRESA] = @I_ID_EMPRESA
                      AND [ID_CLIENTE] = @I_ID_CLIENTE
                      AND [CODIGO_ESTADO] IN (N'CONFIRMADO', N'VENDIDO')
                )
                BEGIN
                    THROW 51000, N'El pedido no pertenece al cliente o todavía no está confirmado.', 1;
                END;

                IF @I_TIPO NOT IN (N'RETIRO_RECARGA', N'ENTREGA_PEDIDO')
                BEGIN
                    THROW 51000, N'Un pedido solamente puede originar su retiro o su entrega.', 1;
                END;

                IF EXISTS
                (
                    SELECT 1
                    FROM [LOGISTICA].[SOLICITUDES]
                    WHERE [ID_PEDIDO] = @I_ID_PEDIDO
                      AND [TIPO_SERVICIO] = @I_TIPO
                      AND [CODIGO_ESTADO] <> N'CANCELADA'
                )
                BEGIN
                    THROW 51000, N'El pedido ya posee esta operación logística.', 1;
                END;

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM [COMERCIAL].[PEDIDOS_ACTIVOS]
                    WHERE [ID_PEDIDO] = @I_ID_PEDIDO
                      AND [CODIGO_ESTADO] = N'ACTIVO'
                      AND
                      (
                          @I_TIPO = N'RETIRO_RECARGA' AND [MODALIDAD_INGRESO] = N'RETIRO_OXITIGRE'
                          OR @I_TIPO = N'ENTREGA_PEDIDO' AND [MODALIDAD_RETORNO] = N'ENTREGA_OXITIGRE'
                      )
                )
                BEGIN
                    THROW 51000, N'El pedido no posee activos configurados para esta operación.', 1;
                END;
            END;
            -- FIN: El pedido es la fuente autoritativa cuando origina el retiro o la entrega.

            INSERT INTO [LOGISTICA].[SOLICITUDES]
            (
                [ID_EMPRESA],
                [ID_CLIENTE],
                [ID_DIRECCION],
                [ID_PEDIDO],
                [ID_DEPOSITO_DESTINO],
                [CODIGO],
                [TIPO_SERVICIO],
                [PRIORIDAD],
                [FECHA_SOLICITADA],
                [HORA_DESDE],
                [HORA_HASTA],
                [INSTRUCCIONES],
                [OBSERVACION],
                [ID_USUARIO_ALTA]
            )
            VALUES
            (
                @I_ID_EMPRESA,
                @I_ID_CLIENTE,
                @I_ID_DIRECCION,
                @I_ID_PEDIDO,
                @I_ID_DEPOSITO,
                N'PENDIENTE',
                @I_TIPO,
                @I_PRIORIDAD,
                @I_FECHA,
                @I_HORA_DESDE,
                @I_HORA_HASTA,
                TRIM(@I_INSTRUCCIONES),
                NULLIF(TRIM(@I_OBSERVACION), N''),
                @S_ID_USUARIO
            );

            SET @O_ID = SCOPE_IDENTITY();

            UPDATE [LOGISTICA].[SOLICITUDES]
            SET [CODIGO] = CONCAT(N'SOL-', FORMAT(@O_ID, N'000000'))
            WHERE [ID_SOLICITUD] = @O_ID;

            IF @I_ID_PEDIDO IS NOT NULL
            BEGIN
                INSERT INTO [LOGISTICA].[SOLICITUDES_ACTIVOS]
                (
                    [ID_SOLICITUD],
                    [ID_ACTIVO],
                    [PROPIETARIO],
                    [ROL],
                    [NUMERO_SERIE],
                    [PRODUCTO],
                    [CANTIDAD_CONTENIDO],
                    [UNIDAD],
                    [CONDICION],
                    [OBSERVACION],
                    [FECHA_DEVOLUCION_PREVISTA],
                    [ID_USUARIO_ALTA]
                )
                SELECT
                    @O_ID,
                    [ACT].[ID_ACTIVO],
                    CASE WHEN [ACT].[ID_CLIENTE_PROPIETARIO] IS NULL THEN N'OXITIGRE' ELSE N'CLIENTE' END,
                    CASE
                        WHEN @I_TIPO = N'RETIRO_RECARGA' THEN N'RETIRO_CLIENTE'
                        WHEN [VINC].[TIPO_VINCULO] = N'PRESTAMO' THEN N'PRESTAMO_TEMPORAL'
                        WHEN [VINC].[TIPO_VINCULO] = N'VENTA_ACTIVO' THEN N'ENTREGA'
                        WHEN [ACT].[ID_CLIENTE_PROPIETARIO] IS NOT NULL THEN N'DEVOLUCION'
                        ELSE N'PRESTAMO_TEMPORAL'
                    END,
                    [ACT].[NUMERO_SERIE],
                    [PRO].[NOMBRE],
                    [CON].[CANTIDAD],
                    COALESCE([CON].[UNIDAD], [ACT].[UNIDAD_CAPACIDAD]),
                    [ACT].[CONDICION_ACTUAL],
                    [VINC].[OBSERVACION],
                    [VINC].[FECHA_DEVOLUCION_PREVISTA],
                    @S_ID_USUARIO
                FROM [COMERCIAL].[PEDIDOS_ACTIVOS] AS [VINC]
                INNER JOIN [INVENTARIO].[ACTIVOS] AS [ACT]
                    ON [ACT].[ID_ACTIVO] = [VINC].[ID_ACTIVO]
                INNER JOIN [INVENTARIO].[PRODUCTOS] AS [PRO]
                    ON [PRO].[ID_PRODUCTO] = [ACT].[ID_PRODUCTO]
                OUTER APPLY
                (
                    SELECT SUM([ACO].[CANTIDAD_ACTUAL]) AS [CANTIDAD], MAX([UNI].[SIMBOLO]) AS [UNIDAD]
                    FROM [INVENTARIO].[ACTIVOS_CONTENIDOS] AS [ACO]
                    INNER JOIN [INVENTARIO].[PRODUCTOS] AS [PCO]
                        ON [PCO].[ID_PRODUCTO] = [ACO].[ID_PRODUCTO_CONTENIDO]
                    INNER JOIN [CONFIGURACION].[UNIDADES_MEDIDA] AS [UNI]
                        ON [UNI].[ID_UNIDAD_MEDIDA] = [PCO].[ID_UNIDAD_MEDIDA]
                    WHERE [ACO].[ID_ACTIVO] = [ACT].[ID_ACTIVO]
                      AND [ACO].[CODIGO_ESTADO] = N'ACTIVO'
                ) AS [CON]
                WHERE [VINC].[ID_PEDIDO] = @I_ID_PEDIDO
                  AND [VINC].[CODIGO_ESTADO] = N'ACTIVO'
                  AND
                  (
                      @I_TIPO = N'RETIRO_RECARGA' AND [VINC].[MODALIDAD_INGRESO] = N'RETIRO_OXITIGRE'
                      OR @I_TIPO = N'ENTREGA_PEDIDO' AND [VINC].[MODALIDAD_RETORNO] = N'ENTREGA_OXITIGRE'
                  );
            END
            ELSE IF ISJSON(@I_JSON) = 1
            BEGIN
                INSERT INTO [LOGISTICA].[SOLICITUDES_ACTIVOS]
                (
                    [ID_SOLICITUD],
                    [ID_ACTIVO],
                    [PROPIETARIO],
                    [ROL],
                    [NUMERO_SERIE],
                    [PRODUCTO],
                    [CANTIDAD_CONTENIDO],
                    [UNIDAD],
                    [CONDICION],
                    [OBSERVACION],
                    [FECHA_DEVOLUCION_PREVISTA],
                    [ID_USUARIO_ALTA]
                )
                SELECT
                    @O_ID,
                    [J].[ID_ACTIVO],
                    [J].[PROPIETARIO],
                    [J].[ROL],
                    TRIM([J].[NUMERO_SERIE]),
                    [J].[PRODUCTO],
                    [J].[CANTIDAD_CONTENIDO],
                    [J].[UNIDAD],
                    [J].[CONDICION],
                    [J].[OBSERVACION],
                    [J].[FECHA_DEVOLUCION_PREVISTA],
                    @S_ID_USUARIO
                FROM OPENJSON(@I_JSON)
                WITH
                (
                    [ID_ACTIVO] BIGINT '$.AssetId',
                    [PROPIETARIO] NVARCHAR(20) '$.Owner',
                    [ROL] NVARCHAR(30) '$.Role',
                    [NUMERO_SERIE] NVARCHAR(100) '$.SerialNumber',
                    [PRODUCTO] NVARCHAR(200) '$.Product',
                    [CANTIDAD_CONTENIDO] DECIMAL(19,4) '$.ContentQuantity',
                    [UNIDAD] NVARCHAR(20) '$.Unit',
                    [CONDICION] NVARCHAR(30) '$.Condition',
                    [OBSERVACION] NVARCHAR(500) '$.Observation',
                    [FECHA_DEVOLUCION_PREVISTA] DATE '$.ExpectedReturnDate'
                ) AS [J]
                WHERE NULLIF(TRIM([J].[NUMERO_SERIE]), N'') IS NOT NULL;
            END;

            INSERT INTO [LOGISTICA].[EVENTOS]
            (
                [ID_EMPRESA],
                [ID_SOLICITUD],
                [TIPO_EVENTO],
                [ESTADO_NUEVO],
                [OBSERVACION],
                [ID_USUARIO],
                [ID_SESION],
                [CORRELACION]
            )
            VALUES
            (
                @I_ID_EMPRESA,
                @O_ID,
                N'SOLICITUD_CREADA',
                N'PENDIENTE',
                TRIM(@I_INSTRUCCIONES),
                @S_ID_USUARIO,
                @S_ID_SESION,
                NEWID()
            );

            INSERT INTO [LOGISTICA].[NOTIFICACIONES]
            (
                [ID_EMPRESA],
                [ID_SOLICITUD],
                [TIPO_EVENTO],
                [CANAL],
                [DESTINATARIO],
                [MENSAJE]
            )
            SELECT
                @I_ID_EMPRESA,
                @O_ID,
                N'SOLICITUD_CREADA',
                N'SIMULADO',
                COALESCE(NULLIF([CORREO], N''), [TELEFONO]),
                CONCAT
                (
                    N'Registramos la solicitud ',
                    (
                        SELECT [CODIGO]
                        FROM [LOGISTICA].[SOLICITUDES]
                        WHERE [ID_SOLICITUD] = @O_ID
                    ),
                    N' para el ',
                    CONVERT(NVARCHAR(10), @I_FECHA, 103),
                    N'.'
                )
            FROM [LOGISTICA].[CLIENTES_DIRECCIONES]
            WHERE [ID_DIRECCION] = @I_ID_DIRECCION;

            SET @O_FILAS_AFECTADAS = 1;
        END;
        -- FIN: Creación de solicitud y registro de los activos cuya custodia cambiará.

        -- INICIO: Alta o modificación de un transportista vinculado a un usuario autorizado.
        IF @I_ACCION = N'TRANSPORTISTA_GUARDAR'
        BEGIN
            IF @I_ID_USUARIO IS NULL
               OR @I_TIPO_PROPIEDAD NOT IN (N'EMPRESA', N'PARTICULAR')
               OR NULLIF(TRIM(@I_LICENCIA), N'') IS NULL
               OR @I_CODIGO_ESTADO NOT IN (N'ACTIVO', N'INACTIVO')
               OR NOT EXISTS
               (
                   SELECT 1
                   FROM [SEGURIDAD].[USUARIOS] AS [USR]
                   INNER JOIN [SEGURIDAD].[USUARIOS_ROLES] AS [UR]
                       ON [UR].[ID_USUARIO] = [USR].[ID_USUARIO] AND [UR].[CODIGO_ESTADO] = N'ACTIVO'
                   INNER JOIN [SEGURIDAD].[ROLES] AS [ROL]
                       ON [ROL].[ID_ROL] = [UR].[ID_ROL] AND [ROL].[CODIGO] = N'TRANSPORTISTA'
                   WHERE [USR].[ID_USUARIO] = @I_ID_USUARIO
                     AND [USR].[ID_EMPRESA] = @I_ID_EMPRESA
                     AND [USR].[CODIGO_ESTADO] = N'ACTIVO'
               )
            BEGIN
                THROW 51000, N'El usuario debe estar activo, tener rol TRANSPORTISTA y contar con licencia y vínculo válidos.', 1;
            END;

            IF @I_ID IS NULL
            BEGIN
                INSERT INTO [LOGISTICA].[TRANSPORTISTAS]
                (
                    [ID_EMPRESA], [ID_USUARIO], [CODIGO], [TIPO_VINCULO], [DOCUMENTO],
                    [TELEFONO], [LICENCIA], [CATEGORIA_LICENCIA], [VENCIMIENTO_LICENCIA],
                    [OBSERVACION], [CODIGO_ESTADO], [ID_USUARIO_ALTA]
                )
                VALUES
                (
                    @I_ID_EMPRESA, @I_ID_USUARIO, N'PENDIENTE', @I_TIPO_PROPIEDAD,
                    NULLIF(TRIM(@I_DOCUMENTO), N''), NULLIF(TRIM(@I_TELEFONO), N''),
                    TRIM(@I_LICENCIA), NULLIF(TRIM(@I_CATEGORIA), N''), @I_VENCIMIENTO,
                    NULLIF(TRIM(@I_OBSERVACION), N''), @I_CODIGO_ESTADO, @S_ID_USUARIO
                );

                SET @O_ID = SCOPE_IDENTITY();

                UPDATE [LOGISTICA].[TRANSPORTISTAS]
                SET [CODIGO] = CONCAT(N'TRA-', FORMAT(@O_ID, N'000000'))
                WHERE [ID_TRANSPORTISTA] = @O_ID;
            END;
            ELSE
            BEGIN
                UPDATE [LOGISTICA].[TRANSPORTISTAS]
                SET [ID_USUARIO] = @I_ID_USUARIO,
                    [TIPO_VINCULO] = @I_TIPO_PROPIEDAD,
                    [DOCUMENTO] = NULLIF(TRIM(@I_DOCUMENTO), N''),
                    [TELEFONO] = NULLIF(TRIM(@I_TELEFONO), N''),
                    [LICENCIA] = TRIM(@I_LICENCIA),
                    [CATEGORIA_LICENCIA] = NULLIF(TRIM(@I_CATEGORIA), N''),
                    [VENCIMIENTO_LICENCIA] = @I_VENCIMIENTO,
                    [OBSERVACION] = NULLIF(TRIM(@I_OBSERVACION), N''),
                    [CODIGO_ESTADO] = @I_CODIGO_ESTADO,
                    [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(),
                    [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO
                WHERE [ID_TRANSPORTISTA] = @I_ID
                  AND [ID_EMPRESA] = @I_ID_EMPRESA
                  AND [ROW_VERSION] = @I_ROW_VERSION;

                IF @@ROWCOUNT = 0
                    THROW 51000, N'El transportista cambió o no pertenece a la empresa. Actualizá y reintentá.', 1;

                SET @O_ID = @I_ID;
            END;

            SET @O_FILAS_AFECTADAS = 1;
        END;
        -- FIN: Alta o modificación de un transportista vinculado a un usuario autorizado.

        -- INICIO: Alta o modificación de un vehículo y su propietario real.
        IF @I_ACCION = N'VEHICULO_GUARDAR'
        BEGIN
            IF NULLIF(TRIM(@I_PATENTE), N'') IS NULL
               OR @I_TIPO NOT IN (N'CAMION', N'CAMIONETA', N'UTILITARIO', N'AUTO', N'OTRO')
               OR @I_TIPO_PROPIEDAD NOT IN (N'EMPRESA', N'TRANSPORTISTA')
               OR @I_CODIGO_ESTADO NOT IN (N'ACTIVO', N'INACTIVO')
               OR (@I_TIPO_PROPIEDAD = N'EMPRESA' AND @I_ID_TRANSPORTISTA IS NOT NULL)
               OR (@I_TIPO_PROPIEDAD = N'TRANSPORTISTA' AND NOT EXISTS
                   (
                       SELECT 1 FROM [LOGISTICA].[TRANSPORTISTAS]
                       WHERE [ID_TRANSPORTISTA] = @I_ID_TRANSPORTISTA
                         AND [ID_EMPRESA] = @I_ID_EMPRESA
                         AND [CODIGO_ESTADO] = N'ACTIVO'
                   ))
            BEGIN
                THROW 51000, N'Informá patente, tipo, propiedad y propietario válidos.', 1;
            END;

            IF @I_ID IS NULL
            BEGIN
                INSERT INTO [LOGISTICA].[VEHICULOS]
                (
                    [ID_EMPRESA], [ID_TRANSPORTISTA_PROPIETARIO], [CODIGO], [PATENTE],
                    [TIPO_VEHICULO], [TIPO_PROPIEDAD], [MARCA], [MODELO], [ANIO],
                    [CAPACIDAD_CARGA_KG], [POLIZA_SEGURO], [VENCIMIENTO_SEGURO],
                    [VENCIMIENTO_REVISION], [OBSERVACION], [CODIGO_ESTADO], [ID_USUARIO_ALTA]
                )
                VALUES
                (
                    @I_ID_EMPRESA, @I_ID_TRANSPORTISTA, N'PENDIENTE', TRIM(@I_PATENTE),
                    @I_TIPO, @I_TIPO_PROPIEDAD, NULLIF(TRIM(@I_MARCA), N''),
                    NULLIF(TRIM(@I_MODELO), N''), @I_ANIO, @I_CAPACIDAD,
                    NULLIF(TRIM(@I_POLIZA), N''), @I_VENCIMIENTO_SEGURO,
                    @I_VENCIMIENTO_REVISION, NULLIF(TRIM(@I_OBSERVACION), N''),
                    @I_CODIGO_ESTADO, @S_ID_USUARIO
                );

                SET @O_ID = SCOPE_IDENTITY();

                UPDATE [LOGISTICA].[VEHICULOS]
                SET [CODIGO] = CONCAT(N'VEH-', FORMAT(@O_ID, N'000000'))
                WHERE [ID_VEHICULO] = @O_ID;
            END;
            ELSE
            BEGIN
                UPDATE [LOGISTICA].[VEHICULOS]
                SET [ID_TRANSPORTISTA_PROPIETARIO] = @I_ID_TRANSPORTISTA,
                    [PATENTE] = TRIM(@I_PATENTE),
                    [TIPO_VEHICULO] = @I_TIPO,
                    [TIPO_PROPIEDAD] = @I_TIPO_PROPIEDAD,
                    [MARCA] = NULLIF(TRIM(@I_MARCA), N''),
                    [MODELO] = NULLIF(TRIM(@I_MODELO), N''),
                    [ANIO] = @I_ANIO,
                    [CAPACIDAD_CARGA_KG] = @I_CAPACIDAD,
                    [POLIZA_SEGURO] = NULLIF(TRIM(@I_POLIZA), N''),
                    [VENCIMIENTO_SEGURO] = @I_VENCIMIENTO_SEGURO,
                    [VENCIMIENTO_REVISION] = @I_VENCIMIENTO_REVISION,
                    [OBSERVACION] = NULLIF(TRIM(@I_OBSERVACION), N''),
                    [CODIGO_ESTADO] = @I_CODIGO_ESTADO,
                    [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(),
                    [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO
                WHERE [ID_VEHICULO] = @I_ID
                  AND [ID_EMPRESA] = @I_ID_EMPRESA
                  AND [ROW_VERSION] = @I_ROW_VERSION;

                IF @@ROWCOUNT = 0
                    THROW 51000, N'El vehículo cambió o no pertenece a la empresa. Actualizá y reintentá.', 1;

                SET @O_ID = @I_ID;
            END;

            SET @O_FILAS_AFECTADAS = 1;
        END;
        -- FIN: Alta o modificación de un vehículo y su propietario real.

        -- INICIO: Armado manual de una hoja y foto histórica de cada parada.
        IF @I_ACCION = N'RUTA_CREAR'
        BEGIN
            IF @I_FECHA IS NULL
               OR @I_TIPO NOT IN (N'NORMAL', N'URGENTE')
               OR @I_TIPO_ASIGNACION NOT IN (N'DIRECTA', N'OFERTA')
               OR (@I_TIPO_ASIGNACION = N'DIRECTA' AND
                   (@I_ID_TRANSPORTISTA IS NULL OR @I_ID_VEHICULO IS NULL))
               OR (@I_TIPO_ASIGNACION = N'OFERTA' AND
                   (@I_ID_TRANSPORTISTA IS NOT NULL OR @I_ID_VEHICULO IS NOT NULL))
               OR NULLIF(TRIM(@I_OBSERVACION), N'') IS NULL
               OR ISJSON(@I_JSON) <> 1
            BEGIN
                THROW 51000, N'Informá fecha, tipo, modalidad, observación y solicitudes válidas.', 1;
            END;

            DECLARE @V_CHOFER NVARCHAR(200);
            DECLARE @V_PATENTE NVARCHAR(20);

            IF @I_TIPO_ASIGNACION = N'DIRECTA'
            BEGIN
                SELECT @V_CHOFER = CONCAT_WS(N' ', [USR].[NOMBRES], [USR].[APELLIDO])
                FROM [LOGISTICA].[TRANSPORTISTAS] AS [TRA]
                INNER JOIN [SEGURIDAD].[USUARIOS] AS [USR]
                    ON [USR].[ID_USUARIO] = [TRA].[ID_USUARIO]
                WHERE [TRA].[ID_TRANSPORTISTA] = @I_ID_TRANSPORTISTA
                  AND [TRA].[ID_EMPRESA] = @I_ID_EMPRESA
                  AND [TRA].[CODIGO_ESTADO] = N'ACTIVO'
                  AND ([TRA].[VENCIMIENTO_LICENCIA] IS NULL OR
                       [TRA].[VENCIMIENTO_LICENCIA] >= CAST(SYSUTCDATETIME() AS DATE))
                  AND [USR].[CODIGO_ESTADO] = N'ACTIVO';

                SELECT @V_PATENTE = [PATENTE]
                FROM [LOGISTICA].[VEHICULOS]
                WHERE [ID_VEHICULO] = @I_ID_VEHICULO
                  AND [ID_EMPRESA] = @I_ID_EMPRESA
                  AND [CODIGO_ESTADO] = N'ACTIVO'
                  AND ([ID_TRANSPORTISTA_PROPIETARIO] IS NULL OR
                       [ID_TRANSPORTISTA_PROPIETARIO] = @I_ID_TRANSPORTISTA)
                  AND ([VENCIMIENTO_SEGURO] IS NULL OR
                       [VENCIMIENTO_SEGURO] >= CAST(SYSUTCDATETIME() AS DATE))
                  AND ([VENCIMIENTO_REVISION] IS NULL OR
                       [VENCIMIENTO_REVISION] >= CAST(SYSUTCDATETIME() AS DATE));

                IF @V_CHOFER IS NULL OR @V_PATENTE IS NULL
                    THROW 51000, N'El transportista, su licencia o el vehículo no están habilitados.', 1;

                IF EXISTS
                (
                    SELECT 1
                    FROM [LOGISTICA].[HOJAS_RUTA]
                    WHERE [ID_EMPRESA] = @I_ID_EMPRESA
                      AND [ID_TRANSPORTISTA] = @I_ID_TRANSPORTISTA
                      AND [CODIGO_ESTADO] IN (N'DESPACHADA', N'PAUSADA')
                )
                    THROW 51000, N'El transportista ya posee un recorrido en ejecución.', 1;
            END;

            DECLARE @T_SOLICITUDES TABLE
            (
                [ORDEN] INT NOT NULL,
                [ID_SOLICITUD] BIGINT NOT NULL PRIMARY KEY
            );

            INSERT INTO @T_SOLICITUDES
            SELECT
                [J].[ORDEN],
                [J].[ID_SOLICITUD]
            FROM OPENJSON(@I_JSON)
            WITH
            (
                [ORDEN] INT '$.Order',
                [ID_SOLICITUD] BIGINT '$.RequestId'
            ) AS [J];

            IF NOT EXISTS (SELECT 1 FROM @T_SOLICITUDES)
               OR
               (
                   @I_TIPO = N'URGENTE'
                   AND (SELECT COUNT(*) FROM @T_SOLICITUDES) <> 1
               )
               OR EXISTS
               (
                   SELECT 1
                   FROM @T_SOLICITUDES AS [T]
                   LEFT JOIN [LOGISTICA].[SOLICITUDES] AS [S]
                       ON [S].[ID_SOLICITUD] = [T].[ID_SOLICITUD]
                      AND [S].[ID_EMPRESA] = @I_ID_EMPRESA
                      AND [S].[CODIGO_ESTADO] = N'PENDIENTE'
                   WHERE [S].[ID_SOLICITUD] IS NULL
               )
            BEGIN
                THROW 51000, N'La ruta requiere solicitudes pendientes válidas; una ruta urgente admite una sola.', 1;
            END;

            INSERT INTO [LOGISTICA].[HOJAS_RUTA]
            (
                [ID_EMPRESA],
                [ID_TRANSPORTISTA],
                [ID_VEHICULO],
                [CODIGO],
                [FECHA_RUTA],
                [TIPO_RUTA],
                [TIPO_ASIGNACION],
                [CHOFER],
                [PATENTE],
                [OBSERVACION],
                [FECHA_ASIGNACION_UTC],
                [CODIGO_ESTADO],
                [ID_USUARIO_ALTA]
            )
            VALUES
            (
                @I_ID_EMPRESA,
                @I_ID_TRANSPORTISTA,
                @I_ID_VEHICULO,
                N'PENDIENTE',
                @I_FECHA,
                @I_TIPO,
                @I_TIPO_ASIGNACION,
                @V_CHOFER,
                @V_PATENTE,
                TRIM(@I_OBSERVACION),
                CASE WHEN @I_TIPO_ASIGNACION = N'DIRECTA' THEN SYSUTCDATETIME() END,
                CASE WHEN @I_TIPO_ASIGNACION = N'DIRECTA' THEN N'PLANIFICADA' ELSE N'OFRECIDA' END,
                @S_ID_USUARIO
            );

            SET @O_ID = SCOPE_IDENTITY();

            UPDATE [LOGISTICA].[HOJAS_RUTA]
            SET [CODIGO] = CONCAT(N'RUT-', FORMAT(@O_ID, N'000000'))
            WHERE [ID_HOJA_RUTA] = @O_ID;

            INSERT INTO [LOGISTICA].[HOJAS_RUTA_PARADAS]
            (
                [ID_HOJA_RUTA],
                [ID_SOLICITUD],
                [ORDEN],
                [CLIENTE],
                [DOMICILIO],
                [CONTACTO],
                [TELEFONO],
                [HORA_DESDE],
                [HORA_HASTA],
                [PRIORIDAD],
                [INSTRUCCIONES],
                [ID_USUARIO_ALTA]
            )
            SELECT
                @O_ID,
                [SOL].[ID_SOLICITUD],
                [T].[ORDEN],
                CONCAT_WS(N' ', [CLI].[NOMBRE_RAZON_SOCIAL], [CLI].[APELLIDO]),
                [DIR].[DOMICILIO],
                [DIR].[CONTACTO],
                [DIR].[TELEFONO],
                COALESCE([SOL].[HORA_DESDE], [DIR].[HORA_DESDE]),
                COALESCE([SOL].[HORA_HASTA], [DIR].[HORA_HASTA]),
                [SOL].[PRIORIDAD],
                [SOL].[INSTRUCCIONES],
                @S_ID_USUARIO
            FROM @T_SOLICITUDES AS [T]
            INNER JOIN [LOGISTICA].[SOLICITUDES] AS [SOL]
                ON [SOL].[ID_SOLICITUD] = [T].[ID_SOLICITUD]
            INNER JOIN [LOGISTICA].[CLIENTES_DIRECCIONES] AS [DIR]
                ON [DIR].[ID_DIRECCION] = [SOL].[ID_DIRECCION]
            INNER JOIN [COMERCIAL].[CLIENTES] AS [CLI]
                ON [CLI].[ID_CLIENTE] = [SOL].[ID_CLIENTE];

            UPDATE [SOL]
            SET [CODIGO_ESTADO] = N'PLANIFICADA',
                [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(),
                [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO
            FROM [LOGISTICA].[SOLICITUDES] AS [SOL]
            INNER JOIN @T_SOLICITUDES AS [T]
                ON [T].[ID_SOLICITUD] = [SOL].[ID_SOLICITUD];

            INSERT INTO [LOGISTICA].[EVENTOS]
            (
                [ID_EMPRESA],
                [ID_HOJA_RUTA],
                [TIPO_EVENTO],
                [ESTADO_NUEVO],
                [OBSERVACION],
                [ID_USUARIO],
                [ID_SESION],
                [CORRELACION]
            )
            VALUES
            (
                @I_ID_EMPRESA,
                @O_ID,
                N'RUTA_CREADA',
                CASE WHEN @I_TIPO_ASIGNACION = N'DIRECTA' THEN N'PLANIFICADA' ELSE N'OFRECIDA' END,
                TRIM(@I_OBSERVACION),
                @S_ID_USUARIO,
                @S_ID_SESION,
                NEWID()
            );

            SET @O_FILAS_AFECTADAS = (SELECT COUNT(*) FROM @T_SOLICITUDES);
        END;
        -- FIN: Armado manual de una hoja y foto histórica de cada parada.

        -- INICIO: Edición segura de la agenda mientras la oferta continúa libre.
        IF @I_ACCION = N'RUTA_OFERTA_ACTUALIZAR'
        BEGIN
            IF @I_FECHA IS NULL
               OR @I_TIPO NOT IN (N'NORMAL', N'URGENTE')
               OR NULLIF(TRIM(@I_OBSERVACION), N'') IS NULL
               OR @I_ROW_VERSION IS NULL
            BEGIN
                THROW 51000, N'Informá fecha, tipo, observación y versión de la oferta.', 1;
            END;

            IF @I_TIPO = N'URGENTE'
               AND
               (
                   SELECT COUNT(*)
                   FROM [LOGISTICA].[HOJAS_RUTA_PARADAS]
                   WHERE [ID_HOJA_RUTA] = @I_ID
               ) <> 1
            BEGIN
                THROW 51000, N'Una hoja urgente admite una sola parada.', 1;
            END;

            UPDATE [LOGISTICA].[HOJAS_RUTA]
            SET [FECHA_RUTA] = @I_FECHA,
                [TIPO_RUTA] = @I_TIPO,
                [OBSERVACION] = TRIM(@I_OBSERVACION),
                [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(),
                [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO
            WHERE [ID_HOJA_RUTA] = @I_ID
              AND [ID_EMPRESA] = @I_ID_EMPRESA
              AND [CODIGO_ESTADO] = N'OFRECIDA'
              AND [ID_TRANSPORTISTA] IS NULL
              AND [ROW_VERSION] = @I_ROW_VERSION;

            IF @@ROWCOUNT = 0
                THROW 51000, N'La oferta cambió, fue tomada o ya no está disponible.', 1;

            INSERT INTO [LOGISTICA].[EVENTOS]
            (
                [ID_EMPRESA], [ID_HOJA_RUTA], [TIPO_EVENTO], [ESTADO_ANTERIOR],
                [ESTADO_NUEVO], [OBSERVACION], [ID_USUARIO], [ID_SESION], [CORRELACION]
            )
            VALUES
            (
                @I_ID_EMPRESA, @I_ID, N'RUTA_OFERTA_ACTUALIZADA', N'OFRECIDA',
                N'OFRECIDA', TRIM(@I_OBSERVACION), @S_ID_USUARIO, @S_ID_SESION, NEWID()
            );

            SET @O_ID = @I_ID;
            SET @O_FILAS_AFECTADAS = 1;
        END;
        -- FIN: Edición segura de la agenda mientras la oferta continúa libre.

        -- INICIO: Asignación directa o autoselección atómica de una oferta.
        IF @I_ACCION IN (N'RUTA_ASIGNAR', N'RUTA_TOMAR_OFERTA')
        BEGIN
            IF @I_ID IS NULL OR @I_ID_VEHICULO IS NULL OR @I_ROW_VERSION IS NULL
                THROW 51000, N'Informá hoja, vehículo y versión de la oferta.', 1;

            DECLARE @V_ASIGNAR_TRANSPORTISTA BIGINT = @I_ID_TRANSPORTISTA;
            DECLARE @V_ASIGNAR_CHOFER NVARCHAR(200);
            DECLARE @V_ASIGNAR_PATENTE NVARCHAR(20);

            IF @I_ACCION = N'RUTA_TOMAR_OFERTA'
            BEGIN
                SELECT @V_ASIGNAR_TRANSPORTISTA = [TRA].[ID_TRANSPORTISTA]
                FROM [LOGISTICA].[TRANSPORTISTAS] AS [TRA]
                WHERE [TRA].[ID_EMPRESA] = @I_ID_EMPRESA
                  AND [TRA].[ID_USUARIO] = @S_ID_USUARIO
                  AND [TRA].[CODIGO_ESTADO] = N'ACTIVO';
            END;

            SELECT @V_ASIGNAR_CHOFER = CONCAT_WS(N' ', [USR].[NOMBRES], [USR].[APELLIDO])
            FROM [LOGISTICA].[TRANSPORTISTAS] AS [TRA]
            INNER JOIN [SEGURIDAD].[USUARIOS] AS [USR]
                ON [USR].[ID_USUARIO] = [TRA].[ID_USUARIO]
            WHERE [TRA].[ID_TRANSPORTISTA] = @V_ASIGNAR_TRANSPORTISTA
              AND [TRA].[ID_EMPRESA] = @I_ID_EMPRESA
              AND [TRA].[CODIGO_ESTADO] = N'ACTIVO'
              AND ([TRA].[VENCIMIENTO_LICENCIA] IS NULL OR
                   [TRA].[VENCIMIENTO_LICENCIA] >= CAST(SYSUTCDATETIME() AS DATE))
              AND [USR].[CODIGO_ESTADO] = N'ACTIVO';

            SELECT @V_ASIGNAR_PATENTE = [PATENTE]
            FROM [LOGISTICA].[VEHICULOS]
            WHERE [ID_VEHICULO] = @I_ID_VEHICULO
              AND [ID_EMPRESA] = @I_ID_EMPRESA
              AND [CODIGO_ESTADO] = N'ACTIVO'
              AND ([ID_TRANSPORTISTA_PROPIETARIO] IS NULL OR
                   [ID_TRANSPORTISTA_PROPIETARIO] = @V_ASIGNAR_TRANSPORTISTA)
              AND ([VENCIMIENTO_SEGURO] IS NULL OR
                   [VENCIMIENTO_SEGURO] >= CAST(SYSUTCDATETIME() AS DATE))
              AND ([VENCIMIENTO_REVISION] IS NULL OR
                   [VENCIMIENTO_REVISION] >= CAST(SYSUTCDATETIME() AS DATE));

            IF @V_ASIGNAR_CHOFER IS NULL OR @V_ASIGNAR_PATENTE IS NULL
                THROW 51000, N'El transportista, su licencia o el vehículo no están habilitados.', 1;

            IF EXISTS
            (
                SELECT 1
                FROM [LOGISTICA].[HOJAS_RUTA]
                WHERE [ID_EMPRESA] = @I_ID_EMPRESA
                  AND [ID_TRANSPORTISTA] = @V_ASIGNAR_TRANSPORTISTA
                  AND [CODIGO_ESTADO] IN (N'DESPACHADA', N'PAUSADA')
            )
                THROW 51000, N'El transportista ya posee un recorrido en ejecución.', 1;

            UPDATE [LOGISTICA].[HOJAS_RUTA]
            SET [ID_TRANSPORTISTA] = @V_ASIGNAR_TRANSPORTISTA,
                [ID_VEHICULO] = @I_ID_VEHICULO,
                [TIPO_ASIGNACION] = CASE
                    WHEN @I_ACCION = N'RUTA_TOMAR_OFERTA' THEN N'OFERTA'
                    ELSE N'DIRECTA'
                END,
                [CHOFER] = @V_ASIGNAR_CHOFER,
                [PATENTE] = @V_ASIGNAR_PATENTE,
                [FECHA_ASIGNACION_UTC] = SYSUTCDATETIME(),
                [CODIGO_ESTADO] = N'PLANIFICADA',
                [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(),
                [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO
            WHERE [ID_HOJA_RUTA] = @I_ID
              AND [ID_EMPRESA] = @I_ID_EMPRESA
              AND [ID_TRANSPORTISTA] IS NULL
              AND [CODIGO_ESTADO] = N'OFRECIDA'
              AND [ROW_VERSION] = @I_ROW_VERSION;

            IF @@ROWCOUNT = 0
                THROW 51000, N'La oferta ya fue tomada o cambió. Actualizá la lista.', 1;

            INSERT INTO [LOGISTICA].[EVENTOS]
            (
                [ID_EMPRESA], [ID_HOJA_RUTA], [TIPO_EVENTO], [ESTADO_ANTERIOR],
                [ESTADO_NUEVO], [OBSERVACION], [ID_USUARIO], [ID_SESION], [CORRELACION]
            )
            VALUES
            (
                @I_ID_EMPRESA, @I_ID, @I_ACCION, N'OFRECIDA', N'PLANIFICADA',
                CONCAT(N'Asignada a ', @V_ASIGNAR_CHOFER, N' con ', @V_ASIGNAR_PATENTE, N'.'),
                @S_ID_USUARIO, @S_ID_SESION, NEWID()
            );

            SET @O_ID = @I_ID;
            SET @O_FILAS_AFECTADAS = 1;
        END;
        -- FIN: Asignación directa o autoselección atómica de una oferta.

        -- INICIO: Pausa y reanudación sin alterar solicitudes, paradas ni carga.
        IF @I_ACCION IN (N'RUTA_PAUSAR', N'RUTA_REANUDAR')
        BEGIN
            IF @I_ID IS NULL OR @I_ROW_VERSION IS NULL OR
               NULLIF(TRIM(@I_OBSERVACION), N'') IS NULL
            BEGIN
                THROW 51000, N'Informá hoja, versión y motivo de la transición.', 1;
            END;

            DECLARE @V_ESTADO_PAUSA NVARCHAR(30);
            DECLARE @V_ESTADO_ESPERADO_PAUSA NVARCHAR(30) =
                CASE WHEN @I_ACCION = N'RUTA_PAUSAR' THEN N'DESPACHADA' ELSE N'PAUSADA' END;
            DECLARE @V_ESTADO_NUEVO_PAUSA NVARCHAR(30) =
                CASE WHEN @I_ACCION = N'RUTA_PAUSAR' THEN N'PAUSADA' ELSE N'DESPACHADA' END;

            SELECT @V_ESTADO_PAUSA = [CODIGO_ESTADO]
            FROM [LOGISTICA].[HOJAS_RUTA] WITH (UPDLOCK, HOLDLOCK)
            WHERE [ID_HOJA_RUTA] = @I_ID
              AND [ID_EMPRESA] = @I_ID_EMPRESA
              AND [ROW_VERSION] = @I_ROW_VERSION;

            IF @V_ESTADO_PAUSA IS NULL OR @V_ESTADO_PAUSA <> @V_ESTADO_ESPERADO_PAUSA
                THROW 51000, N'La hoja cambió o no admite esta transición.', 1;

            UPDATE [LOGISTICA].[HOJAS_RUTA]
            SET [CODIGO_ESTADO] = @V_ESTADO_NUEVO_PAUSA,
                [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(),
                [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO
            WHERE [ID_HOJA_RUTA] = @I_ID
              AND [ROW_VERSION] = @I_ROW_VERSION;

            IF @@ROWCOUNT = 0
                THROW 51000, N'La hoja cambió mientras se procesaba la transición.', 1;

            INSERT INTO [LOGISTICA].[EVENTOS]
            (
                [ID_EMPRESA], [ID_HOJA_RUTA], [TIPO_EVENTO], [ESTADO_ANTERIOR],
                [ESTADO_NUEVO], [OBSERVACION], [ID_USUARIO], [ID_SESION], [CORRELACION]
            )
            VALUES
            (
                @I_ID_EMPRESA, @I_ID, @I_ACCION, @V_ESTADO_PAUSA,
                @V_ESTADO_NUEVO_PAUSA, TRIM(@I_OBSERVACION), @S_ID_USUARIO,
                @S_ID_SESION, NEWID()
            );

            SET @O_ID = @I_ID;
            SET @O_FILAS_AFECTADAS = 1;
        END;
        -- FIN: Pausa y reanudación sin alterar solicitudes, paradas ni carga.

        -- INICIO: Despacho, confirmación de parada o cancelación controlada.
        IF @I_ACCION IN (N'RUTA_DESPACHAR', N'RUTA_CANCELAR')
        BEGIN
            IF @I_ACCION = N'RUTA_CANCELAR'
               AND NULLIF(TRIM(@I_OBSERVACION), N'') IS NULL
            BEGIN
                THROW 51000, N'Indicá el motivo de la cancelación.', 1;
            END;

            DECLARE @V_ESTADO_HOJA NVARCHAR(30);

            SELECT @V_ESTADO_HOJA = [CODIGO_ESTADO]
            FROM [LOGISTICA].[HOJAS_RUTA]
            WHERE [ID_HOJA_RUTA] = @I_ID
              AND [ID_EMPRESA] = @I_ID_EMPRESA
              AND [ROW_VERSION] = @I_ROW_VERSION;

            IF @V_ESTADO_HOJA IS NULL
               OR (@I_ACCION = N'RUTA_DESPACHAR' AND @V_ESTADO_HOJA <> N'PLANIFICADA')
               OR (@I_ACCION = N'RUTA_CANCELAR' AND @V_ESTADO_HOJA NOT IN (N'PLANIFICADA', N'OFRECIDA'))
            BEGIN
                THROW 51000, N'La hoja no existe, cambió o no admite esta operación.', 1;
            END;

            DECLARE @V_NUEVO_ESTADO NVARCHAR(30) =
                CASE
                    WHEN @I_ACCION = N'RUTA_DESPACHAR' THEN N'DESPACHADA'
                    ELSE N'CANCELADA'
                END;

            UPDATE [LOGISTICA].[HOJAS_RUTA]
            SET [CODIGO_ESTADO] = @V_NUEVO_ESTADO,
                [FECHA_SALIDA_UTC] = CASE
                    WHEN @I_ACCION = N'RUTA_DESPACHAR' THEN SYSUTCDATETIME()
                    ELSE [FECHA_SALIDA_UTC]
                END,
                [FECHA_CIERRE_UTC] = CASE
                    WHEN @I_ACCION = N'RUTA_CANCELAR' THEN SYSUTCDATETIME()
                    ELSE NULL
                END,
                [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(),
                [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO
            WHERE [ID_HOJA_RUTA] = @I_ID;

            UPDATE [SOL]
            SET [CODIGO_ESTADO] = CASE
                    WHEN @I_ACCION = N'RUTA_DESPACHAR' THEN N'EN_RUTA'
                    ELSE N'PENDIENTE'
                END,
                [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(),
                [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO
            FROM [LOGISTICA].[SOLICITUDES] AS [SOL]
            INNER JOIN [LOGISTICA].[HOJAS_RUTA_PARADAS] AS [PAR]
                ON [PAR].[ID_SOLICITUD] = [SOL].[ID_SOLICITUD]
            WHERE [PAR].[ID_HOJA_RUTA] = @I_ID;

            IF @I_ACCION = N'RUTA_DESPACHAR'
            BEGIN
                INSERT INTO [LOGISTICA].[NOTIFICACIONES]
                (
                    [ID_EMPRESA],
                    [ID_SOLICITUD],
                    [ID_HOJA_RUTA],
                    [TIPO_EVENTO],
                    [CANAL],
                    [DESTINATARIO],
                    [MENSAJE]
                )
                SELECT
                    @I_ID_EMPRESA,
                    [PAR].[ID_SOLICITUD],
                    @I_ID,
                    N'TRANSPORTISTA_EN_CAMINO',
                    N'SIMULADO',
                    [PAR].[TELEFONO],
                    CONCAT
                    (
                        N'El transportista ',
                        [HOJ].[CHOFER],
                        N' salió hacia ',
                        [PAR].[DOMICILIO],
                        N'.'
                    )
                FROM [LOGISTICA].[HOJAS_RUTA_PARADAS] AS [PAR]
                INNER JOIN [LOGISTICA].[HOJAS_RUTA] AS [HOJ]
                    ON [HOJ].[ID_HOJA_RUTA] = [PAR].[ID_HOJA_RUTA]
                WHERE [PAR].[ID_HOJA_RUTA] = @I_ID;
            END;

            INSERT INTO [LOGISTICA].[EVENTOS]
            (
                [ID_EMPRESA],
                [ID_HOJA_RUTA],
                [TIPO_EVENTO],
                [ESTADO_ANTERIOR],
                [ESTADO_NUEVO],
                [OBSERVACION],
                [ID_USUARIO],
                [ID_SESION],
                [CORRELACION]
            )
            VALUES
            (
                @I_ID_EMPRESA,
                @I_ID,
                @I_ACCION,
                @V_ESTADO_HOJA,
                @V_NUEVO_ESTADO,
                COALESCE(NULLIF(TRIM(@I_OBSERVACION), N''), @I_ACCION),
                @S_ID_USUARIO,
                @S_ID_SESION,
                NEWID()
            );

            SET @O_ID = @I_ID;
            SET @O_FILAS_AFECTADAS = 1;
        END;

        IF @I_ACCION IN (N'PARADA_LLEGADA', N'PARADA_INCIDENCIA')
        BEGIN
            IF @I_ACCION = N'PARADA_INCIDENCIA'
               AND
               (
                   @I_RESULTADO NOT IN
                   (
                       N'DEMORA', N'CLIENTE_AUSENTE', N'ACCESO_IMPEDIDO',
                       N'MERCADERIA', N'VEHICULO', N'OTRO'
                   )
                   OR NULLIF(TRIM(@I_OBSERVACION), N'') IS NULL
               )
            BEGIN
                THROW 51000, N'Indicá un tipo y una observación válida para la incidencia.', 1;
            END;

            DECLARE @V_ID_HOJA_OPERATIVA BIGINT,
                    @V_ID_SOLICITUD_OPERATIVA BIGINT,
                    @V_LLEGADA_ACTUAL DATETIME2(3);

            SELECT
                @V_ID_HOJA_OPERATIVA = [PAR].[ID_HOJA_RUTA],
                @V_ID_SOLICITUD_OPERATIVA = [PAR].[ID_SOLICITUD],
                @V_LLEGADA_ACTUAL = [PAR].[FECHA_LLEGADA_UTC]
            FROM [LOGISTICA].[HOJAS_RUTA_PARADAS] AS [PAR] WITH (UPDLOCK, HOLDLOCK)
            INNER JOIN [LOGISTICA].[HOJAS_RUTA] AS [HOJ]
                ON [HOJ].[ID_HOJA_RUTA] = [PAR].[ID_HOJA_RUTA]
               AND [HOJ].[ID_EMPRESA] = @I_ID_EMPRESA
               AND [HOJ].[CODIGO_ESTADO] = N'DESPACHADA'
            WHERE [PAR].[ID_PARADA] = @I_ID
              AND [PAR].[CODIGO_ESTADO] = N'PLANIFICADA'
              AND [PAR].[ROW_VERSION] = @I_ROW_VERSION;

            IF @V_ID_HOJA_OPERATIVA IS NULL
            BEGIN
                THROW 51000, N'La parada no existe, cambió o su hoja no está despachada.', 1;
            END;

            IF @I_ACCION = N'PARADA_LLEGADA'
            BEGIN
                IF @V_LLEGADA_ACTUAL IS NOT NULL
                BEGIN
                    THROW 51000, N'La llegada a esta parada ya fue registrada.', 1;
                END;

                UPDATE [LOGISTICA].[HOJAS_RUTA_PARADAS]
                SET [FECHA_LLEGADA_UTC] = SYSUTCDATETIME()
                WHERE [ID_PARADA] = @I_ID;
            END;

            INSERT INTO [LOGISTICA].[EVENTOS]
            (
                [ID_EMPRESA], [ID_SOLICITUD], [ID_HOJA_RUTA], [ID_PARADA],
                [TIPO_EVENTO], [ESTADO_ANTERIOR], [ESTADO_NUEVO], [OBSERVACION],
                [ID_USUARIO], [ID_SESION], [CORRELACION]
            )
            VALUES
            (
                @I_ID_EMPRESA, @V_ID_SOLICITUD_OPERATIVA, @V_ID_HOJA_OPERATIVA, @I_ID,
                @I_ACCION, N'EN_RUTA', N'EN_RUTA',
                CASE
                    WHEN @I_ACCION = N'PARADA_LLEGADA' THEN N'Llegada registrada por el transportista.'
                    ELSE CONCAT(@I_RESULTADO, N': ', TRIM(@I_OBSERVACION))
                END,
                @S_ID_USUARIO, @S_ID_SESION, NEWID()
            );

            IF @I_ACCION = N'PARADA_INCIDENCIA'
            BEGIN
                INSERT INTO [LOGISTICA].[NOTIFICACIONES]
                (
                    [ID_EMPRESA], [ID_SOLICITUD], [ID_HOJA_RUTA], [TIPO_EVENTO],
                    [CANAL], [DESTINATARIO], [MENSAJE]
                )
                VALUES
                (
                    @I_ID_EMPRESA, @V_ID_SOLICITUD_OPERATIVA, @V_ID_HOJA_OPERATIVA,
                    N'INCIDENCIA_LOGISTICA', N'SIMULADO', N'OPERACION_INTERNA',
                    CONCAT(N'Incidencia ', @I_RESULTADO, N': ', TRIM(@I_OBSERVACION))
                );
            END;

            SET @O_ID = @I_ID;
            SET @O_FILAS_AFECTADAS = 1;
        END;

        IF @I_ACCION = N'PARADA_CONFIRMAR'
        BEGIN
            IF @I_RESULTADO NOT IN (N'ENTREGADA', N'PARCIAL', N'FALLIDA', N'REPROGRAMADA')
               OR NULLIF(TRIM(@I_OBSERVACION), N'') IS NULL
            BEGIN
                THROW 51000, N'Indicá el resultado y una observación obligatoria.', 1;
            END;

            DECLARE @V_ID_HOJA BIGINT,
                    @V_ID_SOLICITUD BIGINT,
                    @V_ESTADO_PARADA NVARCHAR(30),
                    @V_ID_CLIENTE_SOLICITUD BIGINT,
                    @V_ID_DEPOSITO_SOLICITUD BIGINT;

            SELECT
                @V_ID_HOJA = [ID_HOJA_RUTA],
                @V_ID_SOLICITUD = [ID_SOLICITUD],
                @V_ESTADO_PARADA = [CODIGO_ESTADO]
            FROM [LOGISTICA].[HOJAS_RUTA_PARADAS]
            WHERE [ID_PARADA] = @I_ID
              AND [ROW_VERSION] = @I_ROW_VERSION;

            IF @V_ESTADO_PARADA <> N'PLANIFICADA'
               OR NOT EXISTS
               (
                   SELECT 1
                   FROM [LOGISTICA].[HOJAS_RUTA]
                   WHERE [ID_HOJA_RUTA] = @V_ID_HOJA
                     AND [ID_EMPRESA] = @I_ID_EMPRESA
                     AND [CODIGO_ESTADO] = N'DESPACHADA'
               )
            BEGIN
                THROW 51000, N'La parada no existe, cambió o su hoja no está despachada.', 1;
            END;

            SELECT @V_ID_CLIENTE_SOLICITUD = [ID_CLIENTE],
                   @V_ID_DEPOSITO_SOLICITUD = [ID_DEPOSITO_DESTINO]
            FROM [LOGISTICA].[SOLICITUDES]
            WHERE [ID_SOLICITUD] = @V_ID_SOLICITUD;

            DECLARE @T_ACTIVOS_CONFIRMADOS TABLE
            (
                [ID_SOLICITUD_ACTIVO] BIGINT NOT NULL PRIMARY KEY
            );

            IF @I_RESULTADO IN (N'ENTREGADA', N'PARCIAL')
            BEGIN
                IF @I_RESULTADO = N'ENTREGADA'
                BEGIN
                    INSERT INTO @T_ACTIVOS_CONFIRMADOS ([ID_SOLICITUD_ACTIVO])
                    SELECT [ID_SOLICITUD_ACTIVO]
                    FROM [LOGISTICA].[SOLICITUDES_ACTIVOS]
                    WHERE [ID_SOLICITUD] = @V_ID_SOLICITUD;
                END
                ELSE IF ISJSON(@I_JSON) = 1
                BEGIN
                    INSERT INTO @T_ACTIVOS_CONFIRMADOS ([ID_SOLICITUD_ACTIVO])
                    SELECT DISTINCT [ID_SOLICITUD_ACTIVO]
                    FROM OPENJSON(@I_JSON)
                    WITH ([ID_SOLICITUD_ACTIVO] BIGINT '$');
                END;

                IF @I_RESULTADO = N'PARCIAL'
                   AND EXISTS
                   (
                       SELECT 1
                       FROM [LOGISTICA].[SOLICITUDES_ACTIVOS]
                       WHERE [ID_SOLICITUD] = @V_ID_SOLICITUD
                   )
                   AND NOT EXISTS (SELECT 1 FROM @T_ACTIVOS_CONFIRMADOS)
                BEGIN
                    THROW 51000, N'Indicá qué activos fueron operados en la parada parcial.', 1;
                END;

                IF EXISTS
                (
                    SELECT 1
                    FROM @T_ACTIVOS_CONFIRMADOS AS [SEL]
                    LEFT JOIN [LOGISTICA].[SOLICITUDES_ACTIVOS] AS [ACT]
                        ON [ACT].[ID_SOLICITUD_ACTIVO] = [SEL].[ID_SOLICITUD_ACTIVO]
                       AND [ACT].[ID_SOLICITUD] = @V_ID_SOLICITUD
                    WHERE [ACT].[ID_SOLICITUD_ACTIVO] IS NULL
                )
                BEGIN
                    THROW 51000, N'La selección contiene activos ajenos a la parada.', 1;
                END;

                IF EXISTS
                (
                    SELECT 1
                    FROM @T_ACTIVOS_CONFIRMADOS AS [SEL]
                    INNER JOIN [LOGISTICA].[SOLICITUDES_ACTIVOS] AS [SOL_ACT]
                        ON [SOL_ACT].[ID_SOLICITUD_ACTIVO] = [SEL].[ID_SOLICITUD_ACTIVO]
                    INNER JOIN [INVENTARIO].[ACTIVOS] AS [ACT]
                        ON [ACT].[ID_ACTIVO] = [SOL_ACT].[ID_ACTIVO]
                    LEFT JOIN [INVENTARIO].[PRODUCTOS] AS [PRO]
                        ON [PRO].[ID_PRODUCTO] = [ACT].[ID_PRODUCTO]
                    WHERE [ACT].[ID_EMPRESA] <> @I_ID_EMPRESA
                       OR [SOL_ACT].[ROL] = N'RETIRO_CLIENTE'
                          AND ([ACT].[ID_CLIENTE_PROPIETARIO] IS NULL OR [ACT].[CODIGO_ESTADO] <> N'EN_CLIENTE'
                               OR @V_ID_DEPOSITO_SOLICITUD IS NULL)
                       OR [SOL_ACT].[ROL] = N'DEVOLUCION'
                          AND ([ACT].[ID_CLIENTE_PROPIETARIO] IS NULL OR [ACT].[CODIGO_ESTADO] <> N'DISPONIBLE')
                       OR [SOL_ACT].[ROL] = N'ENTREGA'
                          AND ([ACT].[ID_CLIENTE_PROPIETARIO] IS NOT NULL OR [ACT].[CODIGO_ESTADO] <> N'DISPONIBLE')
                       OR [SOL_ACT].[ROL] = N'PRESTAMO_TEMPORAL'
                          AND ([ACT].[ID_CLIENTE_PROPIETARIO] IS NOT NULL OR [ACT].[CODIGO_ESTADO] <> N'DISPONIBLE'
                               OR [PRO].[ADMITE_PRESTAMO] <> 1 OR [SOL_ACT].[FECHA_DEVOLUCION_PREVISTA] IS NULL)
                )
                BEGIN
                    THROW 51000, N'El estado, propietario, depósito o devolución de un activo no permite confirmar la custodia.', 1;
                END;

                DECLARE @V_ID_PRESTAMO BIGINT = NULL;

                IF EXISTS
                (
                    SELECT 1
                    FROM @T_ACTIVOS_CONFIRMADOS AS [SEL]
                    INNER JOIN [LOGISTICA].[SOLICITUDES_ACTIVOS] AS [ACT]
                        ON [ACT].[ID_SOLICITUD_ACTIVO] = [SEL].[ID_SOLICITUD_ACTIVO]
                    WHERE [ACT].[ROL] = N'PRESTAMO_TEMPORAL'
                      AND [ACT].[ID_ACTIVO] IS NOT NULL
                )
                BEGIN
                    DECLARE @V_CODIGO_PRESTAMO NVARCHAR(30) = CONCAT
                    (
                        N'PRE-',
                        RIGHT(REPLICATE(N'0', 6) + CONVERT(NVARCHAR(20),
                            NEXT VALUE FOR [INVENTARIO].[SEQ_PRESTAMO_CODIGO]), 6)
                    );

                    INSERT INTO [INVENTARIO].[PRESTAMOS]
                    (
                        [ID_EMPRESA], [CODIGO], [TIPO_DESTINO], [ID_CLIENTE_DESTINO],
                        [MODALIDAD_ENTREGA], [FECHA_SALIDA_UTC], [FECHA_DEVOLUCION_PREVISTA],
                        [OBSERVACION], [CODIGO_ESTADO], [ID_USUARIO_ALTA]
                    )
                    SELECT @I_ID_EMPRESA, @V_CODIGO_PRESTAMO, N'CLIENTE', @V_ID_CLIENTE_SOLICITUD,
                           N'ENTREGA_PROPIA', SYSUTCDATETIME(),
                           MAX(CONVERT(DATETIME2(3), [ACT].[FECHA_DEVOLUCION_PREVISTA])),
                           TRIM(@I_OBSERVACION), N'PRESTADO', @S_ID_USUARIO
                    FROM @T_ACTIVOS_CONFIRMADOS AS [SEL]
                    INNER JOIN [LOGISTICA].[SOLICITUDES_ACTIVOS] AS [ACT]
                        ON [ACT].[ID_SOLICITUD_ACTIVO] = [SEL].[ID_SOLICITUD_ACTIVO]
                    WHERE [ACT].[ROL] = N'PRESTAMO_TEMPORAL';

                    SET @V_ID_PRESTAMO = SCOPE_IDENTITY();

                    INSERT INTO [INVENTARIO].[PRESTAMOS_DETALLES]
                    (
                        [ID_PRESTAMO], [ID_ACTIVO], [CANTIDAD_SALIDA], [CONDICION_SALIDA],
                        [CODIGO_ESTADO], [ID_USUARIO_ALTA]
                    )
                    SELECT @V_ID_PRESTAMO, [ACT].[ID_ACTIVO], [ACT].[CANTIDAD_CONTENIDO],
                           [ACT].[CONDICION], N'PRESTADO', @S_ID_USUARIO
                    FROM @T_ACTIVOS_CONFIRMADOS AS [SEL]
                    INNER JOIN [LOGISTICA].[SOLICITUDES_ACTIVOS] AS [ACT]
                        ON [ACT].[ID_SOLICITUD_ACTIVO] = [SEL].[ID_SOLICITUD_ACTIVO]
                    WHERE [ACT].[ROL] = N'PRESTAMO_TEMPORAL'
                      AND [ACT].[ID_ACTIVO] IS NOT NULL;

                    UPDATE [ACT]
                    SET [ID_PRESTAMO] = @V_ID_PRESTAMO
                    FROM [LOGISTICA].[SOLICITUDES_ACTIVOS] AS [ACT]
                    INNER JOIN @T_ACTIVOS_CONFIRMADOS AS [SEL]
                        ON [SEL].[ID_SOLICITUD_ACTIVO] = [ACT].[ID_SOLICITUD_ACTIVO]
                    WHERE [ACT].[ROL] = N'PRESTAMO_TEMPORAL';
                END;

                INSERT INTO [INVENTARIO].[ACTIVOS_EVENTOS]
                (
                    [ID_EMPRESA], [ID_ACTIVO], [TIPO_EVENTO], [FECHA_EVENTO_UTC],
                    [CODIGO_ESTADO_ANTES], [CODIGO_ESTADO_DESPUES],
                    [CONDICION_ANTES], [CONDICION_DESPUES], [CANTIDAD_ANTES], [CANTIDAD_DESPUES],
                    [ID_PRESTAMO], [OBSERVACION], [ID_CORRELACION], [CODIGO_ESTADO], [ID_USUARIO_ALTA]
                )
                SELECT @I_ID_EMPRESA, [ACT].[ID_ACTIVO],
                       CASE [SOL_ACT].[ROL]
                           WHEN N'RETIRO_CLIENTE' THEN N'CLIENTE_INGRESO'
                           WHEN N'DEVOLUCION' THEN N'CLIENTE_ENTREGA'
                           WHEN N'PRESTAMO_TEMPORAL' THEN N'PRESTAMO_SALIDA'
                           ELSE N'VENTA_ENTREGA'
                       END,
                       SYSUTCDATETIME(), [ACT].[CODIGO_ESTADO],
                       CASE [SOL_ACT].[ROL]
                           WHEN N'RETIRO_CLIENTE' THEN N'DISPONIBLE'
                           WHEN N'DEVOLUCION' THEN N'EN_CLIENTE'
                           WHEN N'PRESTAMO_TEMPORAL' THEN N'PRESTADO'
                           ELSE N'VENDIDO'
                       END,
                       [ACT].[CONDICION_ACTUAL], [ACT].[CONDICION_ACTUAL],
                       [SOL_ACT].[CANTIDAD_CONTENIDO], [SOL_ACT].[CANTIDAD_CONTENIDO],
                       CASE WHEN [SOL_ACT].[ROL] = N'PRESTAMO_TEMPORAL' THEN @V_ID_PRESTAMO END,
                       TRIM(@I_OBSERVACION), NEWID(), N'CONFIRMADO', @S_ID_USUARIO
                FROM @T_ACTIVOS_CONFIRMADOS AS [SEL]
                INNER JOIN [LOGISTICA].[SOLICITUDES_ACTIVOS] AS [SOL_ACT]
                    ON [SOL_ACT].[ID_SOLICITUD_ACTIVO] = [SEL].[ID_SOLICITUD_ACTIVO]
                INNER JOIN [INVENTARIO].[ACTIVOS] AS [ACT]
                    ON [ACT].[ID_ACTIVO] = [SOL_ACT].[ID_ACTIVO];

                UPDATE [ACT]
                SET [ID_DEPOSITO] = CASE
                        WHEN [SOL_ACT].[ROL] = N'RETIRO_CLIENTE' THEN @V_ID_DEPOSITO_SOLICITUD
                        ELSE NULL
                    END,
                    [ID_UBICACION] = NULL,
                    [CODIGO_ESTADO] = CASE [SOL_ACT].[ROL]
                        WHEN N'RETIRO_CLIENTE' THEN N'DISPONIBLE'
                        WHEN N'DEVOLUCION' THEN N'EN_CLIENTE'
                        WHEN N'PRESTAMO_TEMPORAL' THEN N'PRESTADO'
                        ELSE N'VENDIDO'
                    END,
                    [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(),
                    [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO
                FROM [INVENTARIO].[ACTIVOS] AS [ACT]
                INNER JOIN [LOGISTICA].[SOLICITUDES_ACTIVOS] AS [SOL_ACT]
                    ON [SOL_ACT].[ID_ACTIVO] = [ACT].[ID_ACTIVO]
                INNER JOIN @T_ACTIVOS_CONFIRMADOS AS [SEL]
                    ON [SEL].[ID_SOLICITUD_ACTIVO] = [SOL_ACT].[ID_SOLICITUD_ACTIVO];
            END;

            UPDATE [LOGISTICA].[HOJAS_RUTA_PARADAS]
            SET [RESULTADO] = @I_RESULTADO,
                [OBSERVACION_RESULTADO] = TRIM(@I_OBSERVACION),
                [CODIGO_ESTADO] = @I_RESULTADO,
                [FECHA_LLEGADA_UTC] = COALESCE([FECHA_LLEGADA_UTC], SYSUTCDATETIME()),
                [FECHA_SALIDA_UTC] = SYSUTCDATETIME()
            WHERE [ID_PARADA] = @I_ID;

            UPDATE [LOGISTICA].[SOLICITUDES]
            SET [CODIGO_ESTADO] = CASE WHEN @I_RESULTADO = N'REPROGRAMADA' THEN N'PENDIENTE' ELSE @I_RESULTADO END,
                [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(),
                [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO
            WHERE [ID_SOLICITUD] = @V_ID_SOLICITUD;

            -- INICIO: Estado de custodia derivado del resultado confirmado por el transportista.
            IF @I_RESULTADO IN (N'ENTREGADA', N'PARCIAL')
            BEGIN
                UPDATE [ACT]
                SET [CODIGO_ESTADO] = CASE [ROL]
                    WHEN N'RETIRO_CLIENTE' THEN N'EN_CUSTODIA'
                    WHEN N'PRESTAMO_TEMPORAL' THEN N'PRESTADO'
                    ELSE N'ENTREGADO'
                END
                FROM [LOGISTICA].[SOLICITUDES_ACTIVOS] AS [ACT]
                INNER JOIN @T_ACTIVOS_CONFIRMADOS AS [SEL]
                    ON [SEL].[ID_SOLICITUD_ACTIVO] = [ACT].[ID_SOLICITUD_ACTIVO];
            END;
            -- FIN: Estado de custodia derivado del resultado confirmado por el transportista.

            INSERT INTO [LOGISTICA].[EVENTOS]
            (
                [ID_EMPRESA],
                [ID_SOLICITUD],
                [ID_HOJA_RUTA],
                [ID_PARADA],
                [TIPO_EVENTO],
                [ESTADO_ANTERIOR],
                [ESTADO_NUEVO],
                [OBSERVACION],
                [ID_USUARIO],
                [ID_SESION],
                [CORRELACION]
            )
            VALUES
            (
                @I_ID_EMPRESA,
                @V_ID_SOLICITUD,
                @V_ID_HOJA,
                @I_ID,
                N'PARADA_CONFIRMADA',
                N'EN_RUTA',
                @I_RESULTADO,
                TRIM(@I_OBSERVACION),
                @S_ID_USUARIO,
                @S_ID_SESION,
                NEWID()
            );

            INSERT INTO [LOGISTICA].[NOTIFICACIONES]
            (
                [ID_EMPRESA],
                [ID_SOLICITUD],
                [ID_HOJA_RUTA],
                [TIPO_EVENTO],
                [CANAL],
                [DESTINATARIO],
                [MENSAJE]
            )
            SELECT
                @I_ID_EMPRESA,
                @V_ID_SOLICITUD,
                @V_ID_HOJA,
                N'ENTREGA_RESULTADO',
                N'SIMULADO',
                [TELEFONO],
                CONCAT(N'Resultado del servicio: ', @I_RESULTADO, N'. ', TRIM(@I_OBSERVACION))
            FROM [LOGISTICA].[HOJAS_RUTA_PARADAS]
            WHERE [ID_PARADA] = @I_ID;

            IF NOT EXISTS
            (
                SELECT 1
                FROM [LOGISTICA].[HOJAS_RUTA_PARADAS]
                WHERE [ID_HOJA_RUTA] = @V_ID_HOJA
                  AND [CODIGO_ESTADO] = N'PLANIFICADA'
            )
            BEGIN
                UPDATE [LOGISTICA].[HOJAS_RUTA]
                SET [CODIGO_ESTADO] = N'COMPLETADA',
                    [FECHA_CIERRE_UTC] = SYSUTCDATETIME(),
                    [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(),
                    [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO
                WHERE [ID_HOJA_RUTA] = @V_ID_HOJA;
            END;

            SET @O_ID = @I_ID;
            SET @O_FILAS_AFECTADAS = 1;
        END;
        -- FIN: Despacho, confirmación de parada o cancelación controlada.

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
        BEGIN
            ROLLBACK TRANSACTION;
        END;

        SET @O_CODIGO_ERROR = CASE
            WHEN ERROR_NUMBER() = 51000 THEN 60001
            ELSE 60004
        END;
        SET @O_MENSAJE = ERROR_MESSAGE();
    END CATCH;
END;
