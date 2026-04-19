using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECommerce.Domain.Interfaces
{
    public interface IBrandRepository
    {
        Task<List<Brand>> GetAllAsync();
        Task<PagedResult<Brand>> GetAsync(ECommerce.Domain.QueryParameters.BrandQueryParams parameters);
        Task<Brand?> GetByIdAsync(int id);
    }
}
