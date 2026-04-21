using ECommerce.Application.DTOs.Response;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.QueryParameters;
using ECommerce.Domain.Interfaces;
using ECommerce.Application.Mappings;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;

namespace ECommerce.Application.Services
{
    public class VariantService : IVariantService
    {
        private readonly IProductRepository _productRepository;
        private readonly IProductVariantRepository _productVariantRepository;
        private readonly FluentValidation.IValidator<ECommerce.Application.DTOs.Request.UpdateVariantRequest> _updateValidator;
        private readonly FluentValidation.IValidator<ECommerce.Application.DTOs.Request.CreateVariantRequest> _createValidator;

        public VariantService(
            IProductRepository productRepository,
            IProductVariantRepository productVariantRepository,
            FluentValidation.IValidator<ECommerce.Application.DTOs.Request.UpdateVariantRequest> updateValidator,
            FluentValidation.IValidator<ECommerce.Application.DTOs.Request.CreateVariantRequest> createValidator)
        {
            _productRepository = productRepository;
            _productVariantRepository = productVariantRepository;
            _updateValidator = updateValidator;
            _createValidator = createValidator;
        }

        public async Task<List<VariantResponse>> GetAllVariantsAsync()
        {
            var paged = await _productRepository.GetVariantsAsync(new VariantQueryParams { PageNumber = 1, PageSize = 1000 });
            return paged.Items.Select(v => v.ToVariantResponse()).ToList();
        }

        public async Task<ProductVariantDetailsResponse?> GetVariantDetailsByIdAsync(int variantId)
        {
            var variant = await _productVariantRepository.GetByIdAsync(variantId);
            if (variant == null)
                return null;

            return variant.ToDetailsResponse();
        }

        public async Task<int> CreateVariantAsync(int productId, ECommerce.Application.DTOs.Request.CreateVariantRequest request)
        {
            // Validate
            if (_createValidator != null)
            {
                await _createValidator.ValidateAndThrowAsync(request);
            }

            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null)
                throw new ECommerce.Application.Exceptions.NotFoundException($"Product with id {productId} not found.");

            // check duplicate format-volumn within same product
            var duplicate = ECommerce.Application.Helpers.VariantHelper.ExistsFormatVolumnDuplicate(product.ProductVariants, request.Format, request.Volumn, null);
            if (duplicate)
                throw new ECommerce.Application.Exceptions.ConflictException($"A variant with format {request.Format} and volumn {request.Volumn} already exists for this product.");

            var variant = new ECommerce.Domain.Entities.ProductVariant
            {
                Format = request.Format,
                Volumn = request.Volumn,
                Price = request.Price,
                StockQuantity = request.StockQuantity,
                Images = request.Images.Select(u => new ECommerce.Domain.Entities.VariantImage { Url = u }).ToList()
            };

            variant.Name = NameGenerators.GenerateVariantName(product.Name, variant.Format, variant.Volumn, variant.Unit);

            // add to product and save
            product.ProductVariants.Add(variant);
            await _productRepository.UpdateAsync(product);

            return variant.Id;
        }

        public async Task UpdateVariantByIdAsync(int variantId, ECommerce.Application.DTOs.Request.UpdateVariantRequest request)
        {
            // Validate request
            if (_updateValidator != null)
            {
                await _updateValidator.ValidateAndThrowAsync(request);
            }

            var variant = await _productVariantRepository.GetByIdForUpdateAsync(variantId);
            if (variant == null)
                throw new ECommerce.Application.Exceptions.NotFoundException($"Variant with id {variantId} not found.");

            var product = variant.Product;
            if (product == null)
                throw new ECommerce.Application.Exceptions.NotFoundException($"Parent product for variant id {variantId} not found.");

            // Check duplicate: other variants under same product
            var hasDuplicate = ECommerce.Application.Helpers.VariantHelper.ExistsFormatVolumnDuplicate(product.ProductVariants, request.Format, request.Volumn, variantId);
            if (hasDuplicate)
                throw new ECommerce.Application.Exceptions.ConflictException($"Another variant with format {request.Format} and volumn {request.Volumn} already exists for this product.");

            // Update fields
            variant.Format = request.Format;
            variant.Volumn = request.Volumn;
            variant.Unit = request.Unit;
            variant.Price = request.Price;
            variant.StockQuantity = request.StockQuantity;

            // Update name based on product name
            variant.Name = NameGenerators.GenerateVariantName(product.Name, variant.Format, variant.Volumn, variant.Unit);

            // Replace images
            variant.Images.Clear();
            foreach (var url in request.ImageUrls)
            {
                variant.Images.Add(new ECommerce.Domain.Entities.VariantImage { Url = url, ProductVariant = variant });
            }

            await _productVariantRepository.UpdateAsync(variant);
        }

        public async Task UpdateVariantStatusByIdAsync(int variantId, ECommerce.Domain.Enums.VariantStatus status)
        {
            var variant = await _productVariantRepository.GetByIdForUpdateAsync(variantId);
            if (variant == null)
                throw new ECommerce.Application.Exceptions.NotFoundException($"Variant with id {variantId} not found.");

            // 1. if same status -> conflict
            if (variant.Status == status)
                throw new ECommerce.Application.Exceptions.ConflictException($"Variant with id {variantId} is already {status}.");

            // 2. if current is Discontinued -> cannot update
            if (variant.Status == ECommerce.Domain.Enums.VariantStatus.Discontinued)
                throw new ECommerce.Application.Exceptions.ConflictException("Cannot update a discontinued variant.");

            // 3. If changing between Available and OutOfStock, validate stock quantity
            if ((variant.Status == ECommerce.Domain.Enums.VariantStatus.Available && status == ECommerce.Domain.Enums.VariantStatus.OutOfStock) ||
                (variant.Status == ECommerce.Domain.Enums.VariantStatus.OutOfStock && status == ECommerce.Domain.Enums.VariantStatus.Available) ||
                (status == ECommerce.Domain.Enums.VariantStatus.OutOfStock) || (status == ECommerce.Domain.Enums.VariantStatus.Available))
            {
                // When marking OutOfStock, ensure stock == 0
                if (status == ECommerce.Domain.Enums.VariantStatus.OutOfStock && variant.StockQuantity > 0)
                    throw new ECommerce.Application.Exceptions.ConflictException("Cannot mark variant OutOfStock while stock quantity is greater than 0.");

                // When marking Available, ensure stock > 0
                if (status == ECommerce.Domain.Enums.VariantStatus.Available && variant.StockQuantity <= 0)
                    throw new ECommerce.Application.Exceptions.ConflictException("Cannot mark variant Available while stock quantity is 0.");
            }

            variant.Status = status;
            await _productVariantRepository.UpdateAsync(variant);
        }

        public async Task<PagedResult<VariantResponse>> GetVariantsAsync(VariantQueryParams parameters)
        {
            var paged = await _productRepository.GetVariantsAsync(parameters);
            var mapped = paged.Items.Select(v => v.ToVariantResponse()).ToList();
            return new PagedResult<VariantResponse>(mapped, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }

        public async Task DeleteVariantByIdAsync(int variantId)
        {
            var variant = await _productVariantRepository.GetByIdAsync(variantId);
            if (variant == null)
                throw new ECommerce.Application.Exceptions.NotFoundException($"Variant with id {variantId} not found.");

            await _productVariantRepository.DeleteAsync(variant);
        }
    }
}
