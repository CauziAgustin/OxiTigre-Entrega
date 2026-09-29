/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: SP_USUARIO_PREFERENCIA_GET
Procedimiento: CONFIGURACION.SP_USUARIO_PREFERENCIA_GET
Tipo:                  QUERY
Archivo: SP_USUARIO_PREFERENCIA_GET.sql | Versión: 1.0.1 | Fecha: 2026-09-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Obtiene el idioma preferido del usuario autenticado con español como valor inicial.
Parámetros de entrada: Usuario de la sesión.
Parámetros de salida: Código y mensaje controlados.
Retorno: Una cultura soportada.
Tablas utilizadas: CONFIGURACION.USUARIOS_PREFERENCIAS - READ.
Transacción: No aplica; solo lectura.
Auditoría: No aplica por ser una preferencia de presentación.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.0.1 | 2026-09-20 | FABRICA | Agustin Omar Cauzi | Documentación técnica completa del procedimiento.
===============================================================================
*/
CREATE OR ALTER PROCEDURE [CONFIGURACION].[SP_USUARIO_PREFERENCIA_GET]
    @S_ID_USUARIO BIGINT,
    @O_CODIGO_ERROR BIGINT OUTPUT,
    @O_MENSAJE NVARCHAR(4000) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @O_CODIGO_ERROR = NULL;
    SET @O_MENSAJE = NULL;

    SELECT COALESCE
    (
        (SELECT [CULTURA] FROM [CONFIGURACION].[USUARIOS_PREFERENCIAS]
         WHERE [ID_USUARIO] = @S_ID_USUARIO AND [CODIGO_ESTADO] = N'ACTIVO'),
        N'es-AR'
    ) AS [CULTURA];
END;
