/*
===============================================================================
Proyecto:              Sistema Modular de Gestión OxiTigre
Componente:            COMERCIAL.CLIENTES
Archivo:               CLIENTES.sql
Versión:               1.0.0
Fecha:                 2026-08-19
ID pedido:             FABRICA
Desarrollador:         Agustin Omar Cauzi
Correo:                agustincauzi10@hotmail.com
Descripción funcional: Almacena la identidad y los datos generales del cliente.
Historial de modificaciones:
1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
CREATE TABLE [COMERCIAL].[CLIENTES]
(
    [ID_CLIENTE]                    BIGINT IDENTITY(1,1) NOT NULL,
    [ID_EMPRESA]                    BIGINT NOT NULL,
    [CODIGO]                        NVARCHAR(30) NOT NULL,
    [TIPO_PERSONA]                  CHAR(1) NOT NULL,
    [NOMBRE_RAZON_SOCIAL]           NVARCHAR(200) NOT NULL,
    [APELLIDO]                      NVARCHAR(150) NULL,
    [TIPO_DOCUMENTO]                NVARCHAR(20) NULL,
    [NUMERO_DOCUMENTO]              NVARCHAR(30) NULL,
    [EMAIL]                         NVARCHAR(254) NULL,
    [OBSERVACION]                   NVARCHAR(1000) NULL,
    [CODIGO_ESTADO]                 NVARCHAR(30) NOT NULL
        CONSTRAINT [DF_CLIENTES_CODIGO_ESTADO] DEFAULT (N'ACTIVO'),
    [FECHA_ALTA_UTC]                DATETIME2(3) NOT NULL
        CONSTRAINT [DF_CLIENTES_FECHA_ALTA_UTC] DEFAULT (SYSUTCDATETIME()),
    [ID_USUARIO_ALTA]               BIGINT NOT NULL,
    [FECHA_MODIFICACION_UTC]        DATETIME2(3) NULL,
    [ID_USUARIO_MODIFICACION]       BIGINT NULL,
    [ROW_VERSION]                   ROWVERSION NOT NULL,
    CONSTRAINT [PK_CLIENTES] PRIMARY KEY CLUSTERED ([ID_CLIENTE])
);
