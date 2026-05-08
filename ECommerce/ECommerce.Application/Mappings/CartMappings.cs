using ECommerce.Domain.Entities;
using ECommerce.SharedViewModels.DTOs.Response;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Application.Mappings
{
    public static class CartMappings
    {
        public static CartResponse ToResponse(this Cart cart)
        {
            return new CartResponse
            {
                TotalItems = cart.TotalItems,
                CartItems = cart.CartItems.Select(i => i.To4CartResponse()).ToList(),
                Total = cart.CartItems.Sum(i => i.Quantity * i.ProductVariant.Price)
            };
        }
    }
}
