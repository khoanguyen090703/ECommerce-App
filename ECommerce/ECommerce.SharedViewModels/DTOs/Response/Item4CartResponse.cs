using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.SharedViewModels.DTOs.Response
{
    public class Item4CartResponse
    {
        public int Id { get; set; }

        public int ProductVariantId { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public string ProductName { get; set; } = default!;

        public string ImageUrl { get; set; } = default!;

        public decimal TotalPrice { get; set; }
    }
}
