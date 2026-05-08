using ECommerce.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Domain.Entities
{
    public class Cart : BaseEntity<int>
    {
        public int TotalItems { get; set; } = 0;

        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

        public Customer Customer { get; set; }
    }
}
