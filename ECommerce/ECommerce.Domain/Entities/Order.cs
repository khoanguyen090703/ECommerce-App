using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Domain.Entities
{
    public class Order : BaseEntity<int>
    {
        public decimal TotalAmount { get; set; }

        public decimal SubTotal { get; set; }

        public OrderStatus Status { get; set; }

        public OrderPaymentStatus PaymentStatus { get; set; }

        public string RecipientName { get; set; } = default!;

        public string ShippingAddress { get; set; } = default!;

        public string PhoneNumber { get; set; } = default!;

        public decimal ShippingFee { get; set; } = 0;

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedDate { get; set; } 

        public Customer Customer { get; set; }

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
