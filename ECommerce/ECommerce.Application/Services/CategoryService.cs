using ECommerce.Application.DTOs.Request;
using ECommerce.Application.DTOs.Response;
using ECommerce.Application.Exceptions;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Application.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;

        private readonly IValidator<CreateCategoryRequest> _createCategoryRequestValidator;

        private readonly IValidator<UpdateCategoryRequest> _updateCategoryRequestValidator;

        public CategoryService(ICategoryRepository categoryRepository, IValidator<CreateCategoryRequest> createCategoryRequestValidator, IValidator<UpdateCategoryRequest> updateCategoryRequestValidator)
        {
            _categoryRepository = categoryRepository;
            _createCategoryRequestValidator = createCategoryRequestValidator;
            _updateCategoryRequestValidator = updateCategoryRequestValidator;
        }

        public async Task CreateProductAsync(CreateCategoryRequest request)
        {
            await _createCategoryRequestValidator.ValidateAndThrowAsync(request);

            var category = new Category
            {
                Name = request.Name,
                Description = request.Description
            };
            await _categoryRepository.AddAsync(category);
        }

        public async Task DeleteCategoryByIdAsync(int id)
        {
            var category = await _categoryRepository.GetById(id);
            if (category == null)
                throw new NotFoundException($"Category with id {id} not found.");

            //if (await _categoryRepository.HasProductsAsync(id))
            //    throw new ConflictException($"Category with id {id} can not be deleted because it has products.");

            await _categoryRepository.DeleteAsync(category);
        }

        public async Task<List<CategoryResponse>> GetAllAsync()
        {
            var categories = await _categoryRepository.GetAllAsync();
            var categorieResponses = categories.Select(c => new CategoryResponse
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description
            }).ToList();
            return categorieResponses;
        }

        public async Task UpdateCategoryByIdAsync(int id, UpdateCategoryRequest request)
        {
            var category = await _categoryRepository.GetById(id);
            if(category == null)
                throw new NotFoundException($"Category with id {id} not found.");

            await _updateCategoryRequestValidator.ValidateAndThrowAsync(request);

            if (await _categoryRepository.IsNameExistedExceptAsync(request.Name, id))
                throw new ConflictException($"{request.Name} is existed with another category.");

            category.Name = request.Name;
            category.Description = request.Description;
            await _categoryRepository.UpdateAsync(category);
        }
    }
}
