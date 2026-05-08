using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.SharedViewModels.DTOs.Response
{
    public class CartResponse
    {
        public int TotalItems { get; set; }

        public List<Item4CartResponse> CartItems { get; set; } = new List<Item4CartResponse>();

        public decimal Total { get; set; }
    }
}
