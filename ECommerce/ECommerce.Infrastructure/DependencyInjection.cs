using ECommerce.Domain.Interfaces;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Infrastructure.Persistence.Intercepters;
using ECommerce.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();

            return services;
        }
    }
}
