using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Infrastructure.Persistence.Repositories
{
    public class ProductVariantRepository : IProductVariantRepository
    {
        private readonly AppDbContext _context;

        public ProductVariantRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task DeleteAsync(ProductVariant variant)
        {
            _context.ProductVariants.Remove(variant);
            await _context.SaveChangesAsync();
        }

        public async Task<ProductVariant?> GetByIdAsync(int id)
        {
            var variant = await _context.ProductVariants
                .Include(v => v.Images)
                .FirstOrDefaultAsync(v => v.Id == id);

            return variant;
        }

        public async Task<ProductVariant?> GetByIdForUpdateAsync(int id)
        {
            var variant = await _context.ProductVariants
                .Include(v => v.Images)
                .Include(v => v.Product)
                    .ThenInclude(p => p.ProductVariants).ThenInclude(pv => pv.Images)
                .Include(v => v.Product).ThenInclude(p => p.Reviews)
                .FirstOrDefaultAsync(v => v.Id == id);

            return variant;
        }

        public async Task UpdateAsync(ProductVariant variant)
        {
            _context.ProductVariants.Update(variant);
            await _context.SaveChangesAsync();
        }
    }
}
