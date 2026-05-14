using System.Net.Http.Json;
using System.Text.Json;
using ECommerce.Domain.Common;
using ECommerce.SharedViewModels.DTOs.Response;
using ECommerce.Web.Auth;
using ECommerce.Web.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.Web.Pages.Orders;

[Authorize]
public class IndexModel : PageModel
{
  private const int PageSize = 5;

  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    PropertyNameCaseInsensitive = true
  };

  private static readonly IReadOnlyList<(string Key, string Label)> StatusFilters =
  [
    ("", "Tất cả"),
    ("Pending", "Chờ xác nhận"),
    ("Processing", "Đang xử lý"),
    ("Shipping", "Đang giao hàng"),
    ("Delivered", "Đã giao"),
    ("Cancelled", "Đã hủy")
  ];

  private readonly IHttpClientFactory _httpClientFactory;

  public IndexModel(IHttpClientFactory httpClientFactory)
  {
    _httpClientFactory = httpClientFactory;
  }

  [BindProperty(SupportsGet = true)]
  public string? Status { get; set; }

  [BindProperty(SupportsGet = true)]
  public int PageNumber { get; set; } = 1;

  public IReadOnlyList<(string Key, string Label)> Filters => StatusFilters;

  public PagedResult<MyOrderResponse> Orders { get; private set; } = new([], 0, 1, PageSize);

  public string? ErrorMessage { get; private set; }

  [TempData]
  public string? ActionError { get; set; }

  public async Task OnGetAsync(CancellationToken cancellationToken)
  {
    await LoadOrdersAsync(cancellationToken);
  }

  public async Task<IActionResult> OnPostCancelAsync(int orderId, CancellationToken cancellationToken)
  {
  if (orderId < 1)
  {
    ActionError = "Mã đơn hàng không hợp lệ.";
    return RedirectToPage(new { Status, PageNumber });
  }

  var client = _httpClientFactory.CreateClient(AuthConstants.ApiClientName);
  using var response = await client.PostAsync($"api/orders/{orderId}/cancel", null, cancellationToken);
  if (!response.IsSuccessStatusCode)
  {
    ActionError = await ReadApiErrorAsync(response, cancellationToken)
      ?? "Không hủy được đơn hàng.";
  }

  return RedirectToPage(new { Status, PageNumber });
  }

  public async Task<IActionResult> OnPostPayAgainAsync(int orderId, CancellationToken cancellationToken)
  {
  if (orderId < 1)
  {
    ActionError = "Mã đơn hàng không hợp lệ.";
    return RedirectToPage(new { Status, PageNumber });
  }

  var client = _httpClientFactory.CreateClient(AuthConstants.ApiClientName);
  using var request = new HttpRequestMessage(HttpMethod.Post, $"api/payments/stripe/orders/{orderId}/retry")
  {
    Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
  };

  using var response = await client.SendAsync(request, cancellationToken);
  if (!response.IsSuccessStatusCode)
  {
    ActionError = await ReadApiErrorAsync(response, cancellationToken)
      ?? "Không tạo được phiên thanh toán.";
    return RedirectToPage(new { Status, PageNumber });
  }

  var payload = await response.Content.ReadFromJsonAsync<StripeCheckoutRedirectResponse>(cancellationToken);
  if (string.IsNullOrWhiteSpace(payload?.CheckoutUrl))
  {
    ActionError = "Không nhận được liên kết thanh toán.";
    return RedirectToPage(new { Status, PageNumber });
  }

  return Redirect(payload.CheckoutUrl);
  }

  private async Task LoadOrdersAsync(CancellationToken cancellationToken)
  {
  if (!IsValidStatus(Status))
    Status = string.Empty;

  if (PageNumber < 1)
    PageNumber = 1;

  var query = new List<string>
  {
    $"pageNumber={PageNumber}",
    $"pageSize={PageSize}",
    "sortBy=orderdate_desc"
  };

  if (!string.IsNullOrWhiteSpace(Status))
    query.Add($"status={Uri.EscapeDataString(Status)}");

  try
  {
    var client = _httpClientFactory.CreateClient(AuthConstants.ApiClientName);
    using var response = await client.GetAsync($"api/orders/me?{string.Join('&', query)}", cancellationToken);
    if (!response.IsSuccessStatusCode)
    {
      ErrorMessage = await ReadApiErrorAsync(response, cancellationToken)
        ?? "Không tải được danh sách đơn hàng.";
      Orders = new PagedResult<MyOrderResponse>([], 0, PageNumber, PageSize);
      return;
    }

    var paged = await response.Content.ReadFromJsonAsync<PagedResult<MyOrderResponse>>(JsonOptions, cancellationToken);
    Orders = paged ?? new PagedResult<MyOrderResponse>([], 0, PageNumber, PageSize);
    PageNumber = Orders.PageNumber;
  }
  catch
  {
    ErrorMessage = "Không tải được danh sách đơn hàng.";
    Orders = new PagedResult<MyOrderResponse>([], 0, PageNumber, PageSize);
  }
  }

  private static bool IsValidStatus(string? status)
  {
  if (string.IsNullOrWhiteSpace(status))
    return true;

  return StatusFilters.Any(filter => string.Equals(filter.Key, status, StringComparison.OrdinalIgnoreCase));
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

  private sealed class StripeCheckoutRedirectResponse
  {
    public string? CheckoutUrl { get; set; }
  }
}
