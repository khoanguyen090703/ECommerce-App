using ECommerce.Domain.Enums;
using System;

namespace ECommerce.Application.DTOs.Response
{
    public class VariantResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public decimal Price { get; set; }
        public string Status { get; set; } = default!;
        public string ImageUrl { get; set; } = string.Empty;
    }
}
