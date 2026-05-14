namespace ECommerce.SharedViewModels.DTOs.Response
{
    public class StripePaymentStatusResponse
    {
        public int OrderId { get; set; }

        public string OrderPaymentStatus { get; set; } = default!;

        public string OrderStatus { get; set; } = default!;

        public string PaymentStatus { get; set; } = default!;

        public string? StripeCheckoutSessionId { get; set; }

        public string? StripePaymentIntentId { get; set; }

        public string? StripeSessionStatus { get; set; }

        public DateTime? CheckoutSessionExpiresAt { get; set; }

        public DateTime? PaidAt { get; set; }

        public string? TransactionId { get; set; }

        public string? FailureReason { get; set; }
    }
}
