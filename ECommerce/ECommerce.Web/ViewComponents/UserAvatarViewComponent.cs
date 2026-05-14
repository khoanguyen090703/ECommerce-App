using ECommerce.SharedViewModels.DTOs.Response;
using ECommerce.Web.Auth;
using ECommerce.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace ECommerce.Web.ViewComponents;

public class UserAvatarViewComponent : ViewComponent
{
    private readonly IHttpClientFactory _httpClientFactory;

    public UserAvatarViewComponent(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var model = new UserAvatarViewModel();

        if (HttpContext.User.Identity?.IsAuthenticated != true)
            return View(model);

        try
        {
            var client = _httpClientFactory.CreateClient(AuthConstants.ApiClientName);
            using var response = await client.GetAsync("api/users/me");
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
