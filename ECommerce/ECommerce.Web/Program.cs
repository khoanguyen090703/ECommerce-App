using ECommerce.Web.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromHours(1);
});
builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuthSessionManager, AuthSessionManager>();
builder.Services.AddTransient<ApiAuthenticationHandler>();
builder.Services.AddHttpClient("ApiAnonymous", client =>
{
    var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5206";
    client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
});
builder.Services.AddHttpClient(Options.DefaultName, client =>
{
    var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5206";
    client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
})
    .AddHttpMessageHandler<ApiAuthenticationHandler>();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration.GetSection("JwtSettings");
        var key = jwtSettings["Key"];

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key ?? string.Empty)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        // API token is stored in HttpOnly cookie after sign-in.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                context.Token = context.Request.Cookies["access_token"];
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = JwtCookieRefreshEvents.HandleAuthenticationFailedAsync
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<ForceSignOutMiddleware>();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

// BFF: browser calls same-origin /api/cart* with cookies; forwards to API with Bearer (HttpClient + ApiAuthenticationHandler).
app.MapGet("/api/cart/item-count", async (IHttpClientFactory httpClientFactory, CancellationToken cancellationToken) =>
{
    var client = httpClientFactory.CreateClient();
    using var response = await client.GetAsync("api/cart/item-count", cancellationToken);
    var payload = await response.Content.ReadAsStringAsync(cancellationToken);
    return Results.Content(payload, "application/json", statusCode: (int)response.StatusCode);
}).RequireAuthorization();

app.MapGet("/api/cart", async (IHttpClientFactory httpClientFactory, CancellationToken cancellationToken) =>
{
    var client = httpClientFactory.CreateClient();
    using var response = await client.GetAsync("api/cart", cancellationToken);
    var payload = await response.Content.ReadAsStringAsync(cancellationToken);
    return Results.Content(payload, "application/json", statusCode: (int)response.StatusCode);
}).RequireAuthorization();

app.MapPost("/api/cart/items", async (HttpRequest request, IHttpClientFactory httpClientFactory, CancellationToken cancellationToken) =>
{
    var body = await new StreamReader(request.Body).ReadToEndAsync(cancellationToken);
    var client = httpClientFactory.CreateClient();
    using var req = new HttpRequestMessage(HttpMethod.Post, "api/cart/items")
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
    };
    using var response = await client.SendAsync(req, cancellationToken);
    var payload = await response.Content.ReadAsStringAsync(cancellationToken);
    if (string.IsNullOrWhiteSpace(payload))
    {
        return Results.StatusCode((int)response.StatusCode);
    }

    return Results.Content(payload, "application/json", statusCode: (int)response.StatusCode);
}).RequireAuthorization();

app.MapPatch("/api/cart/items/{itemId:int}", async (int itemId, HttpRequest request, IHttpClientFactory httpClientFactory, CancellationToken cancellationToken) =>
{
    var body = await new StreamReader(request.Body).ReadToEndAsync(cancellationToken);
    var client = httpClientFactory.CreateClient();
    using var req = new HttpRequestMessage(HttpMethod.Patch, $"api/cart/items/{itemId}")
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
    };
    using var response = await client.SendAsync(req, cancellationToken);
    var payload = await response.Content.ReadAsStringAsync(cancellationToken);
    if (string.IsNullOrWhiteSpace(payload))
    {
        return Results.StatusCode((int)response.StatusCode);
    }

    return Results.Content(payload, "application/json", statusCode: (int)response.StatusCode);
}).RequireAuthorization();

app.MapDelete("/api/cart/items/{itemId:int}", async (int itemId, IHttpClientFactory httpClientFactory, CancellationToken cancellationToken) =>
{
    var client = httpClientFactory.CreateClient();
    using var response = await client.DeleteAsync($"api/cart/items/{itemId}", cancellationToken);
    var payload = await response.Content.ReadAsStringAsync(cancellationToken);
    if (string.IsNullOrWhiteSpace(payload))
    {
        return Results.StatusCode((int)response.StatusCode);
    }

    return Results.Content(payload, "application/json", statusCode: (int)response.StatusCode);
}).RequireAuthorization();

app.MapDelete("/api/cart/items", async (IHttpClientFactory httpClientFactory, CancellationToken cancellationToken) =>
{
    var client = httpClientFactory.CreateClient();
    using var response = await client.DeleteAsync("api/cart/items", cancellationToken);
    var payload = await response.Content.ReadAsStringAsync(cancellationToken);
    if (string.IsNullOrWhiteSpace(payload))
    {
        return Results.StatusCode((int)response.StatusCode);
    }

    return Results.Content(payload, "application/json", statusCode: (int)response.StatusCode);
}).RequireAuthorization();

// BFF: checkout & order creation (same-origin /api/checkout, /api/orders)
app.MapGet("/api/checkout", async (IHttpClientFactory httpClientFactory, CancellationToken cancellationToken) =>
{
    var client = httpClientFactory.CreateClient();
    using var response = await client.GetAsync("api/checkout", cancellationToken);
    var payload = await response.Content.ReadAsStringAsync(cancellationToken);
    return Results.Content(payload, "application/json", statusCode: (int)response.StatusCode);
}).RequireAuthorization();

app.MapPost("/api/orders", async (HttpRequest request, IHttpClientFactory httpClientFactory, CancellationToken cancellationToken) =>
{
    var body = await new StreamReader(request.Body).ReadToEndAsync(cancellationToken);
    var client = httpClientFactory.CreateClient();
    using var req = new HttpRequestMessage(HttpMethod.Post, "api/orders")
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
    };
    using var response = await client.SendAsync(req, cancellationToken);
    var payload = await response.Content.ReadAsStringAsync(cancellationToken);
    if (string.IsNullOrWhiteSpace(payload))
    {
        return Results.StatusCode((int)response.StatusCode);
    }

    return Results.Content(payload, "application/json", statusCode: (int)response.StatusCode);
}).RequireAuthorization();

app.MapGet("/api/orders/me", async (HttpRequest request, IHttpClientFactory httpClientFactory, CancellationToken cancellationToken) =>
{
    var query = request.QueryString.HasValue ? request.QueryString.Value : string.Empty;
    var client = httpClientFactory.CreateClient();
    using var response = await client.GetAsync($"api/orders/me{query}", cancellationToken);
    var payload = await response.Content.ReadAsStringAsync(cancellationToken);
    return Results.Content(payload, "application/json", statusCode: (int)response.StatusCode);
}).RequireAuthorization();

app.MapGet("/api/orders/{id:int:min(1)}", async (int id, IHttpClientFactory httpClientFactory, CancellationToken cancellationToken) =>
{
    var client = httpClientFactory.CreateClient();
    using var response = await client.GetAsync($"api/orders/{id}", cancellationToken);
    var payload = await response.Content.ReadAsStringAsync(cancellationToken);
    return Results.Content(payload, "application/json", statusCode: (int)response.StatusCode);
}).RequireAuthorization();

app.UseStaticFiles();

app.Run();
