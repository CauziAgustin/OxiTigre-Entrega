/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: INVENTARIO.UBICACIONES
Archivo: UBICACIONES.sql | Versión: 1.0.0 | Fecha: 2026-08-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Identifica posiciones internas dentro de cada depósito.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
CREATE TABLE [INVENTARIO].[UBICACIONES]
(
    [ID_UBICACION] BIGINT IDENTITY(1,1) NOT NULL,
    [ID_DEPOSITO] BIGINT NOT NULL,
    [CODIGO] NVARCHAR(30) NOT NULL,
    [NOMBRE] NVARCHAR(150) NOT NULL,
    [DESCRIPCION] NVARCHAR(500) NULL,
    [CODIGO_ESTADO] NVARCHAR(30) NOT NULL CONSTRAINT [DF_UBICACIONES_CODIGO_ESTADO] DEFAULT (N'ACTIVO'),
    [FECHA_ALTA_UTC] DATETIME2(3) NOT NULL CONSTRAINT [DF_UBICACIONES_FECHA_ALTA_UTC] DEFAULT (SYSUTCDATETIME()),
    [ID_USUARIO_ALTA] BIGINT NOT NULL,
    [FECHA_MODIFICACION_UTC] DATETIME2(3) NULL,
    [ID_USUARIO_MODIFICACION] BIGINT NULL,
    [ROW_VERSION] ROWVERSION NOT NULL,
    CONSTRAINT [PK_UBICACIONES] PRIMARY KEY CLUSTERED ([ID_UBICACION])
);
