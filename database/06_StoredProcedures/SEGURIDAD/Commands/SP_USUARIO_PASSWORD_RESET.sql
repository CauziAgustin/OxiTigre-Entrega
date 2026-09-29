/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: SP_USUARIO_PASSWORD_RESET
Archivo: SP_USUARIO_PASSWORD_RESET.sql
Procedimiento: SEGURIDAD.SP_USUARIO_PASSWORD_RESET
Tipo: COMMAND
Versión: 1.0.0
Fecha: 2026-08-20
ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi
Correo: agustincauzi10@hotmail.com
Descripción funcional: Reemplaza administrativamente una clave, exige cambio y revoca sesiones.
Parámetros de entrada: Empresa, usuario objetivo y credencial derivada.
Parámetros de sesión: @S_ID_USUARIO identifica al administrador responsable.
Parámetros de salida: Código, mensaje y filas afectadas.
Retorno: No retorna filas.
Tablas utilizadas: SEGURIDAD.USUARIOS y SEGURIDAD.SESIONES - UPDATE.
Transacción: Reinicio y revocación atómicos.
Auditoría: La DAL registra la acción sin hash, salt ni contraseña.
Historial de modificaciones:
1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
CREATE OR ALTER PROCEDURE [SEGURIDAD].[SP_USUARIO_PASSWORD_RESET]
    @I_ID_EMPRESA        BIGINT,
    @I_ID_USUARIO        BIGINT,
    @I_HASH_CLAVE        VARBINARY(64),
    @I_SALT_CLAVE        VARBINARY(32),
    @I_ALGORITMO_CLAVE   NVARCHAR(30),
    @I_ITERACIONES_CLAVE INT,
    @S_ID_USUARIO        BIGINT,
    @O_CODIGO_ERROR      BIGINT OUTPUT,
    @O_MENSAJE           NVARCHAR(4000) OUTPUT,
    @O_FILAS_AFECTADAS   INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @O_CODIGO_ERROR = NULL;
    SET @O_MENSAJE = NULL;
    SET @O_FILAS_AFECTADAS = 0;

    IF @I_ID_EMPRESA IS NULL OR @I_ID_USUARIO IS NULL OR @S_ID_USUARIO IS NULL
       OR @I_HASH_CLAVE IS NULL OR @I_SALT_CLAVE IS NULL OR @I_ITERACIONES_CLAVE < 100000
        THROW 50000, 'El usuario y la credencial derivada son obligatorios.', 1;

    BEGIN TRY
        BEGIN TRANSACTION;
        -- INICIO: Reemplazo de la credencial y limpieza de bloqueo.
        UPDATE [SEGURIDAD].[USUARIOS]
        SET [HASH_CLAVE] = @I_HASH_CLAVE, [SALT_CLAVE] = @I_SALT_CLAVE,
            [ALGORITMO_CLAVE] = @I_ALGORITMO_CLAVE, [ITERACIONES_CLAVE] = @I_ITERACIONES_CLAVE,
            [DEBE_CAMBIAR_CLAVE] = 1, [INTENTOS_FALLIDOS] = 0, [FECHA_BLOQUEO_UTC] = NULL,
            [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(), [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO
        WHERE [ID_USUARIO] = @I_ID_USUARIO AND [ID_EMPRESA] = @I_ID_EMPRESA;
        IF @@ROWCOUNT = 0 THROW 50001, 'El usuario no pertenece a la empresa.', 1;
        SET @O_FILAS_AFECTADAS = 1;
        -- FIN: Reemplazo de la credencial y limpieza de bloqueo.

        -- INICIO: Revocación de todas las sesiones vigentes anteriores.
        UPDATE [SEGURIDAD].[SESIONES]
        SET [CODIGO_ESTADO] = N'REVOCADA', [FECHA_CIERRE_UTC] = SYSUTCDATETIME(),
            [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(), [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO
        WHERE [ID_USUARIO] = @I_ID_USUARIO AND [CODIGO_ESTADO] = N'VIGENTE';
        SET @O_FILAS_AFECTADAS += @@ROWCOUNT;
        -- FIN: Revocación de todas las sesiones vigentes anteriores.
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        SET @O_MENSAJE = ERROR_MESSAGE();
        THROW;
    END CATCH;
END;
