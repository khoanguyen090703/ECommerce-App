using ECommerce.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Domain.Interfaces
{
    public interface IProductVariantRepository
    {
        Task<ProductVariant?> GetByIdAsync(int id);
        Task<ProductVariant?> GetByIdForUpdateAsync(int id);
        Task UpdateAsync(ProductVariant variant);
        Task DeleteAsync(ProductVariant variant);
        Task<List<ProductVariant>> GetByIdsForUpdateAsync(IEnumerable<int> ids);
        Task UpdateRangeAsync(IEnumerable<ProductVariant> variants);
        Task<List<ProductVariant>> GetDefaultVariantsAsync();
    }
}
