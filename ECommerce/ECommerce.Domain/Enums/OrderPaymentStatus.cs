using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Domain.Enums
{
    public enum OrderPaymentStatus
    {
        Unpaid,
        Paid,
        PartiallyRefunded,
        FullyRefunded,
        Failed
    }
}
