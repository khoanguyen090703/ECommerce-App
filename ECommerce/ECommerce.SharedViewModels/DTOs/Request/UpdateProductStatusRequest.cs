using ECommerce.Domain.Enums;

namespace ECommerce.Application.DTOs.Request
{
    public class UpdateProductStatusRequest
    {
        public ProductStatus Status { get; set; }
    }
}
