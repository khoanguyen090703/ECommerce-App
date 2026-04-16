using ECommerce.Application.Exceptions;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
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

        public async Task<List<Product>> GetAllAsync()
        {
            List<Product> products;
            try
            {
                products = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Images)
                    .ToListAsync();

            }
            catch
            {
                throw;
            }
            return products;
        }
    }
}
