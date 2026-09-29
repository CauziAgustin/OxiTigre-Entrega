/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: SP_USUARIO_PREFERENCIA_SAVE
Procedimiento: CONFIGURACION.SP_USUARIO_PREFERENCIA_SAVE
Tipo:                  COMMAND
Archivo: SP_USUARIO_PREFERENCIA_SAVE.sql | Versión: 1.1.1 | Fecha: 2026-09-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Guarda el idioma de presentación elegido por el usuario autenticado.
Parámetros de entrada: Código canónico de una cultura específica validada por la API.
Parámetros de sesión: Usuario propietario y responsable.
Parámetros de salida: Código, mensaje y filas afectadas.
Retorno: No retorna filas.
Tablas utilizadas: CONFIGURACION.USUARIOS_PREFERENCIAS - INSERT/UPDATE.
Transacción: Upsert atómico por usuario.
Auditoría: No registra valores sensibles; la cultura no es confidencial.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-09-20 | FABRICA | Agustin Omar Cauzi | Preferencias para idiomas importables.
Historial: 1.1.1 | 2026-09-20 | FABRICA | Agustin Omar Cauzi | Documentación técnica completa del procedimiento.
===============================================================================
*/
CREATE OR ALTER PROCEDURE [CONFIGURACION].[SP_USUARIO_PREFERENCIA_SAVE]
    @I_CULTURA NVARCHAR(10),
    @S_ID_USUARIO BIGINT,
    @O_CODIGO_ERROR BIGINT OUTPUT,
    @O_MENSAJE NVARCHAR(4000) OUTPUT,
    @O_FILAS_AFECTADAS INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    SET @O_CODIGO_ERROR = NULL; SET @O_MENSAJE = NULL; SET @O_FILAS_AFECTADAS = 0;

    IF LEN(@I_CULTURA) NOT BETWEEN 4 AND 10
       OR CHARINDEX(N'-', @I_CULTURA) NOT BETWEEN 2 AND LEN(@I_CULTURA) - 1
       OR @I_CULTURA LIKE N'% %'
    BEGIN
        SET @O_CODIGO_ERROR = 20001;
        EXEC [AUDITORIA].[SP_ERROR_MESSAGE_RESOLVE] @I_CODIGO_ERROR = @O_CODIGO_ERROR,
            @I_MENSAJE_PERSONALIZADO = N'La cultura específica no tiene un formato válido.', @O_MENSAJE = @O_MENSAJE OUTPUT;
        RETURN;
    END;

    BEGIN TRY
        UPDATE [CONFIGURACION].[USUARIOS_PREFERENCIAS]
        SET [CULTURA] = @I_CULTURA, [CODIGO_ESTADO] = N'ACTIVO', [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(),
            [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO
        WHERE [ID_USUARIO] = @S_ID_USUARIO;
        SET @O_FILAS_AFECTADAS = @@ROWCOUNT;
        IF @O_FILAS_AFECTADAS = 0
        BEGIN
            INSERT INTO [CONFIGURACION].[USUARIOS_PREFERENCIAS]
                ([ID_USUARIO], [CULTURA], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
            VALUES (@S_ID_USUARIO, @I_CULTURA, N'ACTIVO', @S_ID_USUARIO);
            SET @O_FILAS_AFECTADAS = @@ROWCOUNT;
        END;
    END TRY
    BEGIN CATCH
        SET @O_CODIGO_ERROR = 70002; SET @O_MENSAJE = ERROR_MESSAGE(); THROW;
    END CATCH;
END;
