/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: SP_ROL_LIST
Archivo: SP_ROL_LIST.sql
Procedimiento: SEGURIDAD.SP_ROL_LIST
Tipo: QUERY
Versión: 1.1.0
Fecha: 2026-08-20
ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi
Correo: agustincauzi10@hotmail.com
Descripción funcional: Lista roles activos asignables dentro de una empresa.
Parámetros de entrada: @I_ID_EMPRESA limita el aislamiento empresarial.
Parámetros de salida: Código y mensaje controlados.
Retorno: Código y nombre de cada rol activo.
Tablas utilizadas: SEGURIDAD.ROLES - READ.
Transacción: No aplica; solo lectura.
Auditoría: La consulta se registra desde la DAL.
Historial de modificaciones:
1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
1.1.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Código funcional cuando no existen roles activos.
===============================================================================
*/
CREATE OR ALTER PROCEDURE [SEGURIDAD].[SP_ROL_LIST]
    @I_ID_EMPRESA    BIGINT,
    @O_CODIGO_ERROR BIGINT OUTPUT,
    @O_MENSAJE      NVARCHAR(4000) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @O_CODIGO_ERROR = NULL;
    SET @O_MENSAJE = NULL;

    -- INICIO: Consulta de roles activos asignables en la empresa.
    SELECT [CODIGO], [NOMBRE]
    FROM [SEGURIDAD].[ROLES]
    WHERE [ID_EMPRESA] = @I_ID_EMPRESA AND [CODIGO_ESTADO] = N'ACTIVO'
    ORDER BY [NOMBRE];
    -- FIN: Consulta de roles activos asignables en la empresa.

    IF @@ROWCOUNT = 0
    BEGIN
        SET @O_CODIGO_ERROR = 10006;
        EXEC [AUDITORIA].[SP_ERROR_MESSAGE_RESOLVE]
            @I_CODIGO_ERROR = @O_CODIGO_ERROR, @I_MENSAJE_PERSONALIZADO = NULL,
            @O_MENSAJE = @O_MENSAJE OUTPUT;
    END;
END;
