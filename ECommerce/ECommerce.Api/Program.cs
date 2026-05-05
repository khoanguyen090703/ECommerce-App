using ECommerce.Api.ErrorHandlers;
using ECommerce.Api.Filters;
using ECommerce.Application;
using ECommerce.Application.Validators;
using ECommerce.Infrastructure;
using ECommerce.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi;
using System.Text.Json.Serialization;
using InfrastructureDependencyInjection = ECommerce.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Allow enum values to be provided as strings (case-insensitive)
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: true));
    });
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

// JWT Bearer for Swagger UI (Swashbuckle 10 / Microsoft.OpenApi 2.x, OpenAPI 3.1)
const string swaggerJwtSchemeId = "Bearer";

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(swaggerJwtSchemeId, new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT Bearer. Paste the raw token only (Swagger sends it as Authorization: Bearer <token>)."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference(swaggerJwtSchemeId, document)] = []
    });

    options.OperationFilter<SecurityRequirementsOperationFilter>();
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddValidatorsFromAssemblyContaining<CreateProductRequestValidator>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("email-protection", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(10);
        opt.PermitLimit = 3;
        opt.QueueLimit = 0;
        opt.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
    });
});

// Allow large file uploads (default is 30 MB; adjust as needed)
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10 MB
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    await app.Services.InitialiseDatabaseAsync();
    app.UseSwagger(options =>
    {
        options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1;
    });
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ECommerce API v1");
        options.EnablePersistAuthorization();
        options.EnableValidator(null);
    });
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseRateLimiter();

app.UseExceptionHandler();

app.UseRouting();
app.UseCors(InfrastructureDependencyInjection.CorsPolicyName);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
