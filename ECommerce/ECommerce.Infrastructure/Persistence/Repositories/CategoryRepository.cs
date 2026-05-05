using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using ECommerce.Domain.QueryParameters;
using ECommerce.Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        public async Task<PagedResult<Category>> GetAsync(CategoryQueryParams parameters)
        {
            var query = _context.Categories.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
            {
                var term = parameters.SearchTerm.Trim();
                query = query.Where(c =>
                    c.Name.Contains(term)
                    || (c.Description != null && c.Description.Contains(term)));
            }

            query = parameters.SortBy switch
            {
                "name" => query.OrderBy(c => c.Name),
                "name_desc" => query.OrderByDescending(c => c.Name),
                "id" => query.OrderBy(c => c.Id),
                "id_desc" => query.OrderByDescending(c => c.Id),
                "created" => query.OrderBy(c => c.CreatedDate),
                "created_desc" => query.OrderByDescending(c => c.CreatedDate),
                _ => query.OrderByDescending(c => c.Id)
            };

            return await query.ToPagedListAsync(parameters.PageNumber, parameters.PageSize);
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

        public async Task<bool> IsNameExistedAsync(string name)
        {
            var result = await _context.Categories.AnyAsync(c => c.Name.Equals(name));
            return result;
        }

        public async Task UpdateAsync(Category category)
        {
            _context.Categories.Update(category);
            await _context.SaveChangesAsync();
        }
    }
}
