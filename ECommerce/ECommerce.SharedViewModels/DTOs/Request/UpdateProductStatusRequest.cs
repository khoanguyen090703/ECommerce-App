using ECommerce.Domain.Enums;

namespace ECommerce.SharedViewModels.DTOs.Request
{
    public class UpdateProductStatusRequest
    {
        public ProductStatus Status { get; set; }
    }
}
