using ECommerce.SharedViewModels.DTOs.Response;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Mappings
{
    public static class PaymentMethodMappings
    {
        public static PaymentMethodResponse ToResponse(this PaymentMethod pm)
        {
            return new PaymentMethodResponse
            {
                Id = pm.Id,
                Name = pm.Name
            };
        }
    }
}
