using ECommerce.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Domain.Interfaces
{
    public interface IProductRepository
    {
        Task<List<Product>> GetAllAsync();

        Task AddAsync(Product product);

        Task<Product?> GetByIdAsync(int id);

        Task UpdateAsync(Product product);

        Task DeleteAsync(Product product);
    }
}
