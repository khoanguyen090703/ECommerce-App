using System.Net;
using System.Text.Json;
using ECommerce.SharedViewModels.DTOs.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.Web.Pages;

[Authorize]
public class OrderSuccessModel : PageModel
{
    private static readonly TimeSpan OrderApiTimeout = TimeSpan.FromSeconds(8);

    private readonly IHttpClientFactory _httpClientFactory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public OrderSuccessModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public int? OrderId { get; private set; }

    public decimal? TotalAmount { get; private set; }

    public string? PageError { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(Request.Query["orderId"].FirstOrDefault(), out var id) || id < 1)
        {
            PageError = "Thiếu mã đơn hàng. Vui lòng vào mục Đơn hàng của tôi để xem đơn vừa đặt.";
            return;
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
                PageError = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại để xem đơn hàng.";
                return;
            }

            if (response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.Forbidden)
            {
                PageError = "Không tìm thấy đơn hàng hoặc bạn không có quyền xem đơn này.";
                return;
            }

            if (!response.IsSuccessStatusCode)
            {
                PageError = "Không tải được thông tin đơn hàng. Vui lòng thử lại sau hoặc xem trong mục Đơn hàng của tôi.";
                return;
            }

            var json = await response.Content.ReadAsStringAsync(timeoutCts.Token);
            var order = JsonSerializer.Deserialize<OrderDetailsResponse>(json, JsonOptions);
            if (order == null)
            {
                PageError = "Không đọc được dữ liệu đơn hàng.";
                return;
            }

            TotalAmount = order.TotalAmount;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            PageError = "Không tải được thông tin đơn hàng kịp thời. Bạn vẫn có thể mở chi tiết đơn bên dưới.";
        }
        catch (HttpRequestException)
        {
            PageError = "Không kết nối được máy chủ đơn hàng. Bạn vẫn có thể mở chi tiết đơn bên dưới.";
        }
    }
}
