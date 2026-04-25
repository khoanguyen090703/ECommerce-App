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
                    new Category { Name = "Male", ImageUrl = "https://orchard.vn/wp-content/uploads/2024/07/category-nuoc-hoa-nam.webp"},
                    new Category { Name = "Female", ImageUrl = "https://orchard.vn/wp-content/uploads/2024/07/category-nuoc-hoa-nu-600x720.webp"},
                    new Category { Name = "Unisex", ImageUrl = "https://orchard.vn/wp-content/uploads/2024/07/category-nuoc-hoa-unisex-600x720.webp"},
                    new Category { Name = "Niche", ImageUrl = "https://orchard.vn/wp-content/uploads/2024/07/category-nuoc-hoa-niche-600x720.webp"},
                };

                await context.Categories.AddRangeAsync(categories);
                await context.SaveChangesAsync();
            }
        }
    }
}
