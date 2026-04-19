using ECommerce.Application.DTOs.Response;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECommerce.Application.Interfaces
{
    public interface IScentFamilyService
    {
        Task<List<ScentFamilyResponse>> GetAllScentFamiliesAsync();
    }
}
