using ECommerce.Domain.Common;

namespace ECommerce.Domain.QueryParameters
{
    /// <summary>Query for variants belonging to a single product (paged list).</summary>
    public class ProductVariantsQueryParams : BaseQueryParams
    {
        /// <summary>When true, returns variants in any status (admin). Default false: only Available.</summary>
        public bool IncludeAllStatuses { get; set; }
    }
}
