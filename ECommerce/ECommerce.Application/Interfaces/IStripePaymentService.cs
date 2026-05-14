using ECommerce.SharedViewModels.DTOs.Request;
using ECommerce.SharedViewModels.DTOs.Response;

namespace ECommerce.Application.Interfaces
{
    public interface IStripePaymentService
    {
        Task<StripeCheckoutResponse> CreateCheckoutAsync(CreateStripeCheckoutRequest request, CancellationToken cancellationToken = default);

        Task<StripePaymentStatusResponse> GetPaymentStatusAsync(int orderId, CancellationToken cancellationToken = default);

        Task<StripeCheckoutResponse> RetryCheckoutAsync(CreateStripeCheckoutRequest request, CancellationToken cancellationToken = default);

        Task ProcessWebhookAsync(string jsonBody, string stripeSignatureHeader, CancellationToken cancellationToken = default);
    }
}
