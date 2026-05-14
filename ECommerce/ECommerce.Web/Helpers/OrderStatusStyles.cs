namespace ECommerce.Web.Helpers;

public static class OrderStatusStyles
{
  public static string BadgeClass(string? status)
  {
    return (status ?? string.Empty).ToLowerInvariant() switch
    {
      "pending" => "bg-warning text-dark",
      "processing" => "bg-info text-dark",
      "shipping" => "bg-primary",
      "delivered" => "bg-success",
      "cancelled" => "bg-danger",
      _ => "bg-dark"
    };
  }

  public static string FormatStatus(string? status)
  {
    return (status ?? string.Empty).ToLowerInvariant() switch
    {
      "pending" => "Chờ xác nhận",
      "processing" => "Đang xử lý",
      "shipping" => "Đang giao hàng",
      "delivered" => "Đã giao",
      "cancelled" => "Đã hủy",
      _ => string.IsNullOrWhiteSpace(status) ? "—" : status
    };
  }
}
