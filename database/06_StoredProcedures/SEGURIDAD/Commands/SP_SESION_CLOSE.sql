/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: SP_SESION_CLOSE
Archivo: SP_SESION_CLOSE.sql
Procedimiento: SEGURIDAD.SP_SESION_CLOSE
Tipo: COMMAND
Versión: 1.1.0
Fecha: 2026-08-20
ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi
Correo: agustincauzi10@hotmail.com
Descripción funcional: Cierra voluntariamente una sesión vigente del usuario autenticado.
Parámetros de entrada: Identificador de sesión.
Parámetros de sesión: @S_ID_USUARIO impide cerrar sesiones ajenas.
Parámetros de salida: Código, mensaje y filas afectadas.
Retorno: No retorna filas.
Tablas utilizadas: SEGURIDAD.SESIONES - UPDATE.
Transacción: Una actualización atómica.
Auditoría: La DAL registra el cierre con su sesión.
Historial de modificaciones:
1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
1.1.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Captura de fallos técnicos del cierre idempotente.
===============================================================================
*/
CREATE OR ALTER PROCEDURE [SEGURIDAD].[SP_SESION_CLOSE]
    @I_ID_SESION       BIGINT,
    @S_ID_USUARIO      BIGINT,
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
        -- INICIO: Cierre de la sesión propia si continúa vigente.
        UPDATE [SEGURIDAD].[SESIONES]
        SET [CODIGO_ESTADO] = N'CERRADA', [FECHA_CIERRE_UTC] = SYSUTCDATETIME(),
            [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(), [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO
        WHERE [ID_SESION] = @I_ID_SESION AND [ID_USUARIO] = @S_ID_USUARIO AND [CODIGO_ESTADO] = N'VIGENTE';
        SET @O_FILAS_AFECTADAS = @@ROWCOUNT;
        -- FIN: Cierre de la sesión propia si continúa vigente.
    END TRY
    BEGIN CATCH
        SET @O_CODIGO_ERROR = 70002;
        SET @O_MENSAJE = ERROR_MESSAGE();
        THROW;
    END CATCH;
END;
