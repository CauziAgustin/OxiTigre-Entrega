/*
===============================================================================
Proyecto:              Sistema Modular de Gestión OxiTigre
Componente:            SP_EMPRESA_BASE_SAVE
Archivo:               SP_EMPRESA_BASE_SAVE.sql
Procedimiento:         PLATAFORMA.SP_EMPRESA_BASE_SAVE
Tipo:                  COMMAND
Versión:               1.0.0
Fecha:                 2026-08-27
ID pedido:             FABRICA
Desarrollador:         Agustin Omar Cauzi
Correo:                agustincauzi10@hotmail.com
Descripción funcional: Registra o actualiza el destino técnico de una empresa sin almacenar contraseñas.
Parámetros de entrada: Código, nombre, servidor, base, empresa local, versión y opciones de cifrado.
Parámetros de salida: No aplica.
Retorno: No devuelve conjuntos de resultados.
Tablas utilizadas: PLATAFORMA.EMPRESAS_BASES - INSERT/UPDATE.
Transacción: Operación atómica individual.
Auditoría: Conserva fecha de alta o modificación y rowversion.
Historial de modificaciones:
1.0.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
CREATE OR ALTER PROCEDURE [PLATAFORMA].[SP_EMPRESA_BASE_SAVE]
    @I_CODIGO              NVARCHAR(30),
    @I_RAZON_SOCIAL        NVARCHAR(200),
    @I_SERVIDOR_SQL        NVARCHAR(200),
    @I_BASE_DATOS          SYSNAME,
    @I_ID_EMPRESA_LOCAL    BIGINT,
    @I_VERSION_ESQUEMA     NVARCHAR(30),
    @I_CIFRAR_CONEXION     BIT,
    @I_CONFIAR_CERTIFICADO BIT,
    @I_SECRETO_REFERENCIA  NVARCHAR(300) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NULLIF(TRIM(@I_CODIGO), N'') IS NULL
       OR NULLIF(TRIM(@I_RAZON_SOCIAL), N'') IS NULL
       OR NULLIF(TRIM(@I_SERVIDOR_SQL), N'') IS NULL
       OR NULLIF(TRIM(@I_BASE_DATOS), N'') IS NULL
       OR @I_ID_EMPRESA_LOCAL <= 0
       OR NULLIF(TRIM(@I_VERSION_ESQUEMA), N'') IS NULL
        THROW 50000, 'La configuración de empresa y base está incompleta.', 1;

    -- INICIO: Alta o actualización idempotente del registro técnico.
    UPDATE [PLATAFORMA].[EMPRESAS_BASES]
    SET [RAZON_SOCIAL] = TRIM(@I_RAZON_SOCIAL),
        [SERVIDOR_SQL] = TRIM(@I_SERVIDOR_SQL),
        [BASE_DATOS] = TRIM(@I_BASE_DATOS),
        [ID_EMPRESA_LOCAL] = @I_ID_EMPRESA_LOCAL,
        [VERSION_ESQUEMA] = TRIM(@I_VERSION_ESQUEMA),
        [CIFRAR_CONEXION] = @I_CIFRAR_CONEXION,
        [CONFIAR_CERTIFICADO] = @I_CONFIAR_CERTIFICADO,
        [SECRETO_REFERENCIA] = NULLIF(TRIM(@I_SECRETO_REFERENCIA), N''),
        [CODIGO_ESTADO] = N'ACTIVO',
        [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME()
    WHERE [CODIGO] = UPPER(TRIM(@I_CODIGO));

    IF @@ROWCOUNT = 0
    BEGIN
        INSERT INTO [PLATAFORMA].[EMPRESAS_BASES]
            ([CODIGO], [RAZON_SOCIAL], [SERVIDOR_SQL], [BASE_DATOS], [ID_EMPRESA_LOCAL],
             [VERSION_ESQUEMA], [CIFRAR_CONEXION], [CONFIAR_CERTIFICADO], [SECRETO_REFERENCIA])
        VALUES
            (UPPER(TRIM(@I_CODIGO)), TRIM(@I_RAZON_SOCIAL), TRIM(@I_SERVIDOR_SQL), TRIM(@I_BASE_DATOS),
             @I_ID_EMPRESA_LOCAL, TRIM(@I_VERSION_ESQUEMA), @I_CIFRAR_CONEXION,
             @I_CONFIAR_CERTIFICADO, NULLIF(TRIM(@I_SECRETO_REFERENCIA), N''));
    END;
    -- FIN: Alta o actualización idempotente del registro técnico.
END;
