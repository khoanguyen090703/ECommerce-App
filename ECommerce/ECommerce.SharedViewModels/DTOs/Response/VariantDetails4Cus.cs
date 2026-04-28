using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.SharedViewModels.DTOs.Response
{
    public class VariantDetails4Cus
    {
        public int Id { get; set; }

        public string Name { get; set; } = default!;

        public string Description { get; set; } = default!;

        public string Brand { get; set; } = default!;

        public string Categories { get; set; } = default!;

        public string Status { get; set; } = default!;

        public int? ReleaseYear { get; set; }

        public string? ScentFamilies { get; set; }

        public int TotalReviews { get; set; }

        public double AverageRating { get; set; }

        public List<ReviewDetailsResponse> Reviews { get; set; } = new List<ReviewDetailsResponse>();

        public List<Variant4CusProdDetails> ProductVariants { get; set; } = new List<Variant4CusProdDetails>();

        public decimal Price { get; set; }

        public int SoldQuantity { get; set; }

        public List<string> ImageUrls { get; set; } = new List<string>();
    }
}
