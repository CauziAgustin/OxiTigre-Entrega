/*
===============================================================================
Proyecto:              Sistema Modular de Gestión OxiTigre
Componente:            Seed de empresa
Archivo:               02_EMPRESA.sql
Versión:               1.0.0
Fecha:                 2026-08-19
ID pedido:             FABRICA
Desarrollador:         Agustin Omar Cauzi
Correo:                agustincauzi10@hotmail.com
Descripción funcional: Registra la empresa inicial de la base local OxiTigre.
Historial de modificaciones:
1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
IF NOT EXISTS (SELECT 1 FROM [CONFIGURACION].[EMPRESAS] WHERE [CODIGO] = N'OXITIGRE')
BEGIN
    INSERT INTO [CONFIGURACION].[EMPRESAS]
        ([CODIGO], [RAZON_SOCIAL], [NOMBRE_FANTASIA], [CODIGO_ESTADO], [ID_USUARIO_ALTA])
    VALUES
        (N'OXITIGRE', N'OxiTigre', N'OxiTigre', N'ACTIVO', 1);
END;
