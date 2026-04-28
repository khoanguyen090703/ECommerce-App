namespace ECommerce.SharedViewModels.DTOs.Auth;

public class SignInRequest {
    public string Email { get; set; } = default!;

    public string Password { get; set; } = default!;
}