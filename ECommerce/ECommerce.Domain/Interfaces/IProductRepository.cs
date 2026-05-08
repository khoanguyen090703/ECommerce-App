using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using ECommerce.Domain.QueryParameters;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Domain.Interfaces
{
    public interface IProductRepository
    {
        Task<List<Product>> GetAllAsync();

        Task AddAsync(Product product);

        Task<Product?> GetByIdAsync(int id, bool includeProductVariants = true);

        Task<bool> ExistsAsync(int id);

        Task<PagedResult<ProductVariant>> GetVariantsByProductIdAsync(int productId, ProductVariantsQueryParams parameters);

        Task UpdateAsync(Product product);

        Task DeleteAsync(Product product);

        Task<PagedResult<Product>> GetAsync(ProductQueryParams parameters);
        /// <param name="excludeProductId">When set, a product with this id is ignored (for updates).</param>
        Task<bool> IsNameExistedAsync(string name, int? excludeProductId = null);
        Task<PagedResult<ProductVariant>> GetVariantsAsync(ECommerce.Domain.QueryParameters.VariantQueryParams parameters);
        Task<ProductVariant?> GetVariantByIdAsync(int id);
        Task<ProductVariant?> GetVariantByIdForUpdateAsync(int id);
        Task UpdateVariantAsync(ProductVariant variant);
        Task UpdateProductStatusAsync(int id, ECommerce.Domain.Enums.ProductStatus status);
        Task<List<ProductVariant>> GetFeaturedDefaultVariantsAsync();

        Task<List<ProductVariant>> GetVariantsOfProductByIdAsync(int productId);
    }
}
