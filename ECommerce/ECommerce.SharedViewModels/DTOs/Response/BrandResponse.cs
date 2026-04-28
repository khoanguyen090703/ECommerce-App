using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Application.DTOs.Response
{
    public class BrandResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string? ImageUrl { get; set; }
    }
}
