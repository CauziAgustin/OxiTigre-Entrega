/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: CONFIGURACION.TRADUCCIONES_CATALOGO
Archivo: TRADUCCIONES_CATALOGO.sql | Versión: 1.1.0 | Fecha: 2026-09-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Traduce nombres visibles de catálogos manteniendo códigos internos estables.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-09-20 | FABRICA | Agustin Omar Cauzi | Admisión de culturas específicas importables.
===============================================================================
*/
CREATE TABLE [CONFIGURACION].[TRADUCCIONES_CATALOGO]
(
    [ID_TRADUCCION_CATALOGO]        BIGINT IDENTITY(1,1) NOT NULL,
    [ENTIDAD]                       NVARCHAR(100) NOT NULL,
    [CODIGO]                        NVARCHAR(100) NOT NULL,
    [CULTURA]                       NVARCHAR(10) NOT NULL,
    [NOMBRE]                        NVARCHAR(200) NOT NULL,
    [DESCRIPCION]                   NVARCHAR(1000) NULL,
    [CODIGO_ESTADO]                 NVARCHAR(30) NOT NULL
        CONSTRAINT [DF_TRADUCCIONES_CATALOGO_CODIGO_ESTADO] DEFAULT (N'ACTIVO'),
    [FECHA_ALTA_UTC]                DATETIME2(3) NOT NULL
        CONSTRAINT [DF_TRADUCCIONES_CATALOGO_FECHA_ALTA_UTC] DEFAULT (SYSUTCDATETIME()),
    [ID_USUARIO_ALTA]               BIGINT NOT NULL,
    [FECHA_MODIFICACION_UTC]        DATETIME2(3) NULL,
    [ID_USUARIO_MODIFICACION]       BIGINT NULL,
    [ROW_VERSION]                   ROWVERSION NOT NULL,
    CONSTRAINT [PK_TRADUCCIONES_CATALOGO] PRIMARY KEY CLUSTERED ([ID_TRADUCCION_CATALOGO]),
    CONSTRAINT [CK_TRADUCCIONES_CATALOGO_CULTURA] CHECK
    (
        LEN([CULTURA]) BETWEEN 4 AND 10
        AND CHARINDEX(N'-', [CULTURA]) BETWEEN 2 AND LEN([CULTURA]) - 1
        AND [CULTURA] NOT LIKE N'% %'
    )
);
