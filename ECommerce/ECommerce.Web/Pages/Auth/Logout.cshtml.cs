using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.Web.Pages.Auth;

public class LogoutModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public LogoutModel(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public IActionResult OnGet()
    {
        return RedirectToPage("/Index");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var accessToken = Request.Cookies["access_token"];
        var apiBaseUrl = _configuration["ApiBaseUrl"] ?? "http://localhost:5206";

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{apiBaseUrl}/api/auth/logout");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                await client.SendAsync(request);
            }
            catch
            {
                // Still clear cookies so tokens are never left in the browser.
            }
        }

        ClearAuthCookies();

        return RedirectToPage("/Auth/SignIn", new { signedOut = true });
    }

    private void ClearAuthCookies()
    {
        var expired = new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddYears(-1)
        };

        Response.Cookies.Append("access_token", string.Empty, expired);
        Response.Cookies.Append("refresh_token", string.Empty, expired);
    }
}
