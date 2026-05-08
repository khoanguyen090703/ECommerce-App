using ECommerce.SharedViewModels.DTOs.Request;
using System;
using System.Collections.Generic;
using System.Text;
using ECommerce.SharedViewModels.DTOs.Response;

namespace ECommerce.Application.Interfaces
{
    public interface IOrderService
    {
        Task CreateOrderAsync(CreateOrderRequest request);
        Task<CheckoutInfoResponse> GetCheckoutInfoAsync();
    }
}
