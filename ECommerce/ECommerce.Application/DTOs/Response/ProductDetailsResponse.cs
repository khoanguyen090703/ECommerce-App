using System;
using System.Collections.Generic;

namespace ECommerce.Application.DTOs.Response
{
    public class ProductDetailsResponse
    {
        public int Id { get; set; }

        public string Name { get; set; } = default!;

        public string Description { get; set; } = default!;

        public List<string> ImageUrls { get; set; } = new List<string>();

        public string ScentFamilies { get; set; } = default!; // comma separated

        public string Categories { get; set; } = default!; // comma separated

        public List<ProductVariantResponse> Variants { get; set; } = new List<ProductVariantResponse>();

        public List<ReviewDetailsResponse> Reviews { get; set; } = new List<ReviewDetailsResponse>();

        public int TotalReviews { get; set; }

        public double AverageRating { get; set; }

        public string Status { get; set; } = default!;

        public DateTime CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }
    }
}
