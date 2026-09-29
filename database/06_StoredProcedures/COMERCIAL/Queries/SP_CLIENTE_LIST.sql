/*
===============================================================================
Proyecto:              Sistema Modular de Gestión OxiTigre
Componente:            SP_CLIENTE_LIST
Archivo:               SP_CLIENTE_LIST.sql
Procedimiento:         COMERCIAL.SP_CLIENTE_LIST
Tipo:                  QUERY
Versión:               1.0.0
Fecha:                 2026-08-19
ID pedido:             FABRICA
Desarrollador:         Agustin Omar Cauzi
Correo:                agustincauzi10@hotmail.com
Descripción funcional: Lista clientes de una empresa con su teléfono principal y cantidad activa.
Parámetros de entrada: Empresa y código de estado solicitado.
Parámetros de sesión: @S_ID_SESION identifica la sesión funcional que consulta.
Parámetros de salida: @O_CODIGO_ERROR y @O_MENSAJE informan el resultado.
Retorno: Clientes ordenados por código, teléfono principal y cantidad de teléfonos activos.
Tablas utilizadas: COMERCIAL.CLIENTES, CLIENTES_TELEFONOS y CONFIGURACION.ESTADOS - READ.
Transacción: No aplica; solo lectura.
Auditoría: La DAL registra funcionalidad, sesión, SP, duración y resultado.
Historial de modificaciones:
1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
CREATE OR ALTER PROCEDURE [COMERCIAL].[SP_CLIENTE_LIST]
    @I_ID_EMPRESA    BIGINT,
    @I_CODIGO_ESTADO NVARCHAR(30) = N'ACTIVO',
    @S_ID_SESION     BIGINT,
    @O_CODIGO_ERROR  BIGINT OUTPUT,
    @O_MENSAJE       NVARCHAR(4000) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @O_CODIGO_ERROR = NULL;
    SET @O_MENSAJE = NULL;

    -- INICIO: Validación del estado informativo antes de consultar.
    IF NOT EXISTS
    (
        SELECT 1 FROM [CONFIGURACION].[ESTADOS]
        WHERE [ENTIDAD] = N'CLIENTES' AND [CODIGO_ESTADO] = @I_CODIGO_ESTADO
          AND ([FECHA_VIGENCIA_HASTA_UTC] IS NULL OR [FECHA_VIGENCIA_HASTA_UTC] > SYSUTCDATETIME())
    )
        THROW 50000, 'El estado informado no está habilitado para CLIENTES.', 1;
    -- FIN: Validación del estado informativo antes de consultar.

    -- INICIO: Consulta resumida de clientes y medios de contacto activos.
    SELECT [CLI].[ID_CLIENTE], [CLI].[CODIGO], [CLI].[TIPO_PERSONA],
           [CLI].[NOMBRE_RAZON_SOCIAL], [CLI].[APELLIDO], [CLI].[NUMERO_DOCUMENTO],
           [CLI].[EMAIL], [CLI].[CODIGO_ESTADO], [TEL].[TELEFONO_PRINCIPAL],
           [TEL].[CANTIDAD_TELEFONOS]
    FROM [COMERCIAL].[CLIENTES] AS [CLI]
    OUTER APPLY
    (
        SELECT
            MAX(CASE WHEN [T].[ES_PRINCIPAL] = 1 THEN CONCAT(
                CASE WHEN [T].[CODIGO_PAIS] IS NULL THEN N'' ELSE N'+' + [T].[CODIGO_PAIS] + N' ' END,
                CASE WHEN [T].[CODIGO_AREA] IS NULL THEN N'' ELSE [T].[CODIGO_AREA] + N' ' END,
                [T].[NUMERO]) END) AS [TELEFONO_PRINCIPAL],
            COUNT_BIG(*) AS [CANTIDAD_TELEFONOS]
        FROM [COMERCIAL].[CLIENTES_TELEFONOS] AS [T]
        WHERE [T].[ID_CLIENTE] = [CLI].[ID_CLIENTE] AND [T].[CODIGO_ESTADO] = N'ACTIVO'
    ) AS [TEL]
    WHERE [CLI].[ID_EMPRESA] = @I_ID_EMPRESA AND [CLI].[CODIGO_ESTADO] = @I_CODIGO_ESTADO
    ORDER BY [CLI].[CODIGO];
    -- FIN: Consulta resumida de clientes y medios de contacto activos.
END;
