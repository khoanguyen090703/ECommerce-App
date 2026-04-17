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
                new Category { Name = "Sua rua mat",Description = "Sua rua mat"},
                new Category { Name = "Nuoc tay trang",Description = "Nuoc tay trang"},
                new Category { Name = "Kem duong da",Description = "Kem duong da"}
            };

                await context.Categories.AddRangeAsync(categories);
                await context.SaveChangesAsync();
            }
        }
    }
}
