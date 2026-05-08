using System;

namespace ECommerce.SharedViewModels.DTOs.Response
{
    public class CustomerResponse
    {
        public Guid Id { get; set; }

        public string FullName { get; set; } = default!;

        public string? Address { get; set; }

        public string? AvatarUrl { get; set; }

        public string? Email { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }
    }
}
