/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: SP_ERROR_MESSAGE_RESOLVE
Archivo: SP_ERROR_MESSAGE_RESOLVE.sql
Procedimiento: AUDITORIA.SP_ERROR_MESSAGE_RESOLVE
Tipo: QUERY
Versión: 1.1.0
Fecha: 2026-08-20
ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi
Correo: agustincauzi10@hotmail.com
Descripción funcional: Valida el código y resuelve un mensaje personalizado o su descripción central.
Parámetros de entrada: Código funcional y mensaje personalizado opcional.
Parámetros de salida: Mensaje final listo para devolver al consumidor.
Retorno: No retorna filas.
Tablas utilizadas: AUDITORIA.CATALOGO_ERRORES - READ.
Transacción: No aplica; solo lectura.
Auditoría: No se audita para evitar recursión desde otros SP.
Historial de modificaciones:
1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
1.1.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Validación obligatoria del código antes de personalizar el mensaje.
===============================================================================
*/
CREATE OR ALTER PROCEDURE [AUDITORIA].[SP_ERROR_MESSAGE_RESOLVE]
    @I_CODIGO_ERROR         BIGINT,
    @I_MENSAJE_PERSONALIZADO NVARCHAR(4000) = NULL,
    @O_MENSAJE              NVARCHAR(4000) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS
    (
        SELECT 1
        FROM [AUDITORIA].[CATALOGO_ERRORES]
        WHERE [CODIGO_ERROR] = @I_CODIGO_ERROR AND [CODIGO_ESTADO] = N'ACTIVO'
    )
    BEGIN
        SET @O_MENSAJE = CONCAT(N'El código de error ', @I_CODIGO_ERROR, N' no existe o no está activo en el catálogo.');
        THROW 50000, @O_MENSAJE, 1;
    END;

    -- INICIO: Resolución del mensaje funcional con prioridad para el contexto específico.
    SET @O_MENSAJE = NULLIF(LTRIM(RTRIM(@I_MENSAJE_PERSONALIZADO)), N'');
    IF @O_MENSAJE IS NULL
    BEGIN
        SELECT @O_MENSAJE = [DESCRIPCION]
        FROM [AUDITORIA].[CATALOGO_ERRORES]
        WHERE [CODIGO_ERROR] = @I_CODIGO_ERROR AND [CODIGO_ESTADO] = N'ACTIVO';
    END;
    -- FIN: Resolución del mensaje funcional con prioridad para el contexto específico.
END;
