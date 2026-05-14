namespace ECommerce.Web.Auth;

public sealed class TokenResponse
{
    public string AccessToken { get; init; } = string.Empty;

    public string RefreshToken { get; init; } = string.Empty;

    public int ExpiresIn { get; init; }
}
