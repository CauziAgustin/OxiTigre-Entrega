/*
===============================================================================
Proyecto:              Sistema Modular de Gestión OxiTigre
Componente:            SEGURIDAD.USUARIOS
Archivo:               USUARIOS.sql
Versión:               1.0.0
Fecha:                 2026-08-19
ID pedido:             FABRICA
Desarrollador:         Agustin Omar Cauzi
Correo:                agustincauzi10@hotmail.com
Descripción funcional: Almacena identidades de aplicación y credenciales derivadas seguras.
Historial de modificaciones:
1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
CREATE TABLE [SEGURIDAD].[USUARIOS]
(
    [ID_USUARIO]                    BIGINT IDENTITY(1,1) NOT NULL,
    [ID_EMPRESA]                    BIGINT NOT NULL,
    [NOMBRE_USUARIO]                NVARCHAR(100) NOT NULL,
    [NOMBRES]                       NVARCHAR(150) NOT NULL,
    [APELLIDO]                      NVARCHAR(150) NOT NULL,
    [EMAIL]                         NVARCHAR(254) NOT NULL,
    [HASH_CLAVE]                    VARBINARY(64) NOT NULL,
    [SALT_CLAVE]                    VARBINARY(32) NOT NULL,
    [ALGORITMO_CLAVE]               NVARCHAR(30) NOT NULL,
    [ITERACIONES_CLAVE]             INT NOT NULL,
    [DEBE_CAMBIAR_CLAVE]            BIT NOT NULL
        CONSTRAINT [DF_USUARIOS_DEBE_CAMBIAR_CLAVE] DEFAULT (1),
    [INTENTOS_FALLIDOS]             SMALLINT NOT NULL
        CONSTRAINT [DF_USUARIOS_INTENTOS_FALLIDOS] DEFAULT (0),
    [FECHA_ULTIMO_ACCESO_UTC]       DATETIME2(3) NULL,
    [FECHA_BLOQUEO_UTC]             DATETIME2(3) NULL,
    [CODIGO_ESTADO]                 NVARCHAR(30) NOT NULL
        CONSTRAINT [DF_USUARIOS_CODIGO_ESTADO] DEFAULT (N'ACTIVO'),
    [FECHA_ALTA_UTC]                DATETIME2(3) NOT NULL
        CONSTRAINT [DF_USUARIOS_FECHA_ALTA_UTC] DEFAULT (SYSUTCDATETIME()),
    [ID_USUARIO_ALTA]               BIGINT NOT NULL,
    [FECHA_MODIFICACION_UTC]        DATETIME2(3) NULL,
    [ID_USUARIO_MODIFICACION]       BIGINT NULL,
    [ROW_VERSION]                   ROWVERSION NOT NULL,
    CONSTRAINT [PK_USUARIOS] PRIMARY KEY CLUSTERED ([ID_USUARIO])
);
