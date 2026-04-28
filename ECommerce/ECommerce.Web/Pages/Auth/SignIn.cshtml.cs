using ECommerce.SharedViewModels.DTOs.Auth;
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

        public SignInModel(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [BindProperty]
        public SignInInputModel Input { get; set; } = new();

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
                    ModelState.AddModelError(string.Empty, authResponse?.Message ?? "Sign in failed. Please check your credentials.");
                    return Page();
                }

                if (!string.IsNullOrWhiteSpace(authResponse.Token))
                {
                    Response.Cookies.Append("access_token", authResponse.Token, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = Request.IsHttps,
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTimeOffset.UtcNow.AddHours(1)
                    });
                }

                if (!string.IsNullOrWhiteSpace(authResponse.RefreshToken))
                {
                    Response.Cookies.Append("refresh_token", authResponse.RefreshToken, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = Request.IsHttps,
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTimeOffset.UtcNow.AddDays(7)
                    });
                }

                SuccessMessage = authResponse.Message;
                return RedirectToPage("/Index");
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Cannot connect to authentication server. Please try again later.");
                return Page();
            }
        }
    }

    public class SignInInputModel
    {
        [Display(Name = "Email")]
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(100, ErrorMessage = "Email must be at most {1} characters.")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Password")]
        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between {2} and {1} characters.")]
        public string Password { get; set; } = string.Empty;
    }
}
