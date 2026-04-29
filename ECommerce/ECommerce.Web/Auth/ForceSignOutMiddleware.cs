namespace ECommerce.Web.Auth;

public sealed class ForceSignOutMiddleware
{
    private readonly RequestDelegate _next;

    public ForceSignOutMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAuthSessionManager authSessionManager)
    {
        await _next(context);

        if (context.Response.HasStarted || !authSessionManager.ShouldForceSignOut(context, out var redirectPath))
        {
            return;
        }

        if (IsAuthenticationPage(context.Request.Path))
        {
            authSessionManager.ClearTokens(context);
            return;
        }

        authSessionManager.ClearTokens(context);
        context.Response.Redirect(redirectPath);
    }

    private static bool IsAuthenticationPage(PathString path)
        => path.StartsWithSegments("/Auth/SignIn", StringComparison.OrdinalIgnoreCase)
           || path.StartsWithSegments("/Auth/Logout", StringComparison.OrdinalIgnoreCase);
}
