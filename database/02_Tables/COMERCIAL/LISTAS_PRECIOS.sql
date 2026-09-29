/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: COMERCIAL.LISTAS_PRECIOS
Archivo: LISTAS_PRECIOS.sql | Versión: 1.0.0 | Fecha: 2026-08-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Define listas de precios comerciales por empresa y moneda.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
CREATE TABLE [COMERCIAL].[LISTAS_PRECIOS]
(
    [ID_LISTA_PRECIO] BIGINT IDENTITY(1,1) NOT NULL,
    [ID_EMPRESA] BIGINT NOT NULL,
    [CODIGO] NVARCHAR(30) NOT NULL,
    [NOMBRE] NVARCHAR(150) NOT NULL,
    [MONEDA] CHAR(3) NOT NULL,
    [FECHA_VIGENCIA_DESDE] DATE NULL,
    [FECHA_VIGENCIA_HASTA] DATE NULL,
    [CODIGO_ESTADO] NVARCHAR(30) NOT NULL CONSTRAINT [DF_LISTAS_PRECIOS_CODIGO_ESTADO] DEFAULT (N'ACTIVO'),
    [FECHA_ALTA_UTC] DATETIME2(3) NOT NULL CONSTRAINT [DF_LISTAS_PRECIOS_FECHA_ALTA_UTC] DEFAULT (SYSUTCDATETIME()),
    [ID_USUARIO_ALTA] BIGINT NOT NULL,
    [FECHA_MODIFICACION_UTC] DATETIME2(3) NULL,
    [ID_USUARIO_MODIFICACION] BIGINT NULL,
    [ROW_VERSION] ROWVERSION NOT NULL,
    CONSTRAINT [PK_LISTAS_PRECIOS] PRIMARY KEY CLUSTERED ([ID_LISTA_PRECIO])
);
