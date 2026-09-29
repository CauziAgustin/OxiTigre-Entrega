/*
===============================================================================
Proyecto:              Sistema Modular de Gestión OxiTigre
Componente:            SP_USUARIO_AUTH_GET
Archivo:               SP_USUARIO_AUTH_GET.sql
Procedimiento:         SEGURIDAD.SP_USUARIO_AUTH_GET
Tipo:                  QUERY
Versión:               1.2.0
Fecha:                 2026-08-20
ID pedido:             FABRICA
Desarrollador:         Agustin Omar Cauzi
Correo:                agustincauzi10@hotmail.com
Descripción funcional: Obtiene la credencial derivada, roles y permisos de un usuario activo.
Parámetros de entrada: @I_CODIGO_EMPRESA y @I_NOMBRE_USUARIO identifican la cuenta.
Parámetros de salida: @O_CODIGO_ERROR y @O_MENSAJE informan el resultado controlado.
Retorno: Cuatro resultados: cuenta, roles, permisos y sucursales activas.
Tablas utilizadas: CONFIGURACION.EMPRESAS, SUCURSALES, SEGURIDAD.USUARIOS, ROLES, PERMISOS y relaciones - READ.
Transacción: No aplica; solo lectura.
Auditoría: El servicio registra el intento sin exponer hash, salt ni contraseña.
Historial de modificaciones:
1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
1.1.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Exposición controlada del estado de bloqueo.
1.2.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Resultado controlado para credenciales sin cuenta activa.
===============================================================================
*/
CREATE OR ALTER PROCEDURE [SEGURIDAD].[SP_USUARIO_AUTH_GET]
    @I_CODIGO_EMPRESA  NVARCHAR(30),
    @I_NOMBRE_USUARIO NVARCHAR(100),
    @O_CODIGO_ERROR   BIGINT OUTPUT,
    @O_MENSAJE        NVARCHAR(4000) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @O_CODIGO_ERROR = NULL;
    SET @O_MENSAJE = NULL;

    -- INICIO: Consulta de la cuenta activa sin comparar contraseñas en SQL Server.
    SELECT [USR].[ID_USUARIO], [USR].[ID_EMPRESA], [USR].[NOMBRE_USUARIO],
           [USR].[NOMBRES], [USR].[APELLIDO], [USR].[EMAIL], [USR].[HASH_CLAVE],
           [USR].[SALT_CLAVE], [USR].[ALGORITMO_CLAVE], [USR].[ITERACIONES_CLAVE],
           [USR].[DEBE_CAMBIAR_CLAVE], [USR].[INTENTOS_FALLIDOS], [USR].[FECHA_BLOQUEO_UTC]
    FROM [SEGURIDAD].[USUARIOS] AS [USR]
    INNER JOIN [CONFIGURACION].[EMPRESAS] AS [EMP] ON [EMP].[ID_EMPRESA] = [USR].[ID_EMPRESA]
    WHERE [EMP].[CODIGO] = @I_CODIGO_EMPRESA
      AND [EMP].[CODIGO_ESTADO] = N'ACTIVO'
      AND [USR].[NOMBRE_USUARIO] = @I_NOMBRE_USUARIO
      AND [USR].[CODIGO_ESTADO] = N'ACTIVO';
    -- FIN: Consulta de la cuenta activa sin comparar contraseñas en SQL Server.

    IF @@ROWCOUNT = 0
    BEGIN
        SET @O_CODIGO_ERROR = 10001;
        EXEC [AUDITORIA].[SP_ERROR_MESSAGE_RESOLVE]
            @I_CODIGO_ERROR = @O_CODIGO_ERROR, @I_MENSAJE_PERSONALIZADO = NULL,
            @O_MENSAJE = @O_MENSAJE OUTPUT;
    END;

    -- INICIO: Consulta de roles activos de la cuenta.
    SELECT [ROL].[CODIGO]
    FROM [SEGURIDAD].[USUARIOS] AS [USR]
    INNER JOIN [CONFIGURACION].[EMPRESAS] AS [EMP] ON [EMP].[ID_EMPRESA] = [USR].[ID_EMPRESA]
    INNER JOIN [SEGURIDAD].[USUARIOS_ROLES] AS [UR] ON [UR].[ID_USUARIO] = [USR].[ID_USUARIO]
    INNER JOIN [SEGURIDAD].[ROLES] AS [ROL] ON [ROL].[ID_ROL] = [UR].[ID_ROL]
    WHERE [EMP].[CODIGO] = @I_CODIGO_EMPRESA AND [USR].[NOMBRE_USUARIO] = @I_NOMBRE_USUARIO
      AND [UR].[CODIGO_ESTADO] = N'ACTIVO' AND [ROL].[CODIGO_ESTADO] = N'ACTIVO'
      AND ([UR].[FECHA_VIGENCIA_HASTA_UTC] IS NULL OR [UR].[FECHA_VIGENCIA_HASTA_UTC] > SYSUTCDATETIME());
    -- FIN: Consulta de roles activos de la cuenta.

    -- INICIO: Consulta de permisos efectivos sin duplicados.
    SELECT DISTINCT [PER].[CODIGO]
    FROM [SEGURIDAD].[USUARIOS] AS [USR]
    INNER JOIN [CONFIGURACION].[EMPRESAS] AS [EMP] ON [EMP].[ID_EMPRESA] = [USR].[ID_EMPRESA]
    INNER JOIN [SEGURIDAD].[USUARIOS_ROLES] AS [UR] ON [UR].[ID_USUARIO] = [USR].[ID_USUARIO]
    INNER JOIN [SEGURIDAD].[ROLES_PERMISOS] AS [RP] ON [RP].[ID_ROL] = [UR].[ID_ROL]
    INNER JOIN [SEGURIDAD].[PERMISOS] AS [PER] ON [PER].[ID_PERMISO] = [RP].[ID_PERMISO]
    WHERE [EMP].[CODIGO] = @I_CODIGO_EMPRESA AND [USR].[NOMBRE_USUARIO] = @I_NOMBRE_USUARIO
      AND [UR].[CODIGO_ESTADO] = N'ACTIVO' AND [RP].[CODIGO_ESTADO] = N'ACTIVO'
      AND [PER].[CODIGO_ESTADO] = N'ACTIVO'
      AND ([UR].[FECHA_VIGENCIA_HASTA_UTC] IS NULL OR [UR].[FECHA_VIGENCIA_HASTA_UTC] > SYSUTCDATETIME());
    -- FIN: Consulta de permisos efectivos sin duplicados.

    -- INICIO: Consulta de sucursales activas de la empresa autenticada.
    SELECT [SUC].[ID_SUCURSAL], [SUC].[CODIGO], [SUC].[NOMBRE]
    FROM [CONFIGURACION].[SUCURSALES] AS [SUC]
    INNER JOIN [CONFIGURACION].[EMPRESAS] AS [EMP]
        ON [EMP].[ID_EMPRESA] = [SUC].[ID_EMPRESA]
    WHERE [EMP].[CODIGO] = @I_CODIGO_EMPRESA
      AND [EMP].[CODIGO_ESTADO] = N'ACTIVO'
      AND [SUC].[CODIGO_ESTADO] = N'ACTIVO'
    ORDER BY [SUC].[NOMBRE];
    -- FIN: Consulta de sucursales activas de la empresa autenticada.
END;
