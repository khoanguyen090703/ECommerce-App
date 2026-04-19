using ECommerce.Application.DTOs.Response;
using ECommerce.Domain.Entities;
using System.Linq;

namespace ECommerce.Application.Mappings
{
    public static class ProductMappings
    {
        public static ProductResponse4List ToListResponse(this Product p)
        {
            return new ProductResponse4List
            {
                Id = p.Id,
                Name = p.Name,
                ImageUrl = p.Images.FirstOrDefault()?.Url ?? string.Empty,
                Categories = string.Join(", ", p.Categories.Select(c => c.Name)),
                TotalVariants = p.ProductVariants?.Count ?? 0,
                TotalReviews = p.TotalReviews,
                AverageRating = p.AverageRating,
                Status = p.Status,
                CreatedDate = p.CreatedDate,
                UpdatedDate = p.UpdatedDate
            };
        }
    }
}
