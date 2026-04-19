using ECommerce.Application.DTOs.Response;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Application.Mappings;

namespace ECommerce.Application.Services
{
    public class ScentFamilyService : IScentFamilyService
    {
        private readonly IScentFamilyRepository _scentFamilyRepository;

        public ScentFamilyService(IScentFamilyRepository scentFamilyRepository)
        {
            _scentFamilyRepository = scentFamilyRepository;
        }

        public async Task<List<ScentFamilyResponse>> GetAllScentFamiliesAsync()
        {
            var items = await _scentFamilyRepository.GetAllAsync();
            var responses = items.Select(i => i.ToResponse()).ToList();
            return responses;
        }
    }
}
