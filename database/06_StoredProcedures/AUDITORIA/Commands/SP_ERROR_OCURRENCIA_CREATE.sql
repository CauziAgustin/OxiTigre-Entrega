/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: SP_ERROR_OCURRENCIA_CREATE
Archivo: SP_ERROR_OCURRENCIA_CREATE.sql
Procedimiento: AUDITORIA.SP_ERROR_OCURRENCIA_CREATE
Tipo: COMMAND
Versión: 1.0.0
Fecha: 2026-08-20
ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi
Correo: agustincauzi10@hotmail.com
Descripción funcional: Registra una excepción técnica interceptada con código y correlación.
Parámetros de entrada: Código, componente, detalle técnico, origen y correlación.
Parámetros de sesión: IP y aplicación de origen.
Parámetros de salida: Ocurrencia creada, código, mensaje y filas afectadas.
Retorno: No retorna filas.
Tablas utilizadas: CATALOGO_ERRORES - READ; ERRORES_APLICACION - INSERT.
Transacción: Una inserción atómica.
Auditoría: Es el escritor de errores y no se audita a sí mismo.
Historial de modificaciones:
1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
CREATE OR ALTER PROCEDURE [AUDITORIA].[SP_ERROR_OCURRENCIA_CREATE]
    @I_CODIGO_ERROR           BIGINT,
    @I_COMPONENTE             NVARCHAR(200),
    @I_MENSAJE                NVARCHAR(4000),
    @I_DETALLE_TECNICO        NVARCHAR(MAX) = NULL,
    @I_STACK_TRACE            NVARCHAR(MAX) = NULL,
    @S_IP_ORIGEN              NVARCHAR(45) = NULL,
    @S_APLICACION_ORIGEN      NVARCHAR(100),
    @S_ID_CORRELACION         UNIQUEIDENTIFIER,
    @O_ID_ERROR_APLICACION    BIGINT OUTPUT,
    @O_CODIGO_ERROR           BIGINT OUTPUT,
    @O_MENSAJE                NVARCHAR(4000) OUTPUT,
    @O_FILAS_AFECTADAS        INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @O_ID_ERROR_APLICACION = NULL;
    SET @O_CODIGO_ERROR = NULL;
    SET @O_MENSAJE = NULL;
    SET @O_FILAS_AFECTADAS = 0;

    DECLARE @V_ID_CATALOGO_ERROR BIGINT =
        (SELECT [ID_CATALOGO_ERROR] FROM [AUDITORIA].[CATALOGO_ERRORES] WHERE [CODIGO_ERROR] = @I_CODIGO_ERROR AND [CODIGO_ESTADO] = N'ACTIVO');
    IF @V_ID_CATALOGO_ERROR IS NULL THROW 50000, 'El código de error no existe en el catálogo.', 1;

    -- INICIO: Registro técnico sin datos de entrada ni secretos.
    INSERT INTO [AUDITORIA].[ERRORES_APLICACION]
        ([ID_CATALOGO_ERROR], [CODIGO_ERROR], [COMPONENTE], [MENSAJE], [DETALLE_TECNICO], [STACK_TRACE],
         [IP_ORIGEN], [APLICACION_ORIGEN], [ID_CORRELACION], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
    VALUES
        (@V_ID_CATALOGO_ERROR, @I_CODIGO_ERROR, @I_COMPONENTE, @I_MENSAJE, @I_DETALLE_TECNICO,
         @I_STACK_TRACE, @S_IP_ORIGEN, @S_APLICACION_ORIGEN, @S_ID_CORRELACION, N'REGISTRADO', 0);
    SET @O_ID_ERROR_APLICACION = CONVERT(BIGINT, SCOPE_IDENTITY());
    SET @O_FILAS_AFECTADAS = 1;
    -- FIN: Registro técnico sin datos de entrada ni secretos.
END;
