using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Infrastructure.Persistence.Seeders
{
    public static class CategorySeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            if (!await context.Categories.AnyAsync())
            {
                var categories = new List<Category>
                {
                    new Category { Name = "Male"},
                    new Category { Name = "Female"},
                    new Category { Name = "Unisex"},
                    new Category { Name = "Niche"},
                };

                await context.Categories.AddRangeAsync(categories);
                await context.SaveChangesAsync();
            }
        }
    }
}
