using ECommerce.Infrastructure.Identity;
using ECommerce.Infrastructure.Persistence.Seeders;
using Microsoft.AspNetCore.Identity;
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
        public static async Task InitialiseDatabaseAsync(this IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var services = scope.ServiceProvider;

            var context = services.GetRequiredService<AppDbContext>();
            var logger = services.GetRequiredService<ILogger<AppDbContext>>();

            var userManager = services.GetRequiredService<UserManager<AppUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

            try
            {
                // 1. Tự động chạy Migration (Tùy chọn)
                if (context.Database.IsSqlServer())
                {
                    await context.Database.MigrateAsync();
                }

                // 2. Gọi các hàm Seed
                await RoleSeeder.SeedAsync(roleManager);
                await AppUserSeeder.SeedAsync(userManager, roleManager);

                await CategorySeeder.SeedAsync(context);
                
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occured when initializing data.");
                throw;
            }
        }
    }
}
