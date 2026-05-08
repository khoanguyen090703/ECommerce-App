using ECommerce.Domain.Entities;
using ECommerce.Domain.Common;
using ECommerce.Domain.QueryParameters;
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

        Task<PagedResult<Order>> GetByCustomerIdAsync(Guid customerId, OrderQueryParams parameters, CancellationToken cancellationToken = default);

        Task<PagedResult<Order>> GetAsync(OrderQueryParams parameters, CancellationToken cancellationToken = default);

        Task<Order?> GetDetailsByIdAsync(int id, CancellationToken cancellationToken = default);
    }
}
