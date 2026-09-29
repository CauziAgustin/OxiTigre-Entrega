/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: SMOKE_ERROR_MESSAGES
Archivo: SMOKE_ERROR_MESSAGES.sql | Versión: 1.0.0 | Fecha: 2026-08-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Comprueba la resolución central y personalizada de errores catalogados.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
SET NOCOUNT ON;

DECLARE @V_MENSAJE NVARCHAR(4000);

EXEC [AUDITORIA].[SP_ERROR_MESSAGE_RESOLVE]
    @I_CODIGO_ERROR = 40002, @I_MENSAJE_PERSONALIZADO = NULL,
    @O_MENSAJE = @V_MENSAJE OUTPUT;
IF NULLIF(@V_MENSAJE, N'') IS NULL
    THROW 51000, 'No se obtuvo la descripción central del código.', 1;

EXEC [AUDITORIA].[SP_ERROR_MESSAGE_RESOLVE]
    @I_CODIGO_ERROR = 40002, @I_MENSAJE_PERSONALIZADO = N'El cliente 25 no está disponible para esta empresa.',
    @O_MENSAJE = @V_MENSAJE OUTPUT;
IF @V_MENSAJE <> N'El cliente 25 no está disponible para esta empresa.'
    THROW 51001, 'No se respetó el mensaje personalizado.', 1;

BEGIN TRY
    EXEC [AUDITORIA].[SP_ERROR_MESSAGE_RESOLVE]
        @I_CODIGO_ERROR = 49999, @I_MENSAJE_PERSONALIZADO = N'Este mensaje no debe habilitar un código inexistente.',
        @O_MENSAJE = @V_MENSAJE OUTPUT;
    THROW 51002, 'Se aceptó un código inexistente.', 1;
END TRY
BEGIN CATCH
    IF ERROR_NUMBER() <> 50000 THROW;
END CATCH;

SELECT N'OK' AS [RESULTADO], N'Códigos y mensajes de error validados.' AS [DETALLE];
