using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.SharedViewModels.DTOs.Request
{
    public class UpdateCategoryRequest
    {
        public string Name { get; set; } = default!;
        public string Description { get; set; } = default!;
    }
}
