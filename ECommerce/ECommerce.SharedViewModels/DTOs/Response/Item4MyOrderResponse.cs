using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.SharedViewModels.DTOs.Response
{
    public class Item4MyOrderResponse
    {
        public int Id { get; set; }

        public int ProductVariantId { get; set; }

        public string ProductName { get; set; } = default!;

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public string ImageUrl { get; set; } = default!;
    }
}
