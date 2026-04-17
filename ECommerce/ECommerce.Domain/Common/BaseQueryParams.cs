using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Domain.Common
{
    public abstract class BaseQueryParams
    {
        // Tìm kiếm chung
        public string? SearchTerm { get; set; }

        // Phân trang
        public int PageNumber { get; set; } = 1;

        private int _pageSize = 10;
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > 50 ? 50 : value; // Giới hạn tối đa 50 bản ghi
        }

        // Sắp xếp
        public string? SortBy { get; set; }
        public bool IsDescending { get; set; } = false;
    }
}
