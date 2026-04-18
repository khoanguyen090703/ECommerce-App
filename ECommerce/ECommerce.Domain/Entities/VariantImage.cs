using ECommerce.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Domain.Entities
{
    public class VariantImage : BaseEntity<int>
    {
        public string Url { get; set; } = default!;

        public ProductVariant ProductVariant { get; set; } = default!;
    }
}
