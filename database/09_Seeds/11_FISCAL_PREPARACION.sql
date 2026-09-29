/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: Preparación fiscal argentina
Archivo: 11_FISCAL_PREPARACION.sql | Versión: 12.0.0 | Fecha: 2026-08-27 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Registra parámetros no sensibles para preparar la homologación fiscal.
Historial: 12.0.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
DECLARE @V_ID_MODULO BIGINT =
(
    SELECT [ID_MODULO]
    FROM [CONFIGURACION].[MODULOS]
    WHERE [CODIGO] = N'FISCAL'
);

-- INICIO: Valores visibles y editables; certificados y claves privadas quedan fuera de la base.
INSERT INTO [CONFIGURACION].[PARAMETROS_SISTEMA]
    ([ID_MODULO], [CLAVE], [VALOR], [TIPO_DATO], [ES_SECRETO], [DESCRIPCION], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
SELECT @V_ID_MODULO, [DAT].[CLAVE], [DAT].[VALOR], N'TEXTO', 0, [DAT].[DESCRIPCION], N'ACTIVO', 1
FROM (VALUES
    (N'FISCAL.CONDICION_IVA_EMISOR', CONVERT(NVARCHAR(2000), NULL), N'Condición frente al IVA confirmada por el asesor contable.'),
    (N'FISCAL.SERVICIO_ARCA', CONVERT(NVARCHAR(2000), NULL), N'Servicio confirmado para homologación: WSFEV1 o WSMTXCA.'),
    (N'FISCAL.AMBIENTE_ARCA', CONVERT(NVARCHAR(2000), N'HOMOLOGACION'), N'Ambiente fiscal. Producción se habilita mediante despliegue controlado.'),
    (N'FISCAL.PUNTO_VENTA', CONVERT(NVARCHAR(2000), NULL), N'Punto de venta Web Services habilitado por ARCA para la empresa.')
) AS [DAT] ([CLAVE], [VALOR], [DESCRIPCION])
WHERE @V_ID_MODULO IS NOT NULL
  AND NOT EXISTS
  (
      SELECT 1
      FROM [CONFIGURACION].[PARAMETROS_SISTEMA] AS [ACT]
      WHERE [ACT].[ID_MODULO] = @V_ID_MODULO
        AND [ACT].[CLAVE] = [DAT].[CLAVE]
  );
-- FIN: Valores visibles y editables; certificados y claves privadas quedan fuera de la base.
