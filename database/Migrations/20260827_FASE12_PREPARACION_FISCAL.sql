/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: Migración incremental Fase 12A
Archivo: 20260827_FASE12_PREPARACION_FISCAL.sql | Versión: 12.0.0 | Fecha: 2026-08-27 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Incorpora preparación fiscal sin habilitar emisión ni almacenar credenciales.
Historial: 12.0.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
DECLARE @V_ID_MODULO BIGINT;

-- INICIO: El módulo se identifica con el prefijo 9 para sus futuros códigos de error.
IF NOT EXISTS (SELECT 1 FROM [CONFIGURACION].[MODULOS] WHERE [CODIGO] = N'FISCAL')
BEGIN
    INSERT INTO [CONFIGURACION].[MODULOS]
        ([NUMERO_MODULO], [CODIGO], [NOMBRE], [DESCRIPCION], [ORDEN], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
    VALUES
        (9, N'FISCAL', N'Facturación fiscal', N'Preparación y autorización tributaria de comprobantes', 8, N'ACTIVO', 1);
END;

SELECT @V_ID_MODULO = [ID_MODULO]
FROM [CONFIGURACION].[MODULOS]
WHERE [CODIGO] = N'FISCAL';
-- FIN: El módulo se identifica con el prefijo 9 para sus futuros códigos de error.

-- INICIO: La consulta fiscal se autoriza con un permiso propio y auditable.
IF NOT EXISTS (SELECT 1 FROM [SEGURIDAD].[PERMISOS] WHERE [CODIGO] = N'FISCAL.CONSULTAR')
BEGIN
    INSERT INTO [SEGURIDAD].[PERMISOS]
        ([ID_MODULO], [CODIGO], [NOMBRE], [DESCRIPCION], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
    VALUES
        (@V_ID_MODULO, N'FISCAL.CONSULTAR', N'Consultar preparación fiscal',
         N'Permite revisar requisitos y ventas candidatas sin emitir comprobantes.', N'ACTIVO', 1);
END;

INSERT INTO [SEGURIDAD].[ROLES_PERMISOS]
    ([ID_ROL], [ID_PERMISO], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
SELECT [ROL].[ID_ROL], [PER].[ID_PERMISO], N'ACTIVO', 1
FROM [SEGURIDAD].[ROLES] AS [ROL]
CROSS JOIN [SEGURIDAD].[PERMISOS] AS [PER]
WHERE [ROL].[CODIGO] IN (N'ADMINISTRADOR', N'CONSULTA')
  AND [PER].[CODIGO] = N'FISCAL.CONSULTAR'
  AND NOT EXISTS
  (
      SELECT 1
      FROM [SEGURIDAD].[ROLES_PERMISOS] AS [ACT]
      WHERE [ACT].[ID_ROL] = [ROL].[ID_ROL]
        AND [ACT].[ID_PERMISO] = [PER].[ID_PERMISO]
        AND [ACT].[CODIGO_ESTADO] = N'ACTIVO'
  );
-- FIN: La consulta fiscal se autoriza con un permiso propio y auditable.

-- INICIO: Solo se guardan definiciones no sensibles; la credencial se resolverá desde un almacén externo.
INSERT INTO [CONFIGURACION].[PARAMETROS_SISTEMA]
    ([ID_MODULO], [CLAVE], [VALOR], [TIPO_DATO], [ES_SECRETO], [DESCRIPCION], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
SELECT @V_ID_MODULO, [DAT].[CLAVE], [DAT].[VALOR], N'TEXTO', 0, [DAT].[DESCRIPCION], N'ACTIVO', 1
FROM (VALUES
    (N'FISCAL.CONDICION_IVA_EMISOR', CONVERT(NVARCHAR(2000), NULL), N'Condición frente al IVA confirmada por el asesor contable.'),
    (N'FISCAL.SERVICIO_ARCA', CONVERT(NVARCHAR(2000), NULL), N'Servicio confirmado para homologación: WSFEV1 o WSMTXCA.'),
    (N'FISCAL.AMBIENTE_ARCA', CONVERT(NVARCHAR(2000), N'HOMOLOGACION'), N'Ambiente fiscal. Producción se habilita mediante despliegue controlado.'),
    (N'FISCAL.PUNTO_VENTA', CONVERT(NVARCHAR(2000), NULL), N'Punto de venta Web Services habilitado por ARCA para la empresa.')
) AS [DAT] ([CLAVE], [VALOR], [DESCRIPCION])
WHERE NOT EXISTS
(
    SELECT 1
    FROM [CONFIGURACION].[PARAMETROS_SISTEMA] AS [ACT]
    WHERE [ACT].[ID_MODULO] = @V_ID_MODULO
      AND [ACT].[CLAVE] = [DAT].[CLAVE]
);
-- FIN: Solo se guardan definiciones no sensibles; la credencial se resolverá desde un almacén externo.
