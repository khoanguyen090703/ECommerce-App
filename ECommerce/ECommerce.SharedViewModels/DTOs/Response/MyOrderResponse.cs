using System;
using System.Collections.Generic;

namespace ECommerce.SharedViewModels.DTOs.Response
{
    public class MyOrderResponse
    {
        public int Id { get; set; }

        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = default!;

        public string PaymentStatus { get; set; } = default!;

        public DateTime OrderDate { get; set; }

        /// <summary>True when unpaid/failed online (Stripe/VnPay) checkout can be started.</summary>
        public bool CanRetryOnlinePayment { get; set; }

        public List<Item4MyOrderResponse> OrderItems { get; set; } = new List<Item4MyOrderResponse>();
    }
}
