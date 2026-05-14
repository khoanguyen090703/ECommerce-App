using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Text.Json;
using ECommerce.SharedViewModels.DTOs.Request;
using ECommerce.SharedViewModels.DTOs.Response;
using ECommerce.Web.Auth;
using ECommerce.Web.Models;
using ECommerce.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.Web.Pages;

[Authorize]
public class CheckoutModel : PageModel
{
  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    PropertyNameCaseInsensitive = true
  };

  private readonly IHttpClientFactory _httpClientFactory;
  private readonly VietnamAddressClient _vietnamAddressClient;
  private readonly IAuthService _authService;
  private readonly IAuthCookieService _authCookieService;

  public CheckoutModel(
    IHttpClientFactory httpClientFactory,
    VietnamAddressClient vietnamAddressClient,
    IAuthService authService,
    IAuthCookieService authCookieService)
  {
    _httpClientFactory = httpClientFactory;
    _vietnamAddressClient = vietnamAddressClient;
    _authService = authService;
    _authCookieService = authCookieService;
  }

  public CheckoutInfoResponse? Checkout { get; private set; }

  public IReadOnlyList<VietnamRegionOption> Provinces { get; private set; } = [];

  public IReadOnlyList<VietnamRegionOption> Districts { get; private set; } = [];

  public IReadOnlyList<VietnamRegionOption> Wards { get; private set; } = [];

  public string? ErrorMessage { get; private set; }

  [BindProperty]
  public CheckoutInput Input { get; set; } = new();

  public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
  {
    await LoadPageAsync(cancellationToken);
    return Page();
  }

  public async Task<IActionResult> OnGetDistrictsAsync(int provinceCode, CancellationToken cancellationToken)
  {
    if (provinceCode < 1)
      return new JsonResult(Array.Empty<object>());

    try
    {
      var districts = await _vietnamAddressClient.GetDistrictsAsync(provinceCode, cancellationToken);
      return new JsonResult(districts.Select(option => new { code = option.Code, name = option.Name }));
    }
    catch
    {
      return new JsonResult(Array.Empty<object>());
    }
  }

  public async Task<IActionResult> OnGetWardsAsync(int districtCode, CancellationToken cancellationToken)
  {
    if (districtCode < 1)
      return new JsonResult(Array.Empty<object>());

    try
    {
      var wards = await _vietnamAddressClient.GetWardsAsync(districtCode, cancellationToken);
      return new JsonResult(wards.Select(option => new { code = option.Code, name = option.Name }));
    }
    catch
    {
      return new JsonResult(Array.Empty<object>());
    }
  }

  public async Task<IActionResult> OnPostPlaceOrderAsync(CancellationToken cancellationToken)
  {
    await LoadPageAsync(cancellationToken);

    if (Checkout is null)
      return Page();

    if (Checkout.CartItems.Count == 0)
    {
      ErrorMessage = "Giỏ hàng của bạn đang trống.";
      return Page();
    }

    ValidateInput();
    if (!ModelState.IsValid)
      return Page();

    var shippingAddress = BuildShippingAddress();
    var request = new CreateOrderRequest
    {
      RecipientName = Input.RecipientName.Trim(),
      PhoneNumber = Input.PhoneNumber.Trim(),
      ShippingAddress = shippingAddress,
      PaymentMethodId = Input.PaymentMethodId,
      OrderItems = Checkout.CartItems
        .Select(item => new Item4CreateOrderRequest
        {
          ProductVariantId = item.ProductVariantId,
          Quantity = item.Quantity
        })
        .ToList()
    };

    var client = _httpClientFactory.CreateClient(AuthConstants.ApiClientName);
    using var response = await client.PostAsJsonAsync("api/orders", request, cancellationToken);
    if (!response.IsSuccessStatusCode)
    {
      ErrorMessage = await ReadApiErrorAsync(response, cancellationToken)
        ?? "Không tạo được đơn hàng. Vui lòng thử lại.";
      return Page();
    }

    var created = await response.Content.ReadFromJsonAsync<CreateOrderResponse>(JsonOptions, cancellationToken);
    if (created is null || created.OrderId < 1)
    {
      ErrorMessage = "Không tạo được đơn hàng. Vui lòng thử lại.";
      return Page();
    }

    if (!created.RequiresOnlinePayment)
      return RedirectToPage("/OrderSuccess", new { orderId = created.OrderId });

    await TryRefreshSessionAsync(cancellationToken);

    using var stripeRequest = new HttpRequestMessage(HttpMethod.Post, "api/payments/stripe/checkout")
    {
      Content = JsonContent.Create(new CreateStripeCheckoutRequest { OrderId = created.OrderId })
    };

    using var stripeResponse = await client.SendAsync(stripeRequest, cancellationToken);
    if (!stripeResponse.IsSuccessStatusCode)
    {
      ErrorMessage = await ReadApiErrorAsync(stripeResponse, cancellationToken)
        ?? "Không tạo được phiên thanh toán.";
      return RedirectToPage("/Orders/PaymentPending", new { orderId = created.OrderId });
    }

    var stripePayload = await stripeResponse.Content.ReadFromJsonAsync<StripeCheckoutRedirectResponse>(JsonOptions, cancellationToken);
    if (string.IsNullOrWhiteSpace(stripePayload?.CheckoutUrl))
    {
      ErrorMessage = "Không nhận được liên kết thanh toán.";
      return RedirectToPage("/Orders/PaymentPending", new { orderId = created.OrderId });
    }

    return Redirect(stripePayload.CheckoutUrl);
  }

  private async Task LoadPageAsync(CancellationToken cancellationToken)
  {
    try
    {
      var client = _httpClientFactory.CreateClient(AuthConstants.ApiClientName);
      using var response = await client.GetAsync("api/checkout", cancellationToken);
      if (!response.IsSuccessStatusCode)
      {
        ErrorMessage = await ReadApiErrorAsync(response, cancellationToken)
          ?? "Không tải được trang thanh toán.";
        return;
      }

      Checkout = await response.Content.ReadFromJsonAsync<CheckoutInfoResponse>(JsonOptions, cancellationToken);
      if (Checkout is null)
      {
        ErrorMessage = "Không tải được trang thanh toán.";
        return;
      }

      Provinces = await _vietnamAddressClient.GetProvincesAsync(cancellationToken);

      if (Input.ProvinceCode > 0)
      {
        Districts = await _vietnamAddressClient.GetDistrictsAsync(Input.ProvinceCode, cancellationToken);
        if (Input.DistrictCode > 0)
          Wards = await _vietnamAddressClient.GetWardsAsync(Input.DistrictCode, cancellationToken);
      }
    }
    catch
    {
      ErrorMessage = "Không tải được trang thanh toán.";
    }
  }

  private void ValidateInput()
  {
    if (string.IsNullOrWhiteSpace(Input.RecipientName))
      ModelState.AddModelError(nameof(Input.RecipientName), "Vui lòng nhập tên người nhận.");

    if (string.IsNullOrWhiteSpace(Input.PhoneNumber))
      ModelState.AddModelError(nameof(Input.PhoneNumber), "Vui lòng nhập số điện thoại.");

    if (Input.ProvinceCode < 1)
      ModelState.AddModelError(nameof(Input.ProvinceCode), "Vui lòng chọn tỉnh/thành.");

    if (Input.DistrictCode < 1)
      ModelState.AddModelError(nameof(Input.DistrictCode), "Vui lòng chọn quận/huyện.");

    if (Input.WardCode < 1)
      ModelState.AddModelError(nameof(Input.WardCode), "Vui lòng chọn phường/xã.");

    if (string.IsNullOrWhiteSpace(Input.Street))
      ModelState.AddModelError(nameof(Input.Street), "Vui lòng nhập số nhà và tên đường.");

    if (Input.PaymentMethodId < 1)
      ModelState.AddModelError(nameof(Input.PaymentMethodId), "Vui lòng chọn phương thức thanh toán.");
  }

  private string BuildShippingAddress()
  {
    var street = Input.Street.Trim();
    var wardName = Wards.FirstOrDefault(option => option.Code == Input.WardCode)?.Name ?? string.Empty;
    var districtName = Districts.FirstOrDefault(option => option.Code == Input.DistrictCode)?.Name ?? string.Empty;
    var provinceName = Provinces.FirstOrDefault(option => option.Code == Input.ProvinceCode)?.Name ?? string.Empty;

    return string.Join(", ", new[] { street, wardName, districtName, provinceName }.Where(part => !string.IsNullOrWhiteSpace(part)));
  }

  private async Task TryRefreshSessionAsync(CancellationToken cancellationToken)
  {
    var refreshToken = HttpContext.Request.Cookies[AuthConstants.RefreshTokenCookieName];
    var accessToken = User.FindFirst(AuthConstants.AccessTokenClaimType)?.Value;
    if (string.IsNullOrWhiteSpace(refreshToken) || string.IsNullOrWhiteSpace(accessToken))
      return;

    try
    {
      var refreshed = await _authService.RefreshAsync(accessToken, refreshToken, cancellationToken);
      if (refreshed is null)
        return;

      var userName = User.Identity?.Name ?? string.Empty;
      await _authCookieService.SignInAsync(HttpContext, refreshed, userName, cancellationToken);
    }
    catch
    {
      // Best-effort refresh before leaving for Stripe.
    }
  }

  private static async Task<string?> ReadApiErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
  {
    var text = await response.Content.ReadAsStringAsync(cancellationToken);
    if (string.IsNullOrWhiteSpace(text))
      return null;

    try
    {
      using var document = JsonDocument.Parse(text);
      if (document.RootElement.TryGetProperty("message", out var message))
        return message.GetString();

      if (document.RootElement.TryGetProperty("title", out var title))
        return title.GetString();

      if (document.RootElement.TryGetProperty("error", out var error))
        return error.GetString();
    }
    catch
    {
      // Ignore malformed error payloads.
    }

    return null;
  }

  public sealed class CheckoutInput
  {
    [Display(Name = "Tên người nhận")]
    public string RecipientName { get; set; } = string.Empty;

    [Display(Name = "Số điện thoại")]
    public string PhoneNumber { get; set; } = string.Empty;

    public int ProvinceCode { get; set; }

    public int DistrictCode { get; set; }

    public int WardCode { get; set; }

    [Display(Name = "Số nhà, tên đường")]
    public string Street { get; set; } = string.Empty;

    public int PaymentMethodId { get; set; }
  }

  private sealed class StripeCheckoutRedirectResponse
  {
    public string? CheckoutUrl { get; set; }
  }
}
