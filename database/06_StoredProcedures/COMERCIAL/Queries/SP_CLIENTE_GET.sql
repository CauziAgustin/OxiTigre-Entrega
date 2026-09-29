/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: SP_CLIENTE_GET
Archivo: SP_CLIENTE_GET.sql
Procedimiento: COMERCIAL.SP_CLIENTE_GET
Tipo: QUERY
Versión: 1.1.0
Fecha: 2026-08-20
ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi
Correo: agustincauzi10@hotmail.com
Descripción funcional: Obtiene el detalle de un cliente y todos sus teléfonos activos.
Parámetros de entrada: Empresa y cliente garantizan aislamiento.
Parámetros de salida: Código y mensaje controlados.
Retorno: Dos resultados: cliente y teléfonos activos ordenados.
Tablas utilizadas: COMERCIAL.CLIENTES, CLIENTES_TELEFONOS y CONFIGURACION.TIPOS_TELEFONO - READ.
Transacción: No aplica; solo lectura.
Auditoría: La DAL registra la consulta funcional.
Historial de modificaciones:
1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
1.1.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Código funcional cuando el cliente no existe.
===============================================================================
*/
CREATE OR ALTER PROCEDURE [COMERCIAL].[SP_CLIENTE_GET]
    @I_ID_EMPRESA    BIGINT,
    @I_ID_CLIENTE    BIGINT,
    @O_CODIGO_ERROR  BIGINT OUTPUT,
    @O_MENSAJE       NVARCHAR(4000) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @O_CODIGO_ERROR = NULL;
    SET @O_MENSAJE = NULL;

    SELECT [CLI].[ID_CLIENTE], [CLI].[CODIGO], [CLI].[TIPO_PERSONA], [CLI].[NOMBRE_RAZON_SOCIAL],
           [CLI].[APELLIDO], [CLI].[TIPO_DOCUMENTO], [CLI].[NUMERO_DOCUMENTO], [CLI].[EMAIL],
           [CLI].[OBSERVACION], [CLI].[CODIGO_ESTADO]
    FROM [COMERCIAL].[CLIENTES] AS [CLI]
    WHERE [CLI].[ID_EMPRESA] = @I_ID_EMPRESA AND [CLI].[ID_CLIENTE] = @I_ID_CLIENTE;

    IF @@ROWCOUNT = 0
    BEGIN
        SET @O_CODIGO_ERROR = 40002;
        EXEC [AUDITORIA].[SP_ERROR_MESSAGE_RESOLVE]
            @I_CODIGO_ERROR = @O_CODIGO_ERROR, @I_MENSAJE_PERSONALIZADO = NULL,
            @O_MENSAJE = @O_MENSAJE OUTPUT;
    END;

    SELECT [TEL].[ID_CLIENTE_TELEFONO], [TIP].[CODIGO] AS [CODIGO_TIPO], [TIP].[NOMBRE] AS [NOMBRE_TIPO],
           [TEL].[CODIGO_PAIS], [TEL].[CODIGO_AREA], [TEL].[NUMERO], [TEL].[INTERNO], [TEL].[ORDEN],
           [TEL].[ES_PRINCIPAL], [TEL].[PERMITE_WHATSAPP], [TEL].[OBSERVACION], [TEL].[CODIGO_ESTADO]
    FROM [COMERCIAL].[CLIENTES_TELEFONOS] AS [TEL]
    INNER JOIN [COMERCIAL].[CLIENTES] AS [CLI] ON [CLI].[ID_CLIENTE] = [TEL].[ID_CLIENTE]
    INNER JOIN [CONFIGURACION].[TIPOS_TELEFONO] AS [TIP] ON [TIP].[ID_TIPO_TELEFONO] = [TEL].[ID_TIPO_TELEFONO]
    WHERE [CLI].[ID_EMPRESA] = @I_ID_EMPRESA AND [CLI].[ID_CLIENTE] = @I_ID_CLIENTE
      AND [TEL].[CODIGO_ESTADO] = N'ACTIVO'
    ORDER BY [TEL].[ORDEN];
END;
