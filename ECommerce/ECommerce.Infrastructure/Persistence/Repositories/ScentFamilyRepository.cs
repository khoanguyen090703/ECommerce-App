using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECommerce.Infrastructure.Persistence.Repositories
{
    public class ScentFamilyRepository : IScentFamilyRepository
    {
        private readonly AppDbContext _context;

        public ScentFamilyRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<ScentFamily>> GetAllAsync()
        {
            var items = await _context.ScentFamilies.AsNoTracking().ToListAsync();
            return items;
        }
    }
}
