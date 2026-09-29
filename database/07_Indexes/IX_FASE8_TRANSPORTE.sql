/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: Índices de transporte de Logística Fase 8
Archivo: IX_FASE8_TRANSPORTE.sql | Versión: 1.0.0 | Fecha: 2026-08-27 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Optimiza selección de transportistas y vehículos activos por empresa.
Historial: 1.0.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Creación inicial idempotente.
===============================================================================
*/
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_LOG_TRANSPORTISTAS_ACTIVOS')
BEGIN
    CREATE INDEX [IX_LOG_TRANSPORTISTAS_ACTIVOS]
        ON [LOGISTICA].[TRANSPORTISTAS] ([ID_EMPRESA], [CODIGO_ESTADO], [ID_USUARIO]);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_LOG_VEHICULOS_ACTIVOS')
BEGIN
    CREATE INDEX [IX_LOG_VEHICULOS_ACTIVOS]
        ON [LOGISTICA].[VEHICULOS] ([ID_EMPRESA], [CODIGO_ESTADO], [TIPO_PROPIEDAD]);
END;
