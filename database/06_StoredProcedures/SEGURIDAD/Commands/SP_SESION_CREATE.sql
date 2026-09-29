/*
===============================================================================
Proyecto:              Sistema Modular de Gestión OxiTigre
Componente:            SP_SESION_CREATE
Archivo:               SP_SESION_CREATE.sql
Procedimiento:         SEGURIDAD.SP_SESION_CREATE
Tipo:                  COMMAND
Versión:               1.0.0
Fecha:                 2026-08-19
ID pedido:             FABRICA
Desarrollador:         Agustin Omar Cauzi
Correo:                agustincauzi10@hotmail.com
Descripción funcional: Registra una sesión usando únicamente el hash irreversible del token.
Parámetros de entrada: Usuario, sucursal opcional, hash, vigencia, origen, aplicación y correlación.
Parámetros de salida: ID de sesión, código, mensaje y filas afectadas.
Retorno: No retorna filas.
Tablas utilizadas: SEGURIDAD.SESIONES - INSERT.
Transacción: Inserción atómica con XACT_ABORT.
Auditoría: La sesión es la raíz de correlación de las ejecuciones posteriores.
Historial de modificaciones:
1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
CREATE OR ALTER PROCEDURE [SEGURIDAD].[SP_SESION_CREATE]
    @I_ID_USUARIO          BIGINT,
    @I_ID_SUCURSAL         BIGINT = NULL,
    @I_HASH_TOKEN          VARBINARY(64),
    @I_FECHA_EXPIRACION_UTC DATETIME2(3),
    @S_IP_ORIGEN           NVARCHAR(45),
    @S_APLICACION_ORIGEN   NVARCHAR(100),
    @S_ID_CORRELACION      UNIQUEIDENTIFIER,
    @O_ID_SESION           BIGINT OUTPUT,
    @O_CODIGO_ERROR        BIGINT OUTPUT,
    @O_MENSAJE             NVARCHAR(4000) OUTPUT,
    @O_FILAS_AFECTADAS     INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @O_ID_SESION = NULL;
    SET @O_CODIGO_ERROR = NULL;
    SET @O_MENSAJE = NULL;
    SET @O_FILAS_AFECTADAS = 0;

    -- INICIO: Validación de la sesión solicitada.
    IF @I_ID_USUARIO IS NULL OR @I_HASH_TOKEN IS NULL OR @I_FECHA_EXPIRACION_UTC <= SYSUTCDATETIME()
       OR @S_ID_CORRELACION IS NULL OR NULLIF(LTRIM(RTRIM(@S_APLICACION_ORIGEN)), N'') IS NULL
        THROW 50000, 'Los datos obligatorios de la sesión no son válidos.', 1;

    IF @I_ID_SUCURSAL IS NOT NULL
       AND NOT EXISTS
       (
           SELECT 1
           FROM [CONFIGURACION].[SUCURSALES] AS [SUC]
           INNER JOIN [SEGURIDAD].[USUARIOS] AS [USR]
               ON [USR].[ID_EMPRESA] = [SUC].[ID_EMPRESA]
           WHERE [USR].[ID_USUARIO] = @I_ID_USUARIO
             AND [SUC].[ID_SUCURSAL] = @I_ID_SUCURSAL
             AND [SUC].[CODIGO_ESTADO] = N'ACTIVO'
       )
        THROW 50000, 'La sucursal no pertenece a la empresa autenticada o está inactiva.', 1;
    -- FIN: Validación de la sesión solicitada.

    BEGIN TRY
        -- INICIO: Registro de la sesión autenticada.
        INSERT INTO [SEGURIDAD].[SESIONES]
        (
            [ID_USUARIO], [ID_SUCURSAL], [HASH_TOKEN], [FECHA_INICIO_UTC], [FECHA_EXPIRACION_UTC],
            [IP_ORIGEN], [APLICACION_ORIGEN], [ID_CORRELACION], [CODIGO_ESTADO], [ID_USUARIO_ALTA]
        )
        VALUES
        (
            @I_ID_USUARIO, @I_ID_SUCURSAL, @I_HASH_TOKEN, SYSUTCDATETIME(), @I_FECHA_EXPIRACION_UTC,
            @S_IP_ORIGEN, @S_APLICACION_ORIGEN, @S_ID_CORRELACION, N'VIGENTE', @I_ID_USUARIO
        );
        SET @O_ID_SESION = CONVERT(BIGINT, SCOPE_IDENTITY());
        SET @O_FILAS_AFECTADAS = @@ROWCOUNT;
        -- FIN: Registro de la sesión autenticada.
    END TRY
    BEGIN CATCH
        SET @O_CODIGO_ERROR = CONVERT(BIGINT, ERROR_NUMBER());
        SET @O_MENSAJE = ERROR_MESSAGE();
        THROW;
    END CATCH;
END;
