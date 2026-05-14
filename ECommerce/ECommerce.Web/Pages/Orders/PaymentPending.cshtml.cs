using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ECommerce.SharedViewModels.DTOs.Response;
using ECommerce.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.Web.Pages.Orders;

[Authorize]
public class PaymentPendingModel : PageModel
{
  private static readonly TimeSpan OrderApiTimeout = TimeSpan.FromSeconds(8);

  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    PropertyNameCaseInsensitive = true
  };

  private readonly IHttpClientFactory _httpClientFactory;

  public PaymentPendingModel(IHttpClientFactory httpClientFactory)
  {
    _httpClientFactory = httpClientFactory;
  }

  public int? OrderId { get; private set; }

  public string? PageError { get; private set; }

  public bool OrderLoaded { get; private set; }

  public string? PaymentStatus { get; private set; }

  public bool CanRetryOnlinePayment { get; private set; }

  public decimal? TotalAmount { get; private set; }

  [TempData]
  public string? ActionError { get; set; }

  public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken = default)
  {
    if (!int.TryParse(Request.Query["orderId"].FirstOrDefault(), out var id) || id < 1)
    {
      PageError = "Thiếu mã đơn hàng. Vui lòng vào mục Đơn hàng của tôi để chọn đơn cần thanh toán lại.";
      return Page();
    }

    OrderId = id;

    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    timeoutCts.CancelAfter(OrderApiTimeout);

    try
    {
      var client = _httpClientFactory.CreateClient(AuthConstants.ApiClientName);
      using var response = await client.GetAsync($"api/orders/{id}", timeoutCts.Token);

      if (response.StatusCode == HttpStatusCode.Unauthorized)
      {
        PageError = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại để tiếp tục thanh toán.";
        return Page();
      }

      if (response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.Forbidden)
      {
        PageError = "Không tìm thấy đơn hàng hoặc bạn không có quyền xem đơn này.";
        return Page();
      }

      if (!response.IsSuccessStatusCode)
      {
        PageError = "Không tải được thông tin đơn hàng. Vui lòng thử lại sau.";
        return Page();
      }

      var json = await response.Content.ReadAsStringAsync(timeoutCts.Token);
      var order = JsonSerializer.Deserialize<OrderDetailsResponse>(json, JsonOptions);
      if (order is null)
      {
        PageError = "Không đọc được dữ liệu đơn hàng.";
        return Page();
      }

      if (string.Equals(order.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase))
        return RedirectToPage("/Orders/Details", new { id });

      OrderLoaded = true;
      PaymentStatus = order.PaymentStatus;
      TotalAmount = order.TotalAmount;
      CanRetryOnlinePayment = order.CanRetryOnlinePayment;
    }
    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
    {
      PageError = "Không tải được thông tin đơn hàng kịp thời. Vui lòng thử lại từ mục Đơn hàng của tôi.";
    }
    catch (HttpRequestException)
    {
      PageError = "Không kết nối được máy chủ đơn hàng. Vui lòng thử lại sau.";
    }

    return Page();
  }

  public async Task<IActionResult> OnPostPayAgainAsync(int orderId, CancellationToken cancellationToken = default)
  {
    if (orderId < 1)
    {
      ActionError = "Mã đơn hàng không hợp lệ.";
      return RedirectToPage(new { orderId });
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
      return RedirectToPage(new { orderId });
    }

    var payload = await response.Content.ReadFromJsonAsync<StripeCheckoutRedirectResponse>(cancellationToken);
    if (string.IsNullOrWhiteSpace(payload?.CheckoutUrl))
    {
      ActionError = "Không nhận được liên kết thanh toán.";
      return RedirectToPage(new { orderId });
    }

    return Redirect(payload.CheckoutUrl);
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
