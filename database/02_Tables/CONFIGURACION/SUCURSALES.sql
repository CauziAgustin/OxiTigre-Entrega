/*
===============================================================================
Proyecto:              Sistema Modular de Gestión OxiTigre
Componente:            CONFIGURACION.SUCURSALES
Archivo:               SUCURSALES.sql
Versión:               1.0.0
Fecha:                 2026-08-19
ID pedido:             FABRICA
Desarrollador:         Agustin Omar Cauzi
Correo:                agustincauzi10@hotmail.com
Descripción funcional: Registra las sedes físicas pertenecientes a una empresa.
Historial de modificaciones:
1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
CREATE TABLE [CONFIGURACION].[SUCURSALES]
(
    [ID_SUCURSAL]                   BIGINT IDENTITY(1,1) NOT NULL,
    [ID_EMPRESA]                    BIGINT NOT NULL,
    [CODIGO]                        NVARCHAR(30) NOT NULL,
    [NOMBRE]                        NVARCHAR(200) NOT NULL,
    [DOMICILIO]                     NVARCHAR(250) NULL,
    [LOCALIDAD]                     NVARCHAR(150) NULL,
    [PROVINCIA]                     NVARCHAR(150) NULL,
    [CODIGO_POSTAL]                 NVARCHAR(20) NULL,
    [CODIGO_ESTADO]                 NVARCHAR(30) NOT NULL
        CONSTRAINT [DF_SUCURSALES_CODIGO_ESTADO] DEFAULT (N'ACTIVO'),
    [FECHA_ALTA_UTC]                DATETIME2(3) NOT NULL
        CONSTRAINT [DF_SUCURSALES_FECHA_ALTA_UTC] DEFAULT (SYSUTCDATETIME()),
    [ID_USUARIO_ALTA]               BIGINT NOT NULL,
    [FECHA_MODIFICACION_UTC]        DATETIME2(3) NULL,
    [ID_USUARIO_MODIFICACION]       BIGINT NULL,
    [ROW_VERSION]                   ROWVERSION NOT NULL,
    CONSTRAINT [PK_SUCURSALES] PRIMARY KEY CLUSTERED ([ID_SUCURSAL])
);
