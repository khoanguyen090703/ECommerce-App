namespace ECommerce.SharedViewModels.DTOs.Response
{
    public class OrderPaymentDetailsResponse
    {
        public int PaymentId { get; set; }

        public string PaymentMethodName { get; set; } = default!;

        public string Status { get; set; } = default!;

        public decimal Amount { get; set; }

        public DateTime? PaidAt { get; set; }

        public string? TransactionId { get; set; }

        public string? StripeCheckoutSessionId { get; set; }

        public string? StripePaymentIntentId { get; set; }

        public string? FailureReason { get; set; }

        public DateTime? CheckoutSessionExpiresAt { get; set; }

        public DateTime? LastStripeWebhookAt { get; set; }
    }
}
