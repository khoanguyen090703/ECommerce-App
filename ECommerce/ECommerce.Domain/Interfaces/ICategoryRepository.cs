using ECommerce.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Domain.Interfaces
{
    public interface ICategoryRepository
    {
        Task<Category?> GetById(int id);

        Task<List<Category>> GetAllAsync();

        Task AddAsync(Category category);

        Task UpdateAsync(Category category);

        Task DeleteAsync(Category category);

        Task<bool> IsNameExistedExceptAsync(string name, int id);

        Task<bool> HasProductsAsync(int id);
    }
}
