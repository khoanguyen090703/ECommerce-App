using System.Collections.Generic;

namespace ECommerce.Application.DTOs.Response
{
    public class ProductWithVariantsResponse
    {
        public int Id { get; set; }
        public string Description { get; set; } = default!;
        public string Brand { get; set; } = default!;
        public List<string> Categories { get; set; } = new List<string>();
        public string Status { get; set; } = default!;
        public int? ReleaseYear { get; set; }
        public List<string> ScentFamilies { get; set; } = new List<string>();
        public int TotalReviews { get; set; }
        public double AverageRating { get; set; }
        public List<ReviewDetailsResponse> Reviews { get; set; } = new List<ReviewDetailsResponse>();
        public List<ProductVariantInProductResponse> ProductVariants { get; set; } = new List<ProductVariantInProductResponse>();
    }
}
