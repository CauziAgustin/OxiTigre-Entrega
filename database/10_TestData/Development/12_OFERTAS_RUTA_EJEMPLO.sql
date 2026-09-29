/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: Ofertas de hojas de ruta para Desarrollo
Archivo: 12_OFERTAS_RUTA_EJEMPLO.sql | Versión: 1.0.0 | Fecha: 2026-09-01 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Crea dos ofertas con cinco puntos para probar la autoselección móvil.
Historial: 1.0.0 | 2026-09-01 | FABRICA | Agustin Omar Cauzi | Creación inicial idempotente.
===============================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @V_ID_EMPRESA BIGINT =
(
    SELECT [ID_EMPRESA]
    FROM [CONFIGURACION].[EMPRESAS]
    WHERE [CODIGO] = N'OXITIGRE'
);
DECLARE @V_ID_USUARIO BIGINT =
(
    SELECT TOP (1) [ID_USUARIO]
    FROM [SEGURIDAD].[USUARIOS]
    WHERE [ID_EMPRESA] = @V_ID_EMPRESA
      AND [CODIGO_ESTADO] = N'ACTIVO'
    ORDER BY [ID_USUARIO]
);
DECLARE @V_ID_CLIENTE BIGINT =
(
    SELECT TOP (1) [ID_CLIENTE]
    FROM [COMERCIAL].[CLIENTES]
    WHERE [ID_EMPRESA] = @V_ID_EMPRESA
      AND [CODIGO_ESTADO] = N'ACTIVO'
    ORDER BY [ID_CLIENTE]
);

IF @V_ID_EMPRESA IS NULL OR @V_ID_USUARIO IS NULL OR @V_ID_CLIENTE IS NULL
    THROW 51000, N'Faltan empresa, usuario o cliente para crear las ofertas demostrativas.', 1;

IF NOT EXISTS
(
    SELECT 1
    FROM [LOGISTICA].[HOJAS_RUTA]
    WHERE [ID_EMPRESA] = @V_ID_EMPRESA
      AND [OBSERVACION] IN
          (N'DEMO_OFERTA_MOVIL: recorrido norte.', N'DEMO_OFERTA_MOVIL: recorrido industrial.')
)
BEGIN
    BEGIN TRANSACTION;

    BEGIN TRY
        DECLARE @T_DIRECCIONES TABLE
        (
            [ORDEN] INT PRIMARY KEY,
            [NOMBRE] NVARCHAR(120) NOT NULL,
            [DOMICILIO] NVARCHAR(250) NOT NULL,
            [LOCALIDAD] NVARCHAR(100) NOT NULL,
            [CONTACTO] NVARCHAR(150) NOT NULL,
            [TELEFONO] NVARCHAR(50) NOT NULL,
            [HORA_DESDE] TIME(0) NOT NULL,
            [HORA_HASTA] TIME(0) NOT NULL,
            [ID_DIRECCION] BIGINT NULL
        );

        INSERT INTO @T_DIRECCIONES
            ([ORDEN], [NOMBRE], [DOMICILIO], [LOCALIDAD], [CONTACTO], [TELEFONO],
             [HORA_DESDE], [HORA_HASTA])
        VALUES
            (1, N'Demo móvil Tigre', N'Av. Cazón 1250', N'Tigre', N'Recepción Tigre', N'+54 11 5555-0201', '09:00', '11:00'),
            (2, N'Demo móvil San Fernando', N'Constitución 860', N'San Fernando', N'Depósito San Fernando', N'+54 11 5555-0202', '11:30', '13:30'),
            (3, N'Demo móvil Victoria', N'Av. Perón 2200', N'Victoria', N'Encargado Victoria', N'+54 11 5555-0203', '14:00', '16:00'),
            (4, N'Demo móvil Virreyes', N'Av. Avellaneda 3150', N'Virreyes', N'Portería Virreyes', N'+54 11 5555-0204', '08:00', '10:00'),
            (5, N'Demo móvil Don Torcuato', N'Av. Alvear 1750', N'Don Torcuato', N'Planta Don Torcuato', N'+54 11 5555-0205', '15:00', '17:00');

        DECLARE @V_ORDEN INT = 1;
        DECLARE @V_ID BIGINT;
        DECLARE @V_FILAS INT;
        DECLARE @V_ERROR BIGINT;
        DECLARE @V_MENSAJE NVARCHAR(4000);
        DECLARE @V_NOMBRE NVARCHAR(120);
        DECLARE @V_DOMICILIO NVARCHAR(250);
        DECLARE @V_LOCALIDAD NVARCHAR(100);
        DECLARE @V_CONTACTO NVARCHAR(150);
        DECLARE @V_TELEFONO NVARCHAR(50);
        DECLARE @V_HORA_DESDE TIME(0);
        DECLARE @V_HORA_HASTA TIME(0);
        DECLARE @V_FECHA DATE;
        DECLARE @V_PRIORIDAD NVARCHAR(30);
        DECLARE @V_TEXTO NVARCHAR(500);

        WHILE @V_ORDEN <= 5
        BEGIN
            SELECT
                @V_NOMBRE = [NOMBRE],
                @V_DOMICILIO = [DOMICILIO],
                @V_LOCALIDAD = [LOCALIDAD],
                @V_CONTACTO = [CONTACTO],
                @V_TELEFONO = [TELEFONO],
                @V_HORA_DESDE = [HORA_DESDE],
                @V_HORA_HASTA = [HORA_HASTA]
            FROM @T_DIRECCIONES
            WHERE [ORDEN] = @V_ORDEN;

            SELECT @V_ID = [ID_DIRECCION]
            FROM [LOGISTICA].[CLIENTES_DIRECCIONES]
            WHERE [ID_EMPRESA] = @V_ID_EMPRESA
              AND [ID_CLIENTE] = @V_ID_CLIENTE
              AND [NOMBRE] = @V_NOMBRE;

            IF @V_ID IS NULL
            BEGIN
                EXEC [LOGISTICA].[SP_LOGISTICA_COMMAND]
                    @I_ACCION = N'DIRECCION_GUARDAR',
                    @I_ID_EMPRESA = @V_ID_EMPRESA,
                    @S_ID_SESION = 1,
                    @S_ID_USUARIO = @V_ID_USUARIO,
                    @I_ID_CLIENTE = @V_ID_CLIENTE,
                    @I_NOMBRE = @V_NOMBRE,
                    @I_DOMICILIO = @V_DOMICILIO,
                    @I_LOCALIDAD = @V_LOCALIDAD,
                    @I_PROVINCIA = N'Buenos Aires',
                    @I_CONTACTO = @V_CONTACTO,
                    @I_TELEFONO = @V_TELEFONO,
                    @I_HORA_DESDE = @V_HORA_DESDE,
                    @I_HORA_HASTA = @V_HORA_HASTA,
                    @I_INSTRUCCIONES = N'Llamar al contacto antes de llegar y respetar la ventana horaria.',
                    @O_ID = @V_ID OUTPUT,
                    @O_FILAS_AFECTADAS = @V_FILAS OUTPUT,
                    @O_CODIGO_ERROR = @V_ERROR OUTPUT,
                    @O_MENSAJE = @V_MENSAJE OUTPUT;

                IF @V_ERROR IS NOT NULL
                    THROW 51000, @V_MENSAJE, 1;
            END;

            UPDATE @T_DIRECCIONES SET [ID_DIRECCION] = @V_ID WHERE [ORDEN] = @V_ORDEN;
            SET @V_ID = NULL;
            SET @V_ORDEN += 1;
        END;

        DECLARE @T_SOLICITUDES TABLE
        (
            [ORDEN] INT PRIMARY KEY,
            [GRUPO_RUTA] INT NOT NULL,
            [ID_SOLICITUD] BIGINT NULL
        );

        INSERT INTO @T_SOLICITUDES ([ORDEN], [GRUPO_RUTA])
        VALUES (1, 1), (2, 1), (3, 1), (4, 2), (5, 2);

        SET @V_ORDEN = 1;

        WHILE @V_ORDEN <= 5
        BEGIN
            SELECT
                @V_ID = [ID_DIRECCION],
                @V_HORA_DESDE = [HORA_DESDE],
                @V_HORA_HASTA = [HORA_HASTA]
            FROM @T_DIRECCIONES
            WHERE [ORDEN] = @V_ORDEN;

            SET @V_PRIORIDAD = CASE WHEN @V_ORDEN IN (2, 5) THEN N'ALTA' ELSE N'NORMAL' END;
            SET @V_FECHA = DATEADD(DAY, CASE WHEN @V_ORDEN <= 3 THEN 1 ELSE 2 END, CAST(GETDATE() AS DATE));
            SET @V_TEXTO = CONCAT(N'Entrega demostrativa ', @V_ORDEN,
                                  N'. Confirmar recepción desde la aplicación.');

            EXEC [LOGISTICA].[SP_LOGISTICA_COMMAND]
                @I_ACCION = N'SOLICITUD_CREAR',
                @I_ID_EMPRESA = @V_ID_EMPRESA,
                @S_ID_SESION = 1,
                @S_ID_USUARIO = @V_ID_USUARIO,
                @I_ID_CLIENTE = @V_ID_CLIENTE,
                @I_ID_DIRECCION = @V_ID,
                @I_TIPO = N'ENTREGA_PEDIDO',
                @I_PRIORIDAD = @V_PRIORIDAD,
                @I_FECHA = @V_FECHA,
                @I_HORA_DESDE = @V_HORA_DESDE,
                @I_HORA_HASTA = @V_HORA_HASTA,
                @I_INSTRUCCIONES = @V_TEXTO,
                @I_OBSERVACION = @V_TEXTO,
                @O_ID = @V_ID OUTPUT,
                @O_FILAS_AFECTADAS = @V_FILAS OUTPUT,
                @O_CODIGO_ERROR = @V_ERROR OUTPUT,
                @O_MENSAJE = @V_MENSAJE OUTPUT;

            IF @V_ERROR IS NOT NULL
                THROW 51000, @V_MENSAJE, 1;

            UPDATE @T_SOLICITUDES SET [ID_SOLICITUD] = @V_ID WHERE [ORDEN] = @V_ORDEN;
            SET @V_ORDEN += 1;
        END;

        DECLARE @V_GRUPO INT = 1;
        DECLARE @V_JSON NVARCHAR(MAX);

        WHILE @V_GRUPO <= 2
        BEGIN
            SELECT @V_JSON = N'[' + STRING_AGG
            (
                CONVERT(NVARCHAR(MAX), CONCAT(N'{"Order":', [ORDEN], N',"RequestId":', [ID_SOLICITUD], N'}')),
                N','
            ) WITHIN GROUP (ORDER BY [ORDEN]) + N']'
            FROM @T_SOLICITUDES
            WHERE [GRUPO_RUTA] = @V_GRUPO;

            SET @V_FECHA = DATEADD(DAY, @V_GRUPO, CAST(GETDATE() AS DATE));
            SET @V_TEXTO = CASE
                WHEN @V_GRUPO = 1 THEN N'DEMO_OFERTA_MOVIL: recorrido norte.'
                ELSE N'DEMO_OFERTA_MOVIL: recorrido industrial.'
            END;

            EXEC [LOGISTICA].[SP_LOGISTICA_COMMAND]
                @I_ACCION = N'RUTA_CREAR',
                @I_ID_EMPRESA = @V_ID_EMPRESA,
                @S_ID_SESION = 1,
                @S_ID_USUARIO = @V_ID_USUARIO,
                @I_FECHA = @V_FECHA,
                @I_TIPO = N'NORMAL',
                @I_TIPO_ASIGNACION = N'OFERTA',
                @I_OBSERVACION = @V_TEXTO,
                @I_JSON = @V_JSON,
                @O_ID = @V_ID OUTPUT,
                @O_FILAS_AFECTADAS = @V_FILAS OUTPUT,
                @O_CODIGO_ERROR = @V_ERROR OUTPUT,
                @O_MENSAJE = @V_MENSAJE OUTPUT;

            IF @V_ERROR IS NOT NULL
                THROW 51000, @V_MENSAJE, 1;

            SET @V_GRUPO += 1;
        END;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
