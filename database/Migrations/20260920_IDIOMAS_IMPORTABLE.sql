/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: Idiomas importables
Archivo: 20260920_IDIOMAS_IMPORTABLE.sql | Versión: 1.0.0 | Fecha: 2026-09-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Permite persistir culturas específicas validadas por la API sin limitar el sistema a dos idiomas.
Precondición: Tablas de preferencias y traducciones de catálogo instaladas.
Validación posterior: las restricciones aceptan pt-BR y rechazan códigos sin región.
Recuperación: volver a es-AR/en-US antes de restaurar las restricciones anteriores.
Historial: 1.0.0 | 2026-09-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
IF EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE [name] = N'CK_USUARIOS_PREFERENCIAS_CULTURA'
)
BEGIN
    ALTER TABLE [CONFIGURACION].[USUARIOS_PREFERENCIAS]
        DROP CONSTRAINT [CK_USUARIOS_PREFERENCIAS_CULTURA];
END;
GO

ALTER TABLE [CONFIGURACION].[USUARIOS_PREFERENCIAS]
    ADD CONSTRAINT [CK_USUARIOS_PREFERENCIAS_CULTURA] CHECK
    (
        LEN([CULTURA]) BETWEEN 4 AND 10
        AND CHARINDEX(N'-', [CULTURA]) BETWEEN 2 AND LEN([CULTURA]) - 1
        AND [CULTURA] NOT LIKE N'% %'
    );
GO

IF EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE [name] = N'CK_TRADUCCIONES_CATALOGO_CULTURA'
)
BEGIN
    ALTER TABLE [CONFIGURACION].[TRADUCCIONES_CATALOGO]
        DROP CONSTRAINT [CK_TRADUCCIONES_CATALOGO_CULTURA];
END;
GO

ALTER TABLE [CONFIGURACION].[TRADUCCIONES_CATALOGO]
    ADD CONSTRAINT [CK_TRADUCCIONES_CATALOGO_CULTURA] CHECK
    (
        LEN([CULTURA]) BETWEEN 4 AND 10
        AND CHARINDEX(N'-', [CULTURA]) BETWEEN 2 AND LEN([CULTURA]) - 1
        AND [CULTURA] NOT LIKE N'% %'
    );
GO

:r "database\06_StoredProcedures\CONFIGURACION\Commands\SP_USUARIO_PREFERENCIA_SAVE.sql"
GO

:r "database\06_StoredProcedures\CONFIGURACION\Commands\SP_TRADUCCION_CATALOGO_SAVE.sql"
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE [name] = N'CK_USUARIOS_PREFERENCIAS_CULTURA'
)
    THROW 50000, 'No se instaló la validación de culturas importables.', 1;
