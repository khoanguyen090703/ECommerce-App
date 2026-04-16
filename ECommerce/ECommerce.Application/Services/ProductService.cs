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
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;

        private readonly ICategoryRepository _categoryRepository;

        private readonly IValidator<CreateProductRequest> _createProductRequestValidator;

        public ProductService(
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            IValidator<CreateProductRequest> createProductRequestValidator
            )
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _createProductRequestValidator = createProductRequestValidator;
        }

        public async Task AddAsync(CreateProductRequest request)
        {
            //var validationResult = await _productValidator.ValidateAsync(request);
            //if (!validationResult.IsValid)
            //{
            //    var errorMessages = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            //    throw new FluentValidation.ValidationException(errorMessages);
            //}

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
                Price = request.Price,
                Category = category,
                Images = productImages
            };

            await _productRepository.AddAsync(product);
        }

        public async Task<List<ProductResponse4List>> GetAllAsync()
        {
            var products = await _productRepository.GetAllAsync();
            return products.Select(p => new ProductResponse4List
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                Category = p.Category.Name ?? "",
                Image = p.Images.FirstOrDefault()?.Url ?? "",
                CreatedDate = p.CreatedDate,
                UpdatedDate = p.UpdatedDate

            }).ToList();
        }
    }
}
