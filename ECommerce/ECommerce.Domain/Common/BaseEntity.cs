using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Domain.Common
{
    public abstract class BaseEntity : IAuditableEntity
    {
        public int Id { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedDate { get; set; }
    }
}
