/*
===============================================================================
Proyecto:              Sistema Modular de Gestión OxiTigre
Componente:            SP_SESION_VALIDATE
Archivo:               SP_SESION_VALIDATE.sql
Procedimiento:         SEGURIDAD.SP_SESION_VALIDATE
Tipo:                  QUERY
Versión:               1.1.0
Fecha:                 2026-08-20
ID pedido:             FABRICA
Desarrollador:         Agustin Omar Cauzi
Correo:                agustincauzi10@hotmail.com
Descripción funcional: Valida el hash de un token y recupera la identidad de una sesión vigente.
Parámetros de entrada: @I_HASH_TOKEN contiene SHA-512 del token opaco recibido.
Parámetros de salida: @O_CODIGO_ERROR y @O_MENSAJE informan el resultado.
Retorno: Una fila de sesión, seguida por roles y permisos efectivos.
Tablas utilizadas: SEGURIDAD.SESIONES, USUARIOS, ROLES y PERMISOS - READ.
Transacción: No aplica; solo lectura.
Auditoría: La ejecución protegida conserva el ID de sesión validado.
Historial de modificaciones:
1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
1.1.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Código funcional para sesión cerrada, revocada o vencida.
===============================================================================
*/
CREATE OR ALTER PROCEDURE [SEGURIDAD].[SP_SESION_VALIDATE]
    @I_HASH_TOKEN    VARBINARY(64),
    @O_CODIGO_ERROR BIGINT OUTPUT,
    @O_MENSAJE      NVARCHAR(4000) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @O_CODIGO_ERROR = NULL;
    SET @O_MENSAJE = NULL;

    -- INICIO: Recuperación de una sesión vigente y su identidad activa.
    SELECT [SES].[ID_SESION], [SES].[ID_CORRELACION], [SES].[FECHA_EXPIRACION_UTC],
           [USR].[ID_USUARIO], [USR].[ID_EMPRESA], [EMP].[CODIGO] AS [CODIGO_EMPRESA], [USR].[NOMBRE_USUARIO],
           [USR].[NOMBRES], [USR].[APELLIDO], [USR].[DEBE_CAMBIAR_CLAVE],
           [SES].[ID_SUCURSAL], [SUC].[CODIGO] AS [CODIGO_SUCURSAL],
           [SUC].[NOMBRE] AS [NOMBRE_SUCURSAL]
    FROM [SEGURIDAD].[SESIONES] AS [SES]
    INNER JOIN [SEGURIDAD].[USUARIOS] AS [USR] ON [USR].[ID_USUARIO] = [SES].[ID_USUARIO]
    INNER JOIN [CONFIGURACION].[EMPRESAS] AS [EMP] ON [EMP].[ID_EMPRESA] = [USR].[ID_EMPRESA]
    LEFT JOIN [CONFIGURACION].[SUCURSALES] AS [SUC] ON [SUC].[ID_SUCURSAL] = [SES].[ID_SUCURSAL]
    WHERE [SES].[HASH_TOKEN] = @I_HASH_TOKEN AND [SES].[CODIGO_ESTADO] = N'VIGENTE'
      AND [SES].[FECHA_EXPIRACION_UTC] > SYSUTCDATETIME() AND [USR].[CODIGO_ESTADO] = N'ACTIVO';
    -- FIN: Recuperación de una sesión vigente y su identidad activa.

    IF @@ROWCOUNT = 0
    BEGIN
        SET @O_CODIGO_ERROR = 10005;
        EXEC [AUDITORIA].[SP_ERROR_MESSAGE_RESOLVE]
            @I_CODIGO_ERROR = @O_CODIGO_ERROR, @I_MENSAJE_PERSONALIZADO = NULL,
            @O_MENSAJE = @O_MENSAJE OUTPUT;
    END;

    -- INICIO: Recuperación de roles de la sesión.
    SELECT [ROL].[CODIGO]
    FROM [SEGURIDAD].[SESIONES] AS [SES]
    INNER JOIN [SEGURIDAD].[USUARIOS_ROLES] AS [UR] ON [UR].[ID_USUARIO] = [SES].[ID_USUARIO]
    INNER JOIN [SEGURIDAD].[ROLES] AS [ROL] ON [ROL].[ID_ROL] = [UR].[ID_ROL]
    WHERE [SES].[HASH_TOKEN] = @I_HASH_TOKEN AND [SES].[CODIGO_ESTADO] = N'VIGENTE'
      AND [UR].[CODIGO_ESTADO] = N'ACTIVO' AND [ROL].[CODIGO_ESTADO] = N'ACTIVO'
      AND ([UR].[FECHA_VIGENCIA_HASTA_UTC] IS NULL OR [UR].[FECHA_VIGENCIA_HASTA_UTC] > SYSUTCDATETIME());
    -- FIN: Recuperación de roles de la sesión.

    -- INICIO: Recuperación de permisos efectivos de la sesión.
    SELECT DISTINCT [PER].[CODIGO]
    FROM [SEGURIDAD].[SESIONES] AS [SES]
    INNER JOIN [SEGURIDAD].[USUARIOS_ROLES] AS [UR] ON [UR].[ID_USUARIO] = [SES].[ID_USUARIO]
    INNER JOIN [SEGURIDAD].[ROLES_PERMISOS] AS [RP] ON [RP].[ID_ROL] = [UR].[ID_ROL]
    INNER JOIN [SEGURIDAD].[PERMISOS] AS [PER] ON [PER].[ID_PERMISO] = [RP].[ID_PERMISO]
    WHERE [SES].[HASH_TOKEN] = @I_HASH_TOKEN AND [SES].[CODIGO_ESTADO] = N'VIGENTE'
      AND [UR].[CODIGO_ESTADO] = N'ACTIVO' AND [RP].[CODIGO_ESTADO] = N'ACTIVO'
      AND [PER].[CODIGO_ESTADO] = N'ACTIVO';
    -- FIN: Recuperación de permisos efectivos de la sesión.
END;
