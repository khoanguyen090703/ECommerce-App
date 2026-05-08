using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.SharedViewModels.DTOs.Request
{
    public class CreateOrderRequest
    {
        public string RecipientName { get; set; } = default!;

        public string PhoneNumber { get; set; } = default!;

        public string ShippingAddress { get; set; } = default!;

        public int PaymentMethodId { get; set; }

        public List<Item4CreateOrderRequest> OrderItems { get; set; } = default!;
    }
}
