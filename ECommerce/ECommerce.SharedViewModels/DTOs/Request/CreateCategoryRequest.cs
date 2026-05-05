using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.SharedViewModels.DTOs.Request
{
    public class CreateCategoryRequest
    {
        public string Name { get; set; } = default!;

        public string Description { get; set; } = default!;

        public string? ImageUrl { get; set; }
    }
}
