using ECommerce.SharedViewModels.DTOs.Response;
using ECommerce.Domain.Entities;
using System.Linq;

namespace ECommerce.Application.Mappings
{
    public static class ProductWithVariantsMappings
    {
        public static ProductWithVariantsResponse ToProductWithVariantsResponse(this Product p)
        {
            return new ProductWithVariantsResponse
            {
                Id = p.Id,
                Description = p.Description,
                Brand = p.Brand?.Name ?? string.Empty,
                Categories = p.Categories.Select(c => c.Name).ToList(),
                Status = p.Status.ToString(),
                ReleaseYear = p.ReleaseYear,
                ScentFamilies = p.ScentFamilies.Select(s => s.Name).ToList(),
                TotalReviews = p.TotalReviews,
                AverageRating = p.AverageRating,
                Reviews = p.Reviews.Select(r => r.ToDetailsResponse()).ToList(),
                ProductVariants = p.ProductVariants.Select(v => new ProductVariantInProductResponse
                {
                    Id = v.Id,
                    Format = v.Format.ToString(),
                    Volumn = $"{v.Volumn}{v.Unit}",
                    Price = v.Price,
                    Status = v.Status.ToString(),
                    SoldQuantity = v.SoldQuantity,
                    ImageUrls = v.Images.Select(i => i.Url).ToList()
                }).ToList()
            };
        }
    }
}
