using ECommerce.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Domain.Interfaces
{
    public interface ICartItemRepository
    {
        Task<CartItem?> GetByIdAsync(int id);

        Task<CartItem?> GetByCartIdAndProductVariantIdAsync(int cartId, int variantId);

        Task UpdateAsync(CartItem item);

        Task DeleteAsync(CartItem item);
    }
}
