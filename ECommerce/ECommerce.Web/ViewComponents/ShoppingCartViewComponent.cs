using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.ViewComponents;

/// <summary>
/// Header cart trigger, slide-out drawer markup, and client script (data loaded via same-origin /api/cart* BFF).
/// </summary>
public sealed class ShoppingCartViewComponent : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        if (User?.Identity?.IsAuthenticated != true)
        {
            return Content(string.Empty);
        }

        return View();
    }
}
