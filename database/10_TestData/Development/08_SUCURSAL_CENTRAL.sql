/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: Datos de ejemplo de sucursal Central
Archivo: 08_SUCURSAL_CENTRAL.sql | Versión: 1.0.0 | Fecha: 2026-09-02 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Formaliza la sede Central y corrige sus relaciones operativas de Development.
Historial: 1.0.0 | 2026-09-02 | FABRICA | Agustin Omar Cauzi | Creación inicial.
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
    SELECT TOP (1) [ID_USUARIO]
    FROM [SEGURIDAD].[USUARIOS]
    WHERE [ID_EMPRESA] = @V_ID_EMPRESA
    ORDER BY [ID_USUARIO]
);

-- INICIO: Alta idempotente de la sede principal.
IF NOT EXISTS
(
    SELECT 1
    FROM [CONFIGURACION].[SUCURSALES]
    WHERE [ID_EMPRESA] = @V_ID_EMPRESA
      AND [CODIGO] = N'CENTRAL'
)
BEGIN
    INSERT INTO [CONFIGURACION].[SUCURSALES]
        ([ID_EMPRESA], [CODIGO], [NOMBRE], [LOCALIDAD], [PROVINCIA],
         [CODIGO_ESTADO], [ID_USUARIO_ALTA])
    VALUES
        (@V_ID_EMPRESA, N'CENTRAL', N'OxiTigre Central', N'Tigre', N'Buenos Aires',
         N'ACTIVO', @V_ID_USUARIO);
END;
-- FIN: Alta idempotente de la sede principal.

DECLARE @V_ID_SUCURSAL BIGINT =
(
    SELECT [ID_SUCURSAL]
    FROM [CONFIGURACION].[SUCURSALES]
    WHERE [ID_EMPRESA] = @V_ID_EMPRESA
      AND [CODIGO] = N'CENTRAL'
);

-- INICIO: Asociación de los registros cuyo nombre ya los identifica como centrales.
UPDATE [INVENTARIO].[DEPOSITOS]
SET [ID_SUCURSAL] = @V_ID_SUCURSAL,
    [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(),
    [ID_USUARIO_MODIFICACION] = @V_ID_USUARIO
WHERE [ID_EMPRESA] = @V_ID_EMPRESA
  AND [CODIGO] = N'CENTRAL'
  AND ([ID_SUCURSAL] IS NULL OR [ID_SUCURSAL] <> @V_ID_SUCURSAL);

UPDATE [FINANZAS].[CAJAS]
SET [ID_SUCURSAL] = @V_ID_SUCURSAL,
    [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(),
    [ID_USUARIO_MODIFICACION] = @V_ID_USUARIO
WHERE [ID_EMPRESA] = @V_ID_EMPRESA
  AND [CODIGO] = N'CAJA-CENTRAL'
  AND [ID_SUCURSAL] <> @V_ID_SUCURSAL;
-- FIN: Asociación de los registros cuyo nombre ya los identifica como centrales.
