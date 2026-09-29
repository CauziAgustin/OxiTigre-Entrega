/*
===============================================================================
Proyecto:              Sistema Modular de Gestión OxiTigre
Componente:            SP_ERROR_GET_BY_CODE
Archivo:               SP_ERROR_GET_BY_CODE.sql
Procedimiento:         AUDITORIA.SP_ERROR_GET_BY_CODE
Tipo:                  QUERY
Versión:               1.1.0
Fecha:                 2026-08-20
ID pedido:             FABRICA
Desarrollador:         Agustin Omar Cauzi
Correo:                agustincauzi10@hotmail.com

Descripción funcional:
Obtiene la definición de un código de error conocido y su solución preferida
para que el equipo pueda identificar rápidamente módulo, causa y acción.

Parámetros de entrada:
@I_CODIGO_ERROR BIGINT - Código formado por número de módulo y cuatro dígitos.

Parámetros de salida:
@O_CODIGO_ERROR BIGINT OUTPUT - NULL si la consulta se ejecutó correctamente.
@O_MENSAJE NVARCHAR(4000) OUTPUT - Informa si el código no fue encontrado.

Retorno:
Una fila con el error, módulo y solución preferida; ninguna si no existe.

Tablas utilizadas:
AUDITORIA.CATALOGO_ERRORES - READ
CONFIGURACION.MODULOS - READ
AUDITORIA.SOLUCIONES_ERROR - READ

Transacción: No aplica; procedimiento de solo lectura.
Auditoría: El ejecutor DAL registra la consulta y su correlación.

Historial de modificaciones:
1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
1.1.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Devuelve 70001 cuando el código solicitado no existe.
===============================================================================
*/
CREATE OR ALTER PROCEDURE [AUDITORIA].[SP_ERROR_GET_BY_CODE]
    @I_CODIGO_ERROR BIGINT,
    @O_CODIGO_ERROR BIGINT OUTPUT,
    @O_MENSAJE      NVARCHAR(4000) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    -- INICIO: Inicialización de parámetros de salida.
    SET @O_CODIGO_ERROR = NULL;
    SET @O_MENSAJE = NULL;
    -- FIN: Inicialización de parámetros de salida.

    -- INICIO: Validación del código solicitado.
    IF @I_CODIGO_ERROR IS NULL OR @I_CODIGO_ERROR < 10001
    BEGIN
        THROW 50000, 'El código de error informado no es válido.', 1;
    END; -- FIN: Validación del código solicitado.

    -- INICIO: Consulta del diagnóstico y la solución preferida.
    SELECT
        [ERR].[CODIGO_ERROR],
        [MOD].[NUMERO_MODULO],
        [MOD].[CODIGO] AS [CODIGO_MODULO],
        [MOD].[NOMBRE] AS [NOMBRE_MODULO],
        [ERR].[NUMERO_ERROR],
        [ERR].[NOMBRE],
        [ERR].[DESCRIPCION],
        [ERR].[CAUSA_PROBABLE],
        [ERR].[ACCION_RECOMENDADA],
        [ERR].[SEVERIDAD],
        [SOL].[TITULO] AS [TITULO_SOLUCION],
        [SOL].[PASOS_SOLUCION]
    FROM [AUDITORIA].[CATALOGO_ERRORES] AS [ERR]
    INNER JOIN [CONFIGURACION].[MODULOS] AS [MOD]
        ON [MOD].[ID_MODULO] = [ERR].[ID_MODULO]
    OUTER APPLY
    (
        SELECT TOP (1) [S].[TITULO], [S].[PASOS_SOLUCION]
        FROM [AUDITORIA].[SOLUCIONES_ERROR] AS [S]
        WHERE [S].[ID_CATALOGO_ERROR] = [ERR].[ID_CATALOGO_ERROR]
          AND [S].[CODIGO_ESTADO] = N'ACTIVO'
        ORDER BY [S].[ES_PREFERIDA] DESC, [S].[ID_SOLUCION_ERROR]
    ) AS [SOL]
    WHERE [ERR].[CODIGO_ERROR] = @I_CODIGO_ERROR
      AND [ERR].[CODIGO_ESTADO] = N'ACTIVO';
    -- FIN: Consulta del diagnóstico y la solución preferida.

    -- INICIO: Comunicación de un resultado vacío mediante el código general del catálogo.
    IF @@ROWCOUNT = 0
    BEGIN
        SET @O_CODIGO_ERROR = 70001;
        SET @O_MENSAJE = N'No existe un error activo con el código informado.';
    END; -- FIN: Comunicación de un resultado vacío mediante el código general del catálogo.
END;
