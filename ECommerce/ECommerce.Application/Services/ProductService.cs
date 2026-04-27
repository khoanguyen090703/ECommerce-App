using ECommerce.Application.DTOs.Request;
using ECommerce.Application.DTOs.Response;
using ECommerce.Application.Exceptions;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using ECommerce.Domain.QueryParameters;
using ECommerce.Domain.Enums;
using ECommerce.Application.Mappings;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace ECommerce.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IBrandRepository _brandRepository;
        private readonly IScentFamilyRepository _scentFamilyRepository;

        private readonly IValidator<CreateProductRequest> _createProductRequestValidator;
        private readonly IValidator<UpdateProductRequest> _updateProductRequestValidator;

        public ProductService(
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            IBrandRepository brandRepository,
            IScentFamilyRepository scentFamilyRepository,
            IValidator<CreateProductRequest> createProductRequestValidator,
            IValidator<UpdateProductRequest> updateProductRequestValidator)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _brandRepository = brandRepository;
            _scentFamilyRepository = scentFamilyRepository;
            _createProductRequestValidator = createProductRequestValidator;
            _updateProductRequestValidator = updateProductRequestValidator;
        }

        public async Task AddAsync(CreateProductRequest request)
        {
            await _createProductRequestValidator.ValidateAndThrowAsync(request);

            // Check brand exists
            var brand = await _brandRepository.GetByIdAsync(request.BrandId);
            if (brand == null)
                throw new NotFoundException($"Brand with id {request.BrandId} not found.");

            // Check categories
            var categories = new List<Category>();
            foreach (var cid in request.CategoryIds)
            {
                var cat = await _categoryRepository.GetById(cid);
                if (cat == null)
                    throw new NotFoundException($"Category with id {cid} not found.");
                categories.Add(cat);
            }

            // Check scent families
            var scentFamilies = new List<ScentFamily>();
            foreach (var sfid in request.ScentFamilyIds)
            {
                var sf = await _scentFamilyRepository.GetByIdAsync(sfid);
                if (sf == null)
                    throw new NotFoundException($"Scent family with id {sfid} not found.");
                scentFamilies.Add(sf);
            }

            // Product images
            var productImages = request.Images.Select(i => new ProductImage { Url = i }).ToList();

            // Validate duplicate variants by (Format, Volumn)
            var duplicateGroups = request.Variants
                .GroupBy(x => new { x.Format, x.Volumn })
                .Where(g => g.Count() > 1)
                .ToList();

            if (duplicateGroups.Any())
            {
                var pairs = duplicateGroups
                    .Select(g => $"Format={g.Key.Format}, Volumn={g.Key.Volumn}")
                    .ToList();
                throw new ConflictException($"Duplicate variants detected for: {string.Join("; ", pairs)}. Each variant must have unique (Format, Volumn).");
            }

            // Variants: generate variant names from product name + format + volumn + unit
            var productVariants = new List<ProductVariant>();
            foreach (var v in request.Variants)
            {
                var variant = new ProductVariant
                {
                    Format = v.Format,
                    Volumn = v.Volumn,
                    Price = v.Price,
                    StockQuantity = v.StockQuantity,
                    Images = v.Images.Select(url => new VariantImage { Url = url }).ToList()
                };
                productVariants.Add(variant);
            }

            // Generate product name from Brand + Line + Concentration using helper
            var generatedProductName = NameGenerators.GenerateProductName(brand.Name, request.Line, request.Concentration);

            // Check if product name already exists
            if (await _productRepository.IsNameExistedAsync(generatedProductName))
                throw new ConflictException($"Product with name '{generatedProductName}' already exists.");

            // Set variant names using generated product name
            foreach (var pv in productVariants)
            {
                pv.Name = NameGenerators.GenerateVariantName(generatedProductName, pv.Format, pv.Volumn, pv.Unit);
            }

            var product = new Product
            {
                Name = generatedProductName,
                Description = request.Description,
                Brand = brand,
                Line = request.Line ?? string.Empty,
                ReleaseYear = request.ReleaseYear,
                Concentration = request.Concentration,
                Images = productImages,
                Categories = categories,
                ScentFamilies = scentFamilies,
                ProductVariants = productVariants
            };

            

            await _productRepository.AddAsync(product);
        }

        public async Task DeleteProductByIdAsync(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
                throw new NotFoundException($"Product with id {id} not found.");

            // Only allow deleting products in Draft status
            if (product.Status != ProductStatus.Draft)
                throw new ConflictException($"Product with id {id} cannot be deleted because its status is {product.Status}.");

            await _productRepository.DeleteAsync(product);
        }

        public async Task<List<ProductResponse4List>> GetAllAsync()
        {
            var products = await _productRepository.GetAllAsync();
            return products.Select(p => p.ToListResponse()).ToList();
        }

        public async Task<ProductDetailsResponse?> GetProductByIdAsync(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
                throw new NotFoundException($"Product with id {id} not found.");

            // Map using mappings
            return product.ToDetailsResponse();
        }

        public async Task<VariantDetails4Cus?> GetProductWithVariantsByVariantIdAsync(int variantId)
        {
            var variant = await _productRepository.GetVariantByIdAsync(variantId);
            if (variant == null)
                throw new NotFoundException($"Variant with id {variantId} not found.");

            var product = variant.Product;
            if (product == null)
                throw new NotFoundException($"Parent product for variant id {variantId} not found.");

            var variants = await _productRepository.GetVariantsOfProductByIdAsync(product.Id);
            var variantResponses = variants.Select(v => v.To4CusProdDetails()).ToList();

            var response = variant.ToDetails4Cus();
            response.ProductVariants = variantResponses;

            return response;
        }

        public async Task<PagedResult<ProductResponse4List>> GetProductsAsync(ProductQueryParams parameters)
        {
            var pagedProducts = await _productRepository.GetAsync(parameters);
            var response = pagedProducts.Items.Select(p => p.ToListResponse()).ToList();

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

            // Validate request
            await _updateProductRequestValidator.ValidateAndThrowAsync(request);

            // Validate brand
            var brand = await _brandRepository.GetByIdAsync(request.BrandId);
            if (brand == null)
                throw new NotFoundException($"Brand with id {request.BrandId} not found in update product.");

            // Validate categories
            var categories = new List<Category>();
            foreach (var cid in request.CategoryIds)
            {
                var cat = await _categoryRepository.GetById(cid);
                if (cat == null)
                    throw new NotFoundException($"Category with id {cid} not found in update product.");
                categories.Add(cat);
            }

            // Update allowed fields only (do not modify TotalReviews, AverageRating, Status, ProductVariants, Reviews)
            var previousName = product.Name;

            product.Description = request.Description;
            product.Brand = brand;
            product.Line = request.Line ?? product.Line;
            product.ReleaseYear = request.ReleaseYear;
            product.Concentration = request.Concentration;

            // If name changed, ensure uniqueness and update variant names accordingly
            if (!string.Equals(previousName, request.Name, StringComparison.OrdinalIgnoreCase))
            {
                if (await _productRepository.IsNameExistedAsync(request.Name))
                    throw new ConflictException($"Product with name '{request.Name}' already exists.");

                product.Name = request.Name;

                // Update variant names based on new product name
                foreach (var pv in product.ProductVariants)
                {
                    pv.Name = NameGenerators.GenerateVariantName(product.Name, pv.Format, pv.Volumn, pv.Unit);
                }
            }

            // Replace categories
            product.Categories.Clear();
            foreach (var c in categories)
            {
                product.Categories.Add(c);
            }

            // Image update is possible but currently disabled. To enable images replacement,
            // uncomment the block below.
            /*
            // Replace product images
            product.Images.Clear();
            var newImages = request.Images.Select(u => new ProductImage { Url = u }).ToList();
            foreach (var img in newImages)
            {
                product.Images.Add(img);
            }
            */

            await _productRepository.UpdateAsync(product);
        }

        public async Task UpdateProductStatusAsync(int id, ProductStatus status)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
                throw new NotFoundException($"Product with id {id} not found.");
            
            // Same status
            if (status == product.Status)
                throw new ConflictException($"Product with id {id} is already {status}.");

            // Update to Draft is not allowed
            if (status == ProductStatus.Draft)
                throw new ConflictException("Updating product to Draft status is not allowed.");

            // Update Archived product is not allowed 
            if (product.Status == ProductStatus.Archived)
                throw new ConflictException("Product with id {id} is archived, cannot be updated to any status.");

            // Update from Draft to Active, ensure product has variants and at least one variant has stock > 0
            if (status == ProductStatus.Active)
            {
                // When activating from Draft -> Active, ensure product has variants
                if (product.ProductVariants == null || !product.ProductVariants.Any())
                    throw new ConflictException("Product has no variants. Please add at least one variant before activating the product.");

                // Ensure at least one variant has stock > 0
                var anyWithStock = product.ProductVariants.Any(v => v.StockQuantity > 0);
                if (!anyWithStock)
                    throw new ConflictException("All variants have stock equal to 0. Please add stock to at least one variant before activating the product.");
            }

            // Update from Draft to Inactive is not allowed
            if (product.Status == ProductStatus.Draft && status == ProductStatus.Inactive)
            {
                throw new ConflictException("Update Draft product to Inactive is not allowed.");
            }

            

            // Rule 2: To Active, ensure product has at least one variant
            if (status == ProductStatus.Active)
            {
                if (product.ProductVariants == null || !product.ProductVariants.Any())
                    throw new ConflictException("Product has no variants. Add variants or remove the product completely.");
            }

            // All checks passed, update status
            product.Status = status;
            await _productRepository.UpdateAsync(product);
        }
    }
}
