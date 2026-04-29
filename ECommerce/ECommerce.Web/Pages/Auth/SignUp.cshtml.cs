using ECommerce.SharedViewModels.DTOs.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;

namespace ECommerce.Web.Pages.Auth
{
    public class SignUpModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public SignUpModel(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [BindProperty]
        public SignUpInputModel Input { get; set; } = new();

        [TempData]
        public string? SuccessMessage { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var request = new SignUpRequest
            {
                Email = Input.Email,
                Password = Input.Password,
                FullName = Input.FullName
            };

            try
            {
                var client = _httpClientFactory.CreateClient();
                var apiBaseUrl = _configuration["ApiBaseUrl"] ?? "http://localhost:5206";
                var response = await client.PostAsJsonAsync($"{apiBaseUrl}/api/auth/signup", request);
                var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();

                if (!response.IsSuccessStatusCode || authResponse?.IsSuccess != true)
                {
                    ModelState.AddModelError(string.Empty, authResponse?.Message ?? "Đăng ký thất bại. Vui lòng thử lại.");
                    return Page();
                }

                SuccessMessage = authResponse.Message;
                return RedirectToPage("/Auth/SignIn");
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Không kết nối được máy chủ xác thực. Vui lòng thử lại sau.");
                return Page();
            }
        }
    }

    public class SignUpInputModel
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

        [Display(Name = "Xác nhận mật khẩu")]
        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
        [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Display(Name = "Họ và tên")]
        [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ và tên phải từ {2} đến {1} ký tự.")]
        public string FullName { get; set; } = string.Empty;
    }
}
