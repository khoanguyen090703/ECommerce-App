using ECommerce.Application.DTOs.Request;
using ECommerce.Application.DTOs.Response;
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

        Task UpdateProductByIdAsync(int id, UpdateProductRequest request);

        Task DeleteProductByIdAsync(int id);

        Task<PagedResult<ProductResponse4List>> GetProductsAsync(ProductQueryParams parameters);
    }
}
