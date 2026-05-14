using ECommerce.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.SharedViewModels.DTOs.Request
{
    public class UpdateVariantRequest
    {
        public VariantFormat Format { get; set; } = VariantFormat.FullBottle;

        public int Volumn { get; set; }

        public string Unit { get; set; } = "ml";

        public decimal Price { get; set; }

        public List<string> ImageUrls { get; set; } = new List<string>();
    }
}
