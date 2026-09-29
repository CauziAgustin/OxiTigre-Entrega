/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: SP_CLIENTE_BORRADOR_GET
Archivo: SP_CLIENTE_BORRADOR_GET.sql
Procedimiento: COMERCIAL.SP_CLIENTE_BORRADOR_GET
Tipo: QUERY
Versión: 1.0.0
Fecha: 2026-08-20
ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi
Correo: agustincauzi10@hotmail.com
Descripción funcional: Recupera la última precarga incompleta del usuario en su empresa.
Parámetros de entrada: Empresa y usuario autenticado.
Parámetros de salida: Código y mensaje controlados.
Retorno: JSON y fecha del borrador activo, si existe.
Tablas utilizadas: COMERCIAL.CLIENTES_BORRADORES - READ.
Transacción: No aplica; solo lectura.
Auditoría: La DAL registra la consulta funcional.
Historial de modificaciones:
1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
CREATE OR ALTER PROCEDURE [COMERCIAL].[SP_CLIENTE_BORRADOR_GET]
    @I_ID_EMPRESA    BIGINT,
    @S_ID_USUARIO    BIGINT,
    @O_CODIGO_ERROR  BIGINT OUTPUT,
    @O_MENSAJE       NVARCHAR(4000) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @O_CODIGO_ERROR = NULL;
    SET @O_MENSAJE = NULL;

    -- INICIO: Recuperación aislada del borrador vigente.
    SELECT TOP (1) [CONTENIDO_JSON], [FECHA_ULTIMO_GUARDADO_UTC]
    FROM [COMERCIAL].[CLIENTES_BORRADORES]
    WHERE [ID_EMPRESA] = @I_ID_EMPRESA AND [ID_USUARIO] = @S_ID_USUARIO
      AND [CODIGO_ESTADO] = N'BORRADOR'
    ORDER BY [FECHA_ULTIMO_GUARDADO_UTC] DESC;
    -- FIN: Recuperación aislada del borrador vigente.
END;
