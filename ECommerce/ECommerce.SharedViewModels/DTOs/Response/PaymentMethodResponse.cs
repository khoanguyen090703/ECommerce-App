using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.SharedViewModels.DTOs.Response
{
    public class PaymentMethodResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
    }
}
