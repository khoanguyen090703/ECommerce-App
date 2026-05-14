namespace ECommerce.SharedViewModels.DTOs.Response
{
    public class StripeCheckoutResponse
    {
        /// <summary>Stripe-hosted Checkout URL (payment link).</summary>
        public string CheckoutUrl { get; set; } = default!;

        public string SessionId { get; set; } = default!;

        public DateTime? SessionExpiresAt { get; set; }
    }
}
