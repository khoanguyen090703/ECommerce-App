using ECommerce.Domain.Entities;

namespace ECommerce.Domain.Interfaces
{
    public interface IPaymentRepository
    {
        Task AddAsync(Payment payment);

        Task UpdateAsync(Payment payment);

        Task<Payment?> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken = default);

        Task<Payment?> GetByStripeCheckoutSessionIdAsync(string checkoutSessionId, CancellationToken cancellationToken = default);

        Task<Payment?> GetByStripePaymentIntentIdAsync(string paymentIntentId, CancellationToken cancellationToken = default);

        Task<Payment?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    }
}
