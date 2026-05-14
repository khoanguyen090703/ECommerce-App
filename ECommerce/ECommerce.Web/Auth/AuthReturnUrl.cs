using Microsoft.AspNetCore.Http;

namespace ECommerce.Web.Auth;

public static class AuthReturnUrl
{
    public static string? Normalize(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
            return null;

        var value = returnUrl.Trim();
        if (!value.StartsWith("/", StringComparison.Ordinal))
            return null;

        if (value.StartsWith("//", StringComparison.Ordinal))
            return null;

        return value;
    }

    public static string BuildSignInUrl(HttpRequest request)
    {
        var returnUrl = Normalize(request.Path + request.QueryString);
        return string.IsNullOrEmpty(returnUrl)
            ? "/Auth/SignIn"
            : $"/Auth/SignIn?returnUrl={Uri.EscapeDataString(returnUrl)}";
    }
}
