/*
===============================================================================
Proyecto:              Sistema Modular de Gestión OxiTigre
Componente:            CONFIGURACION.PARAMETROS_SISTEMA
Archivo:               PARAMETROS_SISTEMA.sql
Versión:               1.0.0
Fecha:                 2026-08-19
ID pedido:             FABRICA
Desarrollador:         Agustin Omar Cauzi
Correo:                agustincauzi10@hotmail.com
Descripción funcional: Almacena parámetros no secretos y referencias a secretos externos.
Historial de modificaciones:
1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
CREATE TABLE [CONFIGURACION].[PARAMETROS_SISTEMA]
(
    [ID_PARAMETRO_SISTEMA]          BIGINT IDENTITY(1,1) NOT NULL,
    [ID_MODULO]                     BIGINT NULL,
    [CLAVE]                         NVARCHAR(100) NOT NULL,
    [VALOR]                         NVARCHAR(2000) NULL,
    [TIPO_DATO]                     NVARCHAR(30) NOT NULL,
    [ES_SECRETO]                    BIT NOT NULL
        CONSTRAINT [DF_PARAMETROS_SISTEMA_ES_SECRETO] DEFAULT (0),
    [REFERENCIA_SECRETO]            NVARCHAR(250) NULL,
    [DESCRIPCION]                   NVARCHAR(500) NULL,
    [CODIGO_ESTADO]                 NVARCHAR(30) NOT NULL
        CONSTRAINT [DF_PARAMETROS_SISTEMA_CODIGO_ESTADO] DEFAULT (N'ACTIVO'),
    [FECHA_ALTA_UTC]                DATETIME2(3) NOT NULL
        CONSTRAINT [DF_PARAMETROS_SISTEMA_FECHA_ALTA_UTC] DEFAULT (SYSUTCDATETIME()),
    [ID_USUARIO_ALTA]               BIGINT NOT NULL,
    [FECHA_MODIFICACION_UTC]        DATETIME2(3) NULL,
    [ID_USUARIO_MODIFICACION]       BIGINT NULL,
    [ROW_VERSION]                   ROWVERSION NOT NULL,
    CONSTRAINT [PK_PARAMETROS_SISTEMA] PRIMARY KEY CLUSTERED ([ID_PARAMETRO_SISTEMA])
);
