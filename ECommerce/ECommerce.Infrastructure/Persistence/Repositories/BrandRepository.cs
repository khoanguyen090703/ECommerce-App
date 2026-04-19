using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Domain.Common;
using ECommerce.Domain.QueryParameters;
using ECommerce.Infrastructure.Persistence.Extensions;
using System.Linq;

namespace ECommerce.Infrastructure.Persistence.Repositories
{
    public class BrandRepository : IBrandRepository
    {
        private readonly AppDbContext _context;

        public BrandRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Brand>> GetAllAsync()
        {
            var brands = await _context.Brands.AsNoTracking().ToListAsync();
            return brands;
        }

        public async Task<PagedResult<Brand>> GetAsync(ECommerce.Domain.QueryParameters.BrandQueryParams parameters)
        {
            var query = _context.Brands.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
            {
                query = query.Where(b => b.Name.Contains(parameters.SearchTerm));
            }

            query = parameters.SortBy switch
            {
                "name_desc" => query.OrderByDescending(b => b.Name),
                _ => query.OrderBy(b => b.Id)
            };

            return await query.ToPagedListAsync(parameters.PageNumber, parameters.PageSize);
        }

        public async Task<Brand?> GetByIdAsync(int id)
        {
            return await _context.Brands.SingleOrDefaultAsync(b => b.Id == id);
        }
    }
}
