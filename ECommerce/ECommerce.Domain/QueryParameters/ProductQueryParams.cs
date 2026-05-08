using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.QueryParameters
{
    public class ProductQueryParams : BaseQueryParams
    {
        public ProductStatus? Status { get; set; }

        public int? BrandId { get; set; }

        public int? CategoryId { get; set; }

        public int? ScentFamilyId { get; set; }
    }
}
