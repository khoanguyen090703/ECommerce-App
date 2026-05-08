using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.Web.Pages.Orders;

[Authorize]
public class DetailsModel : PageModel
{
    public void OnGet()
    {
    }
}
