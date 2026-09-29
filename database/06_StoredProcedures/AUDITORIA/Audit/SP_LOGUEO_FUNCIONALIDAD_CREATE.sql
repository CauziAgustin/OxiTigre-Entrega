/*
===============================================================================
Proyecto:              Sistema Modular de Gestión OxiTigre
Componente:            SP_LOGUEO_FUNCIONALIDAD_CREATE
Archivo:               SP_LOGUEO_FUNCIONALIDAD_CREATE.sql
Procedimiento:         AUDITORIA.SP_LOGUEO_FUNCIONALIDAD_CREATE
Tipo:                  AUDIT
Versión:               1.0.0
Fecha:                 2026-08-19
ID pedido:             FABRICA
Desarrollador:         Agustin Omar Cauzi
Correo:                agustincauzi10@hotmail.com
Descripción funcional: Registra de manera central una ejecución de Stored Procedure.
Parámetros de entrada: Contexto funcional, SP, tiempos, resultado y datos redactados.
Parámetros de salida: ID de auditoría, código y mensaje controlados.
Retorno: No retorna filas.
Tablas utilizadas: CONFIGURACION.EMPRESAS, MODULOS - READ; AUDITORIA.LOGUEO_FUNCIONALIDADES - INSERT.
Transacción: Inserción atómica independiente de la operación observada.
Auditoría: Este SP es el escritor central y no se audita a sí mismo para evitar recursión.
Historial de modificaciones:
1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
CREATE OR ALTER PROCEDURE [AUDITORIA].[SP_LOGUEO_FUNCIONALIDAD_CREATE]
    @I_CODIGO_EMPRESA             NVARCHAR(30),
    @I_CODIGO_MODULO              NVARCHAR(30),
    @I_ID_USUARIO                 BIGINT = NULL,
    @I_ID_SESION                  BIGINT = NULL,
    @I_FUNCIONALIDAD              NVARCHAR(200),
    @I_ACCION                     NVARCHAR(100),
    @I_SCHEMA_SP                  NVARCHAR(128),
    @I_NOMBRE_SP                  NVARCHAR(128),
    @I_PARAMETROS_REDACTADOS_JSON NVARCHAR(MAX) = NULL,
    @I_FECHA_INICIO_UTC           DATETIME2(3),
    @I_FECHA_FIN_UTC              DATETIME2(3),
    @I_FILAS_AFECTADAS            INT = NULL,
    @I_RESULTADO                  NVARCHAR(30),
    @I_CODIGO_ERROR               BIGINT = NULL,
    @I_MENSAJE_ERROR              NVARCHAR(4000) = NULL,
    @S_IP_ORIGEN                  NVARCHAR(45) = NULL,
    @S_APLICACION_ORIGEN          NVARCHAR(100),
    @S_ID_CORRELACION             UNIQUEIDENTIFIER,
    @O_ID_LOGUEO_FUNCIONALIDAD    BIGINT OUTPUT,
    @O_CODIGO_ERROR               BIGINT OUTPUT,
    @O_MENSAJE                    NVARCHAR(4000) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @O_ID_LOGUEO_FUNCIONALIDAD = NULL;
    SET @O_CODIGO_ERROR = NULL;
    SET @O_MENSAJE = NULL;

    DECLARE @V_ID_EMPRESA BIGINT;
    DECLARE @V_ID_MODULO BIGINT;
    SELECT @V_ID_EMPRESA = [ID_EMPRESA] FROM [CONFIGURACION].[EMPRESAS] WHERE [CODIGO] = @I_CODIGO_EMPRESA;
    SELECT @V_ID_MODULO = [ID_MODULO] FROM [CONFIGURACION].[MODULOS] WHERE [CODIGO] = @I_CODIGO_MODULO;

    -- INICIO: Validación de referencias informativas de auditoría.
    IF @V_ID_EMPRESA IS NULL OR @V_ID_MODULO IS NULL
        THROW 50000, 'La empresa o el módulo de auditoría no existen.', 1;
    -- FIN: Validación de referencias informativas de auditoría.

    -- INICIO: Registro central de la ejecución funcional.
    INSERT INTO [AUDITORIA].[LOGUEO_FUNCIONALIDADES]
    (
        [ID_EMPRESA], [ID_MODULO], [ID_USUARIO], [ID_SESION], [FUNCIONALIDAD], [ACCION],
        [SCHEMA_SP], [NOMBRE_SP], [PARAMETROS_REDACTADOS_JSON], [FECHA_INICIO_UTC],
        [FECHA_FIN_UTC], [DURACION_MS], [FILAS_AFECTADAS], [RESULTADO], [CODIGO_ERROR],
        [MENSAJE_ERROR], [IP_ORIGEN], [APLICACION_ORIGEN], [ID_CORRELACION],
        [CODIGO_ESTADO], [ID_USUARIO_ALTA]
    )
    VALUES
    (
        @V_ID_EMPRESA, @V_ID_MODULO, @I_ID_USUARIO, @I_ID_SESION, @I_FUNCIONALIDAD, @I_ACCION,
        @I_SCHEMA_SP, @I_NOMBRE_SP, @I_PARAMETROS_REDACTADOS_JSON, @I_FECHA_INICIO_UTC,
        @I_FECHA_FIN_UTC, DATEDIFF_BIG(MILLISECOND, @I_FECHA_INICIO_UTC, @I_FECHA_FIN_UTC),
        @I_FILAS_AFECTADAS, @I_RESULTADO, @I_CODIGO_ERROR, @I_MENSAJE_ERROR, @S_IP_ORIGEN,
        @S_APLICACION_ORIGEN, @S_ID_CORRELACION, N'REGISTRADO', ISNULL(@I_ID_USUARIO, 1)
    );
    SET @O_ID_LOGUEO_FUNCIONALIDAD = CONVERT(BIGINT, SCOPE_IDENTITY());
    -- FIN: Registro central de la ejecución funcional.
END;
