using ECommerce.Domain.Enums;

namespace ECommerce.SharedViewModels.DTOs.Request
{
    public class UpdateVariantStatusRequest
    {
        public VariantStatus Status { get; set; }
    }
}
