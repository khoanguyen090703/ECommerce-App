using Microsoft.AspNetCore.Http;

namespace ECommerce.Web.Auth;

public interface IAuthSessionManager
{
    string? GetAccessToken(HttpContext context);
    string? GetRefreshToken(HttpContext context);
    void UpdateTokens(HttpContext context, string accessToken, string refreshToken);
    void ClearTokens(HttpContext context);
    void MarkForceSignOut(HttpContext context, string redirectPath = "/Auth/SignIn");
    bool ShouldForceSignOut(HttpContext context, out string redirectPath);
}

public sealed class AuthSessionManager : IAuthSessionManager
{
    private const string AccessTokenCookieName = "access_token";
    private const string RefreshTokenCookieName = "refresh_token";
    private const string ForceSignOutItemKey = "__ForceSignOutRedirectPath";

    public string? GetAccessToken(HttpContext context)
        => context.Request.Cookies[AccessTokenCookieName];

    public string? GetRefreshToken(HttpContext context)
        => context.Request.Cookies[RefreshTokenCookieName];

    public void UpdateTokens(HttpContext context, string accessToken, string refreshToken)
    {
        context.Response.Cookies.Append(AccessTokenCookieName, accessToken, BuildAccessTokenCookieOptions(context));
        context.Response.Cookies.Append(RefreshTokenCookieName, refreshToken, BuildRefreshTokenCookieOptions(context));
    }

    public void ClearTokens(HttpContext context)
    {
        var expired = new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddYears(-1)
        };

        context.Response.Cookies.Append(AccessTokenCookieName, string.Empty, expired);
        context.Response.Cookies.Append(RefreshTokenCookieName, string.Empty, expired);
    }

    public void MarkForceSignOut(HttpContext context, string redirectPath = "/Auth/SignIn")
        => context.Items[ForceSignOutItemKey] = redirectPath;

    public bool ShouldForceSignOut(HttpContext context, out string redirectPath)
    {
        if (context.Items.TryGetValue(ForceSignOutItemKey, out var value) && value is string path && !string.IsNullOrWhiteSpace(path))
        {
            redirectPath = path;
            return true;
        }

        redirectPath = "/Auth/SignIn";
        return false;
    }

    private static CookieOptions BuildAccessTokenCookieOptions(HttpContext context) => new()
    {
        HttpOnly = true,
        Secure = context.Request.IsHttps,
        SameSite = SameSiteMode.Lax,
        Expires = DateTimeOffset.UtcNow.AddHours(1)
    };

    private static CookieOptions BuildRefreshTokenCookieOptions(HttpContext context) => new()
    {
        HttpOnly = true,
        Secure = context.Request.IsHttps,
        SameSite = SameSiteMode.Lax,
        Expires = DateTimeOffset.UtcNow.AddDays(7)
    };
}
