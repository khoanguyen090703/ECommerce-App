using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Application.DTOs.Request
{
    public class UpdateProductRequest
    {
        public string Name { get; set; } = default!;

        public string Description { get; set; } = default!;

        public List<string> Images { get; set; } = new List<string>();

        public int BrandId { get; set; }

        public List<int> CategoryIds { get; set; } = new List<int>();

        public string? Line { get; set; }

        public int? ReleaseYear { get; set; }

        public ProductConcentration Concentration { get; set; }
    }
}
