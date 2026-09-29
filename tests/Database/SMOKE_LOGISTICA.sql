/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: Smoke test de Logística Fase 8
Archivo: SMOKE_LOGISTICA.sql | Versión: 1.1.0 | Fecha: 2026-08-27 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Comprueba objetos, datos de prueba, historial y bandeja transaccional de Logística.
Historial: 1.0.0 | 2026-08-26 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Transportistas, vehículos y vínculo histórico.
===============================================================================
*/
SET NOCOUNT ON;

IF OBJECT_ID(N'LOGISTICA.SOLICITUDES', N'U') IS NULL OR
   OBJECT_ID(N'LOGISTICA.HOJAS_RUTA', N'U') IS NULL OR
   OBJECT_ID(N'LOGISTICA.EVENTOS', N'U') IS NULL OR
   OBJECT_ID(N'LOGISTICA.NOTIFICACIONES', N'U') IS NULL OR
   OBJECT_ID(N'LOGISTICA.TRANSPORTISTAS', N'U') IS NULL OR
   OBJECT_ID(N'LOGISTICA.VEHICULOS', N'U') IS NULL
BEGIN
    THROW 51000, N'Faltan objetos de Logística Fase 8.', 1;
END;

IF COL_LENGTH(N'LOGISTICA.HOJAS_RUTA', N'ID_TRANSPORTISTA') IS NULL OR
   COL_LENGTH(N'LOGISTICA.HOJAS_RUTA', N'ID_VEHICULO') IS NULL
BEGIN
    THROW 51000, N'Falta la asignación histórica de transporte en hojas de ruta.', 1;
END;

IF NOT EXISTS
(
    SELECT 1 FROM [SEGURIDAD].[ROLES] WHERE [CODIGO] = N'TRANSPORTISTA' AND [CODIGO_ESTADO] = N'ACTIVO'
)
BEGIN
    THROW 51000, N'Falta el rol TRANSPORTISTA.', 1;
END;

IF EXISTS
(
    SELECT 1 FROM [LOGISTICA].[VEHICULOS]
    WHERE [PATENTE] LIKE N'%-%' OR [PATENTE] LIKE N'% %'
)
BEGIN
    THROW 51000, N'Las patentes deben persistirse sin separadores.', 1;
END;

IF OBJECT_ID(N'LOGISTICA.SP_LOGISTICA_GET', N'P') IS NULL OR
   OBJECT_ID(N'LOGISTICA.SP_LOGISTICA_COMMAND', N'P') IS NULL
BEGIN
    THROW 51000, N'Faltan procedimientos de Logística Fase 8.', 1;
END;

IF NOT EXISTS (SELECT 1 FROM [AUDITORIA].[CATALOGO_ERRORES] WHERE [CODIGO_ERROR] BETWEEN 60001 AND 60004)
BEGIN
    THROW 51000, N'Faltan errores controlados de Logística.', 1;
END;

SELECT N'OK - Logística Fase 8 instalada' AS [RESULTADO];
