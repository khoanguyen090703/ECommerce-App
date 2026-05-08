using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Domain.Entities
{
    public class Payment : BaseEntity<int>
    {
        public decimal Amount { get; set; }

        public string? TransactionId { get; set; }

        public PaymentStatus Status { get; set; }

        public DateTime? PaidAt { get; set; }

        public Order Order { get; set; }

        public PaymentMethod PaymentMethod { get; set; }
    }
}
