namespace ECommerce.Infrastructure
{
    public class StripeSettings
    {
        public const string SectionName = "Stripe";

        public string SecretKey { get; set; } = string.Empty;

        public string PublishableKey { get; set; } = string.Empty;

        public string WebhookSecret { get; set; } = string.Empty;

        /// <summary>Stripe success redirect. Use {ORDER_ID}; CHECKOUT_SESSION_ID is appended if missing.</summary>
        public string SuccessUrl { get; set; } = string.Empty;

        /// <summary>Customer return URL when checkout is cancelled. Use {ORDER_ID} placeholder.</summary>
        public string CancelUrl { get; set; } = string.Empty;

        /// <summary>ISO code (e.g. usd). VND is zero-decimal for Stripe amounts.</summary>
        public string DefaultCurrency { get; set; } = "usd";
    }
}
