using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities
{
    public class Payment : BaseEntity<int>
    {
        public int OrderId { get; set; }

        public int PaymentMethodId { get; set; }

        public decimal Amount { get; set; }

        public string? TransactionId { get; set; }

        public PaymentStatus Status { get; set; }

        public DateTime? PaidAt { get; set; }

        public string? StripeCheckoutSessionId { get; set; }

        public string? StripePaymentIntentId { get; set; }

        public string? FailureReason { get; set; }

        public DateTime? CheckoutSessionExpiresAt { get; set; }

        public DateTime? LastStripeWebhookAt { get; set; }

        public Order Order { get; set; } = default!;

        public PaymentMethod PaymentMethod { get; set; } = default!;
    }
}
