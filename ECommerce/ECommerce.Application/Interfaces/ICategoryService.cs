using ECommerce.Domain.Common;
using ECommerce.Domain.QueryParameters;
using ECommerce.SharedViewModels.DTOs.Request;
using ECommerce.SharedViewModels.DTOs.Response;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECommerce.Application.Interfaces
{
    public interface ICategoryService
    {
        Task CreateProductAsync(CreateCategoryRequest request);

        Task<List<CategoryResponse>> GetAllAsync();

        Task<PagedResult<CategoryResponse>> GetCategoriesAsync(CategoryQueryParams parameters);

        Task<CategoryResponse> GetCategoryByIdAsync(int id);

        Task UpdateCategoryByIdAsync(int id, UpdateCategoryRequest request);

        Task DeleteCategoryByIdAsync(int id);
    }
}
