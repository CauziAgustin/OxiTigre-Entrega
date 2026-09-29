/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: SP_CLIENTE_BORRADOR_DELETE
Archivo: SP_CLIENTE_BORRADOR_DELETE.sql
Procedimiento: COMERCIAL.SP_CLIENTE_BORRADOR_DELETE
Tipo: COMMAND
Versión: 1.1.0
Fecha: 2026-08-20
ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi
Correo: agustincauzi10@hotmail.com
Descripción funcional: Descarta lógicamente la precarga incompleta del usuario.
Parámetros de entrada: Empresa.
Parámetros de sesión: Sesión y usuario responsables.
Parámetros de salida: Código, mensaje y filas afectadas.
Retorno: No retorna filas.
Tablas utilizadas: COMERCIAL.CLIENTES_BORRADORES - UPDATE.
Transacción: Una actualización atómica.
Auditoría: La DAL registra el descarte funcional.
Historial de modificaciones:
1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
1.1.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Captura de fallos técnicos del descarte idempotente.
===============================================================================
*/
CREATE OR ALTER PROCEDURE [COMERCIAL].[SP_CLIENTE_BORRADOR_DELETE]
    @I_ID_EMPRESA      BIGINT,
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

    BEGIN TRY
        -- INICIO: Descarte del borrador activo propio.
        UPDATE [COMERCIAL].[CLIENTES_BORRADORES]
        SET [CODIGO_ESTADO] = N'DESCARTADO', [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(),
            [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO
        WHERE [ID_EMPRESA] = @I_ID_EMPRESA AND [ID_USUARIO] = @S_ID_USUARIO AND [CODIGO_ESTADO] = N'BORRADOR';
        SET @O_FILAS_AFECTADAS = @@ROWCOUNT;
        -- FIN: Descarte del borrador activo propio.
    END TRY
    BEGIN CATCH
        SET @O_CODIGO_ERROR = 70002;
        SET @O_MENSAJE = ERROR_MESSAGE();
        THROW;
    END CATCH;
END;
