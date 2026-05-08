using System.Collections.Generic;

namespace ECommerce.SharedViewModels.DTOs.Response
{
    public class CheckoutInfoResponse
    {
        public string Email { get; set; } = default!;

        public List<Item4CartResponse> CartItems { get; set; } = new List<Item4CartResponse>();

        public decimal SubTotal { get; set; }

        public List<PaymentMethodResponse> PaymentMethods { get; set; } = new List<PaymentMethodResponse>();
    }
}
