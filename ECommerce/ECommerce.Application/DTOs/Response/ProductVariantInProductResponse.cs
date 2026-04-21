using System.Collections.Generic;

namespace ECommerce.Application.DTOs.Response
{
    public class ProductVariantInProductResponse
    {
        public int Id { get; set; }
        public string Format { get; set; } = default!;
        // Combined volumn and unit, e.g. "50ml"
        public string Volumn { get; set; } = default!;
        public decimal Price { get; set; }
        public string Status { get; set; } = default!;
        public int SoldQuantity { get; set; }
        public List<string> ImageUrls { get; set; } = new List<string>();
    }
}
