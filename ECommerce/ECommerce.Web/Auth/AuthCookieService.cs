using System.Security.Claims;
using ECommerce.SharedViewModels.DTOs.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

namespace ECommerce.Web.Auth;

public interface IAuthCookieService
{
    Task SignInAsync(HttpContext context, TokenResponse tokens, string userName, CancellationToken cancellationToken = default);

    Task SignOutAsync(HttpContext context, CancellationToken cancellationToken = default);

    void SetRefreshTokenCookie(HttpContext context, string refreshToken);
}

public sealed class AuthCookieService : IAuthCookieService
{
    public Task SignInAsync(HttpContext context, TokenResponse tokens, string userName, CancellationToken cancellationToken = default)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, userName),
            new(AuthConstants.AccessTokenClaimType, tokens.AccessToken)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var properties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.Add(AuthConstants.AuthenticationCookieLifetime)
        };

        SetRefreshTokenCookie(context, tokens.RefreshToken);
        return context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, properties);
    }

    public async Task SignOutAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        context.Response.Cookies.Delete(
            AuthConstants.RefreshTokenCookieName,
            BuildRefreshTokenCookieOptions(context, DateTimeOffset.UtcNow.AddYears(-1)));

        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    public void SetRefreshTokenCookie(HttpContext context, string refreshToken)
    {
        context.Response.Cookies.Append(
            AuthConstants.RefreshTokenCookieName,
            refreshToken,
            BuildRefreshTokenCookieOptions(context, DateTimeOffset.UtcNow.Add(AuthConstants.RefreshTokenCookieLifetime)));
    }

    internal static CookieOptions BuildRefreshTokenCookieOptions(HttpContext context, DateTimeOffset expires)
        => new()
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = expires,
            Path = "/"
        };
}
