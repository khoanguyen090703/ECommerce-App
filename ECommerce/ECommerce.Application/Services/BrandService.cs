using ECommerce.Application.DTOs.Response;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Interfaces;
using ECommerce.Application.Mappings;
using ECommerce.Domain.Common;
using ECommerce.Domain.QueryParameters;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ECommerce.Application.Services
{
    public class BrandService : IBrandService
    {
        private readonly IBrandRepository _brandRepository;

        public BrandService(IBrandRepository brandRepository)
        {
            _brandRepository = brandRepository;
        }

        public async Task<List<BrandResponse>> GetAllBrandsAsync()
        {
            var brands = await _brandRepository.GetAllAsync();
            var responses = brands.Select(b => b.ToResponse()).ToList();
            return responses;
        }

        public async Task<PagedResult<BrandResponse>> GetBrandsAsync(BrandQueryParams parameters)
        {
            var paged = await _brandRepository.GetAsync(parameters);
            var mapped = paged.Items.Select(b => b.ToResponse()).ToList();
            return new PagedResult<BrandResponse>(mapped, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }
    }
}
