using System.IdentityModel.Tokens.Jwt;
using ECommerce.SharedViewModels.DTOs.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace ECommerce.Web.Auth;

public static class JwtCookieRefreshEvents
{
    public static async Task HandleAuthenticationFailedAsync(AuthenticationFailedContext context)
    {
        if (context.Exception is not SecurityTokenExpiredException)
            return;

        var httpContext = context.HttpContext;
        if (IsAuthenticationPage(httpContext.Request.Path))
            return;

        var authSessionManager = httpContext.RequestServices.GetRequiredService<IAuthSessionManager>();
        var refreshed = await AuthTokenRefresher.TryRefreshAsync(httpContext, httpContext.RequestAborted);
        if (!refreshed)
        {
            authSessionManager.ClearTokens(httpContext);
            RedirectToSignIn(context);
            return;
        }

        var accessToken = authSessionManager.GetAccessToken(httpContext);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            authSessionManager.ClearTokens(httpContext);
            RedirectToSignIn(context);
            return;
        }

        var tokenValidationParameters = CloneValidationParameters(context.Options.TokenValidationParameters);
        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(accessToken, tokenValidationParameters, out _);

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
        context.Response.Redirect(AuthReturnUrl.BuildSignInUrl(context.Request));
    }

    private static bool IsAuthenticationPage(PathString path)
        => path.StartsWithSegments("/Auth/SignIn", StringComparison.OrdinalIgnoreCase)
           || path.StartsWithSegments("/Auth/Logout", StringComparison.OrdinalIgnoreCase);
}
