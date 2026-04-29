using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using ECommerce.SharedViewModels.DTOs.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace ECommerce.Web.Auth;

public static class JwtCookieRefreshEvents
{
    public static async Task HandleAuthenticationFailedAsync(AuthenticationFailedContext context)
    {
        if (context.Exception is not SecurityTokenExpiredException)
        {
            return;
        }

        var httpContext = context.HttpContext;
        var path = httpContext.Request.Path;
        if (IsAuthenticationPage(path))
        {
            return;
        }

        var authSessionManager = httpContext.RequestServices.GetRequiredService<IAuthSessionManager>();
        var accessToken = authSessionManager.GetAccessToken(httpContext);
        var refreshToken = authSessionManager.GetRefreshToken(httpContext);

        if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken))
        {
            authSessionManager.ClearTokens(httpContext);
            RedirectToSignIn(context);
            return;
        }

        var clientFactory = httpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
        var client = clientFactory.CreateClient("ApiAnonymous");

        using var response = await client.PostAsJsonAsync(
            "api/auth/refresh-token",
            new RefreshTokenRequest
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            },
            httpContext.RequestAborted);

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: httpContext.RequestAborted);
        if (!response.IsSuccessStatusCode || authResponse?.IsSuccess != true || string.IsNullOrWhiteSpace(authResponse.Token) || string.IsNullOrWhiteSpace(authResponse.RefreshToken))
        {
            authSessionManager.ClearTokens(httpContext);
            RedirectToSignIn(context);
            return;
        }

        authSessionManager.UpdateTokens(httpContext, authResponse.Token, authResponse.RefreshToken);

        var tokenValidationParameters = CloneValidationParameters(context.Options.TokenValidationParameters);
        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(authResponse.Token, tokenValidationParameters, out _);

        context.Principal = principal;
        context.Success();
    }

    private static TokenValidationParameters CloneValidationParameters(TokenValidationParameters source)
        => new()
        {
            ValidateIssuerSigningKey = source.ValidateIssuerSigningKey,
            IssuerSigningKey = source.IssuerSigningKey,
            IssuerSigningKeys = source.IssuerSigningKeys,
            ValidateIssuer = source.ValidateIssuer,
            ValidIssuer = source.ValidIssuer,
            ValidIssuers = source.ValidIssuers,
            ValidateAudience = source.ValidateAudience,
            ValidAudience = source.ValidAudience,
            ValidAudiences = source.ValidAudiences,
            ValidateLifetime = source.ValidateLifetime,
            RequireExpirationTime = source.RequireExpirationTime,
            RequireSignedTokens = source.RequireSignedTokens,
            ClockSkew = source.ClockSkew,
            NameClaimType = source.NameClaimType,
            RoleClaimType = source.RoleClaimType
        };

    private static void RedirectToSignIn(AuthenticationFailedContext context)
    {
        context.NoResult();
        context.Response.Redirect("/Auth/SignIn");
    }

    private static bool IsAuthenticationPage(PathString path)
        => path.StartsWithSegments("/Auth/SignIn", StringComparison.OrdinalIgnoreCase)
           || path.StartsWithSegments("/Auth/Logout", StringComparison.OrdinalIgnoreCase);
}
