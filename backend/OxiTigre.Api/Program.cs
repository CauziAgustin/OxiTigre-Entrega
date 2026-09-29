/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.Api.Program
Archivo: Program.cs | Versión: 10.1.0 | Fecha: 2026-08-29 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Configura el host HTTP y los endpoints iniciales de la API.
Historial: 1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Incorporación de autenticación y cambio de contraseña.
Historial: 1.2.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Consulta de empresas posterior a validar credenciales.
Historial: 2.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Administración de usuarios y ciclo completo de clientes.
Historial: 2.1.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Seguridad funcional completa y cierre de sesión.
Historial: 2.2.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Catálogos, borradores y errores HTTP controlados.
Historial: 2.3.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Errores controlados para sesión revocada y cliente inexistente.
Historial: 3.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | API administrativa de Configuración, errores e idioma.
Historial: 4.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | API completa de maestros, saldos y movimientos de Inventario.
Historial: 6.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | API de listas, pedidos, reservas y ventas internas.
Historial: 6.1.0 | 2026-08-21 | FABRICA | Agustin Omar Cauzi | Stock físico, reservado y disponible en la respuesta de Inventario.
Historial: 6.2.0 | 2026-08-21 | FABRICA | Agustin Omar Cauzi | Promociones configurables y descuentos trazables en ventas.
Historial: 6.2.1 | 2026-08-21 | FABRICA | Agustin Omar Cauzi | Rechazo controlado de renglones nulos en pedidos.
Historial: 7.0.0 | 2026-08-24 | FABRICA | Agustin Omar Cauzi | API de proveedores, órdenes y recepciones parciales.
Historial: 7.1.0 | 2026-08-26 | FABRICA | Agustin Omar Cauzi | Snapshot de orden y reversión auditada de recepciones.
Historial: 8.0.0 | 2026-08-26 | FABRICA | Agustin Omar Cauzi | API del circuito logístico y hojas de ruta.
Historial: 8.1.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Endpoints de transportistas, vehículos y asignación de rutas.
Historial: 9.0.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Enrutamiento multiempresa y vista global administrativa.
Historial: 10.0.0 | 2026-08-28 | FABRICA | Agustin Omar Cauzi | Vista móvil segura para recorridos propios del transportista.
Historial: 10.1.0 | 2026-08-29 | FABRICA | Agustin Omar Cauzi | Llegada e incidencias operativas del transportista.
===============================================================================
*/
using OxiTigre.BLL.Commercial;
using OxiTigre.BLL.Configuration;
using OxiTigre.BLL.Finance;
using OxiTigre.BLL.Inventory;
using OxiTigre.BLL.Logistics;
using OxiTigre.BLL.Purchasing;
using OxiTigre.BLL.Security;
using OxiTigre.Contracts.Commercial;
using OxiTigre.Contracts.Configuration;
using OxiTigre.Contracts.Finance;
using OxiTigre.Contracts.Inventory;
using OxiTigre.Contracts.Logistics;
using OxiTigre.Contracts.Platform;
using OxiTigre.Contracts.Purchasing;
using OxiTigre.Contracts.Security;
using OxiTigre.Contracts.System;
using OxiTigre.DAL.Commercial;
using OxiTigre.DAL.Configuration;
using OxiTigre.DAL.Errors;
using OxiTigre.DAL.Finance;
using OxiTigre.DAL.Inventory;
using OxiTigre.DAL.Logistics;
using OxiTigre.DAL.MultiCompany;
using OxiTigre.DAL.Purchasing;
using OxiTigre.DAL.Security;
using OxiTigre.DAL.StoredProcedures;

var builder = WebApplication.CreateBuilder(args);
var platformConnectionString =
    builder.Configuration.GetConnectionString("OxiTigrePlatform")
    ?? throw new InvalidOperationException("Falta ConnectionStrings:OxiTigrePlatform.");
var databases = await CompanyDatabaseRegistry.LoadAsync(platformConnectionString);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<PasswordHasher>();
builder.Services.AddSingleton(databases);
builder.Services.AddScoped<IStoredProcedureAuditWriter>(_ => new SqlStoredProcedureAuditWriter(
    databases
));
builder.Services.AddScoped<IAuthenticationStore>(services => new SqlAuthenticationStore(
    databases,
    services.GetRequiredService<IStoredProcedureAuditWriter>()
));
builder.Services.AddScoped<AuthenticationService>();
builder.Services.AddScoped<IUserAdministrationStore>(services => new SqlUserAdministrationStore(
    databases,
    services.GetRequiredService<IStoredProcedureAuditWriter>()
));
builder.Services.AddScoped<UserAdministrationService>();
builder.Services.AddScoped<IClientStore>(services => new SqlClientStore(
    databases,
    services.GetRequiredService<IStoredProcedureAuditWriter>()
));
builder.Services.AddScoped<ClientService>();
builder.Services.AddScoped<IConfigurationStore>(services => new SqlConfigurationStore(
    databases,
    services.GetRequiredService<IStoredProcedureAuditWriter>()
));
builder.Services.AddScoped<ConfigurationService>();
builder.Services.AddScoped<IInventoryStore>(services => new SqlInventoryStore(
    databases,
    services.GetRequiredService<IStoredProcedureAuditWriter>()
));
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<ICommercialSalesStore>(services => new SqlCommercialSalesStore(
    databases,
    services.GetRequiredService<IStoredProcedureAuditWriter>()
));
builder.Services.AddScoped<CommercialSalesService>();
builder.Services.AddScoped<IPurchasingStore>(services => new SqlPurchasingStore(
    databases,
    services.GetRequiredService<IStoredProcedureAuditWriter>()
));
builder.Services.AddScoped<PurchasingService>();
builder.Services.AddScoped<ITraceabilityStore>(services => new SqlTraceabilityStore(
    databases,
    services.GetRequiredService<IStoredProcedureAuditWriter>()
));
builder.Services.AddScoped<TraceabilityService>();
builder.Services.AddScoped<ILogisticsStore>(services => new SqlLogisticsStore(
    databases,
    services.GetRequiredService<IStoredProcedureAuditWriter>()
));
builder.Services.AddScoped<LogisticsService>();
builder.Services.AddScoped<IFinanceStore>(services => new SqlFinanceStore(
    databases,
    services.GetRequiredService<IStoredProcedureAuditWriter>()
));
builder.Services.AddScoped<FinanceService>();
builder.Services.AddScoped(_ => new SqlApplicationErrorWriter(databases));
builder.Services.AddSingleton<SqlPlatformOverviewStore>();

var app = builder.Build();

app.Use(
    async (context, next) =>
    {
        var correlationId = Guid.NewGuid();
        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers["X-Correlation-ID"] = correlationId.ToString();
        try
        {
            await next();
        }
        catch (Exception exception)
        {
            app.Logger.LogError(exception, "Error no controlado {CorrelationId}", correlationId);
            try
            {
                await context
                    .RequestServices.GetRequiredService<SqlApplicationErrorWriter>()
                    .WriteAsync(
                        exception,
                        $"{context.Request.Method} {context.Request.Path}",
                        context.Connection.RemoteIpAddress?.ToString(),
                        correlationId,
                        context.RequestAborted,
                        context.Items["CompanyCode"] as string
                    );
            }
            catch (Exception auditException)
            {
                app.Logger.LogError(
                    auditException,
                    "No se pudo registrar el error {CorrelationId}",
                    correlationId
                );
            }
            if (context.Response.HasStarted)
                throw;
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(
                new ApiErrorResponse(
                    70002,
                    "Ocurrió un error interno. Informá el código y la correlación al equipo de desarrollo.",
                    correlationId
                )
            );
        }
    }
);

app.MapGet(
    "/health",
    (IHostEnvironment environment) =>
        Results.Ok(
            new HealthResponse("Healthy", environment.EnvironmentName, DateTimeOffset.UtcNow)
        )
);

app.MapGet(
    "/api/platform/overview",
    async (
        HttpContext context,
        AuthenticationService authentication,
        SqlPlatformOverviewStore overview,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        if (!identity.Roles.Contains("ADMINISTRADOR", StringComparer.OrdinalIgnoreCase))
            return AuthorizationError(context);

        var companies = await overview.GetAsync(cancellationToken);
        return Results.Ok(
            new PlatformOverviewResponse(
                companies
                    .Select(company => new CompanyOverviewResponse(
                        company.CompanyCode,
                        company.CompanyName,
                        company.SchemaVersion,
                        company.Status,
                        company.ActiveClients,
                        company.Products,
                        company.PhysicalStock,
                        company.OpenOrders,
                        company.Sales
                    ))
                    .ToList()
            )
        );
    }
);

app.MapPost(
    "/api/security/companies",
    async (
        CredentialsRequest request,
        HttpContext context,
        AuthenticationService authenticationService,
        CancellationToken cancellationToken
    ) =>
    {
        var companies = await authenticationService.GetCompaniesAsync(
            request.Username,
            request.Password,
            cancellationToken
        );
        return companies.Count == 0
            ? AuthenticationError(context)
            : Results.Ok(
                companies.Select(company => new CompanyOptionResponse(
                    company.Code,
                    company.Name,
                    company
                        .Branches.Select(branch => new BranchOptionResponse(
                            branch.BranchId,
                            branch.Code,
                            branch.Name
                        ))
                        .ToList()
                ))
            );
    }
);

app.MapPost(
    "/api/security/login",
    async (
        LoginRequest request,
        HttpContext httpContext,
        AuthenticationService authenticationService,
        CancellationToken cancellationToken
    ) =>
    {
        var command = new LoginCommand(
            request.CompanyCode,
            request.Username,
            request.Password,
            httpContext.Connection.RemoteIpAddress?.ToString(),
            "OxiTigre.Api",
            request.BranchId
        );
        var session = await authenticationService.LoginAsync(command, cancellationToken);
        return session is null
            ? AuthenticationError(httpContext)
            : Results.Ok(
                new LoginResponse(
                    session.SessionId,
                    session.UserId,
                    session.Username,
                    session.DisplayName,
                    session.Token,
                    session.ExpiresAtUtc,
                    session.MustChangePassword,
                    session.Roles,
                    session.Permissions,
                    session.CorrelationId,
                    session.BranchId,
                    session.BranchCode,
                    session.BranchName
                )
            );
    }
);

app.MapGet(
    "/api/security/users",
    async (
        HttpContext context,
        AuthenticationService authentication,
        UserAdministrationService users,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        try
        {
            return Results.Ok(
                (await users.ListAsync(identity, cancellationToken)).Select(
                    user => new UserSummaryResponse(
                        user.UserId,
                        user.Username,
                        user.GivenNames,
                        user.Surname,
                        user.Email,
                        user.StatusCode,
                        user.Roles
                    )
                )
            );
        }
        catch (UnauthorizedAccessException)
        {
            return AuthorizationError(context);
        }
    }
);

app.MapPost(
    "/api/security/users",
    async (
        CreateUserRequest request,
        HttpContext context,
        AuthenticationService authentication,
        UserAdministrationService users,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        try
        {
            var id = await users.CreateAsync(
                identity,
                request.GivenNames,
                request.Surname,
                request.Email,
                request.RoleCodes,
                request.TemporaryPassword,
                cancellationToken
            );
            return Results.Created($"/api/security/users/{id}", new { userId = id });
        }
        catch (UnauthorizedAccessException)
        {
            return AuthorizationError(context);
        }
        catch (ArgumentException exception)
        {
            return ControlledError(context, 10003, exception.Message);
        }
        catch (Microsoft.Data.SqlClient.SqlException exception)
            when (exception.Number is 2601 or 2627 || exception.Number >= 50000)
        {
            return SqlValidationError(
                context,
                10003,
                exception,
                "Ya existe un usuario con los datos informados."
            );
        }
    }
);

app.MapGet(
    "/api/security/roles",
    async (
        HttpContext context,
        AuthenticationService authentication,
        UserAdministrationService users,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        try
        {
            return Results.Ok(
                (await users.ListRolesAsync(identity, cancellationToken)).Select(
                    role => new RoleSummaryResponse(role.Code, role.Name)
                )
            );
        }
        catch (UnauthorizedAccessException)
        {
            return AuthorizationError(context);
        }
    }
);

app.MapPut(
    "/api/security/users/{userId:long}",
    async (
        long userId,
        UpdateUserRequest request,
        HttpContext context,
        AuthenticationService authentication,
        UserAdministrationService users,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        try
        {
            await users.UpdateAsync(
                identity,
                userId,
                request.GivenNames,
                request.Surname,
                request.Email,
                request.StatusCode,
                request.RoleCodes,
                cancellationToken
            );
            return Results.NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return AuthorizationError(context);
        }
        catch (Exception exception)
            when (exception is ArgumentException or InvalidOperationException)
        {
            return ControlledError(context, 10003, exception.Message);
        }
        catch (Microsoft.Data.SqlClient.SqlException exception)
            when (exception.Number is 2601 or 2627 || exception.Number >= 50000)
        {
            return SqlValidationError(
                context,
                10003,
                exception,
                "Ya existe un usuario con los datos informados."
            );
        }
    }
);

app.MapPost(
    "/api/security/users/{userId:long}/reset-password",
    async (
        long userId,
        ResetUserPasswordRequest request,
        HttpContext context,
        AuthenticationService authentication,
        UserAdministrationService users,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        try
        {
            await users.ResetPasswordAsync(
                identity,
                userId,
                request.TemporaryPassword,
                cancellationToken
            );
            return Results.NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return AuthorizationError(context);
        }
        catch (ArgumentException exception)
        {
            return ControlledError(context, 10002, exception.Message);
        }
        catch (Microsoft.Data.SqlClient.SqlException exception)
            when (exception.Number is 2601 or 2627 || exception.Number >= 50000)
        {
            return SqlValidationError(
                context,
                10003,
                exception,
                "Ya existe un usuario con los datos informados."
            );
        }
    }
);

app.MapPost(
    "/api/security/users/{userId:long}/revoke-sessions",
    async (
        long userId,
        HttpContext context,
        AuthenticationService authentication,
        UserAdministrationService users,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        try
        {
            await users.RevokeSessionsAsync(identity, userId, cancellationToken);
            return Results.NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return AuthorizationError(context);
        }
        catch (InvalidOperationException exception)
        {
            return ControlledError(context, 10003, exception.Message);
        }
        catch (Microsoft.Data.SqlClient.SqlException exception) when (exception.Number >= 50000)
        {
            return ControlledError(context, 10003, exception.Message);
        }
    }
);

app.MapPost(
    "/api/security/logout",
    async (
        HttpContext context,
        AuthenticationService authentication,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        await authentication.LogoutAsync(identity, cancellationToken);
        return Results.NoContent();
    }
);

app.MapPost(
    "/api/security/change-password",
    async (
        ChangePasswordRequest request,
        HttpContext context,
        AuthenticationService authenticationService,
        CancellationToken cancellationToken
    ) =>
    {
        try
        {
            var changed = await authenticationService.ChangePasswordAsync(
                request.CompanyCode,
                request.Username,
                request.CurrentPassword,
                request.NewPassword,
                cancellationToken
            );
            return changed ? Results.NoContent() : AuthenticationError(context);
        }
        catch (ArgumentException exception)
        {
            return ControlledError(context, 10002, exception.Message);
        }
    }
);

app.MapGet(
    "/api/security/session",
    async (
        HttpContext httpContext,
        AuthenticationService authenticationService,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(
            httpContext,
            authenticationService,
            cancellationToken
        );
        return identity is null
            ? ControlledError(
                httpContext,
                10005,
                "La sesión fue cerrada, revocada o alcanzó su vencimiento.",
                StatusCodes.Status401Unauthorized
            )
            : Results.Ok(
                new SessionResponse(
                    identity.SessionId,
                    identity.UserId,
                    identity.Username,
                    identity.DisplayName,
                    identity.ExpiresAtUtc,
                    identity.MustChangePassword,
                    identity.Roles,
                    identity.Permissions,
                    identity.CorrelationId,
                    identity.BranchId,
                    identity.BranchCode,
                    identity.BranchName
                )
            );
    }
);

app.MapGet(
    "/api/commercial/clients",
    async (
        HttpContext httpContext,
        string? status,
        AuthenticationService authenticationService,
        ClientService clientService,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(
            httpContext,
            authenticationService,
            cancellationToken
        );
        if (identity is null)
            return AuthenticationError(httpContext);

        try
        {
            var clients = await clientService.ListAsync(
                identity,
                status ?? "ACTIVO",
                cancellationToken
            );
            return Results.Ok(
                clients.Select(client => new ClientSummaryResponse(
                    client.ClientId,
                    client.Code,
                    client.PersonType,
                    client.NameOrBusinessName,
                    client.Surname,
                    client.DocumentNumber,
                    client.Email,
                    client.StatusCode,
                    client.PrimaryPhone,
                    client.PhoneCount
                ))
            );
        }
        catch (UnauthorizedAccessException)
        {
            return AuthorizationError(httpContext);
        }
    }
);

app.MapGet(
    "/api/commercial/client-catalogs",
    async (
        HttpContext context,
        AuthenticationService authentication,
        ClientService clients,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        try
        {
            var catalogs = await clients.GetCatalogsAsync(identity, cancellationToken);
            return Results.Ok(
                new ClientCatalogResponse(
                    catalogs
                        .DocumentTypes.Select(option => new DocumentTypeOptionResponse(
                            option.Code,
                            option.Name,
                            option.AppliesToNaturalPerson,
                            option.AppliesToLegalPerson
                        ))
                        .ToList(),
                    catalogs
                        .Countries.Select(option => new CountryOptionResponse(
                            option.Code,
                            option.Name,
                            option.PhoneCode,
                            option.IsDefault
                        ))
                        .ToList(),
                    catalogs
                        .PhoneTypes.Select(option => new PhoneTypeOptionResponse(
                            option.Code,
                            option.Name
                        ))
                        .ToList()
                )
            );
        }
        catch (UnauthorizedAccessException)
        {
            return AuthorizationError(context);
        }
    }
);

app.MapGet(
    "/api/commercial/client-draft",
    async (
        HttpContext context,
        AuthenticationService authentication,
        ClientService clients,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        try
        {
            var saved = await clients.GetDraftAsync(identity, cancellationToken);
            return saved is null
                ? Results.NoContent()
                : Results.Ok(
                    new SavedClientDraftResponse(ToRequest(saved.Draft), saved.SavedAtUtc)
                );
        }
        catch (UnauthorizedAccessException)
        {
            return AuthorizationError(context);
        }
    }
);

app.MapPut(
    "/api/commercial/client-draft",
    async (
        SaveClientRequest request,
        HttpContext context,
        AuthenticationService authentication,
        ClientService clients,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        try
        {
            await clients.SaveDraftAsync(identity, ToDraft(request), cancellationToken);
            return Results.NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return AuthorizationError(context);
        }
        catch (ArgumentException exception)
        {
            return ControlledError(context, 40001, exception.Message);
        }
        catch (Microsoft.Data.SqlClient.SqlException exception)
            when (exception.Number is 2601 or 2627 || exception.Number >= 50000)
        {
            return SqlValidationError(
                context,
                40001,
                exception,
                "Ya existe un borrador activo para este usuario y empresa."
            );
        }
    }
);

app.MapDelete(
    "/api/commercial/client-draft",
    async (
        HttpContext context,
        AuthenticationService authentication,
        ClientService clients,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        try
        {
            await clients.DeleteDraftAsync(identity, cancellationToken);
            return Results.NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return AuthorizationError(context);
        }
    }
);

app.MapGet(
    "/api/commercial/clients/{clientId:long}",
    async (
        long clientId,
        HttpContext context,
        AuthenticationService authentication,
        ClientService clients,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        try
        {
            var client = await clients.GetAsync(identity, clientId, cancellationToken);
            return client is null
                ? ControlledError(
                    context,
                    40002,
                    "El cliente solicitado no existe o no pertenece a la empresa.",
                    StatusCodes.Status404NotFound
                )
                : Results.Ok(ToResponse(client));
        }
        catch (UnauthorizedAccessException)
        {
            return AuthorizationError(context);
        }
        catch (ArgumentException exception)
        {
            return ControlledError(context, 40001, exception.Message);
        }
    }
);

app.MapPost(
    "/api/commercial/clients",
    async (
        SaveClientRequest request,
        HttpContext context,
        AuthenticationService authentication,
        ClientService clients,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        try
        {
            var clientId = await clients.CreateAsync(identity, ToDraft(request), cancellationToken);
            return Results.Created($"/api/commercial/clients/{clientId}", new { clientId });
        }
        catch (UnauthorizedAccessException)
        {
            return AuthorizationError(context);
        }
        catch (ArgumentException exception)
        {
            return ControlledError(context, 40001, exception.Message);
        }
        catch (Microsoft.Data.SqlClient.SqlException exception)
            when (exception.Number is 2601 or 2627 || exception.Number >= 50000)
        {
            return SqlValidationError(
                context,
                40001,
                exception,
                "Ya existe un cliente con el mismo tipo y número de documento."
            );
        }
    }
);

app.MapPut(
    "/api/commercial/clients/{clientId:long}",
    async (
        long clientId,
        SaveClientRequest request,
        HttpContext context,
        AuthenticationService authentication,
        ClientService clients,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        try
        {
            await clients.UpdateAsync(identity, clientId, ToDraft(request), cancellationToken);
            return Results.NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return AuthorizationError(context);
        }
        catch (ArgumentException exception)
        {
            return ControlledError(context, 40001, exception.Message);
        }
        catch (Microsoft.Data.SqlClient.SqlException exception)
            when (exception.Number is 2601 or 2627 || exception.Number >= 50000)
        {
            return SqlValidationError(
                context,
                40001,
                exception,
                "Ya existe un cliente con el mismo tipo y número de documento."
            );
        }
    }
);

app.MapPatch(
    "/api/commercial/clients/{clientId:long}/status",
    async (
        long clientId,
        ChangeClientStatusRequest request,
        HttpContext context,
        AuthenticationService authentication,
        ClientService clients,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        try
        {
            await clients.ChangeStatusAsync(
                identity,
                clientId,
                request.StatusCode,
                cancellationToken
            );
            return Results.NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return AuthorizationError(context);
        }
        catch (ArgumentException exception)
        {
            return ControlledError(context, 40001, exception.Message);
        }
        catch (Microsoft.Data.SqlClient.SqlException exception) when (exception.Number >= 50000)
        {
            return ControlledError(context, 40001, exception.Message);
        }
    }
);

app.MapGet(
    "/api/configuration",
    async (
        HttpContext context,
        AuthenticationService authentication,
        ConfigurationService configuration,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        return await ConfigurationAction(
            context,
            async () =>
                Results.Ok(
                    ToConfigurationResponse(
                        await configuration.GetAsync(identity, cancellationToken)
                    )
                )
        );
    }
);

app.MapGet(
    "/api/configuration/language",
    async (
        HttpContext context,
        AuthenticationService authentication,
        ConfigurationService configuration,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        return await ConfigurationAction(
            context,
            async () =>
                Results.Ok(
                    new LanguagePreferenceResponse(
                        await configuration.GetCultureAsync(identity, cancellationToken)
                    )
                )
        );
    }
);

app.MapPut(
    "/api/configuration/language",
    async (
        SaveLanguagePreferenceRequest request,
        HttpContext context,
        AuthenticationService authentication,
        ConfigurationService configuration,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        return await ConfigurationAction(
            context,
            async () =>
            {
                await configuration.SaveCultureAsync(
                    identity,
                    request.CultureCode,
                    cancellationToken
                );
                return Results.NoContent();
            }
        );
    }
);

app.MapPut(
    "/api/configuration/company",
    async (
        UpdateCompanyConfigurationRequest request,
        HttpContext context,
        AuthenticationService authentication,
        ConfigurationService configuration,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        return await ConfigurationAction(
            context,
            async () =>
            {
                await configuration.UpdateCompanyAsync(
                    identity,
                    new CompanyChange(
                        request.LegalName,
                        request.TradeName,
                        request.TaxId,
                        request.Email,
                        Version(request.RowVersion)
                    ),
                    cancellationToken
                );
                return Results.NoContent();
            }
        );
    }
);

app.MapPost(
    "/api/configuration/branches",
    async (
        SaveBranchConfigurationRequest request,
        HttpContext context,
        AuthenticationService authentication,
        ConfigurationService configuration,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        return await ConfigurationAction(
            context,
            async () =>
                Results.Ok(
                    new SavedConfigurationResponse(
                        await configuration.SaveBranchAsync(
                            identity,
                            new BranchChange(
                                request.BranchId,
                                request.Code,
                                request.Name,
                                request.Address,
                                request.City,
                                request.Province,
                                request.PostalCode,
                                request.StatusCode,
                                OptionalVersion(request.RowVersion)
                            ),
                            cancellationToken
                        )
                    )
                )
        );
    }
);

app.MapPost(
    "/api/configuration/operating-units",
    async (
        SaveOperatingUnitConfigurationRequest request,
        HttpContext context,
        AuthenticationService authentication,
        ConfigurationService configuration,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        return await ConfigurationAction(
            context,
            async () =>
                Results.Ok(
                    new SavedConfigurationResponse(
                        await configuration.SaveOperatingUnitAsync(
                            identity,
                            new OperatingUnitChange(
                                request.OperatingUnitId,
                                request.BranchId,
                                request.Code,
                                request.Name,
                                request.Description,
                                request.StatusCode,
                                OptionalVersion(request.RowVersion)
                            ),
                            cancellationToken
                        )
                    )
                )
        );
    }
);

app.MapPost(
    "/api/configuration/phone-types",
    async (
        SavePhoneTypeConfigurationRequest request,
        HttpContext context,
        AuthenticationService authentication,
        ConfigurationService configuration,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        return await ConfigurationAction(
            context,
            async () =>
                Results.Ok(
                    new SavedConfigurationResponse(
                        await configuration.SavePhoneTypeAsync(
                            identity,
                            new PhoneTypeChange(
                                request.PhoneTypeId,
                                request.Code,
                                request.Name,
                                request.Description,
                                request.StatusCode,
                                OptionalVersion(request.RowVersion)
                            ),
                            cancellationToken
                        )
                    )
                )
        );
    }
);

app.MapPut(
    "/api/configuration/states",
    async (
        UpdateStateConfigurationRequest request,
        HttpContext context,
        AuthenticationService authentication,
        ConfigurationService configuration,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        return await ConfigurationAction(
            context,
            async () =>
            {
                await configuration.UpdateStateAsync(
                    identity,
                    new StateChange(
                        request.StateId,
                        request.Name,
                        request.Description,
                        request.Order,
                        request.ValidUntilUtc,
                        Version(request.RowVersion)
                    ),
                    cancellationToken
                );
                return Results.NoContent();
            }
        );
    }
);

app.MapPost(
    "/api/configuration/parameters",
    async (
        SaveSystemParameterConfigurationRequest request,
        HttpContext context,
        AuthenticationService authentication,
        ConfigurationService configuration,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        return await ConfigurationAction(
            context,
            async () =>
                Results.Ok(
                    new SavedConfigurationResponse(
                        await configuration.SaveParameterAsync(
                            identity,
                            new SystemParameterChange(
                                request.ParameterId,
                                request.ModuleId,
                                request.Key,
                                request.Value,
                                request.DataType,
                                request.IsSecret,
                                request.SecretReference,
                                request.Description,
                                request.StatusCode,
                                OptionalVersion(request.RowVersion)
                            ),
                            cancellationToken
                        )
                    )
                )
        );
    }
);

app.MapPost(
    "/api/configuration/modules",
    async (
        SaveModuleConfigurationRequest request,
        HttpContext context,
        AuthenticationService authentication,
        ConfigurationService configuration,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        return await ConfigurationAction(
            context,
            async () =>
                Results.Ok(
                    new SavedConfigurationResponse(
                        await configuration.SaveModuleAsync(
                            identity,
                            new ModuleChange(
                                request.ModuleId,
                                request.ModuleNumber,
                                request.Code,
                                request.Name,
                                request.Description,
                                request.Order,
                                request.StatusCode,
                                OptionalVersion(request.RowVersion)
                            ),
                            cancellationToken
                        )
                    )
                )
        );
    }
);

app.MapPost(
    "/api/configuration/translations",
    async (
        SaveCatalogTranslationRequest request,
        HttpContext context,
        AuthenticationService authentication,
        ConfigurationService configuration,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        return await ConfigurationAction(
            context,
            async () =>
                Results.Ok(
                    new SavedConfigurationResponse(
                        await configuration.SaveTranslationAsync(
                            identity,
                            new CatalogTranslationChange(
                                request.TranslationId,
                                request.Entity,
                                request.Code,
                                request.CultureCode,
                                request.Name,
                                request.Description,
                                request.StatusCode,
                                OptionalVersion(request.RowVersion)
                            ),
                            cancellationToken
                        )
                    )
                )
        );
    }
);

app.MapPost(
    "/api/audit/errors",
    async (
        CreateErrorCatalogRequest request,
        HttpContext context,
        AuthenticationService authentication,
        ConfigurationService configuration,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        return await ConfigurationAction(
            context,
            async () =>
                Results.Ok(
                    new CreatedErrorCodeResponse(
                        await configuration.CreateErrorAsync(
                            identity,
                            new CreateErrorCatalogChange(
                                request.ModuleId,
                                request.Name,
                                request.Description,
                                request.ProbableCause,
                                request.RecommendedAction,
                                request.Severity
                            ),
                            cancellationToken
                        )
                    )
                ),
            70003
        );
    }
);

app.MapPut(
    "/api/audit/errors",
    async (
        UpdateErrorCatalogRequest request,
        HttpContext context,
        AuthenticationService authentication,
        ConfigurationService configuration,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        return await ConfigurationAction(
            context,
            async () =>
            {
                await configuration.UpdateErrorAsync(
                    identity,
                    new UpdateErrorCatalogChange(
                        request.ErrorId,
                        request.Name,
                        request.Description,
                        request.ProbableCause,
                        request.RecommendedAction,
                        request.Severity,
                        request.StatusCode,
                        Version(request.RowVersion)
                    ),
                    cancellationToken
                );
                return Results.NoContent();
            },
            70003
        );
    }
);

app.MapGet(
    "/api/inventory",
    async (
        HttpContext context,
        AuthenticationService authentication,
        InventoryService inventory,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        return await InventoryAction(
            context,
            async () =>
                Results.Ok(
                    ToInventoryResponse(await inventory.GetAsync(identity, cancellationToken))
                )
        );
    }
);

app.MapPost(
    "/api/inventory/measurement-units",
    async (
        SaveMeasurementUnitRequest request,
        HttpContext context,
        AuthenticationService authentication,
        InventoryService inventory,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        return await InventoryAction(
            context,
            async () =>
                Results.Ok(
                    new SavedInventoryResponse(
                        await inventory.SaveMeasurementUnitAsync(
                            identity,
                            new MeasurementUnitChange(
                                request.MeasurementUnitId,
                                request.Code,
                                request.Name,
                                request.Symbol,
                                request.AllowsDecimals,
                                request.StatusCode,
                                OptionalVersion(request.RowVersion)
                            ),
                            cancellationToken
                        )
                    )
                )
        );
    }
);

app.MapPost(
    "/api/inventory/categories",
    async (
        SaveProductCategoryRequest request,
        HttpContext context,
        AuthenticationService authentication,
        InventoryService inventory,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        return await InventoryAction(
            context,
            async () =>
                Results.Ok(
                    new SavedInventoryResponse(
                        await inventory.SaveCategoryAsync(
                            identity,
                            new ProductCategoryChange(
                                request.ProductCategoryId,
                                request.Code,
                                request.Name,
                                request.Description,
                                request.StatusCode,
                                OptionalVersion(request.RowVersion)
                            ),
                            cancellationToken
                        )
                    )
                )
        );
    }
);

app.MapPost(
    "/api/inventory/products",
    async (
        SaveProductRequest request,
        HttpContext context,
        AuthenticationService authentication,
        InventoryService inventory,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        return await InventoryAction(
            context,
            async () =>
                Results.Ok(
                    new SavedInventoryResponse(
                        await inventory.SaveProductAsync(
                            identity,
                            new ProductChange(
                                request.ProductId,
                                request.ProductCategoryId,
                                request.MeasurementUnitId,
                                request.Name,
                                request.Description,
                                request.Barcode,
                                request.StatusCode,
                                OptionalVersion(request.RowVersion),
                                request.ItemType,
                                request.TrackingType,
                                request.IsReusable,
                                request.AllowsLoans,
                                request.RequiresMaintenance,
                                request.AllowsMeasurements
                            ),
                            cancellationToken
                        )
                    )
                )
        );
    }
);

app.MapPost(
    "/api/inventory/warehouses",
    async (
        SaveWarehouseRequest request,
        HttpContext context,
        AuthenticationService authentication,
        InventoryService inventory,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        return await InventoryAction(
            context,
            async () =>
                Results.Ok(
                    new SavedInventoryResponse(
                        await inventory.SaveWarehouseAsync(
                            identity,
                            new WarehouseChange(
                                request.WarehouseId,
                                request.BranchId,
                                request.Code,
                                request.Name,
                                request.Address,
                                request.Description,
                                request.StatusCode,
                                OptionalVersion(request.RowVersion)
                            ),
                            cancellationToken
                        )
                    )
                )
        );
    }
);

app.MapPost(
    "/api/inventory/locations",
    async (
        SaveWarehouseLocationRequest request,
        HttpContext context,
        AuthenticationService authentication,
        InventoryService inventory,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        return await InventoryAction(
            context,
            async () =>
                Results.Ok(
                    new SavedInventoryResponse(
                        await inventory.SaveLocationAsync(
                            identity,
                            new WarehouseLocationChange(
                                request.LocationId,
                                request.WarehouseId,
                                request.Code,
                                request.Name,
                                request.Description,
                                request.StatusCode,
                                OptionalVersion(request.RowVersion)
                            ),
                            cancellationToken
                        )
                    )
                )
        );
    }
);

app.MapPut(
    "/api/inventory/stock/{stockBalanceId:long}/minimum",
    async (
        long stockBalanceId,
        UpdateMinimumStockRequest request,
        HttpContext context,
        AuthenticationService authentication,
        InventoryService inventory,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        return await InventoryAction(
            context,
            async () =>
            {
                await inventory.UpdateMinimumStockAsync(
                    identity,
                    new MinimumStockChange(
                        stockBalanceId,
                        request.MinimumStock,
                        Version(request.RowVersion)
                    ),
                    cancellationToken
                );
                return Results.NoContent();
            }
        );
    }
);

app.MapPost(
    "/api/inventory/movements",
    async (
        CreateInventoryMovementRequest request,
        HttpContext context,
        AuthenticationService authentication,
        InventoryService inventory,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        return await InventoryAction(
            context,
            async () =>
                Results.Ok(
                    new SavedInventoryResponse(
                        await inventory.CreateMovementAsync(
                            identity,
                            new InventoryMovementChange(
                                request.MovementType,
                                request.MovementDateUtc,
                                request.Observation,
                                request
                                    .Details.Select(detail => new InventoryMovementDetailChange(
                                        detail.ProductId,
                                        detail.OriginWarehouseId,
                                        detail.OriginLocationId,
                                        detail.DestinationWarehouseId,
                                        detail.DestinationLocationId,
                                        detail.Quantity
                                    ))
                                    .ToList()
                            ),
                            cancellationToken
                        )
                    )
                )
        );
    }
);

app.MapGet(
    "/api/commercial/sales",
    async (
        HttpContext context,
        AuthenticationService authentication,
        CommercialSalesService sales,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        return await CommercialAction(
            context,
            async () =>
                Results.Ok(ToSalesResponse(await sales.GetAsync(identity, cancellationToken)))
        );
    }
);

app.MapPost(
    "/api/commercial/price-lists",
    async (
        SavePriceListRequest request,
        HttpContext context,
        AuthenticationService authentication,
        CommercialSalesService sales,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        return await CommercialAction(
            context,
            async () =>
                Results.Ok(
                    new SavedCommercialResponse(
                        await sales.SavePriceListAsync(
                            identity,
                            new(
                                request.PriceListId,
                                request.Code,
                                request.Name,
                                request.Currency,
                                request.ValidFrom,
                                request.ValidUntil,
                                request.StatusCode,
                                OptionalVersion(request.RowVersion)
                            ),
                            cancellationToken
                        )
                    )
                )
        );
    }
);

app.MapPost(
    "/api/commercial/prices",
    async (
        SavePriceListProductRequest request,
        HttpContext context,
        AuthenticationService authentication,
        CommercialSalesService sales,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        return await CommercialAction(
            context,
            async () =>
                Results.Ok(
                    new SavedCommercialResponse(
                        await sales.SavePriceAsync(
                            identity,
                            new(
                                request.PriceListProductId,
                                request.PriceListId,
                                request.ProductId,
                                request.UnitPrice,
                                request.StatusCode,
                                OptionalVersion(request.RowVersion)
                            ),
                            cancellationToken
                        )
                    )
                )
        );
    }
);

app.MapPost(
    "/api/commercial/promotions",
    async (
        SavePromotionRequest request,
        HttpContext context,
        AuthenticationService authentication,
        CommercialSalesService sales,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        return await CommercialAction(
            context,
            async () =>
                Results.Ok(
                    new SavedCommercialResponse(
                        await sales.SavePromotionAsync(
                            identity,
                            new(
                                request.PromotionId,
                                request.ProductId,
                                request.Code,
                                request.Name,
                                request.Description,
                                request.PromotionType,
                                request.RequiredQuantity,
                                request.PaidQuantity,
                                request.DiscountedQuantity,
                                request.DiscountRate,
                                request.PackagePrice,
                                request.ValidFrom,
                                request.ValidUntil,
                                request.StatusCode,
                                OptionalVersion(request.RowVersion)
                            ),
                            cancellationToken
                        )
                    )
                )
        );
    }
);

app.MapPost(
    "/api/commercial/orders",
    async (
        SaveOrderRequest request,
        HttpContext context,
        AuthenticationService authentication,
        CommercialSalesService sales,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        return await CommercialAction(
            context,
            async () =>
                Results.Ok(
                    new SavedCommercialResponse(
                        await sales.SaveOrderAsync(
                            identity,
                            new(
                                request.OrderId,
                                request.ClientId,
                                request.PriceListId,
                                request.OrderDateUtc,
                                request.Currency,
                                request.Observation,
                                (request.Details ?? [])
                                    .Select(item =>
                                        item is null
                                            ? throw new ArgumentException(
                                                "Los renglones del pedido no pueden contener valores nulos."
                                            )
                                            : new OrderDetailChange(
                                                item.ProductId,
                                                item.WarehouseId,
                                                item.Quantity,
                                                item.UnitPrice,
                                                item.DiscountRate,
                                                item.TaxRate,
                                                item.PromotionId
                                            )
                                    )
                                    .ToList(),
                                OptionalVersion(request.RowVersion),
                                (request.Assets ?? [])
                                    .Select(item =>
                                        item is null
                                            ? throw new ArgumentException(
                                                "Los activos del pedido no pueden contener valores nulos."
                                            )
                                            : new OrderAssetChange(
                                                item.AssetId,
                                                item.ProductLineId,
                                                item.LinkType,
                                                item.ReturnMode,
                                                item.Observation,
                                                item.InboundMode,
                                                item.ExpectedReturnDate
                                            )
                                    )
                                    .ToList()
                            ),
                            cancellationToken
                        )
                    )
                )
        );
    }
);

app.MapPost(
    "/api/commercial/orders/{orderId:long}/confirm",
    async (
        long orderId,
        OrderTransitionRequest request,
        HttpContext context,
        AuthenticationService authentication,
        CommercialSalesService sales,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        return await CommercialAction(
            context,
            async () =>
            {
                await sales.ConfirmOrderAsync(
                    identity,
                    orderId,
                    Version(request.RowVersion),
                    cancellationToken
                );
                return Results.NoContent();
            }
        );
    }
);

app.MapPost(
    "/api/commercial/orders/{orderId:long}/cancel",
    async (
        long orderId,
        OrderTransitionRequest request,
        HttpContext context,
        AuthenticationService authentication,
        CommercialSalesService sales,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        return await CommercialAction(
            context,
            async () =>
            {
                await sales.CancelOrderAsync(
                    identity,
                    orderId,
                    Version(request.RowVersion),
                    cancellationToken
                );
                return Results.NoContent();
            }
        );
    }
);

app.MapPost(
    "/api/commercial/orders/{orderId:long}/sale",
    async (
        long orderId,
        CreateSaleRequest request,
        HttpContext context,
        AuthenticationService authentication,
        CommercialSalesService sales,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        return await CommercialAction(
            context,
            async () =>
                Results.Ok(
                    new SavedCommercialResponse(
                        await sales.CreateSaleAsync(
                            identity,
                            orderId,
                            request.SaleDateUtc,
                            Version(request.RowVersion),
                            cancellationToken
                        )
                    )
                )
        );
    }
);

app.MapGet(
    "/api/purchasing",
    async (
        HttpContext context,
        AuthenticationService authentication,
        PurchasingService purchasing,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        return await PurchasingAction(
            context,
            async () =>
                Results.Ok(
                    ToPurchasingResponse(await purchasing.GetAsync(identity, cancellationToken))
                )
        );
    }
);

app.MapPost(
    "/api/purchasing/suppliers",
    async (
        SaveSupplierRequest request,
        HttpContext context,
        AuthenticationService authentication,
        PurchasingService purchasing,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        return await PurchasingAction(
            context,
            async () =>
                Results.Ok(
                    new SavedPurchasingResponse(
                        await purchasing.SaveSupplierAsync(
                            identity,
                            new SupplierChange(
                                request.SupplierId,
                                request.LegalName,
                                request.TradeName,
                                request.TaxId,
                                request.Email,
                                request.Phone,
                                request.PaymentTerms,
                                request.Observation,
                                request.StatusCode,
                                (request.Contacts ?? [])
                                    .Select(contact => new SupplierContactChange(
                                        contact.Name,
                                        contact.Position,
                                        contact.Email,
                                        contact.Phone,
                                        contact.IsPrimary
                                    ))
                                    .ToList(),
                                OptionalVersion(request.RowVersion)
                            ),
                            cancellationToken
                        )
                    )
                )
        );
    }
);

app.MapPost(
    "/api/purchasing/orders",
    async (
        SavePurchaseOrderRequest request,
        HttpContext context,
        AuthenticationService authentication,
        PurchasingService purchasing,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        return await PurchasingAction(
            context,
            async () =>
                Results.Ok(
                    new SavedPurchasingResponse(
                        await purchasing.SaveOrderAsync(
                            identity,
                            new PurchaseOrderChange(
                                request.PurchaseOrderId,
                                request.SupplierId,
                                request.WarehouseId,
                                request.OrderDateUtc,
                                request.ExpectedDeliveryDate,
                                request.Currency,
                                request.Observation,
                                (request.Details ?? [])
                                    .Select(detail => new PurchaseOrderDetailChange(
                                        detail.ProductId,
                                        detail.Quantity,
                                        detail.UnitCost,
                                        detail.DiscountRate,
                                        detail.TaxRate
                                    ))
                                    .ToList(),
                                OptionalVersion(request.RowVersion)
                            ),
                            cancellationToken
                        )
                    )
                )
        );
    }
);

app.MapPost(
    "/api/purchasing/orders/{orderId:long}/submit",
    async (
        long orderId,
        PurchaseOrderTransitionRequest request,
        HttpContext context,
        AuthenticationService authentication,
        PurchasingService purchasing,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        return await PurchasingAction(
            context,
            async () =>
            {
                await purchasing.SubmitOrderAsync(
                    identity,
                    orderId,
                    Version(request.RowVersion),
                    cancellationToken
                );
                return Results.NoContent();
            }
        );
    }
);

app.MapPost(
    "/api/purchasing/orders/{orderId:long}/approve",
    async (
        long orderId,
        PurchaseOrderTransitionRequest request,
        HttpContext context,
        AuthenticationService authentication,
        PurchasingService purchasing,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        return await PurchasingAction(
            context,
            async () =>
            {
                await purchasing.ApproveOrderAsync(
                    identity,
                    orderId,
                    Version(request.RowVersion),
                    cancellationToken
                );
                return Results.NoContent();
            }
        );
    }
);

app.MapPost(
    "/api/purchasing/orders/{orderId:long}/cancel",
    async (
        long orderId,
        PurchaseOrderTransitionRequest request,
        HttpContext context,
        AuthenticationService authentication,
        PurchasingService purchasing,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        return await PurchasingAction(
            context,
            async () =>
            {
                await purchasing.CancelOrderAsync(
                    identity,
                    orderId,
                    Version(request.RowVersion),
                    cancellationToken
                );
                return Results.NoContent();
            }
        );
    }
);

app.MapPost(
    "/api/purchasing/orders/{orderId:long}/close",
    async (
        long orderId,
        ClosePurchaseOrderRequest request,
        HttpContext context,
        AuthenticationService authentication,
        PurchasingService purchasing,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        return await PurchasingAction(
            context,
            async () =>
            {
                await purchasing.CloseOrderAsync(
                    identity,
                    orderId,
                    request.Reason,
                    Version(request.RowVersion),
                    cancellationToken
                );
                return Results.NoContent();
            }
        );
    }
);

app.MapPost(
    "/api/purchasing/orders/{orderId:long}/receipts",
    async (
        long orderId,
        CreateGoodsReceiptRequest request,
        HttpContext context,
        AuthenticationService authentication,
        PurchasingService purchasing,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        return await PurchasingAction(
            context,
            async () =>
                Results.Ok(
                    new SavedPurchasingResponse(
                        await purchasing.CreateReceiptAsync(
                            identity,
                            new GoodsReceiptChange(
                                orderId,
                                request.ReceiptDateUtc,
                                request.SupplierDocumentNumber,
                                request.Observation,
                                (request.Details ?? [])
                                    .Select(detail => new GoodsReceiptDetailChange(
                                        detail.PurchaseOrderLineId,
                                        detail.AcceptedQuantity,
                                        detail.RejectedQuantity,
                                        detail.DamagedQuantity,
                                        detail.DifferenceReason,
                                        (detail.Lots ?? [])
                                            .Select(lot => new GoodsReceiptLotChange(
                                                lot.Code,
                                                lot.Quantity,
                                                lot.ManufactureDate,
                                                lot.ExpirationDate
                                            ))
                                            .ToList(),
                                        (detail.SerialNumbers ?? [])
                                            .Select(serial => new GoodsReceiptSerialChange(
                                                serial.SerialNumber,
                                                serial.AssetType,
                                                serial.Capacity,
                                                serial.OwnerCode,
                                                serial.OwnerName,
                                                serial.ConditionCode
                                            ))
                                            .ToList(),
                                        (detail.ContainerContents ?? [])
                                            .Select(
                                                content => new GoodsReceiptContainerContentChange(
                                                    content.AssetSerialNumber,
                                                    content.ContentProductId,
                                                    content.LotCode,
                                                    content.Quantity,
                                                    content.MeasurementMethod
                                                )
                                            )
                                            .ToList()
                                    ))
                                    .ToList(),
                                Version(request.OrderRowVersion)
                            ),
                            cancellationToken
                        )
                    )
                )
        );
    }
);

app.MapPost(
    "/api/purchasing/receipts/{receiptId:long}/reverse",
    async (
        long receiptId,
        ReverseGoodsReceiptRequest request,
        HttpContext context,
        AuthenticationService authentication,
        PurchasingService purchasing,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        return await PurchasingAction(
            context,
            async () =>
            {
                await purchasing.ReverseReceiptAsync(
                    identity,
                    receiptId,
                    request.Reason,
                    Version(request.RowVersion),
                    cancellationToken
                );
                return Results.NoContent();
            }
        );
    }
);

app.MapGet(
    "/api/inventory/traceability",
    async (
        HttpContext context,
        AuthenticationService authentication,
        TraceabilityService service,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        return await InventoryAction(
            context,
            async () =>
                Results.Ok(
                    ToTraceabilityResponse(await service.GetAsync(identity, cancellationToken))
                )
        );
    }
);

app.MapPost(
    "/api/inventory/traceability/client-assets",
    async (
        SaveClientAssetRequest request,
        HttpContext context,
        AuthenticationService authentication,
        TraceabilityService service,
        CancellationToken cancellationToken
    ) =>
        await TraceabilityCreate(
            context,
            authentication,
            (identity, token) =>
                service.SaveClientAssetAsync(
                    identity,
                    new(
                        request.ClientId,
                        request.ProductId,
                        request.WarehouseId,
                        request.SerialNumber,
                        request.AssetType,
                        request.Capacity,
                        request.CapacityUnit,
                        request.ConditionCode,
                        request.IsInCustody,
                        request.Observation
                    ),
                    token
                ),
            cancellationToken
        )
);

app.MapPost(
    "/api/inventory/traceability/client-assets/{id:long}/custody",
    async (
        long id,
        ChangeClientAssetCustodyRequest request,
        HttpContext context,
        AuthenticationService authentication,
        TraceabilityService service,
        CancellationToken cancellationToken
    ) =>
    {
        if (request.AssetId != id)
        {
            return ControlledError(
                context,
                30001,
                "El activo de la ruta no coincide con el cuerpo de la solicitud."
            );
        }

        return await TraceabilityCreate(
            context,
            authentication,
            (identity, token) =>
                service.ChangeClientAssetCustodyAsync(
                    identity,
                    new(
                        id,
                        request.Action,
                        request.WarehouseId,
                        request.Observation,
                        Version(request.RowVersion)
                    ),
                    token
                ),
            cancellationToken
        );
    }
);

app.MapPost(
    "/api/inventory/traceability/measurements",
    async (
        CreateAssetMeasurementRequest request,
        HttpContext context,
        AuthenticationService authentication,
        TraceabilityService service,
        CancellationToken cancellationToken
    ) =>
        await TraceabilityCreate(
            context,
            authentication,
            (identity, token) =>
                service.CreateMeasurementAsync(
                    identity,
                    new(
                        request.AssetId,
                        request.MeasurementDateUtc,
                        request.MeasurementType,
                        request.Value,
                        request.Unit,
                        request.Method,
                        request.Source,
                        request.IsEstimated,
                        request.DeviceReference,
                        request.Observation
                    ),
                    token
                ),
            cancellationToken
        )
);

app.MapPost(
    "/api/inventory/traceability/transformations",
    async (
        CreateTransformationRequest request,
        HttpContext context,
        AuthenticationService authentication,
        TraceabilityService service,
        CancellationToken cancellationToken
    ) =>
        await TraceabilityCreate(
            context,
            authentication,
            (identity, token) =>
                service.CreateTransformationAsync(
                    identity,
                    new(
                        request.SourceLotId,
                        request.SourceAssetId,
                        request.ContentProductId,
                        request.TransformationDateUtc,
                        request.SourceQuantity,
                        request.LossQuantity,
                        request.Method,
                        request.LossReason,
                        request.Observation,
                        (request.Destinations ?? [])
                            .Select(item => new TransformationDestinationChange(
                                item.AssetId,
                                item.LotId,
                                item.Quantity
                            ))
                            .ToList()
                    ),
                    token
                ),
            cancellationToken
        )
);

app.MapPost(
    "/api/inventory/traceability/incidents",
    async (
        CreateIncidentRequest request,
        HttpContext context,
        AuthenticationService authentication,
        TraceabilityService service,
        CancellationToken cancellationToken
    ) =>
        await TraceabilityCreate(
            context,
            authentication,
            (identity, token) =>
                service.CreateIncidentAsync(
                    identity,
                    new(
                        request.ProductId,
                        request.WarehouseId,
                        request.AssetId,
                        request.LotId,
                        request.IncidentType,
                        request.IncidentDateUtc,
                        request.QuantityBefore,
                        request.LossQuantity,
                        request.QuantityAfter,
                        request.IsEstimated,
                        request.MeasurementMethod,
                        request.Cause,
                        request.ActionTaken,
                        request.EvidenceReference
                    ),
                    token
                ),
            cancellationToken
        )
);

app.MapPost(
    "/api/inventory/traceability/maintenances",
    async (
        CreateMaintenanceRequest request,
        HttpContext context,
        AuthenticationService authentication,
        TraceabilityService service,
        CancellationToken cancellationToken
    ) =>
        await TraceabilityCreate(
            context,
            authentication,
            (identity, token) =>
                service.CreateMaintenanceAsync(
                    identity,
                    new(
                        request.AssetId,
                        request.IncidentId,
                        request.SupplierId,
                        request.MaintenanceType,
                        request.StartDateUtc,
                        request.WorkDescription,
                        request.Cost,
                        null
                    ),
                    token
                ),
            cancellationToken
        )
);

app.MapPost(
    "/api/inventory/traceability/maintenances/{id:long}/complete",
    async (
        long id,
        CompleteMaintenanceRequest request,
        HttpContext context,
        AuthenticationService authentication,
        TraceabilityService service,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        if (request.MaintenanceId != id)
            return ControlledError(
                context,
                30001,
                "El mantenimiento de la ruta no coincide con el cuerpo de la solicitud."
            );
        return await InventoryAction(
            context,
            async () =>
            {
                await service.CompleteMaintenanceAsync(
                    identity,
                    new(
                        id,
                        request.EndDateUtc,
                        request.Result,
                        request.OldComponent,
                        request.NewComponent,
                        request.Cost,
                        request.CertificateReference,
                        request.NextReviewDate,
                        Version(request.RowVersion)
                    ),
                    cancellationToken
                );
                return Results.NoContent();
            }
        );
    }
);

app.MapPost(
    "/api/inventory/traceability/loans",
    async (
        CreateLoanRequest request,
        HttpContext context,
        AuthenticationService authentication,
        TraceabilityService service,
        CancellationToken cancellationToken
    ) =>
        await TraceabilityCreate(
            context,
            authentication,
            (identity, token) =>
                service.CreateLoanAsync(
                    identity,
                    new(
                        request.DestinationType,
                        request.DestinationBranchId,
                        request.DestinationClientId,
                        request.ExternalDestination,
                        request.DeliveryMode,
                        request.DepartureDateUtc,
                        request.ExpectedReturnDateUtc,
                        request.Observation,
                        request.DestinationCompanyCode,
                        request.IntercompanyCorrelationId,
                        (request.Assets ?? [])
                            .Select(item => new LoanAssetChange(
                                item.AssetId,
                                item.LotId,
                                item.Quantity,
                                item.ConditionCode
                            ))
                            .ToList()
                    ),
                    token
                ),
            cancellationToken
        )
);

app.MapPost(
    "/api/inventory/traceability/loans/{id:long}/return",
    async (
        long id,
        ReturnLoanRequest request,
        HttpContext context,
        AuthenticationService authentication,
        TraceabilityService service,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        if (request.LoanId != id)
            return ControlledError(
                context,
                30001,
                "El préstamo de la ruta no coincide con el cuerpo de la solicitud."
            );
        return await InventoryAction(
            context,
            async () =>
            {
                if (request.Assets?.Any(item => item is null) == true)
                    throw new ArgumentException("La devolución contiene un renglón vacío.");
                await service.ReturnLoanAsync(
                    identity,
                    new(
                        id,
                        request.ReturnDateUtc,
                        request.Observation,
                        (request.Assets ?? [])
                            .Select(item => new LoanAssetReturnChange(
                                item.LoanLineId,
                                item.Quantity,
                                item.ConditionCode,
                                item.Observation
                            ))
                            .ToList(),
                        Version(request.RowVersion)
                    ),
                    cancellationToken
                );
                return Results.NoContent();
            }
        );
    }
);

app.MapGet(
    "/api/logistics",
    async (
        HttpContext context,
        AuthenticationService authentication,
        LogisticsService service,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
        {
            return AuthenticationError(context);
        }

        return await LogisticsAction(
            context,
            async () =>
                Results.Ok(ToLogisticsResponse(await service.GetAsync(identity, cancellationToken)))
        );
    }
);

app.MapGet(
    "/api/driver",
    async (
        HttpContext context,
        AuthenticationService authentication,
        LogisticsService service,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
        {
            return AuthenticationError(context);
        }

        return await LogisticsAction(
            context,
            async () =>
                Results.Ok(
                    ToDriverResponse(await service.GetDriverAsync(identity, cancellationToken))
                )
        );
    }
);

app.MapPost(
    "/api/logistics/addresses",
    async (
        SaveLogisticsAddressRequest request,
        HttpContext context,
        AuthenticationService authentication,
        LogisticsService service,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
        {
            return AuthenticationError(context);
        }

        var change = new AddressChange(
            request.ClientId,
            request.Name,
            request.Address,
            request.City,
            request.Province,
            request.PostalCode,
            request.Contact,
            request.Phone,
            request.Email,
            request.FromTime,
            request.ToTime,
            request.Instructions
        );

        return await LogisticsAction(
            context,
            async () =>
                Results.Ok(
                    new SavedLogisticsResponse(
                        await service.SaveAddressAsync(identity, change, cancellationToken)
                    )
                )
        );
    }
);

app.MapPost(
    "/api/logistics/requests",
    async (
        CreateLogisticsRequest request,
        HttpContext context,
        AuthenticationService authentication,
        LogisticsService service,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
        {
            return AuthenticationError(context);
        }

        var assets = (request.Assets ?? [])
            .Select(item => new RequestAssetChange(
                item.AssetId,
                item.Owner,
                item.Role,
                item.SerialNumber,
                item.Product,
                item.ContentQuantity,
                item.Unit,
                item.Condition,
                item.Observation,
                item.ExpectedReturnDate
            ))
            .ToList();
        var change = new LogisticsRequestChange(
            request.ClientId,
            request.AddressId,
            request.OrderId,
            request.ServiceType,
            request.Priority,
            request.RequestedDate,
            request.FromTime,
            request.ToTime,
            request.Instructions,
            request.Observation,
            assets,
            request.DestinationWarehouseId
        );

        return await LogisticsAction(
            context,
            async () =>
                Results.Ok(
                    new SavedLogisticsResponse(
                        await service.CreateRequestAsync(identity, change, cancellationToken)
                    )
                )
        );
    }
);

app.MapPost(
    "/api/logistics/transporters",
    async (
        SaveTransporterRequest request,
        HttpContext context,
        AuthenticationService authentication,
        LogisticsService service,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        var change = new TransporterChange(
            request.TransporterId,
            request.UserId,
            request.RelationshipType,
            request.Document,
            request.Phone,
            request.License,
            request.LicenseCategory,
            request.LicenseExpiration,
            request.Observation,
            request.StatusCode,
            OptionalVersion(request.RowVersion)
        );
        return await LogisticsAction(
            context,
            async () =>
                Results.Ok(
                    new SavedLogisticsResponse(
                        await service.SaveTransporterAsync(identity, change, cancellationToken)
                    )
                )
        );
    }
);

app.MapPost(
    "/api/logistics/vehicles",
    async (
        SaveLogisticsVehicleRequest request,
        HttpContext context,
        AuthenticationService authentication,
        LogisticsService service,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        var change = new LogisticsVehicleChange(
            request.VehicleId,
            request.OwnerTransporterId,
            request.Plate,
            request.VehicleType,
            request.OwnershipType,
            request.Brand,
            request.Model,
            request.Year,
            request.LoadCapacityKg,
            request.InsurancePolicy,
            request.InsuranceExpiration,
            request.InspectionExpiration,
            request.Observation,
            request.StatusCode,
            OptionalVersion(request.RowVersion)
        );
        return await LogisticsAction(
            context,
            async () =>
                Results.Ok(
                    new SavedLogisticsResponse(
                        await service.SaveVehicleAsync(identity, change, cancellationToken)
                    )
                )
        );
    }
);

app.MapPost(
    "/api/logistics/routes",
    async (
        CreateRouteRequest request,
        HttpContext context,
        AuthenticationService authentication,
        LogisticsService service,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
        {
            return AuthenticationError(context);
        }

        var requests = (request.Requests ?? [])
            .Select(item => new RouteRequestChange(item.Order, item.RequestId))
            .ToList();
        var change = new RouteChange(
            request.RouteDate,
            request.RouteType,
            request.AssignmentType,
            request.TransporterId,
            request.VehicleId,
            request.Observation,
            requests
        );

        return await LogisticsAction(
            context,
            async () =>
                Results.Ok(
                    new SavedLogisticsResponse(
                        await service.CreateRouteAsync(identity, change, cancellationToken)
                    )
                )
        );
    }
);

app.MapPost(
    "/api/logistics/routes/{id:long}/assign",
    async (
        long id,
        AssignRouteRequest request,
        HttpContext context,
        AuthenticationService authentication,
        LogisticsService service,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
        {
            return AuthenticationError(context);
        }

        return await LogisticsAction(
            context,
            async () =>
            {
                await service.AssignRouteAsync(
                    identity,
                    id,
                    new(request.TransporterId, request.VehicleId, Version(request.RowVersion)),
                    cancellationToken
                );
                return Results.NoContent();
            }
        );
    }
);

app.MapPut(
    "/api/logistics/routes/{id:long}/offer",
    async (
        long id,
        UpdateRouteOfferRequest request,
        HttpContext context,
        AuthenticationService authentication,
        LogisticsService service,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
        {
            return AuthenticationError(context);
        }

        return await LogisticsAction(
            context,
            async () =>
            {
                await service.UpdateRouteOfferAsync(
                    identity,
                    id,
                    new(
                        request.RouteDate,
                        request.RouteType,
                        request.Observation,
                        Version(request.RowVersion)
                    ),
                    cancellationToken
                );
                return Results.NoContent();
            }
        );
    }
);

app.MapPost(
    "/api/driver/offers/{id:long}/claim",
    async (
        long id,
        ClaimRouteOfferRequest request,
        HttpContext context,
        AuthenticationService authentication,
        LogisticsService service,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
        {
            return AuthenticationError(context);
        }

        return await LogisticsAction(
            context,
            async () =>
            {
                await service.ClaimRouteOfferAsync(
                    identity,
                    id,
                    request.VehicleId,
                    Version(request.RowVersion),
                    cancellationToken
                );
                return Results.NoContent();
            }
        );
    }
);

app.MapPost(
    "/api/logistics/routes/{id:long}/dispatch",
    async (
        long id,
        LogisticsTransitionRequest request,
        HttpContext context,
        AuthenticationService authentication,
        LogisticsService service,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
        {
            return AuthenticationError(context);
        }

        return await LogisticsAction(
            context,
            async () =>
            {
                await service.ChangeRouteAsync(
                    identity,
                    id,
                    Version(request.RowVersion),
                    false,
                    request.Observation,
                    cancellationToken
                );
                return Results.NoContent();
            }
        );
    }
);

app.MapPost(
    "/api/logistics/routes/{id:long}/cancel",
    async (
        long id,
        LogisticsTransitionRequest request,
        HttpContext context,
        AuthenticationService authentication,
        LogisticsService service,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
        {
            return AuthenticationError(context);
        }

        return await LogisticsAction(
            context,
            async () =>
            {
                await service.ChangeRouteAsync(
                    identity,
                    id,
                    Version(request.RowVersion),
                    true,
                    request.Observation,
                    cancellationToken
                );
                return Results.NoContent();
            }
        );
    }
);

app.MapPost(
    "/api/logistics/routes/{id:long}/pause",
    async (
        long id,
        LogisticsTransitionRequest request,
        HttpContext context,
        AuthenticationService authentication,
        LogisticsService service,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
        {
            return AuthenticationError(context);
        }

        return await LogisticsAction(
            context,
            async () =>
            {
                await service.ChangeRoutePauseAsync(
                    identity,
                    id,
                    Version(request.RowVersion),
                    true,
                    request.Observation,
                    cancellationToken
                );
                return Results.NoContent();
            }
        );
    }
);

app.MapPost(
    "/api/logistics/routes/{id:long}/resume",
    async (
        long id,
        LogisticsTransitionRequest request,
        HttpContext context,
        AuthenticationService authentication,
        LogisticsService service,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
        {
            return AuthenticationError(context);
        }

        return await LogisticsAction(
            context,
            async () =>
            {
                await service.ChangeRoutePauseAsync(
                    identity,
                    id,
                    Version(request.RowVersion),
                    false,
                    request.Observation,
                    cancellationToken
                );
                return Results.NoContent();
            }
        );
    }
);

app.MapPost(
    "/api/logistics/stops/{id:long}/arrival",
    async (
        long id,
        LogisticsTransitionRequest request,
        HttpContext context,
        AuthenticationService authentication,
        LogisticsService service,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
        {
            return AuthenticationError(context);
        }

        return await LogisticsAction(
            context,
            async () =>
            {
                await service.MarkStopArrivalAsync(
                    identity,
                    id,
                    Version(request.RowVersion),
                    cancellationToken
                );
                return Results.NoContent();
            }
        );
    }
);

app.MapPost(
    "/api/logistics/stops/{id:long}/incidents",
    async (
        long id,
        ReportRouteStopIncidentRequest request,
        HttpContext context,
        AuthenticationService authentication,
        LogisticsService service,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
        {
            return AuthenticationError(context);
        }

        return await LogisticsAction(
            context,
            async () =>
            {
                await service.ReportStopIncidentAsync(
                    identity,
                    id,
                    request.Type,
                    request.Observation,
                    Version(request.RowVersion),
                    cancellationToken
                );
                return Results.NoContent();
            }
        );
    }
);

app.MapPost(
    "/api/logistics/stops/{id:long}/complete",
    async (
        long id,
        CompleteRouteStopRequest request,
        HttpContext context,
        AuthenticationService authentication,
        LogisticsService service,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
        {
            return AuthenticationError(context);
        }

        return await LogisticsAction(
            context,
            async () =>
            {
                await service.CompleteStopAsync(
                    identity,
                    id,
                    request.Result,
                    request.Observation,
                    Version(request.RowVersion),
                    request.CompletedRequestAssetIds,
                    cancellationToken
                );
                return Results.NoContent();
            }
        );
    }
);

app.MapGet(
    "/api/finance",
    async (
        HttpContext context,
        AuthenticationService authentication,
        FinanceService service,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        return await FinanceAction(
            context,
            async () =>
                Results.Ok(ToFinanceResponse(await service.GetAsync(identity, cancellationToken)))
        );
    }
);

app.MapPost(
    "/api/finance/payment-methods",
    async (
        SavePaymentMethodRequest request,
        HttpContext context,
        AuthenticationService authentication,
        FinanceService service,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        var change = new PaymentMethodChange(
            request.PaymentMethodId,
            request.Code,
            request.Name,
            request.Type,
            request.AffectsCash,
            request.RequiresReference,
            request.StatusCode,
            OptionalVersion(request.RowVersion)
        );
        return await FinanceAction(
            context,
            async () =>
                Results.Ok(
                    new SavedFinanceResponse(
                        await service.SavePaymentMethodAsync(identity, change, cancellationToken)
                    )
                )
        );
    }
);

app.MapPost(
    "/api/finance/cash-boxes",
    async (
        SaveCashBoxRequest request,
        HttpContext context,
        AuthenticationService authentication,
        FinanceService service,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        var change = new CashBoxChange(
            request.CashBoxId,
            request.BranchId,
            request.Code,
            request.Name,
            request.Currency,
            request.StatusCode,
            OptionalVersion(request.RowVersion)
        );
        return await FinanceAction(
            context,
            async () =>
                Results.Ok(
                    new SavedFinanceResponse(
                        await service.SaveCashBoxAsync(identity, change, cancellationToken)
                    )
                )
        );
    }
);

app.MapPost(
    "/api/finance/cash-sessions/open",
    async (
        OpenCashSessionRequest request,
        HttpContext context,
        AuthenticationService authentication,
        FinanceService service,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        return await FinanceAction(
            context,
            async () =>
                Results.Ok(
                    new SavedFinanceResponse(
                        await service.OpenCashSessionAsync(
                            identity,
                            request.CashBoxId,
                            request.OpeningAmount,
                            request.Observation,
                            cancellationToken
                        )
                    )
                )
        );
    }
);

app.MapPost(
    "/api/finance/cash-sessions/{id:long}/close",
    async (
        long id,
        CloseCashSessionRequest request,
        HttpContext context,
        AuthenticationService authentication,
        FinanceService service,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        return await FinanceAction(
            context,
            async () =>
            {
                await service.CloseCashSessionAsync(
                    identity,
                    id,
                    request.CountedAmount,
                    request.Observation,
                    Version(request.RowVersion),
                    cancellationToken
                );
                return Results.NoContent();
            }
        );
    }
);

app.MapPost(
    "/api/finance/payments",
    async (
        RegisterCustomerPaymentRequest request,
        HttpContext context,
        AuthenticationService authentication,
        FinanceService service,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        var methods = (request.Methods ?? [])
            .Select(item => new PaymentMethodAmount(
                item.PaymentMethodId,
                item.Amount,
                item.Reference
            ))
            .ToList();
        var applications = (request.Applications ?? [])
            .Select(item => new SaleApplicationAmount(item.SaleId, item.Amount))
            .ToList();
        var change = new CustomerPaymentChange(
            request.ClientId,
            request.CashSessionId,
            request.PaymentDateUtc,
            request.Currency,
            request.Total,
            request.Observation,
            methods,
            applications
        );
        return await FinanceAction(
            context,
            async () =>
                Results.Ok(
                    new SavedFinanceResponse(
                        await service.RegisterPaymentAsync(identity, change, cancellationToken)
                    )
                )
        );
    }
);

app.MapPost(
    "/api/finance/payments/{id:long}/reverse",
    async (
        long id,
        ReverseCustomerPaymentRequest request,
        HttpContext context,
        AuthenticationService authentication,
        FinanceService service,
        CancellationToken cancellationToken
    ) =>
    {
        var identity = await GetIdentityAsync(context, authentication, cancellationToken);
        if (identity is null)
            return AuthenticationError(context);
        return await FinanceAction(
            context,
            async () =>
            {
                await service.ReversePaymentAsync(
                    identity,
                    id,
                    request.Reason,
                    Version(request.RowVersion),
                    cancellationToken
                );
                return Results.NoContent();
            }
        );
    }
);

app.Run();

static async Task<IResult> TraceabilityCreate(
    HttpContext context,
    AuthenticationService authentication,
    Func<SessionIdentity, CancellationToken, Task<long>> operation,
    CancellationToken cancellationToken
)
{
    var identity = await GetIdentityAsync(context, authentication, cancellationToken);
    if (identity is null)
        return AuthenticationError(context);
    return await InventoryAction(
        context,
        async () =>
            Results.Ok(new SavedTraceabilityResponse(await operation(identity, cancellationToken)))
    );
}

static async Task<SessionIdentity?> GetIdentityAsync(
    HttpContext context,
    AuthenticationService authenticationService,
    CancellationToken cancellationToken
)
{
    var authorization = context.Request.Headers.Authorization.ToString();
    if (
        !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
        || string.IsNullOrWhiteSpace(authorization[7..])
    )
        return null;

    var identity = await authenticationService.ValidateSessionAsync(
        authorization[7..].Trim(),
        cancellationToken
    );
    if (identity is not null)
        context.Items["CompanyCode"] = identity.CompanyCode;
    return identity;
}

static ClientDraft ToDraft(SaveClientRequest request) =>
    new(
        request.PersonType ?? string.Empty,
        request.NameOrBusinessName ?? string.Empty,
        request.Surname,
        request.DocumentType,
        request.DocumentNumber,
        request.Email,
        request.Observation,
        (request.Phones ?? [])
            .Select(phone => new ClientPhoneDraft(
                phone.TypeCode ?? string.Empty,
                phone.CountryCode,
                phone.AreaCode,
                phone.Number,
                phone.Extension,
                phone.IsPrimary,
                phone.AllowsWhatsApp,
                phone.Observation
            ))
            .ToList()
    );

static SaveClientRequest ToRequest(ClientDraft draft) =>
    new(
        draft.PersonType,
        draft.NameOrBusinessName,
        draft.Surname,
        draft.DocumentType,
        draft.DocumentNumber,
        draft.Email,
        draft.Observation,
        draft
            .Phones.Select(phone => new ClientPhoneRequest(
                phone.TypeCode,
                phone.CountryCode,
                phone.AreaCode,
                phone.Number,
                phone.Extension,
                phone.IsPrimary,
                phone.AllowsWhatsApp,
                phone.Observation
            ))
            .ToList()
    );

static IResult ControlledError(
    HttpContext context,
    long errorCode,
    string message,
    int statusCode = StatusCodes.Status400BadRequest
) =>
    Results.Json(
        new ApiErrorResponse(
            errorCode,
            UserMessage(message),
            context.Items["CorrelationId"] is Guid correlationId ? correlationId : Guid.NewGuid()
        ),
        statusCode: statusCode
    );

static string UserMessage(string message)
{
    var parameterIndex = message.IndexOf(" (Parameter '", StringComparison.Ordinal);
    return parameterIndex > 0 ? message[..parameterIndex] : message;
}

static IResult SqlValidationError(
    HttpContext context,
    long errorCode,
    Microsoft.Data.SqlClient.SqlException exception,
    string duplicateMessage
) =>
    ControlledError(
        context,
        errorCode,
        exception.Number is 2601 or 2627 ? duplicateMessage : exception.Message
    );

static IResult AuthenticationError(HttpContext context) =>
    ControlledError(
        context,
        10001,
        "La sesión o las credenciales no son válidas.",
        StatusCodes.Status401Unauthorized
    );

static IResult AuthorizationError(HttpContext context) =>
    ControlledError(
        context,
        10004,
        "No posee permisos para realizar esta operación.",
        StatusCodes.Status403Forbidden
    );

static ClientDetailResponse ToResponse(ClientDetails client) =>
    new(
        client.ClientId,
        client.Code,
        client.PersonType,
        client.NameOrBusinessName,
        client.Surname,
        client.DocumentType,
        client.DocumentNumber,
        client.Email,
        client.Observation,
        client.StatusCode,
        client
            .Phones.Select(phone => new ClientPhoneResponse(
                phone.PhoneId,
                phone.TypeCode,
                phone.TypeName,
                phone.CountryCode,
                phone.AreaCode,
                phone.Number,
                phone.Extension,
                phone.Order,
                phone.IsPrimary,
                phone.AllowsWhatsApp,
                phone.Observation,
                phone.StatusCode
            ))
            .ToList()
    );

static async Task<IResult> ConfigurationAction(
    HttpContext context,
    Func<Task<IResult>> action,
    long validationErrorCode = 20001
)
{
    try
    {
        return await action();
    }
    catch (UnauthorizedAccessException)
    {
        return AuthorizationError(context);
    }
    catch (ArgumentException exception)
    {
        return ControlledError(context, validationErrorCode, exception.Message);
    }
    catch (ConfigurationOperationException exception)
    {
        return ControlledError(
            context,
            exception.ErrorCode,
            exception.Message,
            exception.ErrorCode == 20003
                ? StatusCodes.Status409Conflict
                : StatusCodes.Status400BadRequest
        );
    }
    catch (Microsoft.Data.SqlClient.SqlException exception)
        when (exception.Number is 2601 or 2627 || exception.Number >= 50000)
    {
        return ControlledError(
            context,
            validationErrorCode,
            exception.Number is 2601 or 2627
                ? "Ya existe un registro con los mismos datos técnicos."
                : exception.Message
        );
    }
}

static async Task<IResult> InventoryAction(HttpContext context, Func<Task<IResult>> action)
{
    try
    {
        return await action();
    }
    catch (UnauthorizedAccessException)
    {
        return AuthorizationError(context);
    }
    catch (ArgumentException exception)
    {
        return ControlledError(context, 30001, exception.Message);
    }
    catch (InventoryOperationException exception)
    {
        return ControlledError(
            context,
            exception.ErrorCode,
            exception.Message,
            exception.ErrorCode == 30003
                ? StatusCodes.Status409Conflict
                : StatusCodes.Status400BadRequest
        );
    }
    catch (Microsoft.Data.SqlClient.SqlException exception)
        when (exception.Number is 2601 or 2627 || exception.Number >= 50000)
    {
        return ControlledError(
            context,
            30001,
            exception.Number is 2601 or 2627
                ? "Ya existe un registro con los mismos datos técnicos."
                : exception.Message
        );
    }
}

static async Task<IResult> CommercialAction(HttpContext context, Func<Task<IResult>> action)
{
    try
    {
        return await action();
    }
    catch (UnauthorizedAccessException)
    {
        return AuthorizationError(context);
    }
    catch (ArgumentException exception)
    {
        return ControlledError(context, 40004, exception.Message);
    }
    catch (CommercialOperationException exception)
    {
        return ControlledError(
            context,
            exception.ErrorCode,
            exception.Message,
            exception.ErrorCode == 40007
                ? StatusCodes.Status409Conflict
                : StatusCodes.Status400BadRequest
        );
    }
    catch (Microsoft.Data.SqlClient.SqlException exception)
        when (exception.Number is 2601 or 2627 || exception.Number >= 50000)
    {
        return ControlledError(
            context,
            40004,
            exception.Number is 2601 or 2627
                ? "Ya existe un registro con los mismos datos comerciales."
                : exception.Message
        );
    }
}

static async Task<IResult> PurchasingAction(HttpContext context, Func<Task<IResult>> action)
{
    try
    {
        return await action();
    }
    catch (UnauthorizedAccessException)
    {
        return AuthorizationError(context);
    }
    catch (ArgumentException exception)
    {
        return ControlledError(context, 50004, exception.Message);
    }
    catch (PurchasingOperationException exception)
    {
        return ControlledError(
            context,
            exception.ErrorCode,
            exception.Message,
            exception.ErrorCode == 50007
                ? StatusCodes.Status409Conflict
                : StatusCodes.Status400BadRequest
        );
    }
    catch (Microsoft.Data.SqlClient.SqlException exception)
        when (exception.Number is 2601 or 2627 || exception.Number >= 50000)
    {
        return ControlledError(
            context,
            50004,
            exception.Number is 2601 or 2627
                ? "Ya existe un registro con los mismos datos técnicos."
                : exception.Message
        );
    }
}

static async Task<IResult> LogisticsAction(HttpContext context, Func<Task<IResult>> action)
{
    try
    {
        return await action();
    }
    catch (UnauthorizedAccessException)
    {
        return AuthorizationError(context);
    }
    catch (ArgumentException exception)
    {
        return ControlledError(context, 60001, exception.Message);
    }
    catch (LogisticsOperationException exception)
    {
        return ControlledError(
            context,
            exception.ErrorCode,
            exception.Message,
            exception.ErrorCode == 60003
                ? StatusCodes.Status409Conflict
                : StatusCodes.Status400BadRequest
        );
    }
    catch (Microsoft.Data.SqlClient.SqlException exception)
        when (exception.Number is 2601 or 2627 || exception.Number >= 50000)
    {
        return ControlledError(
            context,
            60001,
            exception.Number is 2601 or 2627
                ? "Ya existe un registro logístico con esos datos."
                : exception.Message
        );
    }
}

static async Task<IResult> FinanceAction(HttpContext context, Func<Task<IResult>> action)
{
    try
    {
        return await action();
    }
    catch (UnauthorizedAccessException)
    {
        return AuthorizationError(context);
    }
    catch (ArgumentException exception)
    {
        return ControlledError(context, 80001, exception.Message);
    }
    catch (FinanceOperationException exception)
    {
        return ControlledError(
            context,
            exception.ErrorCode,
            exception.Message,
            exception.ErrorCode is 80002 or 80004
                ? StatusCodes.Status409Conflict
                : StatusCodes.Status400BadRequest
        );
    }
    catch (Microsoft.Data.SqlClient.SqlException exception)
        when (exception.Number is 2601 or 2627 || exception.Number >= 50000)
    {
        return ControlledError(
            context,
            80001,
            exception.Number is 2601 or 2627
                ? "Ya existe un registro financiero con esos datos."
                : exception.Message
        );
    }
}

static byte[] Version(string value)
{
    try
    {
        var version = Convert.FromBase64String(value ?? string.Empty);
        return version.Length == 8 ? version : throw new FormatException();
    }
    catch (FormatException)
    {
        throw new ArgumentException("La versión del registro no es válida.", nameof(value));
    }
}

static byte[]? OptionalVersion(string? value) =>
    string.IsNullOrWhiteSpace(value) ? null : Version(value);

static ConfigurationSnapshotResponse ToConfigurationResponse(ConfigurationSnapshot value) =>
    new(
        new CompanyConfigurationResponse(
            value.Company.CompanyId,
            value.Company.Code,
            value.Company.LegalName,
            value.Company.TradeName,
            value.Company.TaxId,
            value.Company.Email,
            value.Company.StatusCode,
            Convert.ToBase64String(value.Company.RowVersion)
        ),
        value
            .Branches.Select(item => new BranchConfigurationResponse(
                item.BranchId,
                item.Code,
                item.Name,
                item.Address,
                item.City,
                item.Province,
                item.PostalCode,
                item.StatusCode,
                Convert.ToBase64String(item.RowVersion)
            ))
            .ToList(),
        value
            .OperatingUnits.Select(item => new OperatingUnitConfigurationResponse(
                item.OperatingUnitId,
                item.BranchId,
                item.Code,
                item.Name,
                item.Description,
                item.StatusCode,
                Convert.ToBase64String(item.RowVersion)
            ))
            .ToList(),
        value
            .PhoneTypes.Select(item => new PhoneTypeConfigurationResponse(
                item.PhoneTypeId,
                item.Code,
                item.Name,
                item.Description,
                item.StatusCode,
                Convert.ToBase64String(item.RowVersion)
            ))
            .ToList(),
        value
            .States.Select(item => new StateConfigurationResponse(
                item.StateId,
                item.Entity,
                item.StatusCode,
                item.Name,
                item.Description,
                item.IsInitial,
                item.IsFinal,
                item.Order,
                item.ValidUntilUtc,
                Convert.ToBase64String(item.RowVersion)
            ))
            .ToList(),
        value
            .Parameters.Select(item => new SystemParameterConfigurationResponse(
                item.ParameterId,
                item.ModuleId,
                item.ModuleCode,
                item.Key,
                item.Value,
                item.DataType,
                item.IsSecret,
                item.HasSecretReference,
                item.Description,
                item.StatusCode,
                Convert.ToBase64String(item.RowVersion)
            ))
            .ToList(),
        value
            .Modules.Select(item => new ModuleConfigurationResponse(
                item.ModuleId,
                item.ModuleNumber,
                item.Code,
                item.Name,
                item.Description,
                item.Order,
                item.StatusCode,
                Convert.ToBase64String(item.RowVersion)
            ))
            .ToList(),
        value
            .Errors.Select(item => new ErrorCatalogConfigurationResponse(
                item.ErrorId,
                item.ModuleId,
                item.ModuleCode,
                item.ErrorNumber,
                item.ErrorCode,
                item.Name,
                item.Description,
                item.ProbableCause,
                item.RecommendedAction,
                item.Severity,
                item.StatusCode,
                Convert.ToBase64String(item.RowVersion)
            ))
            .ToList(),
        value
            .Translations.Select(item => new CatalogTranslationConfigurationResponse(
                item.TranslationId,
                item.Entity,
                item.Code,
                item.CultureCode,
                item.Name,
                item.Description,
                item.StatusCode,
                Convert.ToBase64String(item.RowVersion)
            ))
            .ToList(),
        value.CultureCode
    );

static InventorySnapshotResponse ToInventoryResponse(InventorySnapshot value) =>
    new(
        value
            .MeasurementUnits.Select(item => new MeasurementUnitResponse(
                item.MeasurementUnitId,
                item.Code,
                item.Name,
                item.Symbol,
                item.AllowsDecimals,
                item.StatusCode,
                Convert.ToBase64String(item.RowVersion)
            ))
            .ToList(),
        value
            .Categories.Select(item => new ProductCategoryResponse(
                item.ProductCategoryId,
                item.Code,
                item.Name,
                item.Description,
                item.StatusCode,
                Convert.ToBase64String(item.RowVersion)
            ))
            .ToList(),
        value
            .Products.Select(item => new ProductResponse(
                item.ProductId,
                item.ProductCategoryId,
                item.MeasurementUnitId,
                item.Code,
                item.Name,
                item.Description,
                item.Barcode,
                item.Category,
                item.UnitSymbol,
                item.StatusCode,
                Convert.ToBase64String(item.RowVersion),
                item.ItemType,
                item.TrackingType,
                item.IsReusable,
                item.AllowsLoans,
                item.RequiresMaintenance,
                item.AllowsMeasurements
            ))
            .ToList(),
        value
            .Warehouses.Select(item => new WarehouseResponse(
                item.WarehouseId,
                item.BranchId,
                item.Code,
                item.Name,
                item.Address,
                item.Description,
                item.StatusCode,
                Convert.ToBase64String(item.RowVersion)
            ))
            .ToList(),
        value
            .Locations.Select(item => new WarehouseLocationResponse(
                item.LocationId,
                item.WarehouseId,
                item.Code,
                item.Name,
                item.Description,
                item.StatusCode,
                Convert.ToBase64String(item.RowVersion)
            ))
            .ToList(),
        value
            .Stock.Select(item => new StockBalanceResponse(
                item.StockBalanceId,
                item.ProductId,
                item.WarehouseId,
                item.ProductCode,
                item.Product,
                item.Warehouse,
                item.UnitSymbol,
                item.Quantity,
                item.ReservedQuantity,
                item.AvailableQuantity,
                item.MinimumStock,
                item.IsBelowMinimum,
                Convert.ToBase64String(item.RowVersion)
            ))
            .ToList(),
        value
            .Movements.Select(item => new InventoryMovementLineResponse(
                item.MovementId,
                item.MovementCode,
                item.MovementType,
                item.MovementDateUtc,
                item.Observation,
                item.StatusCode,
                item.MovementLineId,
                item.ProductId,
                item.ProductCode,
                item.Product,
                item.OriginWarehouseId,
                item.OriginWarehouse,
                item.OriginLocationId,
                item.OriginLocation,
                item.DestinationWarehouseId,
                item.DestinationWarehouse,
                item.DestinationLocationId,
                item.DestinationLocation,
                item.Quantity,
                item.UnitSymbol
            ))
            .ToList()
    );

static SalesSnapshotResponse ToSalesResponse(SalesSnapshot value) =>
    new(
        value
            .Clients.Select(item => new SalesClientResponse(item.ClientId, item.Code, item.Name))
            .ToList(),
        value
            .PriceLists.Select(item => new PriceListResponse(
                item.PriceListId,
                item.Code,
                item.Name,
                item.Currency,
                item.ValidFrom,
                item.ValidUntil,
                item.StatusCode,
                Convert.ToBase64String(item.RowVersion)
            ))
            .ToList(),
        value
            .Prices.Select(item => new PriceListProductResponse(
                item.PriceListProductId,
                item.PriceListId,
                item.ProductId,
                item.ProductCode,
                item.Product,
                item.UnitPrice,
                item.StatusCode,
                Convert.ToBase64String(item.RowVersion)
            ))
            .ToList(),
        value
            .Products.Select(item => new SalesProductResponse(
                item.ProductId,
                item.Code,
                item.Name,
                item.UnitSymbol,
                item.ItemType
            ))
            .ToList(),
        value
            .Warehouses.Select(item => new SalesWarehouseResponse(
                item.WarehouseId,
                item.Code,
                item.Name
            ))
            .ToList(),
        value
            .Promotions.Select(item => new PromotionResponse(
                item.PromotionId,
                item.ProductId,
                item.ProductCode,
                item.Product,
                item.Code,
                item.Name,
                item.Description,
                item.PromotionType,
                item.RequiredQuantity,
                item.PaidQuantity,
                item.DiscountedQuantity,
                item.DiscountRate,
                item.PackagePrice,
                item.ValidFrom,
                item.ValidUntil,
                item.StatusCode,
                Convert.ToBase64String(item.RowVersion)
            ))
            .ToList(),
        value
            .Orders.Select(item => new OrderLineResponse(
                item.OrderId,
                item.ClientId,
                item.PriceListId,
                item.OrderCode,
                item.OrderDateUtc,
                item.Client,
                item.Currency,
                item.Subtotal,
                item.TotalDiscount,
                item.TotalTax,
                item.Total,
                item.Observation,
                item.StatusCode,
                Convert.ToBase64String(item.RowVersion),
                item.OrderLineId,
                item.ProductId,
                item.ProductCode,
                item.Product,
                item.WarehouseId,
                item.Warehouse,
                item.Quantity,
                item.UnitPrice,
                item.DiscountRate,
                item.TaxRate,
                item.LineSubtotal,
                item.DiscountAmount,
                item.TaxAmount,
                item.LineTotal,
                item.UnitSymbol,
                item.PromotionId,
                item.PromotionCode,
                item.PromotionName
            ))
            .ToList(),
        value
            .Sales.Select(item => new SaleLineResponse(
                item.SaleId,
                item.OrderId,
                item.ClientId,
                item.MovementId,
                item.SaleCode,
                item.SaleDateUtc,
                item.Client,
                item.Currency,
                item.Subtotal,
                item.TotalDiscount,
                item.TotalTax,
                item.Total,
                item.Observation,
                item.StatusCode,
                item.SaleLineId,
                item.ProductId,
                item.ProductCode,
                item.Product,
                item.WarehouseId,
                item.Warehouse,
                item.Quantity,
                item.UnitPrice,
                item.DiscountRate,
                item.DiscountAmount,
                item.TaxRate,
                item.LineTotal,
                item.UnitSymbol,
                item.PromotionId,
                item.PromotionCode,
                item.PromotionName
            ))
            .ToList(),
        value
            .Assets.Select(item => new SalesAssetResponse(
                item.AssetId,
                item.ClientOwnerId,
                item.Code,
                item.SerialNumber,
                item.Product,
                item.Owner,
                item.ConditionCode,
                item.StatusCode
            ))
            .ToList(),
        value
            .OrderAssets.Select(item => new OrderAssetResponse(
                item.OrderAssetId,
                item.OrderId,
                item.AssetId,
                item.ProductLineId,
                item.AssetCode,
                item.SerialNumber,
                item.ProductLine,
                item.LinkType,
                item.ReturnMode,
                item.Observation,
                item.InboundMode,
                item.ExpectedReturnDate
            ))
            .ToList()
    );

static PurchasingSnapshotResponse ToPurchasingResponse(PurchasingSnapshot value) =>
    new(
        value
            .Suppliers.Select(item => new SupplierResponse(
                item.SupplierId,
                item.Code,
                item.LegalName,
                item.TradeName,
                item.TaxId,
                item.Email,
                item.Phone,
                item.PaymentTerms,
                item.Observation,
                item.StatusCode,
                Convert.ToBase64String(item.RowVersion)
            ))
            .ToList(),
        value
            .SupplierContacts.Select(item => new SupplierContactResponse(
                item.SupplierContactId,
                item.SupplierId,
                item.Name,
                item.Position,
                item.Email,
                item.Phone,
                item.IsPrimary,
                item.StatusCode,
                Convert.ToBase64String(item.RowVersion)
            ))
            .ToList(),
        value
            .Products.Select(item => new PurchasingProductResponse(
                item.ProductId,
                item.Code,
                item.Name,
                item.UnitSymbol,
                item.TrackingType,
                item.IsReusable,
                item.AllowsLoans,
                item.RequiresMaintenance,
                item.AllowsMeasurements
            ))
            .ToList(),
        value
            .Warehouses.Select(item => new PurchasingWarehouseResponse(
                item.WarehouseId,
                item.Code,
                item.Name
            ))
            .ToList(),
        value
            .Orders.Select(item => new PurchaseOrderLineResponse(
                item.PurchaseOrderId,
                item.SupplierId,
                item.WarehouseId,
                item.OrderCode,
                item.OrderDateUtc,
                item.ExpectedDeliveryDate,
                item.Supplier,
                item.SupplierTaxId,
                item.SupplierPaymentTerms,
                item.Warehouse,
                item.Currency,
                item.Subtotal,
                item.TotalDiscount,
                item.TotalTax,
                item.Total,
                item.Observation,
                item.StatusCode,
                Convert.ToBase64String(item.RowVersion),
                item.PurchaseOrderLineId,
                item.ProductId,
                item.ProductCode,
                item.Product,
                item.OrderedQuantity,
                item.ReceivedQuantity,
                item.PendingQuantity,
                item.UnitCost,
                item.DiscountRate,
                item.TaxRate,
                item.LineSubtotal,
                item.DiscountAmount,
                item.TaxAmount,
                item.LineTotal,
                item.UnitSymbol
            ))
            .ToList(),
        value
            .Receipts.Select(item => new GoodsReceiptLineResponse(
                item.GoodsReceiptId,
                item.PurchaseOrderId,
                item.MovementId,
                item.ReversalMovementId,
                item.ReceiptCode,
                item.ReceiptDateUtc,
                item.SupplierDocumentNumber,
                item.Observation,
                item.ReversalReason,
                item.ReversedAtUtc,
                item.ReversedByUserId,
                item.StatusCode,
                Convert.ToBase64String(item.RowVersion),
                item.GoodsReceiptLineId,
                item.PurchaseOrderLineId,
                item.ProductId,
                item.ProductCode,
                item.Product,
                item.AcceptedQuantity,
                item.RejectedQuantity,
                item.DamagedQuantity,
                item.DifferenceReason,
                item.UnitSymbol
            ))
            .ToList()
    );

static LogisticsSnapshotResponse ToLogisticsResponse(LogisticsSnapshot value) =>
    new(
        value
            .Addresses.Select(x => new LogisticsAddressResponse(
                x.AddressId,
                x.ClientId,
                x.Code,
                x.Name,
                x.Client,
                x.Address,
                x.City,
                x.Province,
                x.PostalCode,
                x.Latitude,
                x.Longitude,
                x.Contact,
                x.Phone,
                x.Email,
                x.FromTime,
                x.ToTime,
                x.ServiceMinutes,
                x.Restrictions,
                x.Instructions,
                x.StatusCode,
                Convert.ToBase64String(x.RowVersion)
            ))
            .ToList(),
        value
            .Requests.Select(x => new LogisticsRequestResponse(
                x.RequestId,
                x.ClientId,
                x.AddressId,
                x.OrderId,
                x.Code,
                x.Client,
                x.Address,
                x.ServiceType,
                x.Priority,
                x.RequestedDate,
                x.FromTime,
                x.ToTime,
                x.Instructions,
                x.Observation,
                x.StatusCode,
                Convert.ToBase64String(x.RowVersion),
                x.DestinationWarehouseId,
                x.DestinationWarehouse
            ))
            .ToList(),
        value
            .Assets.Select(x => new LogisticsAssetResponse(
                x.RequestAssetId,
                x.RequestId,
                x.AssetId,
                x.Owner,
                x.Role,
                x.SerialNumber,
                x.Product,
                x.ContentQuantity,
                x.Unit,
                x.Condition,
                x.Observation,
                x.StatusCode,
                x.ExpectedReturnDate,
                x.LoanId
            ))
            .ToList(),
        value.Routes.Select(ToRouteResponse).ToList(),
        value
            .Stops.Select(x => new RouteStopResponse(
                x.StopId,
                x.RouteId,
                x.RequestId,
                x.Order,
                x.Client,
                x.Address,
                x.Contact,
                x.Phone,
                x.FromTime,
                x.ToTime,
                x.Priority,
                x.Instructions,
                x.ArrivalUtc,
                x.DepartureUtc,
                x.Result,
                x.ResultObservation,
                x.StatusCode,
                Convert.ToBase64String(x.RowVersion)
            ))
            .ToList(),
        value
            .Events.Select(x => new LogisticsEventResponse(
                x.EventId,
                x.RequestId,
                x.RouteId,
                x.StopId,
                x.EventType,
                x.PreviousStatus,
                x.NewStatus,
                x.Observation,
                x.UserId,
                x.SessionId,
                x.Correlation,
                x.DateUtc
            ))
            .ToList(),
        value
            .Notifications.Select(x => new LogisticsNotificationResponse(
                x.NotificationId,
                x.RequestId,
                x.RouteId,
                x.EventType,
                x.Channel,
                x.Recipient,
                x.Message,
                x.Attempts,
                x.StatusCode,
                x.CreatedUtc,
                x.SentUtc
            ))
            .ToList(),
        value.Clients.Select(x => new LogisticsClientResponse(x.ClientId, x.Code, x.Name)).ToList(),
        value
            .Transporters.Select(x => new TransporterResponse(
                x.TransporterId,
                x.UserId,
                x.Code,
                x.UserName,
                x.FullName,
                x.RelationshipType,
                x.Document,
                x.Phone,
                x.License,
                x.LicenseCategory,
                x.LicenseExpiration,
                x.Observation,
                x.StatusCode,
                Convert.ToBase64String(x.RowVersion)
            ))
            .ToList(),
        value
            .Vehicles.Select(x => new LogisticsVehicleResponse(
                x.VehicleId,
                x.OwnerTransporterId,
                x.Code,
                x.Plate,
                x.VehicleType,
                x.OwnershipType,
                x.Owner,
                x.Brand,
                x.Model,
                x.Year,
                x.LoadCapacityKg,
                x.InsurancePolicy,
                x.InsuranceExpiration,
                x.InspectionExpiration,
                x.Observation,
                x.StatusCode,
                Convert.ToBase64String(x.RowVersion)
            ))
            .ToList(),
        value
            .TransporterUsers.Select(x => new TransporterUserResponse(
                x.UserId,
                x.UserName,
                x.FullName
            ))
            .ToList(),
        value
            .OrderCandidates.Select(x => new LogisticsOrderCandidateResponse(
                x.OrderId,
                x.ClientId,
                x.OrderCode,
                x.Client,
                x.Operation,
                x.ServiceType,
                x.OrderDate,
                x.Total,
                x.Outstanding,
                x.PaymentStatus,
                x.AssetCount,
                x.ExpectedReturnDate,
                x.Summary
            ))
            .ToList(),
        value
            .Warehouses.Select(x => new LogisticsWarehouseResponse(x.WarehouseId, x.Code, x.Name))
            .ToList()
    );

static DriverSnapshotResponse ToDriverResponse(DriverSnapshot value) =>
    new(
        new DriverProfileResponse(
            value.Driver.TransporterId,
            value.Driver.Code,
            value.Driver.FullName,
            value.Driver.RelationshipType,
            value.Driver.Document,
            value.Driver.Phone,
            value.Driver.License,
            value.Driver.LicenseCategory,
            value.Driver.LicenseExpiration,
            value.Driver.Observation
        ),
        value.Routes.Select(ToRouteResponse).ToList(),
        value.Offers.Select(ToRouteResponse).ToList(),
        value.History.Select(ToRouteResponse).ToList(),
        value
            .Stops.Select(x => new RouteStopResponse(
                x.StopId,
                x.RouteId,
                x.RequestId,
                x.Order,
                x.Client,
                x.Address,
                x.Contact,
                x.Phone,
                x.FromTime,
                x.ToTime,
                x.Priority,
                x.Instructions,
                x.ArrivalUtc,
                x.DepartureUtc,
                x.Result,
                x.ResultObservation,
                x.StatusCode,
                Convert.ToBase64String(x.RowVersion)
            ))
            .ToList(),
        value
            .Assets.Select(x => new LogisticsAssetResponse(
                x.RequestAssetId,
                x.RequestId,
                x.AssetId,
                x.Owner,
                x.Role,
                x.SerialNumber,
                x.Product,
                x.ContentQuantity,
                x.Unit,
                x.Condition,
                x.Observation,
                x.StatusCode,
                x.ExpectedReturnDate,
                x.LoanId
            ))
            .ToList(),
        value
            .Vehicles.Select(x => new LogisticsVehicleResponse(
                x.VehicleId,
                x.OwnerTransporterId,
                x.Code,
                x.Plate,
                x.VehicleType,
                x.OwnershipType,
                x.Owner,
                x.Brand,
                x.Model,
                x.Year,
                x.LoadCapacityKg,
                x.InsurancePolicy,
                x.InsuranceExpiration,
                x.InspectionExpiration,
                x.Observation,
                x.StatusCode,
                Convert.ToBase64String(x.RowVersion)
            ))
            .ToList()
    );

static RouteSheetResponse ToRouteResponse(RouteSheet value) =>
    new(
        value.RouteId,
        value.TransporterId,
        value.VehicleId,
        value.Code,
        value.RouteDate,
        value.RouteType,
        value.AssignmentType,
        value.Driver,
        value.Plate,
        value.Observation,
        value.AssignedUtc,
        value.DepartureUtc,
        value.ClosedUtc,
        value.StatusCode,
        Convert.ToBase64String(value.RowVersion)
    );

static FinanceSnapshotResponse ToFinanceResponse(FinanceSnapshot value) =>
    new(
        value.Branches.Select(x => new FinanceBranchResponse(x.BranchId, x.Code, x.Name)).ToList(),
        value
            .Clients.Select(x => new FinanceClientResponse(x.ClientId, x.Code, x.Name, x.Balance))
            .ToList(),
        value
            .PaymentMethods.Select(x => new PaymentMethodResponse(
                x.PaymentMethodId,
                x.Code,
                x.Name,
                x.Type,
                x.AffectsCash,
                x.RequiresReference,
                x.StatusCode,
                Convert.ToBase64String(x.RowVersion)
            ))
            .ToList(),
        value
            .CashBoxes.Select(x => new CashBoxResponse(
                x.CashBoxId,
                x.BranchId,
                x.Code,
                x.Name,
                x.Branch,
                x.Currency,
                x.StatusCode,
                Convert.ToBase64String(x.RowVersion)
            ))
            .ToList(),
        value
            .CashSessions.Select(x => new CashSessionResponse(
                x.CashSessionId,
                x.CashBoxId,
                x.CashBox,
                x.OpeningUserId,
                x.User,
                x.OpenedUtc,
                x.OpeningAmount,
                x.ClosedUtc,
                x.ExpectedAmount,
                x.CountedAmount,
                x.Difference,
                x.OpeningObservation,
                x.ClosingObservation,
                x.StatusCode,
                Convert.ToBase64String(x.RowVersion)
            ))
            .ToList(),
        value
            .Sales.Select(x => new ReceivableSaleResponse(
                x.SaleId,
                x.ClientId,
                x.Code,
                x.SaleDateUtc,
                x.Client,
                x.Currency,
                x.Total,
                x.Outstanding,
                x.StatusCode
            ))
            .ToList(),
        value
            .Payments.Select(x => new CustomerPaymentResponse(
                x.PaymentId,
                x.ClientId,
                x.CashSessionId,
                x.Code,
                x.PaymentDateUtc,
                x.Client,
                x.Currency,
                x.Total,
                x.Observation,
                x.ReversalReason,
                x.ReversedUtc,
                x.StatusCode,
                Convert.ToBase64String(x.RowVersion)
            ))
            .ToList(),
        value
            .PaymentLines.Select(x => new CustomerPaymentMethodResponse(
                x.PaymentLineId,
                x.PaymentId,
                x.PaymentMethodId,
                x.PaymentMethod,
                x.Amount,
                x.Reference,
                x.StatusCode
            ))
            .ToList(),
        value
            .Applications.Select(x => new PaymentApplicationResponse(
                x.ApplicationId,
                x.PaymentId,
                x.SaleId,
                x.Sale,
                x.Amount,
                x.StatusCode
            ))
            .ToList(),
        value
            .AccountMovements.Select(x => new AccountMovementResponse(
                x.AccountMovementId,
                x.ClientId,
                x.SaleId,
                x.PaymentId,
                x.MovementType,
                x.Source,
                x.MovementDateUtc,
                x.Currency,
                x.Amount,
                x.Description,
                x.Correlation,
                x.StatusCode
            ))
            .ToList()
    );

static TraceabilitySnapshotResponse ToTraceabilityResponse(TraceabilitySnapshot value) =>
    new(
        value
            .Lots.Select(item => new TraceLotResponse(
                item.LotId,
                item.ProductId,
                item.Code,
                item.SupplierLotCode,
                item.Product,
                item.WarehouseId,
                item.Warehouse,
                item.Quantity,
                item.ExpirationDate,
                item.StatusCode
            ))
            .ToList(),
        value
            .Assets.Select(item => new TraceAssetResponse(
                item.AssetId,
                item.ProductId,
                item.OwnerClientId,
                item.Code,
                item.SerialNumber,
                item.Product,
                item.AssetType,
                item.Owner,
                item.ConditionCode,
                item.WarehouseId,
                item.Warehouse,
                item.LocationId,
                item.Location,
                item.StatusCode,
                item.Capacity,
                item.CapacityUnit,
                item.ContentProductId,
                item.ContentProduct,
                item.ContentLotId,
                item.ContentLot,
                item.ContentQuantity,
                item.ContentUnit,
                Convert.ToBase64String(item.RowVersion)
            ))
            .ToList(),
        value
            .Measurements.Select(item => new AssetMeasurementResponse(
                item.MeasurementId,
                item.AssetId,
                item.Asset,
                item.MeasurementDateUtc,
                item.MeasurementType,
                item.Value,
                item.Unit,
                item.Method,
                item.Source,
                item.IsEstimated,
                item.DeviceReference,
                item.Observation
            ))
            .ToList(),
        value
            .Transformations.Select(item => new TransformationLineResponse(
                item.TransformationId,
                item.Code,
                item.TransformationDateUtc,
                item.SourceLotId,
                item.SourceLot,
                item.SourceAssetId,
                item.SourceAsset,
                item.Product,
                item.SourceQuantity,
                item.LossQuantity,
                item.Method,
                item.LossReason,
                item.DestinationAssetId,
                item.DestinationAsset,
                item.LoadedQuantity
            ))
            .ToList(),
        value
            .Incidents.Select(item => new IndustrialIncidentResponse(
                item.IncidentId,
                item.Code,
                item.IncidentDateUtc,
                item.IncidentType,
                item.Product,
                item.Warehouse,
                item.AssetId,
                item.Asset,
                item.LossQuantity,
                item.IsEstimated,
                item.Cause,
                item.ActionTaken,
                item.StatusCode
            ))
            .ToList(),
        value
            .Maintenances.Select(item => new AssetMaintenanceResponse(
                item.MaintenanceId,
                item.Code,
                item.AssetId,
                item.Asset,
                item.MaintenanceType,
                item.StartDateUtc,
                item.EndDateUtc,
                item.WorkDescription,
                item.Result,
                item.Cost,
                item.NextReviewDate,
                item.StatusCode,
                Convert.ToBase64String(item.RowVersion)
            ))
            .ToList(),
        value
            .Loans.Select(item => new AssetLoanLineResponse(
                item.LoanId,
                item.Code,
                item.Destination,
                item.DeliveryMode,
                item.DepartureDateUtc,
                item.ExpectedReturnDateUtc,
                item.ActualReturnDateUtc,
                item.StatusCode,
                Convert.ToBase64String(item.RowVersion),
                item.LoanLineId,
                item.AssetId,
                item.Asset,
                item.DepartureQuantity,
                item.DepartureCondition,
                item.ReturnQuantity,
                item.ReturnCondition
            ))
            .ToList(),
        value
            .Events.Select(item => new AssetEventResponse(
                item.EventId,
                item.AssetId,
                item.Asset,
                item.EventType,
                item.EventDateUtc,
                item.StatusBefore,
                item.StatusAfter,
                item.ConditionBefore,
                item.ConditionAfter,
                item.QuantityBefore,
                item.QuantityAfter,
                item.Observation,
                item.CorrelationId,
                item.UserId
            ))
            .ToList(),
        value
            .Branches.Select(item => new TraceDestinationResponse(item.Id, item.Code, item.Name))
            .ToList(),
        value
            .Clients.Select(item => new TraceDestinationResponse(item.Id, item.Code, item.Name))
            .ToList()
    );

/// <summary>Expone el punto de entrada de la API para ejecución y pruebas de integración.</summary>
public partial class Program;
