using ECommerce.Web.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.Web.Pages.Auth;

public class LogoutModel : PageModel
{
    private readonly IAuthService _authService;
    private readonly IAuthCookieService _authCookieService;

    public LogoutModel(IAuthService authService, IAuthCookieService authCookieService)
    {
        _authService = authService;
        _authCookieService = authCookieService;
    }

    public IActionResult OnGet()
    {
        return RedirectToPage("/Index");
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _authService.RevokeAsync(cancellationToken);
        }
        catch
        {
            // Local sign-out still proceeds.
        }

        await _authCookieService.SignOutAsync(HttpContext, cancellationToken);

        return RedirectToPage("/Auth/SignIn", new { signedOut = true });
    }
}
