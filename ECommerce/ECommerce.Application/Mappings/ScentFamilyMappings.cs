using ECommerce.Application.DTOs.Response;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Mappings
{
    public static class ScentFamilyMappings
    {
        public static ScentFamilyResponse ToResponse(this ScentFamily entity)
        {
            return new ScentFamilyResponse
            {
                Id = entity.Id,
                Name = entity.Name,
                Description = entity.Description
            };
        }
    }
}
