namespace ECommerce.SharedViewModels.DTOs.Response
{
    /// <summary>Shared DTO for restock picker list and single-variant panel.</summary>
    public class VariantStockPanelResponse
    {
        public int Id { get; set; }

        public string Name { get; set; } = default!;

        /// <summary>First variant image URL (relative or absolute).</summary>
        public string FirstImageUrl { get; set; } = string.Empty;

        public int StockQuantity { get; set; }

        public string Status { get; set; } = default!;

        public int ProductId { get; set; }

        public string ProductName { get; set; } = default!;
    }
}
