using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Identity;
using ECommerce.SharedViewModels.DTOs.Response;

namespace ECommerce.Infrastructure.Mappings;

/// <summary>
/// Map AppUser (Identity) sang UserProfileResponse. Customer (domain) tùy chọn để bổ sung FullName, AvatarUrl.
/// </summary>
public static class UserProfileMappings
{
    public static UserProfileResponse ToUserProfileResponse(this AppUser user, Customer? customer = null)
    {
        return new UserProfileResponse
        {
            Id = user.Id.ToString(),
            Email = user.Email ?? string.Empty,
            FullName = customer?.FullName ?? user.UserName ?? user.Email ?? string.Empty,
            AvatarUrl = customer?.AvatarUrl
        };
    }
}
