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

        public async Task<PagedResult<ProductVariant>> GetVariantsAsync(ECommerce.Domain.QueryParameters.VariantQueryParams parameters)
        {
            var query = _context.ProductVariants
                .Include(v => v.Images)
                .Include(v => v.Product).ThenInclude(p => p.Images)
                .Include(v => v.Product).ThenInclude(p => p.Categories)
                .Include(v => v.Product).ThenInclude(p => p.ScentFamilies)
                .Include(v => v.Product).ThenInclude(p => p.Brand)
                .AsNoTracking().AsQueryable();

            // Only include variants whose parent product is Active
            query = query.Where(v => v.Product.Status == ECommerce.Domain.Enums.ProductStatus.Active);

            // Search term on variant name
            if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
            {
                query = query.Where(v => v.Name.Contains(parameters.SearchTerm));
            }

            // Filter by scent family
            if (!string.IsNullOrWhiteSpace(parameters.ScentFamily))
            {
                query = query.Where(v => v.Product.ScentFamilies.Any(sf => sf.Name.Contains(parameters.ScentFamily)));
            }

            // Filter by category
            if (!string.IsNullOrWhiteSpace(parameters.Category))
            {
                query = query.Where(v => v.Product.Categories.Any(c => c.Name.Contains(parameters.Category)));
            }
            
            // Filter by brand
            if (!string.IsNullOrWhiteSpace(parameters.Brand))
            {
                query = query.Where(v => v.Product.Brand != null && v.Product.Brand.Name.Contains(parameters.Brand));
            }

            // Filter by price range
            if (parameters.FromPrice.HasValue)
            {
                query = query.Where(v => v.Price >= parameters.FromPrice.Value);
            }
            if (parameters.ToPrice.HasValue)
            {
                query = query.Where(v => v.Price <= parameters.ToPrice.Value);
            }

            // Sort
            query = parameters.SortBy switch
            {
                "price_desc" => query.OrderByDescending(v => v.Price),
                "price_asc" => query.OrderBy(v => v.Price),
                "updated_desc" => query.OrderByDescending(v => v.UpdatedDate ?? v.CreatedDate),
                "updated_asc" => query.OrderBy(v => v.UpdatedDate ?? v.CreatedDate),
                _ => query.OrderBy(v => v.Id)
            };

            return await query.ToPagedListAsync(parameters.PageNumber, parameters.PageSize);
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

        public async Task<ProductVariant?> GetVariantByIdAsync(int id)
        {
            var variant = await _context.ProductVariants
                .Include(v => v.Images)
                .Include(v => v.Product)
                    .ThenInclude(p => p.Images)
                .Include(v => v.Product)
                    .ThenInclude(p => p.Categories)
                .Include(v => v.Product)
                    .ThenInclude(p => p.ScentFamilies)
                //.Include(v => v.Product)
                //    .ThenInclude(p => p.ProductVariants).ThenInclude(pv => pv.Images)
                .Include(v => v.Product)
                    .ThenInclude(p => p.Reviews).ThenInclude(r => r.ReviewResponses)
                .AsNoTracking()
                .SingleOrDefaultAsync(v => v.Id == id);

            return variant;
        }

        public async Task<ProductVariant?> GetVariantByIdForUpdateAsync(int id)
        {
            var variant = await _context.ProductVariants
                .Include(v => v.Images)
                .Include(v => v.Product)
                    .ThenInclude(p => p.Images)
                .Include(v => v.Product)
                    .ThenInclude(p => p.Categories)
                .Include(v => v.Product)
                    .ThenInclude(p => p.ScentFamilies)
                .Include(v => v.Product)
                    .ThenInclude(p => p.ProductVariants).ThenInclude(pv => pv.Images)
                .Include(v => v.Product)
                    .ThenInclude(p => p.Reviews).ThenInclude(r => r.ReviewResponses)
                .SingleOrDefaultAsync(v => v.Id == id);

            return variant;
        }

        public async Task UpdateVariantAsync(ProductVariant variant)
        {
            _context.ProductVariants.Update(variant);
            await _context.SaveChangesAsync();
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

        public async Task UpdateProductStatusAsync(int id, ECommerce.Domain.Enums.ProductStatus status)
        {
            var product = await _context.Products.SingleOrDefaultAsync(p => p.Id == id);
            if (product == null)
                return;

            product.Status = status;
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
        }

        public async Task<List<ProductVariant>> GetFeaturedDefaultVariantsAsync()
        {
            var variants = await _context.Products
                .Where(p => p.IsFeatured && p.Status == ECommerce.Domain.Enums.ProductStatus.Active)
                .SelectMany(p => p.ProductVariants)
                .Where(v => v.IsDefault)
                .Include(v => v.Images)
                .ToListAsync();

            return variants;
        }
    }
}
