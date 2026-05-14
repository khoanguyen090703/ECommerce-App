using ECommerce.Domain.Common;
using ECommerce.Domain.QueryParameters;
using ECommerce.SharedViewModels.DTOs.Request;
using ECommerce.SharedViewModels.DTOs.Response;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECommerce.Application.Interfaces
{
    public interface IVariantService
    {
        Task<PagedResult<VariantResponse>> GetVariantsAsync(VariantQueryParams parameters);
        Task<List<VariantResponse>> GetAllVariantsAsync();
        Task<ProductVariantDetailsResponse?> GetVariantDetailsByIdAsync(int variantId);
        Task UpdateVariantByIdAsync(int variantId, ECommerce.SharedViewModels.DTOs.Request.UpdateVariantRequest request);

        Task UpdateVariantStatusByIdAsync(int variantId, ECommerce.Domain.Enums.VariantStatus status);

        Task DeleteVariantByIdAsync(int variantId);
        Task<int> CreateVariantAsync(int productId, ECommerce.SharedViewModels.DTOs.Request.CreateVariantRequest request);
        Task<List<VariantResponse>> GetFeaturedVariantsAsync();
        Task SetFeaturedVariantsAsync(IEnumerable<int> variantIds);

        Task<PagedResult<VariantStockPanelResponse>> GetVariantsForStockRestockAsync(RestockVariantQueryParams parameters);

        Task<VariantStockPanelResponse?> GetVariantStockPanelByIdAsync(int variantId);

        Task AddStockToVariantsAsync(AddVariantStockBatchRequest request);
    }
}
