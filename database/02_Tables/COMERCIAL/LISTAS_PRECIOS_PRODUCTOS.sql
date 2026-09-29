/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: COMERCIAL.LISTAS_PRECIOS_PRODUCTOS
Archivo: LISTAS_PRECIOS_PRODUCTOS.sql | Versión: 1.0.0 | Fecha: 2026-08-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Asigna un precio vigente a cada producto dentro de una lista.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
CREATE TABLE [COMERCIAL].[LISTAS_PRECIOS_PRODUCTOS]
(
    [ID_LISTA_PRECIO_PRODUCTO] BIGINT IDENTITY(1,1) NOT NULL,
    [ID_LISTA_PRECIO] BIGINT NOT NULL,
    [ID_PRODUCTO] BIGINT NOT NULL,
    [PRECIO_UNITARIO] DECIMAL(19,4) NOT NULL,
    [CODIGO_ESTADO] NVARCHAR(30) NOT NULL CONSTRAINT [DF_LISTAS_PRECIOS_PRODUCTOS_CODIGO_ESTADO] DEFAULT (N'ACTIVO'),
    [FECHA_ALTA_UTC] DATETIME2(3) NOT NULL CONSTRAINT [DF_LISTAS_PRECIOS_PRODUCTOS_FECHA_ALTA_UTC] DEFAULT (SYSUTCDATETIME()),
    [ID_USUARIO_ALTA] BIGINT NOT NULL,
    [FECHA_MODIFICACION_UTC] DATETIME2(3) NULL,
    [ID_USUARIO_MODIFICACION] BIGINT NULL,
    [ROW_VERSION] ROWVERSION NOT NULL,
    CONSTRAINT [PK_LISTAS_PRECIOS_PRODUCTOS] PRIMARY KEY CLUSTERED ([ID_LISTA_PRECIO_PRODUCTO])
);
