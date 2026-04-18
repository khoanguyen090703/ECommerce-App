using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Domain.Entities
{
    public class ProductVariant : BaseEntity<int>
    {
        public string Name { get; set; } = default!;

        public VariantFormat Format { get; set; } = VariantFormat.FullBottle;

        public int Volumn { get; set; }

        public string Unit { get; set; } = "ml";

        public decimal Price { get; set; }

        public int StockQuantity { get; set; } = default;

        public VariantStatus Status { get; set; } = VariantStatus.Available;

        public int SoldQuantity { get; set; } = default;

        public Product Product { get; set; } = default!;

        public ICollection<VariantImage> Images { get; set; } = new List<VariantImage>();
    }
}
