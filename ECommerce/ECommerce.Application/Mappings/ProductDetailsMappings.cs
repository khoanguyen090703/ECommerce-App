using ECommerce.SharedViewModels.DTOs.Response;
using ECommerce.Domain.Entities;
using System.Collections.Generic;
using System.Linq;

namespace ECommerce.Application.Mappings
{
    public static class ProductDetailsMappings
    {
        public static ProductDetailsResponse ToDetailsResponse(this Product product)
        {
            return new ProductDetailsResponse
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                ImageUrls = product.Images.Select(i => i.Url).ToList(),
                ScentFamilies = string.Join(", ", product.ScentFamilies.Select(s => s.Name)),
                Categories = string.Join(", ", product.Categories.Select(c => c.Name)),
                Variants = product.ProductVariants?.Select(v => v.ToResponse()).ToList() ?? new List<ProductVariantResponse>(),
                Reviews = product.Reviews.Select(r => r.ToDetailsResponse()).ToList(),
                TotalReviews = product.TotalReviews,
                AverageRating = product.AverageRating,
                Status = product.Status.ToString(),
                CreatedDate = product.CreatedDate,
                UpdatedDate = product.UpdatedDate,
                BrandId = product.Brand?.Id ?? 0,
                BrandName = product.Brand?.Name ?? string.Empty,
                Line = product.Line ?? string.Empty,
                ReleaseYear = product.ReleaseYear,
                Concentration = product.Concentration.ToString(),
                CategoryIds = product.Categories.Select(c => c.Id).ToList(),
                ScentFamilyIds = product.ScentFamilies.Select(s => s.Id).ToList()
            };
        }
    }
}
