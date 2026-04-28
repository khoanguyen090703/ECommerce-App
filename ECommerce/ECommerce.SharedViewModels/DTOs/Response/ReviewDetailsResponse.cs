using System;
using System.Collections.Generic;

namespace ECommerce.Application.DTOs.Response
{
    public class ReviewDetailsResponse
    {
        public int Id { get; set; }
        public string? Comment { get; set; }
        public string Status { get; set; } = default!;
        public bool IsPurchased { get; set; }
        public int Rating { get; set; }
        public string CustomerId { get; set; } = default!;
        public string CustomerName { get; set; } = default!;
        public List<string> ImageUrls { get; set; } = new List<string>();
        public ReviewResponseDetailsDto? ReviewResponse { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
