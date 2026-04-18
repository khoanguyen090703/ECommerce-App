using ECommerce.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Domain.Entities
{
    public class ReviewImage : BaseEntity<int>
    {
        public string Url { get; set; } = default!;

        public Review Review { get; set; } = default!;
    }
}
