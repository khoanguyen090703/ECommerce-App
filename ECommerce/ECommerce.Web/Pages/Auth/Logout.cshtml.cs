using ECommerce.Web.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.Web.Pages.Auth;

public class LogoutModel : PageModel
{
    private readonly IAuthSessionManager _authSessionManager;
    private readonly IHttpClientFactory _httpClientFactory;

    public LogoutModel(IHttpClientFactory httpClientFactory, IAuthSessionManager authSessionManager)
    {
        _httpClientFactory = httpClientFactory;
        _authSessionManager = authSessionManager;
    }

    public IActionResult OnGet()
    {
        return RedirectToPage("/Index");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var accessToken = _authSessionManager.GetAccessToken(HttpContext);

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                using var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/logout");
                await client.SendAsync(request);
            }
            catch
            {
                // Still clear cookies so tokens are never left in the browser.
            }
        }

        _authSessionManager.ClearTokens(HttpContext);

        return RedirectToPage("/Auth/SignIn", new { signedOut = true });
    }
}
