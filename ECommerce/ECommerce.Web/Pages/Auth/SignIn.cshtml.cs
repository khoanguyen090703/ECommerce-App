using ECommerce.SharedViewModels.DTOs.Auth;
using ECommerce.Web.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;

namespace ECommerce.Web.Pages.Auth
{
    public class SignInModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IAuthSessionManager _authSessionManager;

        public SignInModel(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IAuthSessionManager authSessionManager)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _authSessionManager = authSessionManager;
        }

        [BindProperty]
        public SignInInputModel Input { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        [TempData]
        public string? SuccessMessage { get; set; }

        public void OnGet()
        {
            ReturnUrl = AuthReturnUrl.Normalize(ReturnUrl);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var request = new SignInRequest
            {
                Email = Input.Email,
                Password = Input.Password
            };

            try
            {
                var client = _httpClientFactory.CreateClient();
                var apiBaseUrl = _configuration["ApiBaseUrl"] ?? "http://localhost:5206";
                var response = await client.PostAsJsonAsync($"{apiBaseUrl}/api/auth/signin", request);
                var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();

                if (!response.IsSuccessStatusCode || authResponse?.IsSuccess != true)
                {
                    ModelState.AddModelError(string.Empty, authResponse?.Message ?? "Đăng nhập thất bại. Vui lòng kiểm tra email và mật khẩu.");
                    return Page();
                }

                if (!string.IsNullOrWhiteSpace(authResponse.Token) && !string.IsNullOrWhiteSpace(authResponse.RefreshToken))
                {
                    _authSessionManager.UpdateTokens(HttpContext, authResponse.Token, authResponse.RefreshToken);
                }

                SuccessMessage = authResponse.Message;
                var destination = AuthReturnUrl.Normalize(ReturnUrl) ?? "/Index";
                return Redirect(destination);
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Không kết nối được máy chủ xác thực. Vui lòng thử lại sau.");
                return Page();
            }
        }
    }

    public class SignInInputModel
    {
        [Display(Name = "Email")]
        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [StringLength(100, ErrorMessage = "Email tối đa {1} ký tự.")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Mật khẩu")]
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ {2} đến {1} ký tự.")]
        public string Password { get; set; } = string.Empty;
    }
}
