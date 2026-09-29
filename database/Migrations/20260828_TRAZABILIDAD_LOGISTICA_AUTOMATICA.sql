/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: Trazabilidad automática de custodia logística
Archivo: 20260828_TRAZABILIDAD_LOGISTICA_AUTOMATICA.sql | Versión: 1.0.0 | Fecha: 2026-08-28 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Actualiza custodia, ventas y préstamos al confirmar los activos de una parada.
Precondición: Integración de pedidos, logística y finanzas aplicada.
Validación posterior: depósito de retiro, devolución prevista y préstamo trazable por activo.
Recuperación: restaurar el respaldo anterior; las columnas agregadas son compatibles con solicitudes históricas.
Historial: 1.0.0 | 2026-08-28 | FABRICA | Agustin Omar Cauzi | Implementación inicial.
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

GO
:r "database\06_StoredProcedures\LOGISTICA\Commands\SP_LOGISTICA_COMMAND.sql"
GO
:r "database\06_StoredProcedures\LOGISTICA\Queries\SP_LOGISTICA_GET.sql"
GO

IF COL_LENGTH(N'LOGISTICA.SOLICITUDES', N'ID_DEPOSITO_DESTINO') IS NULL
   OR COL_LENGTH(N'LOGISTICA.SOLICITUDES_ACTIVOS', N'ID_PRESTAMO') IS NULL
BEGIN
    THROW 50000, 'No se completó la trazabilidad automática de Logística.', 1;
END;
