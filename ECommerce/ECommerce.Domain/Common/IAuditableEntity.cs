using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Domain.Common
{
    public interface IAuditableEntity
    {
        public DateTime CreatedDate { get; set; } 

        public DateTime? UpdatedDate { get; set; }
    }
}
