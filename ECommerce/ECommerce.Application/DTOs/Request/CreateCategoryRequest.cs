using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Application.DTOs.Request
{
    public class CreateCategoryRequest
    {
        public string Name { get; set; } = default!;

        public string Description { get; set; } = default!;
    }
}
