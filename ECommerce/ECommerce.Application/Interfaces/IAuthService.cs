using ECommerce.SharedViewModels.DTOs.Auth;

namespace ECommerce.Application.Interfaces;

public interface IAuthService {
    Task<AuthResponse> SignUpAsync(SignUpRequest request);
    Task<AuthResponse> SignInAsync(SignInRequest request);
    Task<AuthResponse> ConfirmEmailAsync(string userId, string token);
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);
    Task<AuthResponse> LogoutAsync(string userId);

    Task<AuthResponse> ResendEmailConfirmationAsync(ResendEmailConfirmationRequest request);
}