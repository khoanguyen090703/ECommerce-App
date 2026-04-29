using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Exceptions;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Interfaces;
using ECommerce.Infrastructure.Identity;
using ECommerce.Infrastructure.Mappings;
using ECommerce.SharedViewModels.DTOs.Response;
using Microsoft.AspNetCore.Identity;

namespace ECommerce.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICustomerRepository _customerRepository;

    public UserService(
        UserManager<AppUser> userManager,
        ICurrentUserService currentUserService,
        ICustomerRepository customerRepository)
    {
        _userManager = userManager;
        _currentUserService = currentUserService;
        _customerRepository = customerRepository;
    }

    public async Task<UserProfileResponse> GetMyProfileAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            throw new UnauthorizedAccessException("Unable to resolve the current user from the token.");

        var user = await _userManager.FindByIdAsync(userId.Value.ToString());
        if (user == null)
            throw new NotFoundException("User account was not found.");

        var customer = await _customerRepository.GetByIdentityIdAsync(userId.Value, cancellationToken);
        return user.ToUserProfileResponse(customer);
    }
}
