/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: SP_USUARIO_SESIONES_REVOKE
Archivo: SP_USUARIO_SESIONES_REVOKE.sql
Procedimiento: SEGURIDAD.SP_USUARIO_SESIONES_REVOKE
Tipo: COMMAND
Versión: 1.1.0
Fecha: 2026-08-20
ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi
Correo: agustincauzi10@hotmail.com
Descripción funcional: Revoca administrativamente todas las sesiones vigentes de un usuario.
Parámetros de entrada: Empresa y usuario objetivo.
Parámetros de sesión: @S_ID_USUARIO identifica al administrador responsable.
Parámetros de salida: Código, mensaje y filas afectadas.
Retorno: No retorna filas.
Tablas utilizadas: SEGURIDAD.USUARIOS - READ; SEGURIDAD.SESIONES - UPDATE.
Transacción: Una actualización atómica.
Auditoría: La DAL registra la acción y el administrador responsable.
Historial de modificaciones:
1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
1.1.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Autorrevocación y salidas de error catalogadas.
===============================================================================
*/
CREATE OR ALTER PROCEDURE [SEGURIDAD].[SP_USUARIO_SESIONES_REVOKE]
    @I_ID_EMPRESA      BIGINT,
    @I_ID_USUARIO      BIGINT,
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

    IF NOT EXISTS (SELECT 1 FROM [SEGURIDAD].[USUARIOS] WHERE [ID_USUARIO] = @I_ID_USUARIO AND [ID_EMPRESA] = @I_ID_EMPRESA)
    BEGIN
        SET @O_CODIGO_ERROR = 10003;
        EXEC [AUDITORIA].[SP_ERROR_MESSAGE_RESOLVE]
            @I_CODIGO_ERROR = @O_CODIGO_ERROR,
            @I_MENSAJE_PERSONALIZADO = N'El usuario no pertenece a la empresa.',
            @O_MENSAJE = @O_MENSAJE OUTPUT;
        THROW 50000, @O_MENSAJE, 1;
    END;

    BEGIN TRY
        -- INICIO: Revocación de sesiones vigentes del usuario validado.
        UPDATE [SEGURIDAD].[SESIONES]
        SET [CODIGO_ESTADO] = N'REVOCADA', [FECHA_CIERRE_UTC] = SYSUTCDATETIME(),
            [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(), [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO
        WHERE [ID_USUARIO] = @I_ID_USUARIO AND [CODIGO_ESTADO] = N'VIGENTE';
        SET @O_FILAS_AFECTADAS = @@ROWCOUNT;
        -- FIN: Revocación de sesiones vigentes del usuario validado.
    END TRY
    BEGIN CATCH
        SET @O_CODIGO_ERROR = COALESCE(@O_CODIGO_ERROR, CONVERT(BIGINT, 70002));
        SET @O_MENSAJE = COALESCE(@O_MENSAJE, ERROR_MESSAGE());
        THROW;
    END CATCH;
END;
