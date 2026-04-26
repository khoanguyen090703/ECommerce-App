using ECommerce.Domain.Common;

namespace ECommerce.Domain.QueryParameters
{
    public class VariantQueryParams : BaseQueryParams
    {
        // Filter by product's scent family name
        public string? ScentFamily { get; set; }

        // Filter by product's category name
        public string? Category { get; set; }
        
        // Filter by product's brand name
        public string? Brand { get; set; }

        // Price range
        public decimal? FromPrice { get; set; }
        public decimal? ToPrice { get; set; }
    }
}
