using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ECommerce.Infrastructure.Persistence.Repositories
{
    public class PaymentMethodRepository : IPaymentMethodRepository
    {
        private readonly AppDbContext _context;

        public PaymentMethodRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PaymentMethod?> GetByIdAsync(int id)
        {
            return await _context.PaymentMethods.FindAsync(id);
        }

        public async Task<List<PaymentMethod>> GetAllAsync(bool includeInactive = false)
        {
            var query = _context.PaymentMethods.AsQueryable();
            if (!includeInactive)
                query = query.Where(pm => pm.IsActive);

            return await query.ToListAsync();
        }
    }
}
