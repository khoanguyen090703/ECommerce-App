using ECommerce.Domain.Enums;

namespace ECommerce.Application.DTOs.Request
{
    public class UpdateVariantStatusRequest
    {
        public VariantStatus Status { get; set; }
    }
}
