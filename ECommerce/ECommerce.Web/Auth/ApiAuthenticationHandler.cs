using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ECommerce.SharedViewModels.DTOs.Auth;
using Microsoft.AspNetCore.Http;

namespace ECommerce.Web.Auth;

public sealed class ApiAuthenticationHandler : DelegatingHandler
{
    private static readonly HttpRequestOptionsKey<bool> RetryAttemptedKey = new("AuthRetryAttempted");

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAuthSessionManager _authSessionManager;

    public ApiAuthenticationHandler(
        IHttpContextAccessor httpContextAccessor,
        IHttpClientFactory httpClientFactory,
        IAuthSessionManager authSessionManager)
    {
        _httpContextAccessor = httpContextAccessor;
        _httpClientFactory = httpClientFactory;
        _authSessionManager = authSessionManager;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return await base.SendAsync(request, cancellationToken);
        }

        var accessToken = _authSessionManager.GetAccessToken(httpContext);
        if (!string.IsNullOrWhiteSpace(accessToken) && request.Headers.Authorization is null && !IsRefreshTokenRequest(request))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        var retryRequest = await CloneHttpRequestMessageAsync(request, cancellationToken);
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode != HttpStatusCode.Unauthorized ||
            IsRefreshTokenRequest(request) ||
            request.Options.TryGetValue(RetryAttemptedKey, out var alreadyRetried) && alreadyRetried)
        {
            return response;
        }

        var refreshToken = _authSessionManager.GetRefreshToken(httpContext);
        if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken))
        {
            _authSessionManager.MarkForceSignOut(httpContext);
            return response;
        }

        response.Dispose();

        var refreshResult = await TryRefreshTokenAsync(accessToken, refreshToken, cancellationToken);
        if (!refreshResult.IsSuccess || string.IsNullOrWhiteSpace(refreshResult.Token) || string.IsNullOrWhiteSpace(refreshResult.RefreshToken))
        {
            _authSessionManager.MarkForceSignOut(httpContext);
            return new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                RequestMessage = request
            };
        }

        _authSessionManager.UpdateTokens(httpContext, refreshResult.Token, refreshResult.RefreshToken);
        retryRequest.Options.Set(RetryAttemptedKey, true);
        retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshResult.Token);

        return await base.SendAsync(retryRequest, cancellationToken);
    }

    private async Task<AuthResponse> TryRefreshTokenAsync(string accessToken, string refreshToken, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("ApiAnonymous");
        using var response = await client.PostAsJsonAsync(
            "api/auth/refresh-token",
            new RefreshTokenRequest
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            },
            cancellationToken);

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: cancellationToken);

        if (response.IsSuccessStatusCode && authResponse?.IsSuccess == true)
        {
            return authResponse;
        }

        return new AuthResponse
        {
            IsSuccess = false,
            Message = authResponse?.Message ?? "Refresh token failed."
        };
    }

    private static bool IsRefreshTokenRequest(HttpRequestMessage request)
    {
        var path = request.RequestUri?.IsAbsoluteUri == true
            ? request.RequestUri.AbsolutePath
            : request.RequestUri?.ToString();

        return string.Equals(path, "/api/auth/refresh-token", StringComparison.OrdinalIgnoreCase)
               || string.Equals(path, "api/auth/refresh-token", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version,
            VersionPolicy = request.VersionPolicy
        };

        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        foreach (var option in request.Options)
        {
            clone.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);
        }

        if (request.Content is null)
        {
            return clone;
        }

        var contentBytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
        var contentClone = new ByteArrayContent(contentBytes);

        foreach (var header in request.Content.Headers)
        {
            contentClone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        clone.Content = contentClone;
        return clone;
    }
}
