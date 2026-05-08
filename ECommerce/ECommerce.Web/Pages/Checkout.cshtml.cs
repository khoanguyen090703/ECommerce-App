using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.Web.Pages;

[Authorize]
public class CheckoutModel : PageModel
{
    public void OnGet()
    {
    }
}
