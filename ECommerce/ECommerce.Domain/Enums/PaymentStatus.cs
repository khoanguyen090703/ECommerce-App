using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Domain.Enums
{
    public enum PaymentStatus
    {
        Pending,
        Completed,
        Failed,
        PartiallyRefunded,
        FullyRefunded
    }
}
