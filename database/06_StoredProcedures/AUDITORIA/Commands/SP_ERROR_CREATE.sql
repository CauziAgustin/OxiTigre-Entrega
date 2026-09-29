/*
===============================================================================
Proyecto:              Sistema Modular de Gestión OxiTigre
Componente:            SP_ERROR_CREATE
Archivo:               SP_ERROR_CREATE.sql
Procedimiento:         AUDITORIA.SP_ERROR_CREATE
Tipo:                  COMMAND
Versión:               1.0.0
Fecha:                 2026-08-19
ID pedido:             FABRICA
Desarrollador:         Agustin Omar Cauzi
Correo:                agustincauzi10@hotmail.com

Descripción funcional:
Registra un error y genera el próximo código como NUMERO_MODULO * 10000 más
un correlativo de cuatro posiciones, sin ceros agregados delante del módulo.

Parámetros de entrada:
@I_ID_MODULO BIGINT - Módulo propietario del error.
@I_NOMBRE, @I_DESCRIPCION, @I_CAUSA_PROBABLE, @I_ACCION_RECOMENDADA - Diagnóstico.
@I_SEVERIDAD NVARCHAR(30) - INFORMATIVO, ADVERTENCIA, ERROR o CRITICO.
@S_ID_USUARIO BIGINT - Usuario funcional responsable.

Parámetros de salida:
@O_CODIGO_GENERADO BIGINT OUTPUT - Código funcional nuevo.
@O_CODIGO_ERROR BIGINT OUTPUT - NULL si finalizó correctamente.
@O_MENSAJE NVARCHAR(4000) OUTPUT - Detalle controlado.
@O_FILAS_AFECTADAS INT OUTPUT - Registros insertados.

Retorno: No retorna filas.
Tablas utilizadas: CONFIGURACION.MODULOS - READ; AUDITORIA.CATALOGO_ERRORES - INSERT.
Transacción: SERIALIZABLE para impedir correlativos duplicados por concurrencia.
Auditoría: El ejecutor DAL registra la operación y su correlación.

Historial de modificaciones:
1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
CREATE OR ALTER PROCEDURE [AUDITORIA].[SP_ERROR_CREATE]
    @I_ID_MODULO             BIGINT,
    @I_NOMBRE                NVARCHAR(200),
    @I_DESCRIPCION           NVARCHAR(1000),
    @I_CAUSA_PROBABLE        NVARCHAR(1000) = NULL,
    @I_ACCION_RECOMENDADA    NVARCHAR(2000) = NULL,
    @I_SEVERIDAD             NVARCHAR(30),
    @S_ID_USUARIO            BIGINT,
    @O_CODIGO_GENERADO       BIGINT OUTPUT,
    @O_CODIGO_ERROR          BIGINT OUTPUT,
    @O_MENSAJE               NVARCHAR(4000) OUTPUT,
    @O_FILAS_AFECTADAS       INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @V_NUMERO_MODULO SMALLINT;
    DECLARE @V_NUMERO_ERROR SMALLINT;

    -- INICIO: Inicialización de parámetros de salida.
    SET @O_CODIGO_GENERADO = NULL;
    SET @O_CODIGO_ERROR = NULL;
    SET @O_MENSAJE = NULL;
    SET @O_FILAS_AFECTADAS = 0;
    -- FIN: Inicialización de parámetros de salida.

    -- INICIO: Validación de datos obligatorios.
    IF @I_ID_MODULO IS NULL OR NULLIF(LTRIM(RTRIM(@I_NOMBRE)), N'') IS NULL
       OR NULLIF(LTRIM(RTRIM(@I_DESCRIPCION)), N'') IS NULL OR @S_ID_USUARIO IS NULL
    BEGIN
        THROW 50000, 'Módulo, nombre, descripción y usuario son obligatorios.', 1;
    END; -- FIN: Validación de datos obligatorios.

    BEGIN TRY
        SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
        BEGIN TRANSACTION;

        -- INICIO: Bloqueo del correlativo y obtención del siguiente número del módulo.
        SELECT @V_NUMERO_MODULO = [NUMERO_MODULO]
        FROM [CONFIGURACION].[MODULOS] WITH (UPDLOCK, HOLDLOCK)
        WHERE [ID_MODULO] = @I_ID_MODULO AND [CODIGO_ESTADO] = N'ACTIVO';

        IF @V_NUMERO_MODULO IS NULL
        BEGIN
            THROW 50001, 'El módulo informado no existe o no está activo.', 1;
        END;

        SELECT @V_NUMERO_ERROR = CONVERT(SMALLINT, ISNULL(MAX([NUMERO_ERROR]), 0) + 1)
        FROM [AUDITORIA].[CATALOGO_ERRORES] WITH (UPDLOCK, HOLDLOCK)
        WHERE [ID_MODULO] = @I_ID_MODULO;

        IF @V_NUMERO_ERROR > 9999
        BEGIN
            THROW 50002, 'El módulo agotó los 9999 códigos de error disponibles.', 1;
        END;
        -- FIN: Bloqueo del correlativo y obtención del siguiente número del módulo.

        -- INICIO: Construcción e inserción del código funcional.
        SET @O_CODIGO_GENERADO = CONVERT(BIGINT, @V_NUMERO_MODULO) * 10000 + @V_NUMERO_ERROR;

        INSERT INTO [AUDITORIA].[CATALOGO_ERRORES]
        (
            [ID_MODULO], [NUMERO_ERROR], [CODIGO_ERROR], [NOMBRE], [DESCRIPCION],
            [CAUSA_PROBABLE], [ACCION_RECOMENDADA], [SEVERIDAD], [CODIGO_ESTADO], [ID_USUARIO_ALTA]
        )
        VALUES
        (
            @I_ID_MODULO, @V_NUMERO_ERROR, @O_CODIGO_GENERADO, @I_NOMBRE, @I_DESCRIPCION,
            @I_CAUSA_PROBABLE, @I_ACCION_RECOMENDADA, @I_SEVERIDAD, N'ACTIVO', @S_ID_USUARIO
        );

        SET @O_FILAS_AFECTADAS = @@ROWCOUNT;
        COMMIT TRANSACTION;
        -- FIN: Construcción e inserción del código funcional.
    END TRY
    BEGIN CATCH
        -- INICIO: Reversión y propagación controlada del error.
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        SET @O_CODIGO_ERROR = CONVERT(BIGINT, ERROR_NUMBER());
        SET @O_MENSAJE = ERROR_MESSAGE();
        THROW;
        -- FIN: Reversión y propagación controlada del error.
    END CATCH;
END;
