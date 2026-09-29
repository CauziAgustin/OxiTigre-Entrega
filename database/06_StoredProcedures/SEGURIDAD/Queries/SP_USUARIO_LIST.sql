/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: SP_USUARIO_LIST
Archivo: SP_USUARIO_LIST.sql
Procedimiento: SEGURIDAD.SP_USUARIO_LIST
Tipo: QUERY
Versión: 1.0.0
Fecha: 2026-08-20
ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi
Correo: agustincauzi10@hotmail.com
Descripción funcional: Lista usuarios de una empresa con sus roles activos.
Parámetros de entrada: @I_ID_EMPRESA limita el aislamiento empresarial.
Parámetros de salida: Código y mensaje controlados.
Retorno: Identidad, estado y roles del usuario.
Tablas utilizadas: SEGURIDAD.USUARIOS, USUARIOS_ROLES y ROLES - READ.
Transacción: No aplica; solo lectura.
Auditoría: La consulta se registra desde la DAL.
Historial de modificaciones:
1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
CREATE OR ALTER PROCEDURE [SEGURIDAD].[SP_USUARIO_LIST]
    @I_ID_EMPRESA    BIGINT,
    @O_CODIGO_ERROR  BIGINT OUTPUT,
    @O_MENSAJE       NVARCHAR(4000) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @O_CODIGO_ERROR = NULL;
    SET @O_MENSAJE = NULL;

    SELECT [USR].[ID_USUARIO], [USR].[NOMBRE_USUARIO], [USR].[NOMBRES], [USR].[APELLIDO],
           [USR].[EMAIL], [USR].[CODIGO_ESTADO], ISNULL(STRING_AGG([ROL].[CODIGO], N','), N'') AS [ROLES]
    FROM [SEGURIDAD].[USUARIOS] AS [USR]
    LEFT JOIN [SEGURIDAD].[USUARIOS_ROLES] AS [UR]
        ON [UR].[ID_USUARIO] = [USR].[ID_USUARIO] AND [UR].[CODIGO_ESTADO] = N'ACTIVO'
    LEFT JOIN [SEGURIDAD].[ROLES] AS [ROL]
        ON [ROL].[ID_ROL] = [UR].[ID_ROL] AND [ROL].[CODIGO_ESTADO] = N'ACTIVO'
    WHERE [USR].[ID_EMPRESA] = @I_ID_EMPRESA
    GROUP BY [USR].[ID_USUARIO], [USR].[NOMBRE_USUARIO], [USR].[NOMBRES], [USR].[APELLIDO],
             [USR].[EMAIL], [USR].[CODIGO_ESTADO]
    ORDER BY [USR].[APELLIDO], [USR].[NOMBRES];
END;
