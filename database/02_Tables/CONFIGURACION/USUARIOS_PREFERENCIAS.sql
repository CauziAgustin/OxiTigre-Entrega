/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: CONFIGURACION.USUARIOS_PREFERENCIAS
Archivo: USUARIOS_PREFERENCIAS.sql | Versión: 1.1.0 | Fecha: 2026-09-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Conserva preferencias personales de presentación sin alterar datos de negocio.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-09-20 | FABRICA | Agustin Omar Cauzi | Admisión de culturas específicas importables.
===============================================================================
*/
CREATE TABLE [CONFIGURACION].[USUARIOS_PREFERENCIAS]
(
    [ID_USUARIO_PREFERENCIA]        BIGINT IDENTITY(1,1) NOT NULL,
    [ID_USUARIO]                    BIGINT NOT NULL,
    [CULTURA]                       NVARCHAR(10) NOT NULL
        CONSTRAINT [DF_USUARIOS_PREFERENCIAS_CULTURA] DEFAULT (N'es-AR'),
    [CODIGO_ESTADO]                 NVARCHAR(30) NOT NULL
        CONSTRAINT [DF_USUARIOS_PREFERENCIAS_CODIGO_ESTADO] DEFAULT (N'ACTIVO'),
    [FECHA_ALTA_UTC]                DATETIME2(3) NOT NULL
        CONSTRAINT [DF_USUARIOS_PREFERENCIAS_FECHA_ALTA_UTC] DEFAULT (SYSUTCDATETIME()),
    [ID_USUARIO_ALTA]               BIGINT NOT NULL,
    [FECHA_MODIFICACION_UTC]        DATETIME2(3) NULL,
    [ID_USUARIO_MODIFICACION]       BIGINT NULL,
    [ROW_VERSION]                   ROWVERSION NOT NULL,
    CONSTRAINT [PK_USUARIOS_PREFERENCIAS] PRIMARY KEY CLUSTERED ([ID_USUARIO_PREFERENCIA]),
    CONSTRAINT [CK_USUARIOS_PREFERENCIAS_CULTURA] CHECK
    (
        LEN([CULTURA]) BETWEEN 4 AND 10
        AND CHARINDEX(N'-', [CULTURA]) BETWEEN 2 AND LEN([CULTURA]) - 1
        AND [CULTURA] NOT LIKE N'% %'
    )
);
