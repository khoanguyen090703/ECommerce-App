using ECommerce.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.SharedViewModels.DTOs.Response
{
    public class OrderResponse
    {
        public int Id { get; set; }

        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = default!;

        public string PaymentStatus { get; set; } = default!;

        public string RecipientName { get; set; } = default!;

        public DateTime OrderDate { get; set; }

        public DateTime? CompletedDate { get; set; }
    }
}
