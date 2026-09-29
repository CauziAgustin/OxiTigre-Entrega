/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: AUDITORIA.MIGRACIONES_ESQUEMA
Archivo: MIGRACIONES_ESQUEMA.sql | Versión: 1.0.0 | Fecha: 2026-08-27 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Conserva el archivo y hash de cada migración incremental aplicada a una base.
Historial: 1.0.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
CREATE TABLE [AUDITORIA].[MIGRACIONES_ESQUEMA]
(
    [ID_MIGRACION_ESQUEMA] BIGINT IDENTITY(1,1) NOT NULL,
    [ARCHIVO] NVARCHAR(260) NOT NULL,
    [HASH_SHA256] CHAR(64) NOT NULL,
    [FECHA_APLICACION_UTC] DATETIME2(3) NOT NULL
        CONSTRAINT [DF_MIGRACIONES_ESQUEMA_FECHA] DEFAULT (SYSUTCDATETIME()),
    [USUARIO_APLICACION] SYSNAME NOT NULL
        CONSTRAINT [DF_MIGRACIONES_ESQUEMA_USUARIO] DEFAULT (ORIGINAL_LOGIN()),
    [CODIGO_ESTADO] NVARCHAR(30) NOT NULL
        CONSTRAINT [DF_MIGRACIONES_ESQUEMA_ESTADO] DEFAULT (N'APLICADA'),
    CONSTRAINT [PK_MIGRACIONES_ESQUEMA] PRIMARY KEY CLUSTERED ([ID_MIGRACION_ESQUEMA]),
    CONSTRAINT [UX_MIGRACIONES_ESQUEMA_ARCHIVO] UNIQUE ([ARCHIVO]),
    CONSTRAINT [CK_MIGRACIONES_ESQUEMA_ESTADO] CHECK ([CODIGO_ESTADO] = N'APLICADA')
);
