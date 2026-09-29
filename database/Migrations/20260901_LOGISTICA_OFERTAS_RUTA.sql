/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: Ofertas de hojas de ruta
Archivo: 20260901_LOGISTICA_OFERTAS_RUTA.sql | Versión: 1.0.0 | Fecha: 2026-09-01 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Permite publicar, editar y tomar hojas sin duplicar la entidad de ruta.
Precondición: Fase 8 de Logística aplicada.
Validación posterior: existen modalidad, fecha de asignación, estado OFRECIDA y acciones consolidadas.
Recuperación: restaurar la base previa; los nuevos datos forman parte de la trazabilidad histórica.
Historial: 1.0.0 | 2026-09-01 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
IF COL_LENGTH(N'[LOGISTICA].[HOJAS_RUTA]', N'TIPO_ASIGNACION') IS NULL
BEGIN
    ALTER TABLE [LOGISTICA].[HOJAS_RUTA]
        ADD [TIPO_ASIGNACION] NVARCHAR(20) NOT NULL
            CONSTRAINT [DF_LOG_HOJA_TIPO_ASIGNACION] DEFAULT (N'DIRECTA');
END;

IF COL_LENGTH(N'[LOGISTICA].[HOJAS_RUTA]', N'FECHA_ASIGNACION_UTC') IS NULL
BEGIN
    ALTER TABLE [LOGISTICA].[HOJAS_RUTA] ADD [FECHA_ASIGNACION_UTC] DATETIME2(3) NULL;
END;
GO

UPDATE [LOGISTICA].[HOJAS_RUTA]
SET [FECHA_ASIGNACION_UTC] = [FECHA_ALTA_UTC]
WHERE [ID_TRANSPORTISTA] IS NOT NULL
  AND [FECHA_ASIGNACION_UTC] IS NULL;

ALTER TABLE [LOGISTICA].[HOJAS_RUTA] ALTER COLUMN [CHOFER] NVARCHAR(200) NULL;

IF OBJECT_ID(N'[LOGISTICA].[CK_LOG_HOJA_TIPO_ASIGNACION]', N'C') IS NULL
BEGIN
    ALTER TABLE [LOGISTICA].[HOJAS_RUTA]
        ADD CONSTRAINT [CK_LOG_HOJA_TIPO_ASIGNACION]
            CHECK ([TIPO_ASIGNACION] IN (N'DIRECTA', N'OFERTA'));
END;

INSERT INTO [CONFIGURACION].[ESTADOS]
    ([ENTIDAD], [CODIGO_ESTADO], [NOMBRE], [DESCRIPCION], [ES_INICIAL], [ES_FINAL],
     [ORDEN], [ID_USUARIO_ALTA])
SELECT N'HOJAS_RUTA', N'OFRECIDA', N'Disponible',
       N'Hoja publicada para que un transportista habilitado pueda tomarla.',
       1, 0, 0, 1
WHERE NOT EXISTS
(
    SELECT 1
    FROM [CONFIGURACION].[ESTADOS]
    WHERE [ENTIDAD] = N'HOJAS_RUTA'
      AND [CODIGO_ESTADO] = N'OFRECIDA'
);
GO

:r "database\06_StoredProcedures\LOGISTICA\Queries\SP_LOGISTICA_GET.sql"
GO
:r "database\06_StoredProcedures\LOGISTICA\Commands\SP_LOGISTICA_COMMAND.sql"
GO

IF COL_LENGTH(N'[LOGISTICA].[HOJAS_RUTA]', N'TIPO_ASIGNACION') IS NULL
   OR COL_LENGTH(N'[LOGISTICA].[HOJAS_RUTA]', N'FECHA_ASIGNACION_UTC') IS NULL
   OR OBJECT_DEFINITION(OBJECT_ID(N'LOGISTICA.SP_LOGISTICA_COMMAND')) NOT LIKE N'%RUTA_TOMAR_OFERTA%'
BEGIN
    THROW 50000, 'No se instaló el circuito de ofertas de hojas de ruta.', 1;
END;
