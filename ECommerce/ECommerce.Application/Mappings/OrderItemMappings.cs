using ECommerce.Domain.Entities;
using ECommerce.SharedViewModels.DTOs.Response;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Application.Mappings
{
    public static class OrderItemMappings
    {
        public static Item4MyOrderResponse ToItem4MyOrderResponse(this OrderItem orderItem)
            {
                return new Item4MyOrderResponse
                {
                    Id = orderItem.Id,
                    ProductVariantId = orderItem.ProductVariant.Id,
                    ProductName = orderItem.ProductVariant.Name,
                    Quantity = orderItem.Quantity,
                    UnitPrice = orderItem.UnitPrice,
                    ImageUrl = orderItem.ProductVariant.Images.FirstOrDefault()?.Url ?? string.Empty
                };
        }
    }
}
