/*
===============================================================================
Proyecto:              Sistema Modular de Gestión OxiTigre
Componente:            SP_EMPRESA_BASE_LIST
Archivo:               SP_EMPRESA_BASE_LIST.sql
Procedimiento:         PLATAFORMA.SP_EMPRESA_BASE_LIST
Tipo:                  QUERY
Versión:               1.0.0
Fecha:                 2026-08-27
ID pedido:             FABRICA
Desarrollador:         Agustin Omar Cauzi
Correo:                agustincauzi10@hotmail.com
Descripción funcional: Lista las bases operativas activas que la API puede resolver.
Parámetros de entrada: No recibe parámetros.
Parámetros de salida: No aplica.
Retorno: Empresas activas, ubicación técnica, versión y referencia externa de secreto.
Tablas utilizadas: PLATAFORMA.EMPRESAS_BASES - READ.
Transacción: No aplica; solo lectura.
Auditoría: El arranque de la API registra fallos de carga sin exponer secretos.
Historial de modificaciones:
1.0.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
CREATE OR ALTER PROCEDURE [PLATAFORMA].[SP_EMPRESA_BASE_LIST]
AS
BEGIN
    SET NOCOUNT ON;

    -- INICIO: Lectura ordenada de bases operativas habilitadas.
    SELECT
        [ID_EMPRESA_PLATAFORMA],
        [CODIGO],
        [RAZON_SOCIAL],
        [SERVIDOR_SQL],
        [BASE_DATOS],
        [ID_EMPRESA_LOCAL],
        [VERSION_ESQUEMA],
        [CIFRAR_CONEXION],
        [CONFIAR_CERTIFICADO],
        [SECRETO_REFERENCIA]
    FROM [PLATAFORMA].[EMPRESAS_BASES]
    WHERE [CODIGO_ESTADO] = N'ACTIVO'
    ORDER BY [RAZON_SOCIAL];
    -- FIN: Lectura ordenada de bases operativas habilitadas.
END;
