using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using ECommerce.Domain.QueryParameters;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECommerce.Domain.Interfaces
{
    public interface ICategoryRepository
    {
        Task<Category?> GetById(int id);

        Task<List<Category>> GetAllAsync();

        Task<PagedResult<Category>> GetAsync(CategoryQueryParams parameters);

        Task AddAsync(Category category);

        Task UpdateAsync(Category category);

        Task DeleteAsync(Category category);

        Task<bool> IsNameExistedExceptAsync(string name, int id);

        Task<bool> IsNameExistedAsync(string name);

        Task<bool> HasProductsAsync(int id);
    }
}

