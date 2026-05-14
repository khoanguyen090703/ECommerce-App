using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.QueryParameters
{
    /// <summary>Query for admin listing variants eligible for restock (Available / OutOfStock only).</summary>
    public class RestockVariantQueryParams : BaseQueryParams
    {
        /// <summary>When set, must be <see cref="VariantStatus.Available"/> or <see cref="VariantStatus.OutOfStock"/>.</summary>
        public VariantStatus? Status { get; set; }
    }
}
