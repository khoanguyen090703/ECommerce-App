using ECommerce.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Domain.Entities
{
    public class CartItem : BaseEntity<int>
    {
        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal TotalPrice { get; set; }

        public Cart Cart { get; set; } = default!;

        public ProductVariant ProductVariant { get; set; } = default!;
    }
}
