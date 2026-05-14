using ECommerce.Web.Auth;
using ECommerce.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5206";

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.IdleTimeout = TimeSpan.FromHours(1);
});
builder.Services.AddRazorPages();
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
    options.HttpOnly = HttpOnlyPolicy.Always;
    options.Secure = CookieSecurePolicy.SameAsRequest;
});
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Strict;
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuthCookieService, AuthCookieService>();
builder.Services.AddSingleton<VietnamAddressClient>();
builder.Services.AddTransient<TokenRefreshHandler>();
builder.Services.AddHttpClient(AuthConstants.ApiAnonymousClientName, client =>
{
    client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(15);
});
builder.Services.AddHttpClient(AuthConstants.ApiClientName, client =>
{
    client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(15);
})
    .AddHttpMessageHandler<TokenRefreshHandler>();
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.Path = "/";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = AuthConstants.AuthenticationCookieLifetime;
        options.LoginPath = "/Auth/SignIn";
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            context.Response.Redirect(AuthReturnUrl.BuildSignInUrl(context.Request));
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseRouting();

app.UseCookiePolicy();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.MapPost("/api/auth/refresh-session", async (
    HttpContext httpContext,
    IAuthService authService,
    IAuthCookieService authCookieService,
    CancellationToken cancellationToken) =>
{
    var refreshToken = httpContext.Request.Cookies[AuthConstants.RefreshTokenCookieName];
    var accessToken = httpContext.User.FindFirstValue(AuthConstants.AccessTokenClaimType);
    if (string.IsNullOrWhiteSpace(refreshToken) || string.IsNullOrWhiteSpace(accessToken))
        return Results.Unauthorized();

    var refreshed = await authService.RefreshAsync(accessToken, refreshToken, cancellationToken);
    if (refreshed is null)
    {
        await authCookieService.SignOutAsync(httpContext, cancellationToken);
        return Results.Unauthorized();
    }

    var userName = httpContext.User.Identity?.Name ?? string.Empty;
    await authCookieService.SignInAsync(httpContext, refreshed, userName, cancellationToken);
    return Results.Ok();
}).RequireAuthorization();

app.MapGet("/api/cart/item-count", async (IHttpClientFactory httpClientFactory, CancellationToken cancellationToken) =>
{
    var client = httpClientFactory.CreateClient(AuthConstants.ApiClientName);
    using var response = await client.GetAsync("api/cart/item-count", cancellationToken);
    var payload = await response.Content.ReadAsStringAsync(cancellationToken);
    return Results.Content(payload, "application/json", statusCode: (int)response.StatusCode);
}).RequireAuthorization();

app.MapGet("/api/cart", async (IHttpClientFactory httpClientFactory, CancellationToken cancellationToken) =>
{
    var client = httpClientFactory.CreateClient(AuthConstants.ApiClientName);
    using var response = await client.GetAsync("api/cart", cancellationToken);
    var payload = await response.Content.ReadAsStringAsync(cancellationToken);
    return Results.Content(payload, "application/json", statusCode: (int)response.StatusCode);
}).RequireAuthorization();

app.MapPost("/api/cart/items", async (HttpRequest request, IHttpClientFactory httpClientFactory, CancellationToken cancellationToken) =>
{
    var body = await new StreamReader(request.Body).ReadToEndAsync(cancellationToken);
    var client = httpClientFactory.CreateClient(AuthConstants.ApiClientName);
    using var req = new HttpRequestMessage(HttpMethod.Post, "api/cart/items")
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
    };
    using var response = await client.SendAsync(req, cancellationToken);
    var payload = await response.Content.ReadAsStringAsync(cancellationToken);
    if (string.IsNullOrWhiteSpace(payload))
        return Results.StatusCode((int)response.StatusCode);

    return Results.Content(payload, "application/json", statusCode: (int)response.StatusCode);
}).RequireAuthorization();

app.MapPatch("/api/cart/items/{itemId:int}", async (int itemId, HttpRequest request, IHttpClientFactory httpClientFactory, CancellationToken cancellationToken) =>
{
    var body = await new StreamReader(request.Body).ReadToEndAsync(cancellationToken);
    var client = httpClientFactory.CreateClient(AuthConstants.ApiClientName);
    using var req = new HttpRequestMessage(HttpMethod.Patch, $"api/cart/items/{itemId}")
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
    };
    using var response = await client.SendAsync(req, cancellationToken);
    var payload = await response.Content.ReadAsStringAsync(cancellationToken);
    if (string.IsNullOrWhiteSpace(payload))
        return Results.StatusCode((int)response.StatusCode);

    return Results.Content(payload, "application/json", statusCode: (int)response.StatusCode);
}).RequireAuthorization();

app.MapDelete("/api/cart/items/{itemId:int}", async (int itemId, IHttpClientFactory httpClientFactory, CancellationToken cancellationToken) =>
{
    var client = httpClientFactory.CreateClient(AuthConstants.ApiClientName);
    using var response = await client.DeleteAsync($"api/cart/items/{itemId}", cancellationToken);
    var payload = await response.Content.ReadAsStringAsync(cancellationToken);
    if (string.IsNullOrWhiteSpace(payload))
        return Results.StatusCode((int)response.StatusCode);

    return Results.Content(payload, "application/json", statusCode: (int)response.StatusCode);
}).RequireAuthorization();

app.MapDelete("/api/cart/items", async (IHttpClientFactory httpClientFactory, CancellationToken cancellationToken) =>
{
    var client = httpClientFactory.CreateClient(AuthConstants.ApiClientName);
    using var response = await client.DeleteAsync("api/cart/items", cancellationToken);
    var payload = await response.Content.ReadAsStringAsync(cancellationToken);
    if (string.IsNullOrWhiteSpace(payload))
        return Results.StatusCode((int)response.StatusCode);

    return Results.Content(payload, "application/json", statusCode: (int)response.StatusCode);
}).RequireAuthorization();

app.MapGet("/api/checkout", async (IHttpClientFactory httpClientFactory, CancellationToken cancellationToken) =>
{
    var client = httpClientFactory.CreateClient(AuthConstants.ApiClientName);
    using var response = await client.GetAsync("api/checkout", cancellationToken);
    var payload = await response.Content.ReadAsStringAsync(cancellationToken);
    return Results.Content(payload, "application/json", statusCode: (int)response.StatusCode);
}).RequireAuthorization();

app.MapPost("/api/orders", async (HttpRequest request, IHttpClientFactory httpClientFactory, CancellationToken cancellationToken) =>
{
    var body = await new StreamReader(request.Body).ReadToEndAsync(cancellationToken);
    var client = httpClientFactory.CreateClient(AuthConstants.ApiClientName);
    using var req = new HttpRequestMessage(HttpMethod.Post, "api/orders")
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
    };
    using var response = await client.SendAsync(req, cancellationToken);
    var payload = await response.Content.ReadAsStringAsync(cancellationToken);
    if (string.IsNullOrWhiteSpace(payload))
        return Results.StatusCode((int)response.StatusCode);

    return Results.Content(payload, "application/json", statusCode: (int)response.StatusCode);
}).RequireAuthorization();

app.MapGet("/api/orders/me", async (HttpRequest request, IHttpClientFactory httpClientFactory, CancellationToken cancellationToken) =>
{
    var query = request.QueryString.HasValue ? request.QueryString.Value : string.Empty;
    var client = httpClientFactory.CreateClient(AuthConstants.ApiClientName);
    using var response = await client.GetAsync($"api/orders/me{query}", cancellationToken);
    var payload = await response.Content.ReadAsStringAsync(cancellationToken);
    return Results.Content(payload, "application/json", statusCode: (int)response.StatusCode);
}).RequireAuthorization();

app.MapGet("/api/orders/{id:int:min(1)}", async (int id, IHttpClientFactory httpClientFactory, CancellationToken cancellationToken) =>
{
    var client = httpClientFactory.CreateClient(AuthConstants.ApiClientName);
    using var response = await client.GetAsync($"api/orders/{id}", cancellationToken);
    var payload = await response.Content.ReadAsStringAsync(cancellationToken);
    return Results.Content(payload, "application/json", statusCode: (int)response.StatusCode);
}).RequireAuthorization();

app.MapPost("/api/orders/{id:int:min(1)}/cancel", async (int id, IHttpClientFactory httpClientFactory, CancellationToken cancellationToken) =>
{
    var client = httpClientFactory.CreateClient(AuthConstants.ApiClientName);
    using var response = await client.PostAsync($"api/orders/{id}/cancel", null, cancellationToken);
    var payload = await response.Content.ReadAsStringAsync(cancellationToken);
    if (string.IsNullOrWhiteSpace(payload))
        return Results.StatusCode((int)response.StatusCode);

    return Results.Content(payload, "application/json", statusCode: (int)response.StatusCode);
}).RequireAuthorization();

app.MapPost("/api/payments/stripe/checkout", async (HttpRequest request, IHttpClientFactory httpClientFactory, CancellationToken cancellationToken) =>
{
    var body = await new StreamReader(request.Body).ReadToEndAsync(cancellationToken);
    var client = httpClientFactory.CreateClient(AuthConstants.ApiClientName);
    using var req = new HttpRequestMessage(HttpMethod.Post, "api/payments/stripe/checkout")
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
    };
    using var response = await client.SendAsync(req, cancellationToken);
    var payload = await response.Content.ReadAsStringAsync(cancellationToken);
    if (string.IsNullOrWhiteSpace(payload))
        return Results.StatusCode((int)response.StatusCode);

    return Results.Content(payload, "application/json", statusCode: (int)response.StatusCode);
}).RequireAuthorization();

app.MapGet("/api/payments/stripe/orders/{orderId:int:min(1)}/status", async (int orderId, IHttpClientFactory httpClientFactory, CancellationToken cancellationToken) =>
{
    var client = httpClientFactory.CreateClient(AuthConstants.ApiClientName);
    using var response = await client.GetAsync($"api/payments/stripe/orders/{orderId}/status", cancellationToken);
    var payload = await response.Content.ReadAsStringAsync(cancellationToken);
    return Results.Content(payload, "application/json", statusCode: (int)response.StatusCode);
}).RequireAuthorization();

app.MapPost("/api/payments/stripe/orders/{orderId:int:min(1)}/retry", async (int orderId, IHttpClientFactory httpClientFactory, CancellationToken cancellationToken) =>
{
    var client = httpClientFactory.CreateClient(AuthConstants.ApiClientName);
    using var req = new HttpRequestMessage(HttpMethod.Post, $"api/payments/stripe/orders/{orderId}/retry")
    {
        Content = new StringContent("{}", Encoding.UTF8, "application/json")
    };
    using var response = await client.SendAsync(req, cancellationToken);
    var payload = await response.Content.ReadAsStringAsync(cancellationToken);
    if (string.IsNullOrWhiteSpace(payload))
        return Results.StatusCode((int)response.StatusCode);

    return Results.Content(payload, "application/json", statusCode: (int)response.StatusCode);
}).RequireAuthorization();

app.UseStaticFiles();

app.Run();
