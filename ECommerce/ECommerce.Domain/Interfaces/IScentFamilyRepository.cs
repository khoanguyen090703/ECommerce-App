using ECommerce.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECommerce.Domain.Interfaces
{
    public interface IScentFamilyRepository
    {
        Task<List<ScentFamily>> GetAllAsync();
        Task<ScentFamily?> GetByIdAsync(int id);
    }
}
