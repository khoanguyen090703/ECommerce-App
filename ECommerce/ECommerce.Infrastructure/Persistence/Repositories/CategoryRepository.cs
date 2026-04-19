using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Infrastructure.Persistence.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly AppDbContext _context;

        public CategoryRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Category category)
        {
            await _context.Categories.AddAsync(category);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Category category)
        {
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Category>> GetAllAsync()
        {
            var categories = await _context.Categories.ToListAsync();
            return categories;
        }

        public async Task<Category?> GetById(int id)
        {
            var category = await _context.Categories.SingleOrDefaultAsync(c => c.Id == id);
            return category;
        }

        public async Task<bool> HasProductsAsync(int id)
        {
            var result = await _context.Categories
                    .AnyAsync(c => c.Id == id && c.Products.Any());
            return result;
        }

        public async Task<bool> IsNameExistedExceptAsync(string name, int id)
        {
            var result = await _context.Categories.AnyAsync(c => c.Name.Equals(name) && c.Id != id);
            return result;
        }

        public async Task UpdateAsync(Category category)
        {
            _context.Categories.Update(category);
            await _context.SaveChangesAsync();
        }
    }
}
