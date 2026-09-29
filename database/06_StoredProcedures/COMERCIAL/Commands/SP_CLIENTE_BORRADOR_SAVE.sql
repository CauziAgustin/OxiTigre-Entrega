/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: SP_CLIENTE_BORRADOR_SAVE
Archivo: SP_CLIENTE_BORRADOR_SAVE.sql
Procedimiento: COMERCIAL.SP_CLIENTE_BORRADOR_SAVE
Tipo: COMMAND
Versión: 1.0.0
Fecha: 2026-08-20
ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi
Correo: agustincauzi10@hotmail.com
Descripción funcional: Crea o reemplaza la precarga incompleta del usuario.
Parámetros de entrada: Empresa y contenido JSON parcial.
Parámetros de sesión: Sesión y usuario responsables.
Parámetros de salida: Código, mensaje y filas afectadas.
Retorno: No retorna filas.
Tablas utilizadas: COMERCIAL.CLIENTES_BORRADORES - INSERT/UPDATE.
Transacción: Upsert serializable para mantener un borrador activo.
Auditoría: La DAL registra la acción sin copiar datos personales al log.
Historial de modificaciones:
1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
CREATE OR ALTER PROCEDURE [COMERCIAL].[SP_CLIENTE_BORRADOR_SAVE]
    @I_ID_EMPRESA      BIGINT,
    @I_CONTENIDO_JSON  NVARCHAR(MAX),
    @S_ID_SESION       BIGINT,
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
    IF @I_ID_EMPRESA IS NULL OR @S_ID_SESION IS NULL OR @S_ID_USUARIO IS NULL OR ISJSON(@I_CONTENIDO_JSON) <> 1
        THROW 50000, 'Empresa, sesión, usuario y contenido JSON válido son obligatorios.', 1;

    BEGIN TRY
        SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
        BEGIN TRANSACTION;
        -- INICIO: Actualización del borrador activo existente.
        UPDATE [COMERCIAL].[CLIENTES_BORRADORES] WITH (UPDLOCK, HOLDLOCK)
        SET [CONTENIDO_JSON] = @I_CONTENIDO_JSON, [FECHA_ULTIMO_GUARDADO_UTC] = SYSUTCDATETIME(),
            [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(), [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO
        WHERE [ID_EMPRESA] = @I_ID_EMPRESA AND [ID_USUARIO] = @S_ID_USUARIO AND [CODIGO_ESTADO] = N'BORRADOR';
        SET @O_FILAS_AFECTADAS = @@ROWCOUNT;
        -- FIN: Actualización del borrador activo existente.

        -- INICIO: Alta cuando el usuario todavía no posee borrador activo.
        IF @O_FILAS_AFECTADAS = 0
        BEGIN
            INSERT INTO [COMERCIAL].[CLIENTES_BORRADORES]
                ([ID_EMPRESA], [ID_USUARIO], [CONTENIDO_JSON], [FECHA_ULTIMO_GUARDADO_UTC], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
            VALUES (@I_ID_EMPRESA, @S_ID_USUARIO, @I_CONTENIDO_JSON, SYSUTCDATETIME(), N'BORRADOR', @S_ID_USUARIO);
            SET @O_FILAS_AFECTADAS = 1;
        END;
        -- FIN: Alta cuando el usuario todavía no posee borrador activo.
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        SET @O_MENSAJE = ERROR_MESSAGE();
        THROW;
    END CATCH;
END;
