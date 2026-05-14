using ECommerce.Web.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace ECommerce.Web.Pages.Auth
{
    public class SignInModel : PageModel
    {
        private readonly IAuthService _authService;
        private readonly IAuthCookieService _authCookieService;

        public SignInModel(IAuthService authService, IAuthCookieService authCookieService)
        {
            _authService = authService;
            _authCookieService = authCookieService;
        }

        [BindProperty]
        public SignInInputModel Input { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        [TempData]
        public string? SuccessMessage { get; set; }

        public bool SignedOut { get; private set; }

        public void OnGet()
        {
            ReturnUrl = AuthReturnUrl.Normalize(ReturnUrl);
            SignedOut = string.Equals(Request.Query["signedOut"], "true", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return Page();

            try
            {
                var tokenResponse = await _authService.LoginAsync(Input.Email, Input.Password, cancellationToken);
                if (tokenResponse is null)
                {
                    ModelState.AddModelError(string.Empty, "Đăng nhập thất bại. Vui lòng kiểm tra email và mật khẩu.");
                    return Page();
                }

                await _authCookieService.SignInAsync(HttpContext, tokenResponse, Input.Email, cancellationToken);

                SuccessMessage = "Đăng nhập thành công.";
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
