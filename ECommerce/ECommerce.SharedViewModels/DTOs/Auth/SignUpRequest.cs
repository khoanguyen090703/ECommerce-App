namespace ECommerce.SharedViewModels.DTOs.Auth;

public class SignUpRequest {
    public string Email { get; set; } = default!;

    public string Password { get; set; } = default!;

    public string FullName { get; set; } = default!;
}