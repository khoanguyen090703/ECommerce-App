using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Infrastructure.Persistence.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly AppDbContext _context;

        public CartRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Cart cart)
        {
            await _context.Carts.AddAsync(cart);
            await _context.SaveChangesAsync();
        }

        public async Task<Cart?> GetByCustomerIdAsync(string customerId)
        {
            return await _context.Carts
                .Include(c => c.CartItems).ThenInclude(ci => ci.ProductVariant).ThenInclude(pv => pv.Images)
                .FirstOrDefaultAsync(c => c.Customer.Id.ToString().Equals(customerId));
        }

        public async Task<List<CartItem>> GetCartItemsByCartIdAsync(int cartId)
        {
            return await _context.Carts.Where(c => c.Id == cartId)
                .SelectMany(c => c.CartItems)
                .ToListAsync();
        }

        public async Task UpdateAsync(Cart cart)
        {
            _context.Carts.Update(cart);
            await _context.SaveChangesAsync();
        }
    }
}
