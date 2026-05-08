using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.Web.Pages;

[Authorize]
public class OrderSuccessModel : PageModel
{
    public void OnGet()
    {
    }
}
