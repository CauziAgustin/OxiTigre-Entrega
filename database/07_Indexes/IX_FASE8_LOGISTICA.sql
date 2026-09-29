/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: Índices de Logística Fase 8
Archivo: IX_FASE8_LOGISTICA.sql | Versión: 1.0.0 | Fecha: 2026-08-26 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Optimiza agenda, rutas históricas, custodia y notificaciones pendientes.
Historial: 1.0.0 | 2026-08-26 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
CREATE INDEX [IX_LOG_DIRECCIONES_CLIENTE]
    ON [LOGISTICA].[CLIENTES_DIRECCIONES]
    ([ID_EMPRESA], [ID_CLIENTE], [CODIGO_ESTADO]);

CREATE INDEX [IX_LOG_SOLICITUDES_AGENDA]
    ON [LOGISTICA].[SOLICITUDES]
    ([ID_EMPRESA], [FECHA_SOLICITADA], [CODIGO_ESTADO], [PRIORIDAD]);

CREATE INDEX [IX_LOG_SOLICITUDES_ACTIVOS_SERIE]
    ON [LOGISTICA].[SOLICITUDES_ACTIVOS]
    ([NUMERO_SERIE], [CODIGO_ESTADO]);

CREATE INDEX [IX_LOG_HOJAS_FECHA]
    ON [LOGISTICA].[HOJAS_RUTA]
    ([ID_EMPRESA], [FECHA_RUTA], [CODIGO_ESTADO]);

CREATE INDEX [IX_LOG_PARADAS_SOLICITUD]
    ON [LOGISTICA].[HOJAS_RUTA_PARADAS]
    ([ID_SOLICITUD], [CODIGO_ESTADO]);

CREATE INDEX [IX_LOG_EVENTOS_ENTIDAD]
    ON [LOGISTICA].[EVENTOS]
    ([ID_EMPRESA], [ID_SOLICITUD], [ID_HOJA_RUTA], [FECHA_UTC] DESC);

CREATE INDEX [IX_LOG_NOTIFICACIONES_PENDIENTES]
    ON [LOGISTICA].[NOTIFICACIONES]
    ([CODIGO_ESTADO], [PROXIMO_INTENTO_UTC])
    INCLUDE ([CANAL], [DESTINATARIO]);
