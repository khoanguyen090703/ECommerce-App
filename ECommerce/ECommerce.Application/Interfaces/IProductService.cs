using ECommerce.SharedViewModels.DTOs.Request;
using ECommerce.SharedViewModels.DTOs.Response;
using ECommerce.Domain.Common;
using ECommerce.Domain.QueryParameters;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Application.Interfaces
{
    public interface IProductService
    {
        Task<List<ProductResponse4List>> GetAllAsync();

        Task AddAsync(CreateProductRequest request);

        Task<ProductDetailsResponse?> GetProductByIdAsync(int id);

        Task<VariantDetails4Cus?> GetProductWithVariantsByVariantIdAsync(int variantId);

        Task UpdateProductByIdAsync(int id, UpdateProductRequest request);

        Task UpdateProductStatusAsync(int id, ECommerce.Domain.Enums.ProductStatus status);

        Task DeleteProductByIdAsync(int id);

        Task<PagedResult<ProductResponse4List>> GetProductsAsync(ProductQueryParams parameters);
    }
}
