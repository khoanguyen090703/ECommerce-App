using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Domain.Common
{
    public abstract class BaseEntity<TId> : IAuditableEntity
    {
        public TId Id { get; set; } = default!;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedDate { get; set; }
    }
}
