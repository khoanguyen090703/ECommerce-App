using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.SharedViewModels.DTOs.Request
{
    public class Item4CreateOrderRequest
    {
        public int ProductVariantId { get; set; }

        public int Quantity { get; set; }
    }
}
