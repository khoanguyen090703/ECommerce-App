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

        public async Task SetFeaturedVariantsAsync(IEnumerable<int> variantIds)
        {
            var ids = variantIds?.ToList() ?? new List<int>();

            // Load requested variants with product info
            var variants = await _productVariantRepository.GetByIdsForUpdateAsync(ids);

            // Validate existence
            var foundIds = variants.Select(v => v.Id).ToHashSet();
            var notFound = ids.Where(i => !foundIds.Contains(i)).ToList();
            if (notFound.Any())
                throw new ECommerce.Application.Exceptions.NotFoundException($"Variants not found: {string.Join(",", notFound)}");

            // Validate statuses: variant must be Available and product must be Active
            var invalids = variants.Where(v => v.Status != ECommerce.Domain.Enums.VariantStatus.Available || v.Product == null || v.Product.Status != ECommerce.Domain.Enums.ProductStatus.Active).ToList();
            if (invalids.Any())
            {
                var messages = invalids.Select(v => $"Variant {v.Id} status {v.Status} or product status {v.Product?.Status.ToString() ?? "<null>"} invalid").ToList();
                throw new ECommerce.Application.Exceptions.ConflictException(string.Join("; ", messages));
            }

            var currentFeatured = await _productRepository.GetFeaturedDefaultVariantsAsync();
            if (!currentFeatured.Any())
            {
                // No existing featured variants, simply set requested ones as featured
                foreach (var v in variants)
                {
                    v.IsDefault = true;
                    v.Product.IsFeatured = true;
                }

            }
            else
            {
                // There are existing featured variants, need to update accordingly
                var currentFeaturedIds = currentFeatured.Select(v => v.Id).ToHashSet();
                // Variants to be set as featured: those in requested but not currently featured
                var toBeFeatured = variants.Where(v => !currentFeaturedIds.Contains(v.Id)).ToList();
                foreach (var v in toBeFeatured)
                {
                    v.IsDefault = true;
                    v.Product.IsFeatured = true;
                }
                // Variants to be unfeatured: those currently featured but not in requested
                var toBeUnfeatured = currentFeatured.Where(v => !ids.Contains(v.Id)).ToList();
                foreach (var v in toBeUnfeatured)
                {
                    v.IsDefault = false;
                    // Check if product still has any default variant after unfeaturing this one
                    var hasOtherDefault = currentFeatured.Any(cv => cv.Id != v.Id && cv.IsDefault) || toBeFeatured.Any(tf => tf.Product.Id == v.Product.Id);
                    if (!hasOtherDefault)
                    {
                        v.Product.IsFeatured = false; 
                    }
                }
            }
            // For each product, find existing default variants and compare with requested
            var productGroups = variants.GroupBy(v => v.Product.Id);

            var toUpdate = new List<ECommerce.Domain.Entities.ProductVariant>();

            

            // Persist changes: update changed variants and products
            if (toUpdate.Any())
            {
                await _productVariantRepository.UpdateRangeAsync(toUpdate);
            }

            // Also update products IsFeatured where necessary
            var affectedProducts = variants.Select(v => v.Product).Distinct().ToList();
            foreach (var p in affectedProducts)
            {
                await _productRepository.UpdateAsync(p);
            }
        }

        public async Task<List<VariantResponse>> GetFeaturedVariantsAsync()
        {
            var variants = await _productRepository.GetFeaturedDefaultVariantsAsync();
            return variants.Select(v => v.ToVariantResponse()).ToList(); // Formatting change
        }

        public async Task DeleteVariantByIdAsync(int variantId)
        {
            var variant = await _productVariantRepository.GetByIdAsync(variantId);
            if (variant == null)
                throw new ECommerce.Application.Exceptions.NotFoundException($"Variant with id {variantId} not found.");

            if(variant.Product.Status != Domain.Enums.ProductStatus.Draft)
                throw new ECommerce.Application.Exceptions.ConflictException($"Variant with id {variantId} cannot be deleted because its product is not in draft status.");

            await _productVariantRepository.DeleteAsync(variant);
        }
    }
}
