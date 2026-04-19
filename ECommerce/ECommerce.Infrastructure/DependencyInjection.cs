using ECommerce.Application.Common.Interfaces;
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
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Infrastructure
{
    public static class DependencyInjection
    {
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
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    //ValidIssuer = configuration["Jwt:Issuer"],
                    //ValidAudience = configuration["Jwt:Audience"],
                    //IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!))
                };
            })
            // 3. Cấu hình Google
            //.AddGoogle(googleOptions => {
            //    googleOptions.ClientId = configuration["Authentication:Google:ClientId"]!;
            //    googleOptions.ClientSecret = configuration["Authentication:Google:ClientSecret"]!;
            //})
            ;

            // Đăng ký HttpContextAccessor - Thư viện dùng để truy cập HttpContext từ Service
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();

            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();

            return services;
        }
    }
}
