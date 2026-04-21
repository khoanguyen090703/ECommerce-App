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
    }
}
