using ECommerce.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Domain.Entities
{
    public class PaymentMethod : BaseEntity<int>
    {
        public string Name { get; set; } = default!;

        public bool IsActive { get; set; } = true;

        public decimal? Fee { get; set; }

        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
