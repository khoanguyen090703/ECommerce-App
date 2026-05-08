using ECommerce.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Domain.Interfaces
{
    public interface IOrderRepository
    {
        Task AddAsync(Order order);

        Task UpdateAsync(Order order);

        Task<Order?> GetByIdAsync(int id);
    }
}
