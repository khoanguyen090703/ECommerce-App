using ECommerce.SharedViewModels.DTOs.Response;
using ECommerce.Domain.Entities;
using System.Linq;

namespace ECommerce.Application.Mappings
{
    public static class VariantToVariantResponseMappings
    {
        public static VariantResponse ToVariantResponse(this ProductVariant v)
        {
            return new VariantResponse
            {
                Id = v.Id,
                Name = v.Name,
                Price = v.Price,
                Status = v.Status.ToString(),
                ImageUrl = v.Images.FirstOrDefault()?.Url ?? string.Empty
            };
        }
    }
}
