using ECommerce.Domain.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Infrastructure.Persistence.Extensions
{
    public static class QueryableExtensions
    {
        public static async Task<PagedResult<T>> ToPagedListAsync<T>(
        this IQueryable<T> source,
        int pageNumber,
        int pageSize) where T : class
        {
            // 1. Đếm tổng số bản ghi trước khi phân trang
            var count = await source.CountAsync();

            // 2. Thực hiện lấy dữ liệu của trang hiện tại
            // Skip: bỏ qua các bản ghi của các trang trước
            // Take: lấy đúng số lượng bản ghi của trang hiện tại
            var items = await source
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // 3. Trả về đối tượng PagedResult đã đóng gói dữ liệu và metadata
            return new PagedResult<T>(items, count, pageNumber, pageSize);
        }
    }
}
