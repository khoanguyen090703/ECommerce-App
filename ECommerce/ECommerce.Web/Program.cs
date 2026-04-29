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
app.UseStaticFiles();

app.Run();
