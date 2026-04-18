using ECommerce.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Domain.Entities
{
    public class ReviewResponse : BaseEntity<int>
    {
        public string Comment { get; set; } = default!;

        public int ReviewId { get; set; }
        public Review Review { get; set; } = default!;

        //public Staff Staff { get; set; } = default!;
    }
}
