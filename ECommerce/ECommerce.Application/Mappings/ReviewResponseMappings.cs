using ECommerce.Application.DTOs.Response;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Mappings
{
    public static class ReviewResponseMappings
    {
        public static ReviewResponseDetailsDto ToDto(this ReviewResponse rr)
        {
            return new ReviewResponseDetailsDto
            {
                Id = rr.Id,
                Comment = rr.Comment
            };
        }
    }
}
