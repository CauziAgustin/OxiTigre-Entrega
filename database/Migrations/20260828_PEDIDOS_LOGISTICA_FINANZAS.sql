/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: Integración de pedidos, logística y finanzas
Archivo: 20260828_PEDIDOS_LOGISTICA_FINANZAS.sql | Versión: 1.0.0 | Fecha: 2026-08-28 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Deriva retiros y entregas desde pedidos y expone el saldo financiero sin duplicar datos.
Precondición: Migración comercial de servicios y activos, Logística y Finanzas instaladas.
Validación posterior: modalidades de ingreso, devolución prevista, solicitudes derivadas y estado de pago consultable.
Recuperación: restaurar el respaldo anterior; las columnas agregadas preservan todos los vínculos existentes.
Historial: 1.0.0 | 2026-08-28 | FABRICA | Agustin Omar Cauzi | Implementación inicial.
===============================================================================
*/
IF COL_LENGTH(N'COMERCIAL.PEDIDOS_ACTIVOS', N'MODALIDAD_INGRESO') IS NULL
BEGIN
    ALTER TABLE [COMERCIAL].[PEDIDOS_ACTIVOS]
        ADD [MODALIDAD_INGRESO] NVARCHAR(30) NOT NULL
            CONSTRAINT [DF_PEDIDOS_ACTIVOS_MODALIDAD_INGRESO] DEFAULT (N'NO_APLICA');
END;

IF COL_LENGTH(N'COMERCIAL.PEDIDOS_ACTIVOS', N'FECHA_DEVOLUCION_PREVISTA') IS NULL
BEGIN
    ALTER TABLE [COMERCIAL].[PEDIDOS_ACTIVOS]
        ADD [FECHA_DEVOLUCION_PREVISTA] DATE NULL;
END;

GO

IF OBJECT_ID(N'COMERCIAL.CK_PEDIDOS_ACTIVOS_INGRESO', N'C') IS NULL
BEGIN
    ALTER TABLE [COMERCIAL].[PEDIDOS_ACTIVOS]
        ADD CONSTRAINT [CK_PEDIDOS_ACTIVOS_INGRESO]
            CHECK ([MODALIDAD_INGRESO] IN (N'ENTREGA_CLIENTE', N'RETIRO_OXITIGRE', N'NO_APLICA'));
END;

IF OBJECT_ID(N'COMERCIAL.CK_PEDIDOS_ACTIVOS_RETORNO', N'C') IS NULL
BEGIN
    ALTER TABLE [COMERCIAL].[PEDIDOS_ACTIVOS]
        ADD CONSTRAINT [CK_PEDIDOS_ACTIVOS_RETORNO]
            CHECK ([MODALIDAD_RETORNO] IN (N'RETIRO_CLIENTE', N'ENTREGA_OXITIGRE', N'NO_APLICA'));
END;

GO
:r "database\06_StoredProcedures\COMERCIAL\Commands\SP_PEDIDO_SAVE.sql"
GO
:r "database\06_StoredProcedures\COMERCIAL\Queries\SP_VENTAS_GET.sql"
GO
:r "database\06_StoredProcedures\LOGISTICA\Commands\SP_LOGISTICA_COMMAND.sql"
GO
:r "database\06_StoredProcedures\LOGISTICA\Queries\SP_LOGISTICA_GET.sql"
GO

IF COL_LENGTH(N'COMERCIAL.PEDIDOS_ACTIVOS', N'MODALIDAD_INGRESO') IS NULL
   OR COL_LENGTH(N'COMERCIAL.PEDIDOS_ACTIVOS', N'FECHA_DEVOLUCION_PREVISTA') IS NULL
   OR OBJECT_ID(N'LOGISTICA.SP_LOGISTICA_COMMAND', N'P') IS NULL
   OR OBJECT_ID(N'LOGISTICA.SP_LOGISTICA_GET', N'P') IS NULL
BEGIN
    THROW 50000, 'No se completó la integración de pedidos, logística y finanzas.', 1;
END;
