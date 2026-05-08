using ECommerce.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Domain.Entities
{
    public class OrderItem : BaseEntity<int>
    {
        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal TotalPrice { get; set; }

        public Order Order { get; set; }

        public ProductVariant ProductVariant { get; set; }
    }
}
