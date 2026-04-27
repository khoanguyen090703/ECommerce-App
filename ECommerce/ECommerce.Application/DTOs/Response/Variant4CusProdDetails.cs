using ECommerce.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Application.DTOs.Response
{
    public class Variant4CusProdDetails
    {
        public int Id { get; set; }
        public string Format { get; set; } = default!;
        public string Volumn { get; set; } = default!;
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = default!;
    }
}
