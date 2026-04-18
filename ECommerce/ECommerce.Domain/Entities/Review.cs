using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Domain.Entities
{
    public class Review : BaseEntity<int>
    {
        public string? Comment { get; set; }

        public ReviewStatus Status { get; set; } = ReviewStatus.Approved;

        public bool IsPurchased { get; set; } = true;

        public int Rating { get; set; }

        public Product Product { get; set; } = default!;

        public Customer Customer { get; set; } = default!;

        public ICollection<ReviewImage> Images { get; set; } = new List<ReviewImage>();

        public ReviewResponse ReviewResponses { get; set; } = default!;
    }
}
