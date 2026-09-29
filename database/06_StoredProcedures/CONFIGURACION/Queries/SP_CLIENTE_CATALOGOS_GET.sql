/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: SP_CLIENTE_CATALOGOS_GET
Archivo: SP_CLIENTE_CATALOGOS_GET.sql
Procedimiento: CONFIGURACION.SP_CLIENTE_CATALOGOS_GET
Tipo: QUERY
Versión: 1.2.0
Fecha: 2026-08-20
ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi
Correo: agustincauzi10@hotmail.com
Descripción funcional: Obtiene documentos, países y tipos de teléfono activos para clientes.
Parámetros de entrada: Usuario para aplicar su preferencia de idioma.
Parámetros de salida: Código y mensaje controlados.
Retorno: Tres conjuntos de resultados parametrizados.
Tablas utilizadas: TIPOS_DOCUMENTO, PAISES y TIPOS_TELEFONO - READ.
Transacción: No aplica; solo lectura.
Auditoría: La DAL registra la consulta funcional.
Historial de modificaciones:
1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
1.1.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Verificación de catálogos obligatorios.
1.2.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Nombres localizados por preferencia del usuario.
===============================================================================
*/
CREATE OR ALTER PROCEDURE [CONFIGURACION].[SP_CLIENTE_CATALOGOS_GET]
    @S_ID_USUARIO    BIGINT,
    @O_CODIGO_ERROR BIGINT OUTPUT,
    @O_MENSAJE      NVARCHAR(4000) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @O_CODIGO_ERROR = NULL;
    SET @O_MENSAJE = NULL;
    DECLARE @V_CANTIDAD_DOCUMENTOS INT;
    DECLARE @V_CANTIDAD_PAISES INT;
    DECLARE @V_CANTIDAD_TELEFONOS INT;
    DECLARE @V_CULTURA NVARCHAR(10) = COALESCE
    (
        (SELECT [CULTURA] FROM [CONFIGURACION].[USUARIOS_PREFERENCIAS]
         WHERE [ID_USUARIO] = @S_ID_USUARIO AND [CODIGO_ESTADO] = N'ACTIVO'),
        N'es-AR'
    );

    -- INICIO: Tipos de documento activos y alcance por persona.
    SELECT [T].[CODIGO], COALESCE([TR].[NOMBRE], [T].[NOMBRE]) AS [NOMBRE], [T].[APLICA_PERSONA_FISICA], [T].[APLICA_PERSONA_JURIDICA]
    FROM [CONFIGURACION].[TIPOS_DOCUMENTO] AS [T]
    LEFT JOIN [CONFIGURACION].[TRADUCCIONES_CATALOGO] AS [TR]
      ON [TR].[ENTIDAD] = N'TIPOS_DOCUMENTO' AND [TR].[CODIGO] = [T].[CODIGO]
     AND [TR].[CULTURA] = @V_CULTURA AND [TR].[CODIGO_ESTADO] = N'ACTIVO'
    WHERE [T].[CODIGO_ESTADO] = N'ACTIVO' ORDER BY [NOMBRE];
    SET @V_CANTIDAD_DOCUMENTOS = @@ROWCOUNT;
    -- FIN: Tipos de documento activos y alcance por persona.

    -- INICIO: Países activos con el predeterminado primero.
    SELECT [P].[CODIGO], COALESCE([TR].[NOMBRE], [P].[NOMBRE]) AS [NOMBRE], [P].[CODIGO_TELEFONICO], [P].[ES_PREDETERMINADO]
    FROM [CONFIGURACION].[PAISES] AS [P]
    LEFT JOIN [CONFIGURACION].[TRADUCCIONES_CATALOGO] AS [TR]
      ON [TR].[ENTIDAD] = N'PAISES' AND [TR].[CODIGO] = [P].[CODIGO]
     AND [TR].[CULTURA] = @V_CULTURA AND [TR].[CODIGO_ESTADO] = N'ACTIVO'
    WHERE [P].[CODIGO_ESTADO] = N'ACTIVO'
    ORDER BY [ES_PREDETERMINADO] DESC, [NOMBRE];
    SET @V_CANTIDAD_PAISES = @@ROWCOUNT;
    -- FIN: Países activos con el predeterminado primero.

    -- INICIO: Tipos de teléfono activos.
    SELECT [T].[CODIGO], COALESCE([TR].[NOMBRE], [T].[NOMBRE]) AS [NOMBRE]
    FROM [CONFIGURACION].[TIPOS_TELEFONO] AS [T]
    LEFT JOIN [CONFIGURACION].[TRADUCCIONES_CATALOGO] AS [TR]
      ON [TR].[ENTIDAD] = N'TIPOS_TELEFONO' AND [TR].[CODIGO] = [T].[CODIGO]
     AND [TR].[CULTURA] = @V_CULTURA AND [TR].[CODIGO_ESTADO] = N'ACTIVO'
    WHERE [T].[CODIGO_ESTADO] = N'ACTIVO' ORDER BY [NOMBRE];
    SET @V_CANTIDAD_TELEFONOS = @@ROWCOUNT;
    -- FIN: Tipos de teléfono activos.

    IF @V_CANTIDAD_DOCUMENTOS = 0 OR @V_CANTIDAD_PAISES = 0 OR @V_CANTIDAD_TELEFONOS = 0
    BEGIN
        SET @O_CODIGO_ERROR = 40003;
        EXEC [AUDITORIA].[SP_ERROR_MESSAGE_RESOLVE]
            @I_CODIGO_ERROR = @O_CODIGO_ERROR, @I_MENSAJE_PERSONALIZADO = NULL,
            @O_MENSAJE = @O_MENSAJE OUTPUT;
    END;
END;
