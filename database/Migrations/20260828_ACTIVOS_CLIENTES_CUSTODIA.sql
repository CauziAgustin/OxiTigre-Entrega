/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: Migración de activos propiedad de clientes
Archivo: 20260828_ACTIVOS_CLIENTES_CUSTODIA.sql | Versión: 1.0.0 | Fecha: 2026-08-28 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Incorpora propietario cliente, alta y movimientos de custodia sin perder activos existentes.
Precondición: Fase 7 instalada y secuencia INVENTARIO.SEQ_ACTIVO_CODIGO disponible.
Validación posterior: columna, relación y procedimientos de activos de clientes disponibles.
Recuperación: restaurar respaldo previo; la columna nueva es nullable y no reinterpreta registros anteriores.
Historial: 1.0.0 | 2026-08-28 | FABRICA | Agustin Omar Cauzi | Implementación inicial.
===============================================================================
*/
IF COL_LENGTH(N'INVENTARIO.ACTIVOS', N'ID_CLIENTE_PROPIETARIO') IS NULL
BEGIN
    ALTER TABLE [INVENTARIO].[ACTIVOS]
        ADD [ID_CLIENTE_PROPIETARIO] BIGINT NULL;
END;

GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE [name] = N'FK_ACTIVOS_CLIENTES_PROPIETARIOS'
)
BEGIN
    ALTER TABLE [INVENTARIO].[ACTIVOS]
        ADD CONSTRAINT [FK_ACTIVOS_CLIENTES_PROPIETARIOS]
        FOREIGN KEY ([ID_CLIENTE_PROPIETARIO])
        REFERENCES [COMERCIAL].[CLIENTES] ([ID_CLIENTE]);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [object_id] = OBJECT_ID(N'INVENTARIO.ACTIVOS')
      AND [name] = N'IX_ACTIVOS_CLIENTE_PROPIETARIO'
)
BEGIN
    CREATE INDEX [IX_ACTIVOS_CLIENTE_PROPIETARIO]
        ON [INVENTARIO].[ACTIVOS] ([ID_EMPRESA], [ID_CLIENTE_PROPIETARIO], [CODIGO_ESTADO])
        WHERE [ID_CLIENTE_PROPIETARIO] IS NOT NULL;
END;

IF NOT EXISTS (SELECT 1 FROM [AUDITORIA].[CATALOGO_ERRORES] WHERE [CODIGO_ERROR] = 30010)
BEGIN
    DECLARE @V_ID_MODULO_INVENTARIO BIGINT =
    (
        SELECT [ID_MODULO]
        FROM [CONFIGURACION].[MODULOS]
        WHERE [CODIGO] = N'INVENTARIO'
    );

    INSERT INTO [AUDITORIA].[CATALOGO_ERRORES]
    (
        [ID_MODULO], [NUMERO_ERROR], [CODIGO_ERROR], [NOMBRE], [DESCRIPCION],
        [CAUSA_PROBABLE], [ACCION_RECOMENDADA], [SEVERIDAD], [CODIGO_ESTADO], [ID_USUARIO_ALTA]
    )
    VALUES
    (
        @V_ID_MODULO_INVENTARIO, 10, 30010, N'ACTIVO DE CLIENTE INVÁLIDO',
        N'El activo del cliente o su movimiento de custodia no puede confirmarse.',
        N'Cliente, activo, depósito, estado o versión incompatible.',
        N'Actualizar la pantalla y revisar propietario, ubicación y observación.',
        N'ADVERTENCIA', N'ACTIVO', 1
    );
END;

GO
:r "database\06_StoredProcedures\INVENTARIO\Commands\SP_ACTIVO_CLIENTE_SAVE.sql"
GO
:r "database\06_StoredProcedures\INVENTARIO\Commands\SP_ACTIVO_CLIENTE_CUSTODIA.sql"
GO
:r "database\06_StoredProcedures\INVENTARIO\Queries\SP_TRAZABILIDAD_GET.sql"
GO

IF COL_LENGTH(N'INVENTARIO.ACTIVOS', N'ID_CLIENTE_PROPIETARIO') IS NULL
   OR OBJECT_ID(N'INVENTARIO.SP_ACTIVO_CLIENTE_SAVE', N'P') IS NULL
   OR OBJECT_ID(N'INVENTARIO.SP_ACTIVO_CLIENTE_CUSTODIA', N'P') IS NULL
BEGIN
    THROW 50000, 'No se completó la migración de activos propiedad de clientes.', 1;
END;
