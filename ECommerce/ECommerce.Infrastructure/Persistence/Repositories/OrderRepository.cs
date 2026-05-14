using ECommerce.Domain.Entities;
using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Interfaces;
using ECommerce.Domain.QueryParameters;
using ECommerce.Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Infrastructure.Persistence.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly AppDbContext _context;

        public OrderRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Order order)
        {
            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();
        }

        public async Task<Order?> GetByIdAsync(int id)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.ProductVariant).ThenInclude(pv => pv.Images)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task UpdateAsync(Order order)
        {
            if (_context.Entry(order).State == EntityState.Detached)
                _context.Orders.Update(order);
            await _context.SaveChangesAsync();
        }

        public async Task<PagedResult<Order>> GetByCustomerIdAsync(Guid customerId, OrderQueryParams parameters, CancellationToken cancellationToken = default)
        {
            var query = BuildOrderQuery(parameters)
                .Where(o => o.Customer.Id == customerId);

            return await query.ToPagedListAsync(parameters.PageNumber, parameters.PageSize);
        }

        public async Task<PagedResult<Order>> GetAsync(OrderQueryParams parameters, CancellationToken cancellationToken = default)
        {
            var query = BuildOrderQuery(parameters);
            return await query.ToPagedListAsync(parameters.PageNumber, parameters.PageSize);
        }

        public async Task<Order?> GetByIdForUpdateAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.ProductVariant)
                .ThenInclude(pv => pv.Product)
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        }

        public async Task<Order?> GetDetailsByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.ProductVariant).ThenInclude(pv => pv.Images)
                .Include(o => o.Customer)
                .Include(o => o.Payments)
                .ThenInclude(p => p.PaymentMethod)
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        }

        private IQueryable<Order> BuildOrderQuery(OrderQueryParams parameters)
        {
            var query = _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.Payments).ThenInclude(p => p.PaymentMethod)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.ProductVariant).ThenInclude(pv => pv.Images)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
            {
                var term = parameters.SearchTerm.Trim();
                query = query.Where(o =>
                    o.RecipientName.Contains(term)
                    || o.PhoneNumber.Contains(term)
                    || o.ShippingAddress.Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(parameters.Status)
                && Enum.TryParse<OrderStatus>(parameters.Status.Trim(), true, out var parsedStatus))
            {
                query = query.Where(o => o.Status == parsedStatus);
            }

            query = parameters.SortBy?.Trim().ToLowerInvariant() switch
            {
                "id" => query.OrderBy(o => o.Id),
                "id_desc" => query.OrderByDescending(o => o.Id),
                "totalamount" => query.OrderBy(o => o.TotalAmount),
                "totalamount_desc" => query.OrderByDescending(o => o.TotalAmount),
                "status" => query.OrderBy(o => o.Status),
                "status_desc" => query.OrderByDescending(o => o.Status),
                "orderdate" => query.OrderBy(o => o.OrderDate),
                "orderdate_desc" => query.OrderByDescending(o => o.OrderDate),
                "completeddate" => query.OrderBy(o => o.CompletedDate),
                "completeddate_desc" => query.OrderByDescending(o => o.CompletedDate),
                _ => query.OrderByDescending(o => o.OrderDate)
            };

            return query;
        }
    }
}
