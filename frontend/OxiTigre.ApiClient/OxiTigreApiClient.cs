/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.ApiClient.OxiTigreApiClient
Archivo: OxiTigreApiClient.cs | Versión: 10.1.0 | Fecha: 2026-08-29 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Encapsula las llamadas HTTP compartidas hacia la API OxiTigre.
Historial: 1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Incorporación del cliente de seguridad.
Historial: 1.2.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Consulta de empresas disponibles luego de validar credenciales.
Historial: 2.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Ciclo completo de clientes y teléfonos.
Historial: 2.1.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Administración integral de usuarios y sesiones.
Historial: 2.2.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Catálogos, borradores y errores controlados.
Historial: 2.3.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Propagación del error controlado de cliente inexistente.
Historial: 3.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Cliente administrativo de Configuración e idioma.
Historial: 4.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Cliente tipado del módulo Inventario.
Historial: 6.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Cliente tipado para listas, pedidos y ventas.
Historial: 6.1.0 | 2026-08-21 | FABRICA | Agustin Omar Cauzi | Alta y edición tipada de promociones.
Historial: 7.0.0 | 2026-08-24 | FABRICA | Agustin Omar Cauzi | Cliente tipado del circuito de Compras.
Historial: 7.1.0 | 2026-08-26 | FABRICA | Agustin Omar Cauzi | Reversión de recepciones y documentación XML de Compras.
Historial: 8.0.0 | 2026-08-26 | FABRICA | Agustin Omar Cauzi | Cliente tipado del circuito logístico.
Historial: 8.1.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Gestión tipada de transportistas y vehículos.
Historial: 9.0.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Consulta administrativa global multiempresa.
Historial: 10.0.0 | 2026-08-28 | FABRICA | Agustin Omar Cauzi | Cliente móvil de recorridos propios del transportista.
Historial: 10.1.0 | 2026-08-29 | FABRICA | Agustin Omar Cauzi | Registro móvil de llegada e incidencias de parada.
===============================================================================
*/
using System.Net.Http.Headers;
using System.Net.Http.Json;
using OxiTigre.Contracts.Commercial;
using OxiTigre.Contracts.Configuration;
using OxiTigre.Contracts.Finance;
using OxiTigre.Contracts.Inventory;
using OxiTigre.Contracts.Logistics;
using OxiTigre.Contracts.Platform;
using OxiTigre.Contracts.Purchasing;
using OxiTigre.Contracts.Security;
using OxiTigre.Contracts.System;

namespace OxiTigre.ApiClient;

/// <summary>Proporciona acceso tipado a los endpoints públicos de OxiTigre.</summary>
/// <param name="httpClient">Cliente configurado con la URL base del ambiente.</param>
public sealed class OxiTigreApiClient(HttpClient httpClient)
{
    /// <summary>Obtiene solamente los recorridos activos del transportista autenticado.</summary>
    /// <param name="token">Token opaco de la sesión móvil.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Perfil, hojas, paradas y activos asignados.</returns>
    /// <exception cref="HttpRequestException">La API rechazó la consulta.</exception>
    public Task<DriverSnapshotResponse> GetDriverAsync(
        string token,
        CancellationToken cancellationToken = default
    ) => GetAuthorizedAsync<DriverSnapshotResponse>("api/driver", token, cancellationToken);

    /// <summary>Obtiene indicadores separados de todas las empresas registradas.</summary>
    /// <param name="token">Token opaco de una sesión administradora.</param>
    /// <param name="cancellationToken">Token que permite cancelar la consulta.</param>
    /// <returns>Vista global sin mezclar entidades operativas.</returns>
    /// <exception cref="HttpRequestException">La API rechazó la consulta o no está disponible.</exception>
    public Task<PlatformOverviewResponse> GetPlatformOverviewAsync(
        string token,
        CancellationToken cancellationToken = default
    ) =>
        GetAuthorizedAsync<PlatformOverviewResponse>(
            "api/platform/overview",
            token,
            cancellationToken
        );

    /// <summary>Obtiene agenda, hojas históricas, eventos y avisos logísticos.</summary>
    /// <param name="token">Token opaco de la sesión.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Snapshot completo del módulo.</returns>
    /// <exception cref="HttpRequestException">La API rechazó la consulta.</exception>
    public Task<LogisticsSnapshotResponse> GetLogisticsAsync(
        string token,
        CancellationToken cancellationToken = default
    ) => GetAuthorizedAsync<LogisticsSnapshotResponse>("api/logistics", token, cancellationToken);

    /// <summary>Registra un domicilio operativo.</summary>
    /// <param name="token">Token opaco de la sesión.</param>
    /// <param name="request">Domicilio y ventana habitual.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Identificador del domicilio.</returns>
    /// <exception cref="HttpRequestException">La API rechazó la operación.</exception>
    public Task<SavedLogisticsResponse> SaveLogisticsAddressAsync(
        string token,
        SaveLogisticsAddressRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync<SavedLogisticsResponse>(
            HttpMethod.Post,
            "api/logistics/addresses",
            token,
            request,
            cancellationToken
        );

    /// <summary>Crea una solicitud logística.</summary>
    /// <param name="token">Token opaco de la sesión.</param>
    /// <param name="request">Agenda, instrucciones y custodia.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Identificador de la solicitud.</returns>
    /// <exception cref="HttpRequestException">La API rechazó la operación.</exception>
    public Task<SavedLogisticsResponse> CreateLogisticsRequestAsync(
        string token,
        CreateLogisticsRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync<SavedLogisticsResponse>(
            HttpMethod.Post,
            "api/logistics/requests",
            token,
            request,
            cancellationToken
        );

    /// <summary>Crea o modifica un perfil transportista.</summary>
    /// <param name="token">Token opaco de la sesión.</param>
    /// <param name="request">Usuario, licencia, vínculo, vigencia y estado.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Identificador del perfil afectado.</returns>
    /// <exception cref="HttpRequestException">La API rechazó la operación.</exception>
    public Task<SavedLogisticsResponse> SaveTransporterAsync(
        string token,
        SaveTransporterRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync<SavedLogisticsResponse>(
            HttpMethod.Post,
            "api/logistics/transporters",
            token,
            request,
            cancellationToken
        );

    /// <summary>Crea o modifica un vehículo logístico.</summary>
    /// <param name="token">Token opaco de la sesión.</param>
    /// <param name="request">Patente, propiedad, características, vigencias y estado.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Identificador del vehículo afectado.</returns>
    /// <exception cref="HttpRequestException">La API rechazó la operación.</exception>
    public Task<SavedLogisticsResponse> SaveLogisticsVehicleAsync(
        string token,
        SaveLogisticsVehicleRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync<SavedLogisticsResponse>(
            HttpMethod.Post,
            "api/logistics/vehicles",
            token,
            request,
            cancellationToken
        );

    /// <summary>Crea una hoja manual.</summary>
    /// <param name="token">Token opaco de la sesión.</param>
    /// <param name="request">Cabecera y orden de paradas.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Identificador de la hoja.</returns>
    /// <exception cref="HttpRequestException">La API rechazó la operación.</exception>
    public Task<SavedLogisticsResponse> CreateRouteAsync(
        string token,
        CreateRouteRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync<SavedLogisticsResponse>(
            HttpMethod.Post,
            "api/logistics/routes",
            token,
            request,
            cancellationToken
        );

    /// <summary>Asigna desde la oficina una hoja publicada.</summary>
    /// <param name="token">Token opaco de la sesión.</param>
    /// <param name="id">Identificador de la hoja ofrecida.</param>
    /// <param name="request">Transportista, vehículo y versión consultada.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Tarea que finaliza al asignar la hoja.</returns>
    /// <exception cref="HttpRequestException">La API rechazó la asignación.</exception>
    public Task AssignRouteAsync(
        string token,
        long id,
        AssignRouteRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync(
            HttpMethod.Post,
            $"api/logistics/routes/{id}/assign",
            token,
            request,
            cancellationToken
        );

    /// <summary>Actualiza la agenda descriptiva de una hoja todavía ofrecida.</summary>
    /// <param name="token">Token opaco de la sesión.</param>
    /// <param name="id">Identificador de la hoja ofrecida.</param>
    /// <param name="request">Fecha, tipo, observación y versión consultada.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Tarea que finaliza al modificar la oferta.</returns>
    /// <exception cref="HttpRequestException">La API rechazó la modificación.</exception>
    public Task UpdateRouteOfferAsync(
        string token,
        long id,
        UpdateRouteOfferRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync(
            HttpMethod.Put,
            $"api/logistics/routes/{id}/offer",
            token,
            request,
            cancellationToken
        );

    /// <summary>Asigna atómicamente una oferta al transportista autenticado.</summary>
    /// <param name="token">Token opaco de la sesión móvil.</param>
    /// <param name="id">Identificador de la oferta.</param>
    /// <param name="request">Vehículo elegido y versión consultada.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Tarea que finaliza al tomar la oferta.</returns>
    /// <exception cref="HttpRequestException">La oferta cambió o el vehículo no está habilitado.</exception>
    public Task ClaimRouteOfferAsync(
        string token,
        long id,
        ClaimRouteOfferRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync(
            HttpMethod.Post,
            $"api/driver/offers/{id}/claim",
            token,
            request,
            cancellationToken
        );

    /// <summary>Despacha una hoja planificada.</summary>
    /// <param name="token">Token opaco de la sesión.</param>
    /// <param name="id">Identificador de la hoja.</param>
    /// <param name="request">Versión consultada.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Tarea que finaliza al confirmar el despacho.</returns>
    /// <exception cref="HttpRequestException">La API rechazó la transición.</exception>
    public Task DispatchRouteAsync(
        string token,
        long id,
        LogisticsTransitionRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync(
            HttpMethod.Post,
            $"api/logistics/routes/{id}/dispatch",
            token,
            request,
            cancellationToken
        );

    /// <summary>Cancela una hoja planificada.</summary>
    /// <param name="token">Token opaco de la sesión.</param>
    /// <param name="id">Identificador de la hoja.</param>
    /// <param name="request">Versión y motivo.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Tarea que finaliza al confirmar la cancelación.</returns>
    /// <exception cref="HttpRequestException">La API rechazó la transición.</exception>
    public Task CancelRouteAsync(
        string token,
        long id,
        LogisticsTransitionRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync(
            HttpMethod.Post,
            $"api/logistics/routes/{id}/cancel",
            token,
            request,
            cancellationToken
        );

    /// <summary>Pausa una hoja despachada sin perder su carga ni sus paradas.</summary>
    /// <param name="token">Token opaco de la sesión.</param>
    /// <param name="id">Identificador de la hoja.</param>
    /// <param name="request">Versión y motivo de la pausa.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Tarea que finaliza al pausar.</returns>
    /// <exception cref="HttpRequestException">La API rechazó la transición.</exception>
    public Task PauseRouteAsync(
        string token,
        long id,
        LogisticsTransitionRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync(
            HttpMethod.Post,
            $"api/logistics/routes/{id}/pause",
            token,
            request,
            cancellationToken
        );

    /// <summary>Reanuda una hoja pausada.</summary>
    /// <param name="token">Token opaco de la sesión.</param>
    /// <param name="id">Identificador de la hoja.</param>
    /// <param name="request">Versión y observación de la reanudación.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Tarea que finaliza al reanudar.</returns>
    /// <exception cref="HttpRequestException">La API rechazó la transición.</exception>
    public Task ResumeRouteAsync(
        string token,
        long id,
        LogisticsTransitionRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync(
            HttpMethod.Post,
            $"api/logistics/routes/{id}/resume",
            token,
            request,
            cancellationToken
        );

    /// <summary>Registra la llegada del transportista a una parada propia.</summary>
    /// <param name="token">Token opaco de la sesión.</param>
    /// <param name="id">Identificador de la parada.</param>
    /// <param name="request">Versión consultada.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Tarea que finaliza al registrar la hora.</returns>
    /// <exception cref="HttpRequestException">La API rechazó la transición.</exception>
    public Task MarkRouteStopArrivalAsync(
        string token,
        long id,
        LogisticsTransitionRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync(
            HttpMethod.Post,
            $"api/logistics/stops/{id}/arrival",
            token,
            request,
            cancellationToken
        );

    /// <summary>Conserva una incidencia informada durante una parada activa.</summary>
    /// <param name="token">Token opaco de la sesión.</param>
    /// <param name="id">Identificador de la parada.</param>
    /// <param name="request">Tipo, detalle y versión consultada.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Tarea que finaliza al conservar el evento.</returns>
    /// <exception cref="HttpRequestException">La API rechazó la operación.</exception>
    public Task ReportRouteStopIncidentAsync(
        string token,
        long id,
        ReportRouteStopIncidentRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync(
            HttpMethod.Post,
            $"api/logistics/stops/{id}/incidents",
            token,
            request,
            cancellationToken
        );

    /// <summary>Confirma el resultado de una parada.</summary>
    /// <param name="token">Token opaco de la sesión.</param>
    /// <param name="id">Identificador de la parada.</param>
    /// <param name="request">Resultado, observación y versión.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Tarea que finaliza al confirmar la parada.</returns>
    /// <exception cref="HttpRequestException">La API rechazó la confirmación.</exception>
    public Task CompleteRouteStopAsync(
        string token,
        long id,
        CompleteRouteStopRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync(
            HttpMethod.Post,
            $"api/logistics/stops/{id}/complete",
            token,
            request,
            cancellationToken
        );

    /// <summary>Obtiene caja, cobros y cuenta corriente de la empresa autenticada.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Cajas, sesiones, medios de pago, cobros, ventas pendientes y movimientos de la empresa autenticada.</returns>
    public Task<FinanceSnapshotResponse> GetFinanceAsync(
        string token,
        CancellationToken cancellationToken = default
    ) => GetAuthorizedAsync<FinanceSnapshotResponse>("api/finance", token, cancellationToken);

    /// <summary>Crea o modifica un medio de pago.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="request">Código, tipo, reglas de referencia y estado del medio de pago.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador del medio de pago creado o actualizado.</returns>
    public Task<SavedFinanceResponse> SavePaymentMethodAsync(
        string token,
        SavePaymentMethodRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync<SavedFinanceResponse>(
            HttpMethod.Post,
            "api/finance/payment-methods",
            token,
            request,
            cancellationToken
        );

    /// <summary>Crea o modifica una caja física.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="request">Sucursal, código, moneda, estado y versión de la caja.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador de la caja creada o actualizada.</returns>
    public Task<SavedFinanceResponse> SaveCashBoxAsync(
        string token,
        SaveCashBoxRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync<SavedFinanceResponse>(
            HttpMethod.Post,
            "api/finance/cash-boxes",
            token,
            request,
            cancellationToken
        );

    /// <summary>Abre una caja para la sesión autenticada.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="request">Caja, importe inicial y observación de la apertura.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador de la apertura de caja registrada.</returns>
    public Task<SavedFinanceResponse> OpenCashSessionAsync(
        string token,
        OpenCashSessionRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync<SavedFinanceResponse>(
            HttpMethod.Post,
            "api/finance/cash-sessions/open",
            token,
            request,
            cancellationToken
        );

    /// <summary>Cierra una apertura de caja con su arqueo.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="id">Identificador del recurso indicado por la operación.</param>
    /// <param name="request">Importe contado, observación y versión con que se solicita el cierre de caja.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    public Task CloseCashSessionAsync(
        string token,
        long id,
        CloseCashSessionRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync(
            HttpMethod.Post,
            $"api/finance/cash-sessions/{id}/close",
            token,
            request,
            cancellationToken
        );

    /// <summary>Registra un cobro y sus aplicaciones.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="request">Cliente, total, medios de pago y aplicaciones a ventas del cobro.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador del cobro confirmado.</returns>
    public Task<SavedFinanceResponse> RegisterCustomerPaymentAsync(
        string token,
        RegisterCustomerPaymentRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync<SavedFinanceResponse>(
            HttpMethod.Post,
            "api/finance/payments",
            token,
            request,
            cancellationToken
        );

    /// <summary>Revierte un cobro mediante compensación.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="id">Identificador del recurso indicado por la operación.</param>
    /// <param name="request">Motivo obligatorio y versión del cobro que se compensará.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    public Task ReverseCustomerPaymentAsync(
        string token,
        long id,
        ReverseCustomerPaymentRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync(
            HttpMethod.Post,
            $"api/finance/payments/{id}/reverse",
            token,
            request,
            cancellationToken
        );

    /// <summary>Consulta el estado actual de la API.</summary>
    /// <param name="cancellationToken">Permite cancelar la operación HTTP.</param>
    /// <returns>El estado informado por la API o <see langword="null"/> si no existe contenido.</returns>
    public async Task<HealthResponse?> GetHealthAsync(
        CancellationToken cancellationToken = default
    ) => await httpClient.GetFromJsonAsync<HealthResponse>("health", cancellationToken);

    /// <summary>Valida las credenciales y devuelve las empresas disponibles para elegir.</summary>
    /// <param name="request">Usuario y contraseña usados para consultar empresas habilitadas.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Empresas y sucursales habilitadas para las credenciales, o null si la API las rechaza.</returns>
    public async Task<IReadOnlyList<CompanyOptionResponse>?> GetCompaniesAsync(
        CredentialsRequest request,
        CancellationToken cancellationToken = default
    )
    {
        using var response = await httpClient.PostAsJsonAsync(
            "api/security/companies",
            request,
            cancellationToken
        );
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            return null;
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<List<CompanyOptionResponse>>(
            cancellationToken
        );
    }

    /// <summary>Inicia sesión y devuelve <see langword="null"/> cuando las credenciales son inválidas.</summary>
    /// <param name="request">Empresa, sucursal y credenciales con las que se solicita la sesión.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Sesión autenticada y token opaco, o null si el acceso fue rechazado.</returns>
    public async Task<LoginResponse?> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default
    )
    {
        using var response = await httpClient.PostAsJsonAsync(
            "api/security/login",
            request,
            cancellationToken
        );
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            return null;
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken);
    }

    /// <summary>Cambia la contraseña y devuelve falso cuando la credencial actual es inválida.</summary>
    /// <param name="request">Empresa, usuario, contraseña actual y nueva contraseña que se enviarán para el cambio.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>True si se cambió la contraseña; false si la credencial actual fue rechazada.</returns>
    public async Task<bool> ChangePasswordAsync(
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default
    )
    {
        using var response = await httpClient.PostAsJsonAsync(
            "api/security/change-password",
            request,
            cancellationToken
        );
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            return false;
        await EnsureSuccessAsync(response, cancellationToken);
        return true;
    }

    /// <summary>Valida un token opaco y recupera su sesión vigente.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Datos de la sesión vigente, o null si el token fue rechazado.</returns>
    public async Task<SessionResponse?> GetSessionAsync(
        string token,
        CancellationToken cancellationToken = default
    )
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/security/session");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            return null;
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<SessionResponse>(cancellationToken);
    }

    /// <summary>Lista clientes activos autorizados para la sesión indicada.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="statusCode">Código de estado admitido por el dominio.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Clientes filtrados por el estado solicitado.</returns>
    public async Task<IReadOnlyList<ClientSummaryResponse>> GetClientsAsync(
        string token,
        string statusCode = "ACTIVO",
        CancellationToken cancellationToken = default
    )
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/commercial/clients?status={Uri.EscapeDataString(statusCode)}"
        );
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<List<ClientSummaryResponse>>(
                cancellationToken
            ) ?? [];
    }

    /// <summary>Obtiene documentos, países y tipos de teléfono parametrizados.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Catálogos necesarios para cargar y editar clientes.</returns>
    /// <exception cref="InvalidDataException">El contenido recibido no cumple el formato esperado.</exception>
    public async Task<ClientCatalogResponse> GetClientCatalogsAsync(
        string token,
        CancellationToken cancellationToken = default
    )
    {
        using var request = Authorized(HttpMethod.Get, "api/commercial/client-catalogs", token);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<ClientCatalogResponse>(cancellationToken)
            ?? throw new InvalidDataException("La API no devolvió los catálogos de clientes.");
    }

    /// <summary>Obtiene la precarga incompleta del usuario actual.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Borrador del usuario autenticado, o null si no existe.</returns>
    public async Task<SavedClientDraftResponse?> GetClientDraftAsync(
        string token,
        CancellationToken cancellationToken = default
    )
    {
        using var request = Authorized(HttpMethod.Get, "api/commercial/client-draft", token);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            return null;
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<SavedClientDraftResponse>(
            cancellationToken
        );
    }

    /// <summary>Guarda o reemplaza la precarga incompleta del usuario actual.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="draft">Identificación, contacto y demás datos del cliente que se conservarán.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    public async Task SaveClientDraftAsync(
        string token,
        SaveClientRequest draft,
        CancellationToken cancellationToken = default
    )
    {
        using var request = Authorized(HttpMethod.Put, "api/commercial/client-draft", token, draft);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    /// <summary>Descarta la precarga incompleta del usuario actual.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    public async Task DeleteClientDraftAsync(
        string token,
        CancellationToken cancellationToken = default
    )
    {
        using var request = Authorized(HttpMethod.Delete, "api/commercial/client-draft", token);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    /// <summary>Obtiene el detalle editable de un cliente y sus teléfonos.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="clientId">Identificador del cliente, verificado dentro de la empresa activa.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Ficha, contactos y teléfonos del cliente solicitado.</returns>
    /// <exception cref="InvalidDataException">El contenido recibido no cumple el formato esperado.</exception>
    public async Task<ClientDetailResponse> GetClientAsync(
        string token,
        long clientId,
        CancellationToken cancellationToken = default
    )
    {
        using var request = Authorized(HttpMethod.Get, $"api/commercial/clients/{clientId}", token);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<ClientDetailResponse>(cancellationToken)
            ?? throw new InvalidDataException("La API no devolvió el cliente solicitado.");
    }

    /// <summary>Crea un cliente con todos sus teléfonos.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="client">Identificación, contacto y demás datos del cliente que se conservarán.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    public async Task CreateClientAsync(
        string token,
        SaveClientRequest client,
        CancellationToken cancellationToken = default
    )
    {
        using var request = Authorized(HttpMethod.Post, "api/commercial/clients", token, client);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    /// <summary>Actualiza datos generales y teléfonos de un cliente.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="clientId">Identificador del cliente, verificado dentro de la empresa activa.</param>
    /// <param name="client">Identificación, contacto y demás datos del cliente que se conservarán.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    public async Task UpdateClientAsync(
        string token,
        long clientId,
        SaveClientRequest client,
        CancellationToken cancellationToken = default
    )
    {
        using var request = Authorized(
            HttpMethod.Put,
            $"api/commercial/clients/{clientId}",
            token,
            client
        );
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    /// <summary>Cambia el estado lógico de un cliente.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="clientId">Identificador del cliente, verificado dentro de la empresa activa.</param>
    /// <param name="statusCode">Código de estado admitido por el dominio.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    public async Task ChangeClientStatusAsync(
        string token,
        long clientId,
        string statusCode,
        CancellationToken cancellationToken = default
    )
    {
        using var request = Authorized(
            HttpMethod.Patch,
            $"api/commercial/clients/{clientId}/status",
            token,
            new ChangeClientStatusRequest(statusCode)
        );
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    /// <summary>Lista los usuarios de la empresa de la sesión administradora.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Usuarios visibles para la administración de accesos.</returns>
    public async Task<IReadOnlyList<UserSummaryResponse>> GetUsersAsync(
        string token,
        CancellationToken cancellationToken = default
    )
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/security/users");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<List<UserSummaryResponse>>(
                cancellationToken
            ) ?? [];
    }

    /// <summary>Crea un usuario funcional y devuelve su identificador.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="request">Identidad, correo, roles y contraseña temporal del nuevo usuario.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    public async Task CreateUserAsync(
        string token,
        CreateUserRequest request,
        CancellationToken cancellationToken = default
    )
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "api/security/users")
        {
            Content = JsonContent.Create(request),
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await httpClient.SendAsync(message, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    /// <summary>Lista los roles activos que el administrador puede asignar.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Roles disponibles para asignar a usuarios.</returns>
    public async Task<IReadOnlyList<RoleSummaryResponse>> GetRolesAsync(
        string token,
        CancellationToken cancellationToken = default
    )
    {
        using var request = Authorized(HttpMethod.Get, "api/security/roles", token);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<List<RoleSummaryResponse>>(
                cancellationToken
            ) ?? [];
    }

    /// <summary>Actualiza datos, estado y roles de un usuario.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="user">Identidad, correo, roles, estado y versión del usuario que se editará.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    public async Task UpdateUserAsync(
        string token,
        long userId,
        UpdateUserRequest user,
        CancellationToken cancellationToken = default
    )
    {
        using var request = Authorized(HttpMethod.Put, $"api/security/users/{userId}", token, user);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    /// <summary>Reinicia administrativamente una contraseña y revoca las sesiones anteriores.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="temporaryPassword">Contraseña temporal que deberá cambiarse al ingresar.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    public async Task ResetUserPasswordAsync(
        string token,
        long userId,
        string temporaryPassword,
        CancellationToken cancellationToken = default
    )
    {
        using var request = Authorized(
            HttpMethod.Post,
            $"api/security/users/{userId}/reset-password",
            token,
            new ResetUserPasswordRequest(temporaryPassword)
        );
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    /// <summary>Revoca todas las sesiones vigentes del usuario indicado.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="userId">Identificador del usuario responsable de la operación.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    public async Task RevokeUserSessionsAsync(
        string token,
        long userId,
        CancellationToken cancellationToken = default
    )
    {
        using var request = Authorized(
            HttpMethod.Post,
            $"api/security/users/{userId}/revoke-sessions",
            token
        );
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    /// <summary>Cierra la sesión identificada por el token.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    public async Task LogoutAsync(string token, CancellationToken cancellationToken = default)
    {
        using var request = Authorized(HttpMethod.Post, "api/security/logout", token);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    /// <summary>Obtiene toda la configuración administrable para la empresa autenticada.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Empresas, sucursales, catálogos y parámetros configurables.</returns>
    public Task<ConfigurationSnapshotResponse> GetConfigurationAsync(
        string token,
        CancellationToken cancellationToken = default
    ) =>
        GetAuthorizedAsync<ConfigurationSnapshotResponse>(
            "api/configuration",
            token,
            cancellationToken
        );

    /// <summary>Obtiene el idioma preferido del usuario actual.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Idioma preferido que la API tiene guardado para la sesión.</returns>
    public Task<LanguagePreferenceResponse> GetLanguagePreferenceAsync(
        string token,
        CancellationToken cancellationToken = default
    ) =>
        GetAuthorizedAsync<LanguagePreferenceResponse>(
            "api/configuration/language",
            token,
            cancellationToken
        );

    /// <summary>Guarda el idioma preferido del usuario actual.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="request">Cultura preferida que se guardará para el usuario.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    public Task SaveLanguagePreferenceAsync(
        string token,
        SaveLanguagePreferenceRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync(
            HttpMethod.Put,
            "api/configuration/language",
            token,
            request,
            cancellationToken
        );

    /// <summary>Actualiza la empresa propietaria de la sesión.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="request">Datos de la empresa y versión que se actualizarán.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    public Task UpdateCompanyConfigurationAsync(
        string token,
        UpdateCompanyConfigurationRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync(
            HttpMethod.Put,
            "api/configuration/company",
            token,
            request,
            cancellationToken
        );

    /// <summary>Crea o actualiza una sucursal.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="request">Datos de sucursal y versión para crearla o modificarla.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador de la sucursal creada o actualizada.</returns>
    public Task<SavedConfigurationResponse> SaveBranchConfigurationAsync(
        string token,
        SaveBranchConfigurationRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync<SavedConfigurationResponse>(
            HttpMethod.Post,
            "api/configuration/branches",
            token,
            request,
            cancellationToken
        );

    /// <summary>Crea o actualiza una unidad operativa.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="request">Código, descripción y estado de la unidad operativa.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador de la unidad operativa creada o actualizada.</returns>
    public Task<SavedConfigurationResponse> SaveOperatingUnitConfigurationAsync(
        string token,
        SaveOperatingUnitConfigurationRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync<SavedConfigurationResponse>(
            HttpMethod.Post,
            "api/configuration/operating-units",
            token,
            request,
            cancellationToken
        );

    /// <summary>Crea o actualiza un tipo de teléfono.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="request">Código, nombre y estado del tipo de teléfono.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador del tipo de teléfono creado o actualizado.</returns>
    public Task<SavedConfigurationResponse> SavePhoneTypeConfigurationAsync(
        string token,
        SavePhoneTypeConfigurationRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync<SavedConfigurationResponse>(
            HttpMethod.Post,
            "api/configuration/phone-types",
            token,
            request,
            cancellationToken
        );

    /// <summary>Actualiza un estado informativo.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="request">Código, nombre y versión del estado configurable.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    public Task UpdateStateConfigurationAsync(
        string token,
        UpdateStateConfigurationRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync(
            HttpMethod.Put,
            "api/configuration/states",
            token,
            request,
            cancellationToken
        );

    /// <summary>Crea o actualiza un parámetro tipado.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="request">Clave, valor y versión del parámetro del sistema.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador del parámetro de sistema creado o actualizado.</returns>
    public Task<SavedConfigurationResponse> SaveSystemParameterConfigurationAsync(
        string token,
        SaveSystemParameterConfigurationRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync<SavedConfigurationResponse>(
            HttpMethod.Post,
            "api/configuration/parameters",
            token,
            request,
            cancellationToken
        );

    /// <summary>Crea o actualiza un módulo.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="request">Módulo, estado y versión que se configurarán.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador del módulo configurado.</returns>
    public Task<SavedConfigurationResponse> SaveModuleConfigurationAsync(
        string token,
        SaveModuleConfigurationRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync<SavedConfigurationResponse>(
            HttpMethod.Post,
            "api/configuration/modules",
            token,
            request,
            cancellationToken
        );

    /// <summary>Crea o actualiza una traducción de catálogo.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="request">Cultura, catálogo, clave y texto traducido que se guardarán.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador de la traducción de catálogo guardada.</returns>
    public Task<SavedConfigurationResponse> SaveCatalogTranslationAsync(
        string token,
        SaveCatalogTranslationRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync<SavedConfigurationResponse>(
            HttpMethod.Post,
            "api/configuration/translations",
            token,
            request,
            cancellationToken
        );

    /// <summary>Crea un código de error y devuelve el código generado.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="request">Código y descripción del error funcional que se incorporará al catálogo.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Código funcional asignado al nuevo error.</returns>
    public Task<CreatedErrorCodeResponse> CreateErrorCatalogAsync(
        string token,
        CreateErrorCatalogRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync<CreatedErrorCodeResponse>(
            HttpMethod.Post,
            "api/audit/errors",
            token,
            request,
            cancellationToken
        );

    /// <summary>Actualiza el diagnóstico de un código de error.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="request">Código, descripción, estado y versión del error funcional.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    public Task UpdateErrorCatalogAsync(
        string token,
        UpdateErrorCatalogRequest request,
        CancellationToken cancellationToken = default
    ) => SendAuthorizedAsync(HttpMethod.Put, "api/audit/errors", token, request, cancellationToken);

    /// <summary>Obtiene maestros, saldos, alertas y movimientos de Inventario.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Unidades, categorías, productos, depósitos, ubicaciones y existencias visibles.</returns>
    public Task<InventorySnapshotResponse> GetInventoryAsync(
        string token,
        CancellationToken cancellationToken = default
    ) => GetAuthorizedAsync<InventorySnapshotResponse>("api/inventory", token, cancellationToken);

    /// <summary>Crea o actualiza una unidad de medida.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="request">Código, descripción y estado de la unidad de medida.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador de la unidad de medida creada o actualizada.</returns>
    public Task<SavedInventoryResponse> SaveMeasurementUnitAsync(
        string token,
        SaveMeasurementUnitRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync<SavedInventoryResponse>(
            HttpMethod.Post,
            "api/inventory/measurement-units",
            token,
            request,
            cancellationToken
        );

    /// <summary>Crea o actualiza una categoría de producto.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="request">Código, nombre y estado de la categoría de productos.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador de la categoría creada o actualizada.</returns>
    public Task<SavedInventoryResponse> SaveProductCategoryAsync(
        string token,
        SaveProductCategoryRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync<SavedInventoryResponse>(
            HttpMethod.Post,
            "api/inventory/categories",
            token,
            request,
            cancellationToken
        );

    /// <summary>Crea o actualiza un producto.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="request">Código, categoría, unidad y datos comerciales del producto.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador del producto creado o actualizado.</returns>
    public Task<SavedInventoryResponse> SaveProductAsync(
        string token,
        SaveProductRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync<SavedInventoryResponse>(
            HttpMethod.Post,
            "api/inventory/products",
            token,
            request,
            cancellationToken
        );

    /// <summary>Crea o actualiza un depósito.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="request">Código, nombre, domicilio y estado del depósito.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador del depósito creado o actualizado.</returns>
    public Task<SavedInventoryResponse> SaveWarehouseAsync(
        string token,
        SaveWarehouseRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync<SavedInventoryResponse>(
            HttpMethod.Post,
            "api/inventory/warehouses",
            token,
            request,
            cancellationToken
        );

    /// <summary>Crea o actualiza una ubicación.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="request">Depósito, código, nombre y estado de la ubicación.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador de la ubicación creada o actualizada.</returns>
    public Task<SavedInventoryResponse> SaveWarehouseLocationAsync(
        string token,
        SaveWarehouseLocationRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync<SavedInventoryResponse>(
            HttpMethod.Post,
            "api/inventory/locations",
            token,
            request,
            cancellationToken
        );

    /// <summary>Actualiza el mínimo de alerta de una existencia.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="stockBalanceId">Identificador de la posición de existencias por producto y depósito.</param>
    /// <param name="request">Producto, depósito y nuevo umbral de stock mínimo.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    public Task UpdateMinimumStockAsync(
        string token,
        long stockBalanceId,
        UpdateMinimumStockRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync(
            HttpMethod.Put,
            $"api/inventory/stock/{stockBalanceId}/minimum",
            token,
            request,
            cancellationToken
        );

    /// <summary>Confirma un movimiento y devuelve su identificador.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="request">Producto, depósito, cantidad, motivo y referencias del movimiento.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador del movimiento de inventario confirmado.</returns>
    public Task<SavedInventoryResponse> CreateInventoryMovementAsync(
        string token,
        CreateInventoryMovementRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync<SavedInventoryResponse>(
            HttpMethod.Post,
            "api/inventory/movements",
            token,
            request,
            cancellationToken
        );

    /// <summary>Obtiene listas, precios, promociones, pedidos y ventas de la empresa autenticada.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Clientes, listas de precios, promociones, pedidos y ventas de la empresa.</returns>
    public Task<SalesSnapshotResponse> GetSalesAsync(
        string token,
        CancellationToken cancellationToken = default
    ) =>
        GetAuthorizedAsync<SalesSnapshotResponse>("api/commercial/sales", token, cancellationToken);

    /// <summary>Crea o actualiza una lista de precios.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="request">Nombre, vigencia, moneda y estado de la lista de precios.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador de la lista de precios creada o actualizada.</returns>
    public Task<SavedCommercialResponse> SavePriceListAsync(
        string token,
        SavePriceListRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync<SavedCommercialResponse>(
            HttpMethod.Post,
            "api/commercial/price-lists",
            token,
            request,
            cancellationToken
        );

    /// <summary>Crea o actualiza un precio de producto.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="request">Producto, lista y precio vigente que se guardarán.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador del precio de producto guardado en la lista.</returns>
    public Task<SavedCommercialResponse> SavePriceAsync(
        string token,
        SavePriceListProductRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync<SavedCommercialResponse>(
            HttpMethod.Post,
            "api/commercial/prices",
            token,
            request,
            cancellationToken
        );

    /// <summary>Crea o actualiza una promoción.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="request">Vigencia, condición y beneficio de la promoción.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador de la promoción creada o actualizada.</returns>
    public Task<SavedCommercialResponse> SavePromotionAsync(
        string token,
        SavePromotionRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync<SavedCommercialResponse>(
            HttpMethod.Post,
            "api/commercial/promotions",
            token,
            request,
            cancellationToken
        );

    /// <summary>Crea o actualiza un pedido borrador.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="request">Cabecera, cliente y renglones valorizados del pedido.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador del pedido creado o actualizado.</returns>
    public Task<SavedCommercialResponse> SaveOrderAsync(
        string token,
        SaveOrderRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync<SavedCommercialResponse>(
            HttpMethod.Post,
            "api/commercial/orders",
            token,
            request,
            cancellationToken
        );

    /// <summary>Confirma un pedido y reserva su stock.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="orderId">Identificador del pedido que se consulta o modifica.</param>
    /// <param name="request">Versión del pedido leída antes de cambiar su estado.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    public Task ConfirmOrderAsync(
        string token,
        long orderId,
        OrderTransitionRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync(
            HttpMethod.Post,
            $"api/commercial/orders/{orderId}/confirm",
            token,
            request,
            cancellationToken
        );

    /// <summary>Cancela un pedido y libera reservas.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="orderId">Identificador del pedido que se consulta o modifica.</param>
    /// <param name="request">Versión del pedido leída antes de cambiar su estado.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    public Task CancelOrderAsync(
        string token,
        long orderId,
        OrderTransitionRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync(
            HttpMethod.Post,
            $"api/commercial/orders/{orderId}/cancel",
            token,
            request,
            cancellationToken
        );

    /// <summary>Convierte un pedido confirmado en venta interna.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="orderId">Identificador del pedido que se consulta o modifica.</param>
    /// <param name="request">Fecha de venta y versión del pedido confirmado que se facturará internamente.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador de la venta generada a partir del pedido.</returns>
    public Task<SavedCommercialResponse> CreateSaleAsync(
        string token,
        long orderId,
        CreateSaleRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync<SavedCommercialResponse>(
            HttpMethod.Post,
            $"api/commercial/orders/{orderId}/sale",
            token,
            request,
            cancellationToken
        );

    /// <summary>Obtiene proveedores, órdenes y recepciones de Compras.</summary>
    /// <param name="token">Token de la sesión autenticada.</param>
    /// <param name="cancellationToken">Token que permite cancelar la solicitud HTTP.</param>
    /// <returns>Información operativa de Compras disponible para la empresa de la sesión.</returns>
    /// <exception cref="HttpRequestException">La API rechazó la solicitud o no pudo responder.</exception>
    public Task<PurchasingSnapshotResponse> GetPurchasingAsync(
        string token,
        CancellationToken cancellationToken = default
    ) => GetAuthorizedAsync<PurchasingSnapshotResponse>("api/purchasing", token, cancellationToken);

    /// <summary>Crea o actualiza un proveedor.</summary>
    /// <param name="token">Token de la sesión autenticada.</param>
    /// <param name="request">Datos completos del proveedor y sus contactos activos.</param>
    /// <param name="cancellationToken">Token que permite cancelar la solicitud HTTP.</param>
    /// <returns>Identificador del proveedor creado o actualizado.</returns>
    /// <exception cref="HttpRequestException">La API rechazó la solicitud o no pudo responder.</exception>
    public Task<SavedPurchasingResponse> SaveSupplierAsync(
        string token,
        SaveSupplierRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync<SavedPurchasingResponse>(
            HttpMethod.Post,
            "api/purchasing/suppliers",
            token,
            request,
            cancellationToken
        );

    /// <summary>Crea o actualiza una orden de compra en borrador.</summary>
    /// <param name="token">Token de la sesión autenticada.</param>
    /// <param name="request">Cabecera y renglones valorizados de la orden.</param>
    /// <param name="cancellationToken">Token que permite cancelar la solicitud HTTP.</param>
    /// <returns>Identificador de la orden creada o actualizada.</returns>
    /// <exception cref="HttpRequestException">La API rechazó la solicitud o no pudo responder.</exception>
    public Task<SavedPurchasingResponse> SavePurchaseOrderAsync(
        string token,
        SavePurchaseOrderRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync<SavedPurchasingResponse>(
            HttpMethod.Post,
            "api/purchasing/orders",
            token,
            request,
            cancellationToken
        );

    /// <summary>Envía una orden a aprobación.</summary>
    /// <param name="token">Token de la sesión autenticada.</param>
    /// <param name="orderId">Identificador de la orden en borrador.</param>
    /// <param name="request">Versión utilizada para controlar ediciones concurrentes.</param>
    /// <param name="cancellationToken">Token que permite cancelar la solicitud HTTP.</param>
    /// <returns>Tarea que finaliza al confirmarse la transición.</returns>
    /// <exception cref="HttpRequestException">La API rechazó la solicitud o no pudo responder.</exception>
    public Task SubmitPurchaseOrderAsync(
        string token,
        long orderId,
        PurchaseOrderTransitionRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync(
            HttpMethod.Post,
            $"api/purchasing/orders/{orderId}/submit",
            token,
            request,
            cancellationToken
        );

    /// <summary>Aprueba una orden pendiente.</summary>
    /// <param name="token">Token de la sesión autenticada.</param>
    /// <param name="orderId">Identificador de la orden pendiente de aprobación.</param>
    /// <param name="request">Versión utilizada para controlar ediciones concurrentes.</param>
    /// <param name="cancellationToken">Token que permite cancelar la solicitud HTTP.</param>
    /// <returns>Tarea que finaliza al confirmarse la aprobación.</returns>
    /// <exception cref="HttpRequestException">La API rechazó la solicitud o no pudo responder.</exception>
    public Task ApprovePurchaseOrderAsync(
        string token,
        long orderId,
        PurchaseOrderTransitionRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync(
            HttpMethod.Post,
            $"api/purchasing/orders/{orderId}/approve",
            token,
            request,
            cancellationToken
        );

    /// <summary>Cancela una orden conservando su historial.</summary>
    /// <param name="token">Token de la sesión autenticada.</param>
    /// <param name="orderId">Identificador de la orden.</param>
    /// <param name="request">Versión utilizada para controlar ediciones concurrentes.</param>
    /// <param name="cancellationToken">Token que permite cancelar la solicitud HTTP.</param>
    /// <returns>Tarea que finaliza al confirmarse la cancelación.</returns>
    /// <exception cref="HttpRequestException">La API rechazó la solicitud o no pudo responder.</exception>
    public Task CancelPurchaseOrderAsync(
        string token,
        long orderId,
        PurchaseOrderTransitionRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync(
            HttpMethod.Post,
            $"api/purchasing/orders/{orderId}/cancel",
            token,
            request,
            cancellationToken
        );

    /// <summary>Cierra el saldo pendiente de una orden con motivo obligatorio.</summary>
    /// <param name="token">Token de la sesión autenticada.</param>
    /// <param name="orderId">Identificador de la orden parcialmente recibida.</param>
    /// <param name="request">Motivo del cierre y versión utilizada para controlar concurrencia.</param>
    /// <param name="cancellationToken">Token que permite cancelar la solicitud HTTP.</param>
    /// <returns>Tarea que finaliza al confirmarse el cierre.</returns>
    /// <exception cref="HttpRequestException">La API rechazó la solicitud o no pudo responder.</exception>
    public Task ClosePurchaseOrderAsync(
        string token,
        long orderId,
        ClosePurchaseOrderRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync(
            HttpMethod.Post,
            $"api/purchasing/orders/{orderId}/close",
            token,
            request,
            cancellationToken
        );

    /// <summary>Confirma una recepción total o parcial.</summary>
    /// <param name="token">Token de la sesión autenticada.</param>
    /// <param name="orderId">Identificador de la orden aprobada.</param>
    /// <param name="request">Documento, cantidades y trazabilidad realmente recibidos.</param>
    /// <param name="cancellationToken">Token que permite cancelar la solicitud HTTP.</param>
    /// <returns>Identificador de la recepción confirmada.</returns>
    /// <exception cref="HttpRequestException">La API rechazó la solicitud o no pudo responder.</exception>
    public Task<SavedPurchasingResponse> CreateGoodsReceiptAsync(
        string token,
        long orderId,
        CreateGoodsReceiptRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync<SavedPurchasingResponse>(
            HttpMethod.Post,
            $"api/purchasing/orders/{orderId}/receipts",
            token,
            request,
            cancellationToken
        );

    /// <summary>Compensa una recepción intacta conservando el documento original.</summary>
    /// <param name="token">Token de la sesión autenticada.</param>
    /// <param name="receiptId">Identificador de la recepción que se desea reversar.</param>
    /// <param name="request">Motivo y versión de la recepción.</param>
    /// <param name="cancellationToken">Token que permite cancelar la solicitud HTTP.</param>
    /// <returns>Tarea que finaliza cuando la reversión queda confirmada.</returns>
    /// <exception cref="HttpRequestException">La API rechazó la solicitud o no pudo responder.</exception>
    public Task ReverseGoodsReceiptAsync(
        string token,
        long receiptId,
        ReverseGoodsReceiptRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync(
            HttpMethod.Post,
            $"api/purchasing/receipts/{receiptId}/reverse",
            token,
            request,
            cancellationToken
        );

    /// <summary>Obtiene el estado almacenado y el historial industrial.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Activos, lotes, mediciones, fraccionamientos, incidencias y préstamos trazables.</returns>
    public Task<TraceabilitySnapshotResponse> GetTraceabilityAsync(
        string token,
        CancellationToken cancellationToken = default
    ) =>
        GetAuthorizedAsync<TraceabilitySnapshotResponse>(
            "api/inventory/traceability",
            token,
            cancellationToken
        );

    /// <summary>Registra un activo propiedad de un cliente y su ubicación inicial.</summary>
    /// <param name="token">Token de la sesión autenticada.</param>
    /// <param name="request">Propietario, producto, identificación y custodia inicial.</param>
    /// <param name="cancellationToken">Token que permite cancelar la solicitud HTTP.</param>
    /// <returns>Identificador asignado al activo.</returns>
    /// <exception cref="HttpRequestException">La API rechazó la solicitud o no pudo responder.</exception>
    public Task<SavedTraceabilityResponse> SaveClientAssetAsync(
        string token,
        SaveClientAssetRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync<SavedTraceabilityResponse>(
            HttpMethod.Post,
            "api/inventory/traceability/client-assets",
            token,
            request,
            cancellationToken
        );

    /// <summary>Confirma el ingreso o la entrega de un activo propiedad de un cliente.</summary>
    /// <param name="token">Token de la sesión autenticada.</param>
    /// <param name="assetId">Identificador del activo seleccionado.</param>
    /// <param name="request">Acción, depósito, observación y versión leída.</param>
    /// <param name="cancellationToken">Token que permite cancelar la solicitud HTTP.</param>
    /// <returns>Identificador del activo actualizado.</returns>
    /// <exception cref="HttpRequestException">La API rechazó la solicitud o no pudo responder.</exception>
    public Task<SavedTraceabilityResponse> ChangeClientAssetCustodyAsync(
        string token,
        long assetId,
        ChangeClientAssetCustodyRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync<SavedTraceabilityResponse>(
            HttpMethod.Post,
            $"api/inventory/traceability/client-assets/{assetId}/custody",
            token,
            request,
            cancellationToken
        );

    /// <summary>Registra una medición manual o importada.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="request">Activo, fecha, valor, unidad, método y origen de la medición.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador de la medición registrada para el activo.</returns>
    public Task<SavedTraceabilityResponse> CreateAssetMeasurementAsync(
        string token,
        CreateAssetMeasurementRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync<SavedTraceabilityResponse>(
            HttpMethod.Post,
            "api/inventory/traceability/measurements",
            token,
            request,
            cancellationToken
        );

    /// <summary>Confirma un fraccionamiento.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="request">Origen, cantidad consumida, merma y destinos del fraccionamiento.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador del fraccionamiento o transformación confirmada.</returns>
    public Task<SavedTraceabilityResponse> CreateTransformationAsync(
        string token,
        CreateTransformationRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync<SavedTraceabilityResponse>(
            HttpMethod.Post,
            "api/inventory/traceability/transformations",
            token,
            request,
            cancellationToken
        );

    /// <summary>Registra un incidente con su pérdida.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="request">Activo o lote afectado, tipo, fecha, motivo y observación de la incidencia.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador de la incidencia registrada.</returns>
    public Task<SavedTraceabilityResponse> CreateIncidentAsync(
        string token,
        CreateIncidentRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync<SavedTraceabilityResponse>(
            HttpMethod.Post,
            "api/inventory/traceability/incidents",
            token,
            request,
            cancellationToken
        );

    /// <summary>Inicia mantenimiento de un activo.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="request">Activo, tipo, fechas y observación del mantenimiento que se registra.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador del mantenimiento registrado.</returns>
    public Task<SavedTraceabilityResponse> CreateMaintenanceAsync(
        string token,
        CreateMaintenanceRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync<SavedTraceabilityResponse>(
            HttpMethod.Post,
            "api/inventory/traceability/maintenances",
            token,
            request,
            cancellationToken
        );

    /// <summary>Completa un mantenimiento.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="id">Identificador del recurso indicado por la operación.</param>
    /// <param name="request">Resultado, fecha y versión del mantenimiento que se confirma.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    public Task CompleteMaintenanceAsync(
        string token,
        long id,
        CompleteMaintenanceRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync(
            HttpMethod.Post,
            $"api/inventory/traceability/maintenances/{id}/complete",
            token,
            request,
            cancellationToken
        );

    /// <summary>Registra préstamo y snapshot de salida.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="request">Destino, forma de entrega, devolución prevista y activos que se prestan.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Identificador del préstamo de activo registrado.</returns>
    public Task<SavedTraceabilityResponse> CreateLoanAsync(
        string token,
        CreateLoanRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync<SavedTraceabilityResponse>(
            HttpMethod.Post,
            "api/inventory/traceability/loans",
            token,
            request,
            cancellationToken
        );

    /// <summary>Confirma devolución y snapshot de regreso.</summary>
    /// <param name="token">Token de acceso emitido para la sesión autenticada.</param>
    /// <param name="id">Identificador del recurso indicado por la operación.</param>
    /// <param name="request">Préstamo, fecha, activos devueltos y observación de la devolución.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    public Task ReturnLoanAsync(
        string token,
        long id,
        ReturnLoanRequest request,
        CancellationToken cancellationToken = default
    ) =>
        SendAuthorizedAsync(
            HttpMethod.Post,
            $"api/inventory/traceability/loans/{id}/return",
            token,
            request,
            cancellationToken
        );

    /// <summary>Crea una solicitud HTTP autenticada y serializa su contenido cuando corresponde.</summary>
    /// <param name="method">Método HTTP utilizado para la solicitud.</param>
    /// <param name="uri">Ruta relativa del recurso solicitado a la API.</param>
    /// <param name="token">Token de la sesión autenticada.</param>
    /// <param name="content">Contenido que se serializa como cuerpo de la solicitud HTTP.</param>
    /// <returns>Solicitud HTTP configurada con autenticación y contenido.</returns>
    private static HttpRequestMessage Authorized(
        HttpMethod method,
        string uri,
        string token,
        object? content = null
    )
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (content is not null)
            request.Content = JsonContent.Create(content);
        return request;
    }

    /// <summary>Ejecuta una consulta HTTP autenticada y deserializa su respuesta.</summary>
    /// <param name="uri">Ruta relativa del recurso solicitado a la API.</param>
    /// <param name="token">Token de la sesión autenticada.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Respuesta autenticada y deserializada.</returns>
    /// <exception cref="InvalidDataException">Se produce cuando el contenido recibido no cumple el formato admitido.</exception>
    private async Task<T> GetAuthorizedAsync<T>(
        string uri,
        string token,
        CancellationToken cancellationToken
    )
    {
        using var request = Authorized(HttpMethod.Get, uri, token);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken)
            ?? throw new InvalidDataException("La API no devolvió la respuesta esperada.");
    }

    /// <summary>Envía una solicitud HTTP autenticada y procesa su respuesta.</summary>
    /// <param name="method">Método HTTP utilizado para la solicitud.</param>
    /// <param name="uri">Ruta relativa del recurso solicitado a la API.</param>
    /// <param name="token">Token de la sesión autenticada.</param>
    /// <param name="content">Contenido que se serializa como cuerpo de la solicitud HTTP.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    private async Task SendAuthorizedAsync(
        HttpMethod method,
        string uri,
        string token,
        object content,
        CancellationToken cancellationToken
    )
    {
        using var request = Authorized(method, uri, token, content);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    /// <summary>Envía una solicitud HTTP autenticada y procesa su respuesta.</summary>
    /// <param name="method">Método HTTP utilizado para la solicitud.</param>
    /// <param name="uri">Ruta relativa del recurso solicitado a la API.</param>
    /// <param name="token">Token de la sesión autenticada.</param>
    /// <param name="content">Contenido que se serializa como cuerpo de la solicitud HTTP.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Datos producidos por la operación.</returns>
    /// <exception cref="InvalidDataException">Se produce cuando el contenido recibido no cumple el formato admitido.</exception>
    private async Task<T> SendAuthorizedAsync<T>(
        HttpMethod method,
        string uri,
        string token,
        object content,
        CancellationToken cancellationToken
    )
    {
        using var request = Authorized(method, uri, token, content);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken)
            ?? throw new InvalidDataException("La API no devolvió la respuesta esperada.");
    }

    /// <summary>Valida la respuesta HTTP y traduce los errores de la API a una excepción funcional.</summary>
    /// <param name="response">Respuesta HTTP que se valida antes de leer sus datos.</param>
    /// <param name="cancellationToken">Token que permite cancelar la operación asincrónica.</param>
    /// <returns>Tarea que finaliza cuando la operación se completa.</returns>
    /// <exception cref="HttpRequestException">Se produce cuando una respuesta o estado inválido impide completar la operación.</exception>
    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken
    )
    {
        if (response.IsSuccessStatusCode)
            return;
        ApiErrorResponse? error = null;
        try
        {
            error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(cancellationToken);
        }
        catch (System.Text.Json.JsonException) { }
        var message = error is null
            ? $"La operación no pudo completarse ({(int)response.StatusCode})."
            : $"{error.Message} Código {error.ErrorCode}. Correlación {error.CorrelationId}.";
        throw new HttpRequestException(message, null, response.StatusCode);
    }
}
