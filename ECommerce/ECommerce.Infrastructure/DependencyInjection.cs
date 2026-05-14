using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Services;
using ECommerce.Domain.Interfaces;
using ECommerce.Infrastructure.Identity;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Infrastructure.Persistence.Intercepters;
using ECommerce.Infrastructure.Persistence.Repositories;
using ECommerce.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace ECommerce.Infrastructure
{
    public static class DependencyInjection
    {
        public const string CorsPolicyName = "DefaultCorsPolicy";

        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // 1. Đăng ký Interceptor như một Singleton hoặc Scoped
            services.AddSingleton<UpdateAuditableInterceptor>();

            services.AddDbContext<AppDbContext>((sp, options) =>
                    {
                        // Lấy Interceptor từ Service Provider
                        var auditableInterceptor = sp.GetRequiredService<UpdateAuditableInterceptor>();

                        options.UseSqlServer(configuration.GetConnectionString("ECommerceDB"))
                                .AddInterceptors(auditableInterceptor);
                    }
                );

            // 2. Cấu hình Identity
            services.AddIdentity<AppUser, IdentityRole<Guid>>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            services.Configure<DataProtectionTokenProviderOptions>(options =>
            {
                options.TokenLifespan = TimeSpan.FromHours(1); // Default 24 hours
            });

            // 3. Cấu hình JWT
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = configuration["JwtSettings:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = configuration["JwtSettings:Audience"],
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JwtSettings:Key"]!)),
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = JwtRegisteredClaimNames.Email,
                    RoleClaimType = ClaimTypes.Role
                };
            })
            // 3. Cấu hình Google
            //.AddGoogle(googleOptions => {
            //    googleOptions.ClientId = configuration["Authentication:Google:ClientId"]!;
            //    googleOptions.ClientSecret = configuration["Authentication:Google:ClientSecret"]!;
            //})
            ;

            // 4. Cấu hình CORS
            var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
            services.AddCors(options =>
            {
                options.AddPolicy(CorsPolicyName, policy =>
                {
                    if (allowedOrigins.Length > 0)
                    {
                        policy.WithOrigins(allowedOrigins)
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials();
                    }
                    else
                    {
                        // Fallback cho local/dev khi chưa cấu hình origin cụ thể
                        policy.WithOrigins("http://localhost:5284", "https://localhost:7284")
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials();
                    }
                });
            });

            // Cloudinary
            services.Configure<CloudinarySettings>(
                configuration.GetSection(CloudinarySettings.SectionName));
            services.AddScoped<IImageStorageService, CloudinaryImageStorageService>();

            // Stripe Payment
            services.Configure<StripeSettings>(
                configuration.GetSection(StripeSettings.SectionName));     
            services.AddScoped<IStripePaymentService, StripePaymentService>();

            // Đăng ký HttpContextAccessor - Thư viện dùng để truy cập HttpContext từ Service
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IEmailService, EmailService>();

            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<IBrandRepository, BrandRepository>();
            services.AddScoped<IScentFamilyRepository, ScentFamilyRepository>();
            services.AddScoped<IProductVariantRepository, ProductVariantRepository>();
            services.AddScoped<ICartRepository, CartRepository>();
            services.AddScoped<ICartItemRepository, CartItemRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IPaymentMethodRepository, PaymentMethodRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            // Application services
            services.AddScoped<IPaymentMethodService, PaymentMethodService>();
            // Register application layer validators and services if needed
            // Register variant/variant service is via product repository; no separate repo

            return services;
        }
    }
}
