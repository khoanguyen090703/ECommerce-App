using ECommerce.Domain.Common;

namespace ECommerce.Domain.QueryParameters
{
    public class VariantQueryParams : BaseQueryParams
    {
        // Filter by product's scent family name
        public string? ScentFamily { get; set; }

        // Filter by product's category name
        public string? Category { get; set; }

        // Price range
        public decimal? PriceFrom { get; set; }
        public decimal? PriceTo { get; set; }
    }
}
