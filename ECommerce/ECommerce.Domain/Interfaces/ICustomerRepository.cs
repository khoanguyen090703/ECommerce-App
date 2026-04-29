using ECommerce.Domain.Entities;

namespace ECommerce.Domain.Interfaces;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdentityIdAsync(Guid identityId, CancellationToken cancellationToken = default);

    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);
}
