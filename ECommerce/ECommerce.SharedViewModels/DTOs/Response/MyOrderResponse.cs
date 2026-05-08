using ECommerce.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.SharedViewModels.DTOs.Response
{
    public class MyOrderResponse
    {
        public int Id { get; set; }

        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = default!;

        public List<Item4MyOrderResponse> OrderItems { get; set; } = new List<Item4MyOrderResponse>();
    }
}
