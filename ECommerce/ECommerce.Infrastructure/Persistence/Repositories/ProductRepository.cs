using ECommerce.Application.Exceptions;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using ECommerce.Domain.QueryParameters;
using ECommerce.Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Infrastructure.Persistence.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly AppDbContext _context;

        public ProductRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Product product)
        {
                await _context.Products.AddAsync(product);
                await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Product product)
        {
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Product>> GetAllAsync()
        {
            List<Product> products;
            try
            {
                products = await _context.Products
                    //.Include(p => p.Category)
                    .Include(p => p.Images)
                    .Include(p => p.Categories)
                    .Include(p => p.ProductVariants)
                    .Include(p => p.Reviews)
                    .ToListAsync();

            }
            catch
            {
                throw;
            }
            return products;
        }

        public async Task<PagedResult<Product>> GetAsync(ProductQueryParams parameters)
        {
            var query = _context.Products
                //.Include (p => p.Category)
                .Include (p => p.Images)
                .Include(p => p.Categories)
                .Include(p => p.ProductVariants)
                .Include(p => p.Reviews)
                .AsNoTracking().AsQueryable();

            // Search and Filter
            if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
            {
                query = query.Where(x => x.Name.Contains(parameters.SearchTerm));
            }

            // Sort
            query = parameters.SortBy switch
            {
                "name_desc" => query.OrderByDescending(p => p.Name),
                _ => query.OrderBy(x => x.Id)
            };

            // Return with pagination
            return await
                query.ToPagedListAsync(parameters.PageNumber, parameters.PageSize);
        }

        public async Task<Product?> GetByIdAsync(int id)
        {
            var product = await _context.Products
                //.Include (p => p.Category)
                .Include (p => p.Images)
                .Include(p => p.Categories)
                .Include(p => p.ProductVariants).ThenInclude(pv => pv.Images)
                .Include(p => p.Reviews).ThenInclude(r => r.ReviewResponses)
                .SingleOrDefaultAsync(p => p.Id == id);
            return product;
        }

        public async Task<bool> IsNameExistedAsync(string name)
        {
            return await _context.Products.AnyAsync(p => p.Name == name);
        }

        public async Task UpdateAsync(Product product)
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
        }
    }
}
