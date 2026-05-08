using ECommerce.Domain.Entities;
using ECommerce.Domain.Common;
using ECommerce.Domain.Interfaces;
using ECommerce.Domain.QueryParameters;
using ECommerce.Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _context;

    public CustomerRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        await _context.Customers.AddAsync(customer, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Customer?> GetByIdentityIdAsync(Guid identityId, CancellationToken cancellationToken = default)
    {
        return await _context.Customers
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.IdentityId == identityId, cancellationToken);
    }

    public async Task<PagedResult<Customer>> GetAsync(CustomerQueryParams parameters, CancellationToken cancellationToken = default)
    {
        var query = _context.Customers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
            var term = parameters.SearchTerm.Trim();
            query = query.Where(c => c.FullName.Contains(term));
        }

        query = parameters.SortBy switch
        {
            "fullname" => query.OrderBy(c => c.FullName),
            "fullname_desc" => query.OrderByDescending(c => c.FullName),
            "created" => query.OrderBy(c => c.CreatedDate),
            "created_desc" => query.OrderByDescending(c => c.CreatedDate),
            "updated" => query.OrderBy(c => c.UpdatedDate),
            "updated_desc" => query.OrderByDescending(c => c.UpdatedDate),
            "id" => query.OrderBy(c => c.Id),
            "id_desc" => query.OrderByDescending(c => c.Id),
            _ => query.OrderByDescending(c => c.CreatedDate)
        };

        return await query.ToPagedListAsync(parameters.PageNumber, parameters.PageSize);
    }

    public async Task<Dictionary<Guid, string?>> GetEmailsByIdentityIdsAsync(IEnumerable<Guid> identityIds, CancellationToken cancellationToken = default)
    {
        var ids = identityIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<Guid, string?>();

        return await _context.Users
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Email, cancellationToken);
    }

    public async Task<Customer?> GetByIdentityIdAsync(Guid identityId)
    {
        return await _context.Customers
            .SingleOrDefaultAsync(c => c.IdentityId == identityId);
    }

    public async Task UpdateAsync(Customer customer)
    {
        _context.Update(customer);
        await _context.SaveChangesAsync();
    }
}
