using ECommerce.Application.DTOs.Response;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Mappings
{
    public static class BrandMappings
    {
        public static BrandResponse ToResponse(this Brand brand)
        {
            return new BrandResponse
            {
                Id = brand.Id,
                Name = brand.Name,
                ImageUrl = brand.ImageUrl
            };
        }
    }
}
