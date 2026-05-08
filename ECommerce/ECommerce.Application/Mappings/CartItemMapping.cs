using System;
using System.Collections.Generic;
using System.Text;
using ECommerce.Domain.Entities;
using ECommerce.SharedViewModels.DTOs.Response;

namespace ECommerce.Application.Mappings
{
    public static class CartItemMapping
    {
        public static Item4CartResponse To4CartResponse(this CartItem cartItem)
        {
            return new Item4CartResponse
            {
                Id = cartItem.Id,
                ProductVariantId = cartItem.ProductVariant.Id,
                Quantity = cartItem.Quantity,
                UnitPrice = cartItem.UnitPrice,
                ProductName = cartItem.ProductVariant.Name,
                ImageUrl = cartItem.ProductVariant.Images.FirstOrDefault()?.Url ?? string.Empty,
                TotalPrice = cartItem.TotalPrice
            };
        }
    }
}
