using ECommerce.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Application.DTOs.Request
{
    public class UpdateProductRequest
    {
        public string? Name { get; set; }

        public string? Description { get; set; }

        public decimal Price { get; set; }

        //public List<ProductImage> Images { get; set; } = new List<ProductImage>();

        public int CategoryId { get; set; }
    }
}
