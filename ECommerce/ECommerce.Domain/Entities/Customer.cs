using ECommerce.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Domain.Entities
{
    public class Customer : BaseEntity<Guid>
    {
        public string FullName { get; set; } = default!;

        public string? Address { get; set; }

        public string? AvatarUrl { get; set; }

        public Guid IdentityId { get; set; }

        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
