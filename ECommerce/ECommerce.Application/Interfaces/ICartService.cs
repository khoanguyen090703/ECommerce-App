using ECommerce.SharedViewModels.DTOs.Request;
using ECommerce.SharedViewModels.DTOs.Response;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Application.Interfaces
{
    public interface ICartService
    {
        Task<CartResponse> GetCartByCurrentCustomerOrCreateCartAsync();
        Task AddItemToCartAsync(AddCartItemRequest request);
        Task UpdateCartItemQuantityAsync(int itemId, UpdateCartItemQuantityRequest request);
        Task DeleteCartItemAsync(int itemId);
        Task ClearCartAsync();
    }
}
