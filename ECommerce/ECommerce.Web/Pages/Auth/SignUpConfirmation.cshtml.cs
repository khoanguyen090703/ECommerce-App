using ECommerce.SharedViewModels.DTOs.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;

namespace ECommerce.Web.Pages.Auth;

public class SignUpConfirmationModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public SignUpConfirmationModel(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public string Email { get; private set; } = string.Empty;

    /// <summary>
    /// True only on the first GET right after successful signup redirect — triggers 60s client cooldown.
    /// </summary>
    public bool NeedInitialCooldown { get; private set; }

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public bool StatusIsError { get; set; }

    /// <summary>
    /// Sau khi gửi lại email thành công — client bắt đầu đếm 60 giây.
    /// </summary>
    [TempData]
    public bool RestartResendCooldown { get; set; }

    public IActionResult OnGet()
    {
        var email = HttpContext.Session.GetString("PendingConfirmationEmail");
        if (string.IsNullOrWhiteSpace(email))
            return RedirectToPage("/Auth/SignUp");

        Email = email;
        NeedInitialCooldown = HttpContext.Session.GetString("EmailConfirmNeedsCooldown") == "1";
        if (NeedInitialCooldown)
            HttpContext.Session.Remove("EmailConfirmNeedsCooldown");

        return Page();
    }

    public async Task<IActionResult> OnPostResendAsync()
    {
        var email = HttpContext.Session.GetString("PendingConfirmationEmail");
        if (string.IsNullOrWhiteSpace(email))
            return RedirectToPage("/Auth/SignUp");

        Email = email;

        try
        {
            var client = _httpClientFactory.CreateClient();
            var apiBaseUrl = _configuration["ApiBaseUrl"] ?? "http://localhost:5206";
            var body = new ResendEmailConfirmationRequest { Email = email };
            var response = await client.PostAsJsonAsync($"{apiBaseUrl}/api/auth/resend-confirmation", body);
            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();

            if (!response.IsSuccessStatusCode || authResponse?.IsSuccess != true)
            {
                StatusMessage = authResponse?.Message ?? "Không gửi lại được email. Vui lòng thử sau.";
                StatusIsError = true;
                RestartResendCooldown = false;
            }
            else
            {
                StatusMessage = "Đã gửi lại email xác nhận. Vui lòng kiểm tra hộp thư.";
                StatusIsError = false;
                RestartResendCooldown = true;
            }
        }
        catch
        {
            StatusMessage = "Không kết nối được máy chủ. Vui lòng thử lại sau.";
            StatusIsError = true;
            RestartResendCooldown = false;
        }

        return RedirectToPage();
    }
}
