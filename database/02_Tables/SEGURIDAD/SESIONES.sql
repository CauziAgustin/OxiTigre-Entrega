/*
===============================================================================
Proyecto:              Sistema Modular de Gestión OxiTigre
Componente:            SEGURIDAD.SESIONES
Archivo:               SESIONES.sql
Versión:               1.0.1
Fecha:                 2026-08-19
ID pedido:             FABRICA
Desarrollador:         Agustin Omar Cauzi
Correo:                agustincauzi10@hotmail.com
Descripción funcional: Registra sesiones autenticadas sin almacenar tokens en texto plano.
Historial de modificaciones:
1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
1.0.1 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Homogeneización del estado inicial VIGENTE.
===============================================================================
*/
CREATE TABLE [SEGURIDAD].[SESIONES]
(
    [ID_SESION]                     BIGINT IDENTITY(1,1) NOT NULL,
    [ID_USUARIO]                    BIGINT NOT NULL,
    [ID_SUCURSAL]                   BIGINT NULL,
    [ID_DISPOSITIVO_ACCESO]         BIGINT NULL,
    [HASH_TOKEN]                    VARBINARY(64) NOT NULL,
    [FECHA_INICIO_UTC]              DATETIME2(3) NOT NULL,
    [FECHA_EXPIRACION_UTC]          DATETIME2(3) NOT NULL,
    [FECHA_CIERRE_UTC]              DATETIME2(3) NULL,
    [IP_ORIGEN]                     NVARCHAR(45) NULL,
    [APLICACION_ORIGEN]             NVARCHAR(100) NOT NULL,
    [ID_CORRELACION]                UNIQUEIDENTIFIER NOT NULL,
    [CODIGO_ESTADO]                 NVARCHAR(30) NOT NULL
        CONSTRAINT [DF_SESIONES_CODIGO_ESTADO] DEFAULT (N'VIGENTE'),
    [FECHA_ALTA_UTC]                DATETIME2(3) NOT NULL
        CONSTRAINT [DF_SESIONES_FECHA_ALTA_UTC] DEFAULT (SYSUTCDATETIME()),
    [ID_USUARIO_ALTA]               BIGINT NOT NULL,
    [FECHA_MODIFICACION_UTC]        DATETIME2(3) NULL,
    [ID_USUARIO_MODIFICACION]       BIGINT NULL,
    [ROW_VERSION]                   ROWVERSION NOT NULL,
    CONSTRAINT [PK_SESIONES] PRIMARY KEY CLUSTERED ([ID_SESION])
);
