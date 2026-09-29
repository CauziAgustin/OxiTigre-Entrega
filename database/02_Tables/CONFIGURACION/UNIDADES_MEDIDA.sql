/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: CONFIGURACION.UNIDADES_MEDIDA
Archivo: UNIDADES_MEDIDA.sql | Versión: 1.0.0 | Fecha: 2026-08-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Define las unidades homogéneas utilizadas para cuantificar productos.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
CREATE TABLE [CONFIGURACION].[UNIDADES_MEDIDA]
(
    [ID_UNIDAD_MEDIDA] BIGINT IDENTITY(1,1) NOT NULL,
    [CODIGO] NVARCHAR(30) NOT NULL,
    [NOMBRE] NVARCHAR(100) NOT NULL,
    [SIMBOLO] NVARCHAR(20) NOT NULL,
    [PERMITE_DECIMALES] BIT NOT NULL CONSTRAINT [DF_UNIDADES_MEDIDA_PERMITE_DECIMALES] DEFAULT (1),
    [CODIGO_ESTADO] NVARCHAR(30) NOT NULL CONSTRAINT [DF_UNIDADES_MEDIDA_CODIGO_ESTADO] DEFAULT (N'ACTIVO'),
    [FECHA_ALTA_UTC] DATETIME2(3) NOT NULL CONSTRAINT [DF_UNIDADES_MEDIDA_FECHA_ALTA_UTC] DEFAULT (SYSUTCDATETIME()),
    [ID_USUARIO_ALTA] BIGINT NOT NULL,
    [FECHA_MODIFICACION_UTC] DATETIME2(3) NULL,
    [ID_USUARIO_MODIFICACION] BIGINT NULL,
    [ROW_VERSION] ROWVERSION NOT NULL,
    CONSTRAINT [PK_UNIDADES_MEDIDA] PRIMARY KEY CLUSTERED ([ID_UNIDAD_MEDIDA])
);
