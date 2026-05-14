namespace ECommerce.Web.Auth;

public interface IAuthService
{
    Task<TokenResponse?> LoginAsync(string email, string password, CancellationToken cancellationToken = default);

    Task<TokenResponse?> RefreshAsync(string accessToken, string refreshToken, CancellationToken cancellationToken = default);

    Task RevokeAsync(CancellationToken cancellationToken = default);
}
