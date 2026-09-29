/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: SP_USUARIO_LOGIN_RESULT
Archivo: SP_USUARIO_LOGIN_RESULT.sql
Procedimiento: SEGURIDAD.SP_USUARIO_LOGIN_RESULT
Tipo: COMMAND
Versión: 1.1.0
Fecha: 2026-08-20
ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi
Correo: agustincauzi10@hotmail.com
Descripción funcional: Registra el resultado de autenticación y bloquea quince minutos al quinto fallo.
Parámetros de entrada: Usuario y resultado de la verificación criptográfica.
Parámetros de sesión: No aplica; se ejecuta antes de crear la sesión.
Parámetros de salida: Código, mensaje y filas afectadas.
Retorno: No retorna filas.
Tablas utilizadas: SEGURIDAD.USUARIOS - UPDATE.
Transacción: Una actualización atómica.
Auditoría: No registra credenciales; el contador queda en la cuenta.
Historial de modificaciones:
1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
1.1.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Usuario inexistente y fallos técnicos controlados.
===============================================================================
*/
CREATE OR ALTER PROCEDURE [SEGURIDAD].[SP_USUARIO_LOGIN_RESULT]
    @I_ID_USUARIO      BIGINT,
    @I_ES_EXITOSO      BIT,
    @O_CODIGO_ERROR    BIGINT OUTPUT,
    @O_MENSAJE         NVARCHAR(4000) OUTPUT,
    @O_FILAS_AFECTADAS INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @O_CODIGO_ERROR = NULL;
    SET @O_MENSAJE = NULL;
    SET @O_FILAS_AFECTADAS = 0;

    BEGIN TRY
        -- INICIO: Restablecimiento por éxito o incremento y bloqueo por fallo.
        IF @I_ES_EXITOSO = 1
        BEGIN
            UPDATE [SEGURIDAD].[USUARIOS]
            SET [INTENTOS_FALLIDOS] = 0, [FECHA_BLOQUEO_UTC] = NULL,
                [FECHA_ULTIMO_ACCESO_UTC] = SYSUTCDATETIME()
            WHERE [ID_USUARIO] = @I_ID_USUARIO;
        END
        ELSE
        BEGIN
            UPDATE [SEGURIDAD].[USUARIOS]
            SET [INTENTOS_FALLIDOS] = CASE WHEN [INTENTOS_FALLIDOS] < 32767 THEN [INTENTOS_FALLIDOS] + 1 ELSE [INTENTOS_FALLIDOS] END,
                [FECHA_BLOQUEO_UTC] = CASE WHEN [INTENTOS_FALLIDOS] + 1 >= 5 THEN SYSUTCDATETIME() ELSE [FECHA_BLOQUEO_UTC] END
            WHERE [ID_USUARIO] = @I_ID_USUARIO;
        END;
        SET @O_FILAS_AFECTADAS = @@ROWCOUNT;
        -- FIN: Restablecimiento por éxito o incremento y bloqueo por fallo.

        IF @O_FILAS_AFECTADAS = 0
        BEGIN
            SET @O_CODIGO_ERROR = 10001;
            EXEC [AUDITORIA].[SP_ERROR_MESSAGE_RESOLVE]
                @I_CODIGO_ERROR = @O_CODIGO_ERROR, @I_MENSAJE_PERSONALIZADO = NULL,
                @O_MENSAJE = @O_MENSAJE OUTPUT;
            THROW 50000, @O_MENSAJE, 1;
        END;
    END TRY
    BEGIN CATCH
        SET @O_CODIGO_ERROR = COALESCE(@O_CODIGO_ERROR, CONVERT(BIGINT, 70002));
        SET @O_MENSAJE = COALESCE(@O_MENSAJE, ERROR_MESSAGE());
        THROW;
    END CATCH;
END;
