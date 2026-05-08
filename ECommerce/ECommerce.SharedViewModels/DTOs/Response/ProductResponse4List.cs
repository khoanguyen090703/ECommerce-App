using ECommerce.Domain.Enums;
using System;
using System.Collections.Generic;

namespace ECommerce.SharedViewModels.DTOs.Response
{
    public class ProductResponse4List
    {
        public int Id { get; set; }

        public string Name { get; set; } = default!;

        public string ImageUrl { get; set; } = default!;

        public string Categories { get; set; } = default!; // comma separated names

        public int TotalVariants { get; set; }

        public int TotalReviews { get; set; }

        public double AverageRating { get; set; }

        public ProductStatus Status { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }

        public List<ProductVariantListItemResponse> Variants { get; set; } = new List<ProductVariantListItemResponse>();
    }
}
