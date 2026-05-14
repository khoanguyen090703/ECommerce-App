using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using ECommerce.SharedViewModels.DTOs.Auth;

namespace ECommerce.Web.Auth;

public static class AuthTokenRefresher
{
    public static bool AccessTokenNeedsRefresh(string? accessToken, TimeSpan refreshBeforeExpiry)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            return false;

        try
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
            var expiresAt = jwt.ValidTo;
            if (expiresAt == DateTime.MinValue)
                return false;

            return expiresAt <= DateTime.UtcNow.Add(refreshBeforeExpiry);
        }
        catch
        {
            return false;
        }
    }

    public static async Task<bool> TryRefreshAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        var authSessionManager = httpContext.RequestServices.GetRequiredService<IAuthSessionManager>();
        var accessToken = authSessionManager.GetAccessToken(httpContext);
        var refreshToken = authSessionManager.GetRefreshToken(httpContext);

        if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken))
            return false;

        var clientFactory = httpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
        var client = clientFactory.CreateClient("ApiAnonymous");

        using var response = await client.PostAsJsonAsync(
            "api/auth/refresh-token",
            new RefreshTokenRequest
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            },
            cancellationToken);

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: cancellationToken);
        if (!response.IsSuccessStatusCode
            || authResponse?.IsSuccess != true
            || string.IsNullOrWhiteSpace(authResponse.Token)
            || string.IsNullOrWhiteSpace(authResponse.RefreshToken))
        {
            return false;
        }

        authSessionManager.UpdateTokens(httpContext, authResponse.Token, authResponse.RefreshToken);
        return true;
    }
}
