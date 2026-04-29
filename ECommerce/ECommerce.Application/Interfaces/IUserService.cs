using ECommerce.SharedViewModels.DTOs.Response;

namespace ECommerce.Application.Interfaces;

public interface IUserService
{
    Task<UserProfileResponse> GetMyProfileAsync(CancellationToken cancellationToken = default);
}

