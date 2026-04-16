using ECommerce.Application.DTOs.Request;
using ECommerce.Application.DTOs.Response;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Application.Interfaces
{
    public interface ICategoryService
    {
        Task CreateProductAsync(CreateCategoryRequest request);

        Task<List<CategoryResponse>> GetAllAsync();

        Task UpdateCategoryByIdAsync(int id, UpdateCategoryRequest request);

        Task DeleteCategoryByIdAsync(int id);
    }
}
