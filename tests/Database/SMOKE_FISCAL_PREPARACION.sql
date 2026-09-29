/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: Smoke de preparación fiscal Fase 12A
Archivo: SMOKE_FISCAL_PREPARACION.sql | Versión: 12.0.0 | Fecha: 2026-08-27 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Verifica módulo, permiso y parámetros fiscales sin credenciales almacenadas.
Historial: 12.0.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
SET NOCOUNT ON;

DECLARE @V_ID_MODULO BIGINT =
(
    SELECT [ID_MODULO]
    FROM [CONFIGURACION].[MODULOS]
    WHERE [CODIGO] = N'FISCAL'
      AND [NUMERO_MODULO] = 9
);

IF @V_ID_MODULO IS NULL
    THROW 50000, N'No se registró correctamente el módulo fiscal.', 1;

IF NOT EXISTS (SELECT 1 FROM [SEGURIDAD].[PERMISOS] WHERE [CODIGO] = N'FISCAL.CONSULTAR')
    THROW 50000, N'No se registró el permiso de consulta fiscal.', 1;

IF
(
    SELECT COUNT(*)
    FROM [CONFIGURACION].[PARAMETROS_SISTEMA]
    WHERE [ID_MODULO] = @V_ID_MODULO
      AND [CLAVE] IN
      (
          N'FISCAL.CONDICION_IVA_EMISOR',
          N'FISCAL.SERVICIO_ARCA',
          N'FISCAL.AMBIENTE_ARCA',
          N'FISCAL.PUNTO_VENTA'
      )
) <> 4
    THROW 50000, N'Faltan parámetros de preparación fiscal.', 1;

IF EXISTS
(
    SELECT 1
    FROM [CONFIGURACION].[PARAMETROS_SISTEMA]
    WHERE [ID_MODULO] = @V_ID_MODULO
      AND ([ES_SECRETO] = 1 AND [VALOR] IS NOT NULL OR [CLAVE] LIKE N'%CLAVE_PRIVADA%')
)
    THROW 50000, N'Se detectó un secreto fiscal almacenado como valor operativo.', 1;

PRINT N'SMOKE_FISCAL_PREPARACION_OK';
