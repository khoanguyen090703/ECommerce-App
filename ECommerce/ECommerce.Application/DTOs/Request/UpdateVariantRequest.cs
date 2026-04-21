using ECommerce.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Application.DTOs.Request
{
    public class UpdateVariantRequest
    {
        public VariantFormat Format { get; set; } = VariantFormat.FullBottle;

        public int Volumn { get; set; }

        public string Unit { get; set; } = "ml";

        public decimal Price { get; set; }

        public int StockQuantity { get; set; } = 1;

        public List<string> ImageUrls { get; set; } = new List<string>();
    }
}
