using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Domain.Entities
{
    public class Product : BaseEntity<int>
    {
        public string Name { get; set; } = default!;

        public string Description { get; set; } = default!;

        public ProductConcentration Concentration { get; set; } = ProductConcentration.EDP;

        public Brand Brand { get; set; } = default!;

        public string Line {  get; set; } = default!;

        public int? ReleaseYear { get; set; }

        public ICollection<ScentFamily> ScentFamilies { get; set; } = new List<ScentFamily>();

        public int TotalReviews { get; set; } = default;

        public double AverageRating { get; set; } = default;

        public ProductStatus Status { get; set; } = ProductStatus.Draft;

        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

        public ICollection<Category> Categories { get; set; } = new List<Category>();

        public ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();

        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
