using ECommerce.Application.DTOs.Response;
using ECommerce.Domain.Entities;
using System.Linq;

namespace ECommerce.Application.Mappings
{
    public static class VariantMappings
    {
        public static ProductVariantResponse ToResponse(this ProductVariant v)
        {
            return new ProductVariantResponse
            {
                Id = v.Id,
                Name = v.Name,
                Format = v.Format.ToString(),
                Volumn = v.Volumn,
                Unit = v.Unit,
                Price = v.Price,
                StockQuantity = v.StockQuantity,
                Status = v.Status.ToString(),
                SoldQuantity = v.SoldQuantity,
                ImageUrls = v.Images.Select(i => i.Url).ToList()
            };
        }
    }
}
