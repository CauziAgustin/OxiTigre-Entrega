/*
===============================================================================
Proyecto:              Sistema Modular de Gestión OxiTigre
Componente:            SP_USUARIO_PASSWORD_CHANGE
Archivo:               SP_USUARIO_PASSWORD_CHANGE.sql
Procedimiento:         SEGURIDAD.SP_USUARIO_PASSWORD_CHANGE
Tipo:                  COMMAND
Versión:               1.0.0
Fecha:                 2026-08-19
ID pedido:             FABRICA
Desarrollador:         Agustin Omar Cauzi
Correo:                agustincauzi10@hotmail.com
Descripción funcional: Sustituye la credencial derivada y revoca sesiones vigentes del usuario.
Parámetros de entrada: Usuario, hash, salt, algoritmo e iteraciones nuevos.
Parámetros de salida: Código, mensaje y filas afectadas.
Retorno: No retorna filas.
Tablas utilizadas: SEGURIDAD.USUARIOS y SEGURIDAD.SESIONES - UPDATE.
Transacción: Actualización conjunta con rollback automático.
Auditoría: La siguiente autenticación crea una nueva sesión correlacionada.
Historial de modificaciones:
1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
CREATE OR ALTER PROCEDURE [SEGURIDAD].[SP_USUARIO_PASSWORD_CHANGE]
    @I_ID_USUARIO       BIGINT,
    @I_HASH_CLAVE       VARBINARY(64),
    @I_SALT_CLAVE       VARBINARY(32),
    @I_ALGORITMO_CLAVE  NVARCHAR(30),
    @I_ITERACIONES_CLAVE INT,
    @O_CODIGO_ERROR     BIGINT OUTPUT,
    @O_MENSAJE          NVARCHAR(4000) OUTPUT,
    @O_FILAS_AFECTADAS  INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @O_CODIGO_ERROR = NULL;
    SET @O_MENSAJE = NULL;
    SET @O_FILAS_AFECTADAS = 0;

    BEGIN TRY
        BEGIN TRANSACTION;
        -- INICIO: Reemplazo de la credencial y desactivación del cambio obligatorio.
        UPDATE [SEGURIDAD].[USUARIOS]
        SET [HASH_CLAVE] = @I_HASH_CLAVE, [SALT_CLAVE] = @I_SALT_CLAVE,
            [ALGORITMO_CLAVE] = @I_ALGORITMO_CLAVE, [ITERACIONES_CLAVE] = @I_ITERACIONES_CLAVE,
            [DEBE_CAMBIAR_CLAVE] = 0, [INTENTOS_FALLIDOS] = 0,
            [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(), [ID_USUARIO_MODIFICACION] = @I_ID_USUARIO
        WHERE [ID_USUARIO] = @I_ID_USUARIO AND [CODIGO_ESTADO] = N'ACTIVO';
        SET @O_FILAS_AFECTADAS = @@ROWCOUNT;
        IF @O_FILAS_AFECTADAS = 0 THROW 50001, 'El usuario no existe o no está activo.', 1;
        -- FIN: Reemplazo de la credencial y desactivación del cambio obligatorio.

        -- INICIO: Revocación de sesiones anteriores para exigir una nueva autenticación.
        UPDATE [SEGURIDAD].[SESIONES]
        SET [CODIGO_ESTADO] = N'REVOCADA', [FECHA_CIERRE_UTC] = SYSUTCDATETIME(),
            [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(), [ID_USUARIO_MODIFICACION] = @I_ID_USUARIO
        WHERE [ID_USUARIO] = @I_ID_USUARIO AND [CODIGO_ESTADO] = N'VIGENTE';
        -- FIN: Revocación de sesiones anteriores para exigir una nueva autenticación.
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        SET @O_CODIGO_ERROR = CONVERT(BIGINT, ERROR_NUMBER());
        SET @O_MENSAJE = ERROR_MESSAGE();
        THROW;
    END CATCH;
END;
