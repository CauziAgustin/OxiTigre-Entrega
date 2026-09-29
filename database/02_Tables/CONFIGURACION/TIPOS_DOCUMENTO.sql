/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: CONFIGURACION.TIPOS_DOCUMENTO
Archivo: TIPOS_DOCUMENTO.sql
Versión: 1.0.0
Fecha: 2026-08-20
ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi
Correo: agustincauzi10@hotmail.com
Descripción funcional: Parametriza documentos admitidos para personas físicas y jurídicas.
Historial de modificaciones:
1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
CREATE TABLE [CONFIGURACION].[TIPOS_DOCUMENTO]
(
    [ID_TIPO_DOCUMENTO]            BIGINT IDENTITY(1,1) NOT NULL,
    [CODIGO]                       NVARCHAR(20) NOT NULL,
    [NOMBRE]                       NVARCHAR(100) NOT NULL,
    [APLICA_PERSONA_FISICA]        BIT NOT NULL,
    [APLICA_PERSONA_JURIDICA]      BIT NOT NULL,
    [CODIGO_ESTADO]                NVARCHAR(30) NOT NULL
        CONSTRAINT [DF_TIPOS_DOCUMENTO_CODIGO_ESTADO] DEFAULT (N'ACTIVO'),
    [FECHA_ALTA_UTC]               DATETIME2(3) NOT NULL
        CONSTRAINT [DF_TIPOS_DOCUMENTO_FECHA_ALTA_UTC] DEFAULT (SYSUTCDATETIME()),
    [ID_USUARIO_ALTA]              BIGINT NOT NULL,
    [FECHA_MODIFICACION_UTC]       DATETIME2(3) NULL,
    [ID_USUARIO_MODIFICACION]      BIGINT NULL,
    [ROW_VERSION]                  ROWVERSION NOT NULL,
    CONSTRAINT [PK_TIPOS_DOCUMENTO] PRIMARY KEY CLUSTERED ([ID_TIPO_DOCUMENTO])
);
