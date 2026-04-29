using ECommerce.SharedViewModels.DTOs.Response;
using ECommerce.Web.Auth;
using ECommerce.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace ECommerce.Web.ViewComponents;

public class UserAvatarViewComponent : ViewComponent
{
    private readonly IAuthSessionManager _authSessionManager;
    private readonly IHttpClientFactory _httpClientFactory;

    public UserAvatarViewComponent(IHttpClientFactory httpClientFactory, IAuthSessionManager authSessionManager)
    {
        _httpClientFactory = httpClientFactory;
        _authSessionManager = authSessionManager;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var model = new UserAvatarViewModel();
        var accessToken = _authSessionManager.GetAccessToken(HttpContext);

        if (string.IsNullOrWhiteSpace(accessToken))
            return View(model);

        try
        {
            var client = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, "api/users/me");

            using var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return View(model);

            var profile = await response.Content.ReadFromJsonAsync<UserProfileResponse>();
            if (profile == null)
                return View(model);

            model.FullName = string.IsNullOrWhiteSpace(profile.FullName) ? model.FullName : profile.FullName;
            model.Email = profile.Email ?? string.Empty;
            model.AvatarUrl = string.IsNullOrWhiteSpace(profile.AvatarUrl) ? model.AvatarUrl : profile.AvatarUrl;
        }
        catch
        {
            // Keep header rendering stable even if profile API is unavailable.
        }

        return View(model);
    }
}
