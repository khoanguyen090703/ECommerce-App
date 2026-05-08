using ECommerce.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Domain.Interfaces
{
    public interface IPaymentRepository
    {
            Task AddAsync(Payment payment);
    
            Task UpdateAsync(Payment payment);
    
            Task<Payment?> GetByOrderIdAsync(int orderId);
    }
}
