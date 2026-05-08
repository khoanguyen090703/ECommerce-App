using ECommerce.SharedViewModels.DTOs.Response;
using ECommerce.Domain.Entities;
using System.Collections.Generic;
using System.Linq;

namespace ECommerce.Application.Mappings
{
    public static class ProductMappings
    {
        public static ProductVariantListItemResponse ToVariantListItem(this ProductVariant v)
        {
            var firstImage = v.Images?.OrderBy(i => i.Id).FirstOrDefault()?.Url ?? string.Empty;
            return new ProductVariantListItemResponse
            {
                Id = v.Id,
                Name = v.Name,
                Price = v.Price,
                StockQuantity = v.StockQuantity,
                Status = v.Status.ToString(),
                ImageUrl = firstImage
            };
        }

        public static ProductResponse4List ToListResponse(this Product p)
        {
            var variants = p.ProductVariants?
                .OrderBy(v => v.Id)
                .Select(v => v.ToVariantListItem())
                .ToList() ?? new List<ProductVariantListItemResponse>();

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
                UpdatedDate = p.UpdatedDate,
                Variants = variants
            };
        }
    }
}
