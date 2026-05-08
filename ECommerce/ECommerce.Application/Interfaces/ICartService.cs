using ECommerce.SharedViewModels.DTOs.Request;
using ECommerce.SharedViewModels.DTOs.Response;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Application.Interfaces
{
    public interface ICartService
    {
        /// <summary>
        /// Returns the number of distinct line items in the cart for the current customer.
        /// Does not create a cart if one does not exist.
        /// </summary>
        Task<int> GetCartItemCountForCurrentCustomerAsync();

        Task<CartResponse> GetCartByCurrentCustomerOrCreateCartAsync();
        Task AddItemToCartAsync(AddCartItemRequest request);
        Task UpdateCartItemQuantityAsync(int itemId, UpdateCartItemQuantityRequest request);
        Task DeleteCartItemAsync(int itemId);
        Task ClearCartAsync();
    }
}
