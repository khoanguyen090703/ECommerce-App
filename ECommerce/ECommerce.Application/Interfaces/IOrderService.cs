using ECommerce.SharedViewModels.DTOs.Request;
using System;
using System.Collections.Generic;
using System.Text;
using ECommerce.SharedViewModels.DTOs.Response;
using ECommerce.Domain.Common;
using ECommerce.Domain.QueryParameters;

namespace ECommerce.Application.Interfaces
{
    public interface IOrderService
    {
        Task CreateOrderAsync(CreateOrderRequest request);
        Task<CheckoutInfoResponse> GetCheckoutInfoAsync();
        Task<PagedResult<MyOrderResponse>> GetMyOrdersAsync(OrderQueryParams parameters);
        Task<PagedResult<OrderResponse>> GetOrdersAsync(OrderQueryParams parameters);
        Task<OrderDetailsResponse> GetOrderDetailsAsync(int id);
    }
}
