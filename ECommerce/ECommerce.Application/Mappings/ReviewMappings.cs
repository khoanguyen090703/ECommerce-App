using ECommerce.Application.DTOs.Response;
using ECommerce.Domain.Entities;
using System.Linq;

namespace ECommerce.Application.Mappings
{
    public static class ReviewMappings
    {
        public static ReviewDetailsResponse ToDetailsResponse(this Review r)
        {
            return new ReviewDetailsResponse
            {
                Id = r.Id,
                Comment = r.Comment,
                Status = r.Status.ToString(),
                IsPurchased = r.IsPurchased,
                Rating = r.Rating,
                CustomerId = r.Customer.Id.ToString(),
                CustomerName = r.Customer?.FullName ?? string.Empty,
                ImageUrls = r.Images.Select(i => i.Url).ToList(),
                ReviewResponse = r.ReviewResponses.ToDto(),
                CreatedDate = r.CreatedDate
            };
        }
    }
}
