/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: SP_CLIENTE_STATUS_CHANGE
Archivo: SP_CLIENTE_STATUS_CHANGE.sql
Procedimiento: COMERCIAL.SP_CLIENTE_STATUS_CHANGE
Tipo: COMMAND
Versión: 1.1.0
Fecha: 2026-08-20
ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi
Correo: agustincauzi10@hotmail.com
Descripción funcional: Activa o inactiva un cliente sin eliminar información ni teléfonos.
Parámetros de entrada: Empresa, cliente y estado de destino.
Parámetros de sesión: Sesión y usuario responsables.
Parámetros de salida: Código, mensaje y filas afectadas.
Retorno: No retorna filas.
Tablas utilizadas: CONFIGURACION.ESTADOS - READ; COMERCIAL.CLIENTES - UPDATE.
Transacción: Una actualización atómica.
Auditoría: La DAL registra el cambio de estado.
Historial de modificaciones:
1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
1.1.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Errores funcionales y técnicos controlados.
===============================================================================
*/
CREATE OR ALTER PROCEDURE [COMERCIAL].[SP_CLIENTE_STATUS_CHANGE]
    @I_ID_EMPRESA BIGINT, @I_ID_CLIENTE BIGINT, @I_CODIGO_ESTADO NVARCHAR(30),
    @S_ID_SESION BIGINT, @S_ID_USUARIO BIGINT,
    @O_CODIGO_ERROR BIGINT OUTPUT, @O_MENSAJE NVARCHAR(4000) OUTPUT, @O_FILAS_AFECTADAS INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @O_CODIGO_ERROR = NULL; SET @O_MENSAJE = NULL; SET @O_FILAS_AFECTADAS = 0;

    BEGIN TRY
        IF NOT EXISTS
        (
            SELECT 1 FROM [CONFIGURACION].[ESTADOS]
            WHERE [ENTIDAD] = N'CLIENTES' AND [CODIGO_ESTADO] = @I_CODIGO_ESTADO
              AND ([FECHA_VIGENCIA_HASTA_UTC] IS NULL OR [FECHA_VIGENCIA_HASTA_UTC] > SYSUTCDATETIME())
        )
        BEGIN
            SET @O_CODIGO_ERROR = 40001;
            EXEC [AUDITORIA].[SP_ERROR_MESSAGE_RESOLVE]
                @I_CODIGO_ERROR = @O_CODIGO_ERROR,
                @I_MENSAJE_PERSONALIZADO = N'El estado solicitado no está habilitado para CLIENTES.',
                @O_MENSAJE = @O_MENSAJE OUTPUT;
            THROW 50000, @O_MENSAJE, 1;
        END;

        UPDATE [COMERCIAL].[CLIENTES]
        SET [CODIGO_ESTADO] = @I_CODIGO_ESTADO, [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(),
            [ID_USUARIO_MODIFICACION] = @S_ID_USUARIO
        WHERE [ID_EMPRESA] = @I_ID_EMPRESA AND [ID_CLIENTE] = @I_ID_CLIENTE;

        SET @O_FILAS_AFECTADAS = @@ROWCOUNT;
        IF @O_FILAS_AFECTADAS = 0
        BEGIN
            SET @O_CODIGO_ERROR = 40002;
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
