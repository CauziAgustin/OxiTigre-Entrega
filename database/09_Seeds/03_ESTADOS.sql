/*
===============================================================================
Proyecto:              Sistema Modular de Gestión OxiTigre
Componente:            Seed de estados
Archivo:               03_ESTADOS.sql
Versión:               1.4.0
Fecha:                 2026-08-26
ID pedido:             FABRICA
Desarrollador:         Agustin Omar Cauzi
Correo:                agustincauzi10@hotmail.com
Descripción funcional: Documenta estados iniciales admitidos por las entidades del núcleo.
Historial de modificaciones:
1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
1.1.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Preferencias, traducciones y corrección de estados iniciales especiales.
1.2.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Estados homogéneos del módulo Inventario.
1.3.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Estados del ciclo pedido, reserva y venta.
1.4.0 | 2026-08-26 | FABRICA | Agustin Omar Cauzi | Estados del circuito logístico y sus notificaciones.
===============================================================================
*/
WITH [ENTIDADES] AS
(
    SELECT [ENTIDAD]
    FROM (VALUES
        (N'EMPRESAS'), (N'SUCURSALES'), (N'UNIDADES_OPERATIVAS'), (N'MODULOS'),
        (N'PARAMETROS_SISTEMA'), (N'TIPOS_TELEFONO'), (N'TIPOS_DOCUMENTO'), (N'PAISES'),
        (N'USUARIOS_PREFERENCIAS'), (N'TRADUCCIONES_CATALOGO'),
        (N'CLIENTES'), (N'CLIENTES_TELEFONOS'), (N'CLIENTES_BORRADORES'),
        (N'USUARIOS'), (N'ROLES'), (N'PERMISOS'), (N'USUARIOS_ROLES'), (N'ROLES_PERMISOS'),
        (N'DISPOSITIVOS_ACCESO'), (N'CATALOGO_ERRORES'), (N'SOLUCIONES_ERROR'),
        (N'ERRORES_APLICACION'), (N'LOGUEO_FUNCIONALIDADES'), (N'BITACORA_CAMBIOS_ESTADO'),
        (N'UNIDADES_MEDIDA'), (N'CATEGORIAS_PRODUCTO'), (N'PRODUCTOS'), (N'DEPOSITOS'),
        (N'UBICACIONES'), (N'EXISTENCIAS'), (N'MOVIMIENTOS_DETALLES'),
        (N'LISTAS_PRECIOS'), (N'LISTAS_PRECIOS_PRODUCTOS'), (N'PEDIDOS_DETALLES'), (N'VENTAS_DETALLES')
    ) AS [E] ([ENTIDAD])
),
[ESTADOS_BASE] AS
(
    SELECT [CODIGO_ESTADO], [NOMBRE], [DESCRIPCION], [ES_INICIAL], [ES_FINAL], [ORDEN]
    FROM (VALUES
        (N'ACTIVO', N'Activo', N'Registro disponible para su uso funcional.', CONVERT(BIT, 1), CONVERT(BIT, 0), CONVERT(SMALLINT, 0)),
        (N'INACTIVO', N'Inactivo', N'Registro conservado pero no disponible para nuevas operaciones.', CONVERT(BIT, 0), CONVERT(BIT, 1), CONVERT(SMALLINT, 1))
    ) AS [E] ([CODIGO_ESTADO], [NOMBRE], [DESCRIPCION], [ES_INICIAL], [ES_FINAL], [ORDEN])
)
INSERT INTO [CONFIGURACION].[ESTADOS]
    ([ENTIDAD], [CODIGO_ESTADO], [NOMBRE], [DESCRIPCION], [ES_INICIAL], [ES_FINAL], [ORDEN], [ID_USUARIO_ALTA])
SELECT [ENT].[ENTIDAD], [EST].[CODIGO_ESTADO], [EST].[NOMBRE], [EST].[DESCRIPCION],
       [EST].[ES_INICIAL], [EST].[ES_FINAL], [EST].[ORDEN], 1
FROM [ENTIDADES] AS [ENT]
CROSS JOIN [ESTADOS_BASE] AS [EST]
WHERE NOT EXISTS
(
    SELECT 1 FROM [CONFIGURACION].[ESTADOS] AS [ACT]
    WHERE [ACT].[ENTIDAD] = [ENT].[ENTIDAD]
      AND [ACT].[CODIGO_ESTADO] = [EST].[CODIGO_ESTADO]
);

-- INICIO: Estados operativos e históricos de Logística.
INSERT INTO [CONFIGURACION].[ESTADOS]
    ([ENTIDAD], [CODIGO_ESTADO], [NOMBRE], [DESCRIPCION], [ES_INICIAL], [ES_FINAL], [ORDEN], [ID_USUARIO_ALTA])
SELECT [E].[ENTIDAD], [E].[CODIGO_ESTADO], [E].[NOMBRE], [E].[DESCRIPCION], [E].[ES_INICIAL], [E].[ES_FINAL], [E].[ORDEN], 1
FROM (VALUES
    (N'SOLICITUDES_LOGISTICA',N'PENDIENTE',N'Pendiente',N'Solicitud disponible para planificar.',CONVERT(BIT,1),CONVERT(BIT,0),CONVERT(SMALLINT,0)),
    (N'SOLICITUDES_LOGISTICA',N'PLANIFICADA',N'Planificada',N'Solicitud incorporada a una hoja de ruta.',CONVERT(BIT,0),CONVERT(BIT,0),CONVERT(SMALLINT,1)),
    (N'SOLICITUDES_LOGISTICA',N'EN_RUTA',N'En ruta',N'El transportista fue despachado.',CONVERT(BIT,0),CONVERT(BIT,0),CONVERT(SMALLINT,2)),
    (N'SOLICITUDES_LOGISTICA',N'ENTREGADA',N'Entregada',N'Servicio completado.',CONVERT(BIT,0),CONVERT(BIT,1),CONVERT(SMALLINT,3)),
    (N'SOLICITUDES_LOGISTICA',N'PARCIAL',N'Parcial',N'Servicio completado parcialmente.',CONVERT(BIT,0),CONVERT(BIT,1),CONVERT(SMALLINT,4)),
    (N'SOLICITUDES_LOGISTICA',N'FALLIDA',N'Fallida',N'No se pudo completar el servicio.',CONVERT(BIT,0),CONVERT(BIT,1),CONVERT(SMALLINT,5)),
    (N'SOLICITUDES_LOGISTICA',N'REPROGRAMADA',N'Reprogramada',N'La solicitud debe volver a planificarse.',CONVERT(BIT,0),CONVERT(BIT,1),CONVERT(SMALLINT,6)),
    (N'HOJAS_RUTA',N'OFRECIDA',N'Disponible',N'Hoja publicada para que un transportista habilitado pueda tomarla.',CONVERT(BIT,1),CONVERT(BIT,0),CONVERT(SMALLINT,0)),
    (N'HOJAS_RUTA',N'PLANIFICADA',N'Planificada',N'Hoja asignada aún no despachada.',CONVERT(BIT,1),CONVERT(BIT,0),CONVERT(SMALLINT,1)),
    (N'HOJAS_RUTA',N'DESPACHADA',N'Despachada',N'Hoja en ejecución.',CONVERT(BIT,0),CONVERT(BIT,0),CONVERT(SMALLINT,2)),
    (N'HOJAS_RUTA',N'PAUSADA',N'Pausada',N'El recorrido conserva su carga y espera reanudación.',CONVERT(BIT,0),CONVERT(BIT,0),CONVERT(SMALLINT,3)),
    (N'HOJAS_RUTA',N'COMPLETADA',N'Completada',N'Todas las paradas tienen resultado.',CONVERT(BIT,0),CONVERT(BIT,1),CONVERT(SMALLINT,4)),
    (N'HOJAS_RUTA',N'CANCELADA',N'Cancelada',N'Hoja cancelada sin eliminar su historial.',CONVERT(BIT,0),CONVERT(BIT,1),CONVERT(SMALLINT,5)),
    (N'NOTIFICACIONES',N'PENDIENTE',N'Pendiente',N'Aviso esperando un proveedor de envío.',CONVERT(BIT,1),CONVERT(BIT,0),CONVERT(SMALLINT,0)),
    (N'NOTIFICACIONES',N'ENVIADA',N'Enviada',N'Aviso aceptado por el proveedor.',CONVERT(BIT,0),CONVERT(BIT,1),CONVERT(SMALLINT,1)),
    (N'NOTIFICACIONES',N'ERROR',N'Error',N'Aviso agotó los reintentos configurados.',CONVERT(BIT,0),CONVERT(BIT,1),CONVERT(SMALLINT,2))
) AS [E] ([ENTIDAD],[CODIGO_ESTADO],[NOMBRE],[DESCRIPCION],[ES_INICIAL],[ES_FINAL],[ORDEN])
WHERE NOT EXISTS (SELECT 1 FROM [CONFIGURACION].[ESTADOS] AS [A]
                  WHERE [A].[ENTIDAD]=[E].[ENTIDAD] AND [A].[CODIGO_ESTADO]=[E].[CODIGO_ESTADO]);
-- FIN: Estados operativos e históricos de Logística.

UPDATE [CONFIGURACION].[ESTADOS]
SET [ES_INICIAL] = 0, [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(), [ID_USUARIO_MODIFICACION] = 1
WHERE [CODIGO_ESTADO] = N'ACTIVO' AND [ES_INICIAL] = 1
  AND [ENTIDAD] IN (N'CLIENTES_BORRADORES', N'LOGUEO_FUNCIONALIDADES');

INSERT INTO [CONFIGURACION].[ESTADOS]
    ([ENTIDAD], [CODIGO_ESTADO], [NOMBRE], [DESCRIPCION], [ES_INICIAL], [ES_FINAL], [ORDEN], [ID_USUARIO_ALTA])
SELECT [E].[ENTIDAD], [E].[CODIGO_ESTADO], [E].[NOMBRE], [E].[DESCRIPCION], [E].[ES_INICIAL], [E].[ES_FINAL], [E].[ORDEN], 1
FROM (VALUES
    (N'SESIONES', N'VIGENTE', N'Vigente', N'Sesión autenticada utilizable.', CONVERT(BIT, 1), CONVERT(BIT, 0), CONVERT(SMALLINT, 0)),
    (N'SESIONES', N'CERRADA', N'Cerrada', N'Sesión cerrada normalmente.', CONVERT(BIT, 0), CONVERT(BIT, 1), CONVERT(SMALLINT, 1)),
    (N'SESIONES', N'REVOCADA', N'Revocada', N'Sesión invalidada administrativamente.', CONVERT(BIT, 0), CONVERT(BIT, 1), CONVERT(SMALLINT, 2)),
    (N'SESIONES', N'EXPIRADA', N'Expirada', N'Sesión que superó su vigencia.', CONVERT(BIT, 0), CONVERT(BIT, 1), CONVERT(SMALLINT, 3)),
    (N'RECUPERACIONES_CLAVE', N'PENDIENTE', N'Pendiente', N'Recuperación disponible para utilizar.', CONVERT(BIT, 1), CONVERT(BIT, 0), CONVERT(SMALLINT, 0)),
    (N'RECUPERACIONES_CLAVE', N'UTILIZADA', N'Utilizada', N'Recuperación consumida correctamente.', CONVERT(BIT, 0), CONVERT(BIT, 1), CONVERT(SMALLINT, 1)),
    (N'RECUPERACIONES_CLAVE', N'EXPIRADA', N'Expirada', N'Recuperación fuera de vigencia.', CONVERT(BIT, 0), CONVERT(BIT, 1), CONVERT(SMALLINT, 2)),
    (N'CLIENTES_BORRADORES', N'BORRADOR', N'Borrador', N'Precarga incompleta disponible para continuar.', CONVERT(BIT, 1), CONVERT(BIT, 0), CONVERT(SMALLINT, 0)),
    (N'CLIENTES_BORRADORES', N'DESCARTADO', N'Descartado', N'Precarga utilizada o descartada por el usuario.', CONVERT(BIT, 0), CONVERT(BIT, 1), CONVERT(SMALLINT, 1)),
    (N'LOGUEO_FUNCIONALIDADES', N'REGISTRADO', N'Registrado', N'Ejecución funcional almacenada para auditoría.', CONVERT(BIT, 1), CONVERT(BIT, 1), CONVERT(SMALLINT, 0)),
    (N'MOVIMIENTOS', N'CONFIRMADO', N'Confirmado', N'Movimiento aplicado de forma definitiva a las existencias.', CONVERT(BIT, 1), CONVERT(BIT, 1), CONVERT(SMALLINT, 0)),
    (N'PEDIDOS', N'BORRADOR', N'Borrador', N'Pedido editable que todavía no compromete stock.', CONVERT(BIT, 1), CONVERT(BIT, 0), CONVERT(SMALLINT, 0)),
    (N'PEDIDOS', N'CONFIRMADO', N'Confirmado', N'Pedido confirmado con stock reservado.', CONVERT(BIT, 0), CONVERT(BIT, 0), CONVERT(SMALLINT, 1)),
    (N'PEDIDOS', N'CANCELADO', N'Cancelado', N'Pedido cancelado y sin reservas activas.', CONVERT(BIT, 0), CONVERT(BIT, 1), CONVERT(SMALLINT, 2)),
    (N'PEDIDOS', N'VENDIDO', N'Vendido', N'Pedido convertido en venta y descontado del stock físico.', CONVERT(BIT, 0), CONVERT(BIT, 1), CONVERT(SMALLINT, 3)),
    (N'RESERVAS_STOCK', N'RESERVADA', N'Reservada', N'Cantidad comprometida por un pedido confirmado.', CONVERT(BIT, 1), CONVERT(BIT, 0), CONVERT(SMALLINT, 0)),
    (N'RESERVAS_STOCK', N'LIBERADA', N'Liberada', N'Reserva devuelta al disponible por cancelación.', CONVERT(BIT, 0), CONVERT(BIT, 1), CONVERT(SMALLINT, 1)),
    (N'RESERVAS_STOCK', N'APLICADA', N'Aplicada', N'Reserva consumida por la venta correspondiente.', CONVERT(BIT, 0), CONVERT(BIT, 1), CONVERT(SMALLINT, 2)),
    (N'VENTAS', N'CONFIRMADA', N'Confirmada', N'Venta interna registrada y aplicada al inventario.', CONVERT(BIT, 1), CONVERT(BIT, 1), CONVERT(SMALLINT, 0))
) AS [E] ([ENTIDAD], [CODIGO_ESTADO], [NOMBRE], [DESCRIPCION], [ES_INICIAL], [ES_FINAL], [ORDEN])
WHERE NOT EXISTS
(
    SELECT 1 FROM [CONFIGURACION].[ESTADOS] AS [ACT]
    WHERE [ACT].[ENTIDAD] = [E].[ENTIDAD] AND [ACT].[CODIGO_ESTADO] = [E].[CODIGO_ESTADO]
);
