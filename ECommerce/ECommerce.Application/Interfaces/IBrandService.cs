using ECommerce.Application.DTOs.Response;
using System.Collections.Generic;
using ECommerce.Domain.Common;
using System.Threading.Tasks;

namespace ECommerce.Application.Interfaces
{
    public interface IBrandService
    {
        Task<List<BrandResponse>> GetAllBrandsAsync();
        Task<PagedResult<BrandResponse>> GetBrandsAsync(ECommerce.Domain.QueryParameters.BrandQueryParams parameters);
    }
}
