using System.Net;
using System.Text.Json;
using ECommerce.SharedViewModels.DTOs.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.Web.Pages.Orders;

[Authorize]
public class PaymentPendingModel : PageModel
{
    private static readonly TimeSpan OrderApiTimeout = TimeSpan.FromSeconds(8);

    private readonly IHttpClientFactory _httpClientFactory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

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
            var client = _httpClientFactory.CreateClient();
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
            if (order == null)
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
}
