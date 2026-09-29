/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: CONFIGURACION.PAISES
Archivo: PAISES.sql
Versión: 1.0.0
Fecha: 2026-08-20
ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi
Correo: agustincauzi10@hotmail.com
Descripción funcional: Parametriza países y prefijos telefónicos sugeridos.
Historial de modificaciones:
1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
CREATE TABLE [CONFIGURACION].[PAISES]
(
    [ID_PAIS]                      BIGINT IDENTITY(1,1) NOT NULL,
    [CODIGO]                       NVARCHAR(10) NOT NULL,
    [NOMBRE]                       NVARCHAR(100) NOT NULL,
    [CODIGO_TELEFONICO]            NVARCHAR(5) NULL,
    [ES_PREDETERMINADO]            BIT NOT NULL
        CONSTRAINT [DF_PAISES_ES_PREDETERMINADO] DEFAULT (0),
    [CODIGO_ESTADO]                NVARCHAR(30) NOT NULL
        CONSTRAINT [DF_PAISES_CODIGO_ESTADO] DEFAULT (N'ACTIVO'),
    [FECHA_ALTA_UTC]               DATETIME2(3) NOT NULL
        CONSTRAINT [DF_PAISES_FECHA_ALTA_UTC] DEFAULT (SYSUTCDATETIME()),
    [ID_USUARIO_ALTA]              BIGINT NOT NULL,
    [FECHA_MODIFICACION_UTC]       DATETIME2(3) NULL,
    [ID_USUARIO_MODIFICACION]      BIGINT NULL,
    [ROW_VERSION]                  ROWVERSION NOT NULL,
    CONSTRAINT [PK_PAISES] PRIMARY KEY CLUSTERED ([ID_PAIS])
);
