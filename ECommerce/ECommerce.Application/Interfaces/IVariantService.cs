using ECommerce.Domain.Common;
using ECommerce.Domain.QueryParameters;
using ECommerce.Application.DTOs.Response;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECommerce.Application.Interfaces
{
    public interface IVariantService
    {
        Task<PagedResult<VariantResponse>> GetVariantsAsync(VariantQueryParams parameters);
        Task<List<VariantResponse>> GetAllVariantsAsync();
        Task<ProductVariantDetailsResponse?> GetVariantDetailsByIdAsync(int variantId);
        Task UpdateVariantByIdAsync(int variantId, ECommerce.Application.DTOs.Request.UpdateVariantRequest request);

        Task UpdateVariantStatusByIdAsync(int variantId, ECommerce.Domain.Enums.VariantStatus status);

        Task DeleteVariantByIdAsync(int variantId);
    }
}
