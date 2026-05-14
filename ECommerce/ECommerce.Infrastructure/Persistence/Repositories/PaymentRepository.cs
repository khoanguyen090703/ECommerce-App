using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly AppDbContext _context;

        public PaymentRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Payment payment)
        {
            await _context.Payments.AddAsync(payment);
            await _context.SaveChangesAsync();
        }

        public async Task<Payment?> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken = default)
        {
            return await _context.Payments
                .Include(p => p.Order).ThenInclude(o => o.Customer)
                .Include(p => p.PaymentMethod)
                .FirstOrDefaultAsync(p => p.OrderId == orderId, cancellationToken);
        }

        public async Task<Payment?> GetByStripeCheckoutSessionIdAsync(string checkoutSessionId, CancellationToken cancellationToken = default)
        {
            return await _context.Payments
                .Include(p => p.Order).ThenInclude(o => o.Customer)
                .Include(p => p.PaymentMethod)
                .FirstOrDefaultAsync(p => p.StripeCheckoutSessionId == checkoutSessionId, cancellationToken);
        }

        public async Task<Payment?> GetByStripePaymentIntentIdAsync(string paymentIntentId, CancellationToken cancellationToken = default)
        {
            return await _context.Payments
                .Include(p => p.Order).ThenInclude(o => o.Customer)
                .Include(p => p.PaymentMethod)
                .FirstOrDefaultAsync(p => p.StripePaymentIntentId == paymentIntentId, cancellationToken);
        }

        public async Task<Payment?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Payments
                .Include(p => p.Order).ThenInclude(o => o.Customer)
                .Include(p => p.PaymentMethod)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public async Task UpdateAsync(Payment payment)
        {
            if (_context.Entry(payment).State == EntityState.Detached)
                _context.Payments.Update(payment);
            await _context.SaveChangesAsync();
        }
    }
}
