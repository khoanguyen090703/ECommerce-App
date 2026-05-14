using Microsoft.AspNetCore.Http;

namespace ECommerce.Web.Auth;

/// <summary>
/// Refreshes JWT cookies before authentication when the access token is close to expiring
/// (e.g. after returning from Stripe Checkout).
/// </summary>
public sealed class AuthCookieRefreshMiddleware
{
    private static readonly TimeSpan RefreshBeforeExpiry = TimeSpan.FromMinutes(5);

    private readonly RequestDelegate _next;

    public AuthCookieRefreshMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsAuthenticationPage(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var authSessionManager = context.RequestServices.GetRequiredService<IAuthSessionManager>();
        var accessToken = authSessionManager.GetAccessToken(context);
        var refreshToken = authSessionManager.GetRefreshToken(context);

        if (!string.IsNullOrWhiteSpace(accessToken)
            && !string.IsNullOrWhiteSpace(refreshToken)
            && AuthTokenRefresher.AccessTokenNeedsRefresh(accessToken, RefreshBeforeExpiry))
        {
            await AuthTokenRefresher.TryRefreshAsync(context, context.RequestAborted);
        }

        await _next(context);
    }

    private static bool IsAuthenticationPage(PathString path)
        => path.StartsWithSegments("/Auth/SignIn", StringComparison.OrdinalIgnoreCase)
           || path.StartsWithSegments("/Auth/Logout", StringComparison.OrdinalIgnoreCase);
}
