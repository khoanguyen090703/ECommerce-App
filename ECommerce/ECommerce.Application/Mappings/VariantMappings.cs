using ECommerce.SharedViewModels.DTOs.Response;
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

        public static ProductVariantDetailsResponse ToDetailsResponse(this ProductVariant v)
        {
            return new ProductVariantDetailsResponse
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
                ImageUrls = v.Images.Select(i => i.Url).ToList(),
                CreatedDate = v.CreatedDate,
                UpdatedDate = v.UpdatedDate
            };
        }

        public static Variant4CusProdDetails To4CusProdDetails(this ProductVariant v)
        {
            return new Variant4CusProdDetails
            {
                Id = v.Id,
                Format = v.Format.ToString(),
                Volumn = v.Volumn + v.Unit,
                Price = v.Price,
                ImageUrl = v.Images.FirstOrDefault()?.Url ?? string.Empty
            };
        }

        public static VariantDetails4Cus ToDetails4Cus(this ProductVariant v)
        {
            return new VariantDetails4Cus
            {
                Id = v.Id,
                Name = v.Name,
                Description = v.Product.Description,
                Brand = v.Product.Brand.Name,
                Categories = string.Join(", ", v.Product.Categories.Select(c => c.Name).ToList()),
                Status = v.Status.ToString(),
                ReleaseYear = v.Product.ReleaseYear,
                ScentFamilies = string.Join(", ", v.Product.ScentFamilies.Select(cf => cf.Name).ToList()),
                TotalReviews = v.Product.TotalReviews,
                AverageRating = v.Product.AverageRating,
                Reviews = v.Product.Reviews.Select(r => r.ToDetailsResponse()).ToList(),
                Price = v.Price,
                SoldQuantity = v.SoldQuantity,
                ImageUrls = v.Images.Select(i => i.Url).Append(v.Product.Images.FirstOrDefault()?.Url ?? string.Empty).ToList()
            };
        }
    }
}
