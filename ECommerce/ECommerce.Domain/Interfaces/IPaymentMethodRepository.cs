using ECommerce.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Domain.Interfaces
{
    public interface IPaymentMethodRepository
    {
        Task<PaymentMethod?> GetByIdAsync(int id);
        Task<List<PaymentMethod>> GetAllAsync(bool includeInactive = false);
    }
}
