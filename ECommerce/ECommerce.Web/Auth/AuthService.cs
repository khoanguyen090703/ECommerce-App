using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using ECommerce.SharedViewModels.DTOs.Auth;
using Microsoft.AspNetCore.Http;

namespace ECommerce.Web.Auth;

public sealed class AuthService : IAuthService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<TokenResponse?> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient(AuthConstants.ApiAnonymousClientName);
        using var response = await client.PostAsJsonAsync(
            "api/auth/signin",
            new SignInRequest { Email = email, Password = password },
            cancellationToken);

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: cancellationToken);
        if (!response.IsSuccessStatusCode || authResponse?.IsSuccess != true)
            return null;

        return MapTokenResponse(authResponse);
    }

    public async Task<TokenResponse?> RefreshAsync(string accessToken, string refreshToken, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient(AuthConstants.ApiAnonymousClientName);
        using var response = await client.PostAsJsonAsync(
            "api/auth/refresh-token",
            new RefreshTokenRequest
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            },
            cancellationToken);

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: cancellationToken);
        if (response.StatusCode != HttpStatusCode.OK || authResponse?.IsSuccess != true)
            return null;

        return MapTokenResponse(authResponse);
    }

    public async Task RevokeAsync(CancellationToken cancellationToken = default)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context?.User.Identity?.IsAuthenticated != true)
            return;

        try
        {
            var client = _httpClientFactory.CreateClient(AuthConstants.ApiClientName);
            using var response = await client.PostAsync("api/auth/logout", null, cancellationToken);
            _ = response;
        }
        catch
        {
            // Best-effort revoke; local cookies are still cleared by the caller.
        }
    }

    private TokenResponse? MapTokenResponse(AuthResponse authResponse)
    {
        if (string.IsNullOrWhiteSpace(authResponse.Token) || string.IsNullOrWhiteSpace(authResponse.RefreshToken))
            return null;

        return new TokenResponse
        {
            AccessToken = authResponse.Token,
            RefreshToken = authResponse.RefreshToken,
            ExpiresIn = ResolveExpiresInSeconds(authResponse.Token)
        };
    }

    private int ResolveExpiresInSeconds(string accessToken)
    {
        try
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
            if (jwt.ValidTo == DateTime.MinValue)
                return GetConfiguredExpiresInSeconds();

            var seconds = (int)Math.Ceiling((jwt.ValidTo - DateTime.UtcNow).TotalSeconds);
            return Math.Max(60, seconds);
        }
        catch
        {
            return GetConfiguredExpiresInSeconds();
        }
    }

    private int GetConfiguredExpiresInSeconds()
    {
        var minutes = _configuration.GetValue("Auth:AccessTokenFallbackMinutes", 60);
        return Math.Max(60, minutes * 60);
    }
}
