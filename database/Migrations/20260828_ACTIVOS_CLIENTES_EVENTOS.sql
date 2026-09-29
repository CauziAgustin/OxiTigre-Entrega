/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: Migración de eventos de custodia de clientes
Archivo: 20260828_ACTIVOS_CLIENTES_EVENTOS.sql | Versión: 1.0.0 | Fecha: 2026-08-28 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Admite eventos autónomos de alta, ingreso y entrega sin inventar un documento de origen.
Precondición: INVENTARIO.ACTIVOS_EVENTOS y su restricción de origen instalados.
Validación posterior: la restricción admite cero orígenes únicamente para eventos de custodia del cliente.
Recuperación: restaurar la restricción anterior si no existen eventos CLIENTE_*.
Historial: 1.0.0 | 2026-08-28 | FABRICA | Agustin Omar Cauzi | Ampliación controlada del origen de eventos.
===============================================================================
*/
ALTER TABLE [INVENTARIO].[ACTIVOS_EVENTOS]
    DROP CONSTRAINT [CK_ACTIVOS_EVENTOS_ORIGEN];

ALTER TABLE [INVENTARIO].[ACTIVOS_EVENTOS]
    ADD CONSTRAINT [CK_ACTIVOS_EVENTOS_ORIGEN]
    CHECK
    (
        CONVERT(INT, CASE WHEN [ID_RECEPCION_COMPRA] IS NULL THEN 0 ELSE 1 END)
        + CONVERT(INT, CASE WHEN [ID_MEDICION_ACTIVO] IS NULL THEN 0 ELSE 1 END)
        + CONVERT(INT, CASE WHEN [ID_TRANSFORMACION] IS NULL THEN 0 ELSE 1 END)
        + CONVERT(INT, CASE WHEN [ID_INCIDENTE] IS NULL THEN 0 ELSE 1 END)
        + CONVERT(INT, CASE WHEN [ID_MANTENIMIENTO] IS NULL THEN 0 ELSE 1 END)
        + CONVERT(INT, CASE WHEN [ID_PRESTAMO] IS NULL THEN 0 ELSE 1 END)
        = CASE
            WHEN [TIPO_EVENTO] IN (N'CLIENTE_ALTA', N'CLIENTE_INGRESO', N'CLIENTE_ENTREGA') THEN 0
            ELSE 1
          END
    );
