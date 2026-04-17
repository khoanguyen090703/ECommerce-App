using ECommerce.Infrastructure.Persistence.Seeders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Infrastructure.Persistence
{
    public static class DbInitializer
    {
        public static async Task InitialiseDatabaseAsync(this IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

            try
            {
                // 1. Tự động chạy Migration (Tùy chọn)
                if (context.Database.IsSqlServer())
                {
                    await context.Database.MigrateAsync();
                }

                // 2. Gọi các hàm Seed
                await CategorySeeder.SeedAsync(context);
                // await ProductSeeder.SeedAsync(context); // Ví dụ thêm seeder khác
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occured when initializing data.");
                throw;
            }
        }
    }
}
