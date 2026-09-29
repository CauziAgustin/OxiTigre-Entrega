/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.AdminWeb.Program
Archivo: Program.cs | Versión: 1.0.0 | Fecha: 2026-08-21 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Inicia el panel Blazor, autentica contra la API y limita el acceso a administradores.
Historial: 1.0.0 | 2026-08-21 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using OxiTigre.AdminWeb;
using OxiTigre.AdminWeb.Components;
using OxiTigre.ApiClient;
using OxiTigre.Contracts.Security;

var builder = WebApplication.CreateBuilder(args);
var apiUrl = Environment.GetEnvironmentVariable("OXITIGRE_API_URL")
    ?? builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5000/";

builder.Services.AddRazorComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<OxiTigreApiClient>(client => client.BaseAddress = new Uri(apiUrl, UriKind.Absolute));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.Cookie.Name = "OxiTigre.Admin";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/login?error=forbidden";
    options.SlidingExpiration = false;
});
builder.Services.AddAuthorization();

var app = builder.Build();
if (!app.Environment.IsDevelopment()) app.UseExceptionHandler("/error");
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapPost("/auth/login", LoginAsync).AllowAnonymous();
app.MapPost("/auth/logout", LogoutAsync).RequireAuthorization();
app.MapRazorComponents<App>();
app.Run();

static async Task<IResult> LoginAsync(HttpContext context, IAntiforgery antiforgery, OxiTigreApiClient apiClient)
{
    await antiforgery.ValidateRequestAsync(context);
    var form = await context.Request.ReadFormAsync(context.RequestAborted);
    var username = form["username"].ToString().Trim();
    var password = form["password"].ToString();
    var companyCode = form["company"].ToString().Trim().ToUpperInvariant();
    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(companyCode))
        return Results.Redirect("/login?error=required");

    try
    {
        var companies = await apiClient.GetCompaniesAsync(new CredentialsRequest(username, password), context.RequestAborted);
        var company = companies?.FirstOrDefault(item => string.Equals(item.Code, companyCode, StringComparison.OrdinalIgnoreCase));
        if (company is null) return Results.Redirect("/login?error=invalid");

        var session = await apiClient.LoginAsync(new LoginRequest(company.Code, username, password), context.RequestAborted);
        if (session is null) return Results.Redirect("/login?error=invalid");
        if (session.MustChangePassword)
        {
            await apiClient.LogoutAsync(session.Token, context.RequestAborted);
            return Results.Redirect("/login?error=password");
        }
        if (!AdminAccess.IsAdministrator(session.Roles))
        {
            await apiClient.LogoutAsync(session.Token, context.RequestAborted);
            return Results.Redirect("/login?error=forbidden");
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, session.UserId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new Claim(ClaimTypes.Name, session.DisplayName),
            new Claim(ClaimTypes.Role, "ADMINISTRADOR"),
            new Claim("oxitigre:token", session.Token),
            new Claim("oxitigre:company", company.Name)
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = false, ExpiresUtc = session.ExpiresAtUtc });
        return Results.Redirect("/");
    }
    catch (HttpRequestException)
    {
        return Results.Redirect("/login?error=api");
    }
}

static async Task<IResult> LogoutAsync(HttpContext context, IAntiforgery antiforgery, OxiTigreApiClient apiClient)
{
    await antiforgery.ValidateRequestAsync(context);
    var token = context.User.FindFirstValue("oxitigre:token");
    if (!string.IsNullOrWhiteSpace(token))
    {
        try { await apiClient.LogoutAsync(token, context.RequestAborted); }
        catch (HttpRequestException) { /* La cookie local debe cerrarse aunque la API no esté disponible. */ }
    }
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
}

/// <summary>Expone el punto de entrada del panel para comprobaciones de integración.</summary>
public partial class Program;
