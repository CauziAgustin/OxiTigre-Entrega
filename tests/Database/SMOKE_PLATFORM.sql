/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: Prueba de plataforma multiempresa
Archivo: SMOKE_PLATFORM.sql | Versión: 1.0.0 | Fecha: 2026-08-27 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Comprueba registro central, base operativa y sucursal San Fernando sin modificar datos.
Historial: 1.0.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
SET NOCOUNT ON;

IF DB_ID(N'OxiTigre_Platform') IS NULL
    THROW 50000, 'Falta OxiTigre_Platform.', 1;

IF DB_ID(N'OxiTigre_Development') IS NULL
    THROW 50000, 'Falta OxiTigre_Development.', 1;

IF OBJECT_ID(N'OxiTigre_Platform.PLATAFORMA.EMPRESAS_BASES', N'U') IS NULL
    THROW 50000, 'Falta el registro central de empresas.', 1;

IF NOT EXISTS
(
    SELECT 1
    FROM [OxiTigre_Platform].[PLATAFORMA].[EMPRESAS_BASES]
    WHERE [CODIGO] = N'OXITIGRE'
      AND [BASE_DATOS] = N'OxiTigre_Development'
      AND [CODIGO_ESTADO] = N'ACTIVO'
)
    THROW 50000, 'OXITIGRE no está registrada como empresa activa.', 1;

IF NOT EXISTS
(
    SELECT 1
    FROM [OxiTigre_Development].[CONFIGURACION].[SUCURSALES]
    WHERE [CODIGO] = N'SAN_FERNANDO'
      AND [LOCALIDAD] = N'San Fernando'
      AND [CODIGO_ESTADO] = N'ACTIVO'
)
    THROW 50000, 'Falta la sucursal San Fernando.', 1;

IF NOT EXISTS
(
    SELECT 1
    FROM [OxiTigre_Development].[INVENTARIO].[DEPOSITOS] AS [DEP]
    INNER JOIN [OxiTigre_Development].[CONFIGURACION].[SUCURSALES] AS [SUC]
        ON [SUC].[ID_SUCURSAL] = [DEP].[ID_SUCURSAL]
    WHERE [SUC].[CODIGO] = N'SAN_FERNANDO'
      AND [DEP].[CODIGO] = N'SAN_FERNANDO'
)
    THROW 50000, 'Falta el depósito asociado a San Fernando.', 1;

SELECT N'PLATAFORMA_MULTIEMPRESA_OK' AS [RESULTADO];
