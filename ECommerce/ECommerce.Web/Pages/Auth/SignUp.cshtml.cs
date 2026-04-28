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
                    ModelState.AddModelError(string.Empty, authResponse?.Message ?? "Sign up failed. Please try again.");
                    return Page();
                }

                SuccessMessage = authResponse.Message;
                return RedirectToPage("/Auth/SignIn");
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Cannot connect to authentication server. Please try again later.");
                return Page();
            }
        }
    }

    public class SignUpInputModel
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

        [Display(Name = "Confirm password")]
        [Required(ErrorMessage = "Confirm password is required.")]
        [Compare(nameof(Password), ErrorMessage = "Confirm password does not match password.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Display(Name = "Full name")]
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be between {2} and {1} characters.")]
        public string FullName { get; set; } = string.Empty;
    }
}
