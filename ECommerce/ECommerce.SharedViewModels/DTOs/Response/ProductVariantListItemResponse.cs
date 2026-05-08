namespace ECommerce.SharedViewModels.DTOs.Response
{
    /// <summary>
    /// Variant summary embedded in product list API responses.
    /// </summary>
    public class ProductVariantListItemResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string Status { get; set; } = default!;
        /// <summary>First variant image URL, or empty.</summary>
        public string ImageUrl { get; set; } = default!;
    }
}
