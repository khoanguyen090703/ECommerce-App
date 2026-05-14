namespace ECommerce.SharedViewModels.DTOs.Response
{
    /// <summary>
    /// Returned after order creation. Use <see cref="RequiresOnlinePayment"/> with Stripe checkout APIs when true.
    /// </summary>
    public class CreateOrderResponse
    {
        public int OrderId { get; set; }

        /// <summary>
        /// True when payment method is online card (Stripe or legacy VnPay name until migrated).
        /// </summary>
        public bool RequiresOnlinePayment { get; set; }

        public string PaymentStatus { get; set; } = default!;

        /// <summary>True when the customer may open Stripe checkout for this order (unpaid/failed, online method, not cancelled).</summary>
        public bool CanRetryOnlinePayment { get; set; }
    }
}
