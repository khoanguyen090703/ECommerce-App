using ECommerce.Domain.Entities;
using ECommerce.SharedViewModels.DTOs.Response;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Application.Mappings
{
    public static class OrderMappings
    {
        public static MyOrderResponse ToMyOrderResponse(this Order order)
        {
            return new MyOrderResponse
            {
                Id = order.Id,
                TotalAmount = order.TotalAmount,
                Status = order.Status.ToString(),
                OrderItems = order.OrderItems.Select(oi => oi.ToItem4MyOrderResponse()).ToList()
            };
        }

        public static OrderResponse ToOrderResponse(this Order order)
        {
            return new OrderResponse
            {
                Id = order.Id,
                TotalAmount = order.TotalAmount,
                Status = order.Status.ToString(),
                PaymentStatus = order.PaymentStatus.ToString(),
                RecipientName = order.RecipientName,
                OrderDate = order.OrderDate,
                CompletedDate = order.CompletedDate
            };
        }

        public static OrderDetailsResponse ToOrderDetailsResponse(this Order order)
        {
            return new OrderDetailsResponse
            {
                Id = order.Id,
                TotalAmount = order.TotalAmount,
                Status = order.Status.ToString(),
                PaymentStatus = order.PaymentStatus.ToString(),
                RecipientName = order.RecipientName,
                OrderDate = order.OrderDate,
                CompletedDate = order.CompletedDate,
                PhoneNumber = order.PhoneNumber,
                ShippingAddress = order.ShippingAddress,
                SubTotal = order.SubTotal,
                ShippingFee = order.ShippingFee,
                OrderItems = order.OrderItems.Select(oi => oi.ToItem4MyOrderResponse()).ToList()
            };
        }
    }
}
