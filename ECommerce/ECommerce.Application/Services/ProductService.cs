using ECommerce.Application.DTOs.Request;
using ECommerce.Application.DTOs.Response;
using ECommerce.Application.Exceptions;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using ECommerce.Domain.QueryParameters;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;

        private readonly ICategoryRepository _categoryRepository;

        private readonly IValidator<CreateProductRequest> _createProductRequestValidator;

        private readonly IValidator<UpdateProductRequest> _updateProductRequestValidator;

        public ProductService(
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            IValidator<CreateProductRequest> createProductRequestValidator,
            IValidator<UpdateProductRequest> updateProductRequestValidator)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _createProductRequestValidator = createProductRequestValidator;
            _updateProductRequestValidator = updateProductRequestValidator;
        }

        public async Task AddAsync(CreateProductRequest request)
        {
            await _createProductRequestValidator.ValidateAndThrowAsync( request );

            var category = await _categoryRepository.GetById(request.CategoryId);
            if (category == null)
            {
                throw new NotFoundException($"Category with id {request.CategoryId} not found.");
            }

            // Create ProductImages
            var images = request.Images;
            var productImages = images.Select(i => new ProductImage
                    {
                        Url = i
                    }
                ).ToList();

            // Create product
            var product = new Product
            {
                Name = request.Name,
                Description = request.Description,
                Images = productImages
            };

            await _productRepository.AddAsync(product);
        }

        public async Task DeleteProductByIdAsync(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
                throw new NotFoundException($"Product with id {id} not found.");
            await _productRepository.DeleteAsync(product);
        }

        public async Task<List<ProductResponse4List>> GetAllAsync()
        {
            var products = await _productRepository.GetAllAsync();
            return products.Select(p => new ProductResponse4List
            {
                Id = p.Id,
                Name = p.Name,
                Image = p.Images.FirstOrDefault()?.Url ?? "",
                CreatedDate = p.CreatedDate,
                UpdatedDate = p.UpdatedDate

            }).ToList();
        }

        public async Task<ProductDetailsResponse?> GetProductByIdAsync(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
                throw new NotFoundException($"Product with id {id} not found.");

            var response = new ProductDetailsResponse
            {
                Id = id,
                Name = product.Name,
                Description= product.Description,
                Images = product.Images.Select(i => i.Url).ToList(),
                CreatedDate= product.CreatedDate,
                UpdatedDate = product.UpdatedDate
            };
            return response;
        }

        public async Task<PagedResult<ProductResponse4List>> GetProductsAsync(ProductQueryParams parameters)
        {
            var pagedProducts = await _productRepository.GetAsync(parameters);
            var response = pagedProducts.Items.Select(p => new ProductResponse4List
            {
                Id = p.Id,
                Name = p.Name,
                Image = p.Images.FirstOrDefault()?.Url ?? "",
                CreatedDate = p.CreatedDate,
                UpdatedDate = p.UpdatedDate
            }).ToList();

            return new PagedResult<ProductResponse4List>(
                response,
                pagedProducts.TotalCount,
                pagedProducts.PageNumber,
                pagedProducts.PageSize);
        }

        public async Task UpdateProductByIdAsync(int id, UpdateProductRequest request)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
                throw new NotFoundException($"Product with id {id} not found.");

            await _updateProductRequestValidator.ValidateAndThrowAsync(request);

            var category = await _categoryRepository.GetById(request.CategoryId);
            if (category == null)
                throw new NotFoundException($"Category with id {request.CategoryId} not found in update product.");

            product.Name = request.Name;
            product.Description = request.Description;

            await _productRepository.UpdateAsync(product);
        }
    }
}
