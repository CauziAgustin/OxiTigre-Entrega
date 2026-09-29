/*
===============================================================================
Proyecto:              Sistema Modular de Gestión OxiTigre
Componente:            SP_USUARIO_EMPRESA_AUTH_LIST
Archivo:               SP_USUARIO_EMPRESA_AUTH_LIST.sql
Procedimiento:         SEGURIDAD.SP_USUARIO_EMPRESA_AUTH_LIST
Tipo:                  QUERY
Versión:               1.2.0
Fecha:                 2026-08-20
ID pedido:             FABRICA
Desarrollador:         Agustin Omar Cauzi
Correo:                agustincauzi10@hotmail.com
Descripción funcional: Obtiene cuentas activas para validar la clave antes de seleccionar empresa.
Parámetros de entrada: @I_NOMBRE_USUARIO identifica al usuario de aplicación.
Parámetros de salida: @O_CODIGO_ERROR y @O_MENSAJE informan el resultado controlado.
Retorno: Cuenta validable y un segundo resultado con las sucursales activas de la empresa.
Tablas utilizadas: CONFIGURACION.EMPRESAS, CONFIGURACION.SUCURSALES y SEGURIDAD.USUARIOS - READ.
Transacción: No aplica; solo lectura.
Auditoría: La API valida la clave sin registrarla ni exponerla.
Historial de modificaciones:
1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
1.1.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Exposición controlada del estado de bloqueo.
1.2.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Resultado controlado cuando no existen empresas autorizadas.
===============================================================================
*/
CREATE OR ALTER PROCEDURE [SEGURIDAD].[SP_USUARIO_EMPRESA_AUTH_LIST]
    @I_NOMBRE_USUARIO NVARCHAR(100),
    @O_CODIGO_ERROR   BIGINT OUTPUT,
    @O_MENSAJE        NVARCHAR(4000) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @O_CODIGO_ERROR = NULL;
    SET @O_MENSAJE = NULL;

    -- INICIO: Consulta de empresas activas y credenciales derivadas del usuario.
    SELECT [EMP].[CODIGO] AS [CODIGO_EMPRESA], [EMP].[RAZON_SOCIAL] AS [NOMBRE_EMPRESA],
           [USR].[ID_USUARIO], [USR].[ID_EMPRESA], [USR].[NOMBRE_USUARIO], [USR].[NOMBRES], [USR].[APELLIDO],
           [USR].[EMAIL], [USR].[HASH_CLAVE], [USR].[SALT_CLAVE], [USR].[ALGORITMO_CLAVE],
           [USR].[ITERACIONES_CLAVE], [USR].[DEBE_CAMBIAR_CLAVE], [USR].[INTENTOS_FALLIDOS],
           [USR].[FECHA_BLOQUEO_UTC]
    FROM [SEGURIDAD].[USUARIOS] AS [USR]
    INNER JOIN [CONFIGURACION].[EMPRESAS] AS [EMP] ON [EMP].[ID_EMPRESA] = [USR].[ID_EMPRESA]
    WHERE [USR].[NOMBRE_USUARIO] = @I_NOMBRE_USUARIO
      AND [USR].[CODIGO_ESTADO] = N'ACTIVO'
      AND [EMP].[CODIGO_ESTADO] = N'ACTIVO'
    ORDER BY [EMP].[RAZON_SOCIAL];
    -- FIN: Consulta de empresas activas y credenciales derivadas del usuario.

    IF @@ROWCOUNT = 0
    BEGIN
        SET @O_CODIGO_ERROR = 10001;
        EXEC [AUDITORIA].[SP_ERROR_MESSAGE_RESOLVE]
            @I_CODIGO_ERROR = @O_CODIGO_ERROR, @I_MENSAJE_PERSONALIZADO = NULL,
            @O_MENSAJE = @O_MENSAJE OUTPUT;
    END;

    -- INICIO: Sucursales activas de la empresa del usuario.
    SELECT [SUC].[ID_SUCURSAL], [SUC].[CODIGO], [SUC].[NOMBRE]
    FROM [CONFIGURACION].[SUCURSALES] AS [SUC]
    INNER JOIN [SEGURIDAD].[USUARIOS] AS [USR]
        ON [USR].[ID_EMPRESA] = [SUC].[ID_EMPRESA]
    WHERE [USR].[NOMBRE_USUARIO] = @I_NOMBRE_USUARIO
      AND [USR].[CODIGO_ESTADO] = N'ACTIVO'
      AND [SUC].[CODIGO_ESTADO] = N'ACTIVO'
    ORDER BY [SUC].[NOMBRE];
    -- FIN: Sucursales activas de la empresa del usuario.
END;
