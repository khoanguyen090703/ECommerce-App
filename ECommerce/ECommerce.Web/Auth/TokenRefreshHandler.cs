using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;

namespace ECommerce.Web.Auth;

public sealed class TokenRefreshHandler : DelegatingHandler
{
    private static readonly HttpRequestOptionsKey<bool> RetryAttemptedKey = new("AuthRetryAttempted");

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuthService _authService;
    private readonly IAuthCookieService _authCookieService;

    public TokenRefreshHandler(
        IHttpContextAccessor httpContextAccessor,
        IAuthService authService,
        IAuthCookieService authCookieService)
    {
        _httpContextAccessor = httpContextAccessor;
        _authService = authService;
        _authCookieService = authCookieService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            var accessToken = httpContext.User.FindFirstValue(AuthConstants.AccessTokenClaimType);
            if (!string.IsNullOrWhiteSpace(accessToken)
                && request.Headers.Authorization is null
                && !IsAuthEndpoint(request))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }
        }

        var retryRequest = await CloneHttpRequestMessageAsync(request, cancellationToken);
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode != HttpStatusCode.Unauthorized
            || httpContext is null
            || IsAuthEndpoint(request)
            || (request.Options.TryGetValue(RetryAttemptedKey, out var alreadyRetried) && alreadyRetried))
        {
            return response;
        }

        var refreshToken = httpContext.Request.Cookies[AuthConstants.RefreshTokenCookieName];
        var currentAccessToken = httpContext.User.FindFirstValue(AuthConstants.AccessTokenClaimType);
        if (string.IsNullOrWhiteSpace(refreshToken) || string.IsNullOrWhiteSpace(currentAccessToken))
        {
            await _authCookieService.SignOutAsync(httpContext, cancellationToken);
            return response;
        }

        response.Dispose();

        var refreshed = await _authService.RefreshAsync(currentAccessToken, refreshToken, cancellationToken);
        if (refreshed is null)
        {
            await _authCookieService.SignOutAsync(httpContext, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                RequestMessage = request
            };
        }

        var userName = httpContext.User.Identity?.Name ?? string.Empty;
        await _authCookieService.SignInAsync(httpContext, refreshed, userName, cancellationToken);

        retryRequest.Options.Set(RetryAttemptedKey, true);
        retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshed.AccessToken);

        return await base.SendAsync(retryRequest, cancellationToken);
    }

    private static bool IsAuthEndpoint(HttpRequestMessage request)
    {
        var path = request.RequestUri?.IsAbsoluteUri == true
            ? request.RequestUri.AbsolutePath
            : request.RequestUri?.ToString();

        if (string.IsNullOrWhiteSpace(path))
            return false;

        return path.Contains("/api/auth/signin", StringComparison.OrdinalIgnoreCase)
               || path.Contains("/api/auth/signup", StringComparison.OrdinalIgnoreCase)
               || path.Contains("/api/auth/refresh-token", StringComparison.OrdinalIgnoreCase)
               || path.Contains("/api/auth/logout", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version,
            VersionPolicy = request.VersionPolicy
        };

        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        foreach (var option in request.Options)
            clone.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);

        if (request.Content is null)
            return clone;

        var contentBytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
        var contentClone = new ByteArrayContent(contentBytes);

        foreach (var header in request.Content.Headers)
            contentClone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        clone.Content = contentClone;
        return clone;
    }
}
