using System.Collections.Generic;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.DTOs.Request
{
    public class CreateVariantRequest
    {
        public VariantFormat Format { get; set; }

        public int Volumn { get; set; }

        public decimal Price { get; set; }

        public int StockQuantity { get; set; } = 1;

        public List<string> Images { get; set; } = new List<string>();
    }
}
