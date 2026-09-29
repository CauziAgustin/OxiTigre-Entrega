/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: Pausa de recorridos
Archivo: 20260901_LOGISTICA_RUTA_PAUSAS.sql | Versión: 1.0.0 | Fecha: 2026-09-01 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Conserva pausas y reanudaciones sin liberar carga ni solicitudes.
Precondición: Ofertas de hojas de ruta aplicadas.
Validación posterior: estado PAUSADA y acciones consolidadas disponibles.
Recuperación: reanudar hojas pausadas antes de restaurar la versión anterior del procedimiento.
Historial: 1.0.0 | 2026-09-01 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
INSERT INTO [CONFIGURACION].[ESTADOS]
    ([ENTIDAD], [CODIGO_ESTADO], [NOMBRE], [DESCRIPCION], [ES_INICIAL], [ES_FINAL],
     [ORDEN], [ID_USUARIO_ALTA])
SELECT N'HOJAS_RUTA', N'PAUSADA', N'Pausada',
       N'El recorrido conserva su carga y espera reanudación.', 0, 0, 3, 1
WHERE NOT EXISTS
(
    SELECT 1
    FROM [CONFIGURACION].[ESTADOS]
    WHERE [ENTIDAD] = N'HOJAS_RUTA'
      AND [CODIGO_ESTADO] = N'PAUSADA'
);
GO

:r "database\06_StoredProcedures\LOGISTICA\Commands\SP_LOGISTICA_COMMAND.sql"
GO

IF OBJECT_DEFINITION(OBJECT_ID(N'LOGISTICA.SP_LOGISTICA_COMMAND')) NOT LIKE N'%RUTA_REANUDAR%'
BEGIN
    THROW 50000, 'No se instaló la pausa trazable de recorridos.', 1;
END;
