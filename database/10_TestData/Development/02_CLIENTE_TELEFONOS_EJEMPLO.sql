/*
===============================================================================
Proyecto:              Sistema Modular de Gestión OxiTigre
Componente:            Cliente y teléfonos de Development
Archivo:               02_CLIENTE_TELEFONOS_EJEMPLO.sql
Versión:               1.1.0
Fecha:                 2026-08-20
ID pedido:             FABRICA
Desarrollador:         Agustin Omar Cauzi
Correo:                agustincauzi10@hotmail.com
Descripción funcional: Crea datos mínimos para observar el modelo cliente-teléfonos.
Historial de modificaciones:
1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
1.1.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Normalización del código a seis dígitos.
===============================================================================
*/
DECLARE @V_ID_EMPRESA BIGINT = (SELECT [ID_EMPRESA] FROM [CONFIGURACION].[EMPRESAS] WHERE [CODIGO] = N'OXITIGRE');
DECLARE @V_ID_USUARIO BIGINT = (SELECT [ID_USUARIO] FROM [SEGURIDAD].[USUARIOS] WHERE [ID_EMPRESA] = @V_ID_EMPRESA AND [NOMBRE_USUARIO] = N'AOCAUZI');

IF EXISTS (SELECT 1 FROM [COMERCIAL].[CLIENTES] WHERE [ID_EMPRESA] = @V_ID_EMPRESA AND [CODIGO] = N'CLI-0001')
BEGIN
    UPDATE [COMERCIAL].[CLIENTES]
    SET [CODIGO] = N'CLI-000001', [FECHA_MODIFICACION_UTC] = SYSUTCDATETIME(),
        [ID_USUARIO_MODIFICACION] = @V_ID_USUARIO
    WHERE [ID_EMPRESA] = @V_ID_EMPRESA AND [CODIGO] = N'CLI-0001';
END;

IF NOT EXISTS (SELECT 1 FROM [COMERCIAL].[CLIENTES] WHERE [ID_EMPRESA] = @V_ID_EMPRESA AND [CODIGO] = N'CLI-000001')
BEGIN
    INSERT INTO [COMERCIAL].[CLIENTES]
    (
        [ID_EMPRESA], [CODIGO], [TIPO_PERSONA], [NOMBRE_RAZON_SOCIAL], [APELLIDO],
        [TIPO_DOCUMENTO], [NUMERO_DOCUMENTO], [EMAIL], [CODIGO_ESTADO], [ID_USUARIO_ALTA]
    )
    VALUES
    (
        @V_ID_EMPRESA, N'CLI-000001', 'F', N'Cliente', N'Demostración',
        N'DNI', N'00000000', N'cliente.demo@example.com', N'ACTIVO', @V_ID_USUARIO
    );
END;

DECLARE @V_ID_CLIENTE BIGINT =
(
    SELECT [ID_CLIENTE] FROM [COMERCIAL].[CLIENTES]
    WHERE [ID_EMPRESA] = @V_ID_EMPRESA AND [CODIGO] = N'CLI-000001'
);

INSERT INTO [COMERCIAL].[CLIENTES_TELEFONOS]
(
    [ID_CLIENTE], [ID_TIPO_TELEFONO], [CODIGO_PAIS], [CODIGO_AREA], [NUMERO],
    [ORDEN], [ES_PRINCIPAL], [PERMITE_WHATSAPP], [CODIGO_ESTADO], [ID_USUARIO_ALTA]
)
SELECT @V_ID_CLIENTE, [T].[ID_TIPO_TELEFONO], N'54', N'11', [D].[NUMERO],
       [D].[ORDEN], [D].[ES_PRINCIPAL], [D].[PERMITE_WHATSAPP], N'ACTIVO', @V_ID_USUARIO
FROM (VALUES
    (N'MOVIL', N'55550001', CONVERT(SMALLINT, 0), CONVERT(BIT, 1), CONVERT(BIT, 1)),
    (N'FIJO', N'55550002', CONVERT(SMALLINT, 1), CONVERT(BIT, 0), CONVERT(BIT, 0))
) AS [D] ([CODIGO_TIPO], [NUMERO], [ORDEN], [ES_PRINCIPAL], [PERMITE_WHATSAPP])
INNER JOIN [CONFIGURACION].[TIPOS_TELEFONO] AS [T] ON [T].[CODIGO] = [D].[CODIGO_TIPO]
WHERE NOT EXISTS
(
    SELECT 1 FROM [COMERCIAL].[CLIENTES_TELEFONOS] AS [ACT]
    WHERE [ACT].[ID_CLIENTE] = @V_ID_CLIENTE AND [ACT].[ORDEN] = [D].[ORDEN]
      AND [ACT].[CODIGO_ESTADO] = N'ACTIVO'
);

-- INICIO: Sincronización del próximo código luego de insertar datos de Development fuera del SP.
DECLARE @V_PROXIMO_CODIGO BIGINT =
(
    SELECT ISNULL(MAX(TRY_CONVERT(BIGINT, SUBSTRING([CODIGO], 5, 26))), 0) + 1
    FROM [COMERCIAL].[CLIENTES]
    WHERE [CODIGO] LIKE N'CLI-%'
);
DECLARE @V_SQL_SECUENCIA NVARCHAR(200) =
    N'ALTER SEQUENCE [COMERCIAL].[SEQ_CLIENTE_CODIGO] RESTART WITH ' + CONVERT(NVARCHAR(20), @V_PROXIMO_CODIGO) + N';';
EXEC [sys].[sp_executesql] @V_SQL_SECUENCIA;
-- FIN: Sincronización del próximo código luego de insertar datos de Development fuera del SP.
