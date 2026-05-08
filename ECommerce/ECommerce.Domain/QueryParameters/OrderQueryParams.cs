using ECommerce.Domain.Common;

namespace ECommerce.Domain.QueryParameters
{
    public class OrderQueryParams : BaseQueryParams
    {
        public string? Status { get; set; }
    }
}
