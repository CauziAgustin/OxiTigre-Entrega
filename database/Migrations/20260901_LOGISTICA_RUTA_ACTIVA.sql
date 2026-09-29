/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: Exclusividad de recorrido activo
Archivo: 20260901_LOGISTICA_RUTA_ACTIVA.sql | Versión: 1.0.0 | Fecha: 2026-09-01 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Impide asignar otra ruta mientras el transportista está despachado o pausado.
Precondición: Pausa de recorridos instalada.
Validación posterior: el procedimiento considera DESPACHADA y PAUSADA como estados activos.
Recuperación: restaurar la versión anterior del procedimiento si se revierte la regla.
Historial: 1.0.0 | 2026-09-01 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
:r "database\06_StoredProcedures\LOGISTICA\Commands\SP_LOGISTICA_COMMAND.sql"
GO

IF OBJECT_DEFINITION(OBJECT_ID(N'LOGISTICA.SP_LOGISTICA_COMMAND')) NOT LIKE
   N'%CODIGO_ESTADO] IN (N''DESPACHADA'', N''PAUSADA'')%'
BEGIN
    THROW 50000, 'No se instaló la exclusividad de recorrido activo.', 1;
END;
