using System.Collections.Generic;

namespace ECommerce.SharedViewModels.DTOs.Request
{
    public class AddVariantStockBatchRequest
    {
        public List<AddVariantStockLineRequest> Items { get; set; } = new();
    }

    public class AddVariantStockLineRequest
    {
        public int VariantId { get; set; }

        /// <summary>Amount to add to the variant's current stock quantity.</summary>
        public int QuantityToAdd { get; set; }
    }
}
