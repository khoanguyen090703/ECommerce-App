using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.SharedViewModels.DTOs.Response
{
    public class OrderDetailsResponse
    {
        public int Id { get; set; }

        public decimal TotalAmount { get; set; }

        public decimal SubTotal { get; set; }

        public string Status { get; set; } = default!;

        public string PaymentStatus { get; set; } = default!;

        public string RecipientName { get; set; } = default!;

        public string ShippingAddress { get; set; } = default!;

        public string PhoneNumber { get; set; } = default!;

        public decimal ShippingFee { get; set; } = 0;

        public DateTime OrderDate { get; set; }

        public DateTime? CompletedDate { get; set; }

        public List<Item4MyOrderResponse> OrderItems { get; set; } = new List<Item4MyOrderResponse>();
    }
}
