using System.Net.Http.Json;
using System.Text.Json;
using ECommerce.SharedViewModels.DTOs.Response;
using ECommerce.Web.Auth;
using ECommerce.Web.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.Web.Pages.Orders;

[Authorize]
public class DetailsModel : PageModel
{
  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    PropertyNameCaseInsensitive = true
  };

  private readonly IHttpClientFactory _httpClientFactory;

  public DetailsModel(IHttpClientFactory httpClientFactory)
  {
    _httpClientFactory = httpClientFactory;
  }

  public OrderDetailsResponse? Order { get; private set; }

  public string? ErrorMessage { get; private set; }

  [TempData]
  public string? ActionError { get; set; }

  public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
  {
    if (id < 1)
    {
      ErrorMessage = "Mã đơn hàng không hợp lệ.";
      return Page();
    }

    await LoadOrderAsync(id, cancellationToken);
    return Page();
  }

  public async Task<IActionResult> OnPostCancelAsync(int id, CancellationToken cancellationToken)
  {
    if (id < 1)
    {
      ActionError = "Id đơn hàng không hợp lệ.";
      return RedirectToPage(new { id });
    }

    var client = _httpClientFactory.CreateClient(AuthConstants.ApiClientName);
    using var response = await client.PostAsync($"api/orders/{id}/cancel", null, cancellationToken);
    if (!response.IsSuccessStatusCode)
    {
      ActionError = await ReadApiErrorAsync(response, cancellationToken)
        ?? "Không hủy được đơn hàng.";
    }

    return RedirectToPage(new { id });
  }

  public async Task<IActionResult> OnPostPayAgainAsync(int id, CancellationToken cancellationToken)
  {
    if (id < 1)
    {
      ActionError = "Id đơn hàng không hợp lệ.";
      return RedirectToPage(new { id });
    }

    var client = _httpClientFactory.CreateClient(AuthConstants.ApiClientName);
    using var request = new HttpRequestMessage(HttpMethod.Post, $"api/payments/stripe/orders/{id}/retry")
    {
      Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
    };

    using var response = await client.SendAsync(request, cancellationToken);
    if (!response.IsSuccessStatusCode)
    {
      ActionError = await ReadApiErrorAsync(response, cancellationToken)
        ?? "Không tạo được phiên thanh toán.";
      return RedirectToPage(new { id });
    }

    var payload = await response.Content.ReadFromJsonAsync<StripeCheckoutRedirectResponse>(cancellationToken);
    if (string.IsNullOrWhiteSpace(payload?.CheckoutUrl))
    {
      ActionError = "Không nhận được liên kết thanh toán.";
      return RedirectToPage(new { id });
    }

    return Redirect(payload.CheckoutUrl);
  }

  private async Task LoadOrderAsync(int id, CancellationToken cancellationToken)
  {
    try
    {
      var client = _httpClientFactory.CreateClient(AuthConstants.ApiClientName);
      using var response = await client.GetAsync($"api/orders/{id}", cancellationToken);
      if (!response.IsSuccessStatusCode)
      {
        ErrorMessage = await ReadApiErrorAsync(response, cancellationToken)
          ?? "Không tải được chi tiết đơn hàng.";
        return;
      }

      Order = await response.Content.ReadFromJsonAsync<OrderDetailsResponse>(JsonOptions, cancellationToken);
      if (Order is null)
        ErrorMessage = "Không tải được chi tiết đơn hàng.";
    }
    catch
    {
      ErrorMessage = "Không tải được chi tiết đơn hàng.";
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

  private sealed class StripeCheckoutRedirectResponse
  {
    public string? CheckoutUrl { get; set; }
  }
}
