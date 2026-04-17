using ECommerce.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Domain.Entities
{
    public class ProductImage : BaseEntity
    {
        //public int Id { get; set; }

        public string Url { get; set; }

        public Product Product { get; set; }
    }
}
