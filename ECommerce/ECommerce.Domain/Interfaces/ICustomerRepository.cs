using ECommerce.Domain.Entities;
using ECommerce.Domain.Common;
using ECommerce.Domain.QueryParameters;
using System.Collections.Generic;

namespace ECommerce.Domain.Interfaces;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdentityIdAsync(Guid identityId, CancellationToken cancellationToken = default);
    Task<Customer?> GetByIdentityIdAsync(Guid identityId);

    Task<PagedResult<Customer>> GetAsync(CustomerQueryParams parameters, CancellationToken cancellationToken = default);

    Task<Dictionary<Guid, string?>> GetEmailsByIdentityIdsAsync(IEnumerable<Guid> identityIds, CancellationToken cancellationToken = default);

    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);
    Task UpdateAsync(Customer customer);
}
