using ECommerce.Application.Exceptions;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using ECommerce.Domain.QueryParameters;
using ECommerce.Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
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
                .Include(p => p.Images)
                .Include(p => p.Categories)
                .Include(p => p.ScentFamilies)
                .Include(p => p.Brand)
                .Include(p => p.ProductVariants).ThenInclude(v => v.Images)
                .Include(p => p.Reviews)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
            {
                var term = parameters.SearchTerm.Trim();
                query = query.Where(x =>
                    x.Name.Contains(term)
                    || (x.Description != null && x.Description.Contains(term)));
            }

            if (parameters.Status.HasValue)
            {
                query = query.Where(p => p.Status == parameters.Status.Value);
            }

            if (parameters.BrandId is > 0)
            {
                query = query.Where(p => p.Brand != null && p.Brand.Id == parameters.BrandId.Value);
            }

            if (parameters.CategoryId is > 0)
            {
                query = query.Where(p => p.Categories.Any(c => c.Id == parameters.CategoryId.Value));
            }

            if (parameters.ScentFamilyId is > 0)
            {
                query = query.Where(p => p.ScentFamilies.Any(sf => sf.Id == parameters.ScentFamilyId.Value));
            }

            query = parameters.SortBy switch
            {
                "name" => query.OrderBy(p => p.Name),
                "name_desc" => query.OrderByDescending(p => p.Name),
                "id" => query.OrderBy(p => p.Id),
                "id_desc" => query.OrderByDescending(p => p.Id),
                "created" => query.OrderBy(p => p.CreatedDate),
                "created_desc" => query.OrderByDescending(p => p.CreatedDate),
                "updated" => query.OrderBy(p => p.UpdatedDate ?? p.CreatedDate),
                "updated_desc" => query.OrderByDescending(p => p.UpdatedDate ?? p.CreatedDate),
                "status" => query.OrderBy(p => p.Status),
                "status_desc" => query.OrderByDescending(p => p.Status),
                _ => query.OrderByDescending(p => p.Id)
            };

            return await query.ToPagedListAsync(parameters.PageNumber, parameters.PageSize);
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

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Products.AsNoTracking().AnyAsync(p => p.Id == id);
        }

        public async Task<PagedResult<ProductVariant>> GetVariantsByProductIdAsync(int productId, ProductVariantsQueryParams parameters)
        {
            var query = _context.ProductVariants
                .AsNoTracking()
                .Include(v => v.Images)
                .Where(v => v.Product.Id == productId);

            if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
            {
                var term = parameters.SearchTerm.Trim();
                query = query.Where(v => v.Name.Contains(term));
            }

            query = parameters.SortBy switch
            {
                "name" => query.OrderBy(v => v.Name),
                "name_desc" => query.OrderByDescending(v => v.Name),
                "id" => query.OrderBy(v => v.Id),
                "id_desc" => query.OrderByDescending(v => v.Id),
                "price" => query.OrderBy(v => v.Price),
                "price_desc" => query.OrderByDescending(v => v.Price),
                "stock" => query.OrderBy(v => v.StockQuantity),
                "stock_desc" => query.OrderByDescending(v => v.StockQuantity),
                "created" => query.OrderBy(v => v.CreatedDate),
                "created_desc" => query.OrderByDescending(v => v.CreatedDate),
                "status" => query.OrderBy(v => v.Status),
                "status_desc" => query.OrderByDescending(v => v.Status),
                "sold" => query.OrderBy(v => v.SoldQuantity),
                "sold_desc" => query.OrderByDescending(v => v.SoldQuantity),
                _ => query.OrderBy(v => v.Id)
            };

            return await query.ToPagedListAsync(parameters.PageNumber, parameters.PageSize);
        }

        public async Task<Product?> GetByIdAsync(int id, bool includeProductVariants = true)
        {
            IQueryable<Product> query = _context.Products
                .Include(p => p.Images)
                .Include(p => p.Brand)
                .Include(p => p.Categories)
                .Include(p => p.ScentFamilies);

            if (includeProductVariants)
            {
                query = query.Include(p => p.ProductVariants).ThenInclude(pv => pv.Images);
            }

            query = query.Include(p => p.Reviews).ThenInclude(r => r.ReviewResponses);

            var product = await query.SingleOrDefaultAsync(p => p.Id == id);
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
                .Include(v => v.Product)
                    .ThenInclude(p => p.Brand)
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

        public async Task<bool> IsNameExistedAsync(string name, int? excludeProductId = null)
        {
            var query = _context.Products.AsQueryable().Where(p => p.Name == name);
            if (excludeProductId is > 0)
                query = query.Where(p => p.Id != excludeProductId.Value);
            return await query.AnyAsync();
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

        public async Task<List<ProductVariant>> GetVariantsOfProductByIdAsync(int id)
        {
            var variants = await _context.Products
                .Include(v => v.ProductVariants).ThenInclude(pv => pv.Images)
                .AsNoTracking()
                .Where(v => v.Id == id)
                .SelectMany(p => p.ProductVariants) 
                .ToListAsync();

            return variants;
        }
    }
}
