using ECommerce.SharedViewModels.DTOs.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;

namespace ECommerce.Web.Pages.Auth;

public class ConfirmEmailModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public ConfirmEmailModel(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public string? Message { get; set; }

    public bool IsSuccess { get; set; }

    public async Task<IActionResult> OnGetAsync(string? userId, string? token)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
        {
            IsSuccess = false;
            Message = "Liên kết không hợp lệ hoặc thiếu thông tin.";
            return Page();
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            var apiBaseUrl = _configuration["ApiBaseUrl"] ?? "http://localhost:5206";
            var url = $"{apiBaseUrl}/api/auth/confirm-email?userId={Uri.EscapeDataString(userId)}&token={Uri.EscapeDataString(token)}";
            var response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                IsSuccess = true;
                Message = "Xác nhận email thành công. Bạn có thể đăng nhập.";
                return Page();
            }

            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
            IsSuccess = false;
            Message = authResponse?.Message ?? "Xác nhận email thất bại. Liên kết có thể đã hết hạn.";
            return Page();
        }
        catch
        {
            IsSuccess = false;
            Message = "Không kết nối được máy chủ. Vui lòng thử lại sau.";
            return Page();
        }
    }
}
