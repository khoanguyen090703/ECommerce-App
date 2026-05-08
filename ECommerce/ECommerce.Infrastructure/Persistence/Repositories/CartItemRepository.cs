using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Infrastructure.Persistence.Repositories
{
    public class CartItemRepository : ICartItemRepository
    {
        private readonly AppDbContext _context;

        public CartItemRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<CartItem?> GetByIdAsync(int id)
        {
            return await _context.CartItems
                .Include(ci => ci.Cart)
                .Include(ci => ci.ProductVariant)
                .FirstOrDefaultAsync(ci => ci.Id == id);
        }

        public async Task UpdateAsync(CartItem item)
        {
            _context.CartItems.Update(item);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(CartItem item)
        {
            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();
        }

        public async Task<CartItem?> GetByCartIdAndProductVariantIdAsync(int cartId, int variantId)
        {
            return await _context.CartItems
                .Include(ci => ci.Cart)
                .Include(ci => ci.ProductVariant)
                .FirstOrDefaultAsync(ci => ci.Cart.Id == cartId && ci.ProductVariant.Id == variantId);
        }
    }
}