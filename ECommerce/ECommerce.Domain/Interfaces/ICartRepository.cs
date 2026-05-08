using ECommerce.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Domain.Interfaces
{
    public interface ICartRepository
    {
        Task<Cart?> GetByCustomerIdAsync(string customerId);
        Task<List<CartItem>> GetCartItemsByCartIdAsync(int cartId);
        Task UpdateAsync(Cart cart);
        Task AddAsync(Cart cart);
    }
}
