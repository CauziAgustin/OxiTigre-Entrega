/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: Sucursal de la sesión
Archivo: 20260902_LOGIN_SUCURSAL.sql | Versión: 1.0.0 | Fecha: 2026-09-02 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Asocia opcionalmente cada sesión con la sucursal elegida al ingresar.
Precondición: Tablas CONFIGURACION.SUCURSALES y SEGURIDAD.SESIONES instaladas.
Validación posterior: la columna, relación y procedimientos de autenticación deben existir.
Recuperación: cerrar sesiones activas, eliminar la relación y luego la columna ID_SUCURSAL.
Historial: 1.0.0 | 2026-09-02 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
IF COL_LENGTH(N'SEGURIDAD.SESIONES', N'ID_SUCURSAL') IS NULL
BEGIN
    ALTER TABLE [SEGURIDAD].[SESIONES]
        ADD [ID_SUCURSAL] BIGINT NULL;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE [name] = N'FK_SESIONES_SUCURSALES'
)
BEGIN
    ALTER TABLE [SEGURIDAD].[SESIONES]
        ADD CONSTRAINT [FK_SESIONES_SUCURSALES]
        FOREIGN KEY ([ID_SUCURSAL])
        REFERENCES [CONFIGURACION].[SUCURSALES] ([ID_SUCURSAL]);
END;
GO

:r "database\06_StoredProcedures\SEGURIDAD\Queries\SP_USUARIO_EMPRESA_AUTH_LIST.sql"
GO

:r "database\06_StoredProcedures\SEGURIDAD\Queries\SP_USUARIO_AUTH_GET.sql"
GO

:r "database\06_StoredProcedures\SEGURIDAD\Commands\SP_SESION_CREATE.sql"
GO

:r "database\06_StoredProcedures\SEGURIDAD\Queries\SP_SESION_VALIDATE.sql"
GO

IF COL_LENGTH(N'SEGURIDAD.SESIONES', N'ID_SUCURSAL') IS NULL
    THROW 50000, 'No se instaló el contexto de sucursal de la sesión.', 1;
