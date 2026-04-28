namespace ECommerce.SharedViewModels.DTOs.Auth;

public class RefreshTokenRequest {
    public string RefreshToken { get; set; } = default!;
    public string AccessToken { get; set; } = default!;
}