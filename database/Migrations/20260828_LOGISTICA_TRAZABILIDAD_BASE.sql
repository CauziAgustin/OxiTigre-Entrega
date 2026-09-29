/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: Prerrequisito incremental de trazabilidad logística
Archivo: 20260828_LOGISTICA_TRAZABILIDAD_BASE.sql | Versión: 1.0.0 | Fecha: 2026-08-29 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Agrega los campos requeridos antes de recompilar la operación logística integrada.
Precondición: Fase 8 de Logística instalada.
Validación posterior: depósito receptor, devolución prevista y préstamo disponibles en solicitudes.
Recuperación: restaurar el respaldo anterior; las columnas nuevas aceptan valores nulos.
Historial: 1.0.0 | 2026-08-29 | FABRICA | Agustin Omar Cauzi | Reparación del orden incremental de Logística.
===============================================================================
*/
IF COL_LENGTH(N'LOGISTICA.SOLICITUDES', N'ID_DEPOSITO_DESTINO') IS NULL
BEGIN
    ALTER TABLE [LOGISTICA].[SOLICITUDES]
        ADD [ID_DEPOSITO_DESTINO] BIGINT NULL;
END;

IF COL_LENGTH(N'LOGISTICA.SOLICITUDES_ACTIVOS', N'FECHA_DEVOLUCION_PREVISTA') IS NULL
BEGIN
    ALTER TABLE [LOGISTICA].[SOLICITUDES_ACTIVOS]
        ADD [FECHA_DEVOLUCION_PREVISTA] DATE NULL;
END;

IF COL_LENGTH(N'LOGISTICA.SOLICITUDES_ACTIVOS', N'ID_PRESTAMO') IS NULL
BEGIN
    ALTER TABLE [LOGISTICA].[SOLICITUDES_ACTIVOS]
        ADD [ID_PRESTAMO] BIGINT NULL;
END;

IF COL_LENGTH(N'LOGISTICA.SOLICITUDES', N'ID_DEPOSITO_DESTINO') IS NULL
   OR COL_LENGTH(N'LOGISTICA.SOLICITUDES_ACTIVOS', N'FECHA_DEVOLUCION_PREVISTA') IS NULL
   OR COL_LENGTH(N'LOGISTICA.SOLICITUDES_ACTIVOS', N'ID_PRESTAMO') IS NULL
BEGIN
    THROW 50000, 'No se instaló el prerrequisito de trazabilidad logística.', 1;
END;
