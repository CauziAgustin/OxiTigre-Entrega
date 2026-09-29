/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: Datos de ejemplo de sucursal San Fernando
Archivo: 08_SUCURSAL_SAN_FERNANDO.sql | Versión: 1.0.0 | Fecha: 2026-08-27 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Crea la sucursal, unidad operativa y depósito de San Fernando para pruebas multi-sucursal.
Historial: 1.0.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
DECLARE @V_ID_EMPRESA BIGINT =
(
    SELECT [ID_EMPRESA]
    FROM [CONFIGURACION].[EMPRESAS]
    WHERE [CODIGO] = N'OXITIGRE'
);

DECLARE @V_ID_USUARIO BIGINT =
(
    SELECT [ID_USUARIO]
    FROM [SEGURIDAD].[USUARIOS]
    WHERE [ID_EMPRESA] = @V_ID_EMPRESA
      AND [NOMBRE_USUARIO] = N'AOCAUZI'
);

-- INICIO: Alta idempotente de la sede física de San Fernando.
IF NOT EXISTS
(
    SELECT 1
    FROM [CONFIGURACION].[SUCURSALES]
    WHERE [ID_EMPRESA] = @V_ID_EMPRESA
      AND [CODIGO] = N'SAN_FERNANDO'
)
BEGIN
    INSERT INTO [CONFIGURACION].[SUCURSALES]
        ([ID_EMPRESA], [CODIGO], [NOMBRE], [LOCALIDAD], [PROVINCIA], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
    VALUES
        (@V_ID_EMPRESA, N'SAN_FERNANDO', N'OxiTigre San Fernando', N'San Fernando', N'Buenos Aires',
         N'ACTIVO', @V_ID_USUARIO);
END;
-- FIN: Alta idempotente de la sede física de San Fernando.

DECLARE @V_ID_SUCURSAL BIGINT =
(
    SELECT [ID_SUCURSAL]
    FROM [CONFIGURACION].[SUCURSALES]
    WHERE [ID_EMPRESA] = @V_ID_EMPRESA
      AND [CODIGO] = N'SAN_FERNANDO'
);

-- INICIO: Creación del área operativa general de la nueva sucursal.
IF NOT EXISTS
(
    SELECT 1
    FROM [CONFIGURACION].[UNIDADES_OPERATIVAS]
    WHERE [ID_SUCURSAL] = @V_ID_SUCURSAL
      AND [CODIGO] = N'OPERACION_SF'
)
BEGIN
    INSERT INTO [CONFIGURACION].[UNIDADES_OPERATIVAS]
        ([ID_SUCURSAL], [CODIGO], [NOMBRE], [DESCRIPCION], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
    VALUES
        (@V_ID_SUCURSAL, N'OPERACION_SF', N'Operación San Fernando',
         N'Unidad general de demostración; sus áreas definitivas se parametrizarán con la operación real.',
         N'ACTIVO', @V_ID_USUARIO);
END;
-- FIN: Creación del área operativa general de la nueva sucursal.

-- INICIO: Creación del depósito vinculado a San Fernando.
IF NOT EXISTS
(
    SELECT 1
    FROM [INVENTARIO].[DEPOSITOS]
    WHERE [ID_EMPRESA] = @V_ID_EMPRESA
      AND [CODIGO] = N'SAN_FERNANDO'
)
BEGIN
    INSERT INTO [INVENTARIO].[DEPOSITOS]
        ([ID_EMPRESA], [ID_SUCURSAL], [CODIGO], [NOMBRE], [DESCRIPCION], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
    VALUES
        (@V_ID_EMPRESA, @V_ID_SUCURSAL, N'SAN_FERNANDO', N'Depósito San Fernando',
         N'Depósito de demostración vinculado a la sucursal San Fernando.', N'ACTIVO', @V_ID_USUARIO);
END;
-- FIN: Creación del depósito vinculado a San Fernando.

DECLARE @V_ID_DEPOSITO BIGINT =
(
    SELECT [ID_DEPOSITO]
    FROM [INVENTARIO].[DEPOSITOS]
    WHERE [ID_EMPRESA] = @V_ID_EMPRESA
      AND [CODIGO] = N'SAN_FERNANDO'
);

IF NOT EXISTS
(
    SELECT 1
    FROM [INVENTARIO].[UBICACIONES]
    WHERE [ID_DEPOSITO] = @V_ID_DEPOSITO
      AND [CODIGO] = N'RECEPCION'
)
BEGIN
    INSERT INTO [INVENTARIO].[UBICACIONES]
        ([ID_DEPOSITO], [CODIGO], [NOMBRE], [DESCRIPCION], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
    VALUES
        (@V_ID_DEPOSITO, N'RECEPCION', N'Recepción', N'Ingreso inicial de mercadería.', N'ACTIVO', @V_ID_USUARIO);
END;
