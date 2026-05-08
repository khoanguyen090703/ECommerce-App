using System.Collections.Generic;

namespace ECommerce.SharedViewModels.DTOs.Response
{
    public class ProductVariantResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string Format { get; set; } = default!;
        public int Volumn { get; set; }
        public string Unit { get; set; } = default!;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string Status { get; set; } = default!;
        public int SoldQuantity { get; set; }
        public List<string> ImageUrls { get; set; } = new List<string>();

        public bool IsDefault { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }
    }
}
